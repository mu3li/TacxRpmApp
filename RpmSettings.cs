using Microsoft.Maui.Storage;

namespace TacxRpmApp;

public sealed class RpmSettings
{
    public int Uphill
    {
        get => Preferences.Get(nameof(Uphill), 45);
        set => Preferences.Set(nameof(Uphill), Math.Max(0, value));
    }

    public int Cruise
    {
        get => Preferences.Get(nameof(Cruise), 20);
        set => Preferences.Set(nameof(Cruise), Math.Max(0, value));
    }

    public int Downhill
    {
        get => Preferences.Get(nameof(Downhill), 10);
        set => Preferences.Set(nameof(Downhill), Math.Max(0, value));
    }

    public int Increment
    {
        get => Preferences.Get(nameof(Increment), 2);
        set => Preferences.Set(nameof(Increment), Math.Max(1, value));
    }
}
