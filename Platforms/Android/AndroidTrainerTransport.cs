using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Util;
using Java.Util;
using TacxRpmApp.Core.Ble;

namespace TacxRpmApp;

/// <summary>Drives the Tacx NEO FE-C trainer over Android.Bluetooth. Connect/retry orchestration lives in <c>TrainerService</c>.</summary>
public sealed class AndroidTrainerTransport : ITrainerTransport
{
    private readonly UUID FTMS_SERVICE = UUID.FromString("00001826-0000-1000-8000-00805f9b34fb");
    private readonly UUID CONTROL_POINT = UUID.FromString("00002ad9-0000-1000-8000-00805f9b34fb");
    private readonly UUID TACX_SERVICE = UUID.FromString("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
    private readonly UUID TACX_NOTIFY = UUID.FromString("6e40fec2-b5a3-f393-e0a9-e50e24dcca9e");
    private readonly UUID TACX_WRITE = UUID.FromString("6e40fec3-b5a3-f393-e0a9-e50e24dcca9e");
    private readonly UUID CLIENT_CHARACTERISTIC_CONFIGURATION = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");

    private readonly Context _context;
    private readonly Dictionary<string, BluetoothDevice> _discoveredDevices = new();

    private BluetoothGatt? _gatt;
    private BluetoothGattCharacteristic? _controlPoint;
    private BluetoothGattCharacteristic? _tacxNotify;
    private BluetoothGattCharacteristic? _tacxWrite;

    public event EventHandler<byte[]>? PacketReceived;
    public event EventHandler<ConnectionState>? ConnectionChanged;

    public AndroidTrainerTransport()
    {
        _context = Android.App.Application.Context;
    }

    public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(TimeSpan duration, CancellationToken ct)
    {
        try
        {
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
            var scanCallback = new BleScanCallback(discoveredDevices);
            scanner.StartScan(scanCallback);
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

            var bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
            var adapter = bluetoothManager?.Adapter;
            bluetoothDevice ??= adapter?.GetRemoteDevice(device.Address);

            if (bluetoothDevice == null)
            {
                return false;
            }

            _gatt?.Close();
            _controlPoint = null;
            _tacxNotify = null;
            _tacxWrite = null;

            ConnectionChanged?.Invoke(this, ConnectionState.Connecting);

            var connectionTaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _gatt = bluetoothDevice.ConnectGatt(
                _context,
                false,
                new GattCallback(this, connectionTaskSource),
                BluetoothTransports.Le);

            bool connected;
            try
            {
                connected = await connectionTaskSource.Task.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                connected = false;
            }

            if (!connected)
            {
                _gatt?.Close();
            }

            ConnectionChanged?.Invoke(this, connected ? ConnectionState.Connected : ConnectionState.Disconnected);
            return connected;
        }
        catch (Java.Lang.SecurityException)
        {
            ConnectionChanged?.Invoke(this, ConnectionState.Disconnected);
            return false;
        }
    }

    public void Disconnect()
    {
        _gatt?.Disconnect();
        _gatt?.Close();
        _gatt = null;
        _controlPoint = null;
        _tacxNotify = null;
        _tacxWrite = null;
        ConnectionChanged?.Invoke(this, ConnectionState.Disconnected);
    }

    public bool Write(byte[] packet)
    {
        if (_gatt == null || _tacxWrite == null)
        {
            return false;
        }

        _tacxWrite.WriteType = GattWriteType.NoResponse;
        _tacxWrite.SetValue(packet);
        var accepted = _gatt.WriteCharacteristic(_tacxWrite);
        Log.Debug("TacxRpmApp", $"FE-C basic resistance: {Convert.ToHexString(packet)}, accepted={accepted}");
        return accepted;
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

        Log.Debug("TacxRpmApp", $"FE-C notifications: local={notificationEnabled}, descriptor={descriptorWriteStarted}");
    }

    private sealed class GattCallback : BluetoothGattCallback
    {
        private readonly AndroidTrainerTransport _transport;
        private readonly TaskCompletionSource<bool> _tcs;

        public GattCallback(AndroidTrainerTransport transport, TaskCompletionSource<bool> tcs)
        {
            _transport = transport;
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
                _tcs.TrySetResult(false);
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            var service = gatt.GetService(_transport.FTMS_SERVICE);
            var controlPoint = service?.GetCharacteristic(_transport.CONTROL_POINT);
            var tacxService = gatt.GetService(_transport.TACX_SERVICE);
            var tacxNotify = tacxService?.GetCharacteristic(_transport.TACX_NOTIFY);
            var tacxWrite = tacxService?.GetCharacteristic(_transport.TACX_WRITE);
            controlPoint ??= tacxWrite;
            _transport._controlPoint = controlPoint;
            _transport._tacxNotify = tacxNotify;
            _transport._tacxWrite = tacxWrite;

            if (service == null)
            {
                if (tacxWrite != null)
                {
                    _transport.EnableTacxNotifications(gatt);
                    _tcs.TrySetResult(true);
                    return;
                }

                _tcs.TrySetResult(false);
                return;
            }

            if (controlPoint == null)
            {
                _tcs.TrySetResult(false);
                return;
            }

            if (tacxNotify != null)
            {
                _transport.EnableTacxNotifications(gatt);
            }

            _tcs.TrySetResult(true);
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            if (characteristic.Uuid?.Equals(_transport.TACX_NOTIFY) == true)
            {
                var packet = characteristic.GetValue() ?? Array.Empty<byte>();
                Log.Debug("TacxRpmApp", $"FE-C notification: {Convert.ToHexString(packet)}");
                _transport.PacketReceived?.Invoke(_transport, packet);
            }
        }
    }
}
