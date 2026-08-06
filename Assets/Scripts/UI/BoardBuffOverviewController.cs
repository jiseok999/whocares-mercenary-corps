using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 배치칸 우측 상단 — 조합 시너지 UI(보스 보상·조합 시너지 칸·유닛 효과 통합 목록)
/// </summary>
public class BoardBuffOverviewController : MonoBehaviour
{
    static BoardBuffOverviewController instance;

    Canvas rootCanvas;
    RectTransform toggleButtonRect;
    Button toggleButton;
    Image toggleButtonBg;
    Text toggleBadgeText;
    Text toggleLabelText;

    GameObject panelRoot;
    RectTransform panelRect;
    Canvas panelCanvas;
    RectTransform listContent;
    ScrollRect listScroll;
    Text emptyStateText;
    Text headerTitleText;
    Text headerCountText;
    Button closeButton;

    readonly List<BuffRowUi> rows = new List<BuffRowUi>(16);
    readonly List<GameManager.BoardBuffOverviewEntry> entryBuffer = new List<GameManager.BoardBuffOverviewEntry>(16);

    Sprite bossDamageIcon;
    Sprite bossAttackSpeedIcon;
    Sprite bossRangeIcon;
    Sprite bossTraitIcon;

    bool panelOpen;
    string lastSignature = "";

    const float PanelWidth = 400f;
    const float PanelMaxHeight = 520f;
    const float PanelMinHeight = 160f;
    const float PanelHeaderFootprint = 70f;
    const float RowMinHeight = 78f;
    const float RowSpacing = 6f;
    const float RowTextWidth = 320f;
    const int PanelSortingOrder = GameUiSortingLayers.PlacementUi;

    static readonly Color AccentGold = new Color(1f, 0.78f, 0.32f, 1f);
    static readonly Color Cream = new Color(0.93f, 0.86f, 0.74f, 1f);
    static readonly Color Muted = new Color(0.62f, 0.56f, 0.48f, 1f);
    static readonly Color BossBadge = new Color(0.85f, 0.45f, 0.22f, 0.95f);
    static readonly Color SynergyBadge = new Color(0.28f, 0.52f, 0.82f, 0.95f);
    static readonly Color UnitEffectBadge = new Color(0.35f, 0.72f, 0.58f, 0.95f);

    class BuffRowUi
    {
        public GameObject root;
        public Image icon;
        public Image badgeBg;
        public Text badgeText;
        public Text titleText;
        public Text locationText;
        public Text effectText;
    }

    public static void Ensure(Canvas canvas)
    {
        if (instance != null) return;
        if (canvas == null) return;

        Transform placementLayer = GameUiSortingLayers.EnsurePlacementLayer(canvas.transform);
        Transform parent = placementLayer != null ? placementLayer : canvas.transform;

        GameObject host = new GameObject("BoardBuffOverview");
        host.transform.SetParent(parent, false);
        instance = host.AddComponent<BoardBuffOverviewController>();
        instance.Initialize(canvas);
    }

    void Initialize(Canvas canvas)
    {
        rootCanvas = canvas;
        bossDamageIcon = LoadSprite("attack_effect_fire");
        bossAttackSpeedIcon = LoadSprite("1-Lightning");

        CreateToggleButton();
        CreatePanel();
        panelRoot.SetActive(false);
        panelOpen = false;
        GameLocalizationCoordinator.Register(RefreshLocalizedUi);
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            GameLocalizationCoordinator.Unregister(RefreshLocalizedUi);
            instance = null;
        }
    }

    void RefreshLocalizedUi()
    {
        Font font = UIFontProvider.Get();
        if (toggleLabelText != null)
        {
            toggleLabelText.text = GameLocalization.BoardBuffToggleShort;
            UIFontProvider.ApplyFont(toggleLabelText, font);
            if (toggleButtonRect != null)
            {
                float labelWidth = Mathf.Max(52f, toggleLabelText.preferredWidth);
                toggleButtonRect.sizeDelta = new Vector2(36f + labelWidth + 34f, 38f);
            }
        }
        if (toggleBadgeText != null)
        {
            UIFontProvider.ApplyFont(toggleBadgeText, font);
        }
        if (headerTitleText != null)
        {
            headerTitleText.text = GameLocalization.BoardBuffSynergyLabel;
            UIFontProvider.ApplyFont(headerTitleText, font);
        }
        if (headerCountText != null)
        {
            UIFontProvider.ApplyFont(headerCountText, font);
        }
        if (emptyStateText != null)
        {
            emptyStateText.text = GameLocalization.BoardBuffEmpty;
            UIFontProvider.ApplyFont(emptyStateText, font);
        }
        lastSignature = "";
        if (panelOpen)
        {
            RefreshPanelContents();
        }
    }

    void Update()
    {
        PositionToggleButton();
        RefreshBadgeCount();

        if (!panelOpen) return;

        RefreshPanelContents();

        if (WasPointerPressedThisFrame() && !IsPointerOverPanelUi() && !IsPointerOverToggleButton())
        {
            SetPanelOpen(false);
        }
    }

    void RefreshBadgeCount()
    {
        entryBuffer.Clear();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GetAllBoardBuffOverviews(entryBuffer);
        }

        if (toggleBadgeText != null)
        {
            toggleBadgeText.text = entryBuffer.Count.ToString();
        }
    }

    void CreateToggleButton()
    {
        GameObject btnObj = new GameObject("BoardBuffToggle");
        btnObj.transform.SetParent(rootCanvas.transform, false);

        toggleButtonRect = btnObj.AddComponent<RectTransform>();
        toggleButtonRect.sizeDelta = new Vector2(118f, 38f);
        toggleButtonRect.pivot = new Vector2(1f, 1f);

        toggleButtonBg = btnObj.AddComponent<Image>();
        toggleButtonBg.type = Image.Type.Sliced;
        toggleButtonBg.sprite = WarmRoundedSprite.Get(
            new Color(0.14f, 0.10f, 0.08f, 0.96f),
            14,
            new Color(1f, 0.82f, 0.38f, 0.88f),
            2f);
        toggleButtonBg.raycastTarget = true;

        toggleButton = btnObj.AddComponent<Button>();
        toggleButton.targetGraphic = toggleButtonBg;
        ColorBlock colors = toggleButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.06f, 0.94f, 1f);
        colors.pressedColor = new Color(0.82f, 0.78f, 0.68f, 1f);
        colors.selectedColor = Color.white;
        toggleButton.colors = colors;
        toggleButton.onClick.AddListener(TogglePanel);

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = StatIconFactory.Get(StatIconFactory.IconType.Range);
        icon.color = AccentGold;
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(22f, 22f);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text label = labelObj.AddComponent<Text>();
        toggleLabelText = label;
        label.text = GameLocalization.BoardBuffToggleShort;
        label.font = UIFontProvider.Get();
        label.fontSize = 17;
        label.fontStyle = FontStyle.Bold;
        label.color = Cream;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(36f, 0f);
        labelRect.offsetMax = new Vector2(-28f, 0f);

        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(btnObj.transform, false);
        Image badgeBg = badgeObj.AddComponent<Image>();
        badgeBg.type = Image.Type.Sliced;
        badgeBg.sprite = WarmRoundedSprite.Get(AccentGold, 8, new Color(0f, 0f, 0f, 0f), 0f);
        badgeBg.raycastTarget = false;
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 0.5f);
        badgeRect.anchorMax = new Vector2(1f, 0.5f);
        badgeRect.pivot = new Vector2(1f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(-8f, 0f);
        badgeRect.sizeDelta = new Vector2(22f, 22f);

        GameObject badgeTextObj = new GameObject("Count");
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        toggleBadgeText = badgeTextObj.AddComponent<Text>();
        toggleBadgeText.text = "0";
        toggleBadgeText.font = UIFontProvider.Get();
        toggleBadgeText.fontSize = 13;
        toggleBadgeText.fontStyle = FontStyle.Bold;
        toggleBadgeText.color = new Color(0.12f, 0.08f, 0.04f, 1f);
        toggleBadgeText.alignment = TextAnchor.MiddleCenter;
        toggleBadgeText.raycastTarget = false;
        RectTransform badgeTextRect = badgeTextObj.GetComponent<RectTransform>();
        badgeTextRect.anchorMin = Vector2.zero;
        badgeTextRect.anchorMax = Vector2.one;
        badgeTextRect.offsetMin = Vector2.zero;
        badgeTextRect.offsetMax = Vector2.zero;
    }

    void CreatePanel()
    {
        panelRoot = new GameObject("BoardBuffOverviewPanel");
        panelRoot.transform.SetParent(rootCanvas.transform, false);

        panelCanvas = panelRoot.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = PanelSortingOrder;
        panelRoot.AddComponent<GraphicRaycaster>();

        Image blocker = panelRoot.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.35f);
        blocker.raycastTarget = true;

        RectTransform blockerRect = panelRoot.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        GameObject cardObj = new GameObject("Card");
        cardObj.transform.SetParent(panelRoot.transform, false);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.type = Image.Type.Sliced;
        cardBg.sprite = WarmRoundedSprite.Get(
            new Color(0.10f, 0.08f, 0.06f, 0.98f),
            22,
            new Color(1f, 0.82f, 0.38f, 0.55f),
            2f);
        cardBg.raycastTarget = true;

        panelRect = cardObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelMaxHeight);

        GameObject accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(cardObj.transform, false);
        Image accent = accentObj.AddComponent<Image>();
        accent.type = Image.Type.Sliced;
        accent.sprite = WarmRoundedSprite.Get(AccentGold, 4, new Color(0f, 0f, 0f, 0f), 0f);
        accent.raycastTarget = false;
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -10f);
        accentRect.sizeDelta = new Vector2(120f, 5f);

        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(cardObj.transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 52f);

        headerTitleText = CreateText(headerObj.transform, "Title", GameLocalization.BoardBuffSynergyLabel, 24, FontStyle.Bold, AccentGold,
            TextAnchor.MiddleLeft, new Vector2(18f, -14f), new Vector2(260f, 32f));

        headerCountText = CreateText(headerObj.transform, "Count", "", 16, FontStyle.Normal, Muted,
            TextAnchor.MiddleLeft, new Vector2(18f, -36f), new Vector2(280f, 22f));

        closeButton = CreateHeaderButton(headerObj.transform, "Close", "✕", new Vector2(-14f, -26f), OnCloseClicked);

        GameObject scrollObj = new GameObject("Scroll");
        scrollObj.transform.SetParent(cardObj.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(12f, 12f);
        scrollRect.offsetMax = new Vector2(-12f, -58f);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        listScroll = scroll;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportMask = viewportObj.AddComponent<Image>();
        viewportMask.color = new Color(1f, 1f, 1f, 0.01f);
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        listContent = contentObj.AddComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.sizeDelta = new Vector2(0f, 0f);

        scroll.viewport = viewportRect;
        scroll.content = listContent;

        emptyStateText = CreateText(contentObj.transform, "Empty", GameLocalization.BoardBuffEmpty, 17,
            FontStyle.Italic, Muted, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(340f, 80f));
        RectTransform emptyRect = emptyStateText.rectTransform;
        emptyRect.anchorMin = new Vector2(0.5f, 1f);
        emptyRect.anchorMax = new Vector2(0.5f, 1f);
        emptyRect.pivot = new Vector2(0.5f, 1f);
        emptyRect.anchoredPosition = new Vector2(0f, -40f);
        emptyStateText.gameObject.SetActive(false);
    }

    void TogglePanel()
    {
        SetPanelOpen(!panelOpen);
    }

    void OnCloseClicked()
    {
        SetPanelOpen(false);
    }

    void SetPanelOpen(bool open)
    {
        panelOpen = open;
        if (panelRoot != null)
        {
            panelRoot.SetActive(open);
        }

        if (open)
        {
            lastSignature = "";
            RefreshPanelContents();
            if (listScroll != null)
            {
                listScroll.verticalNormalizedPosition = 1f;
            }
            PositionPanelNearButton();
        }
    }

    void RefreshPanelContents()
    {
        entryBuffer.Clear();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GetAllBoardBuffOverviews(entryBuffer);
        }

        string signature = BuildSignature();
        if (signature == lastSignature)
        {
            return;
        }

        lastSignature = signature;
        headerCountText.text = entryBuffer.Count > 0
            ? GameLocalization.BoardBuffHeaderCountFormat(entryBuffer.Count)
            : GameLocalization.BoardBuffHeaderHint;
        UIFontProvider.ApplyFont(headerCountText);

        emptyStateText.gameObject.SetActive(entryBuffer.Count == 0);
        EnsureRowCount(entryBuffer.Count);

        float y = 0f;
        for (int i = 0; i < rows.Count; i++)
        {
            bool show = i < entryBuffer.Count;
            rows[i].root.SetActive(show);
            if (!show) continue;

            GameManager.BoardBuffOverviewEntry entry = entryBuffer[i];
            float rowHeight = LayoutRow(rows[i], entry);
            RectTransform rowRect = rows[i].root.GetComponent<RectTransform>();
            rowRect.anchoredPosition = new Vector2(0f, -y);
            rowRect.sizeDelta = new Vector2(0f, rowHeight);
            y += rowHeight + RowSpacing;
        }

        listContent.sizeDelta = new Vector2(0f, Mathf.Max(y, entryBuffer.Count == 0 ? 120f : y));
        PositionPanelNearButton();
    }

    float LayoutRow(BuffRowUi row, GameManager.BoardBuffOverviewEntry entry)
    {
        ApplyRowContent(row, entry);

        const float left = 52f;
        const float topPad = 8f;
        const float badgeHeight = 18f;
        const float titleTop = topPad + badgeHeight + 6f;

        float titleH = Mathf.Max(22f, MeasureWrappedText(row.titleText, RowTextWidth));
        bool hasLocation = !string.IsNullOrEmpty(entry.location);
        float locH = hasLocation ? Mathf.Max(18f, MeasureWrappedText(row.locationText, RowTextWidth)) : 0f;
        float effectH = Mathf.Max(20f, MeasureWrappedText(row.effectText, RowTextWidth));

        row.locationText.gameObject.SetActive(hasLocation);
        SetAnchoredTextRect(row.titleText, left, titleTop, RowTextWidth, titleH);

        float locTop = titleTop + titleH + 2f;
        if (hasLocation)
        {
            SetAnchoredTextRect(row.locationText, left, locTop, RowTextWidth, locH);
        }

        float effectTop = locTop + (hasLocation ? locH + 4f : 0f);
        SetAnchoredTextRect(row.effectText, left, effectTop, RowTextWidth, effectH);

        return Mathf.Max(RowMinHeight, effectTop + effectH + 10f);
    }

    static void SetAnchoredTextRect(Text text, float left, float top, float width, float height)
    {
        if (text == null) return;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    static float MeasureWrappedText(Text text, float width)
    {
        if (text == null || string.IsNullOrEmpty(text.text)) return 0f;

        TextGenerator generator = new TextGenerator();
        TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(width, 0f));
        return generator.GetPreferredHeight(text.text, settings) / text.pixelsPerUnit;
    }

    string BuildSignature()
    {
        if (entryBuffer.Count == 0) return "empty";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < entryBuffer.Count; i++)
        {
            GameManager.BoardBuffOverviewEntry e = entryBuffer[i];
            sb.Append(e.category).Append('|').Append(e.title).Append('|').Append(e.location)
              .Append('|').Append(e.effect).Append('|').Append(e.sourceUnitNumber).Append(';');
        }
        return sb.ToString();
    }

    void ApplyRowContent(BuffRowUi row, GameManager.BoardBuffOverviewEntry entry)
    {
        bool isSynergy = entry.category == GameLocalization.BoardBuffCategoryKey.Synergy && !string.IsNullOrEmpty(entry.traitName);
        bool isUnitEffect = entry.category == GameLocalization.BoardBuffCategoryKey.UnitEffect;
        row.badgeText.text = GameLocalization.GetBoardBuffCategoryLabel(entry.category);
        if (isUnitEffect)
        {
            row.badgeBg.color = UnitEffectBadge;
        }
        else if (isSynergy)
        {
            row.badgeBg.color = SynergyBadge;
        }
        else
        {
            row.badgeBg.color = BossBadge;
        }
        row.titleText.text = entry.title;
        row.locationText.text = entry.location;
        row.effectText.text = entry.effect;

        if (isSynergy || (isUnitEffect && !string.IsNullOrEmpty(entry.traitName)))
        {
            row.icon.sprite = TraitIconFactory.Get(entry.traitName);
            row.icon.color = Color.white;
        }
        else if (entry.isTraitBuff)
        {
            EnsureBossTraitIcon();
            row.icon.sprite = bossTraitIcon;
            row.icon.color = Color.white;
        }
        else if (entry.isRangeBuff)
        {
            EnsureBossRangeIcon();
            row.icon.sprite = bossRangeIcon;
            row.icon.color = Color.white;
        }
        else
        {
            row.icon.sprite = entry.isAttackSpeed ? bossAttackSpeedIcon : bossDamageIcon;
            row.icon.color = Color.white;
        }

        Font font = UIFontProvider.Get();
        UIFontProvider.ApplyFont(row.badgeText, font);
        UIFontProvider.ApplyFont(row.titleText, font);
        UIFontProvider.ApplyFont(row.locationText, font);
        UIFontProvider.ApplyFont(row.effectText, font);
    }

    void EnsureBossRangeIcon()
    {
        if (bossRangeIcon != null) return;
        bossRangeIcon = Resources.Load<Sprite>("Ability_warden");
        if (bossRangeIcon == null)
        {
            bossRangeIcon = bossDamageIcon;
        }
    }

    void EnsureBossTraitIcon()
    {
        if (bossTraitIcon != null) return;
        bossTraitIcon = Resources.Load<Sprite>("Ability_icons1_15");
        if (bossTraitIcon == null)
        {
            bossTraitIcon = bossDamageIcon;
        }
    }

    void EnsureRowCount(int count)
    {
        while (rows.Count < count)
        {
            rows.Add(CreateRow(listContent, rows.Count));
        }
    }

    BuffRowUi CreateRow(Transform parent, int index)
    {
        GameObject rowObj = new GameObject($"BuffRow_{index}");
        rowObj.transform.SetParent(parent, false);
        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, RowMinHeight);

        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.type = Image.Type.Sliced;
        rowBg.sprite = WarmRoundedSprite.Get(
            new Color(0.16f, 0.13f, 0.10f, 0.92f),
            12,
            new Color(1f, 0.85f, 0.45f, 0.18f),
            1f);
        rowBg.raycastTarget = false;

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(rowObj.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(34f, 34f);

        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(rowObj.transform, false);
        Image badgeBg = badgeObj.AddComponent<Image>();
        badgeBg.type = Image.Type.Sliced;
        badgeBg.sprite = WarmRoundedSprite.Get(BossBadge, 8, new Color(0f, 0f, 0f, 0f), 0f);
        badgeBg.raycastTarget = false;
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(52f, -8f);
        badgeRect.sizeDelta = new Vector2(72f, 18f);

        Text badgeText = CreateText(badgeObj.transform, "Text", "분류", 11, FontStyle.Bold, Color.white,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(72f, 18f));
        RectTransform badgeTextRect = badgeText.rectTransform;
        badgeTextRect.anchorMin = Vector2.zero;
        badgeTextRect.anchorMax = Vector2.one;
        badgeTextRect.offsetMin = Vector2.zero;
        badgeTextRect.offsetMax = Vector2.zero;

        Text titleText = CreateText(rowObj.transform, "Title", "제목", 18, FontStyle.Bold, Cream,
            TextAnchor.UpperLeft, new Vector2(52f, -28f), new Vector2(310f, 24f));

        Text locationText = CreateText(rowObj.transform, "Location", "위치", 14, FontStyle.Normal, Muted,
            TextAnchor.UpperLeft, new Vector2(52f, -48f), new Vector2(320f, 20f));

        Text effectText = CreateText(rowObj.transform, "Effect", "효과", 15, FontStyle.Normal, new Color(0.78f, 0.92f, 0.72f, 1f),
            TextAnchor.UpperLeft, new Vector2(52f, -64f), new Vector2(320f, 20f));

        return new BuffRowUi
        {
            root = rowObj,
            icon = icon,
            badgeBg = badgeBg,
            badgeText = badgeText,
            titleText = titleText,
            locationText = locationText,
            effectText = effectText
        };
    }

    void PositionToggleButton()
    {
        if (toggleButtonRect == null || rootCanvas == null) return;
        if (!TryGetBoardTopRightCanvasLocal(out Vector2 localPoint)) return;
        toggleButtonRect.anchoredPosition = localPoint + new Vector2(8f, 26f);
    }

    void PositionPanelNearButton()
    {
        if (panelRect == null || toggleButtonRect == null) return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        float contentHeight = listContent != null ? listContent.sizeDelta.y : 0f;
        float maxBodyHeight = PanelMaxHeight - PanelHeaderFootprint;
        float bodyHeight = Mathf.Min(contentHeight, maxBodyHeight);
        float panelH = Mathf.Clamp(bodyHeight + PanelHeaderFootprint, PanelMinHeight, PanelMaxHeight);
        panelRect.sizeDelta = new Vector2(PanelWidth, panelH);

        if (listScroll != null)
        {
            listScroll.enabled = contentHeight > maxBodyHeight + 1f;
        }

        Vector2 btnPos = toggleButtonRect.anchoredPosition;
        Vector2 pos = btnPos + new Vector2(-PanelWidth * 0.5f + 40f, -panelH * 0.5f - 48f);
        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        pos.x = Mathf.Clamp(pos.x, -halfW + PanelWidth * 0.5f + 12f, halfW - PanelWidth * 0.5f - 12f);
        pos.y = Mathf.Clamp(pos.y, -halfH + panelH * 0.5f + 12f, halfH - panelH * 0.5f - 12f);
        panelRect.anchoredPosition = pos;
    }

    bool TryGetBoardTopRightCanvasLocal(out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        BoardManager board = FindFirstObjectByType<BoardManager>();
        Camera cam = Camera.main;
        if (board == null || cam == null || rootCanvas == null) return false;

        float aspect = Screen.width / (float)Screen.height;
        float cameraLeft = cam.transform.position.x - (cam.orthographicSize * aspect);
        float boardHeight = board.boardRows * board.cellSize;
        float leftOffset = board.cellSize * 0.5f;
        float worldWidth = cam.orthographicSize * aspect * 2f;
        float unitsPerPixel = worldWidth / Screen.width;
        float uiOffset = (board.leftUiWidthPixels + board.leftUiMarginPixels) * unitsPerPixel;

        Vector3 boardStart = new Vector3(
            cameraLeft + leftOffset + uiOffset,
            cam.transform.position.y + boardHeight * 0.5f - board.cellSize * 0.5f,
            0f
        );

        Vector3 topRightWorld = boardStart + new Vector3((board.boardColumns - 1) * board.cellSize, 0f, 0f);
        topRightWorld.x += board.cellSize * 0.55f;
        topRightWorld.y += board.cellSize * 1.45f;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, topRightWorld),
            null,
            out localPoint
        );
    }

    bool IsPointerOverToggleButton()
    {
        if (toggleButtonRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            toggleButtonRect,
            GetPointerScreenPosition(),
            null
        );
    }

    bool IsPointerOverPanelUi()
    {
        if (panelRect == null || !panelOpen) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            panelRect,
            GetPointerScreenPosition(),
            null
        );
    }

    static Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
        return Input.mousePosition;
    }

    static bool WasPointerPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif
        return Input.GetMouseButtonDown(0);
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style, Color color,
        TextAnchor alignment, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.raycastTarget = false;
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }

    static Button CreateHeaderButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image bg = obj.AddComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.sprite = WarmRoundedSprite.Get(
            new Color(0.22f, 0.14f, 0.10f, 0.95f),
            10,
            new Color(1f, 0.75f, 0.35f, 0.5f),
            1f);
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(32f, 32f);

        Text text = CreateText(obj.transform, "Label", label, 18, FontStyle.Bold, Cream,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(32f, 32f));
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    static Sprite LoadSprite(string resourceName)
    {
        Sprite single = Resources.Load<Sprite>(resourceName);
        if (single != null) return single;
        Sprite[] all = Resources.LoadAll<Sprite>(resourceName);
        if (all != null && all.Length > 0) return all[0];
        return null;
    }
}
