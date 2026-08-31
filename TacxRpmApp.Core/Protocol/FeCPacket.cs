namespace TacxRpmApp.Core.Protocol;

/// <summary>Builds ANT+ FE-C (message ID 0x4E, XOR checksum) packets over Bluetooth Smart.</summary>
public static class FeCPacket
{
    public static byte[] BasicResistance(int resistancePercentValue)
    {
        var clamped = (byte)Math.Clamp(resistancePercentValue, 0, 200);
        var packet = new byte[]
        {
            0xA4, 0x09, 0x4E, 0x05, 0x30,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            clamped, 0x00
        };
        packet[^1] = FeCChecksum.Of(packet.AsSpan(0, packet.Length - 1));
        return packet;
    }
}
