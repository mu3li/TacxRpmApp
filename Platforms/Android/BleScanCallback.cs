using Android.Bluetooth;
using Android.Bluetooth.LE;
using TacxRpmApp.Core.Ble;

namespace TacxRpmApp;

/// <summary>Collects named BLE advertisements into <see cref="BleDeviceInfo"/> entries, shared by both transports.</summary>
internal sealed class BleScanCallback : ScanCallback
{
    private readonly Dictionary<string, BleDeviceInfo> _devices = new();
    private readonly Dictionary<string, BluetoothDevice> _bluetoothDevices;

    public IReadOnlyList<BleDeviceInfo> Devices => _devices.Values.ToList();

    public BleScanCallback(Dictionary<string, BluetoothDevice> bluetoothDevices)
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
