using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Util;
using Java.Util;

namespace TacxRpmApp;

public class TacxNeoService
{
    private readonly UUID FTMS_SERVICE = UUID.FromString("00001826-0000-1000-8000-00805f9b34fb");
    private readonly UUID CONTROL_POINT = UUID.FromString("00002ad9-0000-1000-8000-00805f9b34fb");
    private readonly UUID TACX_SERVICE = UUID.FromString("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
    private readonly UUID TACX_NOTIFY = UUID.FromString("6e40fec2-b5a3-f393-e0a9-e50e24dcca9e");
    private readonly UUID TACX_WRITE = UUID.FromString("6e40fec3-b5a3-f393-e0a9-e50e24dcca9e");
    private readonly UUID CLIENT_CHARACTERISTIC_CONFIGURATION = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");

    private BluetoothGatt? _gatt;
    private BluetoothGattCharacteristic? _controlPoint;
    private BluetoothGattCharacteristic? _tacxNotify;
    private BluetoothGattCharacteristic? _tacxWrite;
    private readonly Dictionary<string, BluetoothDevice> _discoveredDevices = new();

    private readonly Context _context;

    public string LastConnectionStatus { get; private set; } = "";
    public string LastFeCNotification { get; private set; } = "";
    public string LastCommandStatus { get; private set; } = "";
    public ushort? MaximumResistance { get; private set; }

    public TacxNeoService()
    {
        _context = Android.App.Application.Context;
    }

    public async Task<IReadOnlyList<BleDeviceInfo>> ScanDevicesAsync()
    {
        try
        {
            // var adapter = BluetoothAdapter.DefaultAdapter ; DEPRECATED
            var bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
            var adapter = bluetoothManager?.Adapter;
            
            if (adapter == null || !adapter.IsEnabled)
            {
                return Array.Empty<BleDeviceInfo>();
            }

            var scanner = adapter?.BluetoothLeScanner;
            if (scanner == null)
            {
                return Array.Empty<BleDeviceInfo>();
            }

            var discoveredDevices = new Dictionary<string, BluetoothDevice>();
            var scanCallback = new TacxScanCallback(discoveredDevices);
            scanner.StartScan(scanCallback);

            await Task.Delay(TimeSpan.FromSeconds(8));
            scanner.StopScan(scanCallback);
            lock (_discoveredDevices)
            {
                _discoveredDevices.Clear();
                foreach (var device in discoveredDevices)
                {
                    _discoveredDevices[device.Key] = device.Value;
                }
            }
            return scanCallback.Devices;
        }
        catch (Java.Lang.SecurityException)
        {
            return Array.Empty<BleDeviceInfo>();
        }
    }

    public async Task<bool> ConnectAsync(BleDeviceInfo deviceInfo)
    {
        LastConnectionStatus = "A iniciar ligação...";
        try
        {
            BluetoothDevice? device;
            lock (_discoveredDevices)
            {
                _discoveredDevices.TryGetValue(deviceInfo.Address, out device);
            }

            // device ??= BluetoothAdapter.DefaultAdapter?.GetRemoteDevice(deviceInfo.Address); DEPRECATED
            var bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
            var adapter = bluetoothManager?.Adapter;
            device ??= adapter?.GetRemoteDevice(deviceInfo.Address);
            
            if (device == null)
            {
                LastConnectionStatus = "Dispositivo indisponível.";
                return false;
            }

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                _gatt?.Close();
                _controlPoint = null;
                _tacxNotify = null;
                _tacxWrite = null;

                var connectionTaskSource = new TaskCompletionSource<bool>();
                _gatt = device.ConnectGatt(
                    _context,
                    false,
                    new GattCallback(this, connectionTaskSource),
                    BluetoothTransports.Le);
                var connectionCompletedTask = await Task.WhenAny(connectionTaskSource.Task, Task.Delay(TimeSpan.FromSeconds(12)));
                if (connectionCompletedTask == connectionTaskSource.Task && connectionTaskSource.Task.Result)
                {
                    return true;
                }

                _gatt?.Close();
                if (attempt < 2)
                {
                    LastConnectionStatus = "A repetir a ligação...";
                    await Task.Delay(700);
                }
            }

            if (string.IsNullOrEmpty(LastConnectionStatus)
                || LastConnectionStatus == "A repetir a ligação...")
            {
                LastConnectionStatus = "Ligação Bluetooth falhou (133).";
            }

            return false;
        }
        catch (Java.Lang.SecurityException)
        {
            LastConnectionStatus = "Permissão Bluetooth negada.";
            return false;
        }
    }

    public Task SetResistanceAsync(ushort resistanceValue)
    {
        var clampedValue = (byte)Math.Clamp((int)resistanceValue, 0, 200);
        SendBasicResistance(clampedValue);
        return Task.CompletedTask;
    }

    private void SendBasicResistance(byte totalResistance)
    {
        if (_gatt == null || _tacxWrite == null)
        {
            LastConnectionStatus = "Trainer Tacx não está ligado para enviar FE-C.";
            return;
        }

        var packet = new byte[]
        {
            0xA4, 0x09, 0x4E, 0x05, 0x30,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            totalResistance, 0x00
        };
        packet[^1] = CalculateFeCChecksum(packet.AsSpan(0, packet.Length - 1));

        _tacxWrite.WriteType = GattWriteType.NoResponse;
        _tacxWrite.SetValue(packet);
        var accepted = _gatt.WriteCharacteristic(_tacxWrite);
        LastConnectionStatus = accepted
            ? $"Comando FE-C enviado: resistência {totalResistance}/200."
            : "Falha ao enviar comando FE-C.";
        Log.Debug("TacxRpmApp", $"FE-C basic resistance: {Convert.ToHexString(packet)}, accepted={accepted}");
    }

    private void ProcessTacxNotification(byte[] packet)
    {
        LastFeCNotification = Convert.ToHexString(packet);
        if (packet.Length < 13 || packet[0] != 0xA4 || packet[2] != 0x4E)
        {
            return;
        }

        switch (packet[4])
        {
            case 0x36:
                MaximumResistance = (ushort)(packet[9] | (packet[10] << 8));
                Log.Debug("TacxRpmApp", $"FE-C capabilities: maximum resistance={MaximumResistance}");
                break;
            case 0x47:
                LastCommandStatus = packet[7] switch
                {
                    0x00 => "success",
                    0x01 => "fail",
                    0x02 => "not supported",
                    0x03 => "rejected",
                    0xFF => "uninitialized",
                    _ => $"unknown (0x{packet[7]:X2})"
                };
                LastConnectionStatus = $"Estado comando FE-C: {LastCommandStatus}.";
                Log.Debug("TacxRpmApp", $"FE-C command status: command=0x{packet[5]:X2}, status={LastCommandStatus}");
                break;
        }
    }

    private static byte CalculateFeCChecksum(ReadOnlySpan<byte> packet)
    {
        byte checksum = 0;
        foreach (var value in packet)
        {
            checksum ^= value;
        }

        return checksum;
    }

    private class GattCallback : BluetoothGattCallback
    {
        private readonly TacxNeoService _service;
        private readonly TaskCompletionSource<bool> _tcs;

        public GattCallback(TacxNeoService service, TaskCompletionSource<bool> tcs)
        {
            _service = service;
            _tcs = tcs;
        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            if (newState == ProfileState.Connected)
            {
                gatt.DiscoverServices();
            }
            else if (newState == ProfileState.Disconnected)
            {
                _service.LastConnectionStatus = $"Ligação Bluetooth falhou ({status}).";
                _tcs.TrySetResult(false);
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            var service = gatt.GetService(_service.FTMS_SERVICE);
            var controlPoint = service?.GetCharacteristic(_service.CONTROL_POINT);
            var tacxService = gatt.GetService(_service.TACX_SERVICE);
            var tacxNotify = tacxService?.GetCharacteristic(_service.TACX_NOTIFY);
            var tacxWrite = tacxService?.GetCharacteristic(_service.TACX_WRITE);
            controlPoint ??= tacxWrite;
            _service._controlPoint = controlPoint;
            _service._tacxNotify = tacxNotify;
            _service._tacxWrite = tacxWrite;
            if (service == null)
            {
                if (tacxWrite != null)
                {
                    _service.EnableTacxNotifications(gatt);
                    _service.LastConnectionStatus = tacxNotify == null
                        ? "Ligado via serviço proprietário Tacx, sem notificações."
                        : "Ligado via serviço proprietário Tacx; notificações ativas.";
                    _tcs.TrySetResult(true);
                    return;
                }

                var discoveredServices = gatt.Services?
                    .Where(discoveredService => discoveredService != null)
                    .Select(discoveredService =>
                    {
                        var serviceUuid = discoveredService.Uuid?.ToString() ?? "?";
                        var characteristics = discoveredService.Characteristics?
                            .Select(characteristic =>
                                $"{characteristic.Uuid} ({characteristic.Properties})")
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .ToList() ?? new List<string?>();
                        return $"{serviceUuid} [{string.Join(", ", characteristics)}]";
                    })
                    .ToList() ?? new List<string?>();
                var serviceList = discoveredServices.Count == 0
                    ? "nenhum serviço"
                    : string.Join(", ", discoveredServices);
                _service.LastConnectionStatus = $"Sem FTMS. Serviços: {serviceList}";
            }
            else if (controlPoint == null)
            {
                _service.LastConnectionStatus = "O dispositivo não oferece o controlo FTMS.";
            }
            else if (tacxNotify != null)
            {
                _service.EnableTacxNotifications(gatt);
                _service.LastConnectionStatus = "Ligado; notificações Tacx ativas.";
            }
            _tcs.TrySetResult(controlPoint != null);
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            if (characteristic.Uuid?.Equals(_service.TACX_NOTIFY) == true)
            {
                var packet = characteristic.GetValue() ?? Array.Empty<byte>();
                Log.Debug("TacxRpmApp", $"FE-C notification: {Convert.ToHexString(packet)}");
                _service.ProcessTacxNotification(packet);
                if (packet.Length < 5 || packet[4] != 0x47)
                {
                    _service.LastConnectionStatus = $"Notificação FE-C recebida ({packet.Length} bytes).";
                }
            }
        }
    }

    private void EnableTacxNotifications(BluetoothGatt gatt)
    {
        if (_tacxNotify == null)
        {
            return;
        }

        var notificationEnabled = gatt.SetCharacteristicNotification(_tacxNotify, true);
        var descriptor = _tacxNotify.GetDescriptor(CLIENT_CHARACTERISTIC_CONFIGURATION);
        var descriptorWriteStarted = false;
        if (descriptor != null)
        {
            descriptor.SetValue(new byte[] { 0x01, 0x00 });
            descriptorWriteStarted = gatt.WriteDescriptor(descriptor);
        }

        Log.Debug(
            "TacxRpmApp",
            $"FE-C notifications: local={notificationEnabled}, descriptor={descriptorWriteStarted}");
    }

    private sealed class TacxScanCallback : ScanCallback
    {
        private readonly Dictionary<string, BleDeviceInfo> _devices = new();
        private readonly Dictionary<string, BluetoothDevice> _bluetoothDevices;

        public IReadOnlyList<BleDeviceInfo> Devices => _devices.Values.ToList();

        public TacxScanCallback(Dictionary<string, BluetoothDevice> bluetoothDevices)
        {
            _bluetoothDevices = bluetoothDevices;
        }

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            var deviceName = result?.Device?.Name;
            if (!string.IsNullOrWhiteSpace(deviceName) && result?.Device?.Address is string address)
            {
                var advertisedServices = result.ScanRecord?.ServiceUuids?
                    .Select(serviceUuid => serviceUuid.ToString())
                    .ToList() ?? new List<string>();
                _devices[address] = new BleDeviceInfo(
                    deviceName,
                    address//,
                    // advertisedServices.Count == 0
                    //     ? "Nenhum serviço anunciado"
                    //     : string.Join(", ", advertisedServices)
                );
                _bluetoothDevices[address] = result.Device;
            }
        }

        public override void OnScanFailed(ScanFailure errorCode)
        {
        }
    }
}
