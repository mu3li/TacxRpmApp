namespace TacxRpmApp.Core.Protocol;

public enum FeCPage
{
    Unknown,
    GeneralData,
    SpecificTrainerData,
    Capabilities,
    CommandStatus,
    ManufacturerId
}

public readonly record struct FeCParseResult(
    FeCPage Page,
    CommandStatus CommandStatus = CommandStatus.Unknown,
    ushort? MaximumResistance = null,
    byte CommandStatusByte = 0,
    byte CommandId = 0);

/// <summary>Parses ANT+ FE-C (message ID 0x4E) notification pages received from the trainer.</summary>
public static class FeCPageParser
{
    public static FeCParseResult Parse(byte[] packet)
    {
        if (packet.Length < 13 || packet[0] != 0xA4 || packet[2] != 0x4E)
        {
            return new FeCParseResult(FeCPage.Unknown);
        }

        return packet[4] switch
        {
            0x10 => new FeCParseResult(FeCPage.GeneralData),
            0x19 => new FeCParseResult(FeCPage.SpecificTrainerData),
            0x36 => new FeCParseResult(
                FeCPage.Capabilities,
                MaximumResistance: (ushort)(packet[9] | (packet[10] << 8))),
            0x47 => new FeCParseResult(
                FeCPage.CommandStatus,
                CommandStatus: ParseCommandStatus(packet[7]),
                CommandStatusByte: packet[7],
                CommandId: packet[5]),
            0x50 => new FeCParseResult(FeCPage.ManufacturerId),
            _ => new FeCParseResult(FeCPage.Unknown)
        };
    }

    private static CommandStatus ParseCommandStatus(byte value) => value switch
    {
        0x00 => CommandStatus.Success,
        0x01 => CommandStatus.Fail,
        0x02 => CommandStatus.NotSupported,
        0x03 => CommandStatus.Rejected,
        0xFF => CommandStatus.Uninitialized,
        _ => CommandStatus.Unknown
    };
}
