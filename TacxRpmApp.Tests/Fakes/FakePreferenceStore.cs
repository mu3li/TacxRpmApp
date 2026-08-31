using TacxRpmApp.Core.Settings;

namespace TacxRpmApp.Tests.Fakes;

public sealed class FakePreferenceStore : IPreferenceStore
{
    private readonly Dictionary<string, int> _values = new();

    public int GetInt(string key, int defaultValue)
        => _values.TryGetValue(key, out var value) ? value : defaultValue;

    public void SetInt(string key, int value) => _values[key] = value;
}
