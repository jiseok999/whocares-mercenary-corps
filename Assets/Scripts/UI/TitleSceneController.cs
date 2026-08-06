using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// 시작 씬(Title Scene) 컨트롤러
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    const float LeftPanelWidth = 750f;
    const float LeftPanelOffsetX = 100f;
    const float MenuButtonWidth = 320f;
    const float TitleCenterX = 475f;
    const float PillGap = 16f;
    const float LeftPanelEdgeFadeRatio = 0.14f;
    const int LeftPanelTextureWidth = 256;
    const int LeftPanelTextureHeight = 4;

    [Header("UI")]
    public UnityEngine.UI.Text titleText;
    private Canvas titleCanvas;
    private UnityEngine.UI.Image backgroundImage;
    private Texture2D leftPanelTexture;
    private PauseOptionsMenuView settingsMenuView;
    private Text gameStartButtonText;
    private Text archiveButtonText;
    private Text settingsButtonText;
    private Text quitButtonText;
    private MercenaryArchiveView mercenaryArchiveView;
    private QuitConfirmDialogView quitConfirmDialog;
    
    void Start()
    {
        EnsureEventSystem();
        CreateBackgroundImage();
        CreateLeftSidePanel();

        GameSettingsPrefs.ApplySavedSettings();
        MetaCurrency.EnsureLoaded();
        UnitArchiveUnlockData.EnsureLoaded();
        CharacterUpgradeData.EnsureLoaded();
        UIFontProvider.ApplyToAllText();

        CreateMenuButtons();
        mercenaryArchiveView = MercenaryArchiveView.Create(GetTitleCanvas().transform);
        CreateSettingsPopup();
        quitConfirmDialog = QuitConfirmDialogView.Create(GetTitleCanvas().transform, 350);
        CreateTitleLogoImage();
        GameLocalizationCoordinator.Register(OnLanguageChanged);
        
        // SceneLoader가 없으면 생성
        if (SceneLoader.Instance == null)
        {
            GameObject sceneLoaderObj = new GameObject("SceneLoader");
            sceneLoaderObj.AddComponent<SceneLoader>();
        }
    }

    Canvas GetTitleCanvas()
    {
        if (titleCanvas != null) return titleCanvas;

        GameObject existing = GameObject.Find("TitleCanvas");
        if (existing != null)
        {
            titleCanvas = existing.GetComponent<Canvas>();
            if (titleCanvas != null) return titleCanvas;
        }

        GameObject canvasObj = new GameObject("TitleCanvas");
        titleCanvas = canvasObj.AddComponent<Canvas>();
        titleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        titleCanvas.sortingOrder = 0;
        MobileUIScaling.Configure(canvasObj.AddComponent<CanvasScaler>());
        canvasObj.AddComponent<GraphicRaycaster>();
        return titleCanvas;
    }
    
    void CreateBackgroundImage()
    {
        Canvas canvas = GetTitleCanvas();

        if (backgroundImage != null) return;

        Sprite bgSprite = Resources.Load<Sprite>("title_3");
        if (bgSprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("title_3");
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i].name == "title_3_0")
                {
                    bgSprite = sprites[i];
                    break;
                }
            }

            if (bgSprite == null && sprites.Length > 0)
            {
                bgSprite = sprites[0];
            }
        }

        GameObject bgObj = new GameObject("TitleBackground");
        bgObj.transform.SetParent(canvas.transform, false);
        backgroundImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        if (bgSprite != null)
        {
            backgroundImage.sprite = bgSprite;
            backgroundImage.color = Color.white;
        }
        else
        {
            backgroundImage.sprite = null;
            backgroundImage.color = Color.black;
            Debug.LogWarning("title_3.png 스프라이트를 찾지 못했습니다. Assets/Resources/title_3.png 확인 필요");
        }

        RectTransform rectTransform = bgObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 배경이 먼저 렌더링되도록 맨 뒤로 보냄
        bgObj.transform.SetAsFirstSibling();
    }

    void CreateLeftSidePanel()
    {
        Canvas canvas = GetTitleCanvas();
        if (canvas.transform.Find("TitleLeftPanel") != null) return;

        leftPanelTexture = CreateLeftPanelFadeTexture(
            LeftPanelTextureWidth,
            LeftPanelTextureHeight);

        GameObject panelObj = new GameObject("TitleLeftPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RawImage panelImage = panelObj.AddComponent<RawImage>();
        panelImage.texture = leftPanelTexture;
        panelImage.color = new Color(1f, 1f, 1f, 0.7f);
        panelImage.raycastTarget = false;

        CanvasRenderer canvasRenderer = panelObj.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            canvasRenderer.cullTransparentMesh = false;
        }

        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(LeftPanelOffsetX, 0f);
        rect.sizeDelta = new Vector2(LeftPanelWidth, 0f);

        // 배경 바로 위, 버튼·타이틀 텍스트 아래
        panelObj.transform.SetSiblingIndex(1);
    }

    static Texture2D CreateLeftPanelFadeTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float horizontalT = x / (float)(width - 1);
                float alpha = 1f;

                if (horizontalT < LeftPanelEdgeFadeRatio)
                {
                    float fadeT = horizontalT / LeftPanelEdgeFadeRatio;
                    fadeT = fadeT * fadeT * (3f - 2f * fadeT);
                    alpha = fadeT;
                }
                else if (horizontalT > 1f - LeftPanelEdgeFadeRatio)
                {
                    float fadeT = (1f - horizontalT) / LeftPanelEdgeFadeRatio;
                    fadeT = fadeT * fadeT * (3f - 2f * fadeT);
                    alpha = fadeT;
                }

                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        texture.Apply(false, false);
        return texture;
    }

    void OnDestroy()
    {
        GameLocalizationCoordinator.Unregister(OnLanguageChanged);
        if (leftPanelTexture != null)
        {
            Destroy(leftPanelTexture);
            leftPanelTexture = null;
        }
    }

    void OnLanguageChanged()
    {
        RefreshTitleTexts();
        if (settingsMenuView != null)
        {
            settingsMenuView.RefreshLocalizedTexts();
        }

        if (mercenaryArchiveView != null)
        {
            mercenaryArchiveView.RefreshLocalizedTexts();
        }
    }

    void RefreshTitleTexts()
    {
        Font font = UIFontProvider.Get();
        if (gameStartButtonText != null)
        {
            gameStartButtonText.font = font;
            gameStartButtonText.text = GameLocalization.TitleGameStart;
        }

        if (archiveButtonText != null)
        {
            archiveButtonText.font = font;
            archiveButtonText.text = GameLocalization.TitleMercenaryArchive;
        }

        if (settingsButtonText != null)
        {
            settingsButtonText.font = font;
            settingsButtonText.text = GameLocalization.TitleSettings;
        }

        if (quitButtonText != null)
        {
            quitButtonText.font = font;
            quitButtonText.text = GameLocalization.TitleQuit;
        }
    }

    void CreateTitleLogoImage()
    {
        Canvas canvas = GetTitleCanvas();
        Sprite logoSprite = LoadTitleTextSprite();
        if (logoSprite == null)
        {
            Debug.LogWarning("title_text 스프라이트를 찾지 못했습니다. Assets/Resources/title_text.png 확인 필요");
            return;
        }

        GameObject logoObj = new GameObject("TitleLogo");
        logoObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image logoImage = logoObj.AddComponent<UnityEngine.UI.Image>();
        logoImage.sprite = logoSprite;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        logoImage.color = Color.white;

        float aspect = logoSprite.rect.height / Mathf.Max(1f, logoSprite.rect.width);
        const float logoWidth = 580f;
        const float topPadding = 92f;
        float logoHeight = logoWidth * aspect;

        RectTransform rect = logoObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(TitleCenterX, -topPadding);
        rect.sizeDelta = new Vector2(logoWidth, logoHeight);
    }

    static Sprite LoadTitleTextSprite()
    {
        Texture2D texture = Resources.Load<Texture2D>("title_text");
        if (texture != null)
        {
            const float left = 21f;
            const float bottom = 88f;
            const float width = 983f;
            const float height = 795f;
            float clampedWidth = Mathf.Min(width, texture.width - left);
            float clampedHeight = Mathf.Min(height, texture.height - bottom);
            if (clampedWidth > 0f && clampedHeight > 0f)
            {
                return Sprite.Create(
                    texture,
                    new Rect(left, bottom, clampedWidth, clampedHeight),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>("title_text");
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == "title_text_10")
                {
                    return sprites[i];
                }
            }

            Sprite largest = null;
            float largestArea = 0f;
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null) continue;
                float area = sprite.rect.width * sprite.rect.height;
                if (area > largestArea)
                {
                    largestArea = area;
                    largest = sprite;
                }
            }

            if (largest != null)
            {
                return largest;
            }
        }

        return Resources.Load<Sprite>("title_text");
    }

    void CreateMenuButtons()
    {
        Canvas canvas = GetTitleCanvas();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float screenHeight = canvasRect != null && canvasRect.rect.height > 0f
            ? canvasRect.rect.height
            : Screen.height;
        if (screenHeight <= 0f) screenHeight = 1080f;

        const float primaryHeight = 64f;
        const float secondaryHeight = 54f;
        const float menuButtonDownOffset = 248f;
        float stackHeight = primaryHeight + PillGap + secondaryHeight + PillGap + secondaryHeight + PillGap + secondaryHeight;
        float y = -(screenHeight * 0.5f - stackHeight * 0.5f) - menuButtonDownOffset;

        y = CreatePillButton(canvas.transform, "GameStartButton", GameLocalization.TitleGameStart, TitleCenterX, y,
            MenuButtonWidth, primaryHeight, TitlePillStyle.Primary, () => LoadGameScene(), out gameStartButtonText);
        y -= PillGap;
        y = CreatePillButton(canvas.transform, "MercenaryArchiveButton", GameLocalization.TitleMercenaryArchive, TitleCenterX, y,
            MenuButtonWidth, secondaryHeight, TitlePillStyle.Secondary, OpenMercenaryArchive, out archiveButtonText);
        y -= PillGap;
        y = CreatePillButton(canvas.transform, "SettingsButton", GameLocalization.TitleSettings, TitleCenterX, y,
            300f, secondaryHeight, TitlePillStyle.Secondary, OpenSettings, out settingsButtonText);
        y -= PillGap;
        CreatePillButton(canvas.transform, "QuitButton", GameLocalization.TitleQuit, TitleCenterX, y,
            300f, secondaryHeight, TitlePillStyle.Tertiary, OpenQuitConfirm, out quitButtonText);
    }

    void OpenMercenaryArchive()
    {
        if (mercenaryArchiveView != null)
        {
            mercenaryArchiveView.Open();
        }
    }

    void OpenQuitConfirm()
    {
        if (quitConfirmDialog != null)
        {
            quitConfirmDialog.Open();
        }
    }

    enum TitlePillStyle
    {
        Primary,
        Secondary,
        Tertiary
    }

    struct TitlePillVisual
    {
        public Sprite normalSprite;
        public Sprite hoverSprite;
        public Color textColor;
        public Color hoverTextColor;
        public int fontSize;
        public float hoverScale;
    }

    static TitlePillVisual GetPillVisual(TitlePillStyle style)
    {
        switch (style)
        {
            case TitlePillStyle.Primary:
                return new TitlePillVisual
                {
                    normalSprite = WarmRoundedSprite.Get(
                        new Color(1f, 0.72f, 0.18f, 0.96f), 28,
                        new Color(1f, 0.88f, 0.42f, 0.85f), 2.2f),
                    hoverSprite = WarmRoundedSprite.Get(
                        new Color(1f, 0.80f, 0.28f, 1f), 28,
                        new Color(1f, 0.95f, 0.55f, 1f), 2.5f),
                    textColor = new Color(0.18f, 0.08f, 0.02f, 1f),
                    hoverTextColor = new Color(0.10f, 0.04f, 0.0f, 1f),
                    fontSize = 38,
                    hoverScale = 1.04f
                };
            case TitlePillStyle.Secondary:
                return new TitlePillVisual
                {
                    normalSprite = WarmRoundedSprite.Get(
                        new Color(0.03f, 0.10f, 0.24f, 0.38f), 27,
                        new Color(0.45f, 0.80f, 1f, 0.58f), 1.8f),
                    hoverSprite = WarmRoundedSprite.Get(
                        new Color(0.05f, 0.16f, 0.34f, 0.78f), 27,
                        new Color(0.62f, 0.88f, 1f, 0.85f), 2f),
                    textColor = new Color(0.88f, 0.94f, 1f, 1f),
                    hoverTextColor = Color.white,
                    fontSize = 32,
                    hoverScale = 1.03f
                };
            default:
                return new TitlePillVisual
                {
                    normalSprite = WarmRoundedSprite.Get(
                        new Color(0.10f, 0.09f, 0.08f, 0.32f), 27,
                        new Color(0.55f, 0.52f, 0.48f, 0.48f), 1.6f),
                    hoverSprite = WarmRoundedSprite.Get(
                        new Color(0.16f, 0.14f, 0.12f, 0.62f), 27,
                        new Color(0.72f, 0.68f, 0.62f, 0.72f), 1.8f),
                    textColor = new Color(0.78f, 0.74f, 0.68f, 1f),
                    hoverTextColor = new Color(0.92f, 0.88f, 0.82f, 1f),
                    fontSize = 32,
                    hoverScale = 1.03f
                };
        }
    }

    /// <returns>다음 버튼 배치 기준 Y (현재 버튼 하단)</returns>
    float CreatePillButton(
        Transform parent,
        string name,
        string label,
        float centerX,
        float topY,
        float width,
        float height,
        TitlePillStyle style,
        System.Action onClick,
        out Text labelText)
    {
        TitlePillVisual visual = GetPillVisual(style);

        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(centerX, topY);

        UnityEngine.UI.Image image = buttonObj.AddComponent<UnityEngine.UI.Image>();
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.sprite = visual.normalSprite;
        image.color = Color.white;

        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.transition = Selectable.Transition.None;
        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        labelText = textObj.AddComponent<UnityEngine.UI.Text>();
        labelText.text = label;
        labelText.font = UIFontProvider.Get();
        labelText.fontSize = visual.fontSize;
        labelText.color = visual.textColor;
        labelText.alignment = UnityEngine.TextAnchor.MiddleCenter;
        labelText.raycastTarget = false;

        if (style == TitlePillStyle.Primary)
        {
            UnityEngine.UI.Shadow textShadow = textObj.AddComponent<UnityEngine.UI.Shadow>();
            textShadow.effectColor = new Color(1f, 0.92f, 0.55f, 0.35f);
            textShadow.effectDistance = new Vector2(0f, 1.5f);
        }

        RectTransform textRect = labelText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        AddPillHoverEffect(buttonObj, image, labelText, rect, visual);
        return topY - height;
    }

    void AddPillHoverEffect(
        GameObject target,
        UnityEngine.UI.Image image,
        UnityEngine.UI.Text text,
        RectTransform rect,
        TitlePillVisual visual)
    {
        Vector3 normalScale = Vector3.one;
        EventTrigger trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            image.sprite = visual.hoverSprite;
            text.color = visual.hoverTextColor;
            rect.localScale = normalScale * visual.hoverScale;
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            image.sprite = visual.normalSprite;
            text.color = visual.textColor;
            rect.localScale = normalScale;
        });
        trigger.triggers.Add(exit);
    }

    void CreateSettingsPopup()
    {
        Canvas canvas = GetTitleCanvas();
        settingsMenuView = PauseOptionsMenuView.Create(canvas.transform, new PauseOptionsMenuView.Config
        {
            Title = GameLocalization.SettingsTitle,
            ShowGameplayActions = false,
            OnClose = CloseSettings,
            SortingOrder = 300
        });
    }

    void OpenSettings()
    {
        if (settingsMenuView != null)
        {
            settingsMenuView.RefreshValues();
            settingsMenuView.RefreshLocalizedTexts();
            settingsMenuView.transform.SetAsLastSibling();
            settingsMenuView.gameObject.SetActive(true);
        }
    }

    void CloseSettings()
    {
        if (settingsMenuView != null)
        {
            settingsMenuView.gameObject.SetActive(false);
        }
    }

    void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null &&
            eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
    
    /// <summary>
    /// 대기 씬으로 이동
    /// </summary>
    public void LoadGameScene()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneLoader.SceneType.GameScene);
        }
        else
        {
            SceneManager.LoadScene("GameScene");
        }
    }
}

