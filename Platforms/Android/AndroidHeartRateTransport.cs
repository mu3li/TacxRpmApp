using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Util;
using Java.Util;
using TacxRpmApp.Core.Ble;

namespace TacxRpmApp;

/// <summary>Drives a Bluetooth Heart Rate strap (0x180D/0x2A37) over Android.Bluetooth.</summary>
public sealed class AndroidHeartRateTransport : IHeartRateTransport
{
    private static readonly UUID HeartRateServiceUuid = UUID.FromString("0000180D-0000-1000-8000-00805F9B34FB");
    private static readonly UUID HeartRateMeasurementUuid = UUID.FromString("00002A37-0000-1000-8000-00805F9B34FB");
    private static readonly UUID ClientCharacteristicConfigurationUuid = UUID.FromString("00002902-0000-1000-8000-00805F9B34FB");

    private readonly Context _context;
    private readonly Dictionary<string, BluetoothDevice> _discoveredDevices = new();

    private BluetoothGatt? _gatt;
    private BluetoothGattCharacteristic? _heartRateMeasurement;

    public event EventHandler<byte[]>? PacketReceived;
    public event EventHandler<ConnectionState>? ConnectionChanged;

    public AndroidHeartRateTransport()
    {
        _context = Android.App.Application.Context;
    }

    public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(TimeSpan duration, CancellationToken ct)
    {
        try
        {
            var bluetoothManager = (BluetoothManager?)_context.GetSystemService(Context.BluetoothService);
            var adapter = bluetoothManager?.Adapter;
            if (adapter is not { IsEnabled: true })
            {
                return Array.Empty<BleDeviceInfo>();
            }

            var scanner = adapter.BluetoothLeScanner;
            if (scanner == null)
            {
                return Array.Empty<BleDeviceInfo>();
            }

            var discoveredDevices = new Dictionary<string, BluetoothDevice>();
            var scanCallback = new HeartRateScanCallback(discoveredDevices);
            var filter = new ScanFilter.Builder().SetServiceUuid(new ParcelUuid(HeartRateServiceUuid)).Build();
            var settings = new ScanSettings.Builder().SetScanMode(ScanMode.LowLatency).Build();

            scanner.StartScan(new List<ScanFilter> { filter }, settings, scanCallback);
            try
            {
                await Task.Delay(duration, ct);
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
            return scanCallback.Devices;
        }
        catch (Java.Lang.SecurityException)
        {
            return Array.Empty<BleDeviceInfo>();
        }
    }

    public async Task<bool> ConnectAsync(BleDeviceInfo device, CancellationToken ct)
    {
        try
        {
            BluetoothDevice? bluetoothDevice;
            lock (_discoveredDevices)
            {
                _discoveredDevices.TryGetValue(device.Address, out bluetoothDevice);
            }

            if (bluetoothDevice == null)
            {
                var bluetoothManager = (BluetoothManager?)_context.GetSystemService(Context.BluetoothService);
                bluetoothDevice = bluetoothManager?.Adapter?.GetRemoteDevice(device.Address);
            }

            if (bluetoothDevice == null)
            {
                return false;
            }

            Disconnect();
            ConnectionChanged?.Invoke(this, ConnectionState.Connecting);

            var connectionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = new HeartRateGattCallback(this, connectionTcs);
            _gatt = bluetoothDevice.ConnectGatt(_context, false, callback, BluetoothTransports.Le);

            if (_gatt == null)
            {
                return false;
            }

            bool connected;
            try
            {
                connected = await connectionTcs.Task.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                _gatt?.Close();
                _gatt = null;
                connected = false;
            }

            ConnectionChanged?.Invoke(this, connected ? ConnectionState.Connected : ConnectionState.Disconnected);
            return connected;
        }
        catch (Java.Lang.SecurityException)
        {
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
    }

    private void HandleConnected(BluetoothGatt gatt)
    {
        var started = gatt.DiscoverServices();
        if (!started)
        {
            gatt.Close();
        }
    }

    private void HandleServicesDiscovered(BluetoothGatt gatt, GattStatus status, TaskCompletionSource<bool> connectionTcs)
    {
        if (status != GattStatus.Success)
        {
            connectionTcs.TrySetResult(false);
            return;
        }

        var heartRateService = gatt.GetService(HeartRateServiceUuid);
        var measurement = heartRateService?.GetCharacteristic(HeartRateMeasurementUuid);
        if (measurement == null)
        {
            connectionTcs.TrySetResult(false);
            return;
        }

        _gatt = gatt;
        _heartRateMeasurement = measurement;

        var notificationEnabled = EnableHeartRateNotifications(gatt);
        connectionTcs.TrySetResult(notificationEnabled);
    }

    private bool EnableHeartRateNotifications(BluetoothGatt gatt)
    {
        if (_heartRateMeasurement == null)
        {
            return false;
        }

        var localNotificationEnabled = gatt.SetCharacteristicNotification(_heartRateMeasurement, true);
        if (!localNotificationEnabled)
        {
            return false;
        }

        var descriptor = _heartRateMeasurement.GetDescriptor(ClientCharacteristicConfigurationUuid);
        if (descriptor == null)
        {
            return false;
        }

        descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
        return gatt.WriteDescriptor(descriptor);
    }

    private sealed class HeartRateGattCallback : BluetoothGattCallback
    {
        private readonly AndroidHeartRateTransport _transport;
        private readonly TaskCompletionSource<bool> _connectionTcs;

        public HeartRateGattCallback(AndroidHeartRateTransport transport, TaskCompletionSource<bool> connectionTcs)
        {
            _transport = transport;
            _connectionTcs = connectionTcs;
        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            if (newState == ProfileState.Connected)
            {
                _transport.HandleConnected(gatt);
            }
            else if (newState == ProfileState.Disconnected)
            {
                _transport._heartRateMeasurement = null;
                _connectionTcs.TrySetResult(false);
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
            => _transport.HandleServicesDiscovered(gatt, status, _connectionTcs);

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            if (characteristic.Uuid?.Equals(HeartRateMeasurementUuid) == true)
            {
                var packet = characteristic.GetValue() ?? Array.Empty<byte>();
                Log.Debug("TacxRpmApp", $"Heart Rate: {Convert.ToHexString(packet)}");
                _transport.PacketReceived?.Invoke(_transport, packet);
            }
        }
    }

    private sealed class HeartRateScanCallback : ScanCallback
    {
        private readonly Dictionary<string, BleDeviceInfo> _devices = new();
        private readonly Dictionary<string, BluetoothDevice> _bluetoothDevices;

        public IReadOnlyList<BleDeviceInfo> Devices => _devices.Values.ToList();

        public HeartRateScanCallback(Dictionary<string, BluetoothDevice> bluetoothDevices)
        {
            _bluetoothDevices = bluetoothDevices;
        }

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            var deviceName = result?.Device?.Name;
            if (!string.IsNullOrWhiteSpace(deviceName) && result?.Device?.Address is string address)
            {
                _devices[address] = new BleDeviceInfo(deviceName, address);
                _bluetoothDevices[address] = result.Device;
            }
        }

        public override void OnScanFailed(ScanFailure errorCode)
        {
        }
    }
}
