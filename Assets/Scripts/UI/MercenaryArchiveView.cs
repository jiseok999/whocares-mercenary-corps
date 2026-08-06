using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 — 용병단 아카이브 UI (웜톤 카드 레이아웃).
/// </summary>
public class MercenaryArchiveView : MonoBehaviour
{
    struct UnitCardRefs
    {
        public Button button;
        public Button favoriteButton;
        public Text favoriteStarText;
        public Image background;
        public Image border;
        public Image gradeStripe;
        public Image iconBg;
        public Image portraitImage;
        public Text nameText;
        public Text levelText;
    }

    struct SlotRowRefs
    {
        public Image rowBg;
        public Image accentDot;
        public Text labelText;
        public Text levelText;
        public Text costText;
        public Button upgradeButton;
        public Text upgradeButtonText;
        public Image upgradeButtonImage;
    }

    struct FilterChipRefs
    {
        public Button button;
        public Image background;
        public Text label;
    }

    static readonly Color Accent = new Color(1f, 0.78f, 0.30f, 1f);
    static readonly Color Cream = new Color(0.93f, 0.86f, 0.74f, 1f);
    static readonly Color Tan = new Color(0.80f, 0.66f, 0.48f, 1f);
    static readonly Color CardFill = new Color(0.145f, 0.108f, 0.078f, 0.99f);
    static readonly Color CardFillLight = new Color(0.175f, 0.132f, 0.095f, 0.98f);
    static readonly Color InsetFill = new Color(0.08f, 0.06f, 0.045f, 0.55f);

    GameObject panel;
    Text titleText;
    Text badgeText;
    Text medalsText;
    Text rosterSectionText;
    Text upgradeSectionText;
    Text previewSectionText;
    Text detailNameText;
    Text detailGradeText;
    Text detailLockedText;
    Text previewText;
    Image detailGradeBadge;
    SlotRowRefs attackRow;
    SlotRowRefs mobilityRow;
    SlotRowRefs specialtyRow;
    Transform gridContent;
    UnitCardRefs[] unitCards;
    FilterChipRefs[] filterChips;
    FilterChipRefs favoriteChip;
    FilterChipRefs[] detailTabChips;

    GameObject upgradeContent;
    GameObject codexContent;
    GameObject awakeningContent;
    Text codexText;
    Text awakeningLevelText;
    Text awakeningTierText;
    Text awakeningHintText;
    Text awakeningCostText;
    Button awakeningUpgradeButton;
    Text awakeningUpgradeButtonText;
    Image awakeningUpgradeButtonImage;

    int selectedUnit = 1;
    int gradeFilter;
    bool favoritesOnlyFilter;
    int detailTabIndex;

    public static MercenaryArchiveView Create(Transform parent)
    {
        GameObject rootObj = new GameObject("MercenaryArchiveView");
        rootObj.transform.SetParent(parent, false);
        MercenaryArchiveView view = rootObj.AddComponent<MercenaryArchiveView>();
        view.BuildUi();
        view.panel.SetActive(false);
        return view;
    }

    void BuildUi()
    {
        MetaCurrency.EnsureLoaded();
        UnitArchiveUnlockData.EnsureLoaded();
        CharacterUpgradeData.EnsureLoaded();
        UnitArchiveFavorites.EnsureLoaded();
        UnitArchiveAwakening.EnsureLoaded();

        panel = new GameObject("Panel");
        panel.transform.SetParent(transform, false);
        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0.06f, 0.04f, 0.025f, 0.88f);
        StretchRect(panel);

        Canvas overlay = panel.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 250;
        panel.AddComponent<GraphicRaycaster>();

        GameObject shadowObj = CreateSlicedImage(panel.transform, "MainShadow",
            WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.38f), 36, new Color(0f, 0f, 0f, 0f), 0f));
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(1336f, 836f);
        shadowRect.anchoredPosition = new Vector2(0f, -10f);

        GameObject mainCard = CreateSlicedImage(panel.transform, "MainCard",
            WarmRoundedSprite.Get(CardFill, 28, new Color(1f, 0.82f, 0.45f, 0.18f), 2.2f));
        RectTransform mainRect = mainCard.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.sizeDelta = new Vector2(1300f, 800f);
        mainRect.anchoredPosition = Vector2.zero;

        CreateHeader(mainCard.transform);
        CreateDivider(mainCard.transform, 92f, 1220f);

        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(mainCard.transform, false);
        RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(28f, 28f);
        bodyRect.offsetMax = new Vector2(-28f, -108f);

        CreateLeftPanel(bodyObj.transform);
        CreateDetailPanel(bodyObj.transform);
    }

    void CreateHeader(Transform parent)
    {
        GameObject accentObj = CreateSlicedImage(parent, "AccentBar",
            WarmRoundedSprite.Get(Accent, 4, new Color(0f, 0f, 0f, 0f), 0f));
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 1f);
        accentRect.anchoredPosition = new Vector2(36f, -28f);
        accentRect.sizeDelta = new Vector2(56f, 6f);

        GameObject badgeObj = CreateSlicedImage(parent, "Badge",
            WarmRoundedSprite.Get(new Color(1f, 0.78f, 0.30f, 0.14f), 12, new Color(1f, 0.78f, 0.30f, 0.55f), 1.4f));
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(36f, -44f);
        badgeRect.sizeDelta = new Vector2(118f, 28f);
        badgeText = CreateText(badgeObj.transform, "Text", GameLocalization.ArchiveBadge, 14, FontStyle.Bold,
            Accent, TextAnchor.MiddleCenter);
        StretchRect(badgeText.gameObject);

        titleText = CreateText(parent, "Title", GameLocalization.ArchiveTitle, 38, FontStyle.Bold, Accent, TextAnchor.UpperLeft);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(168f, -24f);
        titleRect.sizeDelta = new Vector2(520f, 52f);

        GameObject medalBadge = CreateSlicedImage(parent, "MedalBadge",
            WarmRoundedSprite.Get(new Color(0.03f, 0.10f, 0.24f, 0.55f), 18,
                new Color(0.45f, 0.80f, 1f, 0.45f), 1.6f));
        RectTransform medalRect = medalBadge.GetComponent<RectTransform>();
        medalRect.anchorMin = new Vector2(1f, 1f);
        medalRect.anchorMax = new Vector2(1f, 1f);
        medalRect.pivot = new Vector2(1f, 1f);
        medalRect.anchoredPosition = new Vector2(-68f, -30f);
        medalRect.sizeDelta = new Vector2(200f, 44f);
        medalsText = CreateText(medalBadge.transform, "Medals", GameLocalization.ArchiveMedalsFormat(MetaCurrency.GetMedals()),
            24, FontStyle.Bold, Cream, TextAnchor.MiddleCenter);
        StretchRect(medalsText.gameObject);

        Button closeBtn = CreateCloseXButton(parent, new Vector2(-24f, -24f));
        closeBtn.onClick.AddListener(Close);
    }

    void CreateLeftPanel(Transform parent)
    {
        GameObject leftCard = CreateSlicedImage(parent, "RosterCard",
            WarmRoundedSprite.Get(InsetFill, 20, new Color(1f, 0.85f, 0.55f, 0.08f), 1.5f));
        RectTransform leftRect = leftCard.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.44f, 1f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;

        rosterSectionText = CreateText(leftCard.transform, "Section", GameLocalization.ArchiveRosterSection, 18,
            FontStyle.Bold, Tan, TextAnchor.UpperLeft);
        RectTransform sectionRect = rosterSectionText.rectTransform;
        sectionRect.anchorMin = new Vector2(0f, 1f);
        sectionRect.anchorMax = new Vector2(1f, 1f);
        sectionRect.pivot = new Vector2(0f, 1f);
        sectionRect.anchoredPosition = new Vector2(18f, -12f);
        sectionRect.sizeDelta = new Vector2(-36f, 24f);

        filterChips = new FilterChipRefs[6];
        string[] filterLabels =
        {
            GameLocalization.ArchiveFilterAll,
            GameLocalization.ArchiveGradeFilterFormat(5),
            GameLocalization.ArchiveGradeFilterFormat(4),
            GameLocalization.ArchiveGradeFilterFormat(3),
            GameLocalization.ArchiveGradeFilterFormat(2),
            GameLocalization.ArchiveGradeFilterFormat(1)
        };
        const float filterY = -42f;
        const float chipGap = 5f;
        float filterX = 14f;
        for (int i = 0; i < filterLabels.Length; i++)
        {
            int captured = i;
            float chipW = i == 0 ? 48f : 54f;
            filterChips[i] = CreateFilterChip(leftCard.transform, $"Filter_{i}", filterLabels[i],
                new Vector2(filterX, filterY), new Vector2(chipW, 28f), 13);
            filterChips[i].button.onClick.AddListener(() => SetGradeFilter(captured));
            filterX += chipW + chipGap;
        }

        favoriteChip = CreateFilterChip(leftCard.transform, "Filter_Favorites",
            GameLocalization.ArchiveFilterFavorites, new Vector2(14f, -76f), new Vector2(92f, 28f), 13);
        favoriteChip.button.onClick.AddListener(ToggleFavoritesFilter);

        GameObject scrollFrame = CreateSlicedImage(leftCard.transform, "ScrollFrame",
            WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.22f), 16, new Color(1f, 0.85f, 0.55f, 0.06f), 1f));
        RectTransform scrollFrameRect = scrollFrame.GetComponent<RectTransform>();
        scrollFrameRect.anchorMin = Vector2.zero;
        scrollFrameRect.anchorMax = Vector2.one;
        scrollFrameRect.offsetMin = new Vector2(12f, 12f);
        scrollFrameRect.offsetMax = new Vector2(-12f, -76f);

        GameObject scrollObj = new GameObject("UnitScroll");
        scrollObj.transform.SetParent(scrollFrame.transform, false);
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0f);
        StretchRect(scrollObj);

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        StretchRect(viewportObj);

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        gridContent = contentObj.transform;

        scroll.viewport = viewportObj.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        unitCards = new UnitCardRefs[28];
        const int columns = 2;
        const float cardW = 248f;
        const float cardH = 88f;
        const float gap = 10f;
        int layoutIndex = 0;
        for (int unitNumber = 1; unitNumber <= 28; unitNumber++)
        {
            if (!UnitArchiveRosterRules.IsListedInRoster(unitNumber)) continue;

            int row = layoutIndex / columns;
            int col = layoutIndex % columns;
            unitCards[unitNumber - 1] = CreateUnitCard(contentObj.transform, unitNumber,
                col * (cardW + gap), -row * (cardH + gap), cardW, cardH);
            layoutIndex++;
        }
        contentRect.sizeDelta = new Vector2(0f, Mathf.Ceil(layoutIndex / (float)columns) * (cardH + gap) + 16f);
    }

    UnitCardRefs CreateUnitCard(Transform parent, int unitNumber, float x, float y, float width, float height)
    {
        GameObject cardObj = new GameObject($"UnitCard_{unitNumber}");
        cardObj.transform.SetParent(parent, false);
        RectTransform rect = cardObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x + 10f, y - 8f);
        rect.sizeDelta = new Vector2(width, height);

        Image bg = cardObj.AddComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.sprite = WarmRoundedSprite.Get(CardFillLight, 14, new Color(1f, 0.85f, 0.55f, 0.10f), 1.2f);

        Image border = CreateSlicedImage(cardObj.transform, "Border",
            WarmRoundedSprite.Get(new Color(1f, 0.78f, 0.30f, 0f), 14, new Color(1f, 0.78f, 0.30f, 0.75f), 2f))
            .GetComponent<Image>();
        RectTransform borderRect = border.rectTransform;
        StretchRect(border.gameObject);
        border.raycastTarget = false;

        GameObject stripeObj = CreateSlicedImage(cardObj.transform, "GradeStripe",
            WarmRoundedSprite.Get(GetGradeColor(UnitCombatStats.GetGradeForUnit(unitNumber)), 6,
                new Color(1f, 1f, 1f, 0.18f), 0.8f));
        RectTransform stripeRect = stripeObj.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.anchoredPosition = new Vector2(8f, 0f);
        stripeRect.sizeDelta = new Vector2(5f, -16f);
        Image gradeStripe = stripeObj.GetComponent<Image>();
        gradeStripe.raycastTarget = false;

        GameObject iconObj = CreateSlicedImage(cardObj.transform, "Icon",
            WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.28f), 12, new Color(1f, 1f, 1f, 0.08f), 1f));
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(20f, 0f);
        iconRect.sizeDelta = new Vector2(52f, 52f);
        Image iconBg = iconObj.GetComponent<Image>();
        iconBg.raycastTarget = false;
        Mask iconMask = iconObj.AddComponent<Mask>();
        iconMask.showMaskGraphic = true;

        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(iconObj.transform, false);
        Image portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.color = Color.white;
        portraitImage.preserveAspect = true;
        portraitImage.type = Image.Type.Simple;
        portraitImage.raycastTarget = false;
        portraitImage.enabled = false;
        RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.5f, 1f);
        portraitRect.anchorMax = new Vector2(0.5f, 1f);
        portraitRect.pivot = new Vector2(0.5f, 1f);
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = new Vector2(52f, 52f);

        Button btn = cardObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.fadeDuration = 0.06f;
        btn.colors = colors;
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => SelectUnit(unitNumber));

        Text nameText = CreateText(cardObj.transform, "Name", UnitTraitData.GetDisplayName(unitNumber), 20, FontStyle.Bold,
            UnitTraitData.GetDisplayNameColor(unitNumber), TextAnchor.UpperLeft);
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(82f, -12f);
        nameRect.sizeDelta = new Vector2(-92f, 28f);

        Text levelText = CreateText(cardObj.transform, "Levels", string.Empty, 15, FontStyle.Normal,
            Tan, TextAnchor.LowerLeft);
        RectTransform levelRect = levelText.rectTransform;
        levelRect.anchorMin = new Vector2(0f, 0f);
        levelRect.anchorMax = new Vector2(1f, 0f);
        levelRect.pivot = new Vector2(0f, 0f);
        levelRect.anchoredPosition = new Vector2(82f, 10f);
        levelRect.sizeDelta = new Vector2(-110f, 22f);

        GameObject favObj = new GameObject("Favorite");
        favObj.transform.SetParent(cardObj.transform, false);
        RectTransform favRect = favObj.AddComponent<RectTransform>();
        favRect.anchorMin = new Vector2(1f, 1f);
        favRect.anchorMax = new Vector2(1f, 1f);
        favRect.pivot = new Vector2(1f, 1f);
        favRect.anchoredPosition = new Vector2(-8f, -8f);
        favRect.sizeDelta = new Vector2(28f, 28f);
        Image favBg = favObj.AddComponent<Image>();
        favBg.color = new Color(0f, 0f, 0f, 0f);
        Button favoriteButton = favObj.AddComponent<Button>();
        favoriteButton.transition = Selectable.Transition.ColorTint;
        ColorBlock favColors = favoriteButton.colors;
        favColors.normalColor = Color.white;
        favColors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        favColors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        favColors.fadeDuration = 0.05f;
        favoriteButton.colors = favColors;
        int capturedUnit = unitNumber;
        favoriteButton.onClick.AddListener(() => ToggleUnitFavorite(capturedUnit));

        Text favText = CreateText(favObj.transform, "Icon", "☆", 20, FontStyle.Bold,
            new Color(0.72f, 0.66f, 0.58f, 0.85f), TextAnchor.MiddleCenter);
        StretchRect(favText.gameObject);
        favText.raycastTarget = false;

        return new UnitCardRefs
        {
            button = btn,
            favoriteButton = favoriteButton,
            favoriteStarText = favText,
            background = bg,
            border = border,
            gradeStripe = gradeStripe,
            iconBg = iconBg,
            portraitImage = portraitImage,
            nameText = nameText,
            levelText = levelText
        };
    }

    void CreateDetailPanel(Transform parent)
    {
        GameObject detailCard = CreateSlicedImage(parent, "DetailCard",
            WarmRoundedSprite.Get(InsetFill, 20, new Color(1f, 0.85f, 0.55f, 0.08f), 1.5f));
        RectTransform detailRect = detailCard.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.46f, 0f);
        detailRect.anchorMax = new Vector2(1f, 1f);
        detailRect.offsetMin = Vector2.zero;
        detailRect.offsetMax = Vector2.zero;

        detailNameText = CreateText(detailCard.transform, "DetailName", string.Empty, 34, FontStyle.Bold,
            Accent, TextAnchor.UpperLeft);
        RectTransform nameRect = detailNameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(22f, -16f);
        nameRect.sizeDelta = new Vector2(-180f, 44f);

        GameObject gradeBadgeObj = CreateSlicedImage(detailCard.transform, "GradeBadge",
            WarmRoundedSprite.Get(new Color(1f, 0.78f, 0.30f, 0.12f), 12, new Color(1f, 0.78f, 0.30f, 0.42f), 1.2f));
        RectTransform gradeBadgeRect = gradeBadgeObj.GetComponent<RectTransform>();
        gradeBadgeRect.anchorMin = new Vector2(1f, 1f);
        gradeBadgeRect.anchorMax = new Vector2(1f, 1f);
        gradeBadgeRect.pivot = new Vector2(1f, 1f);
        gradeBadgeRect.anchoredPosition = new Vector2(-18f, -18f);
        gradeBadgeRect.sizeDelta = new Vector2(118f, 32f);
        detailGradeBadge = gradeBadgeObj.GetComponent<Image>();
        detailGradeText = CreateText(gradeBadgeObj.transform, "Text", string.Empty, 17, FontStyle.Bold,
            Accent, TextAnchor.MiddleCenter);
        StretchRect(detailGradeText.gameObject);

        detailLockedText = CreateText(detailCard.transform, "LockedHint", string.Empty, 18, FontStyle.Italic,
            Tan, TextAnchor.UpperLeft);
        RectTransform lockedRect = detailLockedText.rectTransform;
        lockedRect.anchorMin = new Vector2(0f, 1f);
        lockedRect.anchorMax = new Vector2(1f, 1f);
        lockedRect.pivot = new Vector2(0f, 1f);
        lockedRect.anchoredPosition = new Vector2(22f, -62f);
        lockedRect.sizeDelta = new Vector2(-44f, 48f);

        detailTabChips = new FilterChipRefs[3];
        string[] tabLabels =
        {
            GameLocalization.ArchiveTabUpgrade,
            GameLocalization.ArchiveTabCodex,
            GameLocalization.ArchiveTabAwakening
        };
        float tabX = 18f;
        for (int i = 0; i < tabLabels.Length; i++)
        {
            int capturedTab = i;
            float tabW = i == 1 ? 72f : 78f;
            detailTabChips[i] = CreateFilterChip(detailCard.transform, $"DetailTab_{i}", tabLabels[i],
                new Vector2(tabX, -118f), new Vector2(tabW, 30f));
            detailTabChips[i].button.onClick.AddListener(() => SetDetailTab(capturedTab));
            tabX += tabW + 8f;
        }

        upgradeContent = new GameObject("UpgradeContent");
        upgradeContent.transform.SetParent(detailCard.transform, false);
        StretchRect(upgradeContent);

        upgradeSectionText = CreateText(upgradeContent.transform, "UpgradeSection", GameLocalization.ArchiveUpgradeSection,
            18, FontStyle.Bold, Tan, TextAnchor.UpperLeft);
        RectTransform upgradeSectionRect = upgradeSectionText.rectTransform;
        upgradeSectionRect.anchorMin = new Vector2(0f, 1f);
        upgradeSectionRect.anchorMax = new Vector2(1f, 1f);
        upgradeSectionRect.pivot = new Vector2(0f, 1f);
        upgradeSectionRect.anchoredPosition = new Vector2(22f, -156f);
        upgradeSectionRect.sizeDelta = new Vector2(-44f, 24f);

        attackRow = CreateSlotRow(upgradeContent.transform, "AttackRow", GameLocalization.ArchiveAttackSlot,
            new Color(1f, 0.55f, 0.28f, 1f), -192f, () => TryUpgrade(CharacterUpgradeData.UpgradeSlot.Attack));
        mobilityRow = CreateSlotRow(upgradeContent.transform, "MobilityRow", GameLocalization.ArchiveMobilitySlot,
            new Color(0.45f, 0.78f, 1f, 1f), -272f, () => TryUpgrade(CharacterUpgradeData.UpgradeSlot.Mobility));
        specialtyRow = CreateSlotRow(upgradeContent.transform, "SpecialtyRow", string.Empty,
            new Color(0.55f, 0.92f, 0.62f, 1f), -352f, () => TryUpgrade(CharacterUpgradeData.UpgradeSlot.Specialty));

        GameObject previewBox = CreateSlicedImage(upgradeContent.transform, "PreviewBox",
            WarmRoundedSprite.Get(new Color(1f, 0.78f, 0.30f, 0.08f), 16, new Color(1f, 0.78f, 0.30f, 0.28f), 1.4f));
        RectTransform previewBoxRect = previewBox.GetComponent<RectTransform>();
        previewBoxRect.anchorMin = new Vector2(0f, 0f);
        previewBoxRect.anchorMax = new Vector2(1f, 0f);
        previewBoxRect.pivot = new Vector2(0.5f, 0f);
        previewBoxRect.anchoredPosition = new Vector2(0f, 16f);
        previewBoxRect.sizeDelta = new Vector2(-36f, 132f);

        previewSectionText = CreateText(previewBox.transform, "PreviewSection", GameLocalization.ArchivePreviewSection,
            16, FontStyle.Bold, new Color(1f, 0.88f, 0.62f, 1f), TextAnchor.UpperLeft);
        RectTransform previewSectionRect = previewSectionText.rectTransform;
        previewSectionRect.anchorMin = new Vector2(0f, 1f);
        previewSectionRect.anchorMax = new Vector2(1f, 1f);
        previewSectionRect.pivot = new Vector2(0f, 1f);
        previewSectionRect.anchoredPosition = new Vector2(16f, -10f);
        previewSectionRect.sizeDelta = new Vector2(-32f, 22f);

        previewText = CreateText(previewBox.transform, "Preview", string.Empty, 19, FontStyle.Normal,
            Cream, TextAnchor.UpperLeft);
        previewText.lineSpacing = 1.25f;
        RectTransform previewRect = previewText.rectTransform;
        previewRect.anchorMin = new Vector2(0f, 0f);
        previewRect.anchorMax = new Vector2(1f, 1f);
        previewRect.offsetMin = new Vector2(16f, 12f);
        previewRect.offsetMax = new Vector2(-16f, -36f);

        codexContent = CreateSlicedImage(detailCard.transform, "CodexContent",
            WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.18f), 16, new Color(1f, 0.85f, 0.55f, 0.08f), 1f));
        RectTransform codexRect = codexContent.GetComponent<RectTransform>();
        codexRect.anchorMin = new Vector2(0f, 0f);
        codexRect.anchorMax = new Vector2(1f, 1f);
        codexRect.offsetMin = new Vector2(18f, 16f);
        codexRect.offsetMax = new Vector2(-18f, -156f);
        codexText = CreateText(codexContent.transform, "CodexText", string.Empty, 18, FontStyle.Normal,
            Cream, TextAnchor.UpperLeft);
        codexText.lineSpacing = 1.2f;
        RectTransform codexTextRect = codexText.rectTransform;
        codexTextRect.anchorMin = Vector2.zero;
        codexTextRect.anchorMax = Vector2.one;
        codexTextRect.offsetMin = new Vector2(16f, 12f);
        codexTextRect.offsetMax = new Vector2(-16f, -12f);

        awakeningContent = CreateSlicedImage(detailCard.transform, "AwakeningContent",
            WarmRoundedSprite.Get(CardFillLight, 16, new Color(1f, 0.85f, 0.55f, 0.10f), 1.2f));
        RectTransform awakeningRect = awakeningContent.GetComponent<RectTransform>();
        awakeningRect.anchorMin = new Vector2(0f, 0f);
        awakeningRect.anchorMax = new Vector2(1f, 1f);
        awakeningRect.offsetMin = new Vector2(18f, 16f);
        awakeningRect.offsetMax = new Vector2(-18f, -156f);

        Text awakeningSection = CreateText(awakeningContent.transform, "Section", GameLocalization.ArchiveAwakeningSection,
            18, FontStyle.Bold, Tan, TextAnchor.UpperLeft);
        RectTransform awakeningSectionRect = awakeningSection.rectTransform;
        awakeningSectionRect.anchorMin = new Vector2(0f, 1f);
        awakeningSectionRect.anchorMax = new Vector2(1f, 1f);
        awakeningSectionRect.pivot = new Vector2(0f, 1f);
        awakeningSectionRect.anchoredPosition = new Vector2(16f, -10f);
        awakeningSectionRect.sizeDelta = new Vector2(-32f, 24f);

        awakeningLevelText = CreateText(awakeningContent.transform, "Level", string.Empty, 20, FontStyle.Bold,
            Accent, TextAnchor.UpperLeft);
        RectTransform awakeningLevelRect = awakeningLevelText.rectTransform;
        awakeningLevelRect.anchorMin = new Vector2(0f, 1f);
        awakeningLevelRect.anchorMax = new Vector2(1f, 1f);
        awakeningLevelRect.pivot = new Vector2(0f, 1f);
        awakeningLevelRect.anchoredPosition = new Vector2(16f, -38f);
        awakeningLevelRect.sizeDelta = new Vector2(-32f, 28f);

        awakeningTierText = CreateText(awakeningContent.transform, "TierDesc", string.Empty, 17, FontStyle.Normal,
            Cream, TextAnchor.UpperLeft);
        awakeningTierText.lineSpacing = 1.25f;
        RectTransform awakeningTierRect = awakeningTierText.rectTransform;
        awakeningTierRect.anchorMin = new Vector2(0f, 1f);
        awakeningTierRect.anchorMax = new Vector2(1f, 1f);
        awakeningTierRect.pivot = new Vector2(0f, 1f);
        awakeningTierRect.anchoredPosition = new Vector2(16f, -68f);
        awakeningTierRect.sizeDelta = new Vector2(-32f, 220f);

        awakeningHintText = CreateText(awakeningContent.transform, "Hint", string.Empty, 16, FontStyle.Italic,
            Tan, TextAnchor.UpperLeft);
        RectTransform awakeningHintRect = awakeningHintText.rectTransform;
        awakeningHintRect.anchorMin = new Vector2(0f, 0f);
        awakeningHintRect.anchorMax = new Vector2(1f, 0f);
        awakeningHintRect.pivot = new Vector2(0f, 0f);
        awakeningHintRect.anchoredPosition = new Vector2(16f, 58f);
        awakeningHintRect.sizeDelta = new Vector2(-32f, 48f);

        awakeningCostText = CreateText(awakeningContent.transform, "Cost", string.Empty, 17, FontStyle.Normal,
            Tan, TextAnchor.MiddleLeft);
        RectTransform awakeningCostRect = awakeningCostText.rectTransform;
        awakeningCostRect.anchorMin = new Vector2(0f, 0f);
        awakeningCostRect.anchorMax = new Vector2(0f, 0f);
        awakeningCostRect.pivot = new Vector2(0f, 0f);
        awakeningCostRect.anchoredPosition = new Vector2(16f, 14f);
        awakeningCostRect.sizeDelta = new Vector2(220f, 28f);

        awakeningUpgradeButton = CreatePrimaryButton(awakeningContent.transform, "AwakeningUpgrade",
            GameLocalization.ArchiveUpgrade, new Vector2(-16f, 14f), new Vector2(120f, 42f),
            out awakeningUpgradeButtonText, out awakeningUpgradeButtonImage);
        RectTransform awakeningBtnRect = awakeningUpgradeButton.GetComponent<RectTransform>();
        awakeningBtnRect.anchorMin = new Vector2(1f, 0f);
        awakeningBtnRect.anchorMax = new Vector2(1f, 0f);
        awakeningBtnRect.pivot = new Vector2(1f, 0f);
        awakeningUpgradeButton.onClick.AddListener(TryAwakeningUpgrade);
    }

    SlotRowRefs CreateSlotRow(Transform parent, string name, string label, Color dotColor, float topY, System.Action onUpgrade)
    {
        GameObject rowObj = CreateSlicedImage(parent, name,
            WarmRoundedSprite.Get(CardFillLight, 14, new Color(1f, 0.85f, 0.55f, 0.10f), 1.2f));
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = new Vector2(18f, topY);
        rowRect.sizeDelta = new Vector2(-36f, 68f);
        Image rowBg = rowObj.GetComponent<Image>();

        GameObject dotObj = CreateSlicedImage(rowObj.transform, "Dot",
            WarmRoundedSprite.Get(dotColor, 8, new Color(1f, 1f, 1f, 0.25f), 1f));
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0f, 0.5f);
        dotRect.anchorMax = new Vector2(0f, 0.5f);
        dotRect.pivot = new Vector2(0f, 0.5f);
        dotRect.anchoredPosition = new Vector2(14f, 0f);
        dotRect.sizeDelta = new Vector2(14f, 14f);
        Image accentDot = dotObj.GetComponent<Image>();
        accentDot.raycastTarget = false;

        Text labelText = CreateText(rowObj.transform, "Label", label, 22, FontStyle.Bold, Cream, TextAnchor.MiddleLeft);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(38f, 8f);
        labelRect.sizeDelta = new Vector2(72f, 30f);

        Text levelText = CreateText(rowObj.transform, "Level", string.Empty, 18, FontStyle.Normal,
            Tan, TextAnchor.MiddleLeft);
        RectTransform levelRect = levelText.rectTransform;
        levelRect.anchorMin = new Vector2(0f, 0.5f);
        levelRect.anchorMax = new Vector2(0f, 0.5f);
        levelRect.pivot = new Vector2(0f, 0.5f);
        levelRect.anchoredPosition = new Vector2(38f, -14f);
        levelRect.sizeDelta = new Vector2(120f, 24f);

        Text costText = CreateText(rowObj.transform, "Cost", string.Empty, 17, FontStyle.Normal,
            new Color(0.72f, 0.66f, 0.58f, 1f), TextAnchor.MiddleLeft);
        RectTransform costRect = costText.rectTransform;
        costRect.anchorMin = new Vector2(0f, 0.5f);
        costRect.anchorMax = new Vector2(0f, 0.5f);
        costRect.pivot = new Vector2(0f, 0.5f);
        costRect.anchoredPosition = new Vector2(168f, 0f);
        costRect.sizeDelta = new Vector2(180f, 28f);

        Button upgradeBtn = CreatePrimaryButton(rowObj.transform, "Upgrade", GameLocalization.ArchiveUpgrade,
            new Vector2(-12f, 0f), new Vector2(108f, 42f), out Text upgradeButtonText, out Image upgradeButtonImage);
        RectTransform upgradeRect = upgradeBtn.GetComponent<RectTransform>();
        upgradeRect.anchorMin = new Vector2(1f, 0.5f);
        upgradeRect.anchorMax = new Vector2(1f, 0.5f);
        upgradeRect.pivot = new Vector2(1f, 0.5f);
        upgradeBtn.onClick.AddListener(() => onUpgrade?.Invoke());

        return new SlotRowRefs
        {
            rowBg = rowBg,
            accentDot = accentDot,
            labelText = labelText,
            levelText = levelText,
            costText = costText,
            upgradeButton = upgradeBtn,
            upgradeButtonText = upgradeButtonText,
            upgradeButtonImage = upgradeButtonImage
        };
    }

    static GameObject CreateSlicedImage(Transform parent, string name, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.sprite = sprite;
        image.color = Color.white;
        return obj;
    }

    static void CreateDivider(Transform parent, float topY, float width)
    {
        GameObject obj = new GameObject("Divider");
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(1f, 0.85f, 0.55f, 0.12f);
        img.raycastTarget = false;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topY);
        rect.sizeDelta = new Vector2(width, 2f);
    }

    static void StretchRect(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    FilterChipRefs CreateFilterChip(Transform parent, string name, string label, Vector2 pos, Vector2 size, int fontSize = 15)
    {
        GameObject chipObj = new GameObject(name);
        chipObj.transform.SetParent(parent, false);
        RectTransform rect = chipObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image bg = chipObj.AddComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.sprite = WarmRoundedSprite.Get(
            new Color(0.03f, 0.10f, 0.24f, 0.38f), 14,
            new Color(0.45f, 0.80f, 1f, 0.45f), 1.4f);

        Button button = chipObj.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        Text labelText = CreateText(chipObj.transform, "Text", label, fontSize, FontStyle.Bold,
            new Color(0.88f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter);
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        StretchRect(labelText.gameObject);

        return new FilterChipRefs { button = button, background = bg, label = labelText };
    }

    static Button CreateCloseXButton(Transform parent, Vector2 anchoredPosition)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent, false);
        Image closeImage = closeObj.AddComponent<Image>();
        closeImage.type = Image.Type.Sliced;
        closeImage.sprite = WarmRoundedSprite.Get(
            new Color(0.03f, 0.10f, 0.24f, 0.55f), 14,
            new Color(0.45f, 0.80f, 1f, 0.58f), 1.8f);
        Button closeButton = closeObj.AddComponent<Button>();
        closeButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = closeButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.fadeDuration = 0.07f;
        closeButton.colors = colors;

        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = anchoredPosition;
        closeRect.sizeDelta = new Vector2(44f, 44f);

        Text closeText = CreateText(closeObj.transform, "X", "✕", 26, FontStyle.Bold,
            new Color(0.88f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter);
        closeText.raycastTarget = false;
        StretchRect(closeText.gameObject);
        return closeButton;
    }

    static Button CreatePrimaryButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size,
        out Text labelText, out Image buttonImage)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        buttonImage = btnObj.AddComponent<Image>();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.sprite = WarmRoundedSprite.Get(new Color(0.95f, 0.60f, 0.16f, 1f), 12,
            new Color(1f, 0.88f, 0.42f, 0.55f), 1.6f);
        Button button = btnObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        colors.fadeDuration = 0.07f;
        button.colors = colors;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;

        labelText = CreateText(btnObj.transform, "Text", label, 20, FontStyle.Bold,
            new Color(1f, 0.98f, 0.93f, 1f), TextAnchor.MiddleCenter);
        StretchRect(labelText.gameObject);
        return button;
    }

    static Button CreateSecondaryButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size,
        out Text labelText, out Image buttonImage)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        buttonImage = btnObj.AddComponent<Image>();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.sprite = WarmRoundedSprite.Get(
            new Color(0.03f, 0.10f, 0.24f, 0.38f), 14,
            new Color(0.45f, 0.80f, 1f, 0.58f), 1.8f);
        Button button = btnObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.fadeDuration = 0.07f;
        button.colors = colors;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;

        labelText = CreateText(btnObj.transform, "Text", label, 20, FontStyle.Bold,
            new Color(0.88f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter);
        StretchRect(labelText.gameObject);
        return button;
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    static Color GetGradeColor(int grade)
    {
        string hex = UnitCombatStats.GetGradeColorHex(grade);
        return ColorUtility.TryParseHtmlString($"#{hex}", out Color color) ? color : Accent;
    }

    public void Open()
    {
        MetaCurrency.Reload();
        UnitArchiveUnlockData.Reload();
        CharacterUpgradeData.Reload();
        UnitArchiveFavorites.Reload();
        UnitArchiveAwakening.Reload();
        UnitArchivePortraitProvider.ClearCache();
        UnitArchiveRosterRules.EnsureListedSelection(ref selectedUnit);
        transform.SetAsLastSibling();
        panel.SetActive(true);
        RefreshUi();
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void RefreshLocalizedTexts()
    {
        if (titleText != null) titleText.text = GameLocalization.ArchiveTitle;
        if (badgeText != null) badgeText.text = GameLocalization.ArchiveBadge;
        if (rosterSectionText != null) rosterSectionText.text = GameLocalization.ArchiveRosterSection;
        if (upgradeSectionText != null) upgradeSectionText.text = GameLocalization.ArchiveUpgradeSection;
        if (previewSectionText != null) previewSectionText.text = GameLocalization.ArchivePreviewSection;
        if (attackRow.labelText != null) attackRow.labelText.text = GameLocalization.ArchiveAttackSlot;
        if (mobilityRow.labelText != null) mobilityRow.labelText.text = GameLocalization.ArchiveMobilitySlot;
        if (attackRow.upgradeButtonText != null) attackRow.upgradeButtonText.text = GameLocalization.ArchiveUpgrade;
        if (mobilityRow.upgradeButtonText != null) mobilityRow.upgradeButtonText.text = GameLocalization.ArchiveUpgrade;
        if (specialtyRow.upgradeButtonText != null) specialtyRow.upgradeButtonText.text = GameLocalization.ArchiveUpgrade;
        if (filterChips != null && filterChips.Length >= 6)
        {
            SetChipLabel(filterChips[0], GameLocalization.ArchiveFilterAll);
            for (int grade = 5; grade >= 1; grade--)
            {
                SetChipLabel(filterChips[6 - grade], GameLocalization.ArchiveGradeFilterFormat(grade));
            }
        }
        if (favoriteChip.label != null) favoriteChip.label.text = GameLocalization.ArchiveFilterFavorites;
        if (detailTabChips != null && detailTabChips.Length >= 3)
        {
            SetChipLabel(detailTabChips[0], GameLocalization.ArchiveTabUpgrade);
            SetChipLabel(detailTabChips[1], GameLocalization.ArchiveTabCodex);
            SetChipLabel(detailTabChips[2], GameLocalization.ArchiveTabAwakening);
        }
        if (awakeningUpgradeButtonText != null) awakeningUpgradeButtonText.text = GameLocalization.ArchiveUpgrade;
        RefreshUi();
    }

    static void SetChipLabel(FilterChipRefs chip, string label)
    {
        if (chip.label != null) chip.label.text = label;
    }

    void SetGradeFilter(int filterIndex)
    {
        gradeFilter = filterIndex;
        RefreshUi();
    }

    void ToggleFavoritesFilter()
    {
        favoritesOnlyFilter = !favoritesOnlyFilter;
        RefreshUi();
    }

    void SetDetailTab(int tabIndex)
    {
        detailTabIndex = tabIndex;
        RefreshUi();
    }

    void ToggleUnitFavorite(int unitNumber)
    {
        UnitArchiveFavorites.Toggle(unitNumber);
        RefreshUi();
    }

    void TryAwakeningUpgrade()
    {
        if (!UnitArchiveUnlockData.IsUnlocked(selectedUnit)) return;
        if (!UnitArchiveAwakening.TryUpgrade(selectedUnit)) return;
        RefreshUi();
    }

    void SelectUnit(int unitNumber)
    {
        if (!UnitArchiveRosterRules.IsListedInRoster(unitNumber)) return;
        selectedUnit = unitNumber;
        RefreshUi();
    }

    void TryUpgrade(CharacterUpgradeData.UpgradeSlot slot)
    {
        if (!UnitArchiveUnlockData.IsUnlocked(selectedUnit)) return;
        if (!CharacterUpgradeData.TryUpgrade(selectedUnit, slot)) return;
        RefreshUi();
    }

    void RefreshUi()
    {
        UnitArchiveRosterRules.EnsureListedSelection(ref selectedUnit);

        if (medalsText != null)
        {
            medalsText.text = GameLocalization.ArchiveMedalsFormat(MetaCurrency.GetMedals());
        }

        RefreshFilterChips();
        RefreshDetailTabChips();

        for (int i = 0; i < 28; i++)
        {
            int unitNumber = i + 1;
            UnitCardRefs card = unitCards[i];
            if (card.button == null) continue;
            if (!UnitArchiveRosterRules.IsListedInRoster(unitNumber))
            {
                card.button.gameObject.SetActive(false);
                continue;
            }

            bool discovered = UnitArchiveUnlockData.IsDiscovered(unitNumber);
            bool upgradeUnlocked = UnitArchiveUnlockData.IsUnlocked(unitNumber);
            int grade = UnitCombatStats.GetGradeForUnit(unitNumber);
            bool gradeMatch = gradeFilter == 0 || gradeFilter == (6 - grade);
            bool favoriteMatch = !favoritesOnlyFilter || UnitArchiveFavorites.IsFavorite(unitNumber);
            bool visible = gradeMatch && favoriteMatch;
            card.button.gameObject.SetActive(visible);
            if (!visible) continue;

            bool selected = unitNumber == selectedUnit;
            Color gradeColor = GetGradeColor(grade);

            if (card.gradeStripe != null)
            {
                card.gradeStripe.color = discovered ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                card.gradeStripe.sprite = WarmRoundedSprite.Get(
                    discovered ? gradeColor : new Color(0.35f, 0.33f, 0.30f, 1f), 6,
                    new Color(1f, 1f, 1f, 0.18f), 0.8f);
            }

            if (card.iconBg != null)
            {
                card.iconBg.sprite = WarmRoundedSprite.Get(
                    discovered
                        ? new Color(0f, 0f, 0f, 0.35f)
                        : new Color(0f, 0f, 0f, 0.32f),
                    12, new Color(1f, 1f, 1f, discovered ? 0.12f : 0.05f), 1f);
            }

            RefreshUnitPortrait(card, unitNumber, discovered, gradeColor);

            if (discovered)
            {
                card.nameText.text = UnitTraitData.GetDisplayName(unitNumber);
                card.nameText.color = upgradeUnlocked
                    ? UnitTraitData.GetDisplayNameColor(unitNumber)
                    : new Color(0.62f, 0.58f, 0.52f, 1f);
                card.levelText.text = upgradeUnlocked
                    ? UnitArchiveUiText.FormatCardLevels(unitNumber)
                    : UnitArchiveUiText.GetUnlockHint(unitNumber);
                card.levelText.color = upgradeUnlocked ? Tan : new Color(0.58f, 0.52f, 0.46f, 1f);
            }
            else
            {
                card.nameText.text = "???";
                card.nameText.color = new Color(0.45f, 0.42f, 0.38f, 1f);
                card.levelText.text = GameLocalization.ArchiveLockedHint;
                card.levelText.color = new Color(0.58f, 0.52f, 0.46f, 1f);
            }

            if (card.background != null)
            {
                card.background.sprite = WarmRoundedSprite.Get(
                    selected ? new Color(0.22f, 0.17f, 0.11f, 0.98f) : CardFillLight,
                    14,
                    selected ? new Color(1f, 0.78f, 0.30f, 0.28f) : new Color(1f, 0.85f, 0.55f, 0.10f),
                    selected ? 1.8f : 1.2f);
            }

            if (card.border != null)
            {
                card.border.enabled = selected;
            }

            if (card.favoriteStarText != null)
            {
                bool isFavorite = UnitArchiveFavorites.IsFavorite(unitNumber);
                card.favoriteStarText.text = isFavorite ? "★" : "☆";
                card.favoriteStarText.color = isFavorite
                    ? Accent
                    : new Color(0.72f, 0.66f, 0.58f, discovered ? 0.85f : 0.45f);
            }

            if (card.favoriteButton != null)
            {
                card.favoriteButton.gameObject.SetActive(discovered);
            }
        }

        RefreshDetailPanel();
    }

    void RefreshFilterChips()
    {
        RefreshChipGroup(filterChips, gradeFilter, false);
        if (favoriteChip.background != null)
        {
            bool active = favoritesOnlyFilter;
            favoriteChip.background.sprite = WarmRoundedSprite.Get(
                active
                    ? new Color(1f, 0.72f, 0.18f, 0.92f)
                    : new Color(0.03f, 0.10f, 0.24f, 0.38f),
                14,
                active
                    ? new Color(1f, 0.88f, 0.42f, 0.75f)
                    : new Color(0.45f, 0.80f, 1f, 0.45f),
                active ? 1.8f : 1.4f);
            if (favoriteChip.label != null)
            {
                favoriteChip.label.color = active
                    ? new Color(0.18f, 0.08f, 0.02f, 1f)
                    : new Color(0.88f, 0.94f, 1f, 1f);
            }
        }
    }

    void RefreshDetailTabChips()
    {
        RefreshChipGroup(detailTabChips, detailTabIndex, true);
    }

    void RefreshChipGroup(FilterChipRefs[] chips, int activeIndex, bool orangeActive)
    {
        if (chips == null) return;
        for (int i = 0; i < chips.Length; i++)
        {
            FilterChipRefs chip = chips[i];
            if (chip.background == null) continue;
            bool active = activeIndex == i;
            chip.background.sprite = WarmRoundedSprite.Get(
                active
                    ? (orangeActive
                        ? new Color(1f, 0.72f, 0.18f, 0.92f)
                        : new Color(1f, 0.72f, 0.18f, 0.92f))
                    : new Color(0.03f, 0.10f, 0.24f, 0.38f),
                14,
                active
                    ? new Color(1f, 0.88f, 0.42f, 0.75f)
                    : new Color(0.45f, 0.80f, 1f, 0.45f),
                active ? 1.8f : 1.4f);
            if (chip.label != null)
            {
                chip.label.color = active
                    ? new Color(0.18f, 0.08f, 0.02f, 1f)
                    : new Color(0.88f, 0.94f, 1f, 1f);
            }
        }
    }

    static void RefreshUnitPortrait(UnitCardRefs card, int unitNumber, bool discovered, Color gradeColor)
    {
        if (card.portraitImage == null) return;

        if (!discovered)
        {
            card.portraitImage.enabled = false;
            return;
        }

        Sprite sprite = UnitArchivePortraitProvider.GetPortraitSprite(unitNumber);
        if (sprite == null)
        {
            card.portraitImage.enabled = false;
            if (card.iconBg != null)
            {
                card.iconBg.sprite = WarmRoundedSprite.Get(
                    new Color(gradeColor.r, gradeColor.g, gradeColor.b, 0.18f),
                    12, new Color(1f, 1f, 1f, 0.12f), 1f);
            }
            return;
        }

        card.portraitImage.sprite = sprite;
        card.portraitImage.enabled = true;

        RectTransform portraitRect = card.portraitImage.rectTransform;
        portraitRect.anchorMin = Vector2.zero;
        portraitRect.anchorMax = Vector2.one;
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = Vector2.zero;
    }

    void RefreshDetailPanel()
    {
        bool discovered = UnitArchiveUnlockData.IsDiscovered(selectedUnit);
        bool upgradeUnlocked = UnitArchiveUnlockData.IsUnlocked(selectedUnit);
        int grade = UnitCombatStats.GetGradeForUnit(selectedUnit);
        Color gradeColor = GetGradeColor(grade);

        detailNameText.text = discovered ? UnitTraitData.GetDisplayName(selectedUnit) : "???";
        detailNameText.color = discovered
            ? (upgradeUnlocked ? UnitTraitData.GetDisplayNameColor(selectedUnit) : new Color(0.62f, 0.58f, 0.52f, 1f))
            : new Color(0.45f, 0.42f, 0.38f, 1f);
        detailGradeText.text = GameLocalization.GradeNameFormat(grade);
        detailLockedText.text = upgradeUnlocked ? string.Empty : UnitArchiveUiText.GetUnlockHint(selectedUnit);

        if (detailGradeBadge != null)
        {
            detailGradeBadge.sprite = WarmRoundedSprite.Get(
                new Color(gradeColor.r, gradeColor.g, gradeColor.b, discovered ? 0.16f : 0.08f),
                12, new Color(gradeColor.r, gradeColor.g, gradeColor.b, discovered ? 0.55f : 0.25f), 1.2f);
            detailGradeText.color = discovered ? gradeColor : Tan;
        }

        if (specialtyRow.labelText != null)
        {
            string specialtyLabel = UnitArchiveUiText.GetSpecialtyLabel(selectedUnit);
            specialtyRow.labelText.text = string.IsNullOrEmpty(specialtyLabel)
                ? GameLocalization.ArchiveMobilityDisabled
                : specialtyLabel;
        }

        RefreshSlotRow(attackRow, CharacterUpgradeData.UpgradeSlot.Attack, upgradeUnlocked);
        RefreshSlotRow(mobilityRow, CharacterUpgradeData.UpgradeSlot.Mobility, upgradeUnlocked);
        RefreshSlotRow(specialtyRow, CharacterUpgradeData.UpgradeSlot.Specialty, upgradeUnlocked);

        if (previewText != null)
        {
            previewText.text = upgradeUnlocked ? BuildPreviewText(selectedUnit) : string.Empty;
        }

        if (upgradeContent != null) upgradeContent.SetActive(detailTabIndex == 0);
        if (codexContent != null)
        {
            codexContent.SetActive(detailTabIndex == 1);
            if (detailTabIndex == 1 && codexText != null)
            {
                codexText.text = UnitArchiveCodexText.BuildPlainText(selectedUnit, discovered);
            }
        }
        if (awakeningContent != null)
        {
            awakeningContent.SetActive(detailTabIndex == 2);
            if (detailTabIndex == 2) RefreshAwakeningPanel(discovered, upgradeUnlocked);
        }
    }

    void RefreshAwakeningPanel(bool discovered, bool upgradeUnlocked)
    {
        if (awakeningLevelText == null) return;

        if (!discovered)
        {
            awakeningLevelText.text = "???";
            awakeningTierText.text = GameLocalization.ArchiveLockedHint;
            awakeningHintText.text = string.Empty;
            awakeningCostText.text = string.Empty;
            if (awakeningUpgradeButton != null) awakeningUpgradeButton.interactable = false;
            return;
        }

        int level = UnitArchiveAwakening.GetLevel(selectedUnit);
        awakeningLevelText.text = GameLocalization.ArchiveAwakeningLevelFormat(level, UnitArchiveAwakening.GetMaxLevel())
                                    + "  ·  " + GameLocalization.ArchiveAwakeningEffectFormat(UnitArchiveAwakening.GetAttackBonus(selectedUnit));

        var sb = new StringBuilder();
        var defs = UnitEvolutionMilestones.GetMilestoneDefinitions(selectedUnit);
        for (int i = 0; i < defs.Count; i++)
        {
            string prefix = i < level ? "✓ " : "○ ";
            sb.AppendLine(prefix + GameLocalization.ArchiveCodexAwakeningLineFormat(i + 1, defs[i].description));
        }
        awakeningTierText.text = sb.ToString().TrimEnd();

        if (level >= UnitArchiveAwakening.GetMaxLevel())
        {
            awakeningHintText.text = GameLocalization.ArchiveMaxLabel;
            awakeningCostText.text = string.Empty;
            if (awakeningUpgradeButton != null) awakeningUpgradeButton.interactable = false;
            return;
        }

        int nextLevel = level + 1;
        if (!upgradeUnlocked)
        {
            awakeningHintText.text = GameLocalization.ArchiveAwakeningNeedUpgradeUnlock;
            awakeningCostText.text = string.Empty;
            if (awakeningUpgradeButton != null) awakeningUpgradeButton.interactable = false;
            return;
        }

        if (!UnitArchiveAwakening.MeetsRequirement(selectedUnit, nextLevel))
        {
            awakeningHintText.text = UnitArchiveAwakening.GetUnlockHint(selectedUnit, nextLevel);
            awakeningCostText.text = string.Empty;
            if (awakeningUpgradeButton != null) awakeningUpgradeButton.interactable = false;
            return;
        }

        awakeningHintText.text = GameLocalization.ArchiveAwakeningNextTierFormat(nextLevel);
        int cost = UnitArchiveAwakening.GetUpgradeCost(selectedUnit);
        awakeningCostText.text = GameLocalization.ArchiveCostFormat(cost);
        if (awakeningUpgradeButton != null)
        {
            awakeningUpgradeButton.interactable = MetaCurrency.GetMedals() >= cost;
        }
    }

    void RefreshSlotRow(SlotRowRefs row, CharacterUpgradeData.UpgradeSlot slot, bool upgradeUnlocked)
    {
        if (row.upgradeButton == null) return;

        if (row.rowBg != null)
        {
            row.rowBg.sprite = WarmRoundedSprite.Get(
                upgradeUnlocked ? CardFillLight : new Color(0.10f, 0.08f, 0.06f, 0.85f),
                14, new Color(1f, 0.85f, 0.55f, upgradeUnlocked ? 0.10f : 0.05f), 1.2f);
        }

        if (!upgradeUnlocked)
        {
            row.levelText.text = "—";
            row.costText.text = string.Empty;
            row.upgradeButton.interactable = false;
            return;
        }

        if (slot == CharacterUpgradeData.UpgradeSlot.Mobility && !UnitCombatStats.CanAutoAttack(selectedUnit))
        {
            row.levelText.text = GameLocalization.ArchiveMobilityDisabled;
            row.costText.text = string.Empty;
            row.upgradeButton.interactable = false;
            return;
        }

        if (slot == CharacterUpgradeData.UpgradeSlot.Specialty && !UnitArchiveSpecialty.HasSpecialty(selectedUnit))
        {
            row.levelText.text = GameLocalization.ArchiveMobilityDisabled;
            row.costText.text = string.Empty;
            row.upgradeButton.interactable = false;
            return;
        }

        int current;
        int max;
        if (slot == CharacterUpgradeData.UpgradeSlot.Attack)
        {
            current = CharacterUpgradeData.GetAttackLevel(selectedUnit);
            max = CharacterUpgradeData.GetMaxAttackLevel(selectedUnit);
        }
        else if (slot == CharacterUpgradeData.UpgradeSlot.Mobility)
        {
            current = CharacterUpgradeData.GetMobilityLevel(selectedUnit);
            max = CharacterUpgradeData.GetMaxMobilityLevel(selectedUnit);
        }
        else
        {
            current = CharacterUpgradeData.GetSpecialtyLevel(selectedUnit);
            max = CharacterUpgradeData.GetMaxSpecialtyLevel(selectedUnit);
        }

        row.levelText.text = GameLocalization.ArchiveLevelFormat(current, max);
        bool canUpgrade = CharacterUpgradeData.CanUpgrade(selectedUnit, slot);
        if (canUpgrade)
        {
            int cost = CharacterUpgradeData.GetUpgradeCost(selectedUnit, slot, current);
            row.costText.text = GameLocalization.ArchiveCostFormat(cost);
            row.upgradeButton.interactable = MetaCurrency.GetMedals() >= cost;
        }
        else
        {
            row.costText.text = GameLocalization.ArchiveMaxLabel;
            row.upgradeButton.interactable = false;
        }
    }

    static string BuildPreviewText(int unitNumber)
    {
        const int previewEvo = 1;
        int baseCore = UnitCombatStats.CalculateCoreDamage(unitNumber, previewEvo);
        int currentBonus = CharacterUpgradeData.GetAttackBonus(unitNumber);
        int currentAtk = baseCore + currentBonus;

        int nextAtk = currentAtk;
        if (CharacterUpgradeData.CanUpgrade(unitNumber, CharacterUpgradeData.UpgradeSlot.Attack))
        {
            int nextLevel = CharacterUpgradeData.GetAttackLevel(unitNumber) + 1;
            nextAtk = baseCore + CharacterUpgradeData.GetAttackBonusForLevel(nextLevel);
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(GameLocalization.ArchivePreviewAttackFormat(currentAtk, nextAtk));

        if (UnitCombatStats.CanAutoAttack(unitNumber))
        {
            float intervalBefore = UnitCombatStats.GetAttackIntervalForUnit(unitNumber) *
                                   CharacterUpgradeData.GetMobilityIntervalMultiplier(unitNumber);
            float intervalAfter = intervalBefore;
            if (CharacterUpgradeData.CanUpgrade(unitNumber, CharacterUpgradeData.UpgradeSlot.Mobility))
            {
                int nextMobility = CharacterUpgradeData.GetMobilityLevel(unitNumber) + 1;
                intervalAfter = UnitCombatStats.GetAttackIntervalForUnit(unitNumber) *
                                CharacterUpgradeData.GetMobilityIntervalMultiplierForLevel(nextMobility);
            }

            string beforeAps = FormatAps(intervalBefore);
            string afterAps = FormatAps(intervalAfter);
            sb.Append(GameLocalization.ArchivePreviewSpeedFormat(beforeAps, afterAps));
        }

        switch (UnitArchiveSpecialty.GetType(unitNumber))
        {
            case UnitArchiveSpecialty.Type.Range:
            {
                float rangeBefore = UnitCombatStats.GetEffectiveRangeForDisplay(unitNumber);
                float rangeAfter = rangeBefore;
                if (CharacterUpgradeData.CanUpgrade(unitNumber, CharacterUpgradeData.UpgradeSlot.Specialty))
                {
                    int nextLevel = CharacterUpgradeData.GetSpecialtyLevel(unitNumber) + 1;
                    float baseRange = UnitCombatStats.GetAttackRangeForUnit(unitNumber);
                    rangeAfter = baseRange + CharacterUpgradeData.GetRangeBonusForLevel(nextLevel);
                }
                sb.AppendLine();
                sb.Append(GameLocalization.ArchivePreviewRangeFormat(rangeBefore, rangeAfter));
                break;
            }
            case UnitArchiveSpecialty.Type.Economy:
            {
                float mulBefore = CharacterUpgradeData.GetEconomyCooldownMultiplier(unitNumber);
                float mulAfter = mulBefore;
                if (CharacterUpgradeData.CanUpgrade(unitNumber, CharacterUpgradeData.UpgradeSlot.Specialty))
                {
                    mulAfter = CharacterUpgradeData.GetEconomyCooldownMultiplierForLevel(
                        CharacterUpgradeData.GetSpecialtyLevel(unitNumber) + 1);
                }
                sb.AppendLine();
                sb.Append(GameLocalization.ArchivePreviewEconomyFormat(
                    (1f - mulBefore) * 100f, (1f - mulAfter) * 100f));
                break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    static string FormatAps(float interval)
    {
        if (interval <= 0f) return "—";
        return (1f / interval).ToString("0.##");
    }
}
