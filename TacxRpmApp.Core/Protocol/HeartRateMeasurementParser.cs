namespace TacxRpmApp.Core.Protocol;

/// <summary>Parses Bluetooth SIG Heart Rate Measurement (0x2A37) notifications.</summary>
public static class HeartRateMeasurementParser
{
    public static int? Parse(byte[] packet)
    {
        if (packet.Length < 2)
        {
            return null;
        }

        var flags = packet[0];
        var is16Bit = (flags & 0x01) != 0;

        int heartRate;
        if (is16Bit)
        {
            if (packet.Length < 3)
            {
                return null;
            }

            heartRate = packet[1] | (packet[2] << 8);
        }
        else
        {
            heartRate = packet[1];
        }

        // Sanity check. Avoid exposing obviously invalid values.
        if (heartRate <= 0 || heartRate > 300)
        {
            return null;
        }

        return heartRate;
    }
}
