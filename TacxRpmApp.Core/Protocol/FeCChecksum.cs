namespace TacxRpmApp.Core.Protocol;

public static class FeCChecksum
{
    public static byte Of(ReadOnlySpan<byte> packet)
    {
        byte checksum = 0;
        foreach (var value in packet)
        {
            checksum ^= value;
        }

        return checksum;
    }
}
