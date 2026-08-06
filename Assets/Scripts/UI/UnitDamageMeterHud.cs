using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 라운드 유닛별 딜 순위 HUD (접기: 1위+총합, 펼치기: Top5).
/// </summary>
public class UnitDamageMeterHud : MonoBehaviour
{
    public static UnitDamageMeterHud Instance { get; private set; }

    public const float HudWidth = 360f;
    public const float CollapsedHudHeight = 102f;
    const float HudRightOffset = 20f;
    const float HudTopOffset = 20f;
    const float BelowTraitBarGap = 8f;
    const float TraitBarHeight = 96f;
    const float TitleHeight = 34f;
    const float FooterHeight = 28f;
    const float RowHeight = 30f;
    const int ExpandedRowCount = 5;
    const float CollapsedHeight = TitleHeight + RowHeight + FooterHeight + 10f;
    const float ExpandedHeight = TitleHeight + RowHeight * ExpandedRowCount + FooterHeight + 14f;

    static UnitDamageMeterHud instance;

    readonly List<UnitDamageLedger.Entry> entryBuffer = new List<UnitDamageLedger.Entry>(8);
    readonly List<RowUi> rows = new List<RowUi>(ExpandedRowCount);

    GameObject hudRoot;
    RectTransform hudRect;
    Image hudBg;
    Image accentBar;
    Text titleText;
    Text footerText;
    Text toggleHintText;
    Button toggleButton;
    bool expanded;

    struct RowUi
    {
        public GameObject root;
        public Button button;
        public Text rankText;
        public Text nameText;
        public Text valueText;
        public Image barFill;
    }

    public static void EnsureExists()
    {
        if (instance != null) return;
        GameObject host = new GameObject("UnitDamageMeterHud");
        instance = host.AddComponent<UnitDamageMeterHud>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        instance = this;
        GameLocalizationCoordinator.Register(RefreshLocalizedText);
        EnsureHudUi();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            GameLocalizationCoordinator.Unregister(RefreshLocalizedText);
            Instance = null;
            instance = null;
        }
    }

    void RefreshLocalizedText()
    {
        Font font = UIFontProvider.Get();
        if (titleText != null)
        {
            titleText.text = GameLocalization.DamageMeterTitle;
            UIFontProvider.ApplyFont(titleText, font);
        }
        RefreshToggleHint();
        RefreshFooter();
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].rankText != null) UIFontProvider.ApplyFont(rows[i].rankText, font);
            if (rows[i].nameText != null) UIFontProvider.ApplyFont(rows[i].nameText, font);
            if (rows[i].valueText != null) UIFontProvider.ApplyFont(rows[i].valueText, font);
        }
        if (footerText != null) UIFontProvider.ApplyFont(footerText, font);
        if (toggleHintText != null) UIFontProvider.ApplyFont(toggleHintText, font);
    }

    void Update()
    {
        if (hudRoot == null) return;

        bool visible = ShouldShowHud();
        if (hudRoot.activeSelf != visible)
        {
            hudRoot.SetActive(visible);
        }

        if (!visible) return;

        UpdateHudLayout();
        RefreshRows();
    }

    static bool ShouldShowHud()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.IsCombatActive();
    }

    void EnsureHudUi()
    {
        if (hudRoot != null)
        {
            GameUiSortingLayers.ApplySorting(hudRoot, 36, graphicRaycaster: true);
            return;
        }

        Transform parent = GetHudParentTransform();
        hudRoot = new GameObject("UnitDamageMeterHud");
        hudRoot.transform.SetParent(parent, false);

        hudRect = hudRoot.AddComponent<RectTransform>();
        hudRect.sizeDelta = new Vector2(HudWidth, CollapsedHeight);
        hudRect.anchorMin = new Vector2(1f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(1f, 1f);
        ApplyAnchoredPosition();

        GameUiSortingLayers.ApplySorting(hudRoot, 36, graphicRaycaster: true);

        hudBg = hudRoot.AddComponent<Image>();
        hudBg.sprite = WarmRoundedSprite.Get(
            new Color(0.145f, 0.108f, 0.078f, 0.96f),
            16,
            new Color(1f, 0.85f, 0.55f, 0.22f),
            1.5f);
        hudBg.type = Image.Type.Sliced;
        hudBg.raycastTarget = false;

        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(hudRoot.transform, false);
        accentBar = accentObj.AddComponent<Image>();
        accentBar.color = new Color(0.96f, 0.62f, 0.18f, 1f);
        accentBar.raycastTarget = false;
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 3f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(hudRoot.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.font = UIFontProvider.Get();
        titleText.fontSize = 15;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = new Color(1f, 0.79f, 0.30f, 1f);
        titleText.text = GameLocalization.DamageMeterTitle;
        titleText.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(14f, -10f);
        titleRect.sizeDelta = new Vector2(-110f, 20f);

        GameObject toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(hudRoot.transform, false);
        toggleButton = toggleObj.AddComponent<Button>();
        Image toggleBg = toggleObj.AddComponent<Image>();
        toggleBg.sprite = WarmRoundedSprite.Get(
            new Color(0.20f, 0.15f, 0.11f, 0.95f),
            8,
            new Color(1f, 0.85f, 0.55f, 0.14f),
            1f);
        toggleBg.type = Image.Type.Sliced;
        toggleButton.targetGraphic = toggleBg;
        toggleButton.onClick.AddListener(ToggleExpanded);
        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 1f);
        toggleRect.anchorMax = new Vector2(1f, 1f);
        toggleRect.pivot = new Vector2(1f, 1f);
        toggleRect.anchoredPosition = new Vector2(-12f, -8f);
        toggleRect.sizeDelta = new Vector2(84f, 24f);

        GameObject toggleLabelObj = new GameObject("Label");
        toggleLabelObj.transform.SetParent(toggleObj.transform, false);
        toggleHintText = toggleLabelObj.AddComponent<Text>();
        toggleHintText.font = UIFontProvider.Get();
        toggleHintText.fontSize = 12;
        toggleHintText.fontStyle = FontStyle.Bold;
        toggleHintText.alignment = TextAnchor.MiddleCenter;
        toggleHintText.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        toggleHintText.raycastTarget = false;
        RectTransform toggleLabelRect = toggleLabelObj.GetComponent<RectTransform>();
        toggleLabelRect.anchorMin = Vector2.zero;
        toggleLabelRect.anchorMax = Vector2.one;
        toggleLabelRect.offsetMin = Vector2.zero;
        toggleLabelRect.offsetMax = Vector2.zero;

        GameObject rowsRoot = new GameObject("Rows");
        rowsRoot.transform.SetParent(hudRoot.transform, false);
        RectTransform rowsRect = rowsRoot.AddComponent<RectTransform>();
        rowsRect.anchorMin = new Vector2(0f, 0f);
        rowsRect.anchorMax = new Vector2(1f, 1f);
        rowsRect.offsetMin = new Vector2(12f, FooterHeight + 6f);
        rowsRect.offsetMax = new Vector2(-12f, -(TitleHeight + 4f));

        for (int i = 0; i < ExpandedRowCount; i++)
        {
            rows.Add(CreateRow(rowsRoot.transform, i));
        }

        GameObject footerObj = new GameObject("Footer");
        footerObj.transform.SetParent(hudRoot.transform, false);
        footerText = footerObj.AddComponent<Text>();
        footerText.font = UIFontProvider.Get();
        footerText.fontSize = 13;
        footerText.alignment = TextAnchor.MiddleLeft;
        footerText.color = new Color(0.78f, 0.72f, 0.62f, 0.95f);
        footerText.raycastTarget = false;
        RectTransform footerRect = footerObj.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0f, 0f);
        footerRect.anchoredPosition = new Vector2(14f, 8f);
        footerRect.sizeDelta = new Vector2(-28f, 18f);

        toggleObj.transform.SetAsLastSibling();
        RefreshToggleHint();
        hudRoot.SetActive(false);
    }

    RowUi CreateRow(Transform parent, int index)
    {
        RowUi row = new RowUi();
        row.root = new GameObject("Row_" + (index + 1));
        row.root.transform.SetParent(parent, false);

        RectTransform rowRect = row.root.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, RowHeight);
        rowRect.anchoredPosition = new Vector2(0f, -index * RowHeight);

        Image rowBg = row.root.AddComponent<Image>();
        rowBg.color = new Color(1f, 1f, 1f, 0.04f);
        rowBg.raycastTarget = true;
        row.button = row.root.AddComponent<Button>();
        row.button.targetGraphic = rowBg;
        ColorBlock colors = row.button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.04f);
        colors.highlightedColor = new Color(1f, 0.92f, 0.72f, 0.14f);
        colors.pressedColor = new Color(1f, 0.85f, 0.55f, 0.22f);
        colors.selectedColor = colors.highlightedColor;
        row.button.colors = colors;

        GameObject rankObj = new GameObject("Rank");
        rankObj.transform.SetParent(row.root.transform, false);
        row.rankText = rankObj.AddComponent<Text>();
        row.rankText.font = UIFontProvider.Get();
        row.rankText.fontSize = 14;
        row.rankText.fontStyle = FontStyle.Bold;
        row.rankText.alignment = TextAnchor.MiddleLeft;
        row.rankText.color = new Color(1f, 0.92f, 0.72f, 1f);
        row.rankText.raycastTarget = false;
        RectTransform rankRect = rankObj.GetComponent<RectTransform>();
        rankRect.anchorMin = new Vector2(0f, 0f);
        rankRect.anchorMax = new Vector2(0f, 1f);
        rankRect.pivot = new Vector2(0f, 0.5f);
        rankRect.anchoredPosition = new Vector2(4f, 0f);
        rankRect.sizeDelta = new Vector2(24f, 0f);

        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(row.root.transform, false);
        row.nameText = nameObj.AddComponent<Text>();
        row.nameText.font = UIFontProvider.Get();
        row.nameText.fontSize = 13;
        row.nameText.alignment = TextAnchor.MiddleLeft;
        row.nameText.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        row.nameText.raycastTarget = false;
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(28f, 0f);
        nameRect.offsetMax = new Vector2(-72f, 0f);

        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(row.root.transform, false);
        row.valueText = valueObj.AddComponent<Text>();
        row.valueText.font = UIFontProvider.Get();
        row.valueText.fontSize = 13;
        row.valueText.fontStyle = FontStyle.Bold;
        row.valueText.alignment = TextAnchor.MiddleRight;
        row.valueText.color = new Color(1f, 0.92f, 0.72f, 1f);
        row.valueText.raycastTarget = false;
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(-4f, 0f);
        valueRect.sizeDelta = new Vector2(64f, 0f);

        GameObject barBgObj = new GameObject("BarBg");
        barBgObj.transform.SetParent(row.root.transform, false);
        Image barBg = barBgObj.AddComponent<Image>();
        barBg.sprite = WarmGaugeSprite.GetTrackInner(8);
        barBg.type = Image.Type.Sliced;
        barBg.color = Color.white;
        barBg.raycastTarget = false;
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 0f);
        barBgRect.anchorMax = new Vector2(1f, 0f);
        barBgRect.pivot = new Vector2(0.5f, 0f);
        barBgRect.anchoredPosition = new Vector2(0f, 2f);
        barBgRect.sizeDelta = new Vector2(-8f, 6f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barBgObj.transform, false);
        row.barFill = fillObj.AddComponent<Image>();
        row.barFill.sprite = WarmGaugeSprite.GetFill(WarmGaugeSprite.FillStyle.Gold);
        row.barFill.type = Image.Type.Simple;
        row.barFill.color = Color.white;
        row.barFill.raycastTarget = false;
        RectTransform fillRect = row.barFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        return row;
    }

    void ToggleExpanded()
    {
        expanded = !expanded;
        RefreshToggleHint();
        UpdateHudLayout();
        RefreshRows();
    }

    void RefreshToggleHint()
    {
        if (toggleHintText == null) return;
        toggleHintText.text = expanded
            ? GameLocalization.DamageMeterCollapse
            : GameLocalization.DamageMeterExpand;
    }

    void UpdateHudLayout()
    {
        if (hudRect == null) return;

        float height = expanded ? ExpandedHeight : CollapsedHeight;
        hudRect.sizeDelta = new Vector2(HudWidth, height);
        ApplyAnchoredPosition();
    }

    void ApplyAnchoredPosition()
    {
        if (hudRect == null) return;

        float topOffset = HudTopOffset;
        GameObject traitBar = GameObject.Find("TraitSkillBar");
        if (traitBar != null && traitBar.activeInHierarchy)
        {
            topOffset += TraitBarHeight + BelowTraitBarGap;
        }

        hudRect.anchoredPosition = new Vector2(-HudRightOffset, -topOffset);
    }

    void RefreshRows()
    {
        int displayCount = expanded ? ExpandedRowCount : 1;
        UnitDamageLedger.CopySortedEntries(entryBuffer, displayCount);

        for (int i = 0; i < rows.Count; i++)
        {
            RowUi row = rows[i];
            bool show = i < displayCount;
            if (row.root != null) row.root.SetActive(show);
            if (!show) continue;

            if (i < entryBuffer.Count)
            {
                BindRow(row, i + 1, entryBuffer[i]);
            }
            else
            {
                BindEmptyRow(row, i + 1);
            }
        }

        RefreshFooter();
    }

    void BindRow(RowUi row, int rank, UnitDamageLedger.Entry entry)
    {
        row.rankText.text = rank.ToString();
        row.nameText.text = UnitTraitData.GetDisplayName(entry.unitNumber);
        row.valueText.text = GameLocalization.DamageMeterPercent(entry.share * 100f);

        Vector2 anchorMax = row.barFill.rectTransform.anchorMax;
        anchorMax.x = Mathf.Clamp01(entry.share);
        row.barFill.rectTransform.anchorMax = anchorMax;

        Button btn = row.button;
        btn.onClick.RemoveAllListeners();
        int unitNumber = entry.unitNumber;
        btn.onClick.AddListener(() => OnRowClicked(unitNumber));
    }

    void BindEmptyRow(RowUi row, int rank)
    {
        row.rankText.text = rank.ToString();
        row.nameText.text = GameLocalization.DamageMeterEmptyRow;
        row.valueText.text = "—";
        Vector2 anchorMax = row.barFill.rectTransform.anchorMax;
        anchorMax.x = 0f;
        row.barFill.rectTransform.anchorMax = anchorMax;
        row.button.onClick.RemoveAllListeners();
    }

    void RefreshFooter()
    {
        if (footerText == null) return;
        int round = GameManager.Instance != null ? GameManager.Instance.currentRound : 0;
        footerText.text = GameLocalization.DamageMeterFooter(round, UnitDamageLedger.RoundTotalDamage);
    }

    void OnRowClicked(int unitNumber)
    {
        if (unitNumber <= 0) return;
        Character.PulseDamageMeterHighlightForUnit(unitNumber);
    }

    Transform GetHudParentTransform()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isRootCanvas) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;
            if (best == null || c.sortingOrder >= best.sortingOrder)
            {
                best = c;
            }
        }

        if (best != null) return best.transform;

        Canvas fallback = FindFirstObjectByType<Canvas>();
        if (fallback != null)
        {
            return fallback.rootCanvas != null ? fallback.rootCanvas.transform : fallback.transform;
        }

        return transform;
    }
}
