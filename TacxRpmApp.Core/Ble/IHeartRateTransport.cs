namespace TacxRpmApp.Core.Ble;

public interface IHeartRateTransport
{
    Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(TimeSpan duration, CancellationToken ct);
    Task<bool> ConnectAsync(BleDeviceInfo device, CancellationToken ct);
    void Disconnect();
    event EventHandler<byte[]>? PacketReceived;
    event EventHandler<ConnectionState>? ConnectionChanged;
}
