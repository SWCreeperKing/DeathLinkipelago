using System.Collections.Generic;

namespace DeathLinkipelago.Scripts;

public class SettingsData
{
    public Dictionary<string, bool> BoolDictionary = new();
    public Dictionary<string, float> FloatDictionary = new();

    private T GetData<T>(Dictionary<string, T> dict, string key, T def = default)
    {
        if (dict.TryGetValue(key, out var val)) return val;
        return dict[key] = def;
    }

    public bool GetBool(string key, bool def) => GetData(BoolDictionary, key, def);
    public void SetBool(string key, bool val) => BoolDictionary[key] = val;
    public float GetFloat(string key, float def) => GetData(FloatDictionary, key, def);
    public void SetFloat(string key, float val) => FloatDictionary[key] = val;
}