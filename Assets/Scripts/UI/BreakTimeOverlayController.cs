using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 클리어 후 쉬는 시간 — 라운드 HUD 위치에 안내, 화면 중앙에 카운트다운.
/// </summary>
public class BreakTimeOverlayController : MonoBehaviour
{
    static BreakTimeOverlayController instance;

    RectTransform roundHudAnchor;
    GameObject timeTextObject;
    GameObject waveIconsObject;
    CanvasGroup panelGroup;
    CanvasGroup countdownGroup;
    RectTransform panelRect;
    Text titleText;
    Text subtitleText;
    Text countdownText;
    Text centerGuideText;
    RectTransform centerGroupRect;
    Button skipBreakButton;
    Text skipBreakButtonLabel;

    bool targetVisible;
    float panelAlpha;
    float pulseTimer;

    const float FadeSpeed = 4.5f;

    public static void Ensure(Canvas parentCanvas, RectTransform roundHud)
    {
        if (instance != null) return;

        GameObject host = new GameObject("BreakTimeOverlay");
        host.transform.SetParent(parentCanvas != null ? parentCanvas.transform : null, false);
        instance = host.AddComponent<BreakTimeOverlayController>();
        instance.Initialize(parentCanvas, roundHud);
    }

    void Initialize(Canvas parentCanvas, RectTransform roundHud)
    {
        roundHudAnchor = roundHud;
        if (roundHud != null)
        {
            Transform timeText = roundHud.Find("TimeText");
            if (timeText != null)
            {
                timeTextObject = timeText.gameObject;
            }

            Transform waveIcons = roundHud.Find("WaveIcons");
            if (waveIcons != null)
            {
                waveIconsObject = waveIcons.gameObject;
            }
        }

        CreateOverlayUi(parentCanvas);
        SetVisibleImmediate(false);
    }

    void CreateOverlayUi(Canvas parentCanvas)
    {
        Transform rootParent = parentCanvas != null ? parentCanvas.transform : transform;

        Color accent = new Color(1f, 0.78f, 0.32f, 1f);
        Color cream = new Color(0.93f, 0.86f, 0.74f, 1f);

        Transform hudPanelParent = roundHudAnchor != null ? roundHudAnchor : rootParent;
        GameObject panelObj = new GameObject("BreakTimePanel");
        panelObj.transform.SetParent(hudPanelParent, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.type = Image.Type.Sliced;
        panelImage.sprite = WarmRoundedSprite.Get(
            new Color(0.11f, 0.08f, 0.06f, 0.92f),
            28,
            new Color(1f, 0.82f, 0.45f, 0.24f),
            2f);
        panelGroup = panelObj.AddComponent<CanvasGroup>();
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;
        panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.04f, 0.04f);
        panelRect.anchorMax = new Vector2(0.96f, 0.78f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(panelObj.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.raycastTarget = false;
        accentImage.type = Image.Type.Sliced;
        accentImage.sprite = WarmRoundedSprite.Get(accent, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -8f);
        accentRect.sizeDelta = new Vector2(108f, 6f);

        titleText = CreateLabel(panelObj.transform, "Title", "대기 시간", 40, FontStyle.Bold, accent,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -22f), new Vector2(520f, 48f));

        subtitleText = CreateLabel(panelObj.transform, "Subtitle", "유닛 배치 · 상점 이용", 24, FontStyle.Normal, cream,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -66f), new Vector2(520f, 34f));

        GameObject centerGroupObj = new GameObject("BreakTimeCenterGroup");
        centerGroupObj.transform.SetParent(rootParent, false);
        centerGroupRect = centerGroupObj.AddComponent<RectTransform>();
        countdownGroup = centerGroupObj.AddComponent<CanvasGroup>();
        countdownGroup.blocksRaycasts = false;
        countdownGroup.interactable = false;
        centerGroupRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerGroupRect.pivot = new Vector2(0.5f, 0.5f);
        centerGroupRect.anchoredPosition = Vector2.zero;
        centerGroupRect.sizeDelta = new Vector2(640f, 320f);
        GameUiSortingLayers.ApplySorting(centerGroupObj, GameUiSortingLayers.GuideOverlay, graphicRaycaster: true);

        GameObject countdownObj = new GameObject("BreakTimeCenterCountdown");
        countdownObj.transform.SetParent(centerGroupObj.transform, false);
        countdownText = countdownObj.AddComponent<Text>();
        countdownText.text = "60";
        countdownText.font = UIFontProvider.Get();
        countdownText.fontSize = 112;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.color = new Color(1f, 0.95f, 0.82f, 0.96f);
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.raycastTarget = false;

        Outline countdownOutline = countdownObj.AddComponent<Outline>();
        countdownOutline.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.75f);
        countdownOutline.effectDistance = new Vector2(3f, -3f);

        RectTransform countdownRect = countdownObj.GetComponent<RectTransform>();
        countdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        countdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        countdownRect.pivot = new Vector2(0.5f, 0.5f);
        countdownRect.anchoredPosition = new Vector2(0f, 52f);
        countdownRect.sizeDelta = new Vector2(300f, 140f);

        centerGuideText = CreateLabel(centerGroupObj.transform, "CenterGuide",
            "상점에서 유닛을 구매하고\n보드에 배치하세요",
            30, FontStyle.Bold, cream,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -68f), new Vector2(580f, 88f));

        Outline guideOutline = centerGuideText.gameObject.AddComponent<Outline>();
        guideOutline.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.65f);
        guideOutline.effectDistance = new Vector2(2f, -2f);

        skipBreakButton = CreateSkipBreakButton(centerGroupObj.transform);

        panelObj.SetActive(false);
        centerGroupObj.SetActive(false);
    }

    Button CreateSkipBreakButton(Transform parent)
    {
        Color accent = new Color(1f, 0.78f, 0.32f, 1f);
        Color cream = new Color(0.93f, 0.86f, 0.74f, 1f);

        GameObject btnObj = new GameObject("SkipBreakButton");
        btnObj.transform.SetParent(parent, false);

        Image bg = btnObj.AddComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.sprite = WarmRoundedSprite.Get(
            new Color(0.22f, 0.14f, 0.08f, 0.98f),
            18,
            new Color(1f, 0.82f, 0.38f, 0.85f),
            2f);
        bg.raycastTarget = true;

        Button button = btnObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.08f, 0.95f, 1f);
        colors.pressedColor = new Color(0.82f, 0.78f, 0.68f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(OnSkipBreakClicked);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0f, -148f);
        btnRect.sizeDelta = new Vector2(280f, 52f);

        GameObject accentBar = new GameObject("Accent");
        accentBar.transform.SetParent(btnObj.transform, false);
        Image accentImage = accentBar.AddComponent<Image>();
        accentImage.raycastTarget = false;
        accentImage.type = Image.Type.Sliced;
        accentImage.sprite = WarmRoundedSprite.Get(accent, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentBar.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -4f);
        accentRect.sizeDelta = new Vector2(120f, 4f);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.text = "다음 라운드로  ▶";
        label.font = UIFontProvider.Get();
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.color = cream;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        skipBreakButtonLabel = label;

        return button;
    }

    void OnSkipBreakClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SkipRoundBreak();
        }
    }

    static Text CreateLabel(Transform parent, string name, string content, int fontSize, FontStyle style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        bool shouldShow = gm != null && gm.IsWaitingForRoundTransition;

        if (shouldShow != targetVisible)
        {
            targetVisible = shouldShow;
            if (timeTextObject != null)
            {
                timeTextObject.SetActive(!shouldShow);
            }

            if (waveIconsObject != null)
            {
                waveIconsObject.SetActive(!shouldShow);
            }

            if (shouldShow)
            {
                if (panelRect != null)
                {
                    panelRect.SetAsLastSibling();
                }

                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.EnsureShopVisible();
                }
            }
        }

        float targetAlpha = targetVisible ? 1f : 0f;
        panelAlpha = Mathf.MoveTowards(panelAlpha, targetAlpha, FadeSpeed * Time.deltaTime);

        if (panelGroup != null)
        {
            panelGroup.alpha = panelAlpha;
        }

        if (countdownGroup != null)
        {
            countdownGroup.alpha = panelAlpha;
        }

        bool showOverlay = panelAlpha > 0.01f;
        bool overlayInteractive = panelAlpha > 0.95f;

        if (countdownGroup != null)
        {
            countdownGroup.blocksRaycasts = overlayInteractive;
            countdownGroup.interactable = overlayInteractive;
        }

        if (skipBreakButton != null)
        {
            skipBreakButton.interactable = overlayInteractive && shouldShow;
        }

        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(showOverlay);
        }

        if (centerGroupRect != null)
        {
            centerGroupRect.gameObject.SetActive(showOverlay);
        }

        if (showOverlay && ShopManager.Instance != null)
        {
            ShopManager.Instance.EnsureShopVisible();
        }

        if (!targetVisible && panelAlpha <= 0.01f)
        {
            return;
        }

        pulseTimer += Time.deltaTime * 2.2f;
        if (countdownText != null && targetVisible)
        {
            float scalePulse = 1f + Mathf.Sin(pulseTimer) * 0.04f;
            countdownText.transform.localScale = Vector3.one * scalePulse;
        }

        if (gm == null || !targetVisible) return;

        ApplyBreakOverlayCopy(gm);

        int seconds = Mathf.CeilToInt(gm.RoundBreakRemaining);

        if (countdownText != null)
        {
            countdownText.text = seconds.ToString();
        }
    }

    public static void RefreshCopyIfVisible()
    {
        if (instance == null || GameManager.Instance == null) return;
        ApplyBreakOverlayCopy(GameManager.Instance);
    }

    static void ApplyBreakOverlayCopy(GameManager gm)
    {
        if (instance == null || gm == null) return;

        Font font = UIFontProvider.Get();
        bool firstRoundPrep = gm.IsAwaitingFirstRoundDeployment;

        if (instance.titleText != null)
        {
            UIFontProvider.ApplyFont(instance.titleText, font);
            instance.titleText.text = firstRoundPrep ? GameLocalization.BreakRound1Prep : GameLocalization.HudBreakTime;
        }

        if (instance.subtitleText != null)
        {
            UIFontProvider.ApplyFont(instance.subtitleText, font);
            instance.subtitleText.text = GameLocalization.BreakSubtitle;
        }

        if (instance.centerGuideText != null)
        {
            UIFontProvider.ApplyFont(instance.centerGuideText, font);
            instance.centerGuideText.text = firstRoundPrep
                ? GameLocalization.BreakGuideFirst
                : GameLocalization.BreakGuideNormal;
        }

        if (instance.countdownText != null)
        {
            UIFontProvider.ApplyFont(instance.countdownText, font);
        }

        if (instance.skipBreakButtonLabel != null)
        {
            UIFontProvider.ApplyFont(instance.skipBreakButtonLabel, font);
            instance.skipBreakButtonLabel.text = firstRoundPrep
                ? GameLocalization.BreakStartRound1
                : GameLocalization.BreakNextRound;
        }
    }

    void SetVisibleImmediate(bool visible)
    {
        targetVisible = visible;
        panelAlpha = visible ? 1f : 0f;
        if (panelGroup != null)
        {
            panelGroup.alpha = panelAlpha;
        }
        if (countdownGroup != null)
        {
            countdownGroup.alpha = panelAlpha;
            countdownGroup.blocksRaycasts = visible;
            countdownGroup.interactable = visible;
        }
        if (skipBreakButton != null)
        {
            skipBreakButton.interactable = visible;
        }
        if (panelRect != null) panelRect.gameObject.SetActive(visible);
        if (centerGroupRect != null) centerGroupRect.gameObject.SetActive(visible);
        if (timeTextObject != null) timeTextObject.SetActive(!visible);
        if (waveIconsObject != null) waveIconsObject.SetActive(!visible);
        if (visible && ShopManager.Instance != null)
        {
            ShopManager.Instance.EnsureShopVisible();
        }
    }
}
