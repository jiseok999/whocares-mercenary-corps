using System;
using System.Collections.Generic;

/// <summary>
/// 언어 변경 시 등록된 UI 갱신 핸들러를 일괄 호출합니다.
/// </summary>
public static class GameLocalizationCoordinator
{
    static readonly List<Action> Handlers = new List<Action>();
    static bool subscribed;

    public static void EnsureSubscribed()
    {
        if (subscribed) return;
        subscribed = true;
        GameLocalization.LanguageChanged += RefreshAll;
    }

    public static void Register(Action handler)
    {
        EnsureSubscribed();
        if (handler == null || Handlers.Contains(handler)) return;
        Handlers.Add(handler);
    }

    public static void Unregister(Action handler)
    {
        if (handler == null) return;
        Handlers.Remove(handler);
    }

    public static void RefreshAll()
    {
        UIFontProvider.InvalidateCache();
        UIFontProvider.ApplyToAllText();
        for (int i = Handlers.Count - 1; i >= 0; i--)
        {
            try
            {
                Handlers[i]?.Invoke();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"GameLocalization refresh failed: {ex.Message}");
            }
        }
    }
}
