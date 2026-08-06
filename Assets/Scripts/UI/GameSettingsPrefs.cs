using UnityEngine;

/// <summary>
/// 게임 설정(PlayerPrefs) 로드/저장/적용.
/// </summary>
public static class GameSettingsPrefs
{
    public const string MasterVolumePrefKey = "game_master_volume";
    public const string BgmVolumePrefKey = "game_bgm_volume";
    public const string FullscreenPrefKey = "game_fullscreen";
    public const string PartyToastPrefKey = "game_party_toast";
    public const string LanguagePrefKey = "game_language";

    public const int DefaultWidth = 1920;
    public const int DefaultHeight = 1080;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyDisplayOnStartup()
    {
        ApplyDisplaySettings();
    }

    public static GameLanguage LoadLanguage()
    {
        int raw = PlayerPrefs.GetInt(LanguagePrefKey, (int)GameLanguage.Korean);
        if (raw < 0 || raw > (int)GameLanguage.German) raw = (int)GameLanguage.Korean;
        return (GameLanguage)raw;
    }

    public static void SaveLanguage(GameLanguage language)
    {
        PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
        PlayerPrefs.Save();
    }

    public static float LoadMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumePrefKey, AudioListener.volume);
    }

    public static float LoadBgmVolume()
    {
        return PlayerPrefs.GetFloat(BgmVolumePrefKey, 1f);
    }

    public static bool LoadFullscreenSetting()
    {
        return PlayerPrefs.GetInt(FullscreenPrefKey, 0) == 1;
    }

    public static bool LoadPartyToastSetting()
    {
        return PlayerPrefs.GetInt(PartyToastPrefKey, 1) == 1;
    }

    public static void ApplyMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    public static void SaveMasterVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        ApplyMasterVolume(clamped);
        PlayerPrefs.SetFloat(MasterVolumePrefKey, clamped);
        PlayerPrefs.Save();
    }

    public static void SaveBgmVolume(float value, System.Action<float> applyBgmVolume = null)
    {
        float clamped = Mathf.Clamp01(value);
        applyBgmVolume?.Invoke(clamped);
        PlayerPrefs.SetFloat(BgmVolumePrefKey, clamped);
        PlayerPrefs.Save();
    }

    public static void SaveFullscreen(bool isFullscreen)
    {
        ApplyDisplaySettings(isFullscreen);
        PlayerPrefs.SetInt(FullscreenPrefKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SavePartyToast(bool enabled, System.Action onDisabled = null)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.showPartyToast = enabled;
            if (!enabled)
            {
                onDisabled?.Invoke();
            }
        }

        PlayerPrefs.SetInt(PartyToastPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ApplySavedSettings(System.Action onPartyToastDisabled = null)
    {
        GameLocalization.Initialize();
        ApplyMasterVolume(LoadMasterVolume());
        ApplyDisplaySettings();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.showPartyToast = LoadPartyToastSetting();
        }
    }

    static void ApplyDisplaySettings()
    {
        ApplyDisplaySettings(LoadFullscreenSetting());
    }

    static void ApplyDisplaySettings(bool isFullscreen)
    {
        FullScreenMode mode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (Screen.width != DefaultWidth || Screen.height != DefaultHeight || Screen.fullScreenMode != mode)
        {
            Screen.SetResolution(DefaultWidth, DefaultHeight, mode);
        }
    }
}
