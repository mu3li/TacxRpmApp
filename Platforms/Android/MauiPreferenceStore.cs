using Microsoft.Maui.Storage;
using TacxRpmApp.Core.Settings;

namespace TacxRpmApp;

public sealed class MauiPreferenceStore : IPreferenceStore
{
    public int GetInt(string key, int defaultValue) => Preferences.Get(key, defaultValue);

    public void SetInt(string key, int value) => Preferences.Set(key, value);
}
