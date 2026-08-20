using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Util;
using Java.Util;

namespace TacxRpmApp;

public sealed class HeartRateService
{
    private static readonly UUID HeartRateServiceUuid =
        UUID.FromString("0000180D-0000-1000-8000-00805F9B34FB");

    private static readonly UUID HeartRateMeasurementUuid =
        UUID.FromString("00002A37-0000-1000-8000-00805F9B34FB");

    private static readonly UUID ClientCharacteristicConfigurationUuid =
        UUID.FromString("00002902-0000-1000-8000-00805F9B34FB");

    private readonly Context _context;

    private BluetoothGatt? _gatt;
    private BluetoothGattCharacteristic? _heartRateMeasurement;

    private readonly Dictionary<string, BluetoothDevice> _discoveredDevices = new();

    private bool _disposed;

    public string LastConnectionStatus { get; private set; } = "";

    public int? HeartRate { get; private set; }

    public event EventHandler<int>? HeartRateReceived;

    public HeartRateService()
    {
        _context = Android.App.Application.Context;
    }

    public async Task<IReadOnlyList<BleDeviceInfo>> ScanDevicesAsync(
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeartRateService));
        }

        try
        {
            var bluetoothManager =
                (BluetoothManager?)_context.GetSystemService(
                    Context.BluetoothService);

            var adapter = bluetoothManager?.Adapter;

            if (adapter == null)
            {
                LastConnectionStatus =
                    "Bluetooth não está disponível neste dispositivo.";

                return Array.Empty<BleDeviceInfo>();
            }

            if (!adapter.IsEnabled)
            {
                LastConnectionStatus =
                    "Bluetooth está desligado.";

                return Array.Empty<BleDeviceInfo>();
            }

            var scanner = adapter.BluetoothLeScanner;

            if (scanner == null)
            {
                LastConnectionStatus =
                    "BLE scanner indisponível.";

                return Array.Empty<BleDeviceInfo>();
            }

            var discoveredDevices =
                new Dictionary<string, BluetoothDevice>();

            var scanCallback =
                new HeartRateScanCallback(discoveredDevices);

            var filter = new ScanFilter.Builder()
                .SetServiceUuid(
                    new ParcelUuid(HeartRateServiceUuid))
                .Build();

            var settings = new ScanSettings.Builder()
                .SetScanMode(ScanMode.LowLatency)
                .Build();

            lock (_discoveredDevices)
            {
                _discoveredDevices.Clear();
            }

            scanner.StartScan(
                new List<ScanFilter> { filter },
                settings,
                scanCallback);

            try
            {
                await Task.Delay(
                    duration ?? TimeSpan.FromSeconds(8),
                    cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Normal cancellation.
            }
            finally
            {
                scanner.StopScan(scanCallback);
            }

            lock (_discoveredDevices)
            {
                _discoveredDevices.Clear();

                foreach (var device in discoveredDevices)
                {
                    _discoveredDevices[device.Key] = device.Value;
                }
            }

            LastConnectionStatus =
                discoveredDevices.Count == 0
                    ? "Nenhum monitor cardíaco encontrado."
                    : $"{discoveredDevices.Count} monitor(es) cardíaco(s) encontrado(s).";

            return scanCallback.Devices;
        }
        catch (Java.Lang.SecurityException)
        {
            LastConnectionStatus =
                "Permissão Bluetooth negada.";

            return Array.Empty<BleDeviceInfo>();
        }
        catch (Exception ex)
        {
            Log.Error(
                "TacxRpmApp",
                $"Heart Rate scan failed: {ex}");

            LastConnectionStatus =
                "Falha ao procurar monitores cardíacos.";

            return Array.Empty<BleDeviceInfo>();
        }
    }

    public async Task<bool> ConnectAsync(
        BleDeviceInfo deviceInfo,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeartRateService));
        }

        LastConnectionStatus = "A iniciar ligação...";

        try
        {
            BluetoothDevice? device;

            lock (_discoveredDevices)
            {
                _discoveredDevices.TryGetValue(
                    deviceInfo.Address,
                    out device);
            }

            if (device == null)
            {
                var bluetoothManager =
                    (BluetoothManager?)_context.GetSystemService(
                        Context.BluetoothService);

                var adapter = bluetoothManager?.Adapter;

                device = adapter?.GetRemoteDevice(
                    deviceInfo.Address);
            }

            if (device == null)
            {
                LastConnectionStatus =
                    "Monitor cardíaco indisponível.";

                return false;
            }

            Disconnect();

            var connectionTcs =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            var callback =
                new HeartRateGattCallback(
                    this,
                    connectionTcs);

            _gatt = device.ConnectGatt(
                _context,
                false,
                callback,
                BluetoothTransports.Le);

            if (_gatt == null)
            {
                LastConnectionStatus =
                    "Não foi possível iniciar a ligação.";

                return false;
            }

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCts.CancelAfter(
                TimeSpan.FromSeconds(12));

            try
            {
                await connectionTcs.Task.WaitAsync(
                    timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                _gatt?.Close();
                _gatt = null;

                if (cancellationToken.IsCancellationRequested)
                {
                    LastConnectionStatus =
                        "Ligação cancelada.";
                }
                else
                {
                    LastConnectionStatus =
                        "Tempo limite da ligação excedido.";
                }

                return false;
            }

            return connectionTcs.Task.Result;
        }
        catch (Java.Lang.SecurityException)
        {
            LastConnectionStatus =
                "Permissão Bluetooth negada.";

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(
                "TacxRpmApp",
                $"Heart Rate connection failed: {ex}");

            LastConnectionStatus =
                "Falha na ligação ao monitor cardíaco.";

            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            _gatt?.Disconnect();
        }
        catch
        {
            // Ignore disconnect errors.
        }

        try
        {
            _gatt?.Close();
        }
        catch
        {
            // Ignore close errors.
        }

        _gatt = null;
        _heartRateMeasurement = null;

        LastConnectionStatus = "Desligado.";
    }

    private void HandleConnected(
        BluetoothGatt gatt)
    {
        LastConnectionStatus =
            "Ligado; a descobrir serviços...";

        var started = gatt.DiscoverServices();

        if (!started)
        {
            LastConnectionStatus =
                "Não foi possível descobrir os serviços.";

            gatt.Close();
        }
    }

    private void HandleServicesDiscovered(
        BluetoothGatt gatt,
        GattStatus status,
        TaskCompletionSource<bool> connectionTcs)
    {
        if (status != GattStatus.Success)
        {
            LastConnectionStatus =
                $"Falha ao descobrir serviços ({status}).";

            connectionTcs.TrySetResult(false);
            return;
        }

        var heartRateService =
            gatt.GetService(HeartRateServiceUuid);

        if (heartRateService == null)
        {
            LastConnectionStatus =
                "O dispositivo não oferece o serviço Heart Rate (180D).";

            connectionTcs.TrySetResult(false);
            return;
        }

        var measurement =
            heartRateService.GetCharacteristic(
                HeartRateMeasurementUuid);

        if (measurement == null)
        {
            LastConnectionStatus =
                "O dispositivo não oferece " +
                "Heart Rate Measurement (2A37).";

            connectionTcs.TrySetResult(false);
            return;
        }

        _gatt = gatt;
        _heartRateMeasurement = measurement;

        var properties = measurement.Properties;

        Log.Debug(
            "TacxRpmApp",
            $"Heart Rate Measurement properties: {properties}");

        var notificationEnabled =
            EnableHeartRateNotifications(gatt);

        if (!notificationEnabled)
        {
            LastConnectionStatus =
                "Não foi possível ativar as notificações " +
                "do monitor cardíaco.";

            connectionTcs.TrySetResult(false);
            return;
        }

        LastConnectionStatus =
            "Monitor cardíaco ligado; " +
            "a receber frequência cardíaca.";

        connectionTcs.TrySetResult(true);
    }

    private bool EnableHeartRateNotifications(
        BluetoothGatt gatt)
    {
        if (_heartRateMeasurement == null)
        {
            return false;
        }

        var localNotificationEnabled =
            gatt.SetCharacteristicNotification(
                _heartRateMeasurement,
                true);

        if (!localNotificationEnabled)
        {
            Log.Warn(
                "TacxRpmApp",
                "SetCharacteristicNotification returned false.");

            return false;
        }

        var descriptor =
            _heartRateMeasurement.GetDescriptor(
                ClientCharacteristicConfigurationUuid);

        if (descriptor == null)
        {
            Log.Warn(
                "TacxRpmApp",
                "Heart Rate CCCD descriptor not found.");

            return false;
        }

        descriptor.SetValue(
            BluetoothGattDescriptor.EnableNotificationValue.ToArray());

        var descriptorWriteStarted =
            gatt.WriteDescriptor(descriptor);

        Log.Debug(
            "TacxRpmApp",
            $"Heart Rate notifications: " +
            $"local={localNotificationEnabled}, " +
            $"descriptor={descriptorWriteStarted}");

        return descriptorWriteStarted;
    }

    private void ProcessHeartRateMeasurement(
        byte[] packet)
    {
        if (packet.Length < 2)
        {
            return;
        }

        var flags = packet[0];

        // Flags bit 0:
        // 0 = UINT8 heart rate value
        // 1 = UINT16 heart rate value
        var is16Bit = (flags & 0x01) != 0;

        int heartRate;

        if (is16Bit)
        {
            if (packet.Length < 3)
            {
                return;
            }

            heartRate =
                packet[1] |
                (packet[2] << 8);
        }
        else
        {
            heartRate = packet[1];
        }

        // Sanity check. Avoid exposing obviously invalid values.
        if (heartRate <= 0 || heartRate > 300)
        {
            Log.Warn(
                "TacxRpmApp",
                $"Ignoring invalid heart rate: {heartRate}");

            return;
        }

        HeartRate = heartRate;

        Log.Debug(
            "TacxRpmApp",
            $"Heart Rate: {heartRate} BPM " +
            $"[{Convert.ToHexString(packet)}]");

        HeartRateReceived?.Invoke(
            this,
            heartRate);
    }

    private void HandleDisconnected(
        GattStatus status,
        TaskCompletionSource<bool>? connectionTcs)
    {
        LastConnectionStatus =
            $"Monitor cardíaco desligado ({status}).";

        _heartRateMeasurement = null;

        connectionTcs?.TrySetResult(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Disconnect();
    }

    private sealed class HeartRateGattCallback : BluetoothGattCallback
    {
        private readonly HeartRateService _service;
        private readonly TaskCompletionSource<bool> _connectionTcs;

        public HeartRateGattCallback(
            HeartRateService service,
            TaskCompletionSource<bool> connectionTcs)
        {
            _service = service;
            _connectionTcs = connectionTcs;
        }

        public override void OnConnectionStateChange(
            BluetoothGatt gatt,
            GattStatus status,
            ProfileState newState)
        {
            Log.Debug(
                "TacxRpmApp",
                $"Heart Rate connection state: " +
                $"status={status}, state={newState}");

            if (newState == ProfileState.Connected)
            {
                _service.HandleConnected(gatt);
            }
            else if (newState == ProfileState.Disconnected)
            {
                _service.HandleDisconnected(
                    status,
                    _connectionTcs);
            }
        }

        public override void OnServicesDiscovered(
            BluetoothGatt gatt,
            GattStatus status)
        {
            _service.HandleServicesDiscovered(
                gatt,
                status,
                _connectionTcs);
        }

        public override void OnCharacteristicChanged(
            BluetoothGatt gatt,
            BluetoothGattCharacteristic characteristic)
        {
            if (characteristic.Uuid?.Equals(
                    HeartRateMeasurementUuid) != true)
            {
                return;
            }

            var packet =
                characteristic.GetValue()
                ?? Array.Empty<byte>();

            Log.Debug(
                "TacxRpmApp",
                $"Heart Rate notification: " +
                $"{Convert.ToHexString(packet)}");

            _service.ProcessHeartRateMeasurement(packet);
        }
    }

    private sealed class HeartRateScanCallback : ScanCallback
    {
        private readonly Dictionary<string, BleDeviceInfo> _devices = new();

        private readonly Dictionary<string, BluetoothDevice>
            _bluetoothDevices;

        public IReadOnlyList<BleDeviceInfo> Devices =>
            _devices.Values.ToList();

        public HeartRateScanCallback(
            Dictionary<string, BluetoothDevice> bluetoothDevices)
        {
            _bluetoothDevices = bluetoothDevices;
        }

        public override void OnScanResult(
            ScanCallbackType callbackType,
            ScanResult? result)
        {
            var device = result?.Device;

            if (device == null)
            {
                return;
            }

            var address = device.Address;

            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            var deviceName = device.Name;

            if (string.IsNullOrWhiteSpace(deviceName))
            {
                deviceName = "Heart Rate Monitor";
            }

            _devices[address] = new BleDeviceInfo(
                deviceName,
                address);

            _bluetoothDevices[address] = device;

            Log.Debug(
                "TacxRpmApp",
                $"Heart Rate device found: " +
                $"{deviceName} ({address})");
        }

        public override void OnScanFailed(
            ScanFailure errorCode)
        {
            Log.Error(
                "TacxRpmApp",
                $"Heart Rate scan failed: {errorCode}");
        }
    }
}
