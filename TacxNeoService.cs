using Android.Bluetooth;
using Android.Content;
using Java.Util;

namespace TacxRpmApp;

public class TacxNeoService
{
    private readonly UUID FTMS_SERVICE = UUID.FromString("00001826-0000-1000-8000-00805f9b34fb");
    private readonly UUID CONTROL_POINT = UUID.FromString("00002ad9-0000-1000-8000-00805f9b34fb");

    private BluetoothGatt? _gatt;
    private BluetoothGattCharacteristic? _controlPoint;

    private readonly Context _context;

    public TacxNeoService()
    {
        _context = Android.App.Application.Context;
    }

    public Task<bool> ConnectAsync()
    {
        var tcs = new TaskCompletionSource<bool>();

        var adapter = BluetoothAdapter.DefaultAdapter;
        var device = adapter.BondedDevices.FirstOrDefault(d => d.Name.Contains("Tacx"));

        if (device == null)
        {
            tcs.SetResult(false);
            return tcs.Task;
        }

        _gatt = device.ConnectGatt(_context, false, new GattCallback(this, tcs));
        return tcs.Task;
    }

    public Task SetResistanceAsync(ushort newtonValue)
    {
        if (_gatt == null || _controlPoint == null)
            return Task.CompletedTask;

        byte opcode = 0x04; // Set Target Resistance
        byte low = (byte)(newtonValue & 0xFF);
        byte high = (byte)(newtonValue >> 8);

        var data = new byte[] { opcode, low, high };
        _controlPoint.SetValue(data);
        _gatt.WriteCharacteristic(_controlPoint);

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
                _tcs.TrySetResult(false);
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            var service = gatt.GetService(_service.FTMS_SERVICE);
            _service._controlPoint = service.GetCharacteristic(_service.CONTROL_POINT);
            _tcs.TrySetResult(true);
        }
    }
}
