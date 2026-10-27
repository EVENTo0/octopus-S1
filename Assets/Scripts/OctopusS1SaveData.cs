using System;
using UnityEngine;

/// <summary>
/// Local settings and progress, stored as JSON in PlayerPrefs under <see cref="OctopusS1SaveStore.Key"/>.
/// Schema 1: language ("" follows the device, "ar" or "en"), soundOn, volume [0..1],
/// missionsCompleted (>= 0), bestTimeSeconds (0 = no record).
/// Each Android application ID (Dev / Live) has its own PlayerPrefs, so channel data never mixes.
/// </summary>
[Serializable]
public sealed class OctopusS1SaveData
{
    public const int CurrentSchema = 1;

    public int schema = CurrentSchema;
    public string language = "";
    public bool soundOn = true;
    public float volume = 0.8f;
    public int missionsCompleted;
    public float bestTimeSeconds;

    /// <summary>Parses saved JSON. Missing data gives defaults; corrupt or unknown data is replaced or repaired.</summary>
    public static OctopusS1SaveData Parse(string json, out bool recovered)
    {
        recovered = false;
        if (string.IsNullOrEmpty(json)) return new OctopusS1SaveData();
        OctopusS1SaveData data;
        try
        {
            data = JsonUtility.FromJson<OctopusS1SaveData>(json);
        }
        catch (ArgumentException)
        {
            data = null;
        }
        if (data == null || data.schema < 1 || data.schema > CurrentSchema)
        {
            recovered = true;
            return new OctopusS1SaveData();
        }
        recovered = data.Sanitize();
        return data;
    }

    private bool Sanitize()
    {
        var changed = false;
        if (language != "" && language != "ar" && language != "en") { language = ""; changed = true; }
        if (float.IsNaN(volume) || volume < 0f || volume > 1f) { volume = float.IsNaN(volume) ? 0.8f : Mathf.Clamp01(volume); changed = true; }
        if (missionsCompleted < 0) { missionsCompleted = 0; changed = true; }
        if (float.IsNaN(bestTimeSeconds) || float.IsInfinity(bestTimeSeconds) || bestTimeSeconds < 0f) { bestTimeSeconds = 0f; changed = true; }
        return changed;
    }

    public string ToJson() => JsonUtility.ToJson(this);
}

public static class OctopusS1SaveStore
{
    public const string Key = "octopus_s1.save";

    public static OctopusS1SaveData Load()
    {
        bool recovered;
        var data = OctopusS1SaveData.Parse(PlayerPrefs.GetString(Key, ""), out recovered);
        if (recovered)
        {
            Debug.LogWarning("Octopus S1: saved settings were invalid and have been reset or repaired.");
            Save(data);
        }
        return data;
    }

    public static void Save(OctopusS1SaveData data)
    {
        PlayerPrefs.SetString(Key, data.ToJson());
        PlayerPrefs.Save();
    }
}
