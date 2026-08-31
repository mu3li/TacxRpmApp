using TacxRpmApp.Core.Ble;

namespace TacxRpmApp.Tests.Fakes;

public sealed class FakeTrainerTransport : ITrainerTransport
{
    private readonly Queue<bool> _connectResults = new();

    public List<byte[]> WrittenPackets { get; } = new();
    public int ConnectAttempts { get; private set; }
    public bool WriteResult { get; set; } = true;
    public IReadOnlyList<BleDeviceInfo> ScanResult { get; set; } = Array.Empty<BleDeviceInfo>();

    public event EventHandler<byte[]>? PacketReceived;
    public event EventHandler<ConnectionState>? ConnectionChanged;

    public void EnqueueConnectResult(bool result) => _connectResults.Enqueue(result);

    public Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(TimeSpan duration, CancellationToken ct)
        => Task.FromResult(ScanResult);

    public Task<bool> ConnectAsync(BleDeviceInfo device, CancellationToken ct)
    {
        ConnectAttempts++;
        var result = _connectResults.Count > 0 && _connectResults.Dequeue();
        ConnectionChanged?.Invoke(this, result ? ConnectionState.Connected : ConnectionState.Disconnected);
        return Task.FromResult(result);
    }

    public void Disconnect() => ConnectionChanged?.Invoke(this, ConnectionState.Disconnected);

    public bool Write(byte[] packet)
    {
        WrittenPackets.Add(packet);
        return WriteResult;
    }

    public void RaisePacket(byte[] packet) => PacketReceived?.Invoke(this, packet);
}
