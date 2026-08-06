using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 언어별 UI 폰트 제공. 한국어는 픽셀 폰트, 그 외는 OS 폰트(중·일·영).
/// </summary>
public static class UIFontProvider
{
    const string PixelFontResourcePath = "neodgm";
    static Font cachedFont;
    static GameLanguage cachedLanguage = (GameLanguage)(-1);

    public static void InvalidateCache()
    {
        cachedFont = null;
        cachedLanguage = (GameLanguage)(-1);
    }

    public static Font Get()
    {
        GameLanguage language = GameLocalization.CurrentLanguage;
        if (cachedFont != null && cachedLanguage == language)
        {
            return cachedFont;
        }

        cachedLanguage = language;
        switch (language)
        {
            case GameLanguage.English:
                cachedFont = CreateOsFont("Segoe UI", "Arial");
                break;
            case GameLanguage.Japanese:
                cachedFont = CreateOsFont("Yu Gothic UI", "Meiryo UI", "MS Gothic", "Arial");
                break;
            case GameLanguage.TraditionalChinese:
                cachedFont = CreateOsFont("Microsoft JhengHei UI", "PMingLiU", "Arial");
                break;
            case GameLanguage.SimplifiedChinese:
                cachedFont = CreateOsFont("Microsoft YaHei UI", "SimHei", "Arial");
                break;
            case GameLanguage.Thai:
                cachedFont = CreateOsFont("Leelawadee UI", "Tahoma", "Arial");
                break;
            case GameLanguage.Spanish:
            case GameLanguage.Portuguese:
            case GameLanguage.French:
            case GameLanguage.Italian:
            case GameLanguage.German:
                cachedFont = CreateOsFont("Segoe UI", "Arial", "Helvetica");
                break;
            default:
                cachedFont = Resources.Load<Font>(PixelFontResourcePath);
                if (cachedFont == null)
                {
                    cachedFont = CreateOsFont("Malgun Gothic", "Arial");
                }
                break;
        }

        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return cachedFont;
    }

    static Font CreateOsFont(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Font font = Font.CreateDynamicFontFromOSFont(names[i], 16);
            if (font != null)
            {
                return font;
            }
        }

        return null;
    }

    public static void ApplyToAllText()
    {
        Font font = Get();
        Text[] texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
        foreach (Text text in texts)
        {
            ApplyFont(text, font);
        }
    }

    /// <summary>폰트 교체 후 Text 메시를 다시 빌드합니다 (언어 전환 시 빈 글리프 방지).</summary>
    public static void ApplyFont(Text text, Font font = null)
    {
        if (text == null) return;
        font ??= Get();
        if (font == null) return;

        text.font = font;
        string content = text.text;
        if (!string.IsNullOrEmpty(content))
        {
            text.text = string.Empty;
            text.text = content;
        }

        text.SetAllDirty();
    }
}
