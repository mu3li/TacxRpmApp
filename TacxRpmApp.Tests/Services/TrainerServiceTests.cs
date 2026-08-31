using TacxRpmApp.Core.Protocol;
using TacxRpmApp.Core.Services;
using TacxRpmApp.Tests.Fakes;

namespace TacxRpmApp.Tests.Services;

public class TrainerServiceTests
{
    [Fact]
    public async Task ConnectAsync_BothAttemptsFail_RetriesOnceThenFails()
    {
        var transport = new FakeTrainerTransport();
        var service = new TrainerService(transport);

        var connected = await service.ConnectAsync(new TacxRpmApp.Core.Ble.BleDeviceInfo("Neo", "AA:BB"));

        Assert.False(connected);
        Assert.Equal(2, transport.ConnectAttempts);
    }

    [Fact]
    public async Task ConnectAsync_SecondAttemptSucceeds_ReturnsTrue()
    {
        var transport = new FakeTrainerTransport();
        transport.EnqueueConnectResult(false);
        transport.EnqueueConnectResult(true);
        var service = new TrainerService(transport);

        var connected = await service.ConnectAsync(new TacxRpmApp.Core.Ble.BleDeviceInfo("Neo", "AA:BB"));

        Assert.True(connected);
        Assert.Equal(2, transport.ConnectAttempts);
    }

    [Fact]
    public async Task SetResistanceAsync_WritesExactBasicResistanceBytes()
    {
        var transport = new FakeTrainerTransport();
        var service = new TrainerService(transport);

        await service.SetResistanceAsync(45);

        var written = Assert.Single(transport.WrittenPackets);
        Assert.Equal(FeCPacket.BasicResistance(45), written);
    }

    [Fact]
    public void CommandStatusNotification_UpdatesLastCommandStatus()
    {
        var transport = new FakeTrainerTransport();
        var service = new TrainerService(transport);

        transport.RaisePacket(Convert.FromHexString("A4094E0547FFFF00010000207F"));

        Assert.Equal("success", service.LastCommandStatus);
    }
}
