using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Java.Util;

namespace TacxRpmApp;

public class TacxNeoService
{
    private readonly UUID FTMS_SERVICE = UUID.FromString("00001826-0000-1000-8000-00805f9b34fb");
    private readonly UUID CONTROL_POINT = UUID.FromString("00002ad9-0000-1000-8000-00805f9b34fb");
    private readonly UUID TACX_SERVICE = UUID.FromString("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
    private readonly UUID TACX_WRITE = UUID.FromString("6e40fec3-b5a3-f393-e0a9-e50e24dcca9e");

    private BluetoothGatt? _gatt;
    private BluetoothGattCharacteristic? _controlPoint;
    private readonly Dictionary<string, BluetoothDevice> _discoveredDevices = new();

    private readonly Context _context;

    public string LastConnectionStatus { get; private set; } = "";

    public TacxNeoService()
    {
        _context = Android.App.Application.Context;
    }

    public async Task<IReadOnlyList<BleDeviceInfo>> ScanDevicesAsync()
    {
        try
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
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

            device ??= BluetoothAdapter.DefaultAdapter?.GetRemoteDevice(deviceInfo.Address);
            if (device == null)
            {
                LastConnectionStatus = "Dispositivo indisponível.";
                return false;
            }

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                _gatt?.Close();
                _controlPoint = null;

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

    public Task SetResistanceAsync(ushort newtonValue)
    {
        LastConnectionStatus = "Comandos bloqueados até confirmar o protocolo Tacx.";
        return Task.CompletedTask;
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
            var tacxWrite = tacxService?.GetCharacteristic(_service.TACX_WRITE);
            controlPoint ??= tacxWrite;
            _service._controlPoint = controlPoint;
            if (service == null)
            {
                if (tacxWrite != null)
                {
                    _service.LastConnectionStatus = "Ligado via serviço proprietário Tacx.";
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
            _tcs.TrySetResult(controlPoint != null);
        }
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
                    address,
                    advertisedServices.Count == 0
                        ? "Nenhum serviço anunciado"
                        : string.Join(", ", advertisedServices));
                _bluetoothDevices[address] = result.Device;
            }
        }

        public override void OnScanFailed(ScanFailure errorCode)
        {
        }
    }
}
