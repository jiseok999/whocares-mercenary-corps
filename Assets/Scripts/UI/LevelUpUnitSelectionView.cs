using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 유닛 선택 패널 런타임 — 연출·리롤·카드 갱신
/// </summary>
public class LevelUpUnitSelectionView : MonoBehaviour
{
    const float CardWidth = 276f;
    const float CardHeight = 664f;
    const float CardGap = 22f;
    const float CardPortraitTop = 44f;
    const float CardPortraitHeight = 154f;
    const float CardTapRowHeight = 36f;
    const float CardTapRowBottom = 12f;
    const float CardStatsBottomPad = 10f;

    static readonly Color Accent = new Color(1f, 0.78f, 0.30f, 1f);
    static readonly Color Cream = new Color(0.93f, 0.86f, 0.74f, 1f);
    static readonly Color Tan = new Color(0.80f, 0.66f, 0.48f, 1f);

    int playerLevel;
    bool rerollUsed;
    bool isLocked;
    Transform cardsRow;
    Button rerollButton;
    Text rerollLabel;
    CanvasGroup panelGroup;
    RectTransform modalRect;
    RectTransform modalShadowRect;
    RectTransform rerollRect;
    CanvasGroup rerollGroup;
    Image panelDimImage;
    Text titleText;
    LevelUpSelectionCelebrationFx celebrationFx;
    LevelUpSelectionCelebrationFx.Intensity celebrationIntensity;
    readonly List<CardView> cardViews = new List<CardView>();

    struct CardView
    {
        public GameObject root;
        public CanvasGroup group;
        public RectTransform rect;
        public Image bodyImage;
        public Image glowImage;
        public Button button;
        public UnitData unitData;
        public ShopManager.LevelUpUnitChoice choice;
        public Text nameText;
        public Text starsText;
        public Text statsText;
        public Text tapHintText;
        public Text badgeText;
        public Text ribbonText;
        public Text gradePillText;
        public Transform traitChipsRow;
        public float anchoredX;
    }

    public void Initialize(
        int level,
        Transform cardsRoot,
        Button rerollBtn,
        Text rerollTxt,
        CanvasGroup group,
        RectTransform modal,
        RectTransform modalShadow,
        RectTransform rerollRectTransform,
        Image dimImage,
        Text title,
        LevelUpSelectionCelebrationFx fx,
        bool fullCelebration)
    {
        playerLevel = level;
        cardsRow = cardsRoot;
        rerollButton = rerollBtn;
        rerollLabel = rerollTxt;
        panelGroup = group;
        modalRect = modal;
        modalShadowRect = modalShadow;
        rerollRect = rerollRectTransform;
        panelDimImage = dimImage;
        titleText = title;
        celebrationFx = fx;
        celebrationIntensity = fullCelebration
            ? LevelUpSelectionCelebrationFx.Intensity.Full
            : LevelUpSelectionCelebrationFx.Intensity.Soft;

        rerollGroup = rerollRectTransform != null ? rerollRectTransform.GetComponent<CanvasGroup>() : null;
        if (rerollGroup == null && rerollRectTransform != null)
        {
            rerollGroup = rerollRectTransform.gameObject.AddComponent<CanvasGroup>();
        }

        rerollUsed = false;
        isLocked = false;
        PopulateCards(RollChoices());
        GameSfxPlayer.PlayUnitCardReveal();
        StartCoroutine(PlayOpenAnimation());
    }

    List<ShopManager.LevelUpUnitChoice> RollChoices()
    {
        if (ShopManager.Instance == null) return new List<ShopManager.LevelUpUnitChoice>();
        return ShopManager.Instance.RollLevelUpChoices(playerLevel, 3);
    }

    void PopulateCards(List<ShopManager.LevelUpUnitChoice> choices)
    {
        ClearCards();
        if (choices == null || choices.Count == 0) return;

        float totalWidth = choices.Count * CardWidth + (choices.Count - 1) * CardGap;
        float startX = -totalWidth * 0.5f + CardWidth * 0.5f;

        for (int i = 0; i < choices.Count; i++)
        {
            float x = startX + i * (CardWidth + CardGap);
            CardView view = BuildCard(cardsRow, choices[i], x, i);
            cardViews.Add(view);
        }
    }

    void ClearCards()
    {
        for (int i = cardViews.Count - 1; i >= 0; i--)
        {
            if (cardViews[i].root != null)
            {
                Destroy(cardViews[i].root);
            }
        }
        cardViews.Clear();
    }

    public void RefreshLocalizedTexts()
    {
        Font font = UIFontProvider.Get();
        if (rerollLabel != null && !rerollUsed)
        {
            rerollLabel.font = font;
            rerollLabel.text = GameLocalization.LevelUpReroll;
        }

        for (int i = 0; i < cardViews.Count; i++)
        {
            RefreshCardLocalizedTexts(cardViews[i], font);
        }
    }

    void RefreshCardLocalizedTexts(CardView view, Font font)
    {
        if (view.unitData == null) return;

        UnitData unitData = view.unitData;
        ShopManager.LevelUpUnitChoice choice = view.choice;

        if (view.nameText != null)
        {
            view.nameText.font = font;
            view.nameText.text = unitData.GetUnitName();
        }

        if (view.gradePillText != null)
        {
            view.gradePillText.font = font;
            view.gradePillText.text = unitData.GetGradeName();
        }

        if (view.badgeText != null)
        {
            view.badgeText.font = font;
            view.badgeText.text = choice.isNewUnit
                ? GameLocalization.EvolutionNewBadge4
                : GameLocalization.LevelUpEvolutionBadge;
        }

        if (view.starsText != null)
        {
            view.starsText.font = font;
            view.starsText.text = UnitCombatStats.FormatEvolutionStars(choice.resultEvolutionLevel);
        }

        if (view.ribbonText != null)
        {
            view.ribbonText.font = font;
            view.ribbonText.text = GameLocalization.EvolutionRibbonUnlock;
        }

        if (view.statsText != null)
        {
            view.statsText.font = font;
            view.statsText.text = UnitCombatStats.BuildLevelUpCardPreviewRich(
                unitData.unitNumber, choice.isNewUnit,
                choice.currentEvolutionLevel, choice.resultEvolutionLevel);
        }

        if (view.tapHintText != null)
        {
            view.tapHintText.font = font;
            view.tapHintText.text = GameLocalization.LevelUpTapToAcquire;
        }

        RefreshTraitChipLabels(view.traitChipsRow, unitData.unitNumber, font);
    }

    static void RefreshTraitChipLabels(Transform traitChipsRow, int unitNumber, Font font)
    {
        if (traitChipsRow == null) return;

        string[] traits = UnitTraitData.GetTraits(unitNumber);
        if (traits == null || traits.Length == 0) return;

        int chipIndex = 0;
        for (int i = 0; i < traitChipsRow.childCount && chipIndex < traits.Length; i++)
        {
            Transform chip = traitChipsRow.GetChild(i);
            Text chipText = chip.GetComponentInChildren<Text>();
            if (chipText == null) continue;

            chipText.font = font;
            chipText.text = GameLocalization.GetTraitDisplayName(traits[chipIndex]);
            chipIndex++;
        }
    }

    public void OnRerollClicked()
    {
        if (isLocked || rerollUsed) return;
        rerollUsed = true;
        if (rerollButton != null)
        {
            rerollButton.interactable = false;
        }
        if (rerollLabel != null)
        {
            rerollLabel.text = GameLocalization.LevelUpRerollUsed;
            rerollLabel.color = new Color(0.55f, 0.50f, 0.45f, 0.85f);
        }
        StartCoroutine(RerollRoutine());
    }

    IEnumerator RerollRoutine()
    {
        isLocked = true;
        float outDur = 0.14f;
        float elapsed = 0f;
        while (elapsed < outDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / outDur);
            float scale = Mathf.Lerp(1f, 0.82f, t);
            float alpha = Mathf.Lerp(1f, 0f, t);
            ApplyCardsVisual(scale, alpha);
            yield return null;
        }

        PopulateCards(RollChoices());
        GameSfxPlayer.PlayUnitCardReveal();

        for (int i = 0; i < cardViews.Count; i++)
        {
            if (cardViews[i].group != null) cardViews[i].group.alpha = 0f;
            if (cardViews[i].rect != null) cardViews[i].rect.localScale = Vector3.one * 0.82f;
        }

        float inDur = 0.22f;
        elapsed = 0f;
        while (elapsed < inDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / inDur);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cv = cardViews[i];
                float delay = i * 0.06f;
                float localT = Mathf.Clamp01((ease - delay) / Mathf.Max(0.001f, 1f - delay));
                if (cv.group != null) cv.group.alpha = localT;
                if (cv.rect != null) cv.rect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, localT);
            }
            yield return null;
        }

        ApplyCardsVisual(1f, 1f);
        isLocked = false;
    }

    void OnCardClicked(UnitData unitData, CardView selected)
    {
        if (isLocked || unitData == null) return;
        isLocked = true;
        if (rerollButton != null) rerollButton.interactable = false;
        StartCoroutine(SelectCardRoutine(unitData, selected));
    }

    IEnumerator SelectCardRoutine(UnitData unitData, CardView selected)
    {
        for (int i = 0; i < cardViews.Count; i++)
        {
            CardView cv = cardViews[i];
            if (cv.button != null) cv.button.interactable = false;
        }

        float dur = 0.38f;
        float elapsed = 0f;
        Vector3 selStart = selected.rect != null ? selected.rect.localScale : Vector3.one;

        GameObject burstObj = null;
        if (selected.rect != null)
        {
            Text burstText = CreateText(selected.rect, GameLocalization.LevelUpAcquiredBurst, 26, FontStyle.Bold, Accent, TextAnchor.MiddleCenter);
            burstObj = burstText.gameObject;
            RectTransform burstRect = burstObj.GetComponent<RectTransform>();
            burstRect.anchorMin = new Vector2(0.5f, 0.5f);
            burstRect.anchorMax = new Vector2(0.5f, 0.5f);
            burstRect.pivot = new Vector2(0.5f, 0.5f);
            burstRect.anchoredPosition = new Vector2(0f, 40f);
            burstRect.sizeDelta = new Vector2(200f, 48f);
            CanvasGroup burstCg = burstObj.AddComponent<CanvasGroup>();
            burstCg.alpha = 0f;
        }

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);

            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cv = cardViews[i];
                bool isSel = cv.root == selected.root;
                float alpha = isSel ? 1f : Mathf.Lerp(1f, 0.15f, t);
                float scale = isSel ? Mathf.Lerp(selStart.x, 1.14f, EaseOutBack(t)) : Mathf.Lerp(1f, 0.88f, t);
                if (cv.group != null) cv.group.alpha = alpha;
                if (cv.rect != null) cv.rect.localScale = Vector3.one * scale;
                if (isSel && cv.glowImage != null)
                {
                    cv.glowImage.color = new Color(1f, 0.85f, 0.35f, Mathf.Lerp(0.15f, 0.75f, t));
                }
            }

            if (burstObj != null)
            {
                CanvasGroup cg = burstObj.GetComponent<CanvasGroup>();
                float bt = Mathf.Clamp01((t - 0.08f) / 0.55f);
                cg.alpha = bt < 0.7f ? bt / 0.7f : 1f - ((bt - 0.7f) / 0.3f);
                burstObj.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.15f, EaseOutBack(Mathf.Clamp01(t * 1.4f)));
            }

            yield return null;
        }

        if (ShopManager.Instance != null && ShopManager.Instance.GrantLevelUpUnitReward(unitData))
        {
            LevelUpUnitSelectionController.NotifySelectionFinished();
        }
        else
        {
            isLocked = false;
            for (int i = 0; i < cardViews.Count; i++)
            {
                if (cardViews[i].button != null) cardViews[i].button.interactable = true;
            }
            if (!rerollUsed && rerollButton != null) rerollButton.interactable = true;
            ApplyCardsVisual(1f, 1f);
        }
    }

    IEnumerator PlayOpenAnimation()
    {
        if (panelGroup != null) panelGroup.alpha = 0f;

        Vector3 modalBaseScale = Vector3.one;
        if (modalRect != null)
        {
            modalRect.localScale = modalBaseScale * 0.78f;
        }
        if (modalShadowRect != null)
        {
            modalShadowRect.localScale = modalBaseScale * 0.78f;
        }

        Color dimBase = panelDimImage != null ? panelDimImage.color : new Color(0.03f, 0.02f, 0.015f, 0.92f);
        if (panelDimImage != null)
        {
            panelDimImage.color = new Color(dimBase.r, dimBase.g, dimBase.b, 0f);
        }

        Vector2 rerollBasePos = rerollRect != null ? rerollRect.anchoredPosition : Vector2.zero;
        if (rerollGroup != null) rerollGroup.alpha = 0f;
        if (rerollRect != null) rerollRect.anchoredPosition = rerollBasePos + new Vector2(0f, -28f);

        for (int i = 0; i < cardViews.Count; i++)
        {
            CardView cv = cardViews[i];
            if (cv.group != null) cv.group.alpha = 0f;
            if (cv.rect != null)
            {
                cv.rect.localScale = Vector3.one * 0.68f;
                cv.rect.anchoredPosition = new Vector2(cv.anchoredX, -36f);
            }
        }

        if (celebrationFx != null && panelDimImage != null)
        {
            celebrationFx.SpawnConfetti(panelDimImage.rectTransform, celebrationIntensity);
            if (modalRect != null)
            {
                celebrationFx.PlayGoldenBurst(modalRect, celebrationIntensity);
            }
            StartCoroutine(celebrationFx.PlayCheerBanner(transform, celebrationIntensity));
            StartCoroutine(celebrationFx.PlayTitlePop(titleText, celebrationIntensity));
        }

        float dur = celebrationIntensity == LevelUpSelectionCelebrationFx.Intensity.Full ? 0.58f : 0.44f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            if (panelGroup != null) panelGroup.alpha = ease;
            if (panelDimImage != null)
            {
                float goldPulse = celebrationIntensity == LevelUpSelectionCelebrationFx.Intensity.Full
                    ? Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI) * 0.06f
                    : Mathf.Sin(Mathf.Clamp01(t / 0.28f) * Mathf.PI) * 0.03f;
                panelDimImage.color = new Color(
                    dimBase.r + goldPulse,
                    dimBase.g + goldPulse * 0.7f,
                    dimBase.b + goldPulse * 0.2f,
                    Mathf.Lerp(0f, dimBase.a, ease));
            }

            float modalScale = Mathf.Lerp(0.78f, 1f, EaseOutBack(Mathf.Clamp01(t / 0.72f)));
            if (modalRect != null) modalRect.localScale = modalBaseScale * modalScale;
            if (modalShadowRect != null) modalShadowRect.localScale = modalBaseScale * modalScale;

            float cardStart = 0.12f;
            float cardDur = 0.78f;
            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cv = cardViews[i];
                float delay = i * (celebrationIntensity == LevelUpSelectionCelebrationFx.Intensity.Full ? 0.09f : 0.07f);
                float localT = Mathf.Clamp01((ease - cardStart - delay) / Mathf.Max(0.001f, cardDur - delay));
                float cardEase = EaseOutBack(localT);
                if (cv.group != null) cv.group.alpha = localT;
                if (cv.rect != null)
                {
                    cv.rect.localScale = Vector3.one * Mathf.Lerp(0.68f, 1f, cardEase);
                    cv.rect.anchoredPosition = new Vector2(cv.anchoredX, Mathf.Lerp(-36f, 0f, cardEase));
                    if (localT > 0.01f && cv.glowImage != null)
                    {
                        cv.glowImage.color = new Color(1f, 0.85f, 0.35f, Mathf.Lerp(0f, 0.18f, localT));
                    }
                }
            }

            float rerollStart = 0.34f;
            float rerollT = Mathf.Clamp01((t - rerollStart) / 0.45f);
            if (rerollGroup != null) rerollGroup.alpha = rerollT;
            if (rerollRect != null)
            {
                rerollRect.anchoredPosition = Vector2.Lerp(rerollBasePos + new Vector2(0f, -28f), rerollBasePos, EaseOutCubic(rerollT));
            }

            yield return null;
        }

        if (panelDimImage != null) panelDimImage.color = dimBase;
        if (modalRect != null) modalRect.localScale = modalBaseScale;
        if (modalShadowRect != null) modalShadowRect.localScale = modalBaseScale;
        if (rerollGroup != null) rerollGroup.alpha = 1f;
        if (rerollRect != null) rerollRect.anchoredPosition = rerollBasePos;
        ApplyCardsVisual(1f, 1f);
        for (int i = 0; i < cardViews.Count; i++)
        {
            if (cardViews[i].rect != null)
            {
                cardViews[i].rect.anchoredPosition = new Vector2(cardViews[i].anchoredX, 0f);
            }
            if (cardViews[i].glowImage != null)
            {
                cardViews[i].glowImage.color = new Color(1f, 0.85f, 0.35f, 0f);
            }
        }
    }

    static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    void ApplyCardsVisual(float scale, float alpha)
    {
        for (int i = 0; i < cardViews.Count; i++)
        {
            CardView cv = cardViews[i];
            if (cv.group != null) cv.group.alpha = alpha;
            if (cv.rect != null) cv.rect.localScale = Vector3.one * scale;
        }
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    CardView BuildCard(Transform parent, ShopManager.LevelUpUnitChoice choice, float anchoredX, int index)
    {
        CardView view = new CardView();
        UnitData unitData = choice.unitData;
        if (unitData == null) return view;

        view.choice = choice;

        int grade = unitData.grade;
        string gradeHex = UnitCombatStats.GetGradeColorHex(grade);
        Color gradeTint = ShopManager.Instance != null
            ? ShopManager.Instance.GetGradeFrameColorForUi(grade)
            : Color.white;

        GameObject cardObj = new GameObject($"LevelUpCard_{unitData.unitNumber}");
        cardObj.transform.SetParent(parent, false);
        view.root = cardObj;
        view.unitData = unitData;

        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        view.rect = cardRect;
        cardRect.anchorMin = new Vector2(0.5f, 0f);
        cardRect.anchorMax = new Vector2(0.5f, 0f);
        cardRect.pivot = new Vector2(0.5f, 0f);
        cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
        cardRect.anchoredPosition = new Vector2(anchoredX, 0f);
        view.anchoredX = anchoredX;

        CanvasGroup cg = cardObj.AddComponent<CanvasGroup>();
        view.group = cg;

        Image shadow = cardObj.AddComponent<Image>();
        shadow.type = Image.Type.Sliced;
        shadow.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.55f), 28, new Color(0f, 0f, 0f, 0f), 0f);
        shadow.raycastTarget = false;

        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(cardObj.transform, false);
        Image bodyImage = bodyObj.AddComponent<Image>();
        view.bodyImage = bodyImage;
        bodyImage.type = Image.Type.Sliced;
        bodyImage.sprite = WarmRoundedSprite.Get(
            new Color(0.14f, 0.10f, 0.08f, 0.99f),
            24,
            new Color(gradeTint.r, gradeTint.g, gradeTint.b, 0.65f),
            3f);
        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        Stretch(bodyRect, 6f);

        GameObject glowObj = new GameObject("SelectGlow");
        glowObj.transform.SetParent(bodyObj.transform, false);
        Image glowImage = glowObj.AddComponent<Image>();
        view.glowImage = glowImage;
        glowImage.raycastTarget = false;
        glowImage.sprite = WarmRoundedSprite.Get(new Color(1f, 0.82f, 0.35f, 0.2f), 20, new Color(0f, 0f, 0f, 0f), 0f);
        Stretch(glowObj.GetComponent<RectTransform>(), 0f);
        glowImage.color = new Color(1f, 0.85f, 0.35f, 0f);

        GameObject innerFrame = new GameObject("InnerFrame");
        innerFrame.transform.SetParent(bodyObj.transform, false);
        Image innerImg = innerFrame.AddComponent<Image>();
        innerImg.raycastTarget = false;
        innerImg.type = Image.Type.Sliced;
        innerImg.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0f), 18, new Color(1f, 0.88f, 0.55f, 0.12f), 1.5f);
        Stretch(innerFrame.GetComponent<RectTransform>(), 10f);

        // 등급 색상의 은은한 상단 배경 밴드 — 카드별 정체성 부여
        GameObject headerBand = new GameObject("HeaderBand");
        headerBand.transform.SetParent(bodyObj.transform, false);
        Image headerBandImg = headerBand.AddComponent<Image>();
        headerBandImg.raycastTarget = false;
        headerBandImg.type = Image.Type.Sliced;
        headerBandImg.sprite = WarmRoundedSprite.Get(
            new Color(gradeTint.r, gradeTint.g, gradeTint.b, 0.10f), 22, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform headerBandRect = headerBand.GetComponent<RectTransform>();
        headerBandRect.anchorMin = new Vector2(0f, 1f);
        headerBandRect.anchorMax = new Vector2(1f, 1f);
        headerBandRect.pivot = new Vector2(0.5f, 1f);
        headerBandRect.anchoredPosition = Vector2.zero;
        headerBandRect.sizeDelta = new Vector2(0f, 204f);

        GameObject topStripe = new GameObject("GradeStripe");
        topStripe.transform.SetParent(bodyObj.transform, false);
        Image stripeImage = topStripe.AddComponent<Image>();
        stripeImage.raycastTarget = false;
        stripeImage.sprite = WarmRoundedSprite.Get(gradeTint, 6, new Color(1f, 1f, 1f, 0.2f), 1f);
        RectTransform stripeRect = topStripe.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 1f);
        stripeRect.anchorMax = new Vector2(1f, 1f);
        stripeRect.pivot = new Vector2(0.5f, 1f);
        stripeRect.sizeDelta = new Vector2(0f, 7f);

        view.badgeText = CreateBadge(bodyObj.transform, choice.isNewUnit);

        GameObject gradePill = new GameObject("GradePill");
        gradePill.transform.SetParent(bodyObj.transform, false);
        Image pillBg = gradePill.AddComponent<Image>();
        pillBg.raycastTarget = false;
        pillBg.type = Image.Type.Sliced;
        pillBg.sprite = WarmRoundedSprite.Get(
            new Color(gradeTint.r * 0.30f, gradeTint.g * 0.30f, gradeTint.b * 0.30f, 0.92f), 13,
            new Color(gradeTint.r, gradeTint.g, gradeTint.b, 0.85f), 1.5f);
        RectTransform pillRect = gradePill.GetComponent<RectTransform>();
        pillRect.anchorMin = new Vector2(1f, 1f);
        pillRect.anchorMax = new Vector2(1f, 1f);
        pillRect.pivot = new Vector2(1f, 1f);
        pillRect.anchoredPosition = new Vector2(-14f, -16f);
        pillRect.sizeDelta = new Vector2(78f, 26f);
        Text pillText = CreateText(gradePill.transform, unitData.GetGradeName(), 13, FontStyle.Bold,
            ColorUtility.TryParseHtmlString($"#{gradeHex}", out Color parsed) ? parsed : Cream,
            TextAnchor.MiddleCenter);
        Stretch(pillText.rectTransform, 0f);
        view.gradePillText = pillText;

        GameObject portraitFrame = new GameObject("PortraitFrame");
        portraitFrame.transform.SetParent(bodyObj.transform, false);
        Image frameImg = portraitFrame.AddComponent<Image>();
        frameImg.raycastTarget = false;
        frameImg.type = Image.Type.Sliced;
        frameImg.sprite = WarmRoundedSprite.Get(
            new Color(0.05f, 0.035f, 0.025f, 0.85f), 18,
            new Color(gradeTint.r, gradeTint.g, gradeTint.b, 0.5f), 2f);
        RectTransform frameRect = portraitFrame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 1f);
        frameRect.anchorMax = new Vector2(0.5f, 1f);
        frameRect.pivot = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = new Vector2(0f, -CardPortraitTop);
        frameRect.sizeDelta = new Vector2(196f, CardPortraitHeight);

        GameObject visualRoot = new GameObject("UnitVisual");
        visualRoot.transform.SetParent(portraitFrame.transform, false);
        RectTransform visualRect = visualRoot.AddComponent<RectTransform>();
        Stretch(visualRect, 8f);
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.PopulateLevelUpCardVisual(visualRoot.transform, choice);
        }

        float contentTop = CardPortraitTop + CardPortraitHeight + 10f;
        float nameTop = contentTop;
        float starsTop = nameTop + 30f;
        float traitsTop = starsTop + 24f;
        float sectionGap = 8f;

        Text nameText = CreateText(bodyObj.transform, unitData.GetUnitName(), 20, FontStyle.Bold,
            UnitTraitData.GetDisplayNameColor(unitData.unitNumber), TextAnchor.UpperCenter);
        view.nameText = nameText;
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -nameTop);
        nameRect.sizeDelta = new Vector2(CardWidth - 32f, 28f);

        Text starsText = CreateText(bodyObj.transform,
            UnitCombatStats.FormatEvolutionStars(choice.resultEvolutionLevel),
            16, FontStyle.Normal, Cream, TextAnchor.UpperCenter);
        view.starsText = starsText;
        starsText.supportRichText = true;
        RectTransform starsRect = starsText.rectTransform;
        starsRect.anchorMin = new Vector2(0.5f, 1f);
        starsRect.anchorMax = new Vector2(0.5f, 1f);
        starsRect.pivot = new Vector2(0.5f, 1f);
        starsRect.anchoredPosition = new Vector2(0f, -starsTop);
        starsRect.sizeDelta = new Vector2(CardWidth - 28f, 20f);

        view.traitChipsRow = CreateTraitChips(bodyObj.transform, unitData.unitNumber, traitsTop);

        bool hasMilestones = UnitEvolutionMilestones.HasMilestones(unitData.unitNumber);
        bool unlocksMajorMilestone = hasMilestones &&
            UnitEvolutionMilestones.WillUnlockMajorMilestone(unitData.unitNumber, choice.currentEvolutionLevel, choice.resultEvolutionLevel);

        float statsTop = traitsTop + 28f + sectionGap;
        const float evoRibbonHeight = 28f;
        if (unlocksMajorMilestone)
        {
            GameObject evoRibbon = new GameObject("EvoUnlockRibbon");
            evoRibbon.transform.SetParent(bodyObj.transform, false);
            Image ribbonBg = evoRibbon.AddComponent<Image>();
            ribbonBg.raycastTarget = false;
            ribbonBg.type = Image.Type.Sliced;
            ribbonBg.sprite = WarmRoundedSprite.Get(
                new Color(1f, 0.78f, 0.18f, 0.95f), 10,
                new Color(1f, 0.95f, 0.55f, 0.9f), 1.5f);
            RectTransform ribbonRect = evoRibbon.GetComponent<RectTransform>();
            ribbonRect.anchorMin = new Vector2(0.5f, 1f);
            ribbonRect.anchorMax = new Vector2(0.5f, 1f);
            ribbonRect.pivot = new Vector2(0.5f, 1f);
            ribbonRect.anchoredPosition = new Vector2(0f, -statsTop);
            ribbonRect.sizeDelta = new Vector2(CardWidth - 32f, evoRibbonHeight);
            Text ribbonText = CreateText(evoRibbon.transform, GameLocalization.EvolutionRibbonUnlock, 13, FontStyle.Bold,
                new Color(0.22f, 0.12f, 0.02f, 1f), TextAnchor.MiddleCenter);
            view.ribbonText = ribbonText;
            Stretch(ribbonText.rectTransform, 0f);
            statsTop += evoRibbonHeight + sectionGap;
        }

        float statsBottom = CardTapRowBottom + CardTapRowHeight + CardStatsBottomPad;
        float statsHeight = Mathf.Max(168f, CardHeight - statsTop - statsBottom);

        GameObject statsBox = new GameObject("StatsBox");
        statsBox.transform.SetParent(bodyObj.transform, false);
        Image statsBg = statsBox.AddComponent<Image>();
        statsBg.raycastTarget = false;
        statsBg.type = Image.Type.Sliced;
        Color statsBorder = unlocksMajorMilestone
            ? new Color(1f, 0.82f, 0.28f, 0.72f)
            : new Color(1f, 0.85f, 0.55f, 0.14f);
        Color statsFill = unlocksMajorMilestone
            ? new Color(0.08f, 0.05f, 0.02f, 0.52f)
            : new Color(0f, 0f, 0f, 0.38f);
        statsBg.sprite = WarmRoundedSprite.Get(statsFill, 14, statsBorder, unlocksMajorMilestone ? 2.5f : 1.5f);
        RectTransform statsRect = statsBox.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 1f);
        statsRect.anchorMax = new Vector2(0.5f, 1f);
        statsRect.pivot = new Vector2(0.5f, 1f);
        statsRect.anchoredPosition = new Vector2(0f, -statsTop);
        statsRect.sizeDelta = new Vector2(CardWidth - 24f, statsHeight);
        statsBox.AddComponent<RectMask2D>();

        int milestoneLines = hasMilestones
            ? UnitEvolutionMilestones.CountUnlockedMilestonesAtLevel(unitData.unitNumber, choice.resultEvolutionLevel) + 2
            : 0;
        int statsFontSize = milestoneLines >= 5 ? 13 : unlocksMajorMilestone ? 14 : 14;
        float statsLineSpacing = milestoneLines >= 5 ? 1.04f : unlocksMajorMilestone ? 1.1f : 1.08f;
        Text statsText = CreateText(statsBox.transform,
            UnitCombatStats.BuildLevelUpCardPreviewRich(
                unitData.unitNumber, choice.isNewUnit,
                choice.currentEvolutionLevel, choice.resultEvolutionLevel),
            statsFontSize, FontStyle.Normal, Cream, TextAnchor.UpperLeft);
        view.statsText = statsText;
        statsText.supportRichText = true;
        statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statsText.verticalOverflow = VerticalWrapMode.Truncate;
        statsText.lineSpacing = statsLineSpacing;
        StretchWithPadding(statsText.rectTransform, 10f, 8f);

        GameObject tapRow = new GameObject("TapHintRow");
        tapRow.transform.SetParent(bodyObj.transform, false);
        Image tapBg = tapRow.AddComponent<Image>();
        tapBg.raycastTarget = false;
        tapBg.type = Image.Type.Sliced;
        tapBg.sprite = WarmRoundedSprite.Get(
            new Color(1f, 0.76f, 0.28f, 0.95f), 12,
            new Color(1f, 0.92f, 0.62f, 0.9f), 1.5f);
        RectTransform tapRowRect = tapRow.GetComponent<RectTransform>();
        tapRowRect.anchorMin = new Vector2(0.5f, 0f);
        tapRowRect.anchorMax = new Vector2(0.5f, 0f);
        tapRowRect.pivot = new Vector2(0.5f, 0f);
        tapRowRect.anchoredPosition = new Vector2(0f, CardTapRowBottom);
        tapRowRect.sizeDelta = new Vector2(CardWidth - 36f, CardTapRowHeight);
        Text tapHint = CreateText(tapRow.transform, GameLocalization.LevelUpTapToAcquire, 15, FontStyle.Bold,
            new Color(0.24f, 0.15f, 0.05f, 1f), TextAnchor.MiddleCenter);
        view.tapHintText = tapHint;
        Stretch(tapHint.rectTransform, 0f);

        Button button = bodyObj.AddComponent<Button>();
        view.button = button;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
        colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        button.targetGraphic = bodyImage;

        UnitData captured = unitData;
        CardView capturedView = view;
        button.onClick.AddListener(() => OnCardClicked(captured, capturedView));
        AddCardHover(cardObj, glowImage);

        return view;
    }

    static Text CreateBadge(Transform parent, bool isNewUnit)
    {
        // NEW(밝은 초록+진초록 글씨)와 진화(밝은 보라+진보라 글씨) — 형광 배경에 어두운 글씨로 확실한 대비
        Color bgColor = isNewUnit
            ? new Color(0.18f, 0.94f, 0.50f, 1f)
            : new Color(0.72f, 0.42f, 1f, 1f);
        Color textColor = isNewUnit
            ? new Color(0.02f, 0.26f, 0.11f, 1f)
            : new Color(0.18f, 0.04f, 0.34f, 1f);
        Color borderColor = new Color(1f, 1f, 1f, 1f);
        string label = isNewUnit ? GameLocalization.EvolutionNewBadge4 : GameLocalization.LevelUpEvolutionBadge;
        Vector2 size = isNewUnit ? new Vector2(86f, 38f) : new Vector2(98f, 38f);

        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(parent, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(4f, -8f);
        badgeRect.sizeDelta = size;
        badgeRect.localRotation = Quaternion.Euler(0f, 0f, 6f);

        // 그림자 — 카드에서 살짝 떠 있는 느낌
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(badgeObj.transform, false);
        Image shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.raycastTarget = false;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.55f), 16, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        Stretch(shadowRect, 0f);
        shadowRect.anchoredPosition = new Vector2(3f, -4f);

        GameObject bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(badgeObj.transform, false);
        Image badgeBg = bgObj.AddComponent<Image>();
        badgeBg.raycastTarget = false;
        badgeBg.type = Image.Type.Sliced;
        badgeBg.sprite = WarmRoundedSprite.Get(bgColor, 16, borderColor, 2.5f);
        Stretch(bgObj.GetComponent<RectTransform>(), 0f);

        Text badgeText = CreateText(bgObj.transform, label, 18, FontStyle.Bold, textColor, TextAnchor.MiddleCenter);
        Stretch(badgeText.rectTransform, 0f);
        return badgeText;
    }

    static Transform CreateTraitChips(Transform parent, int unitNumber, float topOffset)
    {
        string[] traits = UnitTraitData.GetTraits(unitNumber);
        if (traits == null || traits.Length == 0) return null;

        GameObject row = new GameObject("TraitChips");
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -topOffset);
        rowRect.sizeDelta = new Vector2(CardWidth - 20f, 24f);

        int count = traits.Length;
        float gap = 6f;
        float rowWidth = CardWidth - 20f;
        float chipWidth = Mathf.Min(108f, (rowWidth - gap * (count - 1)) / count);
        float totalWidth = chipWidth * count + gap * (count - 1);
        float startX = -totalWidth * 0.5f + chipWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            string traitName = traits[i];
            Color traitColor = ShopManager.GetShopTraitTextColor(traitName);

            GameObject chip = new GameObject($"Chip_{traitName}");
            chip.transform.SetParent(row.transform, false);
            RectTransform chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0.5f, 0.5f);
            chipRect.anchorMax = new Vector2(0.5f, 0.5f);
            chipRect.pivot = new Vector2(0.5f, 0.5f);
            chipRect.anchoredPosition = new Vector2(startX + i * (chipWidth + gap), 0f);
            chipRect.sizeDelta = new Vector2(chipWidth, 24f);

            Image chipBg = chip.AddComponent<Image>();
            chipBg.raycastTarget = false;
            chipBg.type = Image.Type.Sliced;
            chipBg.sprite = WarmRoundedSprite.Get(
                new Color(traitColor.r * 0.20f, traitColor.g * 0.20f, traitColor.b * 0.20f, 0.85f), 12,
                new Color(traitColor.r, traitColor.g, traitColor.b, 0.55f), 1.2f);

            float textLeftPad = 8f;
            Sprite iconSprite = TraitIconFactory.Get(traitName);
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(chip.transform, false);
                Image icon = iconObj.AddComponent<Image>();
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(7f, 0f);
                iconRect.sizeDelta = new Vector2(15f, 15f);
                textLeftPad = 24f;
            }

            Text chipText = CreateText(chip.transform, GameLocalization.GetTraitDisplayName(traitName), 12, FontStyle.Bold, traitColor, TextAnchor.MiddleCenter);
            RectTransform chipTextRect = chipText.rectTransform;
            chipTextRect.anchorMin = Vector2.zero;
            chipTextRect.anchorMax = Vector2.one;
            chipTextRect.offsetMin = new Vector2(textLeftPad, 0f);
            chipTextRect.offsetMax = new Vector2(-6f, 0f);
        }

        return row.transform;
    }

    static void AddCardHover(GameObject bodyObj, Image glowImage)
    {
        EventTrigger trigger = bodyObj.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (bodyObj != null) bodyObj.transform.localScale = Vector3.one * 1.05f;
            if (glowImage != null) glowImage.color = new Color(1f, 0.85f, 0.35f, 0.22f);
        });
        trigger.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            if (bodyObj != null) bodyObj.transform.localScale = Vector3.one;
            if (glowImage != null) glowImage.color = new Color(1f, 0.85f, 0.35f, 0f);
        });
        trigger.triggers.Add(exit);
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

    static void Stretch(RectTransform rect, float margin)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
    }

    static void StretchWithPadding(RectTransform rect, float horizontal, float vertical)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
    }
}
