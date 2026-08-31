namespace TacxRpmApp.Core.Settings;

public interface IPreferenceStore
{
    int GetInt(string key, int defaultValue);
    void SetInt(string key, int value);
}
