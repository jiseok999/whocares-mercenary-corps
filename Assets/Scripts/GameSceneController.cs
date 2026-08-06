using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// 게임 씬 초기화 컨트롤러
/// </summary>
public class GameSceneController : MonoBehaviour
{
    [Header("Zombie Spawner")]
    public GameObject zombiePrefab; // 좀비 프리팹 (없으면 자동 생성)
    public float spawnXOffset = 2f; // 카메라 오른쪽에서 얼마나 떨어져서 스폰할지
    
    [Header("Grid Settings")]
    public int gridRows = 5;
    public int gridColumns = 9;
    public float cellSize = 0.75f;
    public Vector2 gridStartPosition = new Vector2(-4f, -2f);
    
    private Camera mainCamera;
    private ZombieSpawner zombieSpawner;
    private Sprite uiPixelSprite;
    private Sprite buttonSkillSprite;
    private Texture2D buttonSkillTexture;
    private Texture2D buttonBlueTexture;
    private Texture2D buttonRedTexture;
    private RectTransform roundHudRect;
    private Canvas gameUiCanvas;
    private GuideToastUi.Handle persistentGuideToast;
    private GuideToastUi.Handle transientGuideToast;
    private Coroutine transientToastRoutine;
    private Coroutine persistentGuideFadeRoutine;
    private string lastTransientToastKey;
    private float lastTransientToastTime;

    const float ToastFadeOutDuration = 0.5f;
    const int PlayerLevelHudSortingOrder = 1;
    const int ShopUiSortingOrder = 50;
    private bool firstRoundGuideRequested;
    private bool firstRoundGuidePurchased;
    private RectTransform placementCountRoot;
    private Text placementCountText;
    private Text[] shopOddsTexts;
    private Text shopRefreshCostText;
    private Text upgradeXpCostText;
    private Text shopUpgradeLabelText;
    private Text shopRefreshLabelText;
    private Text hudExpLabelText;
    private Text hudSkillBarTitleText;

    const int ShopRefreshGoldCost = 2;
    const int ShopUpgradeGoldCost = 4;
    const float ShopActionButtonWidth = 204f;
    const float ShopActionButtonHeight = 80f;
    const float ShopActionButtonGap = 8f;
    static readonly Color ShopActionCostNormalColor = Color.white;
    static readonly Color ShopActionCostInsufficientColor = new Color(1f, 0.38f, 0.38f, 1f);
    private AudioSource bgmSource;
    private readonly List<AudioClip> bgmClips = new List<AudioClip>();
    private int currentBgmIndex = 0;
    private SkillManager skillManager;
    private GameObject traitSkillBarRoot;
    private RectTransform traitSkillBarRect;
    private RectTransform traitSkillSlotsRoot;
    private readonly List<TraitSkillSlotUi> traitSkillSlots = new List<TraitSkillSlotUi>();
    private string lastTraitSkillSignature = string.Empty;

    /// <summary>조합(속성) 발동 시 우상단 특수 스킬 바 — false면 UI·SkillManager 등록 없음.</summary>
    static readonly bool EnableTraitSpecialSkillBar = false;

    /// <summary>
    /// 기존 하단 UI(UiDown·상점·골드 HUD·ShopOdds·버튼 등).
    /// false면 하단에 경험치 HUD만 표시. true로 되돌리면 레거시 UI 복구.
    /// </summary>
    static readonly bool EnableLegacyBottomUi = false;

    sealed class TraitSkillSlotUi
    {
        public GameObject root;
        public Button button;
        public Image bg;
        public Image icon;
        public Image armedFrame;
        public Image cooldownOverlay;
        public Text cooldownLabel;
        public Text label;
        public TraitSpecialSkillRegistry.SkillBinding binding;
    }
    private PauseOptionsMenuView pauseOptionsMenuView;
    private bool isPauseMenuOpen = false;
    private Texture2D customMouseCursorTexture;
    private bool customMouseCursorTextureOwned;
    private GameObject roundWaveTooltipObj;
    private RectTransform roundWaveTooltipRect;
    private Image roundWaveTooltipIconImage;
    private Text roundWaveTooltipTitleText;
    private Text roundWaveTooltipBodyText;
    private readonly List<RectTransform> roundWaveSlotRects = new List<RectTransform>(10);
    private RectTransform roundWaveIconsRect;
    private int roundWaveUiSlotCount = 10;
    private float roundWaveUiSlotWidth = 34f;
    private float roundWaveUiSlotGap = 12f;
    private int hoveredRoundWaveSlotIndex = -1;
    private int roundWaveTooltipTargetRound = -1;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
            mainCamera.orthographic = true;
        }
        if (mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        // UI 입력이 먹도록 이벤트 시스템 보장
        EnsureEventSystem();
        UIFontProvider.ApplyToAllText();
        GameSettingsPrefs.ApplySavedSettings();
        ApplyCustomMouseCursor();
        
        // 카메라 위치 설정
        mainCamera.transform.position = new Vector3(0, 0, -10);
        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = 5f;
        }

        // 화면 해상도를 16:9 고정 프레임으로 맞춘다(다른 비율 화면은 레터박스 처리).
        GameCameraFit.ConfigureLetterbox(mainCamera);

        CreateBackgroundImage();
        
        // 좀비 프리팹이 없으면 간단한 좀비 생성
        if (zombiePrefab == null)
        {
            zombiePrefab = CreateSimpleZombiePrefab();
            // 프리팹이 파괴되지 않도록 보호
            DontDestroyOnLoad(zombiePrefab);
            // 프리팹을 비활성화하여 보관 (실제 인스턴스는 활성화된 것을 사용)
            zombiePrefab.SetActive(false);
        }
        
        // 좀비 스포너 생성
        CreateZombieSpawner();
        
        // BoardManager 생성 (롤토체스 스타일 보드)
        CreateBoardManager();
        
        // GridManager 생성 (식물 배치를 위해 - 필요시 사용)
        // CreateGridManager();
        
        // GameManager 확인/생성
        EnsureGameManager();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.showPartyToast = GameSettingsPrefs.LoadPartyToastSetting();
        }
        
        // 라운드 UI 생성
        CreateRoundUI();
        CreateBottomExperienceHud();

        /*
         * ── 레거시 하단 UI (EnableLegacyBottomUi = true 시 복구) ──
         * UiDown, 상점 슬롯, 새로고침/경험치 구매 버튼, ShopOdds, PlayerLevelHud, BreakTimeOverlay
         */
        if (EnableLegacyBottomUi)
        {
            CreateBottomUiDown();
            EnsureShopManager();
            CreateShopUI();
            CreateShopButtons();

            if (gameUiCanvas != null && roundHudRect != null)
            {
                BreakTimeOverlayController.Ensure(gameUiCanvas, roundHudRect);
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.EnsureShopVisible();
                StartCoroutine(EnsureShopVisibleAfterLayout());
            }

            Canvas hudCanvas = gameUiCanvas != null ? gameUiCanvas : FindMainRootCanvas();
            if (hudCanvas != null)
            {
                PlacePlayerLevelHudBehindShop(hudCanvas);
            }

            PlaceLevelNameBackgroundBehindUiDown();
        }
        else
        {
            // 유닛 선택·레벨업 보상용 ShopManager 데이터만 초기화 (UI 없음)
            EnsureShopManager();
        }

        // 속성 조합 기반 특수 스킬 바 — 일시 비활성 (부활: EnableTraitSpecialSkillBar = true)
        if (EnableTraitSpecialSkillBar)
        {
            CreateTraitSkillBarUI();
        }
        CreatePauseUI();
        BoardBuffOverviewController.Ensure(gameUiCanvas != null ? gameUiCanvas : FindMainRootCanvas());
        CreateTraitUI(FindMainRootCanvas());
        GameLocalizationCoordinator.Register(RefreshLocalizedUI);

        // 게임씬 BGM 재생
        InitializeBgm();
    }

    public void RefreshLocalizedUI()
    {
        Font font = UIFontProvider.Get();
        if (hudExpLabelText != null)
        {
            hudExpLabelText.text = GameLocalization.HudExperience;
            UIFontProvider.ApplyFont(hudExpLabelText, font);
        }
        if (hudSkillBarTitleText != null)
        {
            hudSkillBarTitleText.text = GameLocalization.HudSpecialSkills;
            UIFontProvider.ApplyFont(hudSkillBarTitleText, font);
        }
        if (shopUpgradeLabelText != null)
        {
            shopUpgradeLabelText.text = GameLocalization.HudExperience;
            UIFontProvider.ApplyFont(shopUpgradeLabelText, font);
        }
        if (shopRefreshLabelText != null)
        {
            shopRefreshLabelText.text = GameLocalization.HudRefresh;
            UIFontProvider.ApplyFont(shopRefreshLabelText, font);
        }
        for (int i = 0; i < traitSkillSlots.Count; i++)
        {
            TraitSkillSlotUi slot = traitSkillSlots[i];
            if (slot?.cooldownLabel == null) continue;
            if (GameLocalization.MatchesEnglishKey("Used", slot.cooldownLabel.text))
            {
                slot.cooldownLabel.text = GameLocalization.HudSkillUsed;
            }
            UIFontProvider.ApplyFont(slot.cooldownLabel, font);
            if (slot.label != null) UIFontProvider.ApplyFont(slot.label, font);
        }
        lastTraitSkillSignature = string.Empty;
        UpdateTraitSkillBarUI();
        RefreshPlacementCountUI();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshLocalizedUI();
        }
        if (pauseOptionsMenuView != null)
        {
            pauseOptionsMenuView.RefreshLocalizedTexts();
        }
        BreakTimeOverlayController.RefreshCopyIfVisible();
        LevelUpUnitSelectionController.RefreshLocalizedTexts();
        RefreshRoundWaveTooltipIfVisible();
        if (persistentGuideToast != null && persistentGuideToast.IsActive)
        {
            ApplyFirstRoundGuideToastContent();
        }
    }

    void RefreshRoundWaveTooltipIfVisible()
    {
        if (roundWaveTooltipTargetRound < 0 || roundWaveTooltipObj == null || !roundWaveTooltipObj.activeSelf) return;

        string partyName = RoundWaveController.GetPartyDisplayNameForRound(roundWaveTooltipTargetRound);
        string tooltip = RoundWaveController.GetPartyTooltipForRound(roundWaveTooltipTargetRound);
        Font font = UIFontProvider.Get();
        if (roundWaveTooltipTitleText != null)
        {
            UIFontProvider.ApplyFont(roundWaveTooltipTitleText, font);
            roundWaveTooltipTitleText.text = GameLocalization.RoundWaveTooltipTitleFormat(roundWaveTooltipTargetRound, partyName);
        }
        if (roundWaveTooltipBodyText != null)
        {
            UIFontProvider.ApplyFont(roundWaveTooltipBodyText, font);
            roundWaveTooltipBodyText.text = tooltip;
        }
    }

    System.Collections.IEnumerator EnsureShopVisibleAfterLayout()
    {
        yield return null;
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.EnsureShopVisible();
        }

        Canvas hudCanvas = gameUiCanvas != null ? gameUiCanvas : FindMainRootCanvas();
        if (hudCanvas != null)
        {
            PlacePlayerLevelHudBehindShop(hudCanvas);
        }

        PlaceLevelNameBackgroundBehindUiDown();
    }

    void ApplyCustomMouseCursor()
    {
        if (customMouseCursorTexture == null)
        {
            Sprite cursorSprite = LoadMousePointSprite();
            if (cursorSprite == null)
            {
                Debug.LogWarning("mouse_point 커서 리소스를 찾지 못했습니다. Assets/Resources/mouse_point 확인 필요");
                return;
            }

            if (!TryBuildCursorTexture(cursorSprite, out customMouseCursorTexture))
            {
                Debug.LogWarning(
                    "mouse_point 커서 텍스처를 생성하지 못했습니다. Assets/Resources/mouse_point.png에서 Read/Write Enabled를 켜주세요.");
                return;
            }

            customMouseCursorTextureOwned = true;
        }

        // 포인터 끝을 클릭 기준점으로 사용
        Cursor.SetCursor(customMouseCursorTexture, Vector2.zero, CursorMode.Auto);
    }

    static Sprite LoadMousePointSprite()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("mouse_point");
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == "mouse_point_0")
                {
                    return sprites[i];
                }
            }

            if (sprites.Length > 0 && sprites[0] != null)
            {
                return sprites[0];
            }
        }

        return Resources.Load<Sprite>("mouse_point");
    }

    static bool TryBuildCursorTexture(Sprite sprite, out Texture2D cursorTexture)
    {
        cursorTexture = null;
        Texture2D source = sprite.texture;
        if (source == null) return false;

        Rect rect = sprite.rect;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        if (width <= 0 || height <= 0) return false;

        Color[] pixels;
        try
        {
            pixels = source.GetPixels((int)rect.x, (int)rect.y, width, height);
        }
        catch (UnityException)
        {
            return false;
        }

        cursorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        cursorTexture.SetPixels(pixels);
        cursorTexture.Apply(false, false);
        return true;
    }

    void Update()
    {
        // 화면 크기가 바뀐 경우에만 내부에서 재계산한다(가벼운 가드).
        GameCameraFit.ConfigureLetterbox(mainCamera);

        if (EnableTraitSpecialSkillBar)
        {
            UpdateTraitSkillBarUI();
        }
        UpdateRoundWaveSlotTooltipHover();
        UpdateRoundWaveTooltipPosition();
        HandlePauseHotkey();
        MaintainFirstRoundGuideToast();
    }

    void InitializeBgm()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = false;
        bgmSource.volume = GameSettingsPrefs.LoadBgmVolume();

        bgmClips.Clear();
        AudioClip clip = Resources.Load<AudioClip>("gamescene_bgm_2");
        if (clip != null)
        {
            bgmClips.Add(clip);
        }
        else
        {
            Debug.LogWarning("gamescene_bgm_2.mp3 오디오 클립을 찾지 못했습니다. Assets/Resources/gamescene_bgm_2.mp3 확인 필요");
        }

        if (bgmClips.Count > 0)
        {
            currentBgmIndex = 0;
            PlayNextBgm();
            StartCoroutine(BgmLoop());
        }
    }

    void OnDestroy()
    {
        GameLocalizationCoordinator.Unregister(RefreshLocalizedUI);
        if (isPauseMenuOpen)
        {
            Time.timeScale = 1f;
        }

        if (customMouseCursorTextureOwned && customMouseCursorTexture != null)
        {
            Destroy(customMouseCursorTexture);
            customMouseCursorTexture = null;
        }
    }

    void PlayNextBgm()
    {
        if (bgmClips.Count == 0 || bgmSource == null) return;
        if (currentBgmIndex >= bgmClips.Count) currentBgmIndex = 0;

        bgmSource.clip = bgmClips[currentBgmIndex];
        bgmSource.Play();

        currentBgmIndex++;
    }

    IEnumerator BgmLoop()
    {
        while (true)
        {
            if (bgmSource != null && !bgmSource.isPlaying && bgmClips.Count > 0)
            {
                PlayNextBgm();
            }
            yield return null;
        }
    }

    void CreateBackgroundImage()
    {
        GameObject bgObj = new GameObject("GameBackground");
        bgObj.transform.position = new Vector3(0f, 0f, 0f);
        SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        Sprite bgSprite = Resources.Load<Sprite>("BG_map");
        bgRenderer.sprite = bgSprite;
        bgRenderer.color = Color.white;
        bgRenderer.sortingOrder = -100;

        if (bgSprite != null && mainCamera != null && mainCamera.orthographic)
        {
            float worldHeight = mainCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * GameCameraFit.Aspect;
            Vector2 spriteSize = bgSprite.bounds.size;
            float scaleX = spriteSize.x > 0.0001f ? worldWidth / spriteSize.x : 1f;
            float scaleY = spriteSize.y > 0.0001f ? worldHeight / spriteSize.y : 1f;
            bgObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }

    /// <summary>
    /// 하단 경험치 전용 HUD (EnableLegacyBottomUi=false 일 때 사용).
    /// </summary>
    void CreateBottomExperienceHud()
    {
        if (EnableLegacyBottomUi) return;

        Canvas canvas = gameUiCanvas != null ? gameUiCanvas : FindMainRootCanvas();
        if (canvas == null) return;

        Color accent = new Color(1f, 0.78f, 0.30f, 1f);
        Color cream = new Color(0.93f, 0.86f, 0.74f, 1f);
        Color tan = new Color(0.72f, 0.62f, 0.48f, 1f);

        GameObject rootObj = new GameObject("BottomExperienceHud");
        rootObj.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(0f, 168f);

        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(rootObj.transform, false);
        Image shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.raycastTarget = false;
        shadowImg.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.35f), 28, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(980f, 156f);
        shadowRect.anchoredPosition = new Vector2(0f, -6f);

        GameObject shellObj = new GameObject("Shell");
        shellObj.transform.SetParent(rootObj.transform, false);
        Image shellImg = shellObj.AddComponent<Image>();
        shellImg.type = Image.Type.Sliced;
        shellImg.raycastTarget = false;
        shellImg.sprite = WarmGaugeSprite.GetTrackShell();
        RectTransform shellRect = shellObj.GetComponent<RectTransform>();
        shellRect.anchorMin = new Vector2(0.5f, 0.5f);
        shellRect.anchorMax = new Vector2(0.5f, 0.5f);
        shellRect.pivot = new Vector2(0.5f, 0.5f);
        shellRect.sizeDelta = new Vector2(960f, 148f);
        shellRect.anchoredPosition = Vector2.zero;

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(shellObj.transform, false);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.type = Image.Type.Sliced;
        panelImg.raycastTarget = false;
        panelImg.sprite = WarmRoundedSprite.Get(
            new Color(0.10f, 0.07f, 0.05f, 0.97f),
            22,
            new Color(1f, 0.82f, 0.45f, 0.24f),
            2f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(8f, 8f);
        panelRect.offsetMax = new Vector2(-8f, -8f);

        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(panelObj.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.raycastTarget = false;
        accentImage.sprite = WarmRoundedSprite.Get(accent, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(160f, 5f);

        GameObject expLabelObj = new GameObject("ExpLabel");
        expLabelObj.transform.SetParent(panelObj.transform, false);
        Text expLabel = expLabelObj.AddComponent<Text>();
        expLabel.text = GameLocalization.HudExperience;
        hudExpLabelText = expLabel;
        expLabel.font = UIFontProvider.Get();
        expLabel.fontSize = 13;
        expLabel.fontStyle = FontStyle.Bold;
        expLabel.color = tan;
        expLabel.alignment = TextAnchor.MiddleLeft;
        expLabel.raycastTarget = false;
        RectTransform expLabelRect = expLabelObj.GetComponent<RectTransform>();
        expLabelRect.anchorMin = new Vector2(0f, 1f);
        expLabelRect.anchorMax = new Vector2(0f, 1f);
        expLabelRect.pivot = new Vector2(0f, 1f);
        expLabelRect.anchoredPosition = new Vector2(24f, -14f);
        expLabelRect.sizeDelta = new Vector2(160f, 20f);

        GameObject levelTextObj = new GameObject("LevelText");
        levelTextObj.transform.SetParent(panelObj.transform, false);
        Text levelText = levelTextObj.AddComponent<Text>();
        levelText.text = GameLocalization.HudLevelFormat(1);
        levelText.font = UIFontProvider.Get();
        levelText.fontSize = 46;
        levelText.fontStyle = FontStyle.Bold;
        levelText.color = accent;
        levelText.alignment = TextAnchor.MiddleLeft;
        levelText.raycastTarget = false;
        RectTransform levelRect = levelTextObj.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0f, 1f);
        levelRect.anchorMax = new Vector2(0f, 1f);
        levelRect.pivot = new Vector2(0f, 1f);
        levelRect.anchoredPosition = new Vector2(24f, -34f);
        levelRect.sizeDelta = new Vector2(320f, 56f);

        GameObject levelExpTextObj = new GameObject("LevelExperienceText");
        levelExpTextObj.transform.SetParent(panelObj.transform, false);
        Text levelExpText = levelExpTextObj.AddComponent<Text>();
        levelExpText.text = "(0/6)";
        levelExpText.font = UIFontProvider.Get();
        levelExpText.fontSize = 30;
        levelExpText.fontStyle = FontStyle.Bold;
        levelExpText.color = cream;
        levelExpText.alignment = TextAnchor.MiddleRight;
        levelExpText.raycastTarget = false;
        RectTransform levelExpTextRect = levelExpTextObj.GetComponent<RectTransform>();
        levelExpTextRect.anchorMin = new Vector2(1f, 1f);
        levelExpTextRect.anchorMax = new Vector2(1f, 1f);
        levelExpTextRect.pivot = new Vector2(1f, 1f);
        levelExpTextRect.anchoredPosition = new Vector2(-24f, -36f);
        levelExpTextRect.sizeDelta = new Vector2(240f, 44f);

        GameObject gaugeShellObj = new GameObject("GaugeShell");
        gaugeShellObj.transform.SetParent(panelObj.transform, false);
        Image gaugeShell = gaugeShellObj.AddComponent<Image>();
        gaugeShell.sprite = WarmGaugeSprite.GetTrackShell();
        gaugeShell.type = Image.Type.Sliced;
        gaugeShell.color = Color.white;
        gaugeShell.raycastTarget = false;
        RectTransform gaugeShellRect = gaugeShellObj.GetComponent<RectTransform>();
        gaugeShellRect.anchorMin = new Vector2(0.5f, 0f);
        gaugeShellRect.anchorMax = new Vector2(0.5f, 0f);
        gaugeShellRect.pivot = new Vector2(0.5f, 0f);
        gaugeShellRect.anchoredPosition = new Vector2(0f, 18f);
        gaugeShellRect.sizeDelta = new Vector2(900f, 36f);

        GameObject gaugeBgObj = new GameObject("LevelExperienceGaugeBackground");
        gaugeBgObj.transform.SetParent(gaugeShellObj.transform, false);
        Image gaugeBg = gaugeBgObj.AddComponent<Image>();
        gaugeBg.sprite = WarmGaugeSprite.GetTrackInner(16);
        gaugeBg.type = Image.Type.Sliced;
        gaugeBg.color = Color.white;
        gaugeBg.raycastTarget = false;
        RectTransform gaugeBgRect = gaugeBgObj.GetComponent<RectTransform>();
        gaugeBgRect.anchorMin = Vector2.zero;
        gaugeBgRect.anchorMax = Vector2.one;
        gaugeBgRect.offsetMin = new Vector2(4f, 4f);
        gaugeBgRect.offsetMax = new Vector2(-4f, -4f);

        GameObject gaugeFillObj = new GameObject("LevelExperienceGaugeFill");
        gaugeFillObj.transform.SetParent(gaugeBgObj.transform, false);
        Image levelExpGaugeFill = gaugeFillObj.AddComponent<Image>();
        levelExpGaugeFill.sprite = WarmGaugeSprite.GetFill(WarmGaugeSprite.FillStyle.Gold);
        levelExpGaugeFill.color = Color.white;
        levelExpGaugeFill.type = Image.Type.Simple;
        levelExpGaugeFill.raycastTarget = false;
        RectTransform gaugeFillRect = gaugeFillObj.GetComponent<RectTransform>();
        gaugeFillRect.anchorMin = Vector2.zero;
        gaugeFillRect.anchorMax = new Vector2(0f, 1f);
        gaugeFillRect.offsetMin = new Vector2(2f, 2f);
        gaugeFillRect.offsetMax = new Vector2(-2f, -2f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.levelText = levelText;
            GameManager.Instance.levelExperienceText = levelExpText;
            GameManager.Instance.levelExperienceGaugeFill = levelExpGaugeFill;
        }
    }

    void CreateBottomUiDown()
    {
        Canvas canvas = FindMainRootCanvas();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // 하단 UI 이미지 생성 (ui_down)
        GameObject uiDownObj = new GameObject("UiDown");
        uiDownObj.transform.SetParent(canvas.transform, false);
        Image uiDownImage = uiDownObj.AddComponent<Image>();
        Sprite uiDownSprite = Resources.Load<Sprite>("UI_down_0");
        if (uiDownSprite == null)
        {
            Sprite[] uiDownSprites = Resources.LoadAll<Sprite>("UI_down");
            if (uiDownSprites != null && uiDownSprites.Length > 0)
            {
                uiDownSprite = uiDownSprites[0];
                for (int i = 0; i < uiDownSprites.Length; i++)
                {
                    if (uiDownSprites[i] != null && uiDownSprites[i].name == "UI_down_0")
                    {
                        uiDownSprite = uiDownSprites[i];
                        break;
                    }
                }
            }
        }
        if (uiDownSprite == null)
        {
            Debug.LogWarning("UI_down 스프라이트를 찾지 못했습니다. Assets/Resources/UI_down.png 확인 필요");
            Destroy(uiDownObj);
            return;
        }

        uiDownImage.sprite = uiDownSprite;
        uiDownImage.color = Color.white;

        RectTransform rect = uiDownObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(uiDownSprite.rect.width, uiDownSprite.rect.height);

        PlaceLevelNameBackgroundBehindUiDown();
    }

    void CreateTraitSkillBarUI()
    {
        if (!EnableTraitSpecialSkillBar) return;

        Canvas canvas = FindMainRootCanvas();
        if (canvas == null) return;

        EnsureSkillManager();

        traitSkillBarRoot = new GameObject("TraitSkillBar");
        traitSkillBarRoot.transform.SetParent(canvas.transform, false);
        traitSkillBarRect = traitSkillBarRoot.AddComponent<RectTransform>();
        traitSkillBarRect.anchorMin = new Vector2(1f, 1f);
        traitSkillBarRect.anchorMax = new Vector2(1f, 1f);
        traitSkillBarRect.pivot = new Vector2(1f, 1f);
        const float topRightOffset = 20f;
        const float belowDamageMeterGap = 8f;
        float damageMeterOffset = UnitDamageMeterHud.CollapsedHudHeight + belowDamageMeterGap;
        traitSkillBarRect.anchoredPosition = new Vector2(-topRightOffset, -(topRightOffset + damageMeterOffset));

        Image barBg = traitSkillBarRoot.AddComponent<Image>();
        barBg.sprite = WarmRoundedSprite.Get(
            new Color(0.145f, 0.108f, 0.078f, 0.96f),
            18,
            new Color(1f, 0.85f, 0.55f, 0.18f),
            1.4f);
        barBg.type = Image.Type.Sliced;
        barBg.raycastTarget = false;

        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(traitSkillBarRoot.transform, false);
        Image accent = accentObj.AddComponent<Image>();
        accent.color = new Color(0.96f, 0.62f, 0.18f, 1f);
        accent.raycastTarget = false;
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 3f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(traitSkillBarRoot.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = GameLocalization.HudSpecialSkills;
        hudSkillBarTitleText = titleText;
        titleText.font = UIFontProvider.Get();
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.79f, 0.30f, 1f);
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(14f, -8f);
        titleRect.sizeDelta = new Vector2(-28f, 18f);

        GameObject slotsObj = new GameObject("Slots");
        slotsObj.transform.SetParent(traitSkillBarRoot.transform, false);
        traitSkillSlotsRoot = slotsObj.AddComponent<RectTransform>();
        traitSkillSlotsRoot.anchorMin = new Vector2(0f, 0f);
        traitSkillSlotsRoot.anchorMax = new Vector2(1f, 1f);
        traitSkillSlotsRoot.offsetMin = new Vector2(12f, 10f);
        traitSkillSlotsRoot.offsetMax = new Vector2(-12f, -30f);

        traitSkillSlots.Clear();
        IReadOnlyList<TraitSpecialSkillRegistry.SkillBinding> bindings = TraitSpecialSkillRegistry.AllBindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            traitSkillSlots.Add(CreateTraitSkillSlot(traitSkillSlotsRoot, bindings[i], i));
        }

        traitSkillBarRoot.SetActive(false);
        traitSkillBarRoot.transform.SetAsLastSibling();
        UpdateTraitSkillBarUI();
    }

    void EnsureSkillManager()
    {
        if (skillManager != null) return;
        skillManager = FindFirstObjectByType<SkillManager>();
        if (skillManager == null)
        {
            GameObject skillManagerObj = new GameObject("SkillManager");
            skillManager = skillManagerObj.AddComponent<SkillManager>();
        }
    }

    TraitSkillSlotUi CreateTraitSkillSlot(Transform parent, TraitSpecialSkillRegistry.SkillBinding binding, int index)
    {
        const float slotWidth = 72f;
        const float slotHeight = 78f;
        const float slotGap = 8f;

        GameObject root = new GameObject("SkillSlot_" + binding.traitName);
        root.transform.SetParent(parent, false);
        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(slotWidth, slotHeight);
        rect.anchoredPosition = new Vector2(index * (slotWidth + slotGap), 0f);

        Image bg = root.AddComponent<Image>();
        bg.sprite = WarmRoundedSprite.Get(
            new Color(0.20f, 0.15f, 0.11f, 0.98f),
            12,
            new Color(1f, 0.85f, 0.55f, 0.14f),
            1.2f);
        bg.type = Image.Type.Sliced;

        Button button = root.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.04f, 0.96f, 1f);
        colors.pressedColor = new Color(0.88f, 0.84f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        button.targetGraphic = bg;

        GameObject armedObj = new GameObject("ArmedFrame");
        armedObj.transform.SetParent(root.transform, false);
        Image armedFrame = armedObj.AddComponent<Image>();
        armedFrame.sprite = WarmRoundedSprite.Get(
            new Color(0.96f, 0.62f, 0.18f, 0.35f),
            12,
            new Color(1f, 0.78f, 0.30f, 0.95f),
            2f);
        armedFrame.type = Image.Type.Sliced;
        armedFrame.raycastTarget = false;
        armedFrame.enabled = false;
        RectTransform armedRect = armedObj.GetComponent<RectTransform>();
        armedRect.anchorMin = Vector2.zero;
        armedRect.anchorMax = Vector2.one;
        armedRect.offsetMin = new Vector2(-2f, -2f);
        armedRect.offsetMax = new Vector2(2f, 2f);

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(root.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Sprite iconSprite = TraitSpecialSkillRegistry.GetIcon(binding.iconResource);
        icon.sprite = iconSprite;
        icon.color = iconSprite != null ? Color.white : new Color(1f, 0.79f, 0.30f, 1f);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -8f);
        iconRect.sizeDelta = new Vector2(34f, 34f);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(root.transform, false);
        Text label = labelObj.AddComponent<Text>();
        SkillData.SkillInfo info = SkillData.GetById(binding.skillId);
        label.text = info != null ? info.name : binding.traitName;
        label.font = UIFontProvider.Get();
        label.fontSize = 13;
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(0.93f, 0.86f, 0.74f, 1f);
        label.alignment = TextAnchor.LowerCenter;
        label.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -44f);

        GameObject cooldownObj = new GameObject("CooldownOverlay");
        cooldownObj.transform.SetParent(root.transform, false);
        Image cooldownOverlay = cooldownObj.AddComponent<Image>();
        cooldownOverlay.sprite = WarmRoundedSprite.Get(
            new Color(0.08f, 0.06f, 0.05f, 0.78f),
            12,
            new Color(0.45f, 0.38f, 0.32f, 0.35f),
            1f);
        cooldownOverlay.type = Image.Type.Sliced;
        cooldownOverlay.raycastTarget = false;
        cooldownOverlay.enabled = false;
        RectTransform cooldownRect = cooldownObj.GetComponent<RectTransform>();
        cooldownRect.anchorMin = Vector2.zero;
        cooldownRect.anchorMax = Vector2.one;
        cooldownRect.offsetMin = new Vector2(2f, 2f);
        cooldownRect.offsetMax = new Vector2(-2f, -2f);

        GameObject cooldownLabelObj = new GameObject("CooldownLabel");
        cooldownLabelObj.transform.SetParent(cooldownObj.transform, false);
        Text cooldownLabel = cooldownLabelObj.AddComponent<Text>();
        cooldownLabel.text = GameLocalization.HudSkillUsed;
        cooldownLabel.font = UIFontProvider.Get();
        cooldownLabel.fontSize = 11;
        cooldownLabel.fontStyle = FontStyle.Bold;
        cooldownLabel.color = new Color(0.92f, 0.82f, 0.68f, 0.95f);
        cooldownLabel.alignment = TextAnchor.MiddleCenter;
        cooldownLabel.raycastTarget = false;
        cooldownLabel.enabled = false;
        RectTransform cooldownLabelRect = cooldownLabelObj.GetComponent<RectTransform>();
        cooldownLabelRect.anchorMin = Vector2.zero;
        cooldownLabelRect.anchorMax = Vector2.one;
        cooldownLabelRect.offsetMin = Vector2.zero;
        cooldownLabelRect.offsetMax = Vector2.zero;

        root.SetActive(false);
        return new TraitSkillSlotUi
        {
            root = root,
            button = button,
            bg = bg,
            icon = icon,
            armedFrame = armedFrame,
            cooldownOverlay = cooldownOverlay,
            cooldownLabel = cooldownLabel,
            label = label,
            binding = binding
        };
    }

    void UpdateTraitSkillBarUI()
    {
        if (!EnableTraitSpecialSkillBar) return;
        if (traitSkillBarRoot == null || traitSkillSlots.Count == 0) return;

        string signature = TraitSpecialSkillRegistry.BuildActiveSignature();
        if (signature != lastTraitSkillSignature)
        {
            lastTraitSkillSignature = signature;
            RefreshTraitSkillBarRegistration();
        }

        if (skillManager != null)
        {
            skillManager.SyncArmedVisuals();
        }

        RefreshTraitSkillSlotCooldownVisuals();
    }

    void RefreshTraitSkillSlotCooldownVisuals()
    {
        for (int i = 0; i < traitSkillSlots.Count; i++)
        {
            TraitSkillSlotUi slot = traitSkillSlots[i];
            if (slot.root == null || !slot.root.activeSelf) continue;

            bool onCooldown = skillManager != null && skillManager.IsOnCooldown(slot.binding.skillId);
            if (slot.cooldownOverlay != null)
            {
                slot.cooldownOverlay.enabled = onCooldown;
            }

            if (slot.cooldownLabel != null)
            {
                slot.cooldownLabel.enabled = onCooldown;
            }

            if (slot.icon != null)
            {
                slot.icon.color = onCooldown
                    ? new Color(0.55f, 0.50f, 0.45f, 0.65f)
                    : Color.white;
            }

            if (slot.button != null)
            {
                slot.button.interactable = !onCooldown;
            }
        }
    }

    void RefreshTraitSkillBarRegistration()
    {
        var buttons = new List<Button>();
        var images = new List<Image>();
        var armedFrames = new List<Image>();
        var ids = new List<int>();
        int activeCount = 0;
        const float slotWidth = 72f;
        const float slotGap = 8f;

        for (int i = 0; i < traitSkillSlots.Count; i++)
        {
            TraitSkillSlotUi slot = traitSkillSlots[i];
            bool active = TraitManager.Instance != null
                && TraitManager.Instance.GetTraitActiveLevel(slot.binding.traitName) >= 1;
            slot.root.SetActive(active);
            if (!active) continue;

            SkillData.SkillInfo info = SkillData.GetById(slot.binding.skillId);
            if (info != null)
            {
                slot.label.text = info.name;
            }

            RectTransform slotRect = slot.root.GetComponent<RectTransform>();
            slotRect.anchoredPosition = new Vector2(activeCount * (slotWidth + slotGap), 0f);

            buttons.Add(slot.button);
            images.Add(slot.bg);
            armedFrames.Add(slot.armedFrame);
            ids.Add(slot.binding.skillId);
            activeCount++;
        }

        float barWidth = activeCount > 0
            ? 24f + activeCount * slotWidth + (activeCount - 1) * slotGap
            : 0f;
        traitSkillBarRect.sizeDelta = new Vector2(Mathf.Max(barWidth, 0f), 96f);
        traitSkillBarRoot.SetActive(activeCount > 0);

        EnsureSkillManager();
        if (skillManager != null)
        {
            skillManager.RegisterSkills(buttons.ToArray(), images.ToArray(), null, ids.ToArray(), armedFrames.ToArray());
        }
    }


    Sprite GetUIPixelSprite()
    {
        if (uiPixelSprite != null) return uiPixelSprite;
        uiPixelSprite = Resources.Load<Sprite>("UI/UI SIMPLE PIXEL UNSPLIT");
        if (uiPixelSprite == null)
        {
            Debug.LogWarning("UI SIMPLE PIXEL UNSPLIT 스프라이트를 찾지 못했습니다.");
        }
        return uiPixelSprite;
    }

    Texture2D GetButtonSkillTexture()
    {
        if (buttonSkillTexture != null) return buttonSkillTexture;
        buttonSkillTexture = Resources.Load<Texture2D>("button_skill");
        if (buttonSkillTexture == null)
        {
            Debug.LogWarning("button_skill 텍스처를 찾지 못했습니다.");
        }
        return buttonSkillTexture;
    }

    Sprite GetButtonSkillSprite()
    {
        if (buttonSkillSprite != null) return buttonSkillSprite;
        Texture2D texture = GetButtonSkillTexture();
        if (texture == null) return null;
        buttonSkillSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        return buttonSkillSprite;
    }

    Texture2D GetButtonBlueTexture()
    {
        if (buttonBlueTexture != null) return buttonBlueTexture;
        buttonBlueTexture = Resources.Load<Texture2D>("button_blue");
        if (buttonBlueTexture == null)
        {
            Debug.LogWarning("button_blue 텍스처를 찾지 못했습니다.");
        }
        return buttonBlueTexture;
    }

    Texture2D GetButtonRedTexture()
    {
        if (buttonRedTexture != null) return buttonRedTexture;
        buttonRedTexture = Resources.Load<Texture2D>("button_red");
        if (buttonRedTexture == null)
        {
            Debug.LogWarning("button_red 텍스처를 찾지 못했습니다.");
        }
        return buttonRedTexture;
    }

    void PositionShopButtonsAtBottomCenter(Canvas canvas, RectTransform shopButtonRect, RectTransform upgradeButtonRect)
    {
        if (canvas == null || shopButtonRect == null || upgradeButtonRect == null) return;

        shopButtonRect.anchorMin = new Vector2(0.5f, 0f);
        shopButtonRect.anchorMax = new Vector2(0.5f, 0f);
        shopButtonRect.pivot = new Vector2(0.5f, 0f);
        upgradeButtonRect.anchorMin = new Vector2(0.5f, 0f);
        upgradeButtonRect.anchorMax = new Vector2(0.5f, 0f);
        upgradeButtonRect.pivot = new Vector2(0.5f, 0f);

        upgradeButtonRect.anchoredPosition = new Vector2(-632f, 20f);
        shopButtonRect.anchoredPosition = new Vector2(-632f, 20f + ShopActionButtonHeight + ShopActionButtonGap);
    }
    
    private static Sprite cachedPigSprite;

    /// <summary>
    /// 간단한 좀비 프리팹을 생성합니다 (시각적 표현용)
    /// </summary>
    GameObject CreateSimpleZombiePrefab()
    {
        GameObject zombieObj = new GameObject("SimpleZombie");
        
        // SpriteRenderer 추가 (빨간색 사각형으로 표시)
        SpriteRenderer spriteRenderer = zombieObj.AddComponent<SpriteRenderer>();
        Sprite pigSprite = LoadPigSprite();
        if (pigSprite != null)
        {
            spriteRenderer.sprite = pigSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.color = Color.red;
            spriteRenderer.sprite = CreateSprite(Color.red, 32, 32); // 32x32 픽셀 스프라이트
        }
        spriteRenderer.sortingOrder = 1;
        
        ApplyZombieScale(zombieObj.transform, spriteRenderer.sprite, cellSize * 0.9f);
        
        // Collider 추가 (충돌 감지용) - 로컬 스케일이 적용되므로 1f로 설정
        BoxCollider2D collider = zombieObj.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one; // 로컬 스케일에 의해 실제 크기가 결정됨
        
        // Zombie 스크립트 추가
        Zombie zombieScript = zombieObj.AddComponent<Zombie>();
        
        return zombieObj;
    }

    Sprite LoadPigSprite()
    {
        if (cachedPigSprite != null) return cachedPigSprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>("enemy_1_walk");
        if (sprites != null && sprites.Length > 0)
        {
            System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
            cachedPigSprite = sprites[0];
            return cachedPigSprite;
        }

        return null;
    }

    void ApplyZombieScale(Transform target, Sprite sprite, float targetWorldSize)
    {
        if (target == null)
        {
            return;
        }

        float size = 1f;
        if (sprite != null)
        {
            Vector2 spriteSize = sprite.bounds.size;
            size = Mathf.Max(spriteSize.x, spriteSize.y);
        }

        if (size <= 0.0001f)
        {
            size = 1f;
        }

        float scale = targetWorldSize / size;
        target.localScale = new Vector3(scale, scale, 1f);
    }

    RectInt GetNonTransparentBounds(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * width;
            for (int x = 0; x < width; x++)
            {
                Color32 c = pixels[rowOffset + x];
                if (c.a == 0) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new RectInt(0, 0, 0, 0);
        }

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
    
    /// <summary>
    /// 간단한 스프라이트를 생성합니다
    /// </summary>
    Sprite CreateSprite(Color color, int width = 1, int height = 1)
    {
        Texture2D texture = new Texture2D(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
    }
    
    /// <summary>
    /// 좀비 스포너를 생성합니다
    /// </summary>
    void CreateZombieSpawner()
    {
        GameObject spawnerObj = new GameObject("ZombieSpawner");
        zombieSpawner = spawnerObj.AddComponent<ZombieSpawner>();
        
        // 좀비 프리팹이 null이면 다시 생성
        if (zombiePrefab == null)
        {
            zombiePrefab = CreateSimpleZombiePrefab();
            DontDestroyOnLoad(zombiePrefab);
            zombiePrefab.SetActive(false);
        }
        
        // 좀비 프리팹 배열 설정 (레거시용, 라운드 스폰 시스템 사용 시 비활성화됨)
        zombieSpawner.zombiePrefabs = new GameObject[] { zombiePrefab };
        
        // 스폰 설정
        zombieSpawner.spawnInterval = 5f;
        zombieSpawner.spawnIntervalMin = 3f;
        zombieSpawner.spawnIntervalMax = 7f;
        zombieSpawner.maxZombiesPerRow = 2;
        
        // 카메라 오른쪽 밖으로 스폰 위치 설정
        // 주의: 실시간 Screen.width/Screen.height 대신 고정 레퍼런스 종횡비를 쓴다.
        // 안드로이드는 세로로 켜졌다가 가로로 강제 회전하는 짧은 전환 구간에
        // Screen 크기가 잠깐 잘못된 값을 보고할 수 있는데, spawnX는 여기서 딱
        // 한 번만 계산되어 캐싱되므로 그 순간 오차가 게임 내내 유지되어
        // 좀비가 화면 밖 아주 먼 곳에서 스폰되는(=적이 안 보이는) 버그로 이어졌다.
        float cameraRight = mainCamera.orthographicSize * GameCameraFit.Aspect;
        zombieSpawner.spawnX = cameraRight + spawnXOffset;
        
        // 스폰 Y 위치 설정 (5행 기준)
        float cameraTop = mainCamera.orthographicSize;
        float cameraBottom = -cameraTop;
        float rowHeight = (cameraTop * 2) / gridRows;
        zombieSpawner.spawnYPositions = new float[gridRows];
        
        for (int i = 0; i < gridRows; i++)
        {
            zombieSpawner.spawnYPositions[i] = cameraBottom + (rowHeight * i) + (rowHeight * 0.5f);
        }
        
        // CSV 로더 컴포넌트 자동 추가
        ZombieDataLoader zombieDataLoader = spawnerObj.GetComponent<ZombieDataLoader>();
        if (zombieDataLoader == null)
        {
            zombieDataLoader = spawnerObj.AddComponent<ZombieDataLoader>();
        }
        zombieSpawner.zombieDataLoader = zombieDataLoader;
        
        // 라운드 스폰 로더 컴포넌트 자동 추가
        RoundSpawnLoader roundSpawnLoader = spawnerObj.GetComponent<RoundSpawnLoader>();
        if (roundSpawnLoader == null)
        {
            roundSpawnLoader = spawnerObj.AddComponent<RoundSpawnLoader>();
        }
        zombieSpawner.roundSpawnLoader = roundSpawnLoader;
        
        Debug.Log("ZombieSpawner 초기화 완료 - 라운드 웨이브 스폰");
    }
    
    /// <summary>
    /// GridManager를 생성합니다
    /// </summary>
    void CreateGridManager()
    {
        GameObject gridManagerObj = new GameObject("GridManager");
        GridManager gridManager = gridManagerObj.AddComponent<GridManager>();
        
        gridManager.rows = gridRows;
        gridManager.columns = gridColumns;
        gridManager.cellSize = cellSize;
        gridManager.gridStartPosition = gridStartPosition;
        
        // 그리드 셀 프리팹 생성
        GameObject cellPrefab = CreateGridCellPrefab();
        gridManager.gridCellPrefab = cellPrefab;
    }
    
    /// <summary>
    /// 그리드 셀 프리팹을 생성합니다
    /// </summary>
    GameObject CreateGridCellPrefab()
    {
        GameObject cellObj = new GameObject("GridCell");
        
        // SpriteRenderer 추가 (투명 또는 반투명한 표시용)
        SpriteRenderer spriteRenderer = cellObj.AddComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.1f); // 반투명
        spriteRenderer.sprite = CreateSprite(new Color(1f, 1f, 1f, 0.1f));
        spriteRenderer.sortingOrder = -1;
        
        // GridCell 스크립트 추가
        cellObj.AddComponent<GridCell>();
        
        return cellObj;
    }
    
    /// <summary>
    /// BoardManager를 생성합니다
    /// </summary>
    void CreateBoardManager()
    {
        // 인스펙터 값과 무관하게 보드/유닛 크기 고정
        cellSize = 0.75f;

        GameObject boardManagerObj = new GameObject("BoardManager");
        BoardManager boardManager = boardManagerObj.AddComponent<BoardManager>();
        
        // 좀비 크기에 맞춰 칸 크기 설정 (2배 크기로 확대)
        boardManager.cellSize = cellSize;
        boardManager.boardRows = 5;
        boardManager.boardColumns = 5;
        boardManager.benchSlots = 8;
    }
    
    /// <summary>
    /// GameManager가 있는지 확인하고 없으면 생성합니다
    /// </summary>
    void EnsureGameManager()
    {
        if (GameManager.Instance == null)
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
        }
    }
    
    /// <summary>
    /// 라운드 UI를 생성합니다 (상단에 라운드와 시간 표시)
    /// </summary>
    void CreateRoundUI()
    {
        Canvas canvas = FindMainRootCanvas();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        gameUiCanvas = canvas;
        
        // 골드 텍스트 생성 (상단 왼쪽)
        GameObject goldTextObj = new GameObject("GoldText");
        goldTextObj.transform.SetParent(canvas.transform, false);
        UnityEngine.UI.Text goldText = goldTextObj.AddComponent<UnityEngine.UI.Text>();
        goldText.text = "0";
        goldText.font = UIFontProvider.Get();
        goldText.fontSize = 40;
        goldText.color = Color.yellow; // 골드색으로 표시
        goldText.alignment = UnityEngine.TextAnchor.UpperLeft;
        
        RectTransform goldRect = goldTextObj.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0, 1);
        goldRect.anchorMax = new Vector2(0, 1);
        goldRect.pivot = new Vector2(0, 1);
        goldRect.anchoredPosition = EnableLegacyBottomUi
            ? new Vector2(810, -870)
            : new Vector2(24f, -24f);
        goldRect.sizeDelta = new Vector2(300, 50);

        // 배치 카운트 HUD (보드 상단)
        CreatePlacementCountUI(canvas);

        // 상단 라운드 HUD 루트
        GameObject roundHudObj = new GameObject("RoundHud");
        roundHudObj.transform.SetParent(canvas.transform, false);
        roundHudRect = roundHudObj.AddComponent<RectTransform>();
        roundHudRect.anchorMin = new Vector2(0.5f, 1f);
        roundHudRect.anchorMax = new Vector2(0.5f, 1f);
        roundHudRect.pivot = new Vector2(0.5f, 1f);
        roundHudRect.anchoredPosition = new Vector2(0f, -8f);
        roundHudRect.sizeDelta = new Vector2(640f, 200f);

        // 라운드 HUD 배경
        GameObject roundHudBgObj = new GameObject("Background");
        roundHudBgObj.transform.SetParent(roundHudObj.transform, false);
        Image roundHudBg = roundHudBgObj.AddComponent<Image>();
        Sprite roundHudBgSprite = Resources.Load<Sprite>("Round_ui");
        roundHudBg.sprite = roundHudBgSprite != null ? roundHudBgSprite : GetUIPixelSprite();
        roundHudBg.color = roundHudBgSprite != null ? Color.white : new Color(0.12f, 0.12f, 0.15f, 0.95f);
        roundHudBg.raycastTarget = false;
        RectTransform roundHudBgRect = roundHudBgObj.GetComponent<RectTransform>();
        roundHudBgRect.anchorMin = Vector2.zero;
        roundHudBgRect.anchorMax = Vector2.one;
        roundHudBgRect.offsetMin = Vector2.zero;
        roundHudBgRect.offsetMax = Vector2.zero;

        // ROUND 배지
        GameObject roundBadgeObj = new GameObject("RoundBadge");
        roundBadgeObj.transform.SetParent(roundHudObj.transform, false);
        Image roundBadgeImage = roundBadgeObj.AddComponent<Image>();
        Sprite roundTitleSprite = Resources.Load<Sprite>("Round_ui_title");
        roundBadgeImage.sprite = roundTitleSprite != null ? roundTitleSprite : GetUIPixelSprite();
        roundBadgeImage.color = roundTitleSprite != null ? Color.white : new Color(0.55f, 0.22f, 0.88f, 1f);
        roundBadgeImage.raycastTarget = false;
        RectTransform roundBadgeRect = roundBadgeObj.GetComponent<RectTransform>();
        roundBadgeRect.anchorMin = new Vector2(0.5f, 1f);
        roundBadgeRect.anchorMax = new Vector2(0.5f, 1f);
        roundBadgeRect.pivot = new Vector2(0.5f, 1f);
        roundBadgeRect.anchoredPosition = new Vector2(0f, -2f);
        roundBadgeRect.sizeDelta = new Vector2(240f, 54f);

        // 라운드 텍스트
        GameObject roundTextObj = new GameObject("RoundText");
        roundTextObj.transform.SetParent(roundBadgeObj.transform, false);
        UnityEngine.UI.Text roundText = roundTextObj.AddComponent<UnityEngine.UI.Text>();
        roundText.text = GameLocalization.HudRoundFormat(1);
        roundText.font = UIFontProvider.Get();
        roundText.fontSize = 52;
        roundText.resizeTextForBestFit = true;
        roundText.resizeTextMinSize = 28;
        roundText.resizeTextMaxSize = 52;
        roundText.color = Color.white;
        roundText.alignment = UnityEngine.TextAnchor.MiddleCenter;
        RectTransform roundRect = roundTextObj.GetComponent<RectTransform>();
        roundRect.anchorMin = Vector2.zero;
        roundRect.anchorMax = Vector2.one;
        roundRect.offsetMin = Vector2.zero;
        roundRect.offsetMax = Vector2.zero;

        // 상태 텍스트 (기존 timeText 연동)
        GameObject timeTextObj = new GameObject("TimeText");
        timeTextObj.transform.SetParent(roundHudObj.transform, false);
        UnityEngine.UI.Text timeText = timeTextObj.AddComponent<UnityEngine.UI.Text>();
        timeText.text = GameLocalization.HudNormalCombat;
        timeText.font = UIFontProvider.Get();
        timeText.fontSize = 34;
        timeText.resizeTextForBestFit = true;
        timeText.resizeTextMinSize = 20;
        timeText.resizeTextMaxSize = 34;
        timeText.color = new Color(1f, 0.95f, 0.25f, 1f);
        timeText.alignment = UnityEngine.TextAnchor.MiddleCenter;
        RectTransform timeRect = timeTextObj.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 1f);
        timeRect.anchorMax = new Vector2(0.5f, 1f);
        timeRect.pivot = new Vector2(0.5f, 1f);
        timeRect.anchoredPosition = new Vector2(0f, -58f);
        timeRect.sizeDelta = new Vector2(420f, 36f);

        // 웨이브 아이콘(임시 빈 슬롯) 라인
        GameObject waveIconsObj = new GameObject("WaveIcons");
        waveIconsObj.transform.SetParent(roundHudObj.transform, false);
        RectTransform waveIconsRect = waveIconsObj.AddComponent<RectTransform>();
        waveIconsRect.anchorMin = new Vector2(0.5f, 1f);
        waveIconsRect.anchorMax = new Vector2(0.5f, 1f);
        waveIconsRect.pivot = new Vector2(0.5f, 1f);
        waveIconsRect.anchoredPosition = new Vector2(0f, -134f);
        waveIconsRect.sizeDelta = new Vector2(520f, 34f);
        roundWaveIconsRect = waveIconsRect;

        int waveSlotCount = 10;
        float slotWidth = 34f;
        float slotGap = 12f;
        roundWaveUiSlotCount = waveSlotCount;
        roundWaveUiSlotWidth = slotWidth;
        roundWaveUiSlotGap = slotGap;
        Image[] waveSlotImages = new Image[waveSlotCount];
        roundWaveSlotRects.Clear();
        float totalSlotWidth = (waveSlotCount * slotWidth) + ((waveSlotCount - 1) * slotGap);
        float slotStartX = -totalSlotWidth * 0.5f + slotWidth * 0.5f;
        for (int i = 0; i < waveSlotCount; i++)
        {
            GameObject slotObj = new GameObject($"WaveSlot_{i + 1}");
            slotObj.transform.SetParent(waveIconsObj.transform, false);
            Image slotImage = slotObj.AddComponent<Image>();
            slotImage.sprite = GetUIPixelSprite();
            slotImage.color = i == waveSlotCount - 1
                ? new Color(0.3f, 0.08f, 0.08f, 0.95f)   // 마지막(해골 자리) 슬롯
                : new Color(0.45f, 0.12f, 0.12f, 0.92f); // 일반 슬롯
            slotImage.raycastTarget = true;

            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(slotStartX + i * (slotWidth + slotGap), 0f);
            slotRect.sizeDelta = new Vector2(slotWidth, slotWidth);
            waveSlotImages[i] = slotImage;
            roundWaveSlotRects.Add(slotRect);
        }

        // 현재 웨이브 인디케이터(임시)
        GameObject waveIndicatorObj = new GameObject("WaveIndicator");
        waveIndicatorObj.transform.SetParent(waveIconsObj.transform, false);
        Image waveIndicatorImage = waveIndicatorObj.AddComponent<Image>();
        Sprite waveIndicatorSprite = Resources.Load<Sprite>("round_choice_ui");
        if (waveIndicatorSprite == null)
        {
            waveIndicatorSprite = Resources.Load<Sprite>("Round_choice_ui");
        }
        waveIndicatorImage.sprite = waveIndicatorSprite != null ? waveIndicatorSprite : GetUIPixelSprite();
        waveIndicatorImage.color = waveIndicatorSprite != null ? Color.white : new Color(1f, 0.9f, 0.2f, 1f);
        waveIndicatorImage.raycastTarget = false;
        RectTransform waveIndicatorRect = waveIndicatorObj.GetComponent<RectTransform>();
        waveIndicatorRect.anchorMin = new Vector2(0.5f, 1f);
        waveIndicatorRect.anchorMax = new Vector2(0.5f, 1f);
        waveIndicatorRect.pivot = new Vector2(0.5f, 0f);
        waveIndicatorRect.anchoredPosition = new Vector2(slotStartX, 8f);
        waveIndicatorRect.sizeDelta = new Vector2(14f, 8f);

        // 하단 진행바(라운드 클리어 대기 게이지)
        GameObject timerBgObj = new GameObject("RoundTimerBackground");
        timerBgObj.transform.SetParent(roundHudObj.transform, false);
        Image timerBgImage = timerBgObj.AddComponent<Image>();
        timerBgImage.sprite = WarmGaugeSprite.GetTrackShell();
        timerBgImage.type = Image.Type.Sliced;
        timerBgImage.color = Color.white;
        timerBgImage.raycastTarget = false;
        RectTransform timerBgRect = timerBgObj.GetComponent<RectTransform>();
        timerBgRect.anchorMin = new Vector2(0.5f, 0f);
        timerBgRect.anchorMax = new Vector2(0.5f, 0f);
        timerBgRect.pivot = new Vector2(0.5f, 0f);
        timerBgRect.anchoredPosition = new Vector2(0f, 10f);
        timerBgRect.sizeDelta = new Vector2(520f, 24f);

        Text levelText = null;
        Text levelExpText = null;
        Image levelExpGaugeFill = null;

        if (EnableLegacyBottomUi)
        {
        // ── 레거시: 하단 좌측 레벨·경험치·골드 HUD (EnableLegacyBottomUi) ──
        // 레벨 텍스트 생성 (라운드 텍스트 아래)
        GameObject levelTextObj = new GameObject("LevelText");
        levelTextObj.transform.SetParent(canvas.transform, false);
        levelText = levelTextObj.AddComponent<UnityEngine.UI.Text>();
        levelText.text = GameLocalization.HudLevelFormat(1);
        levelText.font = UIFontProvider.Get();
        levelText.fontSize = 32;
        levelText.color = Color.white;
        levelText.alignment = UnityEngine.TextAnchor.UpperLeft;
        
        RectTransform levelRect = levelTextObj.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0, 1);
        levelRect.anchorMax = new Vector2(0, 1);
        levelRect.pivot = new Vector2(0, 1);
        levelRect.anchoredPosition = new Vector2(192, -805); // 추가로 우측 10 이동
        levelRect.sizeDelta = new Vector2(300, 50);

        // 유저 레벨 구간 배경(UI_down_name) - 원본 크기 유지, 다른 UI보다 뒤
        GameObject levelNameBgObj = new GameObject("LevelNameBackground");
        levelNameBgObj.transform.SetParent(canvas.transform, false);
        Image levelNameBgImage = levelNameBgObj.AddComponent<Image>();
        Sprite levelNameBgSprite = Resources.Load<Sprite>("Ui_down_name");
        if (levelNameBgSprite == null)
        {
            levelNameBgSprite = Resources.Load<Sprite>("UI_down_name");
        }
        if (levelNameBgSprite != null)
        {
            levelNameBgImage.sprite = levelNameBgSprite;
            levelNameBgImage.color = Color.white;
            levelNameBgImage.SetNativeSize();
        }
        else
        {
            levelNameBgImage.sprite = GetUIPixelSprite();
            levelNameBgImage.color = new Color(0f, 0f, 0f, 0.5f);
        }
        levelNameBgImage.raycastTarget = false;

        RectTransform levelNameBgRect = levelNameBgObj.GetComponent<RectTransform>();
        levelNameBgRect.anchorMin = new Vector2(0, 1);
        levelNameBgRect.anchorMax = new Vector2(0, 1);
        levelNameBgRect.pivot = new Vector2(0, 1);
        levelNameBgRect.anchoredPosition = new Vector2(180f, -785f);
        float levelUiInnerPaddingX = 14f;
        float levelUiContentWidth = Mathf.Max(80f, levelNameBgRect.sizeDelta.x - (levelUiInnerPaddingX * 2f));

        // Ui_down_name 내부 오른쪽 (현재/필요) 표시
        GameObject levelExpTextObj = new GameObject("LevelExperienceText");
        levelExpTextObj.transform.SetParent(levelNameBgObj.transform, false);
        levelExpText = levelExpTextObj.AddComponent<Text>();
        levelExpText.text = "(0/6)";
        levelExpText.font = UIFontProvider.Get();
        levelExpText.fontSize = 18;
        levelExpText.color = Color.white;
        levelExpText.alignment = TextAnchor.MiddleRight;

        RectTransform levelExpTextRect = levelExpTextObj.GetComponent<RectTransform>();
        levelExpTextRect.anchorMin = new Vector2(1f, 0.58f);
        levelExpTextRect.anchorMax = new Vector2(1f, 0.58f);
        levelExpTextRect.pivot = new Vector2(1f, 0.5f);
        levelExpTextRect.anchoredPosition = new Vector2(-levelUiInnerPaddingX, 0f);
        levelExpTextRect.sizeDelta = new Vector2(levelUiContentWidth, 24f);

        // 바로 아래 가로 경험치 게이지
        GameObject levelExpGaugeBgObj = new GameObject("LevelExperienceGaugeBackground");
        levelExpGaugeBgObj.transform.SetParent(levelNameBgObj.transform, false);
        Image levelExpGaugeBg = levelExpGaugeBgObj.AddComponent<Image>();
        levelExpGaugeBg.sprite = WarmGaugeSprite.GetTrackInner(10);
        levelExpGaugeBg.type = Image.Type.Sliced;
        levelExpGaugeBg.color = Color.white;
        levelExpGaugeBg.raycastTarget = false;

        RectTransform levelExpGaugeBgRect = levelExpGaugeBgObj.GetComponent<RectTransform>();
        levelExpGaugeBgRect.anchorMin = new Vector2(0.5f, 0.34f);
        levelExpGaugeBgRect.anchorMax = new Vector2(0.5f, 0.34f);
        levelExpGaugeBgRect.pivot = new Vector2(0.5f, 0.5f);
        levelExpGaugeBgRect.anchoredPosition = Vector2.zero;
        levelExpGaugeBgRect.sizeDelta = new Vector2(levelUiContentWidth, 10f);

        GameObject levelExpGaugeFillObj = new GameObject("LevelExperienceGaugeFill");
        levelExpGaugeFillObj.transform.SetParent(levelExpGaugeBgObj.transform, false);
        levelExpGaugeFill = levelExpGaugeFillObj.AddComponent<Image>();
        levelExpGaugeFill.sprite = WarmGaugeSprite.GetFill(WarmGaugeSprite.FillStyle.Gold);
        levelExpGaugeFill.color = Color.white;
        levelExpGaugeFill.type = Image.Type.Simple;
        levelExpGaugeFill.raycastTarget = false;

        RectTransform levelExpGaugeFillRect = levelExpGaugeFillObj.GetComponent<RectTransform>();
        levelExpGaugeFillRect.anchorMin = Vector2.zero;
        levelExpGaugeFillRect.anchorMax = new Vector2(0f, 1f);
        levelExpGaugeFillRect.offsetMin = new Vector2(1f, 1f);
        levelExpGaugeFillRect.offsetMax = new Vector2(-1f, -1f);

        // 골드 구간 배경(gold_ui) - Ui_down_name 오른쪽, 원본 크기 유지
        GameObject goldBgObj = new GameObject("GoldBackground");
        goldBgObj.transform.SetParent(canvas.transform, false);
        Image goldBgImage = goldBgObj.AddComponent<Image>();
        Sprite goldBgSprite = Resources.Load<Sprite>("gold_ui");
        if (goldBgSprite == null)
        {
            goldBgSprite = Resources.Load<Sprite>("Gold_ui");
        }
        if (goldBgSprite != null)
        {
            goldBgImage.sprite = goldBgSprite;
            goldBgImage.color = Color.white;
            goldBgImage.SetNativeSize();
        }
        else
        {
            goldBgImage.sprite = GetUIPixelSprite();
            goldBgImage.color = new Color(0f, 0f, 0f, 0.5f);
        }
        goldBgImage.raycastTarget = false;

        RectTransform goldBgRect = goldBgObj.GetComponent<RectTransform>();
        goldBgRect.anchorMin = new Vector2(0, 1);
        goldBgRect.anchorMax = new Vector2(0, 1);
        goldBgRect.pivot = new Vector2(0, 1);
        float goldBgX = levelNameBgRect.anchoredPosition.x + levelNameBgRect.sizeDelta.x + 12f;
        goldBgRect.anchoredPosition = new Vector2(goldBgX, levelNameBgRect.anchoredPosition.y);

        // gold_ui 내부 재화 아이콘(gold) 표시
        GameObject goldIconObj = new GameObject("GoldIcon");
        goldIconObj.transform.SetParent(goldBgObj.transform, false);
        Image goldIconImage = goldIconObj.AddComponent<Image>();
        Sprite goldIconSprite = Resources.Load<Sprite>("gold");
        if (goldIconSprite != null)
        {
            goldIconImage.sprite = goldIconSprite;
            goldIconImage.color = Color.white;
            goldIconImage.SetNativeSize();
        }
        else
        {
            goldIconImage.sprite = GetUIPixelSprite();
            goldIconImage.color = new Color(1f, 0.9f, 0.2f, 1f);
        }
        goldIconImage.raycastTarget = false;

        RectTransform goldIconRect = goldIconObj.GetComponent<RectTransform>();
        if (goldIconSprite != null)
        {
            goldIconRect.sizeDelta = goldIconRect.sizeDelta / GoldSpriteVisual.HudIconSizeDivisor;
        }
        goldIconRect.anchorMin = new Vector2(0f, 0.5f);
        goldIconRect.anchorMax = new Vector2(0f, 0.5f);
        goldIconRect.pivot = new Vector2(0f, 0.5f);
        goldIconRect.anchoredPosition = new Vector2(12f, 0f);

        float goldTextLeftPadding = goldIconRect.sizeDelta.x > 0f ? goldIconRect.sizeDelta.x - 16f : 26f;
        const float goldTextRightInset = 40f; // 숫자를 왼쪽으로 이동시키는 실제 오프셋

        // 기존 골드 텍스트를 gold_ui 내부로 이동
        goldTextObj.transform.SetParent(goldBgObj.transform, false);
        RectTransform goldTextRect = goldTextObj.GetComponent<RectTransform>();
        goldTextRect.anchorMin = Vector2.zero;
        goldTextRect.anchorMax = Vector2.one;
        goldTextRect.pivot = new Vector2(0.5f, 0.5f);
        goldTextRect.anchoredPosition = Vector2.zero;
        goldTextRect.offsetMin = new Vector2(goldTextLeftPadding, 0f);
        goldTextRect.offsetMax = new Vector2(-goldTextRightInset, 0f);
        goldText.alignment = UnityEngine.TextAnchor.MiddleRight;
        goldText.color = Color.white;
        goldText.fontSize = 32;

        EnsurePlayerLevelHudLayer(canvas, levelTextObj, goldBgObj);

        } // EnableLegacyBottomUi — 레거시 하단 HUD 끝

        GameObject timerFillTrackObj = new GameObject("RoundTimerFillTrack");
        timerFillTrackObj.transform.SetParent(timerBgObj.transform, false);
        Image timerFillTrackImage = timerFillTrackObj.AddComponent<Image>();
        timerFillTrackImage.sprite = WarmGaugeSprite.GetTrackInner(18);
        timerFillTrackImage.type = Image.Type.Sliced;
        timerFillTrackImage.color = Color.white;
        timerFillTrackImage.raycastTarget = false;
        RectTransform timerFillTrackRect = timerFillTrackObj.GetComponent<RectTransform>();
        timerFillTrackRect.anchorMin = Vector2.zero;
        timerFillTrackRect.anchorMax = Vector2.one;
        timerFillTrackRect.offsetMin = new Vector2(3f, 3f);
        timerFillTrackRect.offsetMax = new Vector2(-3f, -3f);

        GameObject timerFillObj = new GameObject("RoundTimerFill");
        timerFillObj.transform.SetParent(timerFillTrackObj.transform, false);
        Image timerFillImage = timerFillObj.AddComponent<Image>();
        timerFillImage.sprite = WarmGaugeSprite.GetFill(WarmGaugeSprite.FillStyle.Gold);
        timerFillImage.color = Color.white;
        timerFillImage.type = Image.Type.Simple;
        timerFillImage.raycastTarget = false;
        RectTransform timerFillRect = timerFillObj.GetComponent<RectTransform>();
        timerFillRect.anchorMin = Vector2.zero;
        timerFillRect.anchorMax = Vector2.one;
        timerFillRect.offsetMin = Vector2.zero;
        timerFillRect.offsetMax = Vector2.zero;

        GameObject timerCountdownObj = new GameObject("RoundTimerCountdown");
        timerCountdownObj.transform.SetParent(timerBgObj.transform, false);
        Text timerCountdownText = timerCountdownObj.AddComponent<Text>();
        timerCountdownText.text = "";
        timerCountdownText.font = UIFontProvider.Get();
        timerCountdownText.fontSize = 18;
        timerCountdownText.fontStyle = FontStyle.Bold;
        timerCountdownText.color = new Color(1f, 0.93f, 0.72f, 1f);
        timerCountdownText.alignment = TextAnchor.MiddleCenter;
        timerCountdownText.raycastTarget = false;
        RectTransform timerCountdownRect = timerCountdownObj.GetComponent<RectTransform>();
        timerCountdownRect.anchorMin = Vector2.zero;
        timerCountdownRect.anchorMax = Vector2.one;
        timerCountdownRect.offsetMin = Vector2.zero;
        timerCountdownRect.offsetMax = Vector2.zero;
        timerCountdownObj.transform.SetAsLastSibling();

        // 보스 경고 텍스트 생성 (상단 중앙 아래)
        GameObject bossTextObj = new GameObject("BossWarningText");
        bossTextObj.transform.SetParent(canvas.transform, false);
        UnityEngine.UI.Text bossText = bossTextObj.AddComponent<UnityEngine.UI.Text>();
        bossText.text = GameLocalization.HudBossAppears;
        bossText.font = UIFontProvider.Get();
        bossText.fontSize = 48;
        bossText.color = new Color(1f, 0.3f, 0.3f);
        bossText.alignment = UnityEngine.TextAnchor.UpperCenter;
        
        RectTransform bossRect = bossTextObj.GetComponent<RectTransform>();
        bossRect.anchorMin = new Vector2(0.5f, 1);
        bossRect.anchorMax = new Vector2(0.5f, 1);
        bossRect.pivot = new Vector2(0.5f, 1);
        bossRect.anchoredPosition = new Vector2(0, -80);
        bossRect.sizeDelta = new Vector2(400, 60);
        
        bossTextObj.SetActive(false);

        CreateGuideToastLayer();

        // GameManager에 UI 텍스트 연결
        if (GameManager.Instance != null)
        {
            GameManager.Instance.goldText = goldText;
            GameManager.Instance.roundText = roundText;
            GameManager.Instance.timeText = timeText;
            GameManager.Instance.roundTimerFill = timerFillImage;
            GameManager.Instance.roundTimerFillRect = timerFillRect;
            GameManager.Instance.roundTimerRootObject = timerBgObj;
            GameManager.Instance.roundTimerCountdownText = timerCountdownText;
            GameManager.Instance.levelText = levelText;
            GameManager.Instance.levelExperienceText = levelExpText;
            GameManager.Instance.levelExperienceGaugeFill = levelExpGaugeFill;
            GameManager.Instance.roundWaveIndicatorRect = waveIndicatorRect;
            GameManager.Instance.roundWaveSlotImages = waveSlotImages;
            GameManager.Instance.roundWaveSlotCount = waveSlotCount;
            GameManager.Instance.roundWaveSlotWidth = slotWidth;
            GameManager.Instance.roundWaveSlotGap = slotGap;
            GameManager.Instance.roundWaveIndicatorBaseY = 8f;
            GameManager.Instance.roundWaveIndicatorBobAmplitude = 2.2f;
            GameManager.Instance.roundWaveIndicatorBobSpeed = 3.2f;
            GameManager.Instance.bossWarningText = bossText;
        }

        UpdatePlacementCountUI();
    }

    void CreateGuideToastLayer()
    {
        if (roundHudRect == null) return;

        persistentGuideToast = GuideToastUi.Create(roundHudRect, "PersistentGuideToast");
        persistentGuideToast.ConfigureBelowRoundHud(new Vector2(760f, 118f), roundHudRect);
        persistentGuideToast.SetStyle(GuideToastUi.Style.Guide);

        transientGuideToast = GuideToastUi.Create(roundHudRect, "TransientGuideToast");
        transientGuideToast.ConfigureBelowRoundHud(new Vector2(640f, 96f), roundHudRect);
        transientGuideToast.SetStyle(GuideToastUi.Style.Info);

        if (firstRoundGuideRequested)
        {
            ApplyFirstRoundGuideToastContent();
            persistentGuideToast.Show();
        }
    }

    void RefreshGuideToastLayout()
    {
        persistentGuideToast?.ConfigureBelowRoundHud(new Vector2(760f, 118f), roundHudRect);
        transientGuideToast?.ConfigureBelowRoundHud(new Vector2(640f, 96f), roundHudRect);
    }

    public void ShowTransientToast(
        string title,
        string body,
        GuideToastUi.Style style,
        float duration)
    {
        if (transientGuideToast == null) return;

        string toastKey = $"{title}|{body}|{style}";
        if (toastKey == lastTransientToastKey && Time.unscaledTime - lastTransientToastTime < 0.35f)
        {
            return;
        }

        lastTransientToastKey = toastKey;
        lastTransientToastTime = Time.unscaledTime;

        StopTransientToastRoutine();

        RefreshGuideToastLayout();
        transientGuideToast.SetStyle(style);
        transientGuideToast.SetContent(title, body);
        transientGuideToast.Show();
        transientGuideToast.Root.transform.SetAsLastSibling();

        if (duration > 0f)
        {
            transientToastRoutine = StartCoroutine(HideTransientToastAfter(duration));
        }
    }

    public void HideTransientToast()
    {
        if (transientGuideToast == null || !transientGuideToast.IsActive)
        {
            StopTransientToastRoutine();
            return;
        }

        StopTransientToastRoutine();
        transientToastRoutine = StartCoroutine(FadeOutToastAndHide(transientGuideToast));
    }

    public void ShowPartyToast(string partyName)
    {
        ShowTransientToast(GameLocalization.ToastPartySpawn, partyName, GuideToastUi.Style.Info, 2.2f);
    }

    void StopTransientToastRoutine()
    {
        if (transientToastRoutine != null)
        {
            StopCoroutine(transientToastRoutine);
            transientToastRoutine = null;
        }
    }

    IEnumerator HideTransientToastAfter(float visibleDuration)
    {
        yield return new WaitForSecondsRealtime(visibleDuration);
        if (transientGuideToast != null && transientGuideToast.IsActive)
        {
            yield return FadeOutToastAndHide(transientGuideToast);
        }
        transientToastRoutine = null;
    }

    IEnumerator FadeOutToastAndHide(GuideToastUi.Handle toast)
    {
        if (toast == null || toast.Root == null)
        {
            yield break;
        }

        if (!toast.IsActive)
        {
            yield break;
        }

        float startAlpha = toast.Alpha;
        float elapsed = 0f;
        while (elapsed < ToastFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / ToastFadeOutDuration);
            toast.SetAlpha(Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        toast.Hide();
    }

    public void RequestFirstRoundGuideToast()
    {
        firstRoundGuideRequested = true;
        firstRoundGuidePurchased = false;
        ShowFirstRoundGuideToast();
    }

    public void SetFirstRoundGuidePurchased()
    {
        firstRoundGuidePurchased = true;
        if (persistentGuideToast != null && persistentGuideToast.IsActive)
        {
            ApplyFirstRoundGuideToastContent();
        }
    }

    public void EnsureFirstRoundGuideVisible()
    {
        if (!firstRoundGuideRequested) return;
        ShowFirstRoundGuideToast();
    }

    void MaintainFirstRoundGuideToast()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsAwaitingFirstRoundDeployment)
        {
            return;
        }

        EnsureFirstRoundGuideVisible();
    }

    public void ShowFirstRoundGuideToast()
    {
        firstRoundGuideRequested = true;
        if (persistentGuideToast == null) return;

        ApplyFirstRoundGuideToastContent();
        RefreshGuideToastLayout();
        persistentGuideToast.Show();
        persistentGuideToast.Root.transform.SetAsLastSibling();
    }

    void ApplyFirstRoundGuideToastContent()
    {
        if (persistentGuideToast == null) return;

        persistentGuideToast.SetStyle(GuideToastUi.Style.Guide);
        if (firstRoundGuidePurchased)
        {
            persistentGuideToast.SetContent(
                GameLocalization.FirstRoundGuideTitle,
                GameLocalization.FirstRoundGuideBodyDeploy);
            return;
        }

        persistentGuideToast.SetContent(
            GameLocalization.FirstRoundGuideTitle,
            GameLocalization.FirstRoundGuideBodyPurchase);
    }

    public void HideFirstRoundGuideToast()
    {
        firstRoundGuideRequested = false;
        firstRoundGuidePurchased = false;

        if (persistentGuideToast == null || !persistentGuideToast.IsActive)
        {
            return;
        }

        if (persistentGuideFadeRoutine != null)
        {
            StopCoroutine(persistentGuideFadeRoutine);
        }

        persistentGuideFadeRoutine = StartCoroutine(FadeOutPersistentGuideToast());
    }

    IEnumerator FadeOutPersistentGuideToast()
    {
        yield return FadeOutToastAndHide(persistentGuideToast);
        persistentGuideFadeRoutine = null;
    }

    void EnsureRoundWaveTooltipUI(Transform parent)
    {
        if (roundWaveTooltipObj != null) return;

        roundWaveTooltipObj = new GameObject("RoundWaveTooltip");
        roundWaveTooltipObj.transform.SetParent(parent, false);
        Image bg = roundWaveTooltipObj.AddComponent<Image>();
        TooltipTheme.ApplyStandardBackground(bg, true);

        roundWaveTooltipRect = roundWaveTooltipObj.GetComponent<RectTransform>();
        roundWaveTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        roundWaveTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        roundWaveTooltipRect.pivot = new Vector2(0.5f, 1f);
        roundWaveTooltipRect.sizeDelta = new Vector2(300f, 96f);

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(roundWaveTooltipObj.transform, false);
        roundWaveTooltipIconImage = iconObj.AddComponent<Image>();
        roundWaveTooltipIconImage.sprite = GetUIPixelSprite();
        roundWaveTooltipIconImage.color = new Color(0.45f, 0.12f, 0.12f, 0.95f);
        roundWaveTooltipIconImage.raycastTarget = false;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(10f, -8f);
        iconRect.sizeDelta = new Vector2(44f, 44f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(roundWaveTooltipObj.transform, false);
        roundWaveTooltipTitleText = titleObj.AddComponent<Text>();
        roundWaveTooltipTitleText.font = UIFontProvider.Get();
        roundWaveTooltipTitleText.fontSize = 18;
        roundWaveTooltipTitleText.fontStyle = FontStyle.Bold;
        roundWaveTooltipTitleText.color = new Color(1f, 0.95f, 0.4f, 1f);
        roundWaveTooltipTitleText.supportRichText = true;
        roundWaveTooltipTitleText.alignment = TextAnchor.UpperLeft;
        roundWaveTooltipTitleText.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(62f, -28f);
        titleRect.offsetMax = new Vector2(-10f, -4f);

        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(roundWaveTooltipObj.transform, false);
        roundWaveTooltipBodyText = bodyObj.AddComponent<Text>();
        roundWaveTooltipBodyText.font = UIFontProvider.Get();
        roundWaveTooltipBodyText.fontSize = 15;
        roundWaveTooltipBodyText.color = new Color(0.91f, 0.86f, 0.74f, 1f);
        roundWaveTooltipBodyText.supportRichText = true;
        roundWaveTooltipBodyText.alignment = TextAnchor.UpperLeft;
        roundWaveTooltipBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        roundWaveTooltipBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        roundWaveTooltipBodyText.raycastTarget = false;
        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(62f, 8f);
        bodyRect.offsetMax = new Vector2(-10f, -30f);

        roundWaveTooltipObj.SetActive(false);
    }

    void ShowRoundWaveTooltip(RectTransform slotRect, int slotIndex)
    {
        if (slotRect == null) return;
        EnsureRoundWaveTooltipUI(slotRect.parent);
        if (roundWaveTooltipObj == null || roundWaveTooltipRect == null) return;

        int currentRound = GameManager.Instance != null ? GameManager.Instance.currentRound : 1;
        int blockStart = ((Mathf.Max(1, currentRound) - 1) / 10) * 10 + 1;
        int targetRound = blockStart + Mathf.Clamp(slotIndex, 0, 9);
        roundWaveTooltipTargetRound = targetRound;
        string partyName = RoundWaveController.GetPartyDisplayNameForRound(targetRound);
        string tooltip = RoundWaveController.GetPartyTooltipForRound(targetRound);
        string iconResource = RoundWaveController.GetPartyIconResourceForRound(targetRound);

        if (roundWaveTooltipIconImage != null)
        {
            Sprite iconSprite = Resources.Load<Sprite>(iconResource);
            roundWaveTooltipIconImage.sprite = iconSprite != null ? iconSprite : GetUIPixelSprite();
            roundWaveTooltipIconImage.color = iconSprite != null
                ? Color.white
                : new Color(0.45f, 0.12f, 0.12f, 0.95f);
        }

        if (roundWaveTooltipTitleText != null)
        {
            roundWaveTooltipTitleText.text = GameLocalization.RoundWaveTooltipTitleFormat(targetRound, partyName);
        }
        if (roundWaveTooltipBodyText != null)
        {
            roundWaveTooltipBodyText.text = tooltip;
        }

        Canvas rootCanvas = slotRect.GetComponentInParent<Canvas>();
        Transform parent = rootCanvas != null ? rootCanvas.transform : slotRect.parent;
        roundWaveTooltipObj.transform.SetParent(parent, false);
        roundWaveTooltipObj.SetActive(true);
        UpdateRoundWaveTooltipPosition();
        roundWaveTooltipObj.transform.SetAsLastSibling();
    }

    void HideRoundWaveTooltip()
    {
        roundWaveTooltipTargetRound = -1;
        if (roundWaveTooltipObj != null)
        {
            roundWaveTooltipObj.SetActive(false);
        }
    }

    void UpdateRoundWaveTooltipPosition()
    {
        if (roundWaveTooltipObj == null || roundWaveTooltipRect == null) return;
        if (!roundWaveTooltipObj.activeSelf) return;
        if (hoveredRoundWaveSlotIndex < 0 || hoveredRoundWaveSlotIndex >= roundWaveSlotRects.Count) return;

        RectTransform slotRect = roundWaveSlotRects[hoveredRoundWaveSlotIndex];
        if (slotRect == null) return;

        RectTransform parentRect = roundWaveTooltipRect.parent as RectTransform;
        if (parentRect == null) return;

        Camera uiCam = null;
        Canvas parentCanvas = parentRect.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCam = parentCanvas.worldCamera;
        }

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f; // BL-BR 중점
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, bottomCenterWorld);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCam, out Vector2 localPoint))
        {
            // 슬롯 아이콘 바로 아래에 툴팁 표시
            roundWaveTooltipRect.anchoredPosition = localPoint + new Vector2(0f, -6f);
        }
    }

    void UpdateRoundWaveSlotTooltipHover()
    {
        if (isPauseMenuOpen || (SkillManager.Instance != null && SkillManager.Instance.IsSkillArmed))
        {
            if (hoveredRoundWaveSlotIndex != -1)
            {
                hoveredRoundWaveSlotIndex = -1;
                HideRoundWaveTooltip();
            }
            return;
        }

        if (roundWaveIconsRect == null || roundWaveSlotRects.Count == 0)
        {
            if (hoveredRoundWaveSlotIndex != -1)
            {
                hoveredRoundWaveSlotIndex = -1;
                HideRoundWaveTooltip();
            }
            return;
        }

        if (!TryGetPointerScreenPosition(out Vector2 pointerScreen))
        {
            if (hoveredRoundWaveSlotIndex != -1)
            {
                hoveredRoundWaveSlotIndex = -1;
                HideRoundWaveTooltip();
            }
            return;
        }

        Canvas iconsCanvas = roundWaveIconsRect.GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (iconsCanvas != null && iconsCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCam = iconsCanvas.worldCamera;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(roundWaveIconsRect, pointerScreen, uiCam))
        {
            if (hoveredRoundWaveSlotIndex != -1)
            {
                hoveredRoundWaveSlotIndex = -1;
                HideRoundWaveTooltip();
            }
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(roundWaveIconsRect, pointerScreen, uiCam, out Vector2 local))
        {
            if (hoveredRoundWaveSlotIndex != -1)
            {
                hoveredRoundWaveSlotIndex = -1;
                HideRoundWaveTooltip();
            }
            return;
        }

        int slotCount = Mathf.Max(1, roundWaveUiSlotCount);
        float slotWidth = Mathf.Max(1f, roundWaveUiSlotWidth);
        float slotGap = Mathf.Max(0f, roundWaveUiSlotGap);
        float step = slotWidth + slotGap;
        float totalWidth = slotCount * slotWidth + (slotCount - 1) * slotGap;
        float startX = -totalWidth * 0.5f + slotWidth * 0.5f;
        int hitIndex = Mathf.RoundToInt((local.x - startX) / step);
        hitIndex = Mathf.Clamp(hitIndex, 0, slotCount - 1);

        if (hitIndex < 0 || hitIndex >= roundWaveSlotRects.Count || roundWaveSlotRects[hitIndex] == null)
        {
            if (hoveredRoundWaveSlotIndex != -1)
            {
                hoveredRoundWaveSlotIndex = -1;
                HideRoundWaveTooltip();
            }
            return;
        }

        RectTransform hitRect = roundWaveSlotRects[hitIndex];
        if (!TooltipPointerHelper.IsScreenPointInsideRect(hitRect, pointerScreen))
        {
            if (hoveredRoundWaveSlotIndex != -1)
            {
                hoveredRoundWaveSlotIndex = -1;
                HideRoundWaveTooltip();
            }
            return;
        }

        if (hoveredRoundWaveSlotIndex != hitIndex || roundWaveTooltipObj == null || !roundWaveTooltipObj.activeSelf)
        {
            hoveredRoundWaveSlotIndex = hitIndex;
            ShowRoundWaveTooltip(hitRect, hitIndex);
        }
    }

    /*
    // [테스트 버튼 비활성화] 30라운드 클리어 팝업 즉시 표시
    void CreateDemoClearTestButton(Canvas canvas)
    {
        GameObject buttonObj = new GameObject("DemoClearTestButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.sprite = PauseOptionsMenuView.GetRoundedSprite("testBtn", new Color(0.95f, 0.60f, 0.16f, 0.92f), 12, new Color(1f, 0.82f, 0.45f, 0.6f), 1.5f);
        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DebugShowDemoClear();
            }
        });

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-20f, -20f);
        buttonRect.sizeDelta = new Vector2(150f, 48f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = "▶ 30R 클리어";
        text.font = UIFontProvider.Get();
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.98f, 0.93f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    */

    void CreatePauseUI()
    {
        Canvas canvas = FindMainRootCanvas();
        if (canvas == null)
        {
            return;
        }

        // [테스트 버튼 비활성화] 우측 상단 30라운드 클리어 팝업 버튼
        // CreateDemoClearTestButton(canvas);

        // 좌측 상단 일시정지 버튼
        GameObject pauseButtonObj = new GameObject("PauseButton");
        pauseButtonObj.transform.SetParent(canvas.transform, false);
        Image pauseButtonImage = pauseButtonObj.AddComponent<Image>();
        pauseButtonImage.sprite = CreateSprite(new Color(0.16f, 0.19f, 0.26f, 1f));
        pauseButtonImage.color = Color.white;
        pauseButtonImage.type = Image.Type.Sliced;
        Outline pauseOutline = pauseButtonObj.AddComponent<Outline>();
        pauseOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        pauseOutline.effectDistance = new Vector2(2f, -2f);
        Button pauseButton = pauseButtonObj.AddComponent<Button>();
        pauseButton.transition = Selectable.Transition.ColorTint;
        ColorBlock pauseColors = pauseButton.colors;
        pauseColors.normalColor = Color.white;
        pauseColors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        pauseColors.pressedColor = new Color(0.78f, 0.84f, 1f, 1f);
        pauseColors.selectedColor = Color.white;
        pauseColors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);
        pauseColors.colorMultiplier = 1f;
        pauseColors.fadeDuration = 0.08f;
        pauseButton.colors = pauseColors;
        pauseButton.onClick.AddListener(() => TogglePauseMenu(true));

        RectTransform pauseButtonRect = pauseButtonObj.GetComponent<RectTransform>();
        pauseButtonRect.anchorMin = new Vector2(0f, 1f);
        pauseButtonRect.anchorMax = new Vector2(0f, 1f);
        pauseButtonRect.pivot = new Vector2(0f, 1f);
        pauseButtonRect.anchoredPosition = new Vector2(20f, -20f);
        pauseButtonRect.sizeDelta = new Vector2(72f, 72f);

        // 아이콘(일시정지 바 2개) - 기존 리소스 없이 직접 생성
        GameObject pauseIconRootObj = new GameObject("PauseIcon");
        pauseIconRootObj.transform.SetParent(pauseButtonObj.transform, false);
        RectTransform pauseIconRootRect = pauseIconRootObj.AddComponent<RectTransform>();
        pauseIconRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        pauseIconRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        pauseIconRootRect.pivot = new Vector2(0.5f, 0.5f);
        pauseIconRootRect.anchoredPosition = Vector2.zero;
        pauseIconRootRect.sizeDelta = new Vector2(28f, 34f);

        GameObject leftBarObj = new GameObject("LeftBar");
        leftBarObj.transform.SetParent(pauseIconRootObj.transform, false);
        Image leftBarImage = leftBarObj.AddComponent<Image>();
        leftBarImage.sprite = CreateSprite(new Color(0.92f, 0.96f, 1f, 1f));
        leftBarImage.color = Color.white;
        leftBarImage.raycastTarget = false;
        RectTransform leftBarRect = leftBarObj.GetComponent<RectTransform>();
        leftBarRect.anchorMin = new Vector2(0f, 0f);
        leftBarRect.anchorMax = new Vector2(0f, 1f);
        leftBarRect.pivot = new Vector2(0f, 0.5f);
        leftBarRect.anchoredPosition = new Vector2(0f, 0f);
        leftBarRect.sizeDelta = new Vector2(10f, 0f);

        GameObject rightBarObj = new GameObject("RightBar");
        rightBarObj.transform.SetParent(pauseIconRootObj.transform, false);
        Image rightBarImage = rightBarObj.AddComponent<Image>();
        rightBarImage.sprite = CreateSprite(new Color(0.92f, 0.96f, 1f, 1f));
        rightBarImage.color = Color.white;
        rightBarImage.raycastTarget = false;
        RectTransform rightBarRect = rightBarObj.GetComponent<RectTransform>();
        rightBarRect.anchorMin = new Vector2(1f, 0f);
        rightBarRect.anchorMax = new Vector2(1f, 1f);
        rightBarRect.pivot = new Vector2(1f, 0.5f);
        rightBarRect.anchoredPosition = new Vector2(0f, 0f);
        rightBarRect.sizeDelta = new Vector2(10f, 0f);

        pauseOptionsMenuView = PauseOptionsMenuView.Create(canvas.transform, new PauseOptionsMenuView.Config
        {
            Title = GameLocalization.PauseTitle,
            ShowGameplayActions = true,
            OnClose = () => TogglePauseMenu(false),
            OnContinue = () => TogglePauseMenu(false),
            OnRestart = OnPauseRestartPressed,
            ApplyBgmVolume = ApplyBgmVolume,
            OnPartyToastDisabled = HideTransientToast,
            SortingOrder = 300
        });
    }

    bool TryGetPointerScreenPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }
#endif
        screenPos = Input.mousePosition;
        return true;
    }

    static bool TryParseBoardCellName(string cellName, out int row, out int col)
    {
        row = col = 0;
        if (string.IsNullOrEmpty(cellName) || !cellName.StartsWith("BoardCell_")) return false;
        string[] parts = cellName.Split('_');
        if (parts.Length < 3) return false;
        return int.TryParse(parts[1], out row) && int.TryParse(parts[2], out col);
    }

    static Sprite LoadSpriteWithFallback(string resourceName)
    {
        Sprite sprite = Resources.Load<Sprite>(resourceName);
        if (sprite != null) return sprite;
        Sprite[] all = Resources.LoadAll<Sprite>(resourceName);
        if (all != null && all.Length > 0) return all[0];
        return null;
    }

    Canvas FindMainRootCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas namedUi = null;
        Canvas best = null;
        float bestArea = -1f;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            if (!c.isRootCanvas) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;

            if (c.gameObject.name == "UI")
            {
                namedUi = c;
            }

            // 상점/팝업처럼 overrideSorting으로 분리된 서브 캔버스보다
            // 화면 전체를 담당하는 기본 루트 캔버스를 우선 선택.
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

        if (namedUi != null)
        {
            return namedUi;
        }

        return best != null ? best : FindFirstObjectByType<Canvas>();
    }

    void HandlePauseHotkey()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu(!isPauseMenuOpen);
        }
    }

    void HideAllTooltips()
    {
        HideRoundWaveTooltip();
        hoveredRoundWaveSlotIndex = -1;

        UnitFieldTooltip.Hide();

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.HideActiveTooltip();
        }

        if (TraitManager.Instance != null)
        {
            TraitManager.Instance.HideActiveTooltip();
        }
    }

    void TogglePauseMenu(bool open)
    {
        if (pauseOptionsMenuView == null)
        {
            return;
        }

        if (open)
        {
            pauseOptionsMenuView.RefreshValues();
            pauseOptionsMenuView.RefreshLocalizedTexts();
            isPauseMenuOpen = true;
            pauseOptionsMenuView.transform.SetAsLastSibling();
            pauseOptionsMenuView.gameObject.SetActive(true);
            Time.timeScale = 0f;
            HideAllTooltips();
        }
        else
        {
            isPauseMenuOpen = false;
            pauseOptionsMenuView.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    void OnPauseRestartPressed()
    {
        Time.timeScale = 1f;
        isPauseMenuOpen = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void ApplyBgmVolume(float value)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(value);
        }
    }

    void CreateTraitUI(Canvas canvas)
    {
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            return;
        }

        Transform existingPanel = canvas.transform.Find("TraitPanel");
        if (existingPanel != null)
        {
            Destroy(existingPanel.gameObject);
        }
        TraitManager.ReleaseInstance();

        GameObject traitPanelObj = new GameObject("TraitPanel");
        traitPanelObj.transform.SetParent(canvas.transform, false);

        Canvas traitCanvas = traitPanelObj.AddComponent<Canvas>();
        traitCanvas.overrideSorting = true;
        traitCanvas.sortingOrder = 15;
        traitPanelObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        Image panelImage = traitPanelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0f);
        panelImage.sprite = GetUIPixelSprite();
        panelImage.raycastTarget = false;
        
        RectTransform panelRect = traitPanelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(20, -259);
        panelRect.sizeDelta = new Vector2(240, TraitManager.GetTraitPanelVisibleHeight());

        TraitManager traitManager = traitPanelObj.AddComponent<TraitManager>();
        traitManager.RegisterUI(traitPanelObj.transform, null, null);
        traitPanelObj.transform.SetAsLastSibling();
    }

    
    /// <summary>
    /// 상점 UI가 없을 때 재생성합니다 (ShopManager Start 폴백용).
    /// </summary>
    public void EnsureShopUiCreated()
    {
        if (!EnableLegacyBottomUi) return;
        EnsureShopManager();
        CreateShopUI();
        CreateShopButtons();
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.EnsureShopVisible();
        }
    }

    void EnsureShopManager()
    {
        if (ShopManager.Instance != null) return;

        GameObject shopManagerObj = new GameObject("ShopManager");
        shopManagerObj.AddComponent<ShopManager>();
    }

    /// <summary>
    /// 상점 UI를 생성합니다
    /// </summary>
    void CreateShopUI()
    {
        if (!EnableLegacyBottomUi) return;
        EnsureEventSystem();
        EnsureShopManager();

        Canvas canvas = gameUiCanvas != null ? gameUiCanvas : FindMainRootCanvas();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            gameUiCanvas = canvas;
        }

        ShopManager shopManager = ShopManager.Instance;
        if (shopManager == null)
        {
            Debug.LogError("[Shop] ShopManager.Instance가 null입니다.");
            return;
        }

        if (shopManager.shopPanel != null && shopManager.shopPanel.activeInHierarchy)
        {
            shopManager.EnsureShopVisible();
            return;
        }

        Transform existingLayer = canvas.transform.Find("ShopUiLayer");
        if (existingLayer != null)
        {
            Destroy(existingLayer.gameObject);
        }

        GameObject shopLayer = new GameObject("ShopUiLayer");
        shopLayer.transform.SetParent(canvas.transform, false);
        RectTransform layerRect = shopLayer.AddComponent<RectTransform>();
        layerRect.anchorMin = new Vector2(0f, 0f);
        layerRect.anchorMax = new Vector2(1f, 0f);
        layerRect.pivot = new Vector2(0.5f, 0f);
        layerRect.anchoredPosition = Vector2.zero;
        layerRect.sizeDelta = new Vector2(0f, 272f);

        Canvas shopLayerCanvas = shopLayer.AddComponent<Canvas>();
        shopLayerCanvas.overrideSorting = true;
        shopLayerCanvas.sortingOrder = ShopUiSortingOrder;
        shopLayer.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject shopPanel = new GameObject("ShopPanel");
        shopPanel.transform.SetParent(shopLayer.transform, false);
        Image panelImage = shopPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0f);
        panelImage.sprite = null;
        panelImage.raycastTarget = false;
        CanvasGroup panelGroup = shopPanel.AddComponent<CanvasGroup>();
        panelGroup.blocksRaycasts = true;
        panelGroup.interactable = true;

        RectTransform panelRect = shopPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1400f, 252f);

        shopLayer.SetActive(true);
        shopPanel.SetActive(true);

        GameObject shopSlotsParent = new GameObject("ShopSlots");
        shopSlotsParent.transform.SetParent(shopPanel.transform, false);
        RectTransform slotsRect = shopSlotsParent.AddComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotsRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotsRect.pivot = new Vector2(0.5f, 0.5f);
        slotsRect.anchoredPosition = new Vector2(50f, -4f);
        slotsRect.sizeDelta = new Vector2(1000f, 160f);

        shopManager.shopPanel = shopPanel;
        shopManager.shopSlotsParent = shopSlotsParent.transform;
        shopManager.refreshButton = null;
        shopManager.RefreshShop(ShopManager.ShopRefreshSource.Silent);
        shopManager.EnsureShopVisible();
        PlacePlayerLevelHudBehindShop(canvas);
        PlaceLevelNameBackgroundBehindUiDown();

        Debug.Log("[Shop] ShopUiLayer 생성 완료");
    }

    void EnsurePlayerLevelHudLayer(Canvas canvas, GameObject levelTextObj, GameObject goldBgObj)
    {
        if (canvas == null) return;

        Transform hudRoot = GetOrCreatePlayerLevelHudRoot(canvas.transform);
        ReparentPlayerLevelHudElement(levelTextObj, hudRoot);
        ReparentPlayerLevelHudElement(goldBgObj, hudRoot);
        CollectOrphanedPlayerLevelHudElements(canvas.transform, hudRoot);
        ApplyPlayerLevelHudCanvasSettings(hudRoot);
    }

    /// <summary>Ui_down_name(LevelNameBackground)을 UiDown 바로 뒤(가장 아래)에 둡니다.</summary>
    public void PlaceLevelNameBackgroundBehindUiDown()
    {
        Transform levelBg = FindUiTransform("LevelNameBackground");
        if (levelBg == null)
        {
            return;
        }

        Transform uiDown = FindUiTransform("UiDown");
        Canvas canvas = gameUiCanvas != null ? gameUiCanvas : FindMainRootCanvas();
        if (uiDown != null)
        {
            Canvas uiDownCanvas = uiDown.GetComponentInParent<Canvas>();
            if (uiDownCanvas != null)
            {
                canvas = uiDownCanvas;
            }
        }

        if (canvas == null)
        {
            return;
        }

        Transform canvasRoot = canvas.transform;
        if (levelBg.parent != canvasRoot)
        {
            levelBg.SetParent(canvasRoot, true);
        }

        Canvas levelBgCanvas = levelBg.GetComponent<Canvas>();
        if (levelBgCanvas != null)
        {
            Destroy(levelBgCanvas);
        }

        levelBg.SetAsFirstSibling();

        if (uiDown != null && uiDown.parent == canvasRoot)
        {
            uiDown.SetSiblingIndex(levelBg.GetSiblingIndex() + 1);
        }
    }

    public void PlacePlayerLevelHudBehindShop(Canvas canvas)
    {
        Transform shopLayer = FindUiTransform("ShopUiLayer");
        Transform hudRoot = FindUiTransform("PlayerLevelHud");
        if (shopLayer == null || hudRoot == null)
        {
            PlaceLevelNameBackgroundBehindUiDown();
            return;
        }

        Canvas targetCanvas = shopLayer.GetComponentInParent<Canvas>();
        if (targetCanvas == null)
        {
            PlaceLevelNameBackgroundBehindUiDown();
            return;
        }

        if (hudRoot.parent != targetCanvas.transform)
        {
            hudRoot.SetParent(targetCanvas.transform, true);
        }

        CollectOrphanedPlayerLevelHudElements(targetCanvas.transform, hudRoot);
        ApplyPlayerLevelHudCanvasSettings(hudRoot);

        Canvas shopCanvas = shopLayer.GetComponent<Canvas>();
        if (shopCanvas != null)
        {
            shopCanvas.overrideSorting = true;
            shopCanvas.sortingOrder = ShopUiSortingOrder;
        }

        hudRoot.SetAsFirstSibling();
        shopLayer.SetAsLastSibling();
        PlaceLevelNameBackgroundBehindUiDown();
    }

    static Transform GetOrCreatePlayerLevelHudRoot(Transform canvasTransform)
    {
        Transform hudRoot = canvasTransform.Find("PlayerLevelHud");
        if (hudRoot != null)
        {
            return hudRoot;
        }

        GameObject hudObj = new GameObject("PlayerLevelHud");
        hudObj.transform.SetParent(canvasTransform, false);
        RectTransform hudRect = hudObj.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;
        ApplyPlayerLevelHudCanvasSettings(hudObj.transform);
        return hudObj.transform;
    }

    static void ApplyPlayerLevelHudCanvasSettings(Transform hudRoot)
    {
        if (hudRoot == null) return;

        Canvas hudCanvas = hudRoot.GetComponent<Canvas>();
        if (hudCanvas == null)
        {
            hudCanvas = hudRoot.gameObject.AddComponent<Canvas>();
        }

        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = PlayerLevelHudSortingOrder;
    }

    static void ReparentPlayerLevelHudElement(GameObject element, Transform hudRoot)
    {
        if (element == null || hudRoot == null) return;
        if (element.transform.parent == hudRoot) return;
        element.transform.SetParent(hudRoot, true);
    }

    static void CollectOrphanedPlayerLevelHudElements(Transform canvasTransform, Transform hudRoot)
    {
        if (canvasTransform == null || hudRoot == null) return;

        ReparentIfOrphaned(FindUiTransform("GoldBackground"), hudRoot);
        ReparentIfOrphaned(FindUiTransform("LevelText"), hudRoot);
    }

    static void ReparentIfOrphaned(Transform element, Transform hudRoot)
    {
        if (element == null || hudRoot == null) return;
        if (element == hudRoot || element.IsChildOf(hudRoot)) return;
        element.SetParent(hudRoot, true);
    }

    static Transform FindUiTransform(string objectName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isRootCanvas) continue;
            Transform found = FindChildRecursive(canvas.transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent == null) return null;
        if (parent.name == objectName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null) return found;
        }

        return null;
    }
    
    /// <summary>
    /// 상점 버튼과 업그레이드 버튼을 생성합니다 (화면 하단)
    /// </summary>
    void CreateShopButtons()
    {
        Canvas canvas = gameUiCanvas != null ? gameUiCanvas : FindMainRootCanvas();
        if (canvas == null)
        {
            Debug.LogError("Canvas를 찾을 수 없습니다!");
            return;
        }

        Transform buttonParent = canvas.transform;
        Transform shopLayer = canvas.transform.Find("ShopUiLayer");
        if (shopLayer != null)
        {
            buttonParent = shopLayer;
            if (shopLayer.Find("ShopButton") != null)
            {
                return;
            }
        }
        
        CreateShopOddsUI(canvas, shopLayer != null ? shopLayer : canvas.transform);

        ShopActionButtonBuildResult upgradeButtonBuild = CreateCompactShopActionButton(
            buttonParent,
            "UpgradeButton",
            GameLocalization.HudExperience,
            new Color(0.35f, 0.7f, 1f, 1f),
            GetButtonBlueTexture(),
            ShopUpgradeGoldCost,
            () =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UpgradePlayerLevel();
                }
            });
        upgradeXpCostText = upgradeButtonBuild.costText;
        shopUpgradeLabelText = upgradeButtonBuild.labelText;
        RectTransform upgradeButtonRect = upgradeButtonBuild.rootRect;

        ShopActionButtonBuildResult refreshButtonBuild = CreateCompactShopActionButton(
            buttonParent,
            "ShopButton",
            GameLocalization.HudRefresh,
            new Color(1f, 0.35f, 0.35f, 1f),
            GetButtonRedTexture(),
            ShopRefreshGoldCost,
            () =>
            {
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.TryOpenShop();
                }
            });
        shopRefreshCostText = refreshButtonBuild.costText;
        shopRefreshLabelText = refreshButtonBuild.labelText;
        RectTransform shopButtonRect = refreshButtonBuild.rootRect;

        PositionShopButtonsAtBottomCenter(canvas, shopButtonRect, upgradeButtonRect);
        RefreshShopActionButtonStates();

        /*
        // [테스트 버튼 비활성화] 무료 업그레이드 / 골드 추가 / 라운드 증가
        GameObject freeUpgradeButtonObj = new GameObject("FreeUpgradeButton");
        freeUpgradeButtonObj.transform.SetParent(canvas.transform, false);
        
        RectTransform freeUpgradeRect = freeUpgradeButtonObj.AddComponent<RectTransform>();
        freeUpgradeRect.anchorMin = new Vector2(1, 1);
        freeUpgradeRect.anchorMax = new Vector2(1, 1);
        freeUpgradeRect.pivot = new Vector2(1, 1);
        freeUpgradeRect.sizeDelta = new Vector2(240, 80);
        freeUpgradeRect.anchoredPosition = new Vector2(-20, -20); // 우측 상단
        
        Image freeUpgradeImage = freeUpgradeButtonObj.AddComponent<Image>();
        freeUpgradeImage.color = new Color(0.2f, 0.7f, 0.2f, 1f); // 초록색 배경
        freeUpgradeImage.sprite = GetUIPixelSprite();
        
        Button freeUpgradeButton = freeUpgradeButtonObj.AddComponent<Button>();
        SimpleUIPackTheme.ApplyPrimaryButton(freeUpgradeButton);
        
        GameObject freeUpgradeTextObj = new GameObject("Text");
        freeUpgradeTextObj.transform.SetParent(freeUpgradeButtonObj.transform, false);
        UnityEngine.UI.Text freeUpgradeText = freeUpgradeTextObj.AddComponent<UnityEngine.UI.Text>();
        freeUpgradeText.text = "무료 업그레이드";
        freeUpgradeText.font = UIFontProvider.Get();
        freeUpgradeText.fontSize = 24;
        freeUpgradeText.color = Color.white;
        freeUpgradeText.alignment = UnityEngine.TextAnchor.MiddleCenter;
        
        RectTransform freeUpgradeTextRect = freeUpgradeTextObj.GetComponent<RectTransform>();
        freeUpgradeTextRect.anchorMin = Vector2.zero;
        freeUpgradeTextRect.anchorMax = Vector2.one;
        freeUpgradeTextRect.sizeDelta = Vector2.zero;
        
        freeUpgradeButton.onClick.AddListener(() => {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpgradePlayerLevelFree();
            }
            else
            {
                Debug.LogWarning("GameManager를 찾을 수 없습니다!");
            }
        });
        
        GameObject addGoldButtonObj = new GameObject("AddGoldButton");
        addGoldButtonObj.transform.SetParent(canvas.transform, false);
        
        RectTransform addGoldRect = addGoldButtonObj.AddComponent<RectTransform>();
        addGoldRect.anchorMin = new Vector2(1, 1);
        addGoldRect.anchorMax = new Vector2(1, 1);
        addGoldRect.pivot = new Vector2(1, 1);
        addGoldRect.sizeDelta = new Vector2(200, 70);
        addGoldRect.anchoredPosition = new Vector2(-20, -110);
        
        Image addGoldImage = addGoldButtonObj.AddComponent<Image>();
        addGoldImage.color = new Color(0.9f, 0.7f, 0.1f, 1f);
        addGoldImage.sprite = GetUIPixelSprite();
        
        Button addGoldButton = addGoldButtonObj.AddComponent<Button>();
        SimpleUIPackTheme.ApplyPrimaryButton(addGoldButton);
        
        GameObject addGoldTextObj = new GameObject("Text");
        addGoldTextObj.transform.SetParent(addGoldButtonObj.transform, false);
        UnityEngine.UI.Text addGoldText = addGoldTextObj.AddComponent<UnityEngine.UI.Text>();
        addGoldText.text = "+10 골드";
        addGoldText.font = UIFontProvider.Get();
        addGoldText.fontSize = 24;
        addGoldText.color = Color.white;
        addGoldText.alignment = UnityEngine.TextAnchor.MiddleCenter;
        
        RectTransform addGoldTextRect = addGoldTextObj.GetComponent<RectTransform>();
        addGoldTextRect.anchorMin = Vector2.zero;
        addGoldTextRect.anchorMax = Vector2.one;
        addGoldTextRect.sizeDelta = Vector2.zero;
        
        addGoldButton.onClick.AddListener(() => {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(10);
                Debug.Log("테스트 골드 +10");
            }
            else
            {
                Debug.LogWarning("GameManager를 찾을 수 없습니다!");
            }
        });

        CreateRoundDeltaTestButton(canvas, "AddRounds10Button", "테스트: 라운드 +10", new Vector2(-20f, -190f), 10);
        CreateRoundDeltaTestButton(canvas, "AddRounds5Button", "테스트: 라운드 +5", new Vector2(-20f, -270f), 5);
        CreateRoundDeltaTestButton(canvas, "AddRounds1Button", "테스트: 라운드 +1", new Vector2(-20f, -350f), 1);
        */

        // 우측 하단 skill_popup 비표시
    }

    /*
    // [테스트 버튼 비활성화] 라운드 증가 버튼 헬퍼
    void CreateRoundDeltaTestButton(Canvas canvas, string gameObjectName, string buttonLabel, Vector2 anchoredPosition, int roundsToAdd)
    {
        GameObject btnObj = new GameObject(gameObjectName);
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(200, 70);
        rect.anchoredPosition = anchoredPosition;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.35f, 0.35f, 0.55f, 1f);
        img.sprite = GetUIPixelSprite();

        Button btn = btnObj.AddComponent<Button>();
        SimpleUIPackTheme.ApplyPrimaryButton(btn);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text = buttonLabel;
        t.font = UIFontProvider.Get();
        t.fontSize = 22;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddRoundsForTesting(roundsToAdd);
            }
            else
            {
                Debug.LogWarning("GameManager를 찾을 수 없습니다!");
            }
        });
    }
    */

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

    public void ShowPlacementNotice(string message, float duration = 1.5f)
    {
        ShowTransientToast(GameLocalization.ToastNotice, message, GuideToastUi.Style.Warning, duration);
    }

    void CreatePlacementCountUI(Canvas canvas)
    {
        if (canvas == null || placementCountRoot != null) return;

        const float panelW = 178f;
        const float panelH = 48f;

        Transform placementParent = GameUiSortingLayers.EnsurePlacementLayer(canvas.transform);
        if (placementParent == null)
        {
            placementParent = canvas.transform;
        }

        GameObject rootObj = new GameObject("PlacementCountHud");
        rootObj.transform.SetParent(placementParent, false);
        placementCountRoot = rootObj.AddComponent<RectTransform>();
        placementCountRoot.anchorMin = new Vector2(0.5f, 0.5f);
        placementCountRoot.anchorMax = new Vector2(0.5f, 0.5f);
        placementCountRoot.pivot = new Vector2(0.5f, 0.5f);
        placementCountRoot.sizeDelta = new Vector2(panelW, panelH);

        Image bg = rootObj.AddComponent<Image>();
        bg.sprite = WarmRoundedSprite.Get(
            new Color(0.12f, 0.10f, 0.08f, 0.82f),
            12,
            new Color(1f, 0.85f, 0.55f, 0.12f),
            1f);
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(rootObj.transform, false);
        placementCountText = textObj.AddComponent<Text>();
        placementCountText.text = GameLocalization.HudPlacementFormat(0, GetMaxBoardUnits(), false);
        placementCountText.font = UIFontProvider.Get();
        placementCountText.fontSize = 22;
        placementCountText.fontStyle = FontStyle.Bold;
        placementCountText.color = new Color(0.93f, 0.86f, 0.74f, 1f);
        placementCountText.alignment = TextAnchor.MiddleCenter;
        placementCountText.raycastTarget = false;
        placementCountText.supportRichText = true;

        RectTransform textRect = placementCountText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    void RefreshPlacementCountUI()
    {
        UpdatePlacementCountUI();
    }

    public void UpdatePlacementCountUI()
    {
        if (placementCountRoot == null || placementCountText == null) return;

        int maxUnits = GetMaxBoardUnits();
        int placedUnits = CountBoardUnits();
        bool isFull = maxUnits > 0 && placedUnits >= maxUnits;

        placementCountText.text = GameLocalization.HudPlacementFormat(placedUnits, maxUnits, isFull);
        UIFontProvider.ApplyFont(placementCountText);

        PositionPlacementCountText();
    }

    struct ShopActionButtonBuildResult
    {
        public RectTransform rootRect;
        public Button button;
        public Text costText;
        public Text labelText;
    }

    ShopActionButtonBuildResult CreateCompactShopActionButton(
        Transform parent,
        string objectName,
        string label,
        Color labelColor,
        Texture2D buttonTexture,
        int goldCost,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rootRect = buttonObj.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.sizeDelta = new Vector2(ShopActionButtonWidth, ShopActionButtonHeight);

        RawImage buttonImage = buttonObj.AddComponent<RawImage>();
        buttonImage.color = Color.white;
        buttonImage.texture = buttonTexture;
        buttonImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.None;
        button.interactable = true;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(buttonObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(8f, 6f);
        contentRect.offsetMax = new Vector2(-8f, -6f);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(contentObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = UIFontProvider.Get();
        labelText.fontSize = 22;
        labelText.color = labelColor;
        labelText.alignment = TextAnchor.UpperCenter;
        labelText.raycastTarget = false;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.45f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = new Vector2(0f, -4f);

        GameObject costRowObj = new GameObject("CostRow");
        costRowObj.transform.SetParent(contentObj.transform, false);
        RectTransform costRowRect = costRowObj.AddComponent<RectTransform>();
        costRowRect.anchorMin = new Vector2(0.5f, 0f);
        costRowRect.anchorMax = new Vector2(0.5f, 0f);
        costRowRect.pivot = new Vector2(0.5f, 0f);
        costRowRect.anchoredPosition = new Vector2(0f, 10f);
        costRowRect.sizeDelta = new Vector2(100f, 24f);

        GameObject costIconObj = new GameObject("CostIcon");
        costIconObj.transform.SetParent(costRowObj.transform, false);
        Image costIconImage = costIconObj.AddComponent<Image>();
        Sprite costIconSprite = Resources.Load<Sprite>("gold");
        if (costIconSprite != null)
        {
            costIconImage.sprite = costIconSprite;
            costIconImage.color = Color.white;
            costIconImage.SetNativeSize();
        }
        else
        {
            costIconImage.sprite = GetUIPixelSprite();
            costIconImage.color = new Color(1f, 0.9f, 0.2f, 1f);
        }
        costIconImage.raycastTarget = false;

        RectTransform costIconRect = costIconObj.GetComponent<RectTransform>();
        costIconRect.anchorMin = new Vector2(0f, 0.5f);
        costIconRect.anchorMax = new Vector2(0f, 0.5f);
        costIconRect.pivot = new Vector2(0f, 0.5f);
        costIconRect.anchoredPosition = new Vector2(14f, 0f);
        if (costIconSprite != null)
        {
            costIconRect.sizeDelta = costIconRect.sizeDelta / 7f;
        }

        float costTextLeft = costIconRect.anchoredPosition.x + costIconRect.sizeDelta.x + 4f;

        GameObject costTextObj = new GameObject("CostText");
        costTextObj.transform.SetParent(costRowObj.transform, false);
        Text costText = costTextObj.AddComponent<Text>();
        costText.text = goldCost.ToString();
        costText.font = UIFontProvider.Get();
        costText.fontSize = 20;
        costText.color = ShopActionCostNormalColor;
        costText.alignment = TextAnchor.MiddleLeft;
        costText.raycastTarget = false;

        RectTransform costTextRect = costTextObj.GetComponent<RectTransform>();
        costTextRect.anchorMin = new Vector2(0f, 0f);
        costTextRect.anchorMax = new Vector2(1f, 1f);
        costTextRect.offsetMin = new Vector2(costTextLeft, 0f);
        costTextRect.offsetMax = Vector2.zero;

        return new ShopActionButtonBuildResult
        {
            rootRect = rootRect,
            button = button,
            costText = costText,
            labelText = labelText
        };
    }

    public void RefreshShopActionButtonStates()
    {
        int gold = GameManager.Instance != null ? GameManager.Instance.gold : 0;
        bool atMaxLevel = GameManager.Instance != null &&
                          GameManager.Instance.playerLevel >= GameManager.Instance.maxPlayerLevel;

        if (shopRefreshCostText != null)
        {
            bool canAffordRefresh = gold >= ShopRefreshGoldCost;
            shopRefreshCostText.color = canAffordRefresh ? ShopActionCostNormalColor : ShopActionCostInsufficientColor;
        }

        if (upgradeXpCostText != null)
        {
            bool canAffordUpgrade = !atMaxLevel && gold >= ShopUpgradeGoldCost;
            upgradeXpCostText.color = canAffordUpgrade ? ShopActionCostNormalColor : ShopActionCostInsufficientColor;
        }
    }

    public void UpdateShopOddsUI()
    {
        if (shopOddsTexts == null || shopOddsTexts.Length != 5) return;

        int level = GameManager.Instance != null ? GameManager.Instance.playerLevel : 1;
        var probabilities = LevelGradeProbability.GetProbabilitiesForLevel(level);

        for (int grade = 5; grade >= 1; grade--)
        {
            int index = 5 - grade;
            float percent = probabilities != null && probabilities.ContainsKey(grade) ? probabilities[grade] : 0f;
            shopOddsTexts[index].text = $"{grade}등급 {percent:0}%";
        }
    }

    void CreateShopOddsUI(Canvas canvas, Transform parentOverride = null)
    {
        Transform oddsParent = parentOverride != null ? parentOverride : canvas.transform;
        if (oddsParent.Find("ShopOdds") != null)
        {
            return;
        }

        GameObject oddsObj = new GameObject("ShopOdds");
        oddsObj.transform.SetParent(oddsParent, false);
        RectTransform oddsRect = oddsObj.AddComponent<RectTransform>();
        oddsRect.anchorMin = new Vector2(0.5f, 0f);
        oddsRect.anchorMax = new Vector2(0.5f, 0f);
        oddsRect.pivot = new Vector2(1f, 0f);
        oddsRect.sizeDelta = new Vector2(200, 140);
        // 화면 고정 좌표 (새로고침·경험치 버튼과 함께 약간 오른쪽)
        oddsRect.anchoredPosition = new Vector2(-370f, 0f);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(oddsObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        Sprite oddsBgSprite = Resources.Load<Sprite>("grade_back");
        if (oddsBgSprite == null)
        {
            oddsBgSprite = Resources.Load<Sprite>("1_grade_back");
        }
        bgImage.sprite = oddsBgSprite != null ? oddsBgSprite : GetUIPixelSprite();
        bgImage.color = Color.white;
        bgImage.raycastTarget = false;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgImage.SetNativeSize();
        oddsRect.sizeDelta = bgRect.sizeDelta;
        bgObj.transform.SetSiblingIndex(0);

        shopOddsTexts = new Text[5];
        const int oddsFontSize = 18;
        const float lineHeight = 22f;
        float startY = (shopOddsTexts.Length - 1) * lineHeight * 0.5f;
        float textWidth = Mathf.Max(120f, oddsRect.sizeDelta.x - 24f);

        for (int i = 0; i < 5; i++)
        {
            int grade = 5 - i;
            GameObject textObj = new GameObject($"Odds_{grade}");
            textObj.transform.SetParent(oddsObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = $"{grade}등급 0%";
            text.font = UIFontProvider.Get();
            text.fontSize = oddsFontSize;
            text.color = GetShopOddsGradeColor(grade);
            text.alignment = TextAnchor.MiddleLeft;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(12f, startY - i * lineHeight);
            textRect.sizeDelta = new Vector2(textWidth, lineHeight);

            shopOddsTexts[i] = text;
            textObj.transform.SetAsLastSibling();
        }

        UpdateShopOddsUI();
    }

    static Color GetShopOddsGradeColor(int grade)
    {
        switch (grade)
        {
            case 1: return new Color(1f, 0.71f, 0.3f);   // 골드
            case 2: return new Color(0.78f, 0.61f, 1f);  // 보라
            case 3: return new Color(0.44f, 0.72f, 1f);  // 파랑
            case 4: return new Color(0.5f, 0.85f, 0.54f); // 초록
            default: return new Color(0.72f, 0.75f, 0.8f); // 5등급 은색
        }
    }

    void PositionPlacementCountText()
    {
        if (placementCountRoot == null) return;
        BoardManager board = FindFirstObjectByType<BoardManager>();
        Camera cam = Camera.main;
        if (board == null || cam == null) return;

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

        float boardTopY = boardStart.y;
        float boardCenterX = boardStart.x + (board.boardColumns - 1) * board.cellSize * 0.5f;
        Vector3 worldPos = new Vector3(boardCenterX, boardTopY + board.cellSize * 1.55f, 0f);

        Canvas parentCanvas = placementCountRoot.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, worldPos),
            null,
            out localPoint
        );

        placementCountRoot.anchoredPosition = localPoint;
    }

    int GetMaxBoardUnits()
    {
        int level = GameManager.Instance != null ? GameManager.Instance.playerLevel : 1;
        return Mathf.Min(Mathf.Max(2 + (level - 1), 2), BoardManager.MaxBoardUnitCap);
    }

    int CountBoardUnits()
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        int count = 0;
        foreach (BoardCell cell in cells)
        {
            if (cell != null && cell.isBoardCell && cell.isOccupied)
            {
                count++;
            }
        }
        return count;
    }
}

