namespace TacxRpmApp.Core.Settings;

public sealed class RpmSettings
{
    private readonly IPreferenceStore _store;

    public RpmSettings(IPreferenceStore store)
    {
        _store = store;
    }

    public int Uphill
    {
        get => _store.GetInt(nameof(Uphill), 45);
        set => _store.SetInt(nameof(Uphill), Math.Clamp(value, 0, 200));
    }

    public int Cruise
    {
        get => _store.GetInt(nameof(Cruise), 20);
        set => _store.SetInt(nameof(Cruise), Math.Clamp(value, 0, 200));
    }

    public int Downhill
    {
        get => _store.GetInt(nameof(Downhill), 10);
        set => _store.SetInt(nameof(Downhill), Math.Clamp(value, 0, 200));
    }

    public int Increment
    {
        get => _store.GetInt(nameof(Increment), 2);
        set => _store.SetInt(nameof(Increment), Math.Max(1, value));
    }
}
