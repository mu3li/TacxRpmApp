using TacxRpmApp.Core.Ble;
using TacxRpmApp.Core.Protocol;

namespace TacxRpmApp.Core.Services;

/// <summary>
/// Orchestrates trainer connection retries and FE-C resistance commands over an
/// <see cref="ITrainerTransport"/>. Resilience: one connection retry after 700 ms,
/// 12-second timeout per attempt, up to 2 attempts.
/// </summary>
public sealed class TrainerService
{
    private static readonly TimeSpan ScanDuration = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(700);
    private const int MaxConnectAttempts = 2;

    private readonly ITrainerTransport _transport;

    public string LastConnectionStatus { get; private set; } = "";
    public string LastFeCNotification { get; private set; } = "";
    public string LastCommandStatus { get; private set; } = "";
    public ushort? MaximumResistance { get; private set; }

    public TrainerService(ITrainerTransport transport)
    {
        _transport = transport;
        _transport.PacketReceived += OnPacketReceived;
    }

    public Task<IReadOnlyList<BleDeviceInfo>> ScanDevicesAsync(CancellationToken ct = default)
        => _transport.ScanAsync(ScanDuration, ct);

    public async Task<bool> ConnectAsync(BleDeviceInfo device, CancellationToken ct = default)
    {
        LastConnectionStatus = "A iniciar ligação...";

        for (var attempt = 1; attempt <= MaxConnectAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(ConnectTimeout);

            bool connected;
            try
            {
                connected = await _transport.ConnectAsync(device, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                connected = false;
            }

            if (connected)
            {
                LastConnectionStatus = "Ligado.";
                return true;
            }

            if (attempt < MaxConnectAttempts)
            {
                LastConnectionStatus = "A repetir a ligação...";
                await Task.Delay(RetryDelay, ct);
            }
        }

        LastConnectionStatus = "Ligação Bluetooth falhou.";
        return false;
    }

    public Task<bool> SetResistanceAsync(int resistanceValue)
    {
        var packet = FeCPacket.BasicResistance(resistanceValue);
        var accepted = _transport.Write(packet);
        LastConnectionStatus = accepted
            ? $"Comando FE-C enviado: resistência {packet[11]}/200."
            : "Falha ao enviar comando FE-C.";
        return Task.FromResult(accepted);
    }

    private void OnPacketReceived(object? sender, byte[] packet)
    {
        LastFeCNotification = Convert.ToHexString(packet);
        var result = FeCPageParser.Parse(packet);

        switch (result.Page)
        {
            case FeCPage.Capabilities:
                MaximumResistance = result.MaximumResistance;
                break;
            case FeCPage.CommandStatus:
                LastCommandStatus = FormatCommandStatus(result.CommandStatus, result.CommandStatusByte);
                LastConnectionStatus = $"Estado comando FE-C: {LastCommandStatus}.";
                break;
        }
    }

    private static string FormatCommandStatus(CommandStatus status, byte raw) => status switch
    {
        CommandStatus.Success => "success",
        CommandStatus.Fail => "fail",
        CommandStatus.NotSupported => "not supported",
        CommandStatus.Rejected => "rejected",
        CommandStatus.Uninitialized => "uninitialized",
        _ => $"unknown (0x{raw:X2})"
    };
}
