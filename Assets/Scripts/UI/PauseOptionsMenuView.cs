using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 인게임 일시정지 / 타이틀 설정 공통 옵션 메뉴 UI.
/// </summary>
public class PauseOptionsMenuView : MonoBehaviour
{
    public struct Config
    {
        public string Title;
        public bool ShowGameplayActions;
        public Action OnClose;
        public Action OnContinue;
        public Action OnRestart;
        public Action<float> ApplyBgmVolume;
        public Action OnPartyToastDisabled;
        public int SortingOrder;
    }

    private static readonly Dictionary<string, Sprite> RoundedSpriteCache = new Dictionary<string, Sprite>();

    private Slider masterVolumeSlider;
    private Slider bgmVolumeSlider;
    private Toggle fullscreenToggle;
    private Toggle partyToastToggle;
    private Text masterValueLabel;
    private Text bgmValueLabel;
    private Text titleText;
    private Text soundSectionText;
    private Text masterVolumeLabel;
    private Text bgmVolumeLabel;
    private Text displaySectionText;
    private Text fullscreenLabel;
    private Text partyToastLabel;
    private Text languageSectionText;
    private Text continueButtonText;
    private Text restartButtonText;
    private LanguageDropdownWidget languageDropdown;
    private Action<float> applyBgmVolume;
    private Action onPartyToastDisabled;
    private bool showGameplayActions;

    public static PauseOptionsMenuView Create(Transform canvasParent, Config config)
    {
        const float cardW = 640f;
        const float cardHWithActions = 740f;
        const float cardHSettingsOnly = 640f;
        Color accentColor = new Color(1f, 0.78f, 0.30f, 1f);

        float cardH = config.ShowGameplayActions ? cardHWithActions : cardHSettingsOnly;
        string titleTextValue = string.IsNullOrEmpty(config.Title) ? GameLocalization.SettingsTitle : config.Title;
        Action onClose = config.OnClose ?? (() => { });
        Action onContinue = config.OnContinue ?? onClose;

        GameObject rootPanel = new GameObject(config.ShowGameplayActions ? "PausePanel" : "SettingsPanel");
        rootPanel.transform.SetParent(canvasParent, false);

        Canvas panelCanvas = rootPanel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = config.SortingOrder > 0 ? config.SortingOrder : 300;
        rootPanel.AddComponent<GraphicRaycaster>();

        Image dim = rootPanel.AddComponent<Image>();
        dim.color = new Color(0.06f, 0.04f, 0.025f, 0.82f);
        RectTransform rootRect = rootPanel.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;

        GameObject cardObj = new GameObject("OptionsCard");
        cardObj.transform.SetParent(rootPanel.transform, false);
        Image cardImage = cardObj.AddComponent<Image>();
        cardImage.type = Image.Type.Sliced;
        cardImage.sprite = GetPauseRoundedSprite("card", new Color(0.145f, 0.108f, 0.078f, 0.99f), 30, new Color(1f, 0.85f, 0.55f, 0.12f), 2.5f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(cardW, cardH);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one;

        GameObject shadowObj = new GameObject("CardShadow");
        shadowObj.transform.SetParent(rootPanel.transform, false);
        shadowObj.transform.SetSiblingIndex(cardObj.transform.GetSiblingIndex());
        Image shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.type = Image.Type.Sliced;
        shadowImage.raycastTarget = false;
        shadowImage.sprite = GetPauseRoundedSprite("shadow", new Color(0f, 0f, 0f, 0.35f), 40, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(cardW + 36f, cardH + 36f);
        shadowRect.anchoredPosition = new Vector2(0f, -10f);

        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(cardObj.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.type = Image.Type.Sliced;
        accentImage.raycastTarget = false;
        accentImage.sprite = GetPauseRoundedSprite("accent", accentColor, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 1f);
        accentRect.anchoredPosition = new Vector2(40f, -22f);
        accentRect.sizeDelta = new Vector2(56f, 6f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.text = titleTextValue;
        title.font = UIFontProvider.Get();
        title.fontSize = 40;
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, 0.96f, 0.89f, 1f);
        title.alignment = TextAnchor.UpperLeft;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -34f);
        titleRect.sizeDelta = new Vector2(360f, 56f);

        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(cardObj.transform, false);
        Text subtitle = subtitleObj.AddComponent<Text>();
        subtitle.text = GameLocalization.SettingsSubtitle;
        subtitle.font = UIFontProvider.Get();
        subtitle.fontSize = 18;
        subtitle.color = new Color(0.80f, 0.66f, 0.48f, 1f);
        subtitle.alignment = TextAnchor.UpperLeft;
        RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(0f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.anchoredPosition = new Vector2(42f, -84f);
        subtitleRect.sizeDelta = new Vector2(300f, 24f);

        CreateCloseButton(cardObj.transform, onClose);
        CreateDivider(cardObj.transform, 116f);
        CreateSectionLabel(cardObj.transform, GameLocalization.SettingsSoundSection, 134f, accentColor, out Text soundSectionLabel);

        PauseOptionsMenuView view = rootPanel.AddComponent<PauseOptionsMenuView>();
        view.applyBgmVolume = config.ApplyBgmVolume;
        view.onPartyToastDisabled = config.OnPartyToastDisabled;
        view.showGameplayActions = config.ShowGameplayActions;
        view.titleText = title;
        view.soundSectionText = soundSectionLabel;

        view.masterVolumeSlider = CreateSliderOption(cardObj.transform, GameLocalization.SettingsMasterVolume, 182f, GameSettingsPrefs.LoadMasterVolume(), view.OnMasterVolumeChanged, out view.masterValueLabel, out view.masterVolumeLabel);
        view.bgmVolumeSlider = CreateSliderOption(cardObj.transform, GameLocalization.SettingsBgmVolume, 244f, GameSettingsPrefs.LoadBgmVolume(), view.OnBgmVolumeChanged, out view.bgmValueLabel, out view.bgmVolumeLabel);

        CreateSectionLabel(cardObj.transform, GameLocalization.SettingsDisplaySection, 312f, accentColor, out Text displaySectionLabel);
        view.displaySectionText = displaySectionLabel;
        view.fullscreenToggle = CreateToggleOption(cardObj.transform, GameLocalization.SettingsFullscreen, 360f, GameSettingsPrefs.LoadFullscreenSetting(), view.OnFullscreenChanged, out view.fullscreenLabel);
        view.partyToastToggle = CreateToggleOption(cardObj.transform, GameLocalization.SettingsPartyToast, 420f, GameSettingsPrefs.LoadPartyToastSetting(), view.OnPartyToastChanged, out view.partyToastLabel);

        CreateSectionLabel(cardObj.transform, GameLocalization.SettingsLanguageSection, 478f, accentColor, out Text languageSectionLabel);
        view.languageSectionText = languageSectionLabel;
        view.CreateLanguageSelector(cardObj.transform, 526f);

        if (config.ShowGameplayActions)
        {
            CreateDivider(cardObj.transform, 558f);
            view.continueButtonText = CreateActionButton(cardObj.transform, GameLocalization.SettingsContinue, new Vector2(0f, 110f), new Vector2(560f, 58f),
                new Color(0.95f, 0.60f, 0.16f, 1f), onContinue);
            if (config.OnRestart != null)
            {
                view.restartButtonText = CreateActionButton(cardObj.transform, GameLocalization.SettingsRestart, new Vector2(0f, 42f), new Vector2(560f, 56f),
                    new Color(0.36f, 0.27f, 0.20f, 1f), config.OnRestart);
            }
        }

        GameLocalization.LanguageChanged += view.OnLanguageChanged;
        rootPanel.SetActive(false);
        return view;
    }

    void OnDestroy()
    {
        GameLocalization.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        RefreshLocalizedTexts();
    }

    public void RefreshLocalizedTexts()
    {
        Font font = UIFontProvider.Get();
        if (titleText != null)
        {
            titleText.font = font;
            titleText.text = showGameplayActions ? GameLocalization.PauseTitle : GameLocalization.SettingsTitle;
        }

        ApplyLabel(soundSectionText, font, GameLocalization.SettingsSoundSection);
        ApplyLabel(masterVolumeLabel, font, GameLocalization.SettingsMasterVolume);
        ApplyLabel(bgmVolumeLabel, font, GameLocalization.SettingsBgmVolume);
        ApplyLabel(displaySectionText, font, GameLocalization.SettingsDisplaySection);
        ApplyLabel(fullscreenLabel, font, GameLocalization.SettingsFullscreen);
        ApplyLabel(partyToastLabel, font, GameLocalization.SettingsPartyToast);
        ApplyLabel(languageSectionText, font, GameLocalization.SettingsLanguageSection);

        if (masterValueLabel != null) masterValueLabel.font = font;
        if (bgmValueLabel != null) bgmValueLabel.font = font;

        if (continueButtonText != null)
        {
            continueButtonText.font = font;
            continueButtonText.text = GameLocalization.SettingsContinue;
        }

        if (restartButtonText != null)
        {
            restartButtonText.font = font;
            restartButtonText.text = GameLocalization.SettingsRestart;
        }

        languageDropdown?.RefreshCaption();
    }

    static void ApplyLabel(Text label, Font font, string value)
    {
        if (label == null) return;
        label.font = font;
        label.text = value;
    }

    void CreateLanguageSelector(Transform parent, float topY)
    {
        languageDropdown = LanguageDropdownWidget.Create(parent, topY);
    }

    public static Sprite GetRoundedSprite(string key, Color fill, int cornerRadius, Color borderColor, float borderThickness)
    {
        return GetPauseRoundedSprite(key, fill, cornerRadius, borderColor, borderThickness);
    }

    public void RefreshValues()
    {
        if (masterVolumeSlider != null)
        {
            float v = GameSettingsPrefs.LoadMasterVolume();
            masterVolumeSlider.SetValueWithoutNotify(v);
            if (masterValueLabel != null)
            {
                masterValueLabel.text = Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";
            }
        }

        if (bgmVolumeSlider != null)
        {
            float v = GameSettingsPrefs.LoadBgmVolume();
            bgmVolumeSlider.SetValueWithoutNotify(v);
            if (bgmValueLabel != null)
            {
                bgmValueLabel.text = Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";
            }
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(GameSettingsPrefs.LoadFullscreenSetting());
        }

        if (partyToastToggle != null)
        {
            partyToastToggle.SetIsOnWithoutNotify(GameSettingsPrefs.LoadPartyToastSetting());
        }

        languageDropdown?.RefreshCaption();
    }

    void OnMasterVolumeChanged(float value)
    {
        GameSettingsPrefs.SaveMasterVolume(value);
    }

    void OnBgmVolumeChanged(float value)
    {
        GameSettingsPrefs.SaveBgmVolume(value, applyBgmVolume);
    }

    void OnFullscreenChanged(bool isFullscreen)
    {
        GameSettingsPrefs.SaveFullscreen(isFullscreen);
    }

    void OnPartyToastChanged(bool enabled)
    {
        GameSettingsPrefs.SavePartyToast(enabled, onPartyToastDisabled);
    }

    static void CreateCloseButton(Transform parent, Action onClose)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent, false);
        Image closeImage = closeObj.AddComponent<Image>();
        closeImage.type = Image.Type.Sliced;
        closeImage.sprite = GetPauseRoundedSprite("closeBg", new Color(1f, 0.85f, 0.55f, 0.09f), 18, new Color(1f, 0.85f, 0.55f, 0.16f), 1.5f);
        Button closeButton = closeObj.AddComponent<Button>();
        closeButton.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = closeButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.7f, 0.7f, 1f);
        cb.pressedColor = new Color(0.85f, 0.45f, 0.45f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.1f;
        closeButton.colors = cb;
        closeButton.onClick.AddListener(() => onClose?.Invoke());
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-22f, -22f);
        closeRect.sizeDelta = new Vector2(44f, 44f);

        for (int i = 0; i < 2; i++)
        {
            GameObject barObj = new GameObject(i == 0 ? "XBar1" : "XBar2");
            barObj.transform.SetParent(closeObj.transform, false);
            Image bar = barObj.AddComponent<Image>();
            bar.color = new Color(1f, 0.93f, 0.82f, 0.95f);
            bar.raycastTarget = false;
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(22f, 3.2f);
            barRect.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
        }
    }

    static void CreateDivider(Transform parent, float topY)
    {
        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(parent, false);
        Image divider = dividerObj.AddComponent<Image>();
        divider.color = new Color(1f, 0.85f, 0.55f, 0.12f);
        divider.raycastTarget = false;
        RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(0f, 1f);
        dividerRect.pivot = new Vector2(0f, 1f);
        dividerRect.anchoredPosition = new Vector2(40f, -topY);
        dividerRect.sizeDelta = new Vector2(560f, 1.5f);
    }

    static void CreateSectionLabel(Transform parent, string text, float topY, Color color, out Text label)
    {
        GameObject labelObj = new GameObject("SectionLabel");
        labelObj.transform.SetParent(parent, false);
        label = labelObj.AddComponent<Text>();
        label.text = text;
        label.font = UIFontProvider.Get();
        label.fontSize = 19;
        label.fontStyle = FontStyle.Bold;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(40f, -topY);
        labelRect.sizeDelta = new Vector2(300f, 26f);
    }

    static Slider CreateSliderOption(Transform parent, string labelText, float topY, float initialValue, UnityAction<float> onChanged, out Text valueLabel, out Text optionLabel)
    {
        GameObject labelObj = new GameObject("OptionLabel");
        labelObj.transform.SetParent(parent, false);
        optionLabel = labelObj.AddComponent<Text>();
        optionLabel.text = labelText;
        optionLabel.font = UIFontProvider.Get();
        optionLabel.fontSize = 22;
        optionLabel.color = new Color(0.93f, 0.86f, 0.74f, 1f);
        optionLabel.alignment = TextAnchor.MiddleLeft;
        optionLabel.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(40f, -topY);
        labelRect.sizeDelta = new Vector2(180f, 30f);

        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(parent, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(initialValue);
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(230f, -topY);
        sliderRect.sizeDelta = new Vector2(286f, 16f);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.sprite = GetPauseRoundedSprite("sliderTrack", new Color(1f, 0.88f, 0.66f, 0.13f), 8, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fill = fillObj.AddComponent<Image>();
        fill.type = Image.Type.Sliced;
        fill.sprite = GetPauseRoundedSprite("sliderFill", new Color(1f, 0.78f, 0.30f, 1f), 8, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleSlideAreaObj = new GameObject("Handle Slide Area");
        handleSlideAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform handleSlideAreaRect = handleSlideAreaObj.AddComponent<RectTransform>();
        handleSlideAreaRect.anchorMin = Vector2.zero;
        handleSlideAreaRect.anchorMax = Vector2.one;
        handleSlideAreaRect.offsetMin = Vector2.zero;
        handleSlideAreaRect.offsetMax = Vector2.zero;

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleSlideAreaObj.transform, false);
        Image handle = handleObj.AddComponent<Image>();
        handle.sprite = GetPauseRoundedSprite("handle", new Color(1f, 0.98f, 0.92f, 1f), 12, new Color(1f, 0.70f, 0.22f, 1f), 2.5f);
        handle.type = Image.Type.Simple;
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 24f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject valueObj = new GameObject(labelText + "_Value");
        valueObj.transform.SetParent(parent, false);
        valueLabel = valueObj.AddComponent<Text>();
        valueLabel.text = Mathf.RoundToInt(Mathf.Clamp01(initialValue) * 100f) + "%";
        valueLabel.font = UIFontProvider.Get();
        valueLabel.fontSize = 20;
        valueLabel.fontStyle = FontStyle.Bold;
        valueLabel.color = new Color(1f, 0.78f, 0.30f, 1f);
        valueLabel.alignment = TextAnchor.MiddleRight;
        valueLabel.raycastTarget = false;
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 1f);
        valueRect.anchorMax = new Vector2(0f, 1f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(600f, -topY);
        valueRect.sizeDelta = new Vector2(60f, 30f);

        Text capturedLabel = valueLabel;
        slider.onValueChanged.AddListener(v => capturedLabel.text = Mathf.RoundToInt(v * 100f) + "%");
        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    static Toggle CreateToggleOption(Transform parent, string labelText, float topY, bool initialValue, UnityAction<bool> onChanged, out Text optionLabel)
    {
        GameObject labelObj = new GameObject("OptionLabel");
        labelObj.transform.SetParent(parent, false);
        optionLabel = labelObj.AddComponent<Text>();
        optionLabel.text = labelText;
        optionLabel.font = UIFontProvider.Get();
        optionLabel.fontSize = 22;
        optionLabel.color = new Color(0.93f, 0.86f, 0.74f, 1f);
        optionLabel.alignment = TextAnchor.MiddleLeft;
        optionLabel.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(40f, -topY);
        labelRect.sizeDelta = new Vector2(360f, 30f);

        const float trackW = 62f;
        const float trackH = 32f;
        const float knobSize = 24f;
        GameObject toggleObj = new GameObject("Switch");
        toggleObj.transform.SetParent(parent, false);
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.transition = Selectable.Transition.None;
        Image track = toggleObj.AddComponent<Image>();
        track.type = Image.Type.Sliced;
        track.sprite = GetPauseRoundedSprite("switchTrack", Color.white, 16, new Color(0f, 0f, 0f, 0f), 0f);
        Color switchOnColor = new Color(0.96f, 0.62f, 0.18f, 1f);
        Color switchOffColor = new Color(0.36f, 0.28f, 0.21f, 1f);
        track.color = initialValue ? switchOnColor : switchOffColor;
        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(600f, -topY);
        toggleRect.sizeDelta = new Vector2(trackW, trackH);
        toggle.targetGraphic = track;

        GameObject knobObj = new GameObject("Knob");
        knobObj.transform.SetParent(toggleObj.transform, false);
        Image knob = knobObj.AddComponent<Image>();
        knob.type = Image.Type.Simple;
        knob.sprite = GetPauseRoundedSprite("switchKnob", Color.white, 12, new Color(0f, 0f, 0f, 0.12f), 1.5f);
        knob.raycastTarget = false;
        RectTransform knobRect = knobObj.GetComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(knobSize, knobSize);

        float knobTravel = (trackW - knobSize) * 0.5f - 4f;
        knobRect.anchoredPosition = new Vector2(initialValue ? knobTravel : -knobTravel, 0f);

        PauseSwitchVisual switchVisual = toggleObj.AddComponent<PauseSwitchVisual>();
        switchVisual.toggle = toggle;
        switchVisual.knob = knobRect;
        switchVisual.track = track;
        switchVisual.onColor = switchOnColor;
        switchVisual.offColor = switchOffColor;
        switchVisual.knobOnX = knobTravel;
        switchVisual.knobOffX = -knobTravel;

        toggle.isOn = initialValue;
        toggle.onValueChanged.AddListener(onChanged);
        return toggle;
    }

    static Text CreateActionButton(Transform parent, string buttonLabel, Vector2 anchoredPosition, Vector2 size, Color baseColor, Action onClick)
    {
        GameObject buttonObj = new GameObject("ActionButton");
        buttonObj.transform.SetParent(parent, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.sprite = GetPauseRoundedSprite("btn", Color.white, 14, new Color(0f, 0f, 0f, 0f), 0f);
        buttonImage.color = baseColor;
        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = buttonLabel;
        text.font = UIFontProvider.Get();
        text.fontSize = 25;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.98f, 0.93f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return text;
    }

    static Sprite GetPauseRoundedSprite(string key, Color fill, int cornerRadius, Color borderColor, float borderThickness)
    {
        if (RoundedSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        int r = Mathf.Max(2, cornerRadius);
        int size = r * 2 + 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float half = size / 2f;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float dx = Mathf.Abs(px) - (half - r);
                float dy = Mathf.Abs(py) - (half - r);
                float qx = Mathf.Max(dx, 0f);
                float qy = Mathf.Max(dy, 0f);
                float dist = Mathf.Sqrt(qx * qx + qy * qy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;

                float outerCoverage = Mathf.Clamp01(0.5f - dist);
                float innerCoverage = Mathf.Clamp01(0.5f - (dist + borderThickness));

                Color rgb = Color.Lerp(borderColor, fill, innerCoverage);
                float alpha = outerCoverage * Mathf.Lerp(borderColor.a, fill.a, innerCoverage);
                pixels[y * size + x] = new Color(rgb.r, rgb.g, rgb.b, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(r, r, r, r));
        RoundedSpriteCache[key] = sprite;
        return sprite;
    }
}
