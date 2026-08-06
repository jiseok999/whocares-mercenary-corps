using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임의 전반적인 상태를 관리하는 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>조합 시너지 패널용 항목(보스·조합 시너지·유닛 효과)</summary>
    public struct BoardBuffOverviewEntry
    {
        public string category;
        public string title;
        public string location;
        public string effect;
        public bool isAttackSpeed;
        public bool isRangeBuff;
        public bool isTraitBuff;
        /// <summary>조합 시너지·펭귄 오라 등 아이콘 조회용(없으면 null/empty)</summary>
        public string traitName;
        /// <summary>유닛 전용 효과일 때 유닛 번호(0이면 없음)</summary>
        public int sourceUnitNumber;
    }

    enum BossTileBuffType
    {
        CenterPower,
        FrontlineHaste,
        BacklinePower,
        LeftShield,
        RightHaste,
        MiddleColumnPower,
        CornerPower
    }

    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    public int sunPoints = 50; // 시작 태양 포인트
    public bool gameActive = true;
    private bool shopOpen = false; // 상점이 열려있는지
    
    [Header("Gold System")]
    public int gold = 0; // 골드 (좀비 처치 시 획득)
    
    [Header("Silver System")]
    public int silverCoins = 100; // 실버 코인 (기본 100)
    
    [Header("Round System")]
    public int currentRound = 1;
    [Tooltip("체험판의 마지막 라운드(클리어 시 체험판 완료 팝업 표시)")]
    public int demoFinalRound = 30;
    [Tooltip("라운드 클리어 후 다음 라운드까지 대기 시간(초). useRoguelikeUnitFlow=true면 무시됩니다.")]
    public float roundClearDelay = 60f;
    [Tooltip("라운드 전투 지속 시간(초). 이 시간 안에 웨이브 적이 순차 등장합니다.")]
    public float roundCombatDuration = 20f;
    private float roundTransitionTimer = 0f;
    private float roundCombatTimer = 0f;
    private bool waitingForRoundTransition = false;
    private bool awaitingRound30BossKill = false;
    private float totalPlayTime = 0f;
    
    [Header("Level System")]
    public int playerLevel = 1; // 유저 레벨

    [Header("Experience System")]
    public int currentExperience = 0; // 현재 레벨 구간 경험치
    public int roundExperienceReward = 2; // 라운드 완료 시 획득 경험치
    public int maxPlayerLevel = 30; // 등급 확률 테이블 상한과 동일 (LevelGradeProbability.MaxBalancedLevel)
    private static readonly int[] levelRequiredExperience =
    {
        0,   // dummy (index 0)
        6,   // 1 -> 2
        8,   // 2 -> 3
        10,  // 3 -> 4
        12,  // 4 -> 5
        14,  // 5 -> 6
        16,  // 6 -> 7
        18,  // 7 -> 8
        20,  // 8 -> 9
        22,  // 9 -> 10
        24,  // 10 -> 11
        26,  // 11 -> 12
        28,  // 12 -> 13
        30,  // 13 -> 14
        32,  // 14 -> 15
        35,  // 15 -> 16
        38,  // 16 -> 17
        41,  // 17 -> 18
        44,  // 18 -> 19
        47,  // 19 -> 20
        50,  // 20 -> 21
        54,  // 21 -> 22
        58,  // 22 -> 23
        62,  // 23 -> 24
        66,  // 24 -> 25
        70,  // 25 -> 26
        75,  // 26 -> 27
        80,  // 27 -> 28
        85,  // 28 -> 29
        90   // 29 -> 30
    };
    
    [Header("Result")]
    public int killedZombies = 0;
    private GameObject gameOverPanel;
    private GameObject demoClearPanel;
    private RunEndRewardCalculator.Summary cachedGameOverSummary;
    private RunEndRewardCalculator.Summary cachedDemoClearSummary;
    private bool cachedGameOverSummaryValid;
    private bool cachedDemoClearSummaryValid;
    
    [Header("UI")]
    public UnityEngine.UI.Text sunPointsText;
    public UnityEngine.UI.Text goldText; // 골드 UI 텍스트
    public UnityEngine.UI.Text roundText;
    public UnityEngine.UI.Text timeText;
    public UnityEngine.UI.Image roundTimerFill;
    public RectTransform roundTimerFillRect;
    public GameObject roundTimerRootObject;
    public UnityEngine.UI.Text roundTimerCountdownText;
    public UnityEngine.UI.Text levelText; // 레벨 UI 텍스트
    public UnityEngine.UI.Text levelExperienceText; // (현재/필요) 경험치 텍스트
    public UnityEngine.UI.Image levelExperienceGaugeFill; // 경험치 게이지 채움
    public RectTransform roundWaveIndicatorRect; // 상단 웨이브 인디케이터
    public UnityEngine.UI.Image[] roundWaveSlotImages; // 상단 웨이브 슬롯 아이콘
    public int roundWaveSlotCount = 10;
    public float roundWaveSlotWidth = 34f;
    public float roundWaveSlotGap = 12f;
    public float roundWaveIndicatorBaseY = 8f;
    public float roundWaveIndicatorBobAmplitude = 2.2f;
    public float roundWaveIndicatorBobSpeed = 3.2f;
    public UnityEngine.UI.Text bossWarningText; // 보스 경고 UI
    public bool showPartyToast = true;
    
    private bool roundFlowStarted;
    private bool awaitingFirstRoundDeployment;
    private bool firstRoundCombatStarted;
    private bool awaitingInitialUnitSelection;
    private float combatActionClock;

    [Header("Roguelike Flow")]
    [Tooltip("쉬는 시간·상점 없이 시작/레벨업 유닛 선택 흐름")]
    public bool useRoguelikeUnitFlow = true;

    private bool firstRoundGuideToastRequested;
    private bool firstRoundGuidePendingShopDialogue;
    
    private GridManager gridManager;
    private GameObject bossRewardPanel;
    private bool bossRewardShown = false;
    private bool bossRewardChestSpawned = false;
    private readonly HashSet<BossTileBuffType> acquiredBossTileBuffs = new HashSet<BossTileBuffType>();
    private Sprite bossBuffDamageIcon;
    private Sprite bossBuffAttackSpeedIcon;

    // [비활성] 조합 시너지 칸 — 조합 발동 시 버프 칸을 생성·표시하는 기능 (복구 시 아래 블록 참고)
    struct SynergyCellBuff
    {
        public int row;
        public int col;
        public string trait;
    }
    private readonly List<SynergyCellBuff> synergyCells = new List<SynergyCellBuff>(16);
    private readonly Dictionary<string, Sprite> traitCellIconCache = new Dictionary<string, Sprite>();
    private string lastSynergySignature = "";
    private string lastUnitPlacementSignature = "";

    /*
    struct SynergyCellDef
    {
        public string trait;
        public string colorName;
    }
    private static readonly SynergyCellDef[] SynergyCellDefs =
    {
        new SynergyCellDef { trait = "도깨비", colorName = "초록" },
        new SynergyCellDef { trait = "마법사", colorName = "파랑" },
        new SynergyCellDef { trait = "기사단", colorName = "은색" },
        new SynergyCellDef { trait = "재앙",   colorName = "빨강" },
        new SynergyCellDef { trait = "악마",   colorName = "보라" }
    };
    public const float SynergyCellEffectMultiplier = 2f;
    */
    private readonly Dictionary<string, Sprite> roundPartyIconCache = new Dictionary<string, Sprite>();
    private int lastRoundPartyBlockStart = -1;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GameLocalizationCoordinator.Register(RefreshLocalizedUI);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        currentRound = 1;
        roundTransitionTimer = 0f;
        roundCombatTimer = 0f;
        combatActionClock = 0f;
        waitingForRoundTransition = false;
        playerLevel = 1;
        currentExperience = 0;
        gold = 500;
        silverCoins = 100;
        RunEncounterTracker.BeginRun();
        CharacterUpgradeData.EnsureLoaded();
        acquiredBossTileBuffs.Clear();
        synergyCells.Clear();
        lastSynergySignature = "";
        UpdateUI();
        SubscribeShopMercenaryDialogue();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            GameLocalizationCoordinator.Unregister(RefreshLocalizedUI);
        }
        UnsubscribeShopMercenaryDialogue();
    }

    public void RefreshLocalizedUI()
    {
        Font font = UIFontProvider.Get();
        UIFontProvider.ApplyFont(roundText, font);
        UIFontProvider.ApplyFont(timeText, font);
        UIFontProvider.ApplyFont(roundTimerCountdownText, font);
        UIFontProvider.ApplyFont(bossWarningText, font);
        UIFontProvider.ApplyFont(levelText, font);
        UIFontProvider.ApplyFont(levelExperienceText, font);
        UIFontProvider.ApplyFont(goldText, font);
        UIFontProvider.ApplyFont(sunPointsText, font);
        UpdateRoundUI();
        UpdateUI();
        RefreshOpenResultPanels();
    }

    void SubscribeShopMercenaryDialogue()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.MercenaryDialogueFinished -= OnShopMercenaryDialogueFinished;
            ShopManager.Instance.MercenaryDialogueFinished += OnShopMercenaryDialogueFinished;
        }
    }

    void UnsubscribeShopMercenaryDialogue()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.MercenaryDialogueFinished -= OnShopMercenaryDialogueFinished;
        }
    }

    void OnShopMercenaryDialogueFinished()
    {
        if (!firstRoundGuidePendingShopDialogue) return;

        firstRoundGuidePendingShopDialogue = false;
        ShowFirstRoundGuideToast();
    }
    
    void Update()
    {
        TickCombatActionClock();

        if (gameActive && roundFlowStarted)
        {
            totalPlayTime += Time.deltaTime;
        }

        RefreshSynergyCells();
        RefreshUnitPlacementBuffHighlights();
        RefreshUnitBoardBuffAuras();

        if (awaitingFirstRoundDeployment)
        {
            TryShowFirstRoundGuideToast();
        }

        if (!gameActive || !roundFlowStarted) return;

        UpdateRoundProgress();
        UpdateRoundUI();
    }

    void UpdateRoundProgress()
    {
        // 보스 보상·시작 유닛 선택 등 shopOpen 중에는 진행 중단. 레거시 쉬는 시간은 타이머 계속.
        if (shopOpen && !waitingForRoundTransition && !awaitingInitialUnitSelection) return;

        if (!waitingForRoundTransition)
        {
            if (awaitingRound30BossKill)
            {
                return;
            }

            roundCombatTimer += Time.deltaTime;
            if (roundCombatTimer >= roundCombatDuration)
            {
                if (currentRound >= demoFinalRound && IsNecromancerBossAlive())
                {
                    awaitingRound30BossKill = true;
                    HandleCombatEnded();
                    return;
                }

                ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
                if (spawner != null)
                {
                    spawner.CancelRoundSpawn();
                }

                HandleCombatEnded();

                if (awaitingInitialUnitSelection)
                {
                    return;
                }

                if (useRoguelikeUnitFlow)
                {
                    if (awaitingFirstRoundDeployment)
                    {
                        StartFirstRoundCombat();
                    }
                    else
                    {
                        CompleteRoundBreak();
                    }
                    return;
                }

                waitingForRoundTransition = true;
                roundTransitionTimer = 0f;
            }
            return;
        }

        roundTransitionTimer += Time.deltaTime;
        if (roundTransitionTimer >= roundClearDelay)
        {
            if (awaitingFirstRoundDeployment)
            {
                if (!StartFirstRoundCombat())
                {
                    roundTransitionTimer = Mathf.Max(0f, roundClearDelay - 8f);
                }
            }
            else
            {
                CompleteRoundBreak();
            }
        }
    }

    void CompleteRoundBreak()
    {
        waitingForRoundTransition = false;

        if (currentRound >= demoFinalRound)
        {
            gameActive = false;
            ShowDemoClearUI();
            return;
        }

        BeginRound(currentRound + 1);
    }

    /// <summary>
    /// 쉬는 시간을 즉시 종료하고 다음 라운드로 진행합니다.
    /// </summary>
    public void SkipRoundBreak()
    {
        if (!waitingForRoundTransition) return;
        if (!gameActive || !roundFlowStarted) return;

        if (awaitingFirstRoundDeployment)
        {
            StartFirstRoundCombat();
            return;
        }

        CompleteRoundBreak();
    }

    void HandleCombatEnded()
    {
        GrantTraitRoundEndGold();

        if (!useRoguelikeUnitFlow && ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenBreakShop();
        }
    }

    /// <summary>
    /// 라운드 전투(20초) 진행 중이면 true. 쉬는 시간·첫 배치·상점 UI 중에는 false.
    /// </summary>
    public bool IsCombatActive()
    {
        if (!gameActive || !roundFlowStarted) return false;
        if (shopOpen) return false;
        if (waitingForRoundTransition) return false;
        if (awaitingFirstRoundDeployment) return false;
        return true;
    }

    /// <summary>현재 라운드 전투 시간이 아직 남아 있으면 true (소환 코루틴용)</summary>
    public bool IsRoundCombatActive()
    {
        return IsCombatActive() && (roundCombatTimer < roundCombatDuration || awaitingRound30BossKill);
    }

    public float RoundCombatElapsed => roundCombatTimer;
    public float RoundCombatDuration => roundCombatDuration;
    public float RoundCombatRemaining => awaitingRound30BossKill
        ? 1f
        : Mathf.Max(0f, roundCombatDuration - roundCombatTimer);

    public bool IsAwaitingFirstRoundDeployment => awaitingFirstRoundDeployment;
    public bool IsAwaitingRound30BossKill => awaitingRound30BossKill;

    void OnInitialUnitSelectionComplete()
    {
        if (!awaitingInitialUnitSelection) return;

        awaitingInitialUnitSelection = false;
        awaitingFirstRoundDeployment = false;
        waitingForRoundTransition = false;
        roundTransitionTimer = 0f;
        firstRoundCombatStarted = true;
        roundCombatTimer = 0f;
        SetShopOpen(false);

        HideFirstRoundGuideToast();

        ShowRoundPartyToast();

        ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            spawner.SpawnRoundWave(currentRound);
        }

        UpdateRoundUI();
        Debug.Log("시작 유닛 선택 완료 — 1라운드 전투 시작");
    }

    public bool StartFirstRoundCombat()
    {
        if (!awaitingFirstRoundDeployment) return false;
        if (CountBoardUnits() <= 0)
        {
            GameSceneController controller = FindFirstObjectByType<GameSceneController>();
            if (controller != null)
            {
                controller.ShowPlacementNotice(GameLocalization.ToastDeployFirst, 2f);
            }

            return false;
        }

        awaitingFirstRoundDeployment = false;
        waitingForRoundTransition = false;
        roundTransitionTimer = 0f;
        firstRoundCombatStarted = true;
        roundCombatTimer = 0f;

        HideFirstRoundGuideToast();

        GameSceneController sceneController = FindFirstObjectByType<GameSceneController>();
        if (sceneController != null)
        {
            sceneController.ShowTransientToast(
                "1라운드 시작",
                GameLocalization.ToastEnemiesRush,
                GuideToastUi.Style.Warning,
                2.6f);
        }

        ShowRoundPartyToast();

        ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            spawner.SpawnRoundWave(currentRound);
        }

        UpdateRoundUI();
        Debug.Log("1라운드 — 준비 시간 종료, 전투 시작");
        return true;
    }

    public static int CountBoardUnits()
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            BoardCell cell = cells[i];
            if (cell != null && cell.isBoardCell && cell.isOccupied)
            {
                count++;
            }
        }

        return count;
    }

    public void NotifyFirstRoundUnitPurchased()
    {
        if (!awaitingFirstRoundDeployment) return;

        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.SetFirstRoundGuidePurchased();
        }

        TryShowFirstRoundGuideToast();
    }

    void TryShowFirstRoundGuideToast()
    {
        if (firstRoundGuidePendingShopDialogue) return;
        if (!firstRoundGuideToastRequested && !awaitingFirstRoundDeployment) return;

        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.EnsureFirstRoundGuideVisible();
        }
    }

    void ShowFirstRoundGuideToast()
    {
        firstRoundGuideToastRequested = true;

        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.RequestFirstRoundGuideToast();
        }
    }

    void HideFirstRoundGuideToast()
    {
        firstRoundGuideToastRequested = false;

        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.HideFirstRoundGuideToast();
        }
    }

    public static int CountLivingCombatZombies()
    {
        Zombie.CopyLivingZombiesTo(_zombieCountBuffer);
        return _zombieCountBuffer.Count;
    }

    static readonly List<Zombie> _zombieCountBuffer = new List<Zombie>(256);
    
    /// <summary>
    /// 라운드 시작 처리 (웨이브 스폰, 상점 새로고침, 타이머 리셋)
    /// </summary>
    public void BeginRound(int round)
    {
        int previousRound = currentRound;
        currentRound = Mathf.Max(1, round);
        UnitDamageLedger.ResetForRound(currentRound);
        roundTransitionTimer = 0f;
        waitingForRoundTransition = false;
        awaitingRound30BossKill = false;
        roundFlowStarted = true;

        int roundsPassed = Mathf.Max(0, currentRound - previousRound);
        if (roundsPassed > 0)
        {
            GainExperience(roundsPassed * Mathf.Max(0, roundExperienceReward));
        }

        // 상점은 전투 종료 시 갱신. 최초 라운드 시작(1→1)만 여기서 1회 새로고침
        if (previousRound >= currentRound && ShopManager.Instance != null)
        {
            SubscribeShopMercenaryDialogue();
            ShopManager.Instance.RefreshShop(ShopManager.ShopRefreshSource.Auto);
        }

        bool deferFirstRoundCombat = currentRound == 1 && !firstRoundCombatStarted;

        if (deferFirstRoundCombat)
        {
            if (useRoguelikeUnitFlow)
            {
                awaitingInitialUnitSelection = true;
                awaitingFirstRoundDeployment = false;
                SetShopOpen(true);

                GameSceneController controller = FindFirstObjectByType<GameSceneController>();
                controller?.HideTransientToast();

                LevelUpUnitSelectionController.ShowStartingUnitSelection(OnInitialUnitSelectionComplete);
            }
            else
            {
                awaitingFirstRoundDeployment = true;
                waitingForRoundTransition = true;
                roundTransitionTimer = 0f;

                GameSceneController controller = FindFirstObjectByType<GameSceneController>();
                controller?.HideTransientToast();

                if (ShopManager.Instance != null && previousRound >= currentRound)
                {
                    firstRoundGuidePendingShopDialogue = true;
                }
                else
                {
                    ShowFirstRoundGuideToast();
                }
            }
        }
        else
        {
            ShowRoundPartyToast();
            QueueEnemyHealthMilestoneToast();
        }

        if (!deferFirstRoundCombat)
        {
            roundCombatTimer = 0f;
            ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
            if (spawner != null)
            {
                spawner.SpawnRoundWave(currentRound);
            }
        }

        if (currentRound == 10)
        {
            ShowBossWarning();
        }

        Debug.Log($"라운드 {currentRound} 시작 — 웨이브 스폰 및 상점 새로고침");
        UpdateRoundUI();

        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.ResetRoundCooldowns();
        }
    }

    void ShowRoundPartyToast()
    {
        if (!showPartyToast) return;

        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller == null) return;

        string partyName = RoundWaveController.GetPartyDisplayNameForRound(currentRound);
        controller.ShowPartyToast(partyName);
    }

    void QueueEnemyHealthMilestoneToast()
    {
        if (!EnemyCombatStats.IsHealthMilestoneEntryRound(currentRound)) return;

        float delay = showPartyToast ? 2.35f : 0.1f;
        StartCoroutine(ShowEnemyHealthMilestoneToastAfterDelay(delay));
    }

    System.Collections.IEnumerator ShowEnemyHealthMilestoneToastAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (!EnemyCombatStats.IsHealthMilestoneEntryRound(currentRound))
        {
            yield break;
        }

        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller == null) yield break;

        float cumulativeMul = EnemyCombatStats.GetHealthMilestoneMultiplier(currentRound);
        float stepMul = EnemyCombatStats.HealthMilestoneMultiplier;
        controller.ShowTransientToast(
            GameLocalization.ToastEnemyHpBoost,
            GameLocalization.ToastEnemyHpBoostBody(currentRound, stepMul, cumulativeMul),
            GuideToastUi.Style.Warning,
            2.8f);
    }
    
    /// <summary>
    /// 상점 열림 상태를 설정합니다
    /// </summary>
    public void SetShopOpen(bool open)
    {
        shopOpen = open;
    }
    
    /// <summary>
    /// 상점이 열려있는지 확인합니다
    /// </summary>
    public bool IsShopOpen()
    {
        return shopOpen;
    }

    /// <summary>
    /// 전투 일시정지(쉬는 시간·첫 배치·보스 보상/성장 UI 등) 중이면 true.
    /// 적 이동·공격, 아군 공격·투사체가 멈춥니다.
    /// </summary>
    public bool IsCombatPaused()
    {
        if (!gameActive) return true;
        if (awaitingInitialUnitSelection) return true;
        if (shopOpen) return true;
        if (waitingForRoundTransition) return true;
        if (awaitingFirstRoundDeployment) return true;
        return false;
    }

    /// <summary>
    /// 유닛 공격 쿨 등 전투 타이머가 흐르는 중이면 true. 일시정지·Time.timeScale=0 중에는 false.
    /// </summary>
    public bool ShouldAdvanceCombatActionClock()
    {
        if (!gameActive || !roundFlowStarted) return false;
        if (Time.timeScale <= 0.0001f) return false;
        if (IsCombatPaused()) return false;
        return true;
    }

    void TickCombatActionClock()
    {
        if (!ShouldAdvanceCombatActionClock()) return;
        combatActionClock += Time.deltaTime;
    }

    /// <summary>일시정지 중에도 멈추는 전투 전용 시계(공격 쿨타임 등).</summary>
    public float CombatActionTime => combatActionClock;

    public bool IsAwaitingInitialUnitSelection => awaitingInitialUnitSelection;

    public bool IsWaitingForRoundTransition => waitingForRoundTransition;
    public float RoundBreakElapsed => roundTransitionTimer;
    public float RoundBreakDuration => roundClearDelay;
    public float RoundBreakRemaining => Mathf.Max(0f, roundClearDelay - roundTransitionTimer);
    
    /// <summary>
    /// 라운드 UI를 업데이트합니다
    /// </summary>
    void UpdateRoundUI()
    {
        if (roundText != null)
        {
            roundText.text = GameLocalization.HudRoundFormat(currentRound);
        }
        
        if (timeText != null)
        {
            if (waitingForRoundTransition)
            {
                timeText.text = GameLocalization.HudBreakTime;
            }
            else
            {
                timeText.text = RoundWaveController.GetPartyHudDisplayNameForRound(currentRound);
            }
        }

        if (roundTimerFill != null)
        {
            GameObject timerRootObject = roundTimerRootObject;
            if (timerRootObject == null)
            {
                Transform fallbackRoot = roundTimerFill.transform.parent;
                if (fallbackRoot != null && fallbackRoot.parent != null)
                {
                    fallbackRoot = fallbackRoot.parent;
                }
                timerRootObject = fallbackRoot != null ? fallbackRoot.gameObject : null;
            }

            if (waitingForRoundTransition && roundClearDelay > 0f)
            {
                // 대형 BreakTimeOverlay UI가 표시 — 하단 소형 타이머는 숨김
                if (timerRootObject != null && timerRootObject.activeSelf)
                {
                    timerRootObject.SetActive(false);
                }
            }
            else if (IsCombatActive() && roundCombatDuration > 0f)
            {
                if (timerRootObject != null && !timerRootObject.activeSelf)
                {
                    timerRootObject.SetActive(true);
                }
                float ratio = Mathf.Clamp01(1f - (roundCombatTimer / roundCombatDuration));
                if (roundTimerFillRect != null)
                {
                    Vector2 anchorMax = roundTimerFillRect.anchorMax;
                    anchorMax.x = ratio;
                    roundTimerFillRect.anchorMax = anchorMax;
                }
                if (roundTimerCountdownText != null)
                {
                    if (awaitingRound30BossKill)
                    {
                        roundTimerCountdownText.text = "!";
                    }
                    else
                    {
                        int seconds = Mathf.CeilToInt(RoundCombatRemaining);
                        roundTimerCountdownText.text = seconds.ToString();
                    }
                }
            }
            else
            {
                if (timerRootObject != null && timerRootObject.activeSelf)
                {
                    timerRootObject.SetActive(false);
                }
                if (roundTimerFillRect != null)
                {
                    Vector2 anchorMax = roundTimerFillRect.anchorMax;
                    anchorMax.x = 1f;
                    roundTimerFillRect.anchorMax = anchorMax;
                }
                if (roundTimerCountdownText != null)
                {
                    roundTimerCountdownText.text = string.Empty;
                }
            }
        }
        
        if (levelText != null)
        {
            levelText.text = GameLocalization.HudLevelFormat(playerLevel);
        }

        int requiredExp = playerLevel >= maxPlayerLevel
            ? GetRequiredExperienceForLevel(maxPlayerLevel - 1)
            : GetRequiredExperienceForLevel(playerLevel);
        int shownExp = playerLevel >= maxPlayerLevel
            ? requiredExp
            : Mathf.Clamp(currentExperience, 0, requiredExp);

        if (levelExperienceText != null)
        {
            levelExperienceText.text = $"({shownExp}/{requiredExp})";
        }

        if (levelExperienceGaugeFill != null)
        {
            float ratio = requiredExp > 0 ? (float)shownExp / requiredExp : 1f;
            RectTransform gaugeRect = levelExperienceGaugeFill.rectTransform;
            Vector2 anchorMax = gaugeRect.anchorMax;
            anchorMax.x = Mathf.Clamp01(ratio);
            gaugeRect.anchorMax = anchorMax;
        }

        UpdateRoundWaveIndicatorUI();
    }

    void ShowBossWarning()
    {
        if (bossWarningText == null) return;
        bossWarningText.gameObject.SetActive(true);
        StopCoroutine(nameof(HideBossWarningAfterDelay));
        StartCoroutine(HideBossWarningAfterDelay());
    }

    /// <summary>테스트용: 현재 라운드에 일정 수를 더합니다 (최소 라운드 1).</summary>
    public void AddRoundsForTesting(int delta)
    {
        if (delta <= 0) return;
        BeginRound(currentRound + delta);
        Debug.Log($"[테스트] 라운드 +{delta} → {currentRound}");
    }

    System.Collections.IEnumerator HideBossWarningAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (bossWarningText != null)
        {
            bossWarningText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 태양 포인트를 추가합니다
    /// </summary>
    public void AddSunPoints(int amount)
    {
        sunPoints += amount;
        UpdateUI();
    }
    
    /// <summary>
    /// 태양 포인트를 소비합니다
    /// </summary>
    public bool SpendSunPoints(int cost)
    {
        if (sunPoints >= cost)
        {
            sunPoints -= cost;
            UpdateUI();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// UI를 업데이트합니다
    /// </summary>
    void UpdateUI()
    {
        if (sunPointsText != null)
        {
            sunPointsText.text = GameLocalization.HudSunPointsFormat(sunPoints);
        }
        
        if (goldText != null)
        {
            goldText.text = gold.ToString();
        }

        GameSceneController sceneController = FindFirstObjectByType<GameSceneController>();
        if (sceneController != null)
        {
            sceneController.RefreshShopActionButtonStates();
        }
        
        UpdateRoundUI();
    }
    
    /// <summary>
    /// 좀비가 죽었을 때 호출됩니다
    /// </summary>
    public void OnZombieKilled(Zombie zombie)
    {
        killedZombies += 1;
        GainExperience(1);
        // 좀비 1마리 처치 보상
        AddGold(1);

        if (IsBossZombie(zombie) && !bossRewardChestSpawned)
        {
            bossRewardChestSpawned = true;
            StartCoroutine(ShowBossRewardSequence());
        }

        if (awaitingRound30BossKill && zombie != null && zombie.zombieUID == "necromancer_boss")
        {
            awaitingRound30BossKill = false;
            ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
            if (spawner != null)
            {
                spawner.CancelRoundSpawn();
            }

            gameActive = false;
            ShowDemoClearUI();
        }
    }
    
    public void OnBarrierDestroyed()
    {
        GameOver();
    }
    
    /// <summary>
    /// 골드를 추가합니다
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        UpdateUI();
    }

    /// <summary>라운드 클리어 시 조합(도깨비·자연 등) 골드 보너스를 지급합니다.</summary>
    void GrantTraitRoundEndGold()
    {
        if (TraitManager.Instance == null) return;
        int bonus = TraitManager.Instance.GetRoundEndGoldBonus();
        if (bonus > 0)
        {
            AddGold(bonus);
            Debug.Log($"조합 골드 보너스 +{bonus} (라운드 {currentRound} 종료)");
        }
    }

    public void AddSilver(int amount)
    {
        silverCoins += amount;
        UpdateUI();
    }
    
    /// <summary>
    /// 골드를 소비합니다
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 실버 코인을 소비합니다
    /// </summary>
    public bool SpendSilver(int amount)
    {
        if (silverCoins >= amount)
        {
            silverCoins -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 좀비가 끝에 도달했을 때 호출됩니다
    /// </summary>
    public void OnZombieReachedEnd(Zombie zombie)
    {
        GameOver();
    }
    
    /// <summary>
    /// 게임 오버 처리
    /// </summary>
    void GameOver()
    {
        if (!gameActive) return;
        gameActive = false;
        Debug.Log("게임 오버!");
        ShowGameOverUI();
    }
    
    void ShowGameOverUI(bool applyRewards = true)
    {
        if (gameOverPanel != null) return;
        if (!applyRewards && !cachedGameOverSummaryValid) return;

        if (applyRewards)
        {
            cachedGameOverSummary = RunEndRewardCalculator.ApplyRewards(currentRound);
            cachedGameOverSummaryValid = true;
        }
        RunEndRewardCalculator.Summary rewardSummary = cachedGameOverSummaryValid
            ? cachedGameOverSummary
            : RunEndRewardCalculator.ApplyRewards(currentRound);

        Canvas canvas = FindRootOverlayCanvas();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            MobileUIScaling.Configure(canvasObj.AddComponent<CanvasScaler>());
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 웜톤 팔레트
        Color accentColor = new Color(1f, 0.78f, 0.30f, 1f);
        Color creamColor = new Color(0.93f, 0.86f, 0.74f, 1f);
        Color tanColor = new Color(0.80f, 0.66f, 0.48f, 1f);

        // 전체 화면 어두운 백드롭
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvas.transform, false);
        Canvas overlayCanvas = gameOverPanel.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 400;
        gameOverPanel.AddComponent<GraphicRaycaster>();
        Image panelImage = gameOverPanel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.04f, 0.025f, 0.86f);
        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        const float cardW = 560f;
        const float cardH = 780f;

        // 카드 뒤 그림자
        GameObject shadowObj = new GameObject("CardShadow");
        shadowObj.transform.SetParent(gameOverPanel.transform, false);
        Image shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.type = Image.Type.Sliced;
        shadowImage.raycastTarget = false;
        shadowImage.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.35f), 40, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(cardW + 36f, cardH + 36f);
        shadowRect.anchoredPosition = new Vector2(0f, -12f);

        // 카드 본체 (화면 정중앙)
        GameObject cardObj = new GameObject("ResultCard");
        cardObj.transform.SetParent(gameOverPanel.transform, false);
        Image cardImage = cardObj.AddComponent<Image>();
        cardImage.type = Image.Type.Sliced;
        cardImage.sprite = WarmRoundedSprite.Get(new Color(0.145f, 0.108f, 0.078f, 0.99f), 30, new Color(1f, 0.85f, 0.55f, 0.12f), 2.5f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(cardW, cardH);
        cardRect.anchoredPosition = Vector2.zero;

        // 상단 중앙 액센트 바
        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(cardObj.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.type = Image.Type.Sliced;
        accentImage.raycastTarget = false;
        accentImage.sprite = WarmRoundedSprite.Get(accentColor, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -26f);
        accentRect.sizeDelta = new Vector2(76f, 6f);

        // 타이틀 / 서브타이틀
        Text titleText = CreateResultText(cardObj.transform, GameLocalization.ResultBattleTitle, 44, FontStyle.Bold, accentColor, TextAnchor.UpperCenter);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -44f);
        titleRect.sizeDelta = new Vector2(440f, 54f);

        Text subtitleText = CreateResultText(cardObj.transform, GameLocalization.ResultWallFallen, 20, FontStyle.Normal, tanColor, TextAnchor.UpperCenter);
        RectTransform subtitleRect = subtitleText.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -102f);
        subtitleRect.sizeDelta = new Vector2(440f, 26f);

        CreateResultDivider(cardObj.transform, 140f, cardW);

        // 통계 행
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultRoundReached, $"{currentRound}", 174f, cardW, creamColor, accentColor);
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultSurvivalTime, FormatTime(totalPlayTime), 226f, cardW, creamColor, accentColor);
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultKills, $"{killedZombies}", 278f, cardW, creamColor, accentColor);
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultLevelReached, $"Lv.{playerLevel}", 330f, cardW, creamColor, accentColor);
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultGoldHeld, $"{gold} G", 382f, cardW, creamColor, accentColor);

        float medalSectionEndY = AppendResultMedalBreakdown(
            cardObj.transform,
            434f,
            cardW,
            currentRound,
            rewardSummary,
            tanColor,
            new Color(accentColor.r, accentColor.g, accentColor.b, 0.72f),
            creamColor,
            accentColor);

        CreateResultDivider(cardObj.transform, medalSectionEndY, cardW);

        CreateResultActionButton(
            cardObj.transform,
            "ReturnTitleButton",
            GameLocalization.ResultReturnTitle,
            new Vector2(0f, 44f),
            new Vector2(480f, 52f),
            new Color(0.03f, 0.10f, 0.24f, 0.72f),
            new Color(0.45f, 0.80f, 1f, 0.58f),
            new Color(0.88f, 0.94f, 1f, 1f),
            ReturnToTitleScene);

        CreateResultActionButton(
            cardObj.transform,
            "RetryButton",
            GameLocalization.ResultRetry,
            new Vector2(0f, 108f),
            new Vector2(480f, 58f),
            new Color(0.95f, 0.60f, 0.16f, 1f),
            new Color(0f, 0f, 0f, 0f),
            new Color(1f, 0.98f, 0.93f, 1f),
            RestartGame);
    }

    /// <summary>
    /// 테스트용: 30라운드 클리어 팝업을 즉시 표시합니다.
    /// </summary>
    public void DebugShowDemoClear()
    {
        gameActive = false;
        ShowDemoClearUI();
    }

    void ShowDemoClearUI(bool applyRewards = true)
    {
        if (demoClearPanel != null) return;
        if (!applyRewards && !cachedDemoClearSummaryValid) return;

        if (applyRewards)
        {
            cachedDemoClearSummary = RunEndRewardCalculator.ApplyRewards(currentRound);
            cachedDemoClearSummaryValid = true;
        }
        RunEndRewardCalculator.Summary rewardSummary = cachedDemoClearSummaryValid
            ? cachedDemoClearSummary
            : RunEndRewardCalculator.ApplyRewards(currentRound);

        Canvas canvas = FindRootOverlayCanvas();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            MobileUIScaling.Configure(canvasObj.AddComponent<CanvasScaler>());
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 웜톤 팔레트
        Color accentColor = new Color(1f, 0.78f, 0.30f, 1f);
        Color creamColor = new Color(0.93f, 0.86f, 0.74f, 1f);
        Color tanColor = new Color(0.80f, 0.66f, 0.48f, 1f);

        // 전체 화면 어두운 백드롭
        demoClearPanel = new GameObject("DemoClearPanel");
        demoClearPanel.transform.SetParent(canvas.transform, false);
        Canvas overlayCanvas = demoClearPanel.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 410;
        demoClearPanel.AddComponent<GraphicRaycaster>();
        Image panelImage = demoClearPanel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.04f, 0.025f, 0.88f);
        RectTransform panelRect = demoClearPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        const float cardW = 600f;
        const float cardH = 920f;

        // 카드 뒤 그림자
        GameObject shadowObj = new GameObject("CardShadow");
        shadowObj.transform.SetParent(demoClearPanel.transform, false);
        Image shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.type = Image.Type.Sliced;
        shadowImage.raycastTarget = false;
        shadowImage.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.35f), 40, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(cardW + 40f, cardH + 40f);
        shadowRect.anchoredPosition = new Vector2(0f, -12f);

        // 카드 본체 (화면 정중앙)
        GameObject cardObj = new GameObject("DemoClearCard");
        cardObj.transform.SetParent(demoClearPanel.transform, false);
        Image cardImage = cardObj.AddComponent<Image>();
        cardImage.type = Image.Type.Sliced;
        cardImage.sprite = WarmRoundedSprite.Get(new Color(0.155f, 0.115f, 0.082f, 0.99f), 30, new Color(1f, 0.82f, 0.45f, 0.18f), 2.5f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(cardW, cardH);
        cardRect.anchoredPosition = Vector2.zero;

        // 상단 중앙 액센트 바
        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(cardObj.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.type = Image.Type.Sliced;
        accentImage.raycastTarget = false;
        accentImage.sprite = WarmRoundedSprite.Get(accentColor, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -28f);
        accentRect.sizeDelta = new Vector2(96f, 6f);

        // 라벨 배지 "DEMO COMPLETE"
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(cardObj.transform, false);
        Image badgeImage = badgeObj.AddComponent<Image>();
        badgeImage.type = Image.Type.Sliced;
        badgeImage.raycastTarget = false;
        badgeImage.sprite = WarmRoundedSprite.Get(new Color(1f, 0.78f, 0.30f, 0.16f), 13, new Color(1f, 0.78f, 0.30f, 0.55f), 1.5f);
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -52f);
        badgeRect.sizeDelta = new Vector2(220f, 34f);
        Text badgeText = CreateResultText(badgeObj.transform, GameLocalization.ResultDemoBadge, 16, FontStyle.Bold, accentColor, TextAnchor.MiddleCenter);
        RectTransform badgeTextRect = badgeText.GetComponent<RectTransform>();
        badgeTextRect.anchorMin = Vector2.zero;
        badgeTextRect.anchorMax = Vector2.one;
        badgeTextRect.offsetMin = Vector2.zero;
        badgeTextRect.offsetMax = Vector2.zero;

        // 타이틀
        Text titleText = CreateResultText(cardObj.transform, GameLocalization.ResultDemoClearTitle, 48, FontStyle.Bold, accentColor, TextAnchor.UpperCenter);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -98f);
        titleRect.sizeDelta = new Vector2(520f, 58f);

        // 감사 메시지
        Text thanksText = CreateResultText(cardObj.transform,
            GameLocalization.ResultDemoThanksFormat(demoFinalRound),
            22, FontStyle.Normal, creamColor, TextAnchor.UpperCenter);
        thanksText.horizontalOverflow = HorizontalWrapMode.Wrap;
        thanksText.lineSpacing = 1.15f;
        RectTransform thanksRect = thanksText.GetComponent<RectTransform>();
        thanksRect.anchorMin = new Vector2(0.5f, 1f);
        thanksRect.anchorMax = new Vector2(0.5f, 1f);
        thanksRect.pivot = new Vector2(0.5f, 1f);
        thanksRect.anchoredPosition = new Vector2(0f, -168f);
        thanksRect.sizeDelta = new Vector2(500f, 70f);

        CreateResultDivider(cardObj.transform, 256f, cardW);

        // 통계 요약
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultKills, $"{killedZombies}", 290f, cardW, creamColor, accentColor);
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultSurvivalTime, FormatTime(totalPlayTime), 342f, cardW, creamColor, accentColor);
        CreateResultStatRow(cardObj.transform, GameLocalization.ResultLevelReached, $"Lv.{playerLevel}", 394f, cardW, creamColor, accentColor);

        float medalSectionEndY = AppendResultMedalBreakdown(
            cardObj.transform,
            446f,
            cardW,
            currentRound,
            rewardSummary,
            tanColor,
            new Color(accentColor.r, accentColor.g, accentColor.b, 0.72f),
            creamColor,
            accentColor);

        CreateResultDivider(cardObj.transform, medalSectionEndY, cardW);

        // 기대 메시지 박스
        GameObject teaseObj = new GameObject("TeaseBox");
        teaseObj.transform.SetParent(cardObj.transform, false);
        Image teaseBg = teaseObj.AddComponent<Image>();
        teaseBg.type = Image.Type.Sliced;
        teaseBg.raycastTarget = false;
        teaseBg.sprite = WarmRoundedSprite.Get(new Color(1f, 0.78f, 0.30f, 0.09f), 14, new Color(1f, 0.78f, 0.30f, 0.30f), 1.5f);
        RectTransform teaseRect = teaseObj.GetComponent<RectTransform>();
        teaseRect.anchorMin = new Vector2(0.5f, 1f);
        teaseRect.anchorMax = new Vector2(0.5f, 1f);
        teaseRect.pivot = new Vector2(0.5f, 1f);
        teaseRect.anchoredPosition = new Vector2(0f, -(medalSectionEndY + 24f));
        teaseRect.sizeDelta = new Vector2(cardW - 72f, 116f);

        Text teaseText = CreateResultText(teaseObj.transform,
            GameLocalization.ResultDemoClearBody,
            19, FontStyle.Normal, new Color(1f, 0.88f, 0.62f, 1f), TextAnchor.MiddleCenter);
        teaseText.horizontalOverflow = HorizontalWrapMode.Wrap;
        teaseText.lineSpacing = 1.2f;
        RectTransform teaseTextRect = teaseText.GetComponent<RectTransform>();
        teaseTextRect.anchorMin = Vector2.zero;
        teaseTextRect.anchorMax = Vector2.one;
        teaseTextRect.offsetMin = new Vector2(16f, 8f);
        teaseTextRect.offsetMax = new Vector2(-16f, -8f);

        // 타이틀로 / 다시 플레이
        CreateResultActionButton(
            cardObj.transform,
            "ReturnTitleButton",
            GameLocalization.ResultReturnTitle,
            new Vector2(0f, 44f),
            new Vector2(480f, 52f),
            new Color(0.03f, 0.10f, 0.24f, 0.72f),
            new Color(0.45f, 0.80f, 1f, 0.58f),
            new Color(0.88f, 0.94f, 1f, 1f),
            ReturnToTitleScene);

        CreateResultActionButton(
            cardObj.transform,
            "ReplayButton",
            GameLocalization.ResultPlayAgain,
            new Vector2(0f, 108f),
            new Vector2(480f, 58f),
            new Color(0.95f, 0.60f, 0.16f, 1f),
            new Color(0f, 0f, 0f, 0f),
            new Color(1f, 0.98f, 0.93f, 1f),
            RestartGame);
    }

    Canvas FindRootOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        float bestArea = -1f;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            if (!c.isRootCanvas) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;

            // overrideSorting으로 분리된 서브 캔버스(상점/팝업/HUD)보다
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

        return best != null ? best : FindFirstObjectByType<Canvas>();
    }

    Text CreateResultText(Transform parent, string content, int fontSize, FontStyle style, Color color, TextAnchor anchor)
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
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    void CreateResultDivider(Transform parent, float topY, float cardW)
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
        rect.sizeDelta = new Vector2(cardW - 80f, 1.5f);
    }

    float AppendResultMedalBreakdown(
        Transform parent,
        float startTopY,
        float cardW,
        int roundReached,
        RunEndRewardCalculator.Summary summary,
        Color subLabelColor,
        Color subValueColor,
        Color totalLabelColor,
        Color totalValueColor)
    {
        const float subStep = 38f;
        float y = startTopY;

        CreateResultSubStatRow(
            parent,
            GameLocalization.ResultMedalsRoundLabel(roundReached),
            GameLocalization.ResultMedalsFormat(summary.roundMedals),
            y,
            cardW,
            subLabelColor,
            subValueColor);
        y += subStep;

        CreateResultSubStatRow(
            parent,
            GameLocalization.ResultMedalsMilestoneLabel(roundReached),
            GameLocalization.ResultMedalsFormat(summary.milestoneMedals),
            y,
            cardW,
            subLabelColor,
            subValueColor);
        y += subStep;

        CreateResultSubStatRow(
            parent,
            GameLocalization.ResultMedalsFirstMeetLabel(summary.firstMeetUnitCount),
            GameLocalization.ResultMedalsFormat(summary.firstMeetMedals),
            y,
            cardW,
            subLabelColor,
            subValueColor);
        y += subStep;

        CreateResultStatRow(
            parent,
            GameLocalization.ResultMedalsEarned,
            GameLocalization.ResultMedalsFormat(summary.totalMedals),
            y,
            cardW,
            totalLabelColor,
            totalValueColor);
        return y + 46f;
    }

    void CreateResultSubStatRow(Transform parent, string label, string value, float topY, float cardW, Color labelColor, Color valueColor)
    {
        GameObject rowObj = new GameObject("SubRow_" + label);
        rowObj.transform.SetParent(parent, false);
        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.type = Image.Type.Sliced;
        rowBg.sprite = WarmRoundedSprite.Get(new Color(1f, 0.88f, 0.66f, 0.03f), 8, new Color(0f, 0f, 0f, 0f), 0f);
        rowBg.raycastTarget = false;
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, -topY);
        rowRect.sizeDelta = new Vector2(cardW - 96f, 34f);

        Text labelText = CreateResultText(rowObj.transform, label, 19, FontStyle.Normal, labelColor, TextAnchor.MiddleLeft);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(22f, 0f);
        labelRect.sizeDelta = new Vector2(300f, 30f);

        Text valueText = CreateResultText(rowObj.transform, value, 21, FontStyle.Bold, valueColor, TextAnchor.MiddleRight);
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(-22f, 0f);
        valueRect.sizeDelta = new Vector2(160f, 30f);
    }

    void CreateResultStatRow(Transform parent, string label, string value, float topY, float cardW, Color labelColor, Color valueColor)
    {
        // 행 배경
        GameObject rowObj = new GameObject("Row_" + label);
        rowObj.transform.SetParent(parent, false);
        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.type = Image.Type.Sliced;
        rowBg.sprite = WarmRoundedSprite.Get(new Color(1f, 0.88f, 0.66f, 0.05f), 10, new Color(0f, 0f, 0f, 0f), 0f);
        rowBg.raycastTarget = false;
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, -topY);
        rowRect.sizeDelta = new Vector2(cardW - 80f, 42f);

        // 라벨 (좌측)
        Text labelText = CreateResultText(rowObj.transform, label, 23, FontStyle.Normal, labelColor, TextAnchor.MiddleLeft);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(18f, 0f);
        labelRect.sizeDelta = new Vector2(240f, 34f);

        // 값 (우측, 강조)
        Text valueText = CreateResultText(rowObj.transform, value, 25, FontStyle.Bold, valueColor, TextAnchor.MiddleRight);
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(-18f, 0f);
        valueRect.sizeDelta = new Vector2(240f, 34f);
    }

    Button CreateBossRewardActionButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color baseColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(label + "_Button");
        buttonObj.transform.SetParent(parent, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.sprite = WarmRoundedSprite.Get(Color.white, 14, new Color(0f, 0f, 0f, 0f), 0f);
        buttonImage.color = baseColor;
        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        Text buttonText = CreateResultText(buttonObj.transform, label, 26, FontStyle.Bold, new Color(1f, 0.98f, 0.93f, 1f), TextAnchor.MiddleCenter);
        buttonText.raycastTarget = false;
        RectTransform buttonTextRect = buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        return button;
    }

    void CreateBossRewardOptionRow(Transform parent, BossRewardOption option, float topY, float cardW, Color accentColor, Color tanColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject rowObj = new GameObject("RewardOption_" + option.buffType);
        rowObj.transform.SetParent(parent, false);
        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.type = Image.Type.Sliced;
        rowBg.sprite = WarmRoundedSprite.Get(new Color(0.36f, 0.27f, 0.20f, 1f), 14, new Color(1f, 0.85f, 0.55f, 0.10f), 1.5f);
        Button button = rowObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, -topY);
        rowRect.sizeDelta = new Vector2(cardW - 80f, 88f);

        GameObject rowAccentObj = new GameObject("RowAccent");
        rowAccentObj.transform.SetParent(rowObj.transform, false);
        Image rowAccentImage = rowAccentObj.AddComponent<Image>();
        rowAccentImage.raycastTarget = false;
        rowAccentImage.sprite = WarmRoundedSprite.Get(accentColor, 3, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform rowAccentRect = rowAccentObj.GetComponent<RectTransform>();
        rowAccentRect.anchorMin = new Vector2(0f, 0.5f);
        rowAccentRect.anchorMax = new Vector2(0f, 0.5f);
        rowAccentRect.pivot = new Vector2(0f, 0.5f);
        rowAccentRect.anchoredPosition = new Vector2(10f, 0f);
        rowAccentRect.sizeDelta = new Vector2(5f, 56f);

        Text titleText = CreateResultText(rowObj.transform, option.title, 24, FontStyle.Bold, accentColor, TextAnchor.UpperLeft);
        titleText.gameObject.name = "OptionTitle";
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(20f, -12f);
        titleRect.sizeDelta = new Vector2(-40f, 32f);

        Text descText = CreateResultText(rowObj.transform, option.description, 17, FontStyle.Normal, tanColor, TextAnchor.UpperLeft);
        descText.gameObject.name = "OptionDesc";
        RectTransform descRect = descText.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0f, 1f);
        descRect.anchoredPosition = new Vector2(20f, -46f);
        descRect.sizeDelta = new Vector2(-40f, 34f);
    }
    
    void RefreshOpenResultPanels()
    {
        if (demoClearPanel != null && cachedDemoClearSummaryValid)
        {
            Destroy(demoClearPanel);
            demoClearPanel = null;
            ShowDemoClearUI(applyRewards: false);
        }

        if (gameOverPanel != null && cachedGameOverSummaryValid)
        {
            Destroy(gameOverPanel);
            gameOverPanel = null;
            ShowGameOverUI(applyRewards: false);
        }

        RefreshBossRewardPanelLocalized();
    }

    void RefreshBossRewardPanelLocalized()
    {
        if (bossRewardPanel == null) return;

        Transform card = bossRewardPanel.transform.Find("RewardCard");
        if (card == null) return;

        Font font = UIFontProvider.Get();
        SetNamedText(card, "BossRewardTitle", GameLocalization.BossRewardTitle, font);
        SetNamedText(card, "BossRewardSubtitle", GameLocalization.BossRewardSubtitle, font);
        SetNamedText(card, "BossRewardHint", GameLocalization.BossRewardPickOne, font);
        SetNamedText(card, "BossRewardEmpty", GameLocalization.BossRewardAllClaimed, font);

        for (int i = 0; i < card.childCount; i++)
        {
            Transform row = card.GetChild(i);
            if (row == null || !row.name.StartsWith("RewardOption_")) continue;
            if (!System.Enum.TryParse(row.name.Substring("RewardOption_".Length), out BossTileBuffType buff)) continue;

            Transform titleTransform = row.Find("OptionTitle");
            Transform descTransform = row.Find("OptionDesc");
            if (titleTransform != null)
            {
                Text title = titleTransform.GetComponent<Text>();
                if (title != null)
                {
                    title.font = font;
                    title.text = GetBuffTitle(buff);
                }
            }
            if (descTransform != null)
            {
                Text desc = descTransform.GetComponent<Text>();
                if (desc != null)
                {
                    desc.font = font;
                    desc.text = GetBuffDescriptionShort(buff);
                }
            }
        }

        Button confirmButton = card.GetComponentInChildren<Button>(true);
        if (confirmButton != null)
        {
            Text label = confirmButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.font = font;
                label.text = GameLocalization.BossRewardConfirm;
            }
        }
    }

    static void SetNamedText(Transform root, string objectName, string value, Font font)
    {
        Transform target = root.Find(objectName);
        if (target == null) return;
        Text text = target.GetComponent<Text>();
        if (text == null) return;
        text.font = font;
        text.text = value;
    }

    string FormatTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        return $"{min:00}:{sec:00}";
    }
    
    /// <summary>
    /// 게임이 진행 중인지 반환합니다
    /// </summary>
    public bool IsGameActive()
    {
        return gameActive;
    }
    
    /// <summary>
    /// 골드를 소비해 경험치를 획득합니다 (4골드 소비, 경험치 +2)
    /// </summary>
    public bool UpgradePlayerLevel()
    {
        if (playerLevel >= maxPlayerLevel)
        {
            Debug.Log("이미 최대 레벨입니다.");
            return false;
        }

        // 골드 4개 소비 후 경험치 +2
        if (SpendGold(4))
        {
            int beforeLevel = playerLevel;
            GainExperience(2);
            UpdateUI();
            if (playerLevel > beforeLevel)
            {
                Debug.Log($"업그레이드 구매로 레벨이 {playerLevel}로 올랐습니다!");
            }
            else
            {
                Debug.Log($"업그레이드 구매: 경험치 +2 ({currentExperience}/{GetRequiredExperienceForLevel(playerLevel)})");
            }
            return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다! (4골드 필요)");
            return false;
        }
    }

    /// <summary>
    /// 무료로 유저 레벨을 업그레이드합니다
    /// </summary>
    public void UpgradePlayerLevelFree()
    {
        if (playerLevel >= maxPlayerLevel)
        {
            Debug.Log("이미 최대 레벨입니다.");
            return;
        }
        playerLevel++;
        UpdateUI();
        NotifyPlacementLimitChanged();
        LevelUpUnitSelectionController.EnqueueLevelUps(new List<int> { playerLevel });
        Debug.Log($"무료 업그레이드: 유저 레벨 {playerLevel}");
    }

    void GainExperience(int amount)
    {
        if (amount <= 0) return;
        if (playerLevel >= maxPlayerLevel)
        {
            currentExperience = 0;
            return;
        }

        currentExperience += amount;
        bool leveledUp = false;
        var gainedLevels = new List<int>();
        while (playerLevel < maxPlayerLevel)
        {
            int required = GetRequiredExperienceForLevel(playerLevel);
            if (currentExperience < required) break;

            currentExperience -= required;
            playerLevel++;
            leveledUp = true;
            gainedLevels.Add(playerLevel);
            Debug.Log($"경험치 레벨업! 레벨 {playerLevel}");
        }

        if (playerLevel >= maxPlayerLevel)
        {
            currentExperience = 0;
        }

        if (leveledUp)
        {
            UpdateUI();
            NotifyPlacementLimitChanged();
            LevelUpUnitSelectionController.EnqueueLevelUps(gainedLevels);
        }
    }

    int GetRequiredExperienceForLevel(int level)
    {
        int safeLevel = Mathf.Clamp(level, 1, levelRequiredExperience.Length - 1);
        return levelRequiredExperience[safeLevel];
    }

    void NotifyPlacementLimitChanged()
    {
        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.UpdatePlacementCountUI();
            controller.UpdateShopOddsUI();
        }
    }

    void UpdateRoundWaveIndicatorUI()
    {
        if (roundWaveIndicatorRect != null)
        {
            int count = Mathf.Max(1, roundWaveSlotCount);
            float width = Mathf.Max(1f, roundWaveSlotWidth);
            float gap = Mathf.Max(0f, roundWaveSlotGap);
            int index = Mathf.Abs(currentRound - 1) % count;

            float totalWidth = (count * width) + ((count - 1) * gap);
            float startX = -totalWidth * 0.5f + width * 0.5f;
            float x = startX + index * (width + gap);
            float bob = Mathf.Sin(Time.time * Mathf.Max(0f, roundWaveIndicatorBobSpeed)) * Mathf.Max(0f, roundWaveIndicatorBobAmplitude);
            roundWaveIndicatorRect.anchoredPosition = new Vector2(x, roundWaveIndicatorBaseY + bob);

            Text indicatorText = roundWaveIndicatorRect.GetComponent<Text>();
            if (indicatorText != null)
            {
                indicatorText.color = new Color(1f, 0.9f, 0.2f, 1f); // 노란색
            }

            Image indicatorImage = roundWaveIndicatorRect.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = new Color(1f, 0.9f, 0.2f, 1f); // 노란색
            }
        }

        UpdateRoundWaveSlotIcons();
    }

    void UpdateRoundWaveSlotIcons()
    {
        if (roundWaveSlotImages == null || roundWaveSlotImages.Length == 0) return;

        int blockStart = ((Mathf.Max(1, currentRound) - 1) / 10) * 10 + 1;
        int slotCount = Mathf.Min(roundWaveSlotImages.Length, 10);
        int currentSlotIndex = Mathf.Abs(currentRound - 1) % Mathf.Max(1, slotCount);

        // 블록이 바뀌면 아이콘 리소스를 다시 바인딩
        if (blockStart != lastRoundPartyBlockStart)
        {
            for (int i = 0; i < slotCount; i++)
            {
                Image slotImage = roundWaveSlotImages[i];
                if (slotImage == null) continue;

                int targetRound = blockStart + i;
                string resourceName = RoundWaveController.GetPartyIconResourceForRound(targetRound);
                Sprite icon = LoadRoundPartyIcon(resourceName);
                slotImage.sprite = icon;
            }

            lastRoundPartyBlockStart = blockStart;
        }

        // 현재 라운드 슬롯만 컬러, 나머지는 흑백 처리
        for (int i = 0; i < slotCount; i++)
        {
            Image slotImage = roundWaveSlotImages[i];
            if (slotImage == null) continue;

            bool isCurrent = i == currentSlotIndex;
            bool hasIcon = slotImage.sprite != null;
            if (isCurrent)
            {
                slotImage.color = hasIcon ? Color.white : new Color(0.45f, 0.12f, 0.12f, 0.92f);
            }
            else
            {
                slotImage.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            }
        }
    }

    Sprite LoadRoundPartyIcon(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }

        if (roundPartyIconCache.TryGetValue(resourceName, out Sprite cached))
        {
            return cached;
        }

        Sprite sprite = Resources.Load<Sprite>(resourceName);
        roundPartyIconCache[resourceName] = sprite;
        return sprite;
    }
    
    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// 타이틀 화면으로 돌아갑니다.
    /// </summary>
    public void ReturnToTitleScene()
    {
        Time.timeScale = 1f;
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneLoader.SceneType.TitleScene);
            return;
        }

        SceneManager.LoadScene("TitleScene");
    }

    void CreateResultActionButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Color fillColor,
        Color borderColor,
        Color textColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.sprite = WarmRoundedSprite.Get(fillColor, 14, borderColor, borderColor.a > 0.01f ? 1.6f : 0f);
        buttonImage.color = Color.white;
        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        Text buttonText = CreateResultText(buttonObj.transform, label, 24, FontStyle.Bold, textColor, TextAnchor.MiddleCenter);
        buttonText.raycastTarget = false;
        RectTransform buttonTextRect = buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
    }

    struct BossRewardOption
    {
        public BossTileBuffType buffType;
        public string title;
        public string description;
    }

    IEnumerator ShowBossRewardSequence()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        if (bossRewardPanel == null && !bossRewardShown)
        {
            ShowBossRewardChoices();
        }
    }

    void ShowBossRewardChoices()
    {
        if (bossRewardPanel != null) return;
        bossRewardShown = true;
        SetShopOpen(true);

        Canvas canvas = FindRootOverlayCanvas();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            MobileUIScaling.Configure(canvasObj.AddComponent<CanvasScaler>());
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Color accentColor = new Color(1f, 0.78f, 0.30f, 1f);
        Color creamColor = new Color(0.93f, 0.86f, 0.74f, 1f);
        Color tanColor = new Color(0.80f, 0.66f, 0.48f, 1f);

        List<BossRewardOption> options = BuildBossRewardOptions();
        Shuffle(options);
        int showCount = Mathf.Min(3, options.Count);

        const float cardW = 660f;
        const float optionRowH = 88f;
        const float optionGap = 12f;
        float cardH = showCount > 0
            ? 176f + showCount * optionRowH + (showCount - 1) * optionGap + 48f
            : 420f;

        bossRewardPanel = new GameObject("BossRewardPanel");
        bossRewardPanel.transform.SetParent(canvas.transform, false);
        Canvas overlayCanvas = bossRewardPanel.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 400;
        bossRewardPanel.AddComponent<GraphicRaycaster>();
        Image panelImage = bossRewardPanel.AddComponent<Image>();
        Color dimBase = new Color(0.04f, 0.03f, 0.02f, 0.92f);
        panelImage.color = new Color(dimBase.r, dimBase.g, dimBase.b, 0f);
        RectTransform panelRect = bossRewardPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        CanvasGroup panelGroup = bossRewardPanel.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;

        GameObject glowObj = new GameObject("CardGlow");
        glowObj.transform.SetParent(bossRewardPanel.transform, false);
        Image glowImage = glowObj.AddComponent<Image>();
        glowImage.type = Image.Type.Sliced;
        glowImage.raycastTarget = false;
        glowImage.sprite = WarmRoundedSprite.Get(new Color(1f, 0.82f, 0.35f, 0.18f), 44, new Color(1f, 0.90f, 0.50f, 0.35f), 3f);
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(cardW + 56f, cardH + 56f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.localScale = Vector3.one * 0.78f;

        GameObject shadowObj = new GameObject("CardShadow");
        shadowObj.transform.SetParent(bossRewardPanel.transform, false);
        Image shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.type = Image.Type.Sliced;
        shadowImage.raycastTarget = false;
        shadowImage.sprite = WarmRoundedSprite.Get(new Color(0f, 0f, 0f, 0.35f), 40, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(cardW + 36f, cardH + 36f);
        shadowRect.anchoredPosition = new Vector2(0f, -12f);
        shadowRect.localScale = Vector3.one * 0.78f;

        GameObject cardObj = new GameObject("RewardCard");
        cardObj.transform.SetParent(bossRewardPanel.transform, false);
        Image cardImage = cardObj.AddComponent<Image>();
        cardImage.type = Image.Type.Sliced;
        cardImage.sprite = WarmRoundedSprite.Get(new Color(0.155f, 0.115f, 0.082f, 0.99f), 30, new Color(1f, 0.85f, 0.55f, 0.22f), 3f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(cardW, cardH);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one * 0.78f;
        CanvasGroup cardGroup = cardObj.AddComponent<CanvasGroup>();
        cardGroup.alpha = 0f;

        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(cardObj.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.type = Image.Type.Sliced;
        accentImage.raycastTarget = false;
        accentImage.sprite = WarmRoundedSprite.Get(accentColor, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -22f);
        accentRect.sizeDelta = new Vector2(120f, 6f);

        Text trophyText = CreateResultText(cardObj.transform, "★", 28, FontStyle.Bold, new Color(1f, 0.90f, 0.45f, 1f), TextAnchor.MiddleCenter);
        trophyText.raycastTarget = false;
        RectTransform trophyRect = trophyText.GetComponent<RectTransform>();
        trophyRect.anchorMin = new Vector2(0.5f, 1f);
        trophyRect.anchorMax = new Vector2(0.5f, 1f);
        trophyRect.pivot = new Vector2(0.5f, 0.5f);
        trophyRect.anchoredPosition = new Vector2(0f, -38f);
        trophyRect.sizeDelta = new Vector2(48f, 36f);

        Text titleText = CreateResultText(cardObj.transform, GameLocalization.BossRewardTitle, 44, FontStyle.Bold, accentColor, TextAnchor.UpperCenter);
        titleText.gameObject.name = "BossRewardTitle";
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -58f);
        titleRect.sizeDelta = new Vector2(520f, 54f);

        Text subtitleText = CreateResultText(cardObj.transform, GameLocalization.BossRewardSubtitle, 20, FontStyle.Bold, new Color(1f, 0.92f, 0.58f, 0.92f), TextAnchor.UpperCenter);
        subtitleText.gameObject.name = "BossRewardSubtitle";
        RectTransform subtitleRect = subtitleText.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -116f);
        subtitleRect.sizeDelta = new Vector2(520f, 28f);

        CreateResultDivider(cardObj.transform, 146f, cardW);

        if (showCount <= 0)
        {
            Text emptyText = CreateResultText(cardObj.transform,
                GameLocalization.BossRewardAllClaimed,
                22, FontStyle.Normal, creamColor, TextAnchor.UpperCenter);
            emptyText.gameObject.name = "BossRewardEmpty";
            RectTransform emptyRect = emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.5f, 1f);
            emptyRect.anchorMax = new Vector2(0.5f, 1f);
            emptyRect.pivot = new Vector2(0.5f, 1f);
            emptyRect.anchoredPosition = new Vector2(0f, -180f);
            emptyRect.sizeDelta = new Vector2(cardW - 80f, 90f);

            CreateResultDivider(cardObj.transform, 280f, cardW);
            CreateBossRewardActionButton(cardObj.transform, GameLocalization.BossRewardConfirm, new Vector2(0f, 44f), new Vector2(480f, 62f),
                new Color(0.95f, 0.60f, 0.16f, 1f), CloseBossRewardPanel);
        }
        else
        {
            Text hintText = CreateResultText(cardObj.transform, GameLocalization.BossRewardPickOne, 18, FontStyle.Normal, tanColor, TextAnchor.UpperCenter);
            hintText.gameObject.name = "BossRewardHint";
            RectTransform hintRect = hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 1f);
            hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -166f);
            hintRect.sizeDelta = new Vector2(520f, 24f);

            float rowTopY = 210f;
            for (int i = 0; i < showCount; i++)
            {
                BossRewardOption option = options[i];
                BossTileBuffType capturedBuff = option.buffType;
                float topY = rowTopY + i * (optionRowH + optionGap);
                CreateBossRewardOptionRow(cardObj.transform, option, topY, cardW, accentColor, tanColor, () =>
                {
                    ApplyBossReward(capturedBuff);
                    CloseBossRewardPanel();
                });
            }
        }

        GameSfxPlayer.PlayUnitCardReveal();
        StartCoroutine(PlayBossRewardCelebration(panelGroup, panelImage, dimBase, cardRect, shadowRect, glowRect, cardGroup, titleText));
    }

    IEnumerator PlayBossRewardCelebration(
        CanvasGroup panelGroup,
        Image panelDimImage,
        Color dimBase,
        RectTransform cardRect,
        RectTransform shadowRect,
        RectTransform glowRect,
        CanvasGroup cardGroup,
        Text titleText)
    {
        LevelUpSelectionCelebrationFx fx = LevelUpSelectionCelebrationFx.Create(bossRewardPanel.transform);
        RectTransform panelRect = bossRewardPanel.GetComponent<RectTransform>();
        fx.SpawnConfetti(panelRect, LevelUpSelectionCelebrationFx.Intensity.Full);
        if (cardRect != null)
        {
            fx.PlayGoldenBurst(cardRect, LevelUpSelectionCelebrationFx.Intensity.Full);
        }
        StartCoroutine(fx.PlayCheerBanner(bossRewardPanel.transform, LevelUpSelectionCelebrationFx.Intensity.Full));
        StartCoroutine(fx.PlayTitlePop(titleText, LevelUpSelectionCelebrationFx.Intensity.Full));

        const float dur = 0.58f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float goldPulse = Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI) * 0.06f;

            if (panelGroup != null) panelGroup.alpha = ease;
            if (panelDimImage != null)
            {
                panelDimImage.color = new Color(
                    dimBase.r + goldPulse,
                    dimBase.g + goldPulse * 0.85f,
                    dimBase.b + goldPulse * 0.4f,
                    dimBase.a * ease);
            }

            float cardScale = Mathf.Lerp(0.78f, 1f, ease);
            if (cardRect != null) cardRect.localScale = Vector3.one * cardScale;
            if (shadowRect != null) shadowRect.localScale = Vector3.one * cardScale;
            if (glowRect != null) glowRect.localScale = Vector3.one * cardScale;
            if (cardGroup != null) cardGroup.alpha = ease;

            yield return null;
        }

        if (panelGroup != null) panelGroup.alpha = 1f;
        if (cardGroup != null) cardGroup.alpha = 1f;
        if (panelDimImage != null) panelDimImage.color = dimBase;
        if (cardRect != null) cardRect.localScale = Vector3.one;
        if (shadowRect != null) shadowRect.localScale = Vector3.one;
        if (glowRect != null) glowRect.localScale = Vector3.one;
    }

    void CloseBossRewardPanel()
    {
        if (bossRewardPanel != null)
        {
            Destroy(bossRewardPanel);
            bossRewardPanel = null;
        }
        bossRewardShown = false;
        bossRewardChestSpawned = false;
        SetShopOpen(false);
    }

    void SpawnBossRewardChest(Vector3 position)
    {
        bossRewardChestSpawned = true;

        GameObject chestObj = new GameObject("BossRewardChest");
        chestObj.transform.position = position;

        SpriteRenderer renderer = chestObj.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite(new Color(0.55f, 0.3f, 0.1f));
        renderer.color = new Color(0.55f, 0.3f, 0.1f);
        renderer.sortingOrder = 2;

        // 유닛 1마리와 비슷한 크기
        chestObj.transform.localScale = Vector3.one * 0.6f;

        BoxCollider2D collider = chestObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        BossRewardChest chest = chestObj.AddComponent<BossRewardChest>();
        chest.SetOwner(this);
    }

    public void TryOpenBossRewardFromChest()
    {
        if (bossRewardShown || bossRewardPanel != null) return;
        ShowBossRewardChoices();
    }

    List<BossRewardOption> BuildBossRewardOptions()
    {
        BossTileBuffType[] allTypes =
        {
            BossTileBuffType.CenterPower,
            BossTileBuffType.BacklinePower,
            BossTileBuffType.CornerPower,
            BossTileBuffType.FrontlineHaste,
            BossTileBuffType.LeftShield,
            BossTileBuffType.RightHaste,
            BossTileBuffType.MiddleColumnPower
        };

        var options = new List<BossRewardOption>();
        for (int i = 0; i < allTypes.Length; i++)
        {
            BossTileBuffType buffType = allTypes[i];
            if (acquiredBossTileBuffs.Contains(buffType)) continue;
            options.Add(new BossRewardOption
            {
                buffType = buffType,
                title = GetBuffTitle(buffType),
                description = GetBuffDescriptionShort(buffType)
            });
        }
        return options;
    }

    void ApplyBossReward(BossTileBuffType buffType)
    {
        acquiredBossTileBuffs.Add(buffType);
        RefreshBossTileHighlights();
        RefreshUnitBoardBuffAuras();
    }

    public float GetBoardDamageMultiplier(int row, int col)
    {
        float multiplier = 1f;
        foreach (BossTileBuffType buff in acquiredBossTileBuffs)
        {
            if (!IsBuffCell(buff, row, col)) continue;
            float buffMul = GetBossDamageMultiplierForBuff(buff);
            if (buffMul > 1.001f)
            {
                multiplier *= buffMul;
            }
        }
        return multiplier;
    }

    public float GetBoardAttackSpeedMultiplier(int row, int col)
    {
        float multiplier = 1f;
        foreach (BossTileBuffType buff in acquiredBossTileBuffs)
        {
            if (!IsBuffCell(buff, row, col)) continue;
            float buffMul = GetBossAttackSpeedMultiplierForBuff(buff);
            if (buffMul > 1.001f)
            {
                multiplier *= buffMul;
            }
        }
        multiplier *= GetUnit10PadAttackSpeedMultiplierAtCell(row, col);
        return multiplier;
    }

    public float GetBoardRangeBonus(int row, int col)
    {
        float bonus = 0f;
        foreach (BossTileBuffType buff in acquiredBossTileBuffs)
        {
            if (!IsBuffCell(buff, row, col)) continue;
            bonus += GetBossRangeBonusForBuff(buff);
        }
        return bonus;
    }

    /// <summary>중앙 공명 — 해당 칸 유닛의 조합 기여(기본 1 → 2).</summary>
    public int GetBossTraitCountContribution(Character unit)
    {
        if (unit == null || !unit.IsProperlyPlaced()) return 1;
        if (!acquiredBossTileBuffs.Contains(BossTileBuffType.CenterPower)) return 1;
        if (!TryGetCharacterBoardCell(unit, out int row, out int col)) return 1;
        if (!IsBuffCell(BossTileBuffType.CenterPower, row, col)) return 1;
        return 2;
    }

    /// <summary>중앙 열 증폭 — 조합 특수공격 피해 배율.</summary>
    public float GetBossTraitPeriodicDamageMultiplier(Character anchor)
    {
        if (anchor == null || !anchor.IsProperlyPlaced()) return 1f;
        if (!acquiredBossTileBuffs.Contains(BossTileBuffType.MiddleColumnPower)) return 1f;
        if (!TryGetCharacterBoardCell(anchor, out int row, out int col)) return 1f;
        if (!IsBuffCell(BossTileBuffType.MiddleColumnPower, row, col)) return 1f;
        return 1.2f;
    }

    static float GetUnit10PadAttackSpeedMultiplierAtCell(int row, int col)
    {
        float mul = 1f;
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character src = units[i];
            if (src == null || src.unitNumber != 10 || !src.IsProperlyPlaced()) continue;
            if (!TryGetCharacterBoardCell(src, out int srcRow, out int srcCol)) continue;
            if (col != srcCol) continue;
            if (row == srcRow - 1 || row == srcRow + 1)
            {
                mul *= UnitEvolutionMilestones.GetUnit10PadAttackSpeedMul(src.evolutionLevel);
            }
        }
        return mul;
    }

    /// <summary>조합 시너지 UI — 현재 보드의 보스·조합 시너지·유닛 효과를 수집합니다.</summary>
    public void GetAllBoardBuffOverviews(List<BoardBuffOverviewEntry> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();

        BossTileBuffType[] orderedBoss =
        {
            BossTileBuffType.CenterPower,
            BossTileBuffType.FrontlineHaste,
            BossTileBuffType.BacklinePower,
            BossTileBuffType.LeftShield,
            BossTileBuffType.RightHaste,
            BossTileBuffType.MiddleColumnPower,
            BossTileBuffType.CornerPower
        };

        for (int i = 0; i < orderedBoss.Length; i++)
        {
            BossTileBuffType buff = orderedBoss[i];
            if (!acquiredBossTileBuffs.Contains(buff)) continue;

            buffer.Add(new BoardBuffOverviewEntry
            {
                category = GameLocalization.BoardBuffCategoryKey.Boss,
                title = GetBuffTitle(buff),
                location = GetBossBuffLocationLabel(buff),
                effect = GetBuffEffectText(buff),
                isAttackSpeed = ProvidesAttackSpeedBuff(buff),
                isRangeBuff = ProvidesRangeBuff(buff),
                isTraitBuff = ProvidesTraitBuff(buff),
                traitName = null,
                sourceUnitNumber = 0
            });
        }

        /*
        for (int i = 0; i < synergyCells.Count; i++)
        {
            SynergyCellBuff sc = synergyCells[i];
            string colorName = GetSynergyCellColorName(sc.trait);
            Character occupant = FindCharacterOnCell(sc.row, sc.col);
            bool matched = occupant != null && UnitHasTrait(occupant.unitNumber, sc.trait);
            string placementNote = matched
                ? "해당 조합 유닛 배치됨"
                : "해당 조합 유닛 미배치";

            buffer.Add(new BoardBuffOverviewEntry
            {
                category = "조합 시너지",
                title = $"{sc.trait} 시너지 칸",
                location = $"{colorName} 칸 (행 {sc.row + 1}, 열 {sc.col + 1}) · {placementNote}",
                effect = GetSynergyCellBonusDescription(sc.trait),
                isAttackSpeed = false,
                traitName = sc.trait,
                sourceUnitNumber = 0
            });
        }
        */

        AppendUnitEffectBoardOverviews(buffer);
    }

    public static string GetSynergyCellColorName(string trait)
    {
        return "시너지";
        /*
        int idx = GetSynergyCellIndex(trait);
        if (idx < 0 || idx >= SynergyCellDefs.Length) return "시너지";
        return SynergyCellDefs[idx].colorName;
        */
    }

    string GetBossBuffLocationLabel(BossTileBuffType buff)
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();
        int rows = board != null ? Mathf.Max(1, board.boardRows) : 5;
        int cols = board != null ? Mathf.Max(1, board.boardColumns) : 5;
        int centerRow = rows / 2;
        int centerCol = cols / 2;

        switch (buff)
        {
            case BossTileBuffType.CenterPower:
                return GameLocalization.BossBuffLocCenterCell(centerRow + 1, centerCol + 1);
            case BossTileBuffType.FrontlineHaste:
                return GameLocalization.BossBuffLocFrontlineColumn(cols);
            case BossTileBuffType.BacklinePower:
                return GameLocalization.BossBuffLocBacklineColumn;
            case BossTileBuffType.LeftShield:
                return GameLocalization.BossBuffLocTopRow;
            case BossTileBuffType.RightHaste:
                return GameLocalization.BossBuffLocBottomRow(rows);
            case BossTileBuffType.MiddleColumnPower:
                return GameLocalization.BossBuffLocCenterColumn(centerCol + 1);
            case BossTileBuffType.CornerPower:
                return GameLocalization.BossBuffLocCorners;
            default:
                return GameLocalization.BossBuffLocBoard;
        }
    }

    void RefreshBossTileHighlights()
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        for (int i = 0; i < cells.Length; i++)
        {
            BoardCell cell = cells[i];
            if (cell == null || !cell.isBoardCell) continue;

            if (!TryParseBoardCellName(cell.gameObject.name, out int row, out int col))
            {
                cell.SetBuffIndicators(Color.clear, false, null, null);
                continue;
            }

            Color mixed = Color.clear;
            int colorCount = 0;
            List<Color> iconColors = new List<Color>(4);
            List<Sprite> iconSprites = new List<Sprite>(4);
            foreach (BossTileBuffType buff in acquiredBossTileBuffs)
            {
                if (!IsBuffCell(buff, row, col)) continue;
                Color buffColor = GetBuffHighlightColor(buff);
                mixed += buffColor;
                colorCount++;
                iconColors.Add(buffColor);
                iconSprites.Add(GetBuffIconSprite(buff));
            }

            /*
            // [비활성] 조합 시너지 칸(레이어 A) 오버레이
            for (int r = 0; r < synergyCells.Count; r++)
            {
                SynergyCellBuff sc = synergyCells[r];
                if (sc.row != row || sc.col != col) continue;
                Color traitColor = GetTraitCellColor(sc.trait);
                mixed += traitColor;
                colorCount++;
                iconColors.Add(traitColor);
                iconSprites.Add(GetTraitCellIcon(sc.trait));
            }
            */

            if (TraitManager.Instance != null && TraitManager.Instance.IsBoardCellInPenguinAura(row, col))
            {
                Color auraColor = GetTraitCellColor("펭귄");
                mixed += auraColor;
                colorCount++;
                iconColors.Add(auraColor);
                iconSprites.Add(GetTraitCellIcon("펭귄"));
            }

            if (CellHasUnitRangeMarkerEffect(row, col, out Color rangeColor, out Sprite rangeIcon))
            {
                mixed += rangeColor;
                colorCount++;
                iconColors.Add(rangeColor);
                iconSprites.Add(rangeIcon);
            }

            if (colorCount <= 0)
            {
                cell.SetBuffIndicators(Color.clear, false, null, null);
                continue;
            }

            mixed /= colorCount;
            mixed.a = 0.28f;
            cell.SetBuffIndicators(mixed, true, iconSprites, iconColors);
        }
    }

    /// <summary>[비활성] 발동 중인 조합에 맞춰 시너지 칸을 갱신합니다.</summary>
    void RefreshSynergyCells()
    {
        synergyCells.Clear();
        lastSynergySignature = string.Empty;
        /*
        IReadOnlyList<string> active = TraitManager.Instance != null
            ? TraitManager.Instance.GetActiveTraits()
            : null;

        string signature = (active != null && active.Count > 0) ? string.Join(",", active) : "";
        if (signature == lastSynergySignature) return;
        lastSynergySignature = signature;

        synergyCells.Clear();
        if (active != null && active.Count > 0)
        {
            BoardManager board = FindFirstObjectByType<BoardManager>();
            int rows = board != null ? Mathf.Max(1, board.boardRows) : 5;
            int cols = board != null ? Mathf.Max(1, board.boardColumns) : 3;
            int total = rows * cols;

            for (int i = 0; i < active.Count; i++)
            {
                string trait = active[i];
                int idx = GetSynergyCellIndex(trait);
                if (idx < 0) continue;
                idx %= total;
                synergyCells.Add(new SynergyCellBuff { row = idx / cols, col = idx % cols, trait = trait });
            }
        }

        RefreshBossTileHighlights();
        */
    }

    /*
    static int GetSynergyCellIndex(string trait)
    {
        for (int i = 0; i < SynergyCellDefs.Length; i++)
        {
            if (SynergyCellDefs[i].trait == trait) return i;
        }
        return -1;
    }
    */

    /// <summary>[비활성] 해당 조합이 시너지 칸을 생성하는지 여부.</summary>
    public static bool TraitHasSynergyCell(string trait)
    {
        return false;
    }

    /// <summary>[비활성] 조합 툴팁 시너지 칸 안내.</summary>
    public static string GetSynergyCellTooltipLine(string trait)
    {
        return string.Empty;
        /*
        int idx = GetSynergyCellIndex(trait);
        if (idx < 0) return string.Empty;

        return $"{SynergyCellDefs[idx].colorName} 칸 · {trait} 유닛 배치 시 특수 공격 2배";
        */
    }

    /// <summary>[비활성] 시너지 칸 보너스 설명.</summary>
    public static string GetSynergyCellBonusDescription(string trait)
    {
        return string.Empty;
        /*
        switch (trait)
        {
            default:
                return "해당 조합 유닛 배치 시 특수 공격 2배";
        }
        */
    }

    /// <summary>[비활성] 해당 칸에 같은 조합 유닛이 올라가 시너지 보너스를 받는지 확인.</summary>
    public bool IsUnitOnMatchingSynergyCell(int unitNumber, int row, int col, string trait)
    {
        return false;
        /*
        if (string.IsNullOrEmpty(trait) || !UnitHasTrait(unitNumber, trait)) return false;
        for (int i = 0; i < synergyCells.Count; i++)
        {
            SynergyCellBuff sc = synergyCells[i];
            if (sc.trait == trait && sc.row == row && sc.col == col) return true;
        }
        return false;
        */
    }

    /// <summary>[비활성] 시너지 칸 위에 해당 조합 유닛이 배치되어 있는지.</summary>
    public bool HasMatchingUnitOnSynergyCell(string trait)
    {
        return false;
        /*
        if (string.IsNullOrEmpty(trait)) return false;
        for (int i = 0; i < synergyCells.Count; i++)
        {
            SynergyCellBuff sc = synergyCells[i];
            if (sc.trait != trait) continue;
            Character occupant = FindCharacterOnCell(sc.row, sc.col);
            if (occupant != null && UnitHasTrait(occupant.unitNumber, trait)) return true;
        }
        return false;
        */
    }

    static bool UnitHasTrait(int unitNumber, string trait)
    {
        if (string.IsNullOrEmpty(trait)) return false;
        string[] traits = UnitTraitData.GetTraits(unitNumber);
        for (int i = 0; i < traits.Length; i++)
        {
            if (traits[i] == trait) return true;
        }
        return false;
    }

    Character FindCharacterOnCell(int row, int col)
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        for (int i = 0; i < cells.Length; i++)
        {
            BoardCell cell = cells[i];
            if (cell == null || !cell.isBoardCell) continue;
            if (!TryParseBoardCellName(cell.gameObject.name, out int r, out int c)) continue;
            if (r == row && c == col) return cell.currentCharacter;
        }
        return null;
    }

    void RefreshUnitPlacementBuffHighlights()
    {
        string signature = BuildUnitPlacementSignature();
        if (signature == lastUnitPlacementSignature) return;
        lastUnitPlacementSignature = signature;
        RefreshBossTileHighlights();
    }

    void RefreshUnitBoardBuffAuras()
    {
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || unit.isShopPreviewInstance)
            {
                continue;
            }

            if (!unit.IsProperlyPlaced() || !TryGetCharacterBoardCell(unit, out int row, out int col))
            {
                BoardBuffUnitAura.Clear(unit);
                continue;
            }

            if (TryGetUnitBoardBuffAuraVisual(unit.unitNumber, row, col, out Color tint, out int sourceCount, out bool adjacentProvider))
            {
                BoardBuffUnitAura.Sync(unit, true, tint, sourceCount, adjacentProvider);
            }
            else
            {
                BoardBuffUnitAura.Clear(unit);
            }
        }
    }

    /// <summary>배치칸·시너지·인접 버프(수혜·제공) 유닛의 상시 이펙트 색.</summary>
    public bool TryGetUnitBoardBuffAuraVisual(int unitNumber, int row, int col, out Color tint, out int sourceCount, out bool adjacentProvider)
    {
        tint = Color.clear;
        sourceCount = 0;
        adjacentProvider = false;
        Color sum = Color.black;

        if (GetBoardDamageMultiplier(row, col) > 1.001f)
        {
            sum += new Color(1f, 0.75f, 0.2f, 1f);
            sourceCount++;
        }

        if (GetBoardAttackSpeedMultiplier(row, col) > 1.001f)
        {
            sum += new Color(0.2f, 1f, 0.75f, 1f);
            sourceCount++;
        }

        if (GetBoardRangeBonus(row, col) > 0.001f)
        {
            sum += new Color(0.55f, 0.82f, 1f, 1f);
            sourceCount++;
        }

        if (acquiredBossTileBuffs.Contains(BossTileBuffType.CenterPower)
            && IsBuffCell(BossTileBuffType.CenterPower, row, col))
        {
            sum += new Color(0.92f, 0.55f, 1f, 1f);
            sourceCount++;
        }

        /*
        string[] traits = UnitTraitData.GetTraits(unitNumber);
        if (traits != null)
        {
            for (int i = 0; i < traits.Length; i++)
            {
                string trait = traits[i];
                if (string.IsNullOrEmpty(trait)) continue;
                if (!IsUnitOnMatchingSynergyCell(unitNumber, row, col, trait)) continue;

                Color traitColor = GetTraitCellColor(trait);
                sum.r += traitColor.r;
                sum.g += traitColor.g;
                sum.b += traitColor.b;
                sourceCount++;
            }
        }
        */

        if (TraitManager.Instance != null && TraitManager.Instance.IsPenguinAuraActive())
        {
            Color penguin = GetTraitCellColor("펭귄");

            if (TraitManager.Instance.IsBoardCellInPenguinAura(row, col))
            {
                sum.r += penguin.r;
                sum.g += penguin.g;
                sum.b += penguin.b;
                sourceCount++;
            }

            if (UnitProvidesAdjacentBoardBuff(unitNumber))
            {
                sum.r += penguin.r;
                sum.g += penguin.g;
                sum.b += penguin.b;
                sourceCount++;
                adjacentProvider = true;
            }
        }

        if (sourceCount <= 0) return false;

        tint = sum / sourceCount;
        tint.a = 1f;
        return true;
    }

    /// <summary>인접 칸에 버프를 주는 유닛(현재: 펭귄 오라 방출).</summary>
    static bool UnitProvidesAdjacentBoardBuff(int unitNumber)
    {
        if (unitNumber == 10) return true;
        if (TraitManager.Instance == null || !TraitManager.Instance.IsPenguinAuraActive()) return false;
        return UnitHasTrait(unitNumber, "펭귄");
    }

    static string BuildUnitPlacementSignature()
    {
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || !unit.IsProperlyPlaced()) continue;
            if (!TryGetCharacterBoardCell(unit, out int row, out int col)) continue;
            sb.Append(unit.unitNumber).Append('@').Append(row).Append(',').Append(col).Append(';');
        }
        if (TraitManager.Instance != null)
        {
            sb.Append("|p=").Append(TraitManager.Instance.IsPenguinAuraActive() ? '1' : '0');
        }
        return sb.ToString();
    }

    static bool TryGetCharacterBoardCell(Character unit, out int row, out int col)
    {
        row = col = 0;
        if (unit == null) return false;
        BoardCell cell = unit.GetCurrentBoardCell();
        if (cell == null || !cell.isBoardCell) return false;
        return TryParseBoardCellName(cell.gameObject.name, out row, out col);
    }

    void AppendUnitEffectBoardOverviews(List<BoardBuffOverviewEntry> buffer)
    {
        if (TraitManager.Instance != null && TraitManager.Instance.IsPenguinAuraActive())
        {
            int auraCells = TraitManager.Instance.CountPenguinAuraCells();
            int penguinUnits = TraitManager.Instance.CountPlacedPenguinUnits();
            buffer.Add(new BoardBuffOverviewEntry
            {
                category = GameLocalization.BoardBuffCategoryKey.UnitEffect,
                title = GameLocalization.BoardBuffPenguinAuraTitle,
                location = GameLocalization.BoardBuffPenguinAuraLocationFormat(penguinUnits, auraCells),
                effect = TraitManager.Instance.GetPenguinAuraEffectDescription(),
                isAttackSpeed = true,
                traitName = "펭귄",
                sourceUnitNumber = 0
            });
        }

        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || !unit.IsProperlyPlaced()) continue;
            if (!TryGetCharacterBoardCell(unit, out int unitRow, out int unitCol)) continue;

            if (unit.unitNumber == 16)
            {
                int span = UnitEvolutionMilestones.GetUnit16AuraCellSpan(unit.evolutionLevel);
                buffer.Add(new BoardBuffOverviewEntry
                {
                    category = GameLocalization.BoardBuffCategoryKey.UnitEffect,
                    title = GameLocalization.BoardBuffUnitAuraSideFormat(UnitTraitData.GetDisplayName(16)),
                    location = GameLocalization.BoardBuffUnit16LocationFormat(unitRow + 1, unitCol + 1, span),
                    effect = BuildUnit16AuraEffectText(unit.evolutionLevel),
                    isAttackSpeed = true,
                    traitName = null,
                    sourceUnitNumber = 16
                });
            }
            else if (unit.unitNumber == 10)
            {
                buffer.Add(new BoardBuffOverviewEntry
                {
                    category = GameLocalization.BoardBuffCategoryKey.UnitEffect,
                    title = GameLocalization.BoardBuffUnitAuraVerticalFormat(UnitTraitData.GetDisplayName(10)),
                    location = GameLocalization.BoardBuffUnit10LocationFormat(unitRow + 1, unitCol + 1),
                    effect = BuildUnit10PadEffectText(unit.evolutionLevel),
                    isAttackSpeed = true,
                    traitName = null,
                    sourceUnitNumber = 10
                });
            }
            else if (unit.unitNumber == 19)
            {
                buffer.Add(new BoardBuffOverviewEntry
                {
                    category = GameLocalization.BoardBuffCategoryKey.UnitEffect,
                    title = GameLocalization.BoardBuffUnitBarrierFormat(UnitTraitData.GetDisplayName(19)),
                    location = GameLocalization.BoardBuffUnit19Location,
                    effect = BuildUnit19BarrierEffectText(unit.evolutionLevel),
                    isAttackSpeed = false,
                    traitName = null,
                    sourceUnitNumber = 19
                });
            }
        }
    }

    static string BuildUnit10PadEffectText(int evolutionLevel)
    {
        float mul = UnitEvolutionMilestones.GetUnit10PadAttackSpeedMul(evolutionLevel);
        return GameLocalization.Unit10PadEffectFormat(FormatAuraBuffPercent(mul));
    }

    static string BuildUnit16AuraEffectText(int evolutionLevel)
    {
        int span = UnitEvolutionMilestones.GetUnit16AuraCellSpan(evolutionLevel);
        var buffs = new List<string>(2);
        float dmgMul = UnitEvolutionMilestones.GetUnit16AuraDamageMul(evolutionLevel);
        float hasteMul = UnitEvolutionMilestones.GetUnit16AuraAttackSpeedMul(evolutionLevel);
        if (dmgMul > 1f)
        {
            buffs.Add(GameLocalization.Unit16AuraBuffAttack(FormatAuraBuffPercent(dmgMul)));
        }
        if (hasteMul > 1f)
        {
            buffs.Add(GameLocalization.Unit16AuraBuffAttackSpeed(FormatAuraBuffPercent(hasteMul)));
        }
        string buffText = buffs.Count > 0 ? string.Join("·", buffs) + " " : string.Empty;
        return GameLocalization.Unit16AuraEffectFormat(span, buffText);
    }

    static string FormatAuraBuffPercent(float multiplier)
    {
        return $"+{(multiplier - 1f) * 100f:0.#}%";
    }

    static string BuildUnit19BarrierEffectText(int evolutionLevel)
    {
        float tick = UnitEvolutionMilestones.GetUnit19TickInterval(evolutionLevel);
        string timing = GameLocalization.TimingOncePer(tick);
        string text = GameLocalization.Unit19BarrierEffectFormat(timing);
        return UnitCombatStats.HighlightPatternTiming(text);
    }

    static string FormatEveryOnceTiming(float seconds)
    {
        return GameLocalization.TimingOncePer(seconds);
    }

    static bool CellHasUnitRangeMarkerEffect(int row, int col, out Color color, out Sprite icon)
    {
        color = Color.clear;
        icon = null;
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || !unit.IsProperlyPlaced()) continue;
            if (!TryGetCharacterBoardCell(unit, out int unitRow, out int unitCol)) continue;

            if (unit.unitNumber == 10 && col == unitCol && (row == unitRow - 1 || row == unitRow + 1))
            {
                color = new Color(0.2f, 0.45f, 1f, 0.32f);
                icon = null;
                return true;
            }
            if (unit.unitNumber == 16 && row == unitRow && (col == unitCol - 1 || col == unitCol + 1))
            {
                color = new Color(1f, 0f, 0f, 0.3f);
                icon = null;
                return true;
            }
        }
        return false;
    }

    public Color GetTraitDisplayColor(string trait)
    {
        Color c = GetTraitCellColor(trait);
        c.a = 1f;
        return c;
    }

    Color GetTraitCellColor(string trait)
    {
        switch (trait)
        {
            // 속성
            case "자연": return new Color(0.45f, 0.85f, 0.35f, 0.32f);
            case "불":   return new Color(1f, 0.45f, 0.2f, 0.32f);
            case "얼음": return new Color(0.5f, 0.85f, 1f, 0.32f);
            case "번개": return new Color(1f, 0.95f, 0.3f, 0.32f);
            case "바람": return new Color(0.6f, 1f, 0.8f, 0.32f);
            case "빛":   return new Color(1f, 1f, 0.7f, 0.32f);
            case "어둠": return new Color(0.55f, 0.4f, 0.75f, 0.32f);
            // 종족/직업
            case "도깨비": return new Color(0.35f, 0.8f, 0.5f, 0.32f);
            case "마법사": return new Color(0.5f, 0.6f, 1f, 0.32f);
            case "기사단": return new Color(0.85f, 0.85f, 0.9f, 0.32f);
            case "재앙":   return new Color(0.8f, 0.3f, 0.35f, 0.32f);
            case "악마":   return new Color(0.7f, 0.25f, 0.6f, 0.32f);
            case "파수꾼": return new Color(0.4f, 0.7f, 0.7f, 0.32f);
            case "도적단": return new Color(0.7f, 0.6f, 0.3f, 0.32f);
            case "펭귄":   return new Color(0.6f, 0.9f, 1f, 0.32f);
            default:       return new Color(1f, 1f, 1f, 0.32f);
        }
    }

    Sprite GetTraitCellIcon(string trait)
    {
        if (string.IsNullOrEmpty(trait)) return null;
        if (traitCellIconCache.TryGetValue(trait, out Sprite cached))
        {
            return cached;
        }

        string resourceName;
        switch (trait)
        {
            case "자연": resourceName = "Ability_nature"; break;
            case "불":   resourceName = "Ability_fire"; break;
            case "얼음": resourceName = "Ability_ice"; break;
            case "번개": resourceName = "Ability_thunder"; break;
            case "바람": resourceName = "Ability_wind"; break;
            case "빛":   resourceName = "Ability_light"; break;
            case "어둠": resourceName = "Ability_dark"; break;
            case "도깨비": resourceName = "Ability_goblin"; break;
            case "마법사": resourceName = "Ability_wizard"; break;
            case "기사단": resourceName = "Ability_knight"; break;
            case "재앙":   resourceName = "Ability_end"; break;
            case "악마":   resourceName = "Ability_evil"; break;
            case "파수꾼": resourceName = "Ability_warden"; break;
            case "도적단": resourceName = "Ability_thief"; break;
            case "펭귄":   resourceName = "Ability_penguin"; break;
            default:       resourceName = null; break;
        }

        Sprite icon = string.IsNullOrEmpty(resourceName) ? null : LoadSpriteWithFallback(resourceName);
        traitCellIconCache[trait] = icon;
        return icon;
    }

    static float GetBossDamageMultiplierForBuff(BossTileBuffType buff)
    {
        switch (buff)
        {
            case BossTileBuffType.CornerPower: return 1.12f;
            case BossTileBuffType.LeftShield: return 1.10f;
            case BossTileBuffType.MiddleColumnPower: return 1.10f;
            default: return 1f;
        }
    }

    static float GetBossAttackSpeedMultiplierForBuff(BossTileBuffType buff)
    {
        switch (buff)
        {
            case BossTileBuffType.FrontlineHaste: return 1.10f;
            case BossTileBuffType.RightHaste: return 1.08f;
            default: return 1f;
        }
    }

    static float GetBossRangeBonusForBuff(BossTileBuffType buff)
    {
        switch (buff)
        {
            case BossTileBuffType.BacklinePower: return 1.5f;
            case BossTileBuffType.CornerPower: return 1.0f;
            default: return 0f;
        }
    }

    bool ProvidesDamageBuff(BossTileBuffType buff)
    {
        return GetBossDamageMultiplierForBuff(buff) > 1.001f;
    }

    bool ProvidesAttackSpeedBuff(BossTileBuffType buff)
    {
        return GetBossAttackSpeedMultiplierForBuff(buff) > 1.001f;
    }

    bool ProvidesRangeBuff(BossTileBuffType buff)
    {
        return GetBossRangeBonusForBuff(buff) > 0.001f;
    }

    bool ProvidesTraitBuff(BossTileBuffType buff)
    {
        return buff == BossTileBuffType.CenterPower;
    }

    bool IsBuffCell(BossTileBuffType buff, int row, int col)
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();
        int rows = board != null ? Mathf.Max(1, board.boardRows) : 5;
        int cols = board != null ? Mathf.Max(1, board.boardColumns) : 5;
        int centerRow = rows / 2;
        int centerCol = cols / 2;

        switch (buff)
        {
            case BossTileBuffType.CenterPower:
                return row == centerRow && col == centerCol;
            case BossTileBuffType.FrontlineHaste:
                return col == cols - 1;
            case BossTileBuffType.BacklinePower:
                return col == 0;
            case BossTileBuffType.LeftShield:
                return row == 0;
            case BossTileBuffType.RightHaste:
                return row == rows - 1;
            case BossTileBuffType.MiddleColumnPower:
                return col == centerCol;
            case BossTileBuffType.CornerPower:
                return (row == 0 || row == rows - 1) && (col == 0 || col == cols - 1);
            default:
                return false;
        }
    }

    Color GetBuffHighlightColor(BossTileBuffType buff)
    {
        switch (buff)
        {
            case BossTileBuffType.CenterPower: return new Color(1f, 0.75f, 0.2f, 0.28f);
            case BossTileBuffType.FrontlineHaste: return new Color(0.2f, 1f, 0.75f, 0.28f);
            case BossTileBuffType.BacklinePower: return new Color(0.7f, 0.55f, 1f, 0.28f);
            case BossTileBuffType.LeftShield: return new Color(0.3f, 0.65f, 1f, 0.28f);
            case BossTileBuffType.RightHaste: return new Color(0.2f, 1f, 0.45f, 0.28f);
            case BossTileBuffType.MiddleColumnPower: return new Color(1f, 0.9f, 0.35f, 0.28f);
            case BossTileBuffType.CornerPower: return new Color(1f, 0.45f, 0.45f, 0.28f);
            default: return new Color(1f, 1f, 1f, 0.28f);
        }
    }

    string GetBuffTitle(BossTileBuffType buff)
    {
        switch (buff)
        {
            case BossTileBuffType.CenterPower: return GameLocalization.BossBuffTitleCenterPower;
            case BossTileBuffType.FrontlineHaste: return GameLocalization.BossBuffTitleFrontlineHaste;
            case BossTileBuffType.BacklinePower: return GameLocalization.BossBuffTitleBacklinePower;
            case BossTileBuffType.LeftShield: return GameLocalization.BossBuffTitleLeftShield;
            case BossTileBuffType.RightHaste: return GameLocalization.BossBuffTitleRightHaste;
            case BossTileBuffType.MiddleColumnPower: return GameLocalization.BossBuffTitleMiddleColumnPower;
            case BossTileBuffType.CornerPower: return GameLocalization.BossBuffTitleCornerPower;
            default: return GameLocalization.BossBuffTitleUnknown;
        }
    }

    string GetBuffDescriptionShort(BossTileBuffType buff)
    {
        switch (buff)
        {
            case BossTileBuffType.CenterPower: return GameLocalization.BossBuffDescCenterPower;
            case BossTileBuffType.BacklinePower: return GameLocalization.BossBuffDescBacklinePower;
            case BossTileBuffType.CornerPower: return GameLocalization.BossBuffDescCornerPower;
            case BossTileBuffType.FrontlineHaste: return GameLocalization.BossBuffDescFrontlineHaste;
            case BossTileBuffType.LeftShield: return GameLocalization.BossBuffDescLeftShield;
            case BossTileBuffType.RightHaste: return GameLocalization.BossBuffDescRightHaste;
            case BossTileBuffType.MiddleColumnPower: return GameLocalization.BossBuffDescMiddleColumnPower;
            default: return string.Empty;
        }
    }

    string GetBuffEffectText(BossTileBuffType buff)
    {
        switch (buff)
        {
            case BossTileBuffType.CenterPower: return GameLocalization.BossBuffEffectCenterPower;
            case BossTileBuffType.BacklinePower: return GameLocalization.BossBuffEffectBacklinePower;
            case BossTileBuffType.CornerPower: return GameLocalization.BossBuffEffectCornerPower;
            case BossTileBuffType.FrontlineHaste: return GameLocalization.BossBuffEffectFrontlineHaste;
            case BossTileBuffType.LeftShield: return GameLocalization.BossBuffEffectLeftShield;
            case BossTileBuffType.RightHaste: return GameLocalization.BossBuffEffectRightHaste;
            case BossTileBuffType.MiddleColumnPower: return GameLocalization.BossBuffEffectMiddleColumnPower;
            default: return string.Empty;
        }
    }

    Sprite bossBuffRangeIcon;
    Sprite bossBuffTraitIcon;

    Sprite GetBuffIconSprite(BossTileBuffType buff)
    {
        if (ProvidesTraitBuff(buff))
        {
            if (bossBuffTraitIcon == null)
            {
                bossBuffTraitIcon = LoadSpriteWithFallback("Ability_icons1_15");
            }
            return bossBuffTraitIcon;
        }

        if (ProvidesRangeBuff(buff))
        {
            if (bossBuffRangeIcon == null)
            {
                bossBuffRangeIcon = LoadSpriteWithFallback("Ability_warden");
            }
            return bossBuffRangeIcon;
        }

        if (ProvidesAttackSpeedBuff(buff))
        {
            if (bossBuffAttackSpeedIcon == null)
            {
                bossBuffAttackSpeedIcon = LoadSpriteWithFallback("1-Lightning");
            }
            return bossBuffAttackSpeedIcon;
        }

        if (bossBuffDamageIcon == null)
        {
            bossBuffDamageIcon = LoadSpriteWithFallback("attack_effect_fire");
        }
        return bossBuffDamageIcon;
    }

    static Sprite LoadSpriteWithFallback(string resourceName)
    {
        Sprite single = Resources.Load<Sprite>(resourceName);
        if (single != null) return single;
        Sprite[] all = Resources.LoadAll<Sprite>(resourceName);
        if (all != null && all.Length > 0) return all[0];
        return null;
    }

    static bool TryParseBoardCellName(string cellName, out int row, out int col)
    {
        row = col = 0;
        if (string.IsNullOrEmpty(cellName) || !cellName.StartsWith("BoardCell_")) return false;
        string[] parts = cellName.Split('_');
        if (parts.Length < 3) return false;
        return int.TryParse(parts[1], out row) && int.TryParse(parts[2], out col);
    }

    bool IsBossZombie(Zombie zombie)
    {
        if (zombie == null) return false;
        if (zombie.zombieUID == "boss1" || zombie.zombieUID == "ooze_boss" || zombie.zombieUID == "necromancer_boss")
        {
            return true;
        }
        if (zombie.zombieData == null) return false;
        return zombie.zombieData.zombieName == "Boss1" ||
               zombie.zombieData.zombieName == "OozeBoss" ||
               zombie.zombieData.zombieName == "NecromancerBoss";
    }

    bool IsNecromancerBossAlive()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie zombie = zombies[i];
            if (zombie != null && !zombie.IsDead && zombie.zombieUID == "necromancer_boss")
            {
                return true;
            }
        }

        return false;
    }

    Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

