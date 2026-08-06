using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 / 시작 시 로그라이크 3택1 유닛 선택 UI
/// </summary>
public static class LevelUpUnitSelectionController
{
    const int OverlaySortingOrder = 345;

    // 오른쪽 사이드바 공간 확보를 위해 모달을 왼쪽으로 이동하는 양
    const float SidePanelWidth = 228f;
    const float SidePanelGap = 12f;
    const float GroupShiftX = (SidePanelWidth + SidePanelGap) * 0.5f;
    const float ProbabilityPanelHeight = 332f;
    const float SidebarSectionGap = 14f;

    static readonly Queue<int> pendingLevels = new Queue<int>();
    static GameObject activePanel;
    static LevelUpUnitSelectionView activeSelectionView;
    static bool isStartingUnitPick;
    static System.Action startingPickCompleteCallback;
    static Text activeTitleText;
    static Text activeSubtitleText;
    static Text activeHintText;
    static Text activeRerollLabel;
    static int activePlayerLevel;

    public static bool IsShowing => activePanel != null;

    public static void ShowStartingUnitSelection(System.Action onComplete)
    {
        isStartingUnitPick = true;
        startingPickCompleteCallback = onComplete;
        activePlayerLevel = GameManager.Instance != null ? GameManager.Instance.playerLevel : 1;
        ShowSelection(activePlayerLevel);
    }

    static void ApplyActiveCopy()
    {
        if (isStartingUnitPick)
        {
            SetHeaderCopy(GameLocalization.LevelUpStartTitle, GameLocalization.LevelUpStartSubtitle, GameLocalization.LevelUpStartHint);
        }
        else
        {
            SetHeaderCopy(GameLocalization.LevelUpTitleFormat(activePlayerLevel), GameLocalization.LevelUpSubtitle, GameLocalization.LevelUpHint);
        }
    }

    static void SetHeaderCopy(string title, string subtitle, string hint)
    {
        Font font = UIFontProvider.Get();
        if (activeTitleText != null)
        {
            activeTitleText.font = font;
            activeTitleText.text = title;
        }
        if (activeSubtitleText != null)
        {
            activeSubtitleText.font = font;
            activeSubtitleText.text = subtitle;
        }
        if (activeHintText != null)
        {
            activeHintText.font = font;
            activeHintText.text = hint;
        }
        if (activeRerollLabel != null && activeRerollLabel.color.a > 0.9f)
        {
            activeRerollLabel.font = font;
            activeRerollLabel.text = GameLocalization.LevelUpReroll;
        }
    }

    public static void RefreshLocalizedTexts()
    {
        if (activePanel == null) return;
        ApplyActiveCopy();
        activeSelectionView?.RefreshLocalizedTexts();
    }

    public static void EnqueueLevelUps(IReadOnlyList<int> newLevels)
    {
        if (newLevels == null || newLevels.Count == 0) return;
        for (int i = 0; i < newLevels.Count; i++)
        {
            pendingLevels.Enqueue(newLevels[i]);
        }
        TryShowNext();
    }

    public static void NotifySelectionFinished()
    {
        FinishCurrentAndContinue();
    }

    static void TryShowNext()
    {
        if (activePanel != null) return;
        if (pendingLevels.Count == 0) return;

        isStartingUnitPick = false;
        startingPickCompleteCallback = null;
        activePlayerLevel = pendingLevels.Dequeue();
        ShowSelection(activePlayerLevel);
    }

    static void ShowSelection(int playerLevel)
    {
        activePlayerLevel = playerLevel;
        if (ShopManager.Instance == null)
        {
            Debug.LogWarning("LevelUpUnitSelection: ShopManager 없음");
            FinishCurrentAndContinue();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetShopOpen(true);
        }

        Canvas rootCanvas = FindRootOverlayCanvas();
        if (rootCanvas == null)
        {
            FinishCurrentAndContinue();
            return;
        }

        Color accent = new Color(1f, 0.78f, 0.30f, 1f);
        Color cream = new Color(0.93f, 0.86f, 0.74f, 1f);
        Color tan = new Color(0.80f, 0.66f, 0.48f, 1f);

        activePanel = new GameObject("LevelUpUnitSelectionPanel");
        activePanel.transform.SetParent(rootCanvas.transform, false);

        Canvas overlayCanvas = activePanel.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = OverlaySortingOrder;
        activePanel.AddComponent<GraphicRaycaster>();

        Image dimImage = activePanel.AddComponent<Image>();
        dimImage.color = new Color(0.03f, 0.02f, 0.015f, 0.92f);
        StretchFull(activePanel.GetComponent<RectTransform>());

        GameObject modalShadow = new GameObject("ModalShadow");
        modalShadow.transform.SetParent(activePanel.transform, false);
        Image modalShadowImg = modalShadow.AddComponent<Image>();
        modalShadowImg.raycastTarget = false;
        modalShadowImg.type = Image.Type.Sliced;
        modalShadowImg.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.45f), 36, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform modalShadowRect = modalShadow.GetComponent<RectTransform>();
        modalShadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalShadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalShadowRect.pivot = new Vector2(0.5f, 0.5f);
        modalShadowRect.sizeDelta = new Vector2(940f, 920f);
        modalShadowRect.anchoredPosition = new Vector2(-GroupShiftX, -12f);

        GameObject modalObj = new GameObject("ModalCard");
        modalObj.transform.SetParent(activePanel.transform, false);
        Image modalBg = modalObj.AddComponent<Image>();
        modalBg.type = Image.Type.Sliced;
        modalBg.raycastTarget = false;
        modalBg.sprite = WarmRoundedSprite.Get(
            new Color(0.11f, 0.08f, 0.06f, 0.98f),
            32,
            new Color(1f, 0.82f, 0.45f, 0.28f),
            2.5f);
        RectTransform modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.sizeDelta = new Vector2(900f, 880f);
        modalRect.anchoredPosition = new Vector2(-GroupShiftX, 0f);

        CanvasGroup modalGroup = modalObj.AddComponent<CanvasGroup>();

        GameObject accentBar = new GameObject("AccentBar");
        accentBar.transform.SetParent(modalObj.transform, false);
        Image accentImg = accentBar.AddComponent<Image>();
        accentImg.raycastTarget = false;
        accentImg.sprite = WarmRoundedSprite.Get(accent, 6, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentBar.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -12f);
        accentRect.sizeDelta = new Vector2(140f, 6f);

        LevelUpSelectionCelebrationFx celebrationFx = LevelUpSelectionCelebrationFx.Create(activePanel.transform);

        activeTitleText = CreateHeader(modalObj.transform, accent, tan, cream);

        GameObject cardsRow = new GameObject("CardsRow");
        cardsRow.transform.SetParent(modalObj.transform, false);
        RectTransform cardsRect = cardsRow.AddComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.5f, 0f);
        cardsRect.anchorMax = new Vector2(0.5f, 0f);
        cardsRect.pivot = new Vector2(0.5f, 0f);
        cardsRect.anchoredPosition = new Vector2(0f, 104f);
        cardsRect.sizeDelta = new Vector2(860f, 664f);

        GameObject rerollObj = new GameObject("RerollButton");
        rerollObj.transform.SetParent(modalObj.transform, false);
        Image rerollBg = rerollObj.AddComponent<Image>();
        rerollBg.type = Image.Type.Sliced;
        rerollBg.sprite = WarmRoundedSprite.Get(new Color(0.18f, 0.14f, 0.10f, 1f), 16, accent, 2f);
        RectTransform rerollRect = rerollObj.GetComponent<RectTransform>();
        rerollRect.anchorMin = new Vector2(0.5f, 0f);
        rerollRect.anchorMax = new Vector2(0.5f, 0f);
        rerollRect.pivot = new Vector2(0.5f, 0f);
        rerollRect.anchoredPosition = new Vector2(0f, 24f);
        rerollRect.sizeDelta = new Vector2(320f, 52f);

        Button rerollButton = rerollObj.AddComponent<Button>();
        rerollButton.targetGraphic = rerollBg;
        ColorBlock rerollColors = rerollButton.colors;
        rerollColors.normalColor = Color.white;
        rerollColors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        rerollColors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        rerollButton.colors = rerollColors;

        Text rerollLabel = CreateText(rerollObj.transform, GameLocalization.LevelUpReroll, 18, FontStyle.Bold,
            accent, TextAnchor.MiddleCenter);
        StretchFull(rerollLabel.rectTransform);
        activeRerollLabel = rerollLabel;

        CreateRightSidebar(modalObj.transform, playerLevel, accent, cream, tan);
        TraitManager.SetTraitUiSelectionBoost(true);

        LevelUpUnitSelectionView view = modalObj.AddComponent<LevelUpUnitSelectionView>();
        activeSelectionView = view;
        rerollButton.onClick.AddListener(view.OnRerollClicked);
        view.Initialize(
            playerLevel,
            cardsRow.transform,
            rerollButton,
            rerollLabel,
            modalGroup,
            modalRect,
            modalShadowRect,
            rerollRect,
            dimImage,
            activeTitleText,
            celebrationFx,
            !isStartingUnitPick);
        ApplyActiveCopy();
    }

    static Text CreateHeader(Transform parent, Color accent, Color tan, Color cream)
    {
        Text titleText = CreateText(parent, string.Empty, 40, FontStyle.Bold, accent, TextAnchor.UpperCenter);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(760f, 52f);

        activeSubtitleText = CreateText(parent, string.Empty, 14, FontStyle.Normal, tan, TextAnchor.UpperCenter);
        RectTransform subtitleRect = activeSubtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -92f);
        subtitleRect.sizeDelta = new Vector2(760f, 20f);

        activeHintText = CreateText(parent, string.Empty, 16, FontStyle.Normal, cream, TextAnchor.UpperCenter);
        RectTransform hintRect = activeHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -122f);
        hintRect.sizeDelta = new Vector2(760f, 24f);

        GameObject dividerObj = new GameObject("HeaderDivider");
        dividerObj.transform.SetParent(parent, false);
        Image dividerImg = dividerObj.AddComponent<Image>();
        dividerImg.raycastTarget = false;
        dividerImg.sprite = WarmRoundedSprite.Get(new Color(1f, 0.82f, 0.45f, 0.18f), 2, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 1f);
        dividerRect.anchorMax = new Vector2(0.5f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = new Vector2(0f, -152f);
        dividerRect.sizeDelta = new Vector2(700f, 2f);

        return titleText;
    }

    static void CreateRightSidebar(Transform parent, int playerLevel, Color accent, Color cream, Color tan)
    {
        List<ShopGradePoolLock.GradePoolStatus> poolStatuses = ShopManager.Instance != null
            ? ShopManager.Instance.GetGradePoolStatuses()
            : new List<ShopGradePoolLock.GradePoolStatus>();
        float poolPanelHeight = CalculatePoolPanelHeight(poolStatuses);
        float sidebarHeight = ProbabilityPanelHeight + SidebarSectionGap + poolPanelHeight;

        GameObject sidebarObj = new GameObject("RightSidebar");
        sidebarObj.transform.SetParent(parent, false);
        RectTransform sidebarRect = sidebarObj.AddComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(1f, 0.5f);
        sidebarRect.anchorMax = new Vector2(1f, 0.5f);
        sidebarRect.pivot = new Vector2(0f, 0.5f);
        sidebarRect.anchoredPosition = new Vector2(SidePanelGap, 0f);
        sidebarRect.sizeDelta = new Vector2(SidePanelWidth, sidebarHeight);

        float topY = sidebarHeight * 0.5f;
        CreateGradeProbabilityPanel(sidebarObj.transform, playerLevel, accent, cream, tan, topY);
        float poolTopY = topY - ProbabilityPanelHeight - SidebarSectionGap;
        CreateRegisteredPoolPanel(sidebarObj.transform, poolStatuses, accent, cream, tan, poolTopY, poolPanelHeight);
    }

    static float CalculatePoolPanelHeight(List<ShopGradePoolLock.GradePoolStatus> statuses)
    {
        const float headerHeight = 54f;
        const float emptyBodyHeight = 44f;
        if (statuses == null || statuses.Count == 0) return headerHeight + emptyBodyHeight + 12f;

        const float gradeGap = 8f;
        float bodyHeight = 0f;
        for (int i = 0; i < statuses.Count; i++)
        {
            bodyHeight += GetPoolGradeRowHeight(statuses[i]);
            if (i > 0) bodyHeight += gradeGap;
        }
        return headerHeight + bodyHeight + 12f;
    }

    static float GetPoolGradeRowHeight(ShopGradePoolLock.GradePoolStatus status)
    {
        const float gradeHeaderHeight = 22f;
        const float nameLineHeight = 16f;
        const float padding = 8f;
        int count = status.unitNumbers != null ? status.unitNumbers.Count : 0;
        if (count <= 0) return gradeHeaderHeight + 18f + padding;
        return gradeHeaderHeight + count * nameLineHeight + padding;
    }

    static string BuildPoolUnitNamesText(List<int> unitNumbers)
    {
        if (unitNumbers == null || unitNumbers.Count == 0) return "—";

        var sb = new StringBuilder();
        for (int i = 0; i < unitNumbers.Count; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append("· ");
            sb.Append(UnitTraitData.GetDisplayName(unitNumbers[i]));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 모달 오른쪽 사이드바 상단 — 현재 레벨의 등급별 출현 확률
    /// </summary>
    static void CreateGradeProbabilityPanel(Transform parent, int playerLevel, Color accent, Color cream, Color tan, float topY)
    {
        Dictionary<int, float> probs = LevelGradeProbability.GetProbabilitiesForLevel(playerLevel);

        GameObject panelObj = new GameObject("GradeProbabilityPanel");
        panelObj.transform.SetParent(parent, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.raycastTarget = false;
        bg.type = Image.Type.Sliced;
        bg.sprite = WarmRoundedSprite.Get(
            new Color(0.11f, 0.08f, 0.06f, 0.97f), 24,
            new Color(1f, 0.82f, 0.45f, 0.25f), 2f);
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, topY);
        rect.sizeDelta = new Vector2(0f, ProbabilityPanelHeight);

        Text titleText = CreateText(panelObj.transform, GameLocalization.LevelUpGradeOdds, 18, FontStyle.Bold, accent, TextAnchor.UpperCenter);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -16f);
        titleRect.sizeDelta = new Vector2(SidePanelWidth - 20f, 24f);

        Text lvText = CreateText(panelObj.transform, $"LV {playerLevel}", 13, FontStyle.Normal, tan, TextAnchor.UpperCenter);
        RectTransform lvRect = lvText.rectTransform;
        lvRect.anchorMin = new Vector2(0.5f, 1f);
        lvRect.anchorMax = new Vector2(0.5f, 1f);
        lvRect.pivot = new Vector2(0.5f, 1f);
        lvRect.anchoredPosition = new Vector2(0f, -42f);
        lvRect.sizeDelta = new Vector2(SidePanelWidth - 20f, 18f);

        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(panelObj.transform, false);
        Image dividerImg = dividerObj.AddComponent<Image>();
        dividerImg.raycastTarget = false;
        dividerImg.sprite = WarmRoundedSprite.Get(new Color(1f, 0.82f, 0.45f, 0.18f), 2, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 1f);
        dividerRect.anchorMax = new Vector2(0.5f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = new Vector2(0f, -66f);
        dividerRect.sizeDelta = new Vector2(SidePanelWidth - 36f, 2f);

        float rowStartY = -82f;
        float rowStep = 46f;
        int rowIndex = 0;
        for (int grade = 5; grade >= 1; grade--)
        {
            float percent = probs != null && probs.TryGetValue(grade, out float p) ? p : 0f;
            CreateGradeProbabilityRow(panelObj.transform, grade, percent, rowStartY - rowIndex * rowStep, cream);
            rowIndex++;
        }
    }

    static void CreateGradeProbabilityRow(Transform parent, int grade, float percent, float y, Color cream)
    {
        bool available = percent > 0f;
        string hex = UnitCombatStats.GetGradeColorHex(grade);
        Color gradeColor = ColorUtility.TryParseHtmlString($"#{hex}", out Color parsed) ? parsed : cream;
        float dimAlpha = available ? 1f : 0.32f;

        GameObject rowObj = new GameObject($"GradeRow_{grade}");
        rowObj.transform.SetParent(parent, false);
        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(SidePanelWidth - 28f, 38f);

        Text label = CreateText(rowObj.transform, $"{grade}등급", 15, FontStyle.Bold,
            new Color(gradeColor.r, gradeColor.g, gradeColor.b, dimAlpha), TextAnchor.UpperLeft);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(80f, 20f);

        string pctStr = available ? $"{percent:0.#}%" : "—";
        Text pctText = CreateText(rowObj.transform, pctStr, 15, FontStyle.Bold,
            new Color(cream.r, cream.g, cream.b, dimAlpha), TextAnchor.UpperRight);
        RectTransform pctRect = pctText.rectTransform;
        pctRect.anchorMin = new Vector2(1f, 1f);
        pctRect.anchorMax = new Vector2(1f, 1f);
        pctRect.pivot = new Vector2(1f, 1f);
        pctRect.anchoredPosition = Vector2.zero;
        pctRect.sizeDelta = new Vector2(64f, 20f);

        GameObject barBgObj = new GameObject("BarBg");
        barBgObj.transform.SetParent(rowObj.transform, false);
        Image barBg = barBgObj.AddComponent<Image>();
        barBg.raycastTarget = false;
        barBg.type = Image.Type.Sliced;
        barBg.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.45f), 4, new Color(1f, 1f, 1f, 0.07f), 1f);
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 0f);
        barBgRect.anchorMax = new Vector2(1f, 0f);
        barBgRect.pivot = new Vector2(0.5f, 0f);
        barBgRect.anchoredPosition = new Vector2(0f, 2f);
        barBgRect.sizeDelta = new Vector2(0f, 9f);

        if (available)
        {
            float barWidth = (SidePanelWidth - 28f) * Mathf.Clamp01(percent / 100f);
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barBgObj.transform, false);
            Image fill = fillObj.AddComponent<Image>();
            fill.raycastTarget = false;
            fill.type = Image.Type.Sliced;
            fill.sprite = WarmRoundedSprite.Get(gradeColor, 4, new Color(0f, 0f, 0f, 0f), 0f);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(Mathf.Max(4f, barWidth), 0f);
        }
    }

    static void CreateRegisteredPoolPanel(
        Transform parent,
        List<ShopGradePoolLock.GradePoolStatus> statuses,
        Color accent,
        Color cream,
        Color tan,
        float topY,
        float panelHeight)
    {
        GameObject panelObj = new GameObject("RegisteredPoolPanel");
        panelObj.transform.SetParent(parent, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.raycastTarget = false;
        bg.type = Image.Type.Sliced;
        bg.sprite = WarmRoundedSprite.Get(
            new Color(0.11f, 0.08f, 0.06f, 0.97f), 24,
            new Color(0.55f, 0.78f, 1f, 0.28f), 2f);
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, topY);
        rect.sizeDelta = new Vector2(0f, panelHeight);

        Text title = CreateText(panelObj.transform, GameLocalization.LevelUpRegisteredPool, 18, FontStyle.Bold, accent, TextAnchor.UpperCenter);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -14f);
        titleRect.sizeDelta = new Vector2(SidePanelWidth - 20f, 24f);

        Text hint = CreateText(panelObj.transform, GameLocalization.LevelUpOwnedPoolHint, 12, FontStyle.Normal, tan, TextAnchor.UpperCenter);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -36f);
        hintRect.sizeDelta = new Vector2(SidePanelWidth - 20f, 16f);

        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(panelObj.transform, false);
        Image dividerImg = dividerObj.AddComponent<Image>();
        dividerImg.raycastTarget = false;
        dividerImg.sprite = WarmRoundedSprite.Get(new Color(0.55f, 0.78f, 1f, 0.22f), 2, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 1f);
        dividerRect.anchorMax = new Vector2(0.5f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = new Vector2(0f, -52f);
        dividerRect.sizeDelta = new Vector2(SidePanelWidth - 36f, 2f);

        if (statuses == null || statuses.Count == 0)
        {
            Text emptyText = CreateText(panelObj.transform, GameLocalization.LevelUpNoRegisteredUnits, 13, FontStyle.Normal,
                new Color(tan.r, tan.g, tan.b, 0.9f), TextAnchor.UpperCenter);
            RectTransform emptyRect = emptyText.rectTransform;
            emptyRect.anchorMin = new Vector2(0.5f, 1f);
            emptyRect.anchorMax = new Vector2(0.5f, 1f);
            emptyRect.pivot = new Vector2(0.5f, 1f);
            emptyRect.anchoredPosition = new Vector2(0f, -72f);
            emptyRect.sizeDelta = new Vector2(SidePanelWidth - 28f, 36f);
            return;
        }

        const float rowGap = 8f;
        float rowY = -64f;
        for (int i = 0; i < statuses.Count; i++)
        {
            float rowHeight = GetPoolGradeRowHeight(statuses[i]);
            CreateRegisteredPoolGradeRow(panelObj.transform, statuses[i], rowY, rowHeight, cream, tan, accent);
            rowY -= rowHeight + rowGap;
        }
    }

    static void CreateRegisteredPoolGradeRow(
        Transform parent,
        ShopGradePoolLock.GradePoolStatus status,
        float y,
        float rowHeight,
        Color cream,
        Color tan,
        Color accent)
    {
        GameObject rowObj = new GameObject($"PoolGradeRow_{status.grade}");
        rowObj.transform.SetParent(parent, false);
        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(SidePanelWidth - 24f, rowHeight);

        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.raycastTarget = false;
        rowBg.type = Image.Type.Sliced;
        rowBg.sprite = WarmRoundedSprite.Get(new Color(0.08f, 0.06f, 0.05f, 0.88f), 10,
            new Color(1f, 0.82f, 0.45f, 0.12f), 1f);

        string gradeHex = UnitCombatStats.GetGradeColorHex(status.grade);
        string stateText = status.isLocked
            ? "<color=#FFC94D>고정</color>"
            : $"<color=#9D8A66>{status.distinctCount}/{status.threshold}</color>";

        Text gradeText = CreateText(rowObj.transform, $"<color=#{gradeHex}>{status.grade}등급</color>  {stateText}",
            13, FontStyle.Bold, cream, TextAnchor.UpperLeft);
        gradeText.supportRichText = true;
        RectTransform gradeRect = gradeText.rectTransform;
        gradeRect.anchorMin = new Vector2(0f, 1f);
        gradeRect.anchorMax = new Vector2(1f, 1f);
        gradeRect.pivot = new Vector2(0f, 1f);
        gradeRect.anchoredPosition = new Vector2(8f, -4f);
        gradeRect.sizeDelta = new Vector2(-16f, 18f);

        Text namesText = CreateText(rowObj.transform, BuildPoolUnitNamesText(status.unitNumbers),
            12, FontStyle.Normal, new Color(cream.r, cream.g, cream.b, 0.92f), TextAnchor.UpperLeft);
        namesText.horizontalOverflow = HorizontalWrapMode.Wrap;
        namesText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform namesRect = namesText.rectTransform;
        namesRect.anchorMin = new Vector2(0f, 0f);
        namesRect.anchorMax = new Vector2(1f, 1f);
        namesRect.pivot = new Vector2(0f, 1f);
        namesRect.offsetMin = new Vector2(8f, 4f);
        namesRect.offsetMax = new Vector2(-8f, -22f);
    }

    static void FinishCurrentAndContinue()
    {
        TraitManager.SetTraitUiSelectionBoost(false);

        if (activePanel != null)
        {
            Object.Destroy(activePanel);
            activePanel = null;
            activeSelectionView = null;
            activeTitleText = null;
            activeSubtitleText = null;
            activeHintText = null;
            activeRerollLabel = null;
        }

        if (isStartingUnitPick)
        {
            isStartingUnitPick = false;
            System.Action callback = startingPickCompleteCallback;
            startingPickCompleteCallback = null;
            callback?.Invoke();
            return;
        }

        if (pendingLevels.Count == 0 && GameManager.Instance != null)
        {
            GameManager.Instance.SetShopOpen(false);
        }

        TryShowNext();
    }

    static Canvas FindRootOverlayCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        float bestArea = -1f;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isRootCanvas || c.renderMode == RenderMode.WorldSpace) continue;
            if (best != null && best.overrideSorting != c.overrideSorting)
            {
                if (!c.overrideSorting && best.overrideSorting)
                {
                    best = c;
                    RectTransform r = c.transform as RectTransform;
                    bestArea = r != null ? Mathf.Abs(r.rect.width * r.rect.height) : 0f;
                }
                continue;
            }
            RectTransform rect = c.transform as RectTransform;
            float area = rect != null ? Mathf.Abs(rect.rect.width * rect.rect.height) : 0f;
            if (best == null || area > bestArea)
            {
                best = c;
                bestArea = area;
            }
        }
        return best != null ? best : Object.FindFirstObjectByType<Canvas>();
    }

    static Text CreateText(Transform parent, string content, int fontSize, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.raycastTarget = false;
        return text;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
