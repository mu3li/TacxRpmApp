namespace TacxRpmApp.Core.Ble;

public interface ITrainerTransport
{
    Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(TimeSpan duration, CancellationToken ct);
    Task<bool> ConnectAsync(BleDeviceInfo device, CancellationToken ct);
    void Disconnect();
    bool Write(byte[] packet);
    event EventHandler<byte[]>? PacketReceived;
    event EventHandler<ConnectionState>? ConnectionChanged;
}
