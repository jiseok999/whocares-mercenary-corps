using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조합 특수 공격 발동 시 기준 유닛 위치에 잠깐 뜨는 심플 안내.
/// </summary>
public static class TraitProcToast
{
    const float FadeIn = 0.1f;
    const float Hold = 1.05f;
    const float FadeOut = 0.24f;
    const float SlideOffset = 18f;
    const int SortOrder = 9600;
    static readonly Vector2 WorldOffset = new Vector2(0f, 48f);

    static RectTransform panelRect;
    static CanvasGroup group;
    static Text labelText;
    static Image backdrop;
    static Image accentBar;
    static Image iconImage;
    static Canvas rootCanvas;
    static Canvas panelSortCanvas;
    static MonoBehaviour host;
    static Coroutine activeRoutine;
    static string lastTraitName;
    static Character lastAnchor;

    public static void Show(MonoBehaviour runner, string traitName, string skillName, Character anchorUnit)
    {
        if (runner == null || string.IsNullOrEmpty(traitName)) return;
        EnsureUi(runner);
        host = runner;
        lastTraitName = traitName;
        lastAnchor = anchorUnit;

        string line = string.IsNullOrEmpty(skillName)
            ? GameLocalization.GetTraitDisplayName(traitName)
            : skillName;
        labelText.text = line;
        ApplyTraitTheme(traitName);
        ResizePanel();
        PositionPanel();

        panelRect.gameObject.SetActive(true);
        panelRect.SetAsLastSibling();
        if (panelSortCanvas != null)
        {
            panelSortCanvas.sortingOrder = SortOrder;
        }

        if (activeRoutine != null && host != null)
        {
            host.StopCoroutine(activeRoutine);
        }
        activeRoutine = host.StartCoroutine(Animate());
    }

    static void ApplyTraitTheme(string traitName)
    {
        Color traitColor = Color.white;
        if (GameManager.Instance != null)
        {
            traitColor = GameManager.Instance.GetTraitDisplayColor(traitName);
        }

        if (accentBar != null)
        {
            accentBar.color = traitColor;
        }

        if (iconImage != null)
        {
            iconImage.sprite = TraitIconFactory.Get(traitName);
            iconImage.color = Color.white;
            iconImage.gameObject.SetActive(iconImage.sprite != null);
        }
    }

    static void ResizePanel()
    {
        if (panelRect == null || labelText == null) return;

        float textWidth = labelText.preferredWidth;
        float iconWidth = iconImage != null && iconImage.gameObject.activeSelf ? 28f : 0f;
        float width = Mathf.Clamp(textWidth + iconWidth + 34f, 128f, 210f);
        panelRect.sizeDelta = new Vector2(width, 36f);
    }

    static void PositionPanel()
    {
        if (panelRect == null || rootCanvas == null) return;

        if (panelRect.parent != rootCanvas.transform)
        {
            panelRect.SetParent(rootCanvas.transform, false);
        }

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0f);

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector3 worldAnchor = ResolveWorldAnchor();
        Camera worldCam = Camera.main;
        if (worldCam == null) return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCam, worldAnchor);
        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCam, out Vector2 localPoint))
        {
            return;
        }

        panelRect.anchoredPosition = localPoint + WorldOffset;
    }

    static Vector3 ResolveWorldAnchor()
    {
        if (lastAnchor != null)
        {
            return lastAnchor.transform.position + Vector3.up * 0.55f;
        }

        if (TraitManager.Instance != null
            && !string.IsNullOrEmpty(lastTraitName)
            && TraitManager.Instance.TryGetTraitProcToastAnchor(lastTraitName, out Vector2 screenPoint))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, cam.nearClipPlane + 1f));
                world.z = 0f;
                return world;
            }
        }

        Camera fallbackCam = Camera.main;
        if (fallbackCam != null)
        {
            Vector3 world = fallbackCam.ScreenToWorldPoint(new Vector3(Screen.width * 0.35f, Screen.height * 0.55f, fallbackCam.nearClipPlane + 1f));
            world.z = 0f;
            return world;
        }

        return Vector3.zero;
    }

    static void EnsureUi(MonoBehaviour runner)
    {
        if (panelRect != null && rootCanvas != null && group != null) return;

        panelRect = null;
        rootCanvas = null;
        group = null;
        panelSortCanvas = null;

        rootCanvas = TraitManager.GetOverlayCanvas();
        if (rootCanvas == null)
        {
            GameObject root = new GameObject("TraitProcToastCanvas");
            rootCanvas = root.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = SortOrder;
            MobileUIScaling.Configure(root.AddComponent<CanvasScaler>());
            root.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("TraitProcToastPanel");
        panel.transform.SetParent(rootCanvas.transform, false);
        panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(180f, 36f);

        panelSortCanvas = panel.AddComponent<Canvas>();
        panelSortCanvas.overrideSorting = true;
        panelSortCanvas.sortingOrder = SortOrder;

        group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject bgObj = new GameObject("Backdrop");
        bgObj.transform.SetParent(panel.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        backdrop = bgObj.AddComponent<Image>();
        backdrop.sprite = TooltipTheme.GetStandardBackgroundSprite();
        backdrop.color = new Color(0.08f, 0.1f, 0.12f, 0.92f);
        backdrop.type = Image.Type.Simple;
        backdrop.raycastTarget = false;

        GameObject accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(panel.transform, false);
        RectTransform accentRect = accentObj.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(4f, 0f);
        accentRect.offsetMin = new Vector2(0f, 4f);
        accentRect.offsetMax = new Vector2(4f, -4f);
        accentBar = accentObj.AddComponent<Image>();
        accentBar.sprite = TooltipTheme.GetStandardBackgroundSprite();
        accentBar.color = Color.white;
        accentBar.raycastTarget = false;

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(panel.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(22f, 22f);
        iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(36f, 2f);
        textRect.offsetMax = new Vector2(-10f, -2f);

        labelText = textObj.AddComponent<Text>();
        labelText.font = UIFontProvider.Get();
        labelText.fontSize = 15;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = new Color(0.96f, 0.94f, 0.88f, 1f);
        labelText.raycastTarget = false;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    static IEnumerator Animate()
    {
        if (panelRect == null || group == null) yield break;

        Vector2 targetPos = panelRect.anchoredPosition;
        panelRect.anchoredPosition = targetPos + new Vector2(0f, SlideOffset);

        float t = 0f;
        while (t < FadeIn)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / FadeIn);
            group.alpha = p;
            panelRect.anchoredPosition = Vector2.Lerp(targetPos + new Vector2(0f, SlideOffset), targetPos, p);
            yield return null;
        }
        group.alpha = 1f;
        panelRect.anchoredPosition = targetPos;

        yield return new WaitForSecondsRealtime(Hold);

        PositionPanel();
        targetPos = panelRect.anchoredPosition;

        t = 0f;
        while (t < FadeOut)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / FadeOut);
            group.alpha = 1f - p;
            panelRect.anchoredPosition = targetPos + new Vector2(0f, SlideOffset * p);
            yield return null;
        }
        group.alpha = 0f;
        activeRoutine = null;
    }
}
