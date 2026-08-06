using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 캐릭터 기본 클래스
/// </summary>
public class Character : MonoBehaviour
{
    [Header("Character Settings")]
    public Color characterColor = Color.blue;
    public int unitNumber = 0; // 유닛 번호 (1번 유닛 특수 처리용)
    public int evolutionLevel = 1; // 진화도 (1~9)
    [HideInInspector] public bool isShopPreviewInstance;
    
    [Header("Unit 4 Settings")]
    public float goldSpawnCooldown = 4f;
    private float nextGoldSpawnTime = 0f;
    private bool hasGoldIcon = false;
    
    [Header("Combat Settings")]
    public GameObject projectilePrefab; // 총알 프리팹 (없으면 자동 생성)
    public float attackCooldown = 2f; // UnitCombatStats에서 로드
    public float attackRange = 10f;   // UnitCombatStats에서 로드 (0=전역)
    public int maxHealth = 90;
    public int health = 90;
    public float projectileSpeed = 10f;

    private readonly List<Zombie> _zombieSearchBuffer = new List<Zombie>(512);

    private static RuntimeAnimatorController unit1ProjectileController;
    private static RuntimeAnimatorController unit2ProjectileController;
    private static Sprite unit1ProjectileFallbackSprite;
    private static Sprite unit2ProjectileFallbackSprite;
    private static Sprite unitGradeStarSprite;
    private static Sprite unitFloorShadowSprite;
    private static bool projectileVisualsLoaded = false;
    private static Sprite unit1Sprite;
    private const string Unit1SpumResourceName = "Units/unit_001";
    private const string Unit2SpumResourceName = "Units/unit_002";
    private const string Unit5SpumResourceName = "Units/unit_005";
    private const string Unit5SkillReadyResourceName = "skill_ready";
    private const float Unit5SkillReadyFps = 12f;
    private const float Unit5SkillReadyAlpha = 0.65f;
    private static readonly Color Unit5SkillReadyColor = new Color(1f, 1f, 1f, Unit5SkillReadyAlpha);
    private const string Unit6SpumResourceName = "Units/unit_006";
    private const int Unit1SpumAttackIndex = 0;
    private UnitSpriteAnimator unitSpriteAnimator;
    private Sprite[] unit9AttackFrames;
    private bool isUnit9Attacking = false;
    private Coroutine unit9AttackRoutine;
    private const string Unit9IdleResourceName = "duggonku_idle-Sheet";
    private const string Unit9AttackResourceName = "duggonku-Sheet";
    private const float Unit9AttackFps = 6f;
    private GameObject unit1SpumVisual;
    private SPUM_Prefabs unit1SpumPrefab;
    private Animator unit1SpumAnimator;
    private Coroutine unit1AttackRoutine;
    private float unit1AttackDuration = 0.6f;
    private bool unit1UsesSpum = false;
    private PlayerState unit1SpumState = PlayerState.IDLE;
    private GameObject unit5SpumVisual;
    private SPUM_Prefabs unit5SpumPrefab;
    private bool unit5UsesSpum = false;
    private Animator unit5SpumAnimator;
    private Coroutine unit5AttackRoutine;
    private float unit5AttackDuration = 0.6f;
    private PlayerState unit5SpumState = PlayerState.IDLE;
    private Zombie unit5PendingStrikeTarget;
    private GameObject unit2SpumVisual;
    private SPUM_Prefabs unit2SpumPrefab;
    private bool unit2UsesSpum = false;
    private Animator unit2SpumAnimator;
    private Coroutine unit2AttackRoutine;
    private float unit2AttackDuration = 0.6f;
    private PlayerState unit2SpumState = PlayerState.IDLE;
    private bool usesUnit2Prefab = false;
    private GameObject unit2PrefabVisual;
    private Animator unit2PrefabAnimator;
    private AnimationClip unit2PrefabAttackClip;
    private bool usesUnit7Prefab = false;
    private GameObject unit7PrefabVisual;
    private SPUM_Prefabs unit7SpumPrefab;
    private Animator unit7SpumAnimator;
    private float unit7SpumAttackDuration = 0.6f;
    private PlayerState unit7SpumState = PlayerState.IDLE;
    private bool usesUnit15Prefab = false;
    private GameObject unit15PrefabVisual;
    private SPUM_Prefabs unit15SpumPrefab;
    private Animator unit15SpumAnimator;
    private float unit15SpumAttackDuration = 0.6f;
    private PlayerState unit15SpumState = PlayerState.IDLE;
    private bool usesUnit19Prefab = false;
    private GameObject unit19PrefabVisual;
    private SPUM_Prefabs unit19SpumPrefab;
    private Animator unit19SpumAnimator;
    private float unit19SpumAttackDuration = 0.6f;
    private PlayerState unit19SpumState = PlayerState.IDLE;
    private bool usesUnit13Prefab = false;
    private GameObject unit13PrefabVisual;
    private SPUM_Prefabs unit13SpumPrefab;
    private Animator unit13SpumAnimator;
    private float unit13SpumAttackDuration = 0.6f;
    private PlayerState unit13SpumState = PlayerState.IDLE;
    private bool usesUnit14Prefab = false;
    private GameObject unit14PrefabVisual;
    private SPUM_Prefabs unit14SpumPrefab;
    private Animator unit14SpumAnimator;
    private float unit14SpumAttackDuration = 0.6f;
    private PlayerState unit14SpumState = PlayerState.IDLE;
    private bool usesUnit23Prefab = false;
    private GameObject unit23PrefabVisual;
    private SPUM_Prefabs unit23SpumPrefab;
    private Animator unit23SpumAnimator;
    private float unit23SpumAttackDuration = 0.6f;
    private PlayerState unit23SpumState = PlayerState.IDLE;
    private bool usesUnit25Prefab = false;
    private GameObject unit25PrefabVisual;
    private SPUM_Prefabs unit25SpumPrefab;
    private Animator unit25SpumAnimator;
    private float unit25SpumAttackDuration = 0.6f;
    private PlayerState unit25SpumState = PlayerState.IDLE;
    private bool usesUnit28Prefab = false;
    private GameObject unit28PrefabVisual;
    private SPUM_Prefabs unit28SpumPrefab;
    private Animator unit28SpumAnimator;
    private float unit28SpumAttackDuration = 0.6f;
    private PlayerState unit28SpumState = PlayerState.IDLE;
    private bool usesUnit4Prefab = false;
    private GameObject unit4PrefabVisual;
    private SPUM_Prefabs unit4SpumPrefab;
    private Animator unit4SpumAnimator;
    private float unit4SpumAttackDuration = 0.6f;
    private PlayerState unit4SpumState = PlayerState.IDLE;
    private bool usesUnit8Prefab = false;
    private GameObject unit8PrefabVisual;
    private SPUM_Prefabs unit8SpumPrefab;
    private Animator unit8SpumAnimator;
    private float unit8SpumAttackDuration = 0.6f;
    private PlayerState unit8SpumState = PlayerState.IDLE;
    private bool usesUnit10Prefab = false;
    private GameObject unit10PrefabVisual;
    private SPUM_Prefabs unit10SpumPrefab;
    private Animator unit10SpumAnimator;
    private float unit10SpumAttackDuration = 0.6f;
    private PlayerState unit10SpumState = PlayerState.IDLE;
    private bool usesUnit12Prefab = false;
    private GameObject unit12PrefabVisual;
    private SPUM_Prefabs unit12SpumPrefab;
    private Animator unit12SpumAnimator;
    private float unit12SpumAttackDuration = 0.6f;
    private PlayerState unit12SpumState = PlayerState.IDLE;
    private bool usesUnit21Prefab = false;
    private GameObject unit21PrefabVisual;
    private SPUM_Prefabs unit21SpumPrefab;
    private Animator unit21SpumAnimator;
    private float unit21SpumAttackDuration = 0.6f;
    private PlayerState unit21SpumState = PlayerState.IDLE;
    private bool usesUnit11Prefab = false;
    private GameObject unit11PrefabVisual;
    private SPUM_Prefabs unit11SpumPrefab;
    private Animator unit11SpumAnimator;
    private float unit11SpumAttackDuration = 0.6f;
    private PlayerState unit11SpumState = PlayerState.IDLE;
    private bool usesUnit20Prefab = false;
    private GameObject unit20PrefabVisual;
    private SPUM_Prefabs unit20SpumPrefab;
    private Animator unit20SpumAnimator;
    private float unit20SpumAttackDuration = 0.6f;
    private PlayerState unit20SpumState = PlayerState.IDLE;
    private bool usesUnit22Prefab = false;
    private GameObject unit22PrefabVisual;
    private SPUM_Prefabs unit22SpumPrefab;
    private Animator unit22SpumAnimator;
    private float unit22SpumAttackDuration = 0.6f;
    private PlayerState unit22SpumState = PlayerState.IDLE;
    private GameObject unit6SpumVisual;
    private SPUM_Prefabs unit6SpumPrefab;
    private bool unit6UsesSpum = false;
    private Animator unit6SpumAnimator;
    private Coroutine unit6AttackRoutine;
    private float unit6AttackDuration = 0.6f;
    private PlayerState unit6SpumState = PlayerState.IDLE;
    private bool unit3UsesCustom = false;
    private UnitSpriteAnimator unit3SpriteAnimator;
    private string unit3IdleResourceName;
    private string unit3AttackResourceName;
    private string unit3CurrentResourceName;
    private Coroutine unit3AttackRoutine;
    private const float Unit3Fps = 6f;
    private float unit3AttackDuration = 0.2f;
    private bool usesUnit3Prefab = false;
    private GameObject unit3PrefabVisual;
    private SPUM_Prefabs unit3SpumPrefab;
    private Animator unit3SpumAnimator;
    private float unit3SpumAttackDuration = 0.6f;
    private PlayerState unit3SpumState = PlayerState.IDLE;
    private bool usesUnit18Prefab = false;
    private GameObject unit18PrefabVisual;
    private SPUM_Prefabs unit18SpumPrefab;
    private Animator unit18SpumAnimator;
    private float unit18SpumAttackDuration = 0.6f;
    private PlayerState unit18SpumState = PlayerState.IDLE;
    private bool usesUnit24Prefab = false;
    private GameObject unit24PrefabVisual;
    private SPUM_Prefabs unit24SpumPrefab;
    private Animator unit24SpumAnimator;
    private float unit24SpumAttackDuration = 0.6f;
    private PlayerState unit24SpumState = PlayerState.IDLE;

    /// <summary>네크로맨서 보스 패턴2 — 헥스 종료 시각</summary>
    private float necromancerHexEndTime;
    private Coroutine necromancerHexVisualRoutine;
    private Color necromancerHexStoredColor;
    private bool necromancerHexStored;
    
    private bool isDragging = false;
    BoardCell dragOriginCell;
    Vector3 dragPointerWorldPos;
    static Character activeDragCharacter;
    static int dragPickFrame = -1;
    static Character dragPickCache;

    public bool IsDragging => isDragging;
    public static bool IsDragInProgress => activeDragCharacter != null;
    private BoardCell currentCell;
    private SpriteRenderer spriteRenderer;
    private bool usesOverrideSprite = false;
    private GameObject overridePrefabVisual;
    private bool usesUnit9Prefab = false;
    private SpriteRenderer unit9AttackRenderer;
    private SPUM_Prefabs unit9SpumPrefab;
    private Animator unit9SpumAnimator;
    private float unit9AttackDuration = 0.6f;
    private PlayerState unit9SpumState = PlayerState.IDLE;
    private bool usesUnit17Prefab = false;
    private GameObject unit17PrefabVisual;
    private SPUM_Prefabs unit17SpumPrefab;
    private Animator unit17SpumAnimator;
    private float unit17AttackDuration = 0.6f;
    private PlayerState unit17SpumState = PlayerState.IDLE;
    private bool usesUnit16Prefab = false;
    private GameObject unit16PrefabVisual;
    private SPUM_Prefabs unit16SpumPrefab;
    private Animator unit16SpumAnimator;
    private float unit16AttackDuration = 0.6f;
    private PlayerState unit16SpumState = PlayerState.IDLE;
    private bool usesUnit27Prefab = false;
    private GameObject unit27PrefabVisual;
    private SPUM_Prefabs unit27SpumPrefab;
    private Animator unit27SpumAnimator;
    private float unit27AttackDuration = 0.6f;
    private PlayerState unit27SpumState = PlayerState.IDLE;
    private int unit26ActiveProjectileCount;
    private SpriteRenderer[] unit26TintRenderers;
    private Color[] unit26TintOriginalColors;
    private SpriteRenderer unit26ShadowRenderer;
    private GameObject unit16LeftMarker;
    private GameObject unit16RightMarker;
    private GameObject unit10UpPad;
    private GameObject unit10DownPad;
    private GameObject unit19BarrierPad;
    private GameObject unit22RootPad;
    private float lastAttackTime = 0f;
    private Transform evolutionStarsRoot;
    private Vector3 baseScale;
    private const float LightningFrameTime = 0.06f;
    private const int LightningHitFrameIndex = 3;
    private const float Unit17AttackFrameTime = 0.06f;
    private const int Unit17HitFrameIndex = 4;
    private const float Unit18ArrowFrameTime = 0.06f;
    private const float Unit18StunDuration = 3f;
    private const float Unit22AttackCooldown = 4f;
    private const float Unit22RootPadDuration = 2f;
    private const float Unit22RootStunDuration = 2f;
    private const float Unit22RootPadAlpha = 0.35f;
    private const float Unit8AttackInterval = 5f;
    private const float Unit26BoardPlacementCellUp = 1.5f;
    /// <summary>26번은 본체 스케일 1/3 — 진화 별은 칸 크기 기준(다른 유닛 대비 약 4배 가독).</summary>
    private const float Unit26EvolutionStarCellSizeFactor = 2.08f;
    private const float Unit26EvolutionStarHeightCellFactor = 1.68f;
    private const float Unit26EvolutionStarSpacingFactor = 0.44f;
    /// <summary>SPUM 유닛 보드 배치·Shadow 자식과 동일한 월드 크기 환산용.</summary>
    private const float SpumUnitBoardVisualScaleMul = 1.5f;
    private const float SpumShadowRootScaleMul = 1.027f;
    private const float SpumShadowChildScaleX = 0.05f;
    private const float SpumShadowChildScaleY = 0.015f;
    private const float SpumShadowLocalYOffset = 0.018f;
    private const float Unit26FloorShadowSizeMul = 0.2f;
    private static readonly Color Unit26FloorShadowColor = new Color(0f, 0f, 0f, 0.56f);
    
    void OnDestroy()
    {
        if (activeDragCharacter == this)
        {
            activeDragCharacter = null;
        }

        if (isDragging)
        {
            UnitDragPlacementPreview.Hide();
        }

        if (unit26ActiveProjectileCount > 0)
        {
            unit26ActiveProjectileCount = 0;
            RestoreUnit26ProjectileOwnerVisuals();
        }
    }

    void Start()
    {
        UnitFieldTooltip.EnsureExists();
        UnitAttackRangePreview.EnsureExists();
        UnitDamageMeterHud.EnsureExists();

        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        if (transform.localScale.sqrMagnitude < 0.0001f && boardManager != null)
        {
            transform.localScale = Vector3.one * Mathf.Max(0.01f, boardManager.cellSize);
        }

        baseScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        usesOverrideSprite = unitNumber != 2 && unitNumber != 3 && unitNumber != 7 && unitNumber != 9 && unitNumber != 15 && unitNumber != 16 && unitNumber != 17 && unitNumber != 18 && unitNumber != 19 && unitNumber != 22 && unitNumber != 24 && unitNumber != 27 && TryApplyOverrideUnitSprite();

        if (unitNumber == 9)
        {
            usesUnit9Prefab = TryApplyUnit9PrefabVisual();
            if (!usesUnit9Prefab)
            {
                SetupUnit9Visual();
            }
            unit9AttackFrames = LoadSpriteFrames(Unit9AttackResourceName);
            if (usesUnit9Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 17)
        {
            usesUnit17Prefab = TryApplyUnit17PrefabVisual();
            unit9AttackFrames = LoadSpriteFrames(Unit9AttackResourceName);
            if (usesUnit17Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 16)
        {
            usesUnit16Prefab = TryApplyUnit16PrefabVisual();
            unit9AttackFrames = LoadSpriteFrames(Unit9AttackResourceName);
            transform.localScale = baseScale * 1.5f;
        }
        if (unitNumber == 27)
        {
            usesUnit27Prefab = TryApplyUnit27PrefabVisual();
            unit9AttackFrames = LoadSpriteFrames(Unit9AttackResourceName);
            transform.localScale = baseScale * 1.5f;
        }

        unit1UsesSpum = unitNumber == 1 && !usesOverrideSprite && SetupUnit1SpumVisual();
        if (unitNumber == 2)
        {
            usesUnit2Prefab = TryApplyUnit2PrefabVisual();
            transform.localScale = baseScale;
            if (usesUnit2Prefab && unit2PrefabVisual != null)
            {
                unit2PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }
        if (unitNumber == 7)
        {
            usesUnit7Prefab = TryApplyUnit7PrefabVisual();
            transform.localScale = baseScale * 1.5f;
        }
        if (unitNumber == 15)
        {
            usesUnit15Prefab = TryApplyUnit15PrefabVisual();
            if (usesUnit15Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 19)
        {
            usesUnit19Prefab = TryApplyUnit19PrefabVisual();
            if (usesUnit19Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 13)
        {
            usesUnit13Prefab = TryApplyUnit13PrefabVisual();
            if (usesUnit13Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 14)
        {
            usesUnit14Prefab = TryApplyUnit14PrefabVisual();
            if (usesUnit14Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 23)
        {
            usesUnit23Prefab = TryApplyUnit23PrefabVisual();
            if (usesUnit23Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 25)
        {
            usesUnit25Prefab = TryApplyUnit25PrefabVisual();
            if (usesUnit25Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 28)
        {
            usesUnit28Prefab = TryApplyUnit28PrefabVisual();
            if (usesUnit28Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 8)
        {
            usesUnit8Prefab = TryApplyUnit8PrefabVisual();
            if (usesUnit8Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 4)
        {
            usesUnit4Prefab = TryApplyUnit4PrefabVisual();
            if (usesUnit4Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 10)
        {
            usesUnit10Prefab = TryApplyUnit10PrefabVisual();
            if (usesUnit10Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 12)
        {
            usesUnit12Prefab = TryApplyUnit12PrefabVisual();
            if (usesUnit12Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 21)
        {
            usesUnit21Prefab = TryApplyUnit21PrefabVisual();
            if (usesUnit21Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 11)
        {
            usesUnit11Prefab = TryApplyUnit11PrefabVisual();
            if (usesUnit11Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 20)
        {
            usesUnit20Prefab = TryApplyUnit20PrefabVisual();
            if (usesUnit20Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 22)
        {
            usesUnit22Prefab = TryApplyUnit22PrefabVisual();
            if (usesUnit22Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        unit2UsesSpum = unitNumber == 2 && (usesUnit2Prefab || SetupUnit2SpumVisual());
        if (unitNumber == 3)
        {
            usesUnit3Prefab = TryApplyUnit3PrefabVisual();
            if (usesUnit3Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 18)
        {
            usesUnit18Prefab = TryApplyUnit18PrefabVisual();
            if (usesUnit18Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        if (unitNumber == 24)
        {
            usesUnit24Prefab = TryApplyUnit24PrefabVisual();
            if (usesUnit24Prefab)
            {
                transform.localScale = baseScale * 1.5f;
            }
        }
        unit3UsesCustom = unitNumber == 3 && !usesUnit3Prefab && SetupUnit3Sprites();
        unit5UsesSpum = unitNumber == 5 && !usesOverrideSprite && SetupUnit5SpumVisual();
        unit6UsesSpum = unitNumber == 6 && SetupUnit6SpumVisual();
        
        bool hasStaticUnitArt = !usesOverrideSprite && !unit1UsesSpum && !unit2UsesSpum && !unit3UsesCustom && !unit5UsesSpum && !unit6UsesSpum && TryApplyStaticUnitSprite();
        
        // 스프라이트가 없으면 생성
        if (!unit1UsesSpum && !unit2UsesSpum && !unit3UsesCustom && !unit5UsesSpum && !unit6UsesSpum && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateSquareSprite(characterColor);
        }

        // 1번/9번 유닛은 지정 아트 사용
        if (usesOverrideSprite)
        {
            spriteRenderer.color = Color.white;
            if (unitNumber == 1 || unitNumber == 5 || unitNumber == 16 ||
                unitNumber == 17 || unitNumber == 27)
            {
                transform.localScale = transform.localScale * 1.5f;
            }
            spriteRenderer.flipX = true;
        }
        else if (unitNumber == 1)
        {
            if (unit1UsesSpum)
            {
                spriteRenderer.color = Color.white;
                // 좌우 반전 (오른쪽 바라보게)
                // 1번 유닛 아트 크기를 1.5배 확대
                transform.localScale = transform.localScale * 1.5f;
            }
            else
            {
                Sprite unit1StaticSprite = Resources.Load<Sprite>("unit_1");
                if (unit1StaticSprite != null)
                {
                    if (unitSpriteAnimator != null)
                    {
                        unitSpriteAnimator.enabled = false;
                    }
                    spriteRenderer.sprite = unit1StaticSprite;
                }
                else
                {
                    // 스프라이트 시트 기반 애니메이션 적용 (fallback)
                    if (unitSpriteAnimator == null)
                    {
                        unitSpriteAnimator = gameObject.AddComponent<UnitSpriteAnimator>();
                    }
                    unitSpriteAnimator.Initialize(spriteRenderer, "Sprite-0003-Sheet-Sheet", 6f);
                }

                spriteRenderer.color = Color.white;
                spriteRenderer.flipX = true;
                // 1번 유닛 아트 크기를 1.5배 확대
                transform.localScale = transform.localScale * 1.5f;
            }
        }
        else if (unitNumber == 2 && unit2UsesSpum)
        {
            spriteRenderer.color = Color.white;
            // 1번 유닛과 동일 크기
            transform.localScale = transform.localScale * 1.5f;
        }
        else if (unitNumber == 3 && unit3UsesCustom)
        {
            spriteRenderer.color = Color.white;
        }
        else if (unitNumber == 5 && unit5UsesSpum)
        {
            spriteRenderer.color = Color.white;
            // 1번 유닛과 동일 크기
            transform.localScale = transform.localScale * 1.5f;
        }
        else if (unitNumber == 6 && unit6UsesSpum)
        {
            spriteRenderer.color = Color.white;
            // 1번 유닛과 동일 크기
            transform.localScale = transform.localScale * 1.5f;
        }
        else if (unitNumber == 9 && !usesUnit9Prefab)
        {
            if (unitSpriteAnimator == null)
            {
                unitSpriteAnimator = gameObject.AddComponent<UnitSpriteAnimator>();
            }
            unitSpriteAnimator.Initialize(spriteRenderer, Unit9IdleResourceName, 6f);
            unitSpriteAnimator.EnableBottomAnchor(true);
            unit9AttackFrames = LoadSpriteFrames(Unit9AttackResourceName);
            spriteRenderer.color = Color.white;
            spriteRenderer.flipX = true;
            transform.localScale = transform.localScale * 0.5f;
        }
        else if (hasStaticUnitArt)
        {
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.color = characterColor;
        }
        spriteRenderer.sortingOrder = 1;
        ApplyUnitScale();

        if (unitNumber == 26)
        {
            SetupUnit26FloorShadow();
        }
        
        // 등급·역할별 기본 전투 스탯 적용
        ApplyBaseCombatStats();
        // lastAttackTime=0이면 Time.time < 쿨다운으로 "게임 시작~쿨다운" 동안 한 번도 공격 불가. 배치 직후 첫 사격을 허용.
        lastAttackTime = GetCombatActionTime() - attackCooldown;

        UpdateEvolutionStars();
    }

    bool TryApplyStaticUnitSprite()
    {
        if (unitNumber == 8 && usesUnit8Prefab)
        {
            return false;
        }
        if (unitNumber == 4 && usesUnit4Prefab) return false;
        if (unitNumber == 10 && usesUnit10Prefab) return false;
        if (unitNumber == 12 && usesUnit12Prefab) return false;
        if (unitNumber == 21 && usesUnit21Prefab) return false;
        if (unitNumber != 2 && unitNumber != 3 && unitNumber != 4 && unitNumber != 5 &&
            unitNumber != 6 && unitNumber != 7 && unitNumber != 8 &&
            unitNumber != 10 && unitNumber != 12 && unitNumber != 14 && unitNumber != 16 &&
            unitNumber != 21 && unitNumber != 25 && unitNumber != 26 && unitNumber != 27)
        {
            return false;
        }

        string staticResourceName = unitNumber == 26 ? "unit_026" : $"unit_{unitNumber}";
        Sprite sprite = Resources.Load<Sprite>(staticResourceName);
        if (sprite == null)
        {
            Sprite[] ordered = Resources.LoadAll<Sprite>(staticResourceName);
            if (ordered != null && ordered.Length > 0)
            {
                System.Array.Sort(ordered, (a, b) => string.CompareOrdinal(a.name, b.name));
                sprite = ordered[0];
            }
        }
        if (sprite == null)
        {
            return false;
        }

        spriteRenderer.sprite = sprite;
        return true;
    }

    void ApplyUnitScale()
    {
        if (unit1UsesSpum || unit2UsesSpum || unit5UsesSpum || unit6UsesSpum)
        {
            return;
        }
        if (unitNumber == 16)
        {
            return;
        }
        if (unitNumber == 27)
        {
            return;
        }
        if (unitNumber == 3)
        {
            return;
        }
        if (unitNumber == 18)
        {
            return;
        }
        if (unitNumber == 24)
        {
            return;
        }
        if (unitNumber == 2)
        {
            return;
        }
        if (unitNumber == 7)
        {
            return;
        }
        if (unitNumber == 19)
        {
            return;
        }
        if (unitNumber == 22)
        {
            return;
        }
        if (unitNumber == 15)
        {
            return;
        }
        if (unitNumber == 14 && usesUnit14Prefab)
        {
            return;
        }
        if (unitNumber == 25 && usesUnit25Prefab)
        {
            return;
        }
        if (unitNumber == 8 && usesUnit8Prefab)
        {
            return;
        }
        if (unitNumber == 4 && usesUnit4Prefab)
        {
            return;
        }
        if (unitNumber == 10 && usesUnit10Prefab)
        {
            return;
        }
        if (unitNumber == 12 && usesUnit12Prefab)
        {
            return;
        }
        if (unitNumber == 21 && usesUnit21Prefab)
        {
            return;
        }
        if (usesOverrideSprite && unitNumber == 5)
        {
            return;
        }

        if (unitNumber == 2 || unitNumber == 3 || unitNumber == 4 ||
            unitNumber == 5 || unitNumber == 6 || unitNumber == 7 ||
            unitNumber == 8 || unitNumber == 12 || unitNumber == 14 ||
            unitNumber == 16 || unitNumber == 25 || unitNumber == 26 || unitNumber == 27)
        {
            transform.localScale = baseScale * (1f / 3f);
        }
    }

    Vector3 GetProjectileScale()
    {
        if (unitNumber == 2 || unitNumber == 7 || unitNumber == 8)
        {
            return baseScale;
        }
        if (unitNumber == 4 && usesUnit4Prefab) return baseScale;
        if (unitNumber == 10 && usesUnit10Prefab) return baseScale;
        if (unitNumber == 12 && usesUnit12Prefab) return baseScale;
        if (unitNumber == 21 && usesUnit21Prefab) return baseScale;
        return transform.localScale;
    }

    Vector3 GetScaledProjectileSize(Vector3 baseProjectileScale)
    {
        if (unitNumber >= 1 && unitNumber <= 6)
        {
            return baseProjectileScale * 0.5f;
        }
        return baseProjectileScale;
    }
    
    /// <summary>UnitCombatStats 테이블에서 공격력·체력·공속·사거리를 로드합니다.</summary>
    void ApplyBaseCombatStats()
    {
        if (unitNumber < 1 || unitNumber > 28) return;

        attackCooldown = UnitCombatStats.GetAttackIntervalForUnit(unitNumber);
        attackCooldown *= CharacterUpgradeData.GetMobilityIntervalMultiplier(unitNumber);
        attackRange = UnitCombatStats.GetAttackRangeForUnit(unitNumber);
        if (attackRange > 0f)
        {
            attackRange += CharacterUpgradeData.GetRangeBonus(unitNumber);
        }
        maxHealth = UnitCombatStats.CalculateCoreHealth(unitNumber, evolutionLevel);
        health = maxHealth;
        if (unitNumber == 4)
        {
            goldSpawnCooldown = UnitEvolutionMilestones.GetUnit4GoldCooldown(evolutionLevel);
            goldSpawnCooldown *= CharacterUpgradeData.GetEconomyCooldownMultiplier(unitNumber);
            goldSpawnCooldown = Mathf.Max(0.5f, goldSpawnCooldown - UnitArchiveAwakening.GetGoldCooldownReduction(unitNumber));
        }
    }

    float GetEffectiveAttackRange()
    {
        if (attackRange <= 0f) return float.MaxValue;
        float range = attackRange;
        if (currentCell != null && currentCell.isBoardCell && GameManager.Instance != null
            && TryParseBoardCellName(currentCell.gameObject.name, out int row, out int col))
        {
            range += GameManager.Instance.GetBoardRangeBonus(row, col);
        }
        return range;
    }

    bool IsZombieInAttackRange(Zombie zombie)
    {
        if (zombie == null) return false;
        return Vector2.Distance(transform.position, zombie.transform.position) <= GetEffectiveAttackRange();
    }

    void FilterZombiesByAttackRange()
    {
        for (int i = _zombieSearchBuffer.Count - 1; i >= 0; i--)
        {
            if (!IsZombieInAttackRange(_zombieSearchBuffer[i]))
            {
                _zombieSearchBuffer.RemoveAt(i);
            }
        }
    }

    int GetDamageWithUpgrade(int baseDamage)
    {
        int balancedBaseDamage = UnitCombatStats.CalculateCoreDamage(unitNumber, evolutionLevel);
        if (unitNumber < 1 || unitNumber > 28)
        {
            int raw = Mathf.Max(baseDamage, balancedBaseDamage);
            return Mathf.Max(1, Mathf.RoundToInt(raw * GetBoardCellDamageMultiplier()));
        }
        int bonus = CharacterUpgradeData.GetAttackBonus(unitNumber) + UnitArchiveAwakening.GetAttackBonus(unitNumber);
        int traitBonus = TraitManager.Instance != null ? TraitManager.Instance.GetDamageBonusForUnit(unitNumber) : 0;
        float traitMul = TraitManager.Instance != null ? TraitManager.Instance.GetDamageMultiplierForUnit(unitNumber) : 1f;
        int sum = balancedBaseDamage + bonus + traitBonus;
        return Mathf.Max(1, Mathf.RoundToInt(sum * GetBoardCellDamageMultiplier() * traitMul * GetUnit16AuraDamageMultiplier()));
    }

    float GetUnit16AuraDamageMultiplier()
    {
        if (unitNumber == 16 || currentCell == null || !currentCell.isBoardCell) return 1f;
        if (!TryParseBoardCellName(currentCell.gameObject.name, out int myRow, out int myCol)) return 1f;

        float mul = 1f;
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character src = units[i];
            if (src == null || src.unitNumber != 16 || !src.IsProperlyPlaced()) continue;
            BoardCell srcCell = src.GetCurrentBoardCell();
            if (srcCell == null || !TryParseBoardCellName(srcCell.gameObject.name, out int srcRow, out int srcCol)) continue;
            int span = UnitEvolutionMilestones.GetUnit16AuraCellSpan(src.evolutionLevel);
            if (myRow != srcRow) continue;
            int colDist = Mathf.Abs(myCol - srcCol);
            if (colDist >= 1 && colDist <= span)
            {
                mul *= UnitEvolutionMilestones.GetUnit16AuraDamageMul(src.evolutionLevel);
            }
        }
        return mul;
    }

    float GetUnit16AuraAttackSpeedMultiplier()
    {
        if (unitNumber == 16 || currentCell == null || !currentCell.isBoardCell) return 1f;
        if (!TryParseBoardCellName(currentCell.gameObject.name, out int myRow, out int myCol)) return 1f;

        float mul = 1f;
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character src = units[i];
            if (src == null || src.unitNumber != 16 || !src.IsProperlyPlaced()) continue;
            BoardCell srcCell = src.GetCurrentBoardCell();
            if (srcCell == null || !TryParseBoardCellName(srcCell.gameObject.name, out int srcRow, out int srcCol)) continue;
            int span = UnitEvolutionMilestones.GetUnit16AuraCellSpan(src.evolutionLevel);
            if (myRow != srcRow) continue;
            int colDist = Mathf.Abs(myCol - srcCol);
            if (colDist >= 1 && colDist <= span)
            {
                mul *= UnitEvolutionMilestones.GetUnit16AuraAttackSpeedMul(src.evolutionLevel);
            }
        }
        return mul;
    }

    /// <summary>장판·초당 피해 등 — 공격력 비율 적용(최소 1).</summary>
    int GetPeriodicDamage(float fraction = 1f)
    {
        return Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * Mathf.Clamp(fraction, 0.05f, 2f)));
    }
    
    /// <summary>
    /// 두 색상이 비슷한지 확인합니다 (약간의 오차 허용)
    /// </summary>
    bool IsColorSimilar(Color a, Color b)
    {
        return Vector4.Distance(a, b) < 0.1f;
    }
    
    /// <summary>
    /// 사각형 스프라이트를 생성합니다
    /// </summary>
    Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    Sprite[] LoadSpriteFrames(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return System.Array.Empty<Sprite>();

        Sprite[] loaded = Resources.LoadAll<Sprite>(resourceName);
        if (loaded == null || loaded.Length == 0) return System.Array.Empty<Sprite>();

        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
        return loaded;
    }

    bool TryApplyOverrideUnitSprite()
    {
        if (unitNumber != 1 && unitNumber != 5 &&
            unitNumber != 16 && unitNumber != 17 && unitNumber != 27)
        {
            return false;
        }

        string padded = unitNumber.ToString("000");
        Sprite sprite = Resources.Load<Sprite>($"unit_{padded}");
        if (sprite == null)
        {
            if (unitNumber == 9)
            {
                return TryApplyOverrideUnitPrefab(unitNumber);
            }
            return false;
        }

        if (unitSpriteAnimator != null)
        {
            unitSpriteAnimator.enabled = false;
        }

        spriteRenderer.sprite = sprite;
        return true;
    }

    bool TryApplyOverrideUnitPrefab(int unitNumber)
    {
        string padded = unitNumber.ToString("000");
        GameObject prefab = Resources.Load<GameObject>($"Units/unit_{padded}");
        if (prefab == null)
        {
            return false;
        }

        overridePrefabVisual = Instantiate(prefab, transform);
        overridePrefabVisual.name = $"Unit{unitNumber}Override";
        overridePrefabVisual.transform.localPosition = Vector3.zero;
        overridePrefabVisual.transform.localRotation = Quaternion.identity;
        overridePrefabVisual.transform.localScale = Vector3.one;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        GameObject attackObj = new GameObject("Unit9AttackOverlay");
        attackObj.transform.SetParent(transform, false);
        attackObj.transform.localPosition = Vector3.zero;
        attackObj.transform.localRotation = Quaternion.identity;
        attackObj.transform.localScale = Vector3.one;
        unit9AttackRenderer = attackObj.AddComponent<SpriteRenderer>();
        unit9AttackRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 2;
        unit9AttackRenderer.flipX = true;
        unit9AttackRenderer.enabled = false;

        return true;
    }

    bool TryApplyUnit9PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_009");
        if (prefab == null)
        {
            return false;
        }

        overridePrefabVisual = Instantiate(prefab, transform);
        overridePrefabVisual.name = "Unit9Prefab";
        overridePrefabVisual.transform.localPosition = Vector3.zero;
        overridePrefabVisual.transform.localRotation = Quaternion.identity;
        overridePrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit9SpumPrefab = overridePrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit9SpumPrefab != null)
        {
            unit9SpumPrefab.OverrideControllerInit();
            unit9SpumPrefab.PopulateAnimationLists();
            unit9SpumAnimator = unit9SpumPrefab._anim;
            if (unit9SpumPrefab.ATTACK_List != null && unit9SpumPrefab.ATTACK_List.Count > 0)
            {
                unit9AttackDuration = Mathf.Max(0.1f, unit9SpumPrefab.ATTACK_List[0].length);
            }
            unit9SpumState = PlayerState.IDLE;
            unit9SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }

        SortingGroup sortingGroup = overridePrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit9SpumPrefab != null;
    }

    bool TryApplyUnit17PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_017");
        if (prefab == null)
        {
            return false;
        }

        unit17PrefabVisual = Instantiate(prefab, transform);
        unit17PrefabVisual.name = "Unit17Prefab";
        unit17PrefabVisual.transform.localPosition = Vector3.zero;
        unit17PrefabVisual.transform.localRotation = Quaternion.identity;
        unit17PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit17SpumPrefab = unit17PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit17SpumPrefab != null)
        {
            unit17SpumPrefab.OverrideControllerInit();
            unit17SpumPrefab.PopulateAnimationLists();
            unit17SpumAnimator = unit17SpumPrefab._anim;
            if (unit17SpumPrefab.ATTACK_List != null && unit17SpumPrefab.ATTACK_List.Count > 0)
            {
                unit17AttackDuration = Mathf.Max(0.1f, unit17SpumPrefab.ATTACK_List[0].length);
            }
            unit17SpumState = PlayerState.IDLE;
            unit17SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }

        SortingGroup sortingGroup = unit17PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit17SpumPrefab != null;
    }

    bool TryApplyUnit16PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_016");
        if (prefab == null)
        {
            return false;
        }

        unit16PrefabVisual = Instantiate(prefab, transform);
        unit16PrefabVisual.name = "Unit16Prefab";
        unit16PrefabVisual.transform.localPosition = Vector3.zero;
        unit16PrefabVisual.transform.localRotation = Quaternion.identity;
        unit16PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit16SpumPrefab = unit16PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit16SpumPrefab != null)
        {
            unit16SpumPrefab.OverrideControllerInit();
            unit16SpumPrefab.PopulateAnimationLists();
            unit16SpumAnimator = unit16SpumPrefab._anim;
            if (unit16SpumPrefab.ATTACK_List != null && unit16SpumPrefab.ATTACK_List.Count > 0)
            {
                unit16AttackDuration = Mathf.Max(0.1f, unit16SpumPrefab.ATTACK_List[0].length);
            }
            unit16SpumState = PlayerState.IDLE;
            unit16SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }

        SortingGroup sortingGroup = unit16PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit16SpumPrefab != null;
    }

    bool TryApplyUnit3PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_003");
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Addons/RetroHeroes/2_Prefab/unit_003");
        }
        if (prefab == null)
        {
            return false;
        }

        unit3PrefabVisual = Instantiate(prefab, transform);
        unit3PrefabVisual.name = "Unit3Prefab";
        unit3PrefabVisual.transform.localPosition = Vector3.zero;
        unit3PrefabVisual.transform.localRotation = Quaternion.identity;
        unit3PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit3SpumPrefab = unit3PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit3SpumPrefab != null)
        {
            unit3SpumPrefab.OverrideControllerInit();
            unit3SpumPrefab.PopulateAnimationLists();
            unit3SpumAnimator = unit3SpumPrefab._anim;
            if (unit3SpumPrefab.ATTACK_List != null && unit3SpumPrefab.ATTACK_List.Count > 0)
            {
                unit3SpumAttackDuration = Mathf.Max(0.1f, unit3SpumPrefab.ATTACK_List[0].length);
            }
            unit3SpumState = PlayerState.IDLE;
            unit3SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }

        SortingGroup sortingGroup = unit3PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit3SpumPrefab != null;
    }

    bool TryApplyUnit18PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_018");
        if (prefab == null)
        {
            return false;
        }

        unit18PrefabVisual = Instantiate(prefab, transform);
        unit18PrefabVisual.name = "Unit18Prefab";
        unit18PrefabVisual.transform.localPosition = Vector3.zero;
        unit18PrefabVisual.transform.localRotation = Quaternion.identity;
        unit18PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit18SpumPrefab = unit18PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit18SpumPrefab != null)
        {
            unit18SpumPrefab.OverrideControllerInit();
            unit18SpumPrefab.PopulateAnimationLists();
            unit18SpumAnimator = unit18SpumPrefab._anim;
            if (unit18SpumPrefab.ATTACK_List != null && unit18SpumPrefab.ATTACK_List.Count > 0)
            {
                unit18SpumAttackDuration = Mathf.Max(0.1f, unit18SpumPrefab.ATTACK_List[0].length);
            }
            unit18SpumState = PlayerState.IDLE;
            unit18SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }

        SortingGroup sortingGroup = unit18PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit18SpumPrefab != null;
    }

    bool TryApplyUnit24PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_024");
        if (prefab == null)
        {
            return false;
        }

        unit24PrefabVisual = Instantiate(prefab, transform);
        unit24PrefabVisual.name = "Unit24Prefab";
        unit24PrefabVisual.transform.localPosition = Vector3.zero;
        unit24PrefabVisual.transform.localRotation = Quaternion.identity;
        unit24PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit24SpumPrefab = unit24PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit24SpumPrefab != null)
        {
            unit24SpumPrefab.OverrideControllerInit();
            unit24SpumPrefab.PopulateAnimationLists();
            unit24SpumAnimator = unit24SpumPrefab._anim;
            if (unit24SpumPrefab.ATTACK_List != null && unit24SpumPrefab.ATTACK_List.Count > 0)
            {
                unit24SpumAttackDuration = Mathf.Max(0.1f, unit24SpumPrefab.ATTACK_List[0].length);
            }
            unit24SpumState = PlayerState.IDLE;
            unit24SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }

        SortingGroup sortingGroup = unit24PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit24SpumPrefab != null;
    }

    bool TryApplyUnit27PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_027");
        if (prefab == null)
        {
            return false;
        }

        unit27PrefabVisual = Instantiate(prefab, transform);
        unit27PrefabVisual.name = "Unit27Prefab";
        unit27PrefabVisual.transform.localPosition = Vector3.zero;
        unit27PrefabVisual.transform.localRotation = Quaternion.identity;
        unit27PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit27SpumPrefab = unit27PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit27SpumPrefab != null)
        {
            unit27SpumPrefab.OverrideControllerInit();
            unit27SpumPrefab.PopulateAnimationLists();
            unit27SpumAnimator = unit27SpumPrefab._anim;
            if (unit27SpumPrefab.ATTACK_List != null && unit27SpumPrefab.ATTACK_List.Count > 0)
            {
                unit27AttackDuration = Mathf.Max(0.1f, unit27SpumPrefab.ATTACK_List[0].length);
            }
            unit27SpumState = PlayerState.IDLE;
            unit27SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }

        SortingGroup sortingGroup = unit27PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit27SpumPrefab != null;
    }

    void PlayUnit9AttackAnimation()
    {
        if (usesUnit9Prefab && unit9SpumPrefab != null)
        {
            unit9SpumState = PlayerState.ATTACK;
            unit9SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit9SpumAnimator != null)
            {
                unit9SpumAnimator.speed = 1f;
            }
            return;
        }

        if (unit9AttackFrames == null || unit9AttackFrames.Length == 0)
        {
            return;
        }

        if (unit9AttackRoutine != null)
        {
            StopCoroutine(unit9AttackRoutine);
        }

        unit9AttackRoutine = StartCoroutine(Unit9AttackRoutine());
    }

    System.Collections.IEnumerator Unit9AttackRoutine()
    {
        isUnit9Attacking = true;
        if (unitSpriteAnimator != null)
        {
            unitSpriteAnimator.SetAnimating(false);
        }
        if (usesUnit9Prefab && unit9AttackRenderer != null)
        {
            unit9AttackRenderer.enabled = true;
        }

        SpriteRenderer targetRenderer = usesUnit9Prefab ? unit9AttackRenderer : spriteRenderer;
        if (targetRenderer == null)
        {
            yield break;
        }

        bool restoreAnimator = !usesUnit9Prefab && unitSpriteAnimator != null;
        float frameTime = 1f / Unit9AttackFps;
        for (int i = 0; i < unit9AttackFrames.Length; i++)
        {
            targetRenderer.sprite = unit9AttackFrames[i];
            if (!usesUnit9Prefab && unitSpriteAnimator != null)
            {
                unitSpriteAnimator.ApplyBottomAnchor(targetRenderer.sprite);
            }
            yield return new WaitForSeconds(frameTime);
        }

        if (usesUnit9Prefab && unit9AttackRenderer != null)
        {
            unit9AttackRenderer.enabled = false;
        }

        isUnit9Attacking = false;
        if (restoreAnimator && unitSpriteAnimator != null && IsProperlyPlaced())
        {
            unitSpriteAnimator.SetAnimating(true);
        }
        unit9AttackRoutine = null;
    }

    void PlayUnit17AttackAnimation()
    {
        if (usesUnit17Prefab && unit17SpumPrefab != null)
        {
            unit17SpumState = PlayerState.ATTACK;
            unit17SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit17SpumAnimator != null)
            {
                unit17SpumAnimator.speed = 1f;
            }
            return;
        }
    }

    void PlayUnit16AttackAnimation()
    {
        if (usesUnit16Prefab && unit16SpumPrefab != null)
        {
            unit16SpumState = PlayerState.ATTACK;
            unit16SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit16SpumAnimator != null)
            {
                unit16SpumAnimator.speed = 1f;
            }
            return;
        }
    }

    void PlayUnit18AttackAnimation()
    {
        if (usesUnit18Prefab && unit18SpumPrefab != null)
        {
            unit18SpumState = PlayerState.ATTACK;
            unit18SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit18SpumAnimator != null)
            {
                unit18SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit27AttackAnimation()
    {
        if (usesUnit27Prefab && unit27SpumPrefab != null)
        {
            unit27SpumState = PlayerState.ATTACK;
            unit27SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit27SpumAnimator != null)
            {
                unit27SpumAnimator.speed = 1f;
            }
            return;
        }
    }

    void PlayUnit19AttackAnimation()
    {
        if (usesUnit19Prefab && unit19SpumPrefab != null)
        {
            unit19SpumState = PlayerState.ATTACK;
            unit19SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit19SpumAnimator != null)
            {
                unit19SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit13AttackAnimation()
    {
        if (usesUnit13Prefab && unit13SpumPrefab != null)
        {
            unit13SpumState = PlayerState.ATTACK;
            unit13SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit13SpumAnimator != null)
            {
                unit13SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit14AttackAnimation()
    {
        if (usesUnit14Prefab && unit14SpumPrefab != null)
        {
            unit14SpumState = PlayerState.ATTACK;
            unit14SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit14SpumAnimator != null)
            {
                unit14SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit12AttackAnimation()
    {
        if (usesUnit12Prefab && unit12SpumPrefab != null)
        {
            unit12SpumState = PlayerState.ATTACK;
            unit12SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit12SpumAnimator != null)
            {
                unit12SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit23AttackAnimation()
    {
        if (usesUnit23Prefab && unit23SpumPrefab != null)
        {
            unit23SpumState = PlayerState.ATTACK;
            unit23SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit23SpumAnimator != null)
            {
                unit23SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit25AttackAnimation()
    {
        if (usesUnit25Prefab && unit25SpumPrefab != null)
        {
            unit25SpumState = PlayerState.ATTACK;
            unit25SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit25SpumAnimator != null)
            {
                unit25SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit28AttackAnimation()
    {
        if (usesUnit28Prefab && unit28SpumPrefab != null)
        {
            unit28SpumState = PlayerState.ATTACK;
            unit28SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit28SpumAnimator != null)
            {
                unit28SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit11AttackAnimation()
    {
        if (usesUnit11Prefab && unit11SpumPrefab != null)
        {
            unit11SpumState = PlayerState.ATTACK;
            unit11SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit11SpumAnimator != null)
            {
                unit11SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit20AttackAnimation()
    {
        if (usesUnit20Prefab && unit20SpumPrefab != null)
        {
            unit20SpumState = PlayerState.ATTACK;
            unit20SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit20SpumAnimator != null)
            {
                unit20SpumAnimator.speed = 1f;
            }
        }
    }

    void PlayUnit22AttackAnimation()
    {
        if (usesUnit22Prefab && unit22SpumPrefab != null)
        {
            unit22SpumState = PlayerState.ATTACK;
            unit22SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit22SpumAnimator != null)
            {
                unit22SpumAnimator.speed = 1f;
            }
        }
    }

    void UpdateUnit9SpumState(bool canAnimate)
    {
        if (unit9SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit9SpumState != PlayerState.IDLE)
        {
            unit9SpumState = PlayerState.IDLE;
            unit9SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit17SpumState(bool canAnimate)
    {
        if (unit17SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit17SpumState != PlayerState.IDLE)
        {
            unit17SpumState = PlayerState.IDLE;
            unit17SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit16SpumState(bool canAnimate)
    {
        if (unit16SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit16SpumState != PlayerState.IDLE)
        {
            unit16SpumState = PlayerState.IDLE;
            unit16SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit27SpumState(bool canAnimate)
    {
        if (unit27SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit27SpumState != PlayerState.IDLE)
        {
            unit27SpumState = PlayerState.IDLE;
            unit27SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }
    
    void Update()
    {
        HandleDrag();
        
        // 전투 일시정지(쉬는 시간·보스 보상 등) 중이면 공격 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }
        
        bool isProperlyPlaced = IsProperlyPlaced();

        if (unitNumber == 1 && unit1UsesSpum)
        {
            UpdateUnit1SpumState(isProperlyPlaced);
        }
        if (unitNumber == 2 && unit2UsesSpum)
        {
            UpdateUnit2SpumState(isProperlyPlaced);
        }
        if (unitNumber == 7 && usesUnit7Prefab)
        {
            UpdateUnit7SpumState(isProperlyPlaced);
        }
        if (unitNumber == 15 && usesUnit15Prefab)
        {
            UpdateUnit15SpumState(isProperlyPlaced);
        }
        if (unitNumber == 19 && usesUnit19Prefab)
        {
            UpdateUnit19SpumState(isProperlyPlaced);
        }
        if (unitNumber == 13 && usesUnit13Prefab)
        {
            UpdateUnit13SpumState(isProperlyPlaced);
        }
        if (unitNumber == 14 && usesUnit14Prefab)
        {
            UpdateUnit14SpumState(isProperlyPlaced);
        }
        if (unitNumber == 23 && usesUnit23Prefab)
        {
            UpdateUnit23SpumState(isProperlyPlaced);
        }
        if (unitNumber == 25 && usesUnit25Prefab)
        {
            UpdateUnit25SpumState(isProperlyPlaced);
        }
        if (unitNumber == 28 && usesUnit28Prefab)
        {
            UpdateUnit28SpumState(isProperlyPlaced);
        }
        if (unitNumber == 4 && usesUnit4Prefab)
        {
            UpdateSpumIdleState(ref unit4SpumState, unit4SpumPrefab, isProperlyPlaced);
        }
        if (unitNumber == 8 && usesUnit8Prefab)
        {
            UpdateSpumIdleState(ref unit8SpumState, unit8SpumPrefab, isProperlyPlaced);
        }
        if (unitNumber == 10 && usesUnit10Prefab)
        {
            UpdateSpumIdleState(ref unit10SpumState, unit10SpumPrefab, isProperlyPlaced);
        }
        if (unitNumber == 12 && usesUnit12Prefab)
        {
            UpdateSpumIdleState(ref unit12SpumState, unit12SpumPrefab, isProperlyPlaced);
        }
        if (unitNumber == 21 && usesUnit21Prefab)
        {
            UpdateSpumIdleState(ref unit21SpumState, unit21SpumPrefab, isProperlyPlaced);
        }
        if (unitNumber == 11 && usesUnit11Prefab)
        {
            UpdateUnit11SpumState(isProperlyPlaced);
        }
        if (unitNumber == 20 && usesUnit20Prefab)
        {
            UpdateUnit20SpumState(isProperlyPlaced);
        }
        if (unitNumber == 22 && usesUnit22Prefab)
        {
            UpdateUnit22SpumState(isProperlyPlaced);
        }
        if (unitNumber == 3 && unit3UsesCustom)
        {
            UpdateUnit3State(isProperlyPlaced);
        }
        if (unitNumber == 3 && usesUnit3Prefab)
        {
            UpdateUnit3SpumState(isProperlyPlaced);
        }
        if (unitNumber == 18 && usesUnit18Prefab)
        {
            UpdateUnit18SpumState(isProperlyPlaced);
        }
        if (unitNumber == 24 && usesUnit24Prefab)
        {
            UpdateUnit24SpumState(isProperlyPlaced);
        }
        if (unitNumber == 5 && unit5UsesSpum)
        {
            UpdateUnit5SpumState(isProperlyPlaced);
        }
        if (unitNumber == 6 && unit6UsesSpum)
        {
            UpdateUnit6SpumState(isProperlyPlaced);
        }
        if (unitNumber == 9 && usesUnit9Prefab)
        {
            UpdateUnit9SpumState(isProperlyPlaced);
        }
        if (unitNumber == 17 && usesUnit17Prefab)
        {
            UpdateUnit17SpumState(isProperlyPlaced);
        }
        if (unitNumber == 16 && usesUnit16Prefab)
        {
            UpdateUnit16SpumState(isProperlyPlaced);
        }
        if (unitNumber == 27 && usesUnit27Prefab)
        {
            UpdateUnit27SpumState(isProperlyPlaced);
        }
        
        if ((unitNumber == 1 || unitNumber == 9) && unitSpriteAnimator != null && !usesUnit9Prefab)
        {
            bool canAnimate = isProperlyPlaced;
            if (unitNumber == 9 && isUnit9Attacking)
            {
                canAnimate = false;
            }
            unitSpriteAnimator.SetAnimating(canAnimate);
        }
        
        // 4번 유닛: 골드 생성 처리
        if (unitNumber == 4 && isProperlyPlaced && !isDragging)
        {
            TrySpawnGoldIcon();
            return;
        }
        
        // 메인보드에 배치된 경우에만 공격
        if (isProperlyPlaced)
        {
            TryAttack();
        }
    }

    public bool IsProperlyPlaced()
    {
        float snap = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x) * 0.6f);
        if (currentCell == null || !currentCell.isBoardCell || currentCell.currentCharacter != this || !currentCell.isOccupied)
        {
            return false;
        }

        Vector3 expected = GetPlacementPositionForCell(currentCell);
        return Vector2.Distance(transform.position, expected) < snap;
    }

    static readonly Color DamageMeterHighlightColor = new Color(1f, 0.92f, 0.45f, 1f);
    Coroutine damageMeterHighlightRoutine;

    public static void PulseDamageMeterHighlightForUnit(int unitNumber)
    {
        if (unitNumber <= 0) return;

        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || unit.unitNumber != unitNumber) continue;
            if (!unit.IsProperlyPlaced()) continue;
            unit.PulseDamageMeterHighlight();
        }
    }

    public void PulseDamageMeterHighlight()
    {
        if (damageMeterHighlightRoutine != null)
        {
            StopCoroutine(damageMeterHighlightRoutine);
        }

        damageMeterHighlightRoutine = StartCoroutine(PulseDamageMeterHighlightRoutine());
    }

    System.Collections.IEnumerator PulseDamageMeterHighlightRoutine()
    {
        if (!IsProperlyPlaced()) yield break;

        SpriteRenderer renderer = spriteRenderer;
        if (renderer == null)
        {
            renderer = GetComponent<SpriteRenderer>();
        }
        if (renderer == null) yield break;

        Color original = renderer.color;
        const float duration = 1.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 12f);
            renderer.color = Color.Lerp(original, DamageMeterHighlightColor, pulse * 0.65f);
            yield return null;
        }

        renderer.color = original;
        damageMeterHighlightRoutine = null;
    }

    public bool IsOnBenchOrBoard()
    {
        float snap = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x) * 0.6f);
        if (currentCell == null || currentCell.currentCharacter != this || !currentCell.isOccupied)
        {
            return false;
        }

        Vector3 expected = currentCell.isBoardCell
            ? GetPlacementPositionForCell(currentCell)
            : currentCell.transform.position;
        return Vector2.Distance(transform.position, expected) < snap;
    }

    static readonly Color Unit10PadNormal = new Color(0.2f, 0.45f, 1f, 0.32f);
    static readonly Color Unit10PadHover = new Color(0.35f, 0.62f, 1f, 0.52f);
    static readonly Color Unit16MarkerNormal = new Color(1f, 0f, 0f, 0.3f);
    static readonly Color Unit16MarkerHover = new Color(1f, 0.35f, 0.35f, 0.5f);
    static readonly Color Unit19PadNormal = new Color(0.6f, 0f, 1f, 0.7f);
    static readonly Color Unit19PadHover = new Color(0.75f, 0.25f, 1f, 0.88f);

    /// <summary>사거리 호버 미리보기 시 인접 칸·레인 장판 강조.</summary>
    public void SetRangePreviewMarkerHighlight(bool highlighted)
    {
        if (!IsProperlyPlaced())
        {
            return;
        }

        switch (unitNumber)
        {
            case 10:
                ApplyMarkerHighlight(unit10UpPad, highlighted, Unit10PadNormal, Unit10PadHover);
                ApplyMarkerHighlight(unit10DownPad, highlighted, Unit10PadNormal, Unit10PadHover);
                break;
            case 16:
                ApplyMarkerHighlight(unit16LeftMarker, highlighted, Unit16MarkerNormal, Unit16MarkerHover);
                ApplyMarkerHighlight(unit16RightMarker, highlighted, Unit16MarkerNormal, Unit16MarkerHover);
                break;
            case 19:
                ApplyMarkerHighlight(unit19BarrierPad, highlighted, Unit19PadNormal, Unit19PadHover);
                break;
        }
    }

    static void ApplyMarkerHighlight(GameObject marker, bool highlighted, Color normal, Color hover)
    {
        if (marker == null) return;
        SpriteRenderer renderer = marker.GetComponent<SpriteRenderer>();
        if (renderer == null) return;
        renderer.color = highlighted ? hover : normal;
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        float characterSize = transform.localScale.x;
        return Vector2.Distance(transform.position, worldPoint) < characterSize * 0.7f;
    }

    public bool IsNecromancerHexed()
    {
        return Time.time < necromancerHexEndTime;
    }

    public void ApplyNecromancerHex(float durationSec)
    {
        float d = Mathf.Max(0.2f, durationSec);
        necromancerHexEndTime = Mathf.Max(necromancerHexEndTime, Time.time + d);
        if (necromancerHexVisualRoutine != null)
        {
            StopCoroutine(necromancerHexVisualRoutine);
        }
        necromancerHexVisualRoutine = StartCoroutine(NecromancerHexVisualRoutine(d));
    }

    IEnumerator NecromancerHexVisualRoutine(float duration)
    {
        SpriteRenderer sr = spriteRenderer;
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && !necromancerHexStored)
        {
            necromancerHexStoredColor = sr.color;
            necromancerHexStored = true;
        }
        float end = Time.time + duration;
        while (Time.time < end && sr != null)
        {
            sr.color = new Color(0.35f, 0.95f, 0.42f, necromancerHexStored ? necromancerHexStoredColor.a : sr.color.a);
            yield return null;
        }
        if (sr != null && necromancerHexStored)
        {
            sr.color = necromancerHexStoredColor;
        }
        necromancerHexStored = false;
        necromancerHexVisualRoutine = null;
    }

    public int GetDisplayAttackDamage()
    {
        return GetDamageWithUpgrade(3);
    }
    
    /// <summary>
    /// 드래그 처리를 합니다 (한 번에 1유닛만 드래그·터치 가능).
    /// </summary>
    void HandleDrag()
    {
        if (!TryGetDragPointerState(out bool pressed, out bool released, out bool held, out Vector3 worldPos))
        {
            return;
        }

        if (pressed && GoldIcon.TryHandleClick(worldPos))
        {
            return;
        }

        if (activeDragCharacter != null && activeDragCharacter != this)
        {
            return;
        }

        if (!isDragging && pressed)
        {
            if (GetPointerDragTarget(worldPos) != this)
            {
                return;
            }

            StartDrag(worldPos);
            RefreshDragPlacementPreview();
        }

        if (isDragging)
        {
            if (held)
            {
                dragPointerWorldPos = worldPos;
                RefreshDragPlacementPreview();
            }

            if (released)
            {
                dragPointerWorldPos = worldPos;
                UnitDragPlacementPreview.Hide();
                CommitDrag();
            }
        }
    }

    static Character GetPointerDragTarget(Vector3 worldPos)
    {
        int frame = Time.frameCount;
        if (dragPickFrame == frame)
        {
            return dragPickCache;
        }

        dragPickFrame = frame;
        dragPickCache = null;

        Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        float bestDistance = float.MaxValue;
        foreach (Character character in characters)
        {
            if (character == null || !character.IsOnBenchOrBoard()) continue;
            if (!character.ContainsWorldPoint(worldPos)) continue;

            float distance = Vector2.Distance(character.transform.position, worldPos);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                dragPickCache = character;
            }
        }

        return dragPickCache;
    }

    static bool TryGetDragPointerState(out bool pressed, out bool released, out bool held, out Vector3 worldPos)
    {
        pressed = false;
        released = false;
        held = false;
        worldPos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
        var touchScreen = UnityEngine.InputSystem.Touchscreen.current;
        if (touchScreen != null)
        {
            var touch = touchScreen.primaryTouch;
            bool touchActive = touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame;
            if (touchActive)
            {
                pressed = touch.press.wasPressedThisFrame;
                released = touch.press.wasReleasedThisFrame;
                held = touch.isInProgress;
                worldPos = GetWorldPositionFromScreenStatic(touch.position.ReadValue());
                return true;
            }
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            pressed = mouse.leftButton.wasPressedThisFrame;
            released = mouse.leftButton.wasReleasedThisFrame;
            held = mouse.leftButton.isPressed;
            worldPos = GetWorldPositionFromScreenStatic(mouse.position.ReadValue());
            return pressed || released || held;
        }
#endif

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pressed = touch.phase == TouchPhase.Began;
            released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Began;
            worldPos = GetWorldPositionFromScreenStatic(touch.position);
            return true;
        }

        pressed = Input.GetMouseButtonDown(0);
        released = Input.GetMouseButtonUp(0);
        held = Input.GetMouseButton(0);
        worldPos = GetWorldPositionFromScreenStatic(Input.mousePosition);
        return pressed || released || held;
    }

    static Vector3 GetWorldPositionFromScreenStatic(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 pos = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(pos);
        world.z = 0f;
        return world;
    }
    
    /// <summary>
    /// 드래그를 시작합니다
    /// </summary>
    void StartDrag(Vector3 mousePosition)
    {
        if (activeDragCharacter != null && activeDragCharacter != this)
        {
            return;
        }

        activeDragCharacter = this;
        isDragging = true;
        UnitFieldTooltip.Hide();
        dragPointerWorldPos = mousePosition;
        dragOriginCell = currentCell;
    }

    void RefreshDragPlacementPreview()
    {
        BoardCell originCell = dragOriginCell != null ? dragOriginCell : currentCell;
        if (originCell == null)
        {
            UnitDragPlacementPreview.Hide();
            return;
        }

        UnitDragPlacementPreview.Kind kind = EvaluateDropPreviewKind(originCell, dragPointerWorldPos, out BoardCell targetCell);
        Vector3 originPos = GetDropPreviewWorldPosition(originCell);
        Vector3 targetPos = targetCell != null ? GetDropPreviewWorldPosition(targetCell) : dragPointerWorldPos;
        UnitDragPlacementPreview.UpdatePreview(kind, targetCell, dragPointerWorldPos, originPos, targetPos);
    }

    UnitDragPlacementPreview.Kind EvaluateDropPreviewKind(BoardCell originCell, Vector3 dropPos, out BoardCell targetCell)
    {
        targetCell = null;
        if (originCell == null)
        {
            return UnitDragPlacementPreview.Kind.None;
        }

        targetCell = FindNearestCellAt(dropPos, true);
        if (targetCell == null)
        {
            return UnitDragPlacementPreview.Kind.Invalid;
        }

        if (targetCell == originCell)
        {
            return UnitDragPlacementPreview.Kind.SameCell;
        }

        if (targetCell.isOccupied && targetCell.currentCharacter != null)
        {
            Character other = targetCell.currentCharacter;
            if (CanMergeWith(other))
            {
                return UnitDragPlacementPreview.Kind.Merge;
            }

            return UnitDragPlacementPreview.Kind.Swap;
        }

        if (!targetCell.CanPlaceCharacter())
        {
            return UnitDragPlacementPreview.Kind.Invalid;
        }

        if (targetCell.isBoardCell && !CanPlaceOnBoardFrom(originCell))
        {
            return UnitDragPlacementPreview.Kind.Blocked;
        }

        return UnitDragPlacementPreview.Kind.Move;
    }

    public Vector3 GetDropPreviewWorldPosition(BoardCell cell)
    {
        return GetPlacementPositionForCell(cell);
    }

    void CommitDrag()
    {
        isDragging = false;
        if (activeDragCharacter == this)
        {
            activeDragCharacter = null;
        }

        BoardCell originCell = dragOriginCell != null ? dragOriginCell : currentCell;
        dragOriginCell = null;
        Vector3 dropPos = dragPointerWorldPos;

        if (originCell == null)
        {
            return;
        }

        BoardCell dropCell = FindNearestCellAt(dropPos, true);
        if (dropCell == null)
        {
            ShowInvalidPlacementNotice();
            return;
        }

        if (dropCell == originCell)
        {
            return;
        }

        if (dropCell.isOccupied && dropCell.currentCharacter != null)
        {
            Character other = dropCell.currentCharacter;
            if (CanMergeWith(other))
            {
                originCell.RemoveCharacter();
                other.IncreaseEvolutionLevel();
                Destroy(gameObject);
                return;
            }

            TrySwapWithOccupiedCell(dropCell, originCell, other);
            return;
        }

        if (!dropCell.CanPlaceCharacter())
        {
            ShowInvalidPlacementNotice();
            return;
        }

        if (dropCell.isBoardCell && !CanPlaceOnBoardFrom(originCell))
        {
            ShowPlacementLimitNotice();
            return;
        }

        originCell.RemoveCharacter();
        if (!dropCell.PlaceCharacter(this))
        {
            originCell.PlaceCharacter(this);
            SnapToCell(originCell);
            return;
        }

        SnapToCell(dropCell);
    }

    bool TrySwapWithOccupiedCell(BoardCell targetCell, BoardCell originCell, Character other)
    {
        if (targetCell == null || originCell == null || other == null || other == this)
        {
            return false;
        }

        if (targetCell == originCell)
        {
            return false;
        }

        if (originCell.currentCharacter != this || targetCell.currentCharacter != other)
        {
            return false;
        }

        originCell.RemoveCharacter();
        targetCell.RemoveCharacter();

        if (!targetCell.PlaceCharacter(this))
        {
            targetCell.PlaceCharacter(other);
            originCell.PlaceCharacter(this);
            SnapToCell(originCell);
            return false;
        }

        SnapToCell(targetCell);

        if (!originCell.PlaceCharacter(other))
        {
            targetCell.RemoveCharacter();
            originCell.RemoveCharacter();
            originCell.PlaceCharacter(this);
            targetCell.PlaceCharacter(other);
            SnapToCell(originCell);
            other.SnapToCell(targetCell);
            return false;
        }

        other.SnapToCell(originCell);
        return true;
    }

    void RestoreToOriginOrBench(BoardCell originCell)
    {
        if (originCell != null && originCell.CanPlaceCharacter() && originCell.PlaceCharacter(this))
        {
            SnapToCell(originCell);
            return;
        }

        MoveToBenchOrFallback();
    }

    bool CanPlaceOnBoard()
    {
        return CanPlaceOnBoardFrom(currentCell);
    }

    bool CanPlaceOnBoardFrom(BoardCell originCell)
    {
        if (originCell != null && originCell.isBoardCell)
        {
            return true;
        }

        int maxUnits = GetMaxBoardUnits();
        int currentUnits = CountBoardUnits();
        return currentUnits < maxUnits;
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

    void ShowPlacementLimitNotice()
    {
        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.ShowPlacementNotice(GameLocalization.PlacementInvalid);
        }
        else
        {
            Debug.Log("배치할 수 없습니다");
        }
    }

    void ShowInvalidPlacementNotice()
    {
        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.ShowPlacementNotice(GameLocalization.PlacementNotBoard);
        }
        else
        {
            Debug.Log("배치칸이 아닌 곳에는 이동할 수 없습니다");
        }
    }

    void MoveToBenchOrFallback()
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board != null && board.TryMoveCharacterToBench(this))
        {
            return;
        }

        if (currentCell != null)
        {
            SnapToCell(currentCell);
        }
    }

    void SnapToCell(BoardCell cell)
    {
        if (cell == null) return;
        transform.position = GetPlacementPositionForCell(cell);
        UpdateUnit9AnchorPosition();
    }

    public void SnapToAssignedCell()
    {
        SnapToCell(currentCell);
    }

    Vector3 GetPlacementPositionForCell(BoardCell cell)
    {
        if (cell == null) return transform.position;
        Vector3 pos = cell.transform.position;
        if (cell.isBoardCell)
        {
            pos += GetBoardPlacementWorldOffset();
        }
        return pos;
    }

    Vector3 GetBoardPlacementWorldOffset()
    {
        if (unitNumber != 26) return Vector3.zero;
        return Vector3.up * (GetBoardCellSizeReference() * Unit26BoardPlacementCellUp);
    }

    float GetBoardCellSizeReference()
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board != null)
        {
            return Mathf.Max(0.5f, board.cellSize);
        }

        return Mathf.Max(0.5f, Mathf.Abs(transform.localScale.x) * 3f);
    }

    void GetEvolutionStarLayout(out float starSize, out float yOffset, out float spacingFactor)
    {
        starSize = transform.localScale.x * 0.4f;
        yOffset = transform.localScale.y * 0.6f;
        spacingFactor = 0.38f;

        if (unitNumber != 26)
        {
            return;
        }

        float cell = GetBoardCellSizeReference();
        starSize = cell * Unit26EvolutionStarCellSizeFactor;
        yOffset = cell * Unit26EvolutionStarHeightCellFactor;
        if (currentCell != null && currentCell.isBoardCell)
        {
            yOffset += GetBoardPlacementWorldOffset().y;
        }

        spacingFactor = Unit26EvolutionStarSpacingFactor;
    }

    static void EnsureUnitFloorShadowSpriteLoaded()
    {
        if (unitFloorShadowSprite != null) return;

        unitFloorShadowSprite = Resources.Load<Sprite>("unit_floor_shadow");
        if (unitFloorShadowSprite != null) return;

        unitFloorShadowSprite = Resources.Load<Sprite>("unit_tile");
        if (unitFloorShadowSprite != null) return;

        unitFloorShadowSprite = Resources.Load<Sprite>("unit_tile_0");
        if (unitFloorShadowSprite != null) return;

        Sprite[] tiles = Resources.LoadAll<Sprite>("unit_tile");
        if (tiles == null || tiles.Length == 0) return;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null && tiles[i].name == "unit_tile_0")
            {
                unitFloorShadowSprite = tiles[i];
                return;
            }
        }

        unitFloorShadowSprite = tiles[0];
    }

    void SetupUnit26FloorShadow()
    {
        if (unitNumber != 26) return;

        Transform existing = transform.Find("Unit26Shadow");
        if (existing != null)
        {
            unit26ShadowRenderer = existing.GetComponent<SpriteRenderer>();
            UpdateUnit26FloorShadowLayout();
            return;
        }

        EnsureUnitFloorShadowSpriteLoaded();

        GameObject shadowObj = new GameObject("Unit26Shadow");
        shadowObj.transform.SetParent(transform, false);
        shadowObj.transform.SetAsFirstSibling();

        unit26ShadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        unit26ShadowRenderer.sprite = unitFloorShadowSprite != null
            ? unitFloorShadowSprite
            : CreateSquareSprite(Color.black);
        unit26ShadowRenderer.color = Unit26FloorShadowColor;
        unit26ShadowRenderer.flipX = false;
        unit26ShadowRenderer.flipY = false;

        UpdateUnit26FloorShadowLayout();
    }

    void UpdateUnit26FloorShadowLayout()
    {
        if (unit26ShadowRenderer == null) return;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        EnsureUnitFloorShadowSpriteLoaded();

        float cell = GetBoardCellSizeReference();
        float unitScale = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x));

        float spriteExtent = 1f;
        if (unitFloorShadowSprite != null)
        {
            Vector2 shadowSize = unitFloorShadowSprite.bounds.size;
            spriteExtent = Mathf.Max(shadowSize.x, shadowSize.y, 0.01f);
        }

        float spumVisualScale = cell * SpumUnitBoardVisualScaleMul * SpumShadowRootScaleMul;
        float worldShadowW = spumVisualScale * SpumShadowChildScaleX * spriteExtent;
        float worldShadowH = spumVisualScale * SpumShadowChildScaleY * spriteExtent;
        float worldFootY = spumVisualScale * SpumShadowLocalYOffset;

        float shadowWidth = (worldShadowW / unitScale) * Unit26FloorShadowSizeMul;
        float shadowHeight = (worldShadowH / unitScale) * Unit26FloorShadowSizeMul;
        float footY = worldFootY / unitScale;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float spriteFoot = spriteRenderer.sprite.bounds.min.y * 0.98f;
            footY = Mathf.Min(footY, spriteFoot);
        }

        Transform shadowTransform = unit26ShadowRenderer.transform;
        shadowTransform.localPosition = new Vector3(0f, footY, 0f);
        shadowTransform.localScale = new Vector3(shadowWidth, shadowHeight, 1f);

        int bodyOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 1;
        unit26ShadowRenderer.sortingOrder = bodyOrder - 1;
        unit26ShadowRenderer.sortingLayerID = spriteRenderer != null
            ? spriteRenderer.sortingLayerID
            : 0;
    }

    static void EnsureUnitGradeStarSpriteLoaded()
    {
        if (unitGradeStarSprite != null) return;

        unitGradeStarSprite = Resources.Load<Sprite>("unit_grade_star");
        if (unitGradeStarSprite != null) return;

        unitGradeStarSprite = Resources.Load<Sprite>("unit_grade_star_0");
        if (unitGradeStarSprite != null) return;

        Sprite[] sprites = Resources.LoadAll<Sprite>("unit_grade_star");
        if (sprites == null || sprites.Length == 0) return;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name == "unit_grade_star_0")
            {
                unitGradeStarSprite = sprites[i];
                return;
            }
        }

        unitGradeStarSprite = sprites[0];
    }
    
    /// <summary>
    /// 마우스가 캐릭터 위에 있는지 확인합니다
    /// </summary>
    bool IsMouseOverCharacter(Vector3 mousePosition)
    {
        return ContainsWorldPoint(mousePosition);
    }

    void UpdateUnit9AnchorPosition()
    {
        if (unitNumber == 9 && unitSpriteAnimator != null && spriteRenderer != null)
        {
            unitSpriteAnimator.SetAnchorPosition(spriteRenderer.transform.localPosition);
        }
    }

    void SetupUnit9Visual()
    {
        GameObject visualObj = new GameObject("Unit9Visual");
        visualObj.transform.SetParent(transform, false);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;
        visualObj.transform.localScale = Vector3.one;

        SpriteRenderer visualRenderer = visualObj.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = spriteRenderer.sprite;
        visualRenderer.color = spriteRenderer.color;
        visualRenderer.sortingOrder = spriteRenderer.sortingOrder;
        visualRenderer.flipX = spriteRenderer.flipX;
        visualRenderer.flipY = spriteRenderer.flipY;
        visualRenderer.sharedMaterial = spriteRenderer.sharedMaterial;

        spriteRenderer.enabled = false;
        spriteRenderer = visualRenderer;
    }
    
    /// <summary>
    /// 마우스 월드 좌표를 가져옵니다
    /// </summary>
    Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPos = Input.mousePosition;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
#endif
        return GetWorldPositionFromScreen(screenPos);
    }
    
    /// <summary>
    /// 터치 월드 좌표를 가져옵니다
    /// </summary>
    Vector3 GetTouchWorldPosition()
    {
        Vector2 touchPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            touchPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            return GetWorldPositionFromScreen(touchPos);
        }
#endif
        if (Input.touchCount > 0)
        {
            touchPos = Input.GetTouch(0).position;
        }
        return GetWorldPositionFromScreen(touchPos);
    }

    Vector3 GetWorldPositionFromScreen(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return transform.position;
        }

        Vector3 pos = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(pos);
        worldPos.z = 0f; // 2D 게임이므로 z는 0
        return worldPos;
    }
    
    /// <summary>
    /// 가장 가까운 칸을 찾습니다
    /// </summary>
    BoardCell FindNearestCell(bool includeOccupied)
    {
        return FindNearestCellAt(transform.position, includeOccupied);
    }

    BoardCell FindNearestCellAt(Vector3 worldPos, bool includeOccupied)
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        BoardCell nearestCell = null;
        float nearestDistance = float.MaxValue;
        float snapDistance = transform.localScale.x * 0.6f;

        foreach (BoardCell cell in cells)
        {
            if (!includeOccupied && !cell.CanPlaceCharacter()) continue;

            float distance = Vector2.Distance(worldPos, cell.transform.position);
            if (distance < snapDistance && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCell = cell;
            }
        }

        return nearestCell;
    }
    
    /// <summary>
    /// 현재 칸을 설정합니다
    /// </summary>
    public void SetCell(BoardCell cell)
    {
        currentCell = cell;
        UpdateUnit16SideMarkers();
        UpdateUnit10NeighbourPads();
        UpdateUnit19BarrierPad();
        if (unitNumber == 26 && cell != null && cell.isBoardCell)
        {
            UpdateEvolutionStars();
        }
    }

    public BoardCell GetCurrentBoardCell()
    {
        return currentCell;
    }

    float GetBoardCellDamageMultiplier()
    {
        if (currentCell == null || !currentCell.isBoardCell) return 1f;
        if (GameManager.Instance == null) return 1f;
        if (!TryParseBoardCellName(currentCell.gameObject.name, out int row, out int col)) return 1f;
        return Mathf.Max(0.01f, GameManager.Instance.GetBoardDamageMultiplier(row, col));
    }

    float GetBoardCellAttackSpeedMultiplier()
    {
        if (currentCell == null || !currentCell.isBoardCell) return 1f;
        if (GameManager.Instance == null) return 1f;
        if (!TryParseBoardCellName(currentCell.gameObject.name, out int row, out int col)) return 1f;
        return Mathf.Max(0.01f, GameManager.Instance.GetBoardAttackSpeedMultiplier(row, col));
    }

    /*
    bool IsOnMatchingSynergyCellForTrait(string trait)
    {
        if (currentCell == null || !currentCell.isBoardCell || GameManager.Instance == null) return false;
        if (!TryParseBoardCellName(currentCell.gameObject.name, out int row, out int col)) return false;
        return GameManager.Instance.IsUnitOnMatchingSynergyCell(unitNumber, row, col, trait);
    }
    */

    /// <summary>보스1 등: 전장 보드에서 적(좀비) 쪽(열 +방향)으로 2칸(가능한 만큼) 강제 이동. 목표 칸이 막혀 있으면 실패.</summary>
    public bool Boss1KnockTowardEnemy(int columnSteps)
    {
        if (columnSteps <= 0 || currentCell == null || !currentCell.isBoardCell)
        {
            return false;
        }
        if (!TryParseBoardCellName(currentCell.gameObject.name, out int row, out int col))
        {
            return false;
        }
        BoardManager board = FindFirstObjectByType<BoardManager>();
        int maxCol = board != null && board.boardColumns > 0 ? board.boardColumns - 1 : 2;
        int targetCol = Mathf.Min(col + columnSteps, maxCol);
        if (targetCol <= col) return false;
        BoardCell target = FindBoardCellByIndices(row, targetCol);
        if (target == null || target.isOccupied) return false;
        BoardCell from = currentCell;
        from.RemoveCharacter();
        if (!target.PlaceCharacter(this))
        {
            from.PlaceCharacter(this);
            return false;
        }

        SnapToCell(target);
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

    static BoardCell FindBoardCellByIndices(int row, int col)
    {
        string name = $"BoardCell_{row}_{col}";
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        foreach (BoardCell b in cells)
        {
            if (b != null && b.isBoardCell && b.gameObject.name == name) return b;
        }
        return null;
    }

    public bool CanMergeWith(Character other)
    {
        if (other == null) return false;
        if (unitNumber != other.unitNumber) return false;
        if (evolutionLevel != other.evolutionLevel) return false;
        return evolutionLevel < UnitCombatStats.MaxEvolutionLevel;
    }

    public void IncreaseEvolutionLevel()
    {
        evolutionLevel = Mathf.Clamp(evolutionLevel + 1, 1, UnitCombatStats.MaxEvolutionLevel);
        RunEncounterTracker.RegisterEvolution(unitNumber, evolutionLevel);
        ApplyBaseCombatStats();
        UpdateEvolutionStars();
        UnitAcquireEffect.PlayEvolution(this);
    }

    void UpdateEvolutionStars()
    {
        if (evolutionStarsRoot == null)
        {
            GameObject root = new GameObject("EvolutionStars");
            root.transform.SetParent(transform, false);
            evolutionStarsRoot = root.transform;
        }

        for (int i = evolutionStarsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(evolutionStarsRoot.GetChild(i).gameObject);
        }

        UnitCombatStats.GetEvolutionStarDisplay(evolutionLevel, out int count, out UnitCombatStats.EvolutionStarTier tier);
        Color starColor = UnitCombatStats.GetEvolutionStarColor(tier);
        GetEvolutionStarLayout(out float starSize, out float y, out float spacingFactor);
        float spacing = starSize * spacingFactor;
        float totalWidth = (count - 1) * spacing;
        float startX = -totalWidth * 0.5f;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        int sortOrder = 2;
        int sortingLayerId = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        SpriteRenderer[] unitRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < unitRenderers.Length; i++)
        {
            SpriteRenderer r = unitRenderers[i];
            if (r == null) continue;
            if (r.gameObject.name == "Unit26Shadow") continue;
            if (r.sortingOrder > sortOrder) sortOrder = r.sortingOrder;
        }
        sortOrder += 20; // 유닛의 모든 파츠보다 확실히 앞에 보이도록 여유값 추가

        for (int i = 0; i < count; i++)
        {
            GameObject starObj = new GameObject($"Star_{i + 1}");
            starObj.transform.SetParent(evolutionStarsRoot, false);
            starObj.transform.localPosition = new Vector3(startX + (i * spacing), y, 0f);
            starObj.transform.localScale = new Vector3(starSize, starSize, 1f);

            SpriteRenderer starRenderer = starObj.AddComponent<SpriteRenderer>();
            EnsureUnitGradeStarSpriteLoaded();

            if (unitGradeStarSprite != null)
            {
                starRenderer.sprite = unitGradeStarSprite;
                starRenderer.color = starColor;
            }
            else
            {
                Color fallbackColor = tier == UnitCombatStats.EvolutionStarTier.White
                    ? new Color(1f, 0.9f, 0.1f)
                    : starColor;
                starRenderer.sprite = CreateSquareSprite(fallbackColor);
                starRenderer.color = fallbackColor;
            }
            starRenderer.sortingLayerID = sortingLayerId;
            starRenderer.sortingOrder = sortOrder;
        }
    }

    void UpdateUnit16SideMarkers()
    {
        if (unitNumber != 16)
        {
            ClearUnit16SideMarkers();
            return;
        }

        if (currentCell == null || !currentCell.isBoardCell)
        {
            ClearUnit16SideMarkers();
            return;
        }

        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        float cellSize = boardManager != null ? boardManager.cellSize : 1f;
        Color markerColor = new Color(1f, 0f, 0f, 0.3f);

        int span = UnitEvolutionMilestones.GetUnit16AuraCellSpan(evolutionLevel);
        Vector3 center = currentCell.transform.position;
        unit16LeftMarker = CreateOrMoveMarker(unit16LeftMarker, center + Vector3.left * cellSize * span, cellSize, markerColor, "Unit16LeftMarker");
        unit16RightMarker = CreateOrMoveMarker(unit16RightMarker, center + Vector3.right * cellSize * span, cellSize, markerColor, "Unit16RightMarker");
    }

    /// <summary>유닛 10: 본인 칸 기준 위·아래 인접 1칸씩(총 2칸), 유닛 16 쪽 마커와 동일한 방식의 반투명 사각 장판(파랑).</summary>
    void UpdateUnit10NeighbourPads()
    {
        if (unitNumber != 10)
        {
            ClearUnit10NeighbourPads();
            return;
        }
        if (currentCell == null || !currentCell.isBoardCell)
        {
            ClearUnit10NeighbourPads();
            return;
        }
        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        float cellSize = boardManager != null ? boardManager.cellSize : 1f;
        Color padColor = new Color(0.2f, 0.45f, 1f, 0.32f);
        Vector3 c = currentCell.transform.position;
        unit10UpPad = CreateOrMoveMarker(unit10UpPad, c + Vector3.up * cellSize, cellSize, padColor, "Unit10UpPad");
        unit10DownPad = CreateOrMoveMarker(unit10DownPad, c + Vector3.down * cellSize, cellSize, padColor, "Unit10DownPad");
    }

    void ClearUnit10NeighbourPads()
    {
        if (unit10UpPad != null)
        {
            Destroy(unit10UpPad);
            unit10UpPad = null;
        }
        if (unit10DownPad != null)
        {
            Destroy(unit10DownPad);
            unit10DownPad = null;
        }
    }

    void UpdateUnit19BarrierPad()
    {
        if (unitNumber != 19)
        {
            ClearUnit19BarrierPad();
            return;
        }

        if (currentCell == null || !currentCell.isBoardCell)
        {
            ClearUnit19BarrierPad();
            return;
        }

        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        if (boardManager == null)
        {
            ClearUnit19BarrierPad();
            return;
        }

        float cellSize = boardManager.cellSize;
        if (!boardManager.TryGetLaneBounds(out float minY, out float maxY))
        {
            ClearUnit19BarrierPad();
            return;
        }

        if (!TryGetBoardXBounds(out float minX, out float maxX))
        {
            ClearUnit19BarrierPad();
            return;
        }

        float fullWidth = (maxX - minX) + cellSize;
        float width = fullWidth * UnitEvolutionMilestones.GetUnit19PadWidthMul(evolutionLevel);
        float height = maxY - minY;
        float centerY = (minY + maxY) * 0.5f;
        float padCenterX = (maxX + cellSize * 0.5f) + (width * 0.5f);
        GameObject barrier = GameObject.Find("Barrier");
        if (barrier != null)
        {
            padCenterX = barrier.transform.position.x + (width * 0.5f);
        }
        Vector3 center = new Vector3(padCenterX, centerY, 0f);
        Color padColor = new Color(0.6f, 0f, 1f, 0.7f);

        unit19BarrierPad = CreateOrMovePad(unit19BarrierPad, center, width, height, padColor, "Unit19BarrierPad");
    }

    void ClearUnit19BarrierPad()
    {
        if (unit19BarrierPad != null)
        {
            Destroy(unit19BarrierPad);
            unit19BarrierPad = null;
        }
    }

    bool TryGetBoardXBounds(out float minX, out float maxX)
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        minX = float.MaxValue;
        maxX = float.MinValue;
        bool found = false;

        foreach (BoardCell cell in cells)
        {
            if (cell == null || !cell.isBoardCell) continue;
            float x = cell.transform.position.x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            found = true;
        }

        return found;
    }

    GameObject CreateOrMovePad(GameObject pad, Vector3 position, float width, float height, Color color, string name)
    {
        if (pad == null)
        {
            pad = new GameObject(name);
            SpriteRenderer renderer = pad.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite(color);
            renderer.color = color;
            renderer.sortingOrder = -1;
        }
        pad.transform.position = position;
        pad.transform.localScale = new Vector3(width, height, 1f);
        Unit19BarrierPad padLogic = pad.GetComponent<Unit19BarrierPad>();
        if (padLogic == null)
        {
            padLogic = pad.AddComponent<Unit19BarrierPad>();
        }
        int padDamage = Mathf.Max(1, Mathf.RoundToInt(GetPeriodicDamage(1f) * UnitEvolutionMilestones.GetUnit19DamageTickMul(evolutionLevel)));
        float padInterval = UnitEvolutionMilestones.GetUnit19TickInterval(evolutionLevel);
        padLogic.Configure(width, height, padDamage, padInterval, UnitEvolutionMilestones.HasUnit19SlowOnPad(evolutionLevel));
        padLogic.sourceUnitNumber = unitNumber;
        return pad;
    }

    GameObject CreateOrMoveMarker(GameObject marker, Vector3 position, float size, Color color, string name)
    {
        if (marker == null)
        {
            marker = new GameObject(name);
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite(color);
            renderer.color = color;
            renderer.sortingOrder = 0;
        }
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * size;
        return marker;
    }

    void ClearUnit16SideMarkers()
    {
        if (unit16LeftMarker != null)
        {
            Destroy(unit16LeftMarker);
            unit16LeftMarker = null;
        }
        if (unit16RightMarker != null)
        {
            Destroy(unit16RightMarker);
            unit16RightMarker = null;
        }
    }
    
    /// <summary>
    /// 공격 명중 시, 발사 유닛의 속성 조합(얼음·불·번개) 효과를 대상에 적용합니다.
    /// </summary>
    void ApplyComboOnHit(Zombie target)
    {
        // 조합 온히트 효과는 TraitPeriodicAttackRunner 주기 특수 공격으로 이전됨.
    }

    /// <summary>번개 연쇄: 대상 주변의 다른 좀비에게 추가 피해를 줍니다.</summary>
    void ApplyChainDamage(Zombie origin, int count, int damage)
    {
        if (origin == null || count <= 0 || damage <= 0) return;

        const float chainRadius = 2.5f;
        Zombie[] all = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        Vector3 originPos = origin.transform.position;

        System.Collections.Generic.HashSet<Zombie> hit = new System.Collections.Generic.HashSet<Zombie>();
        for (int round = 0; round < count; round++)
        {
            Zombie best = null;
            float bestDist = chainRadius;
            for (int i = 0; i < all.Length; i++)
            {
                Zombie z = all[i];
                if (z == null || z == origin || z.IsExcludedFromCombat) continue;
                if (hit.Contains(z)) continue;
                float d = Vector2.Distance(originPos, z.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = z;
                }
            }
            if (best == null) break;
            hit.Add(best);
            best.TakeDamage(damage, unitNumber);
        }
    }

    /// <summary>
    /// 공격을 시도합니다 (색상에 따라 다른 패턴)
    /// </summary>
    void TryAttack()
    {
        if (IsNecromancerHexed()) return;
        if (!UnitCombatStats.CanAutoAttack(unitNumber)) return;
        float effectiveCooldown = attackCooldown;
        float attackSpeedMul = GetBoardCellAttackSpeedMultiplier();
        float traitAttackSpeedMul = TraitManager.Instance != null ? TraitManager.Instance.GetAttackSpeedMultiplierForUnit(unitNumber) : 1f;
        attackSpeedMul *= traitAttackSpeedMul;
        attackSpeedMul *= UnitEvolutionMilestones.GetAttackSpeedMultiplier(unitNumber, evolutionLevel);
        attackSpeedMul *= GetUnit16AuraAttackSpeedMultiplier();
        if (attackSpeedMul > 0.0001f)
        {
            effectiveCooldown /= attackSpeedMul;
        }
        if (GetCombatActionTime() < lastAttackTime + effectiveCooldown) return;
        if (unitNumber == 16) return;
        if (unitNumber == 19) return;
        if (unitNumber == 12)
        {
            Zombie target12 = ResolvePrimaryAttackTarget();
            if (target12 == null) return;

            PlayUnit12AttackAnimation();
            FireUnit12JudgmentLance(target12);
            ApplyComboOnHit(target12);
            PlayAllyAttackSfx(true);
            lastAttackTime = GetCombatActionTime();
            return;
        }
        Zombie targetZombie = ResolvePrimaryAttackTarget();
        if (targetZombie == null) return;
        ApplyComboOnHit(targetZombie);

        if (unitNumber == 1 && unit1UsesSpum)
        {
            PlayUnit1AttackAnimation();
        }
        if (unitNumber == 2 && unit2UsesSpum)
        {
            PlayUnit2AttackAnimation();
        }
        if (unitNumber == 3)
        {
            PlayUnit3AttackAnimation();
        }
        if (unitNumber == 5)
        {
            PlayUnit5AttackAnimation();
        }
        if (unitNumber == 6 && unit6UsesSpum)
        {
            PlayUnit6AttackAnimation();
        }
        if (unitNumber == 22 && usesUnit22Prefab)
        {
            PlayUnit22AttackAnimation();
        }
        if (unitNumber == 13 && usesUnit13Prefab)
        {
            PlayUnit13AttackAnimation();
        }
        if (unitNumber == 14 && usesUnit14Prefab)
        {
            PlayUnit14AttackAnimation();
        }
        if (unitNumber == 23 && usesUnit23Prefab)
        {
            PlayUnit23AttackAnimation();
        }
        if (unitNumber == 25 && usesUnit25Prefab)
        {
            PlayUnit25AttackAnimation();
        }
        if (unitNumber == 28 && usesUnit28Prefab)
        {
            PlayUnit28AttackAnimation();
        }
        if (unitNumber == 11 && usesUnit11Prefab)
        {
            PlayUnit11AttackAnimation();
        }
        if (unitNumber == 20 && usesUnit20Prefab)
        {
            PlayUnit20AttackAnimation();
        }

        if (unitNumber == 1)
        {
            FireUnit1Projectile(targetZombie);
        }
        else if (unitNumber == 3)
        {
            FireElectricProjectile(targetZombie);
        }
        else if (unitNumber == 5)
        {
            unit5PendingStrikeTarget = targetZombie;
        }
        else if (unitNumber == 6)
        {
            FireIceProjectile(targetZombie);
        }
        else if (unitNumber == 7)
        {
            FireWideVerticalProjectile();
            PlayUnit7AttackAnimation();
        }
        else if (unitNumber == 9)
        {
            FireArrowSpread();
            PlayUnit9AttackAnimation();
        }
        else if (unitNumber == 17)
        {
            FireGiantFistStrike(targetZombie);
            PlayUnit17AttackAnimation();
        }
        else if (unitNumber == 18)
        {
            FireUnit18ArrowStrike();
            PlayUnit18AttackAnimation();
        }
        else if (unitNumber == 16)
        {
            FireGiantFistStrike(targetZombie);
            PlayUnit16AttackAnimation();
        }
        else if (unitNumber == 27)
        {
            FireUnit27Stone(targetZombie);
            PlayUnit27AttackAnimation();
        }
        else if (unitNumber == 24)
        {
            FireUnit24RicochetProjectile(targetZombie);
        }
        else if (unitNumber == 22)
        {
            FireUnit22RootPad();
            PlayUnit22AttackAnimation();
        }
        else if (unitNumber == 13)
        {
            FireUnit13Shuriken(targetZombie);
        }
        else if (unitNumber == 14)
        {
            FireUnit14BouncingProjectiles();
        }
        else if (unitNumber == 23)
        {
            FireUnit23PiercingShot(targetZombie);
        }
        else if (unitNumber == 25)
        {
            FireUnit25LaserBeam(targetZombie);
        }
        else if (unitNumber == 11)
        {
            FireUnit11IceColumn();
        }
        else if (unitNumber == 20)
        {
            FireUnit20LightningLasers();
        }
        else if (unitNumber == 28)
        {
            FireUnit28VortexProjectile();
        }
        else if (unitNumber == 8)
        {
            FireUnit8VomitLaser(targetZombie);
        }
        else if (unitNumber == 21)
        {
            FireUnit21FanField(targetZombie);
        }
        else if (unitNumber == 26)
        {
            FireUnit26RicochetProjectile(targetZombie);
        }
        else if (unitNumber == 10)
        {
            FireUnit10Attack(targetZombie);
        }
        else
        {
            FireProjectile(targetZombie);
        }
        PlayAllyAttackSfx(UsesProjectileAttackSfx(unitNumber));
        lastAttackTime = GetCombatActionTime();
    }

    static float GetCombatActionTime()
    {
        return GameManager.Instance != null ? GameManager.Instance.CombatActionTime : Time.time;
    }

    static bool UsesProjectileAttackSfx(int unitNum)
    {
        switch (unitNum)
        {
            case 5:
            case 11:
            case 17:
            case 20:
            case 21:
            case 22:
            case 25:
                return false;
            default:
                return true;
        }
    }

    static void PlayAllyAttackSfx(bool isProjectile)
    {
        if (isProjectile) GameSfxPlayer.PlayAllyProjectileAttack();
        else GameSfxPlayer.PlayAllyNonProjectileAttack();
    }

    bool SetupUnit1SpumVisual()
    {
        GameObject prefab = Resources.Load<GameObject>(Unit1SpumResourceName);
        if (prefab == null)
        {
            return false;
        }

        unit1SpumVisual = Instantiate(prefab, transform);
        unit1SpumVisual.name = "Unit1Visual";
        unit1SpumVisual.transform.localPosition = Vector3.zero;
        unit1SpumVisual.transform.localRotation = Quaternion.identity;
        unit1SpumVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit1SpumPrefab = unit1SpumVisual.GetComponent<SPUM_Prefabs>();
        if (unit1SpumPrefab != null)
        {
            unit1SpumPrefab.OverrideControllerInit();
            unit1SpumPrefab.PopulateAnimationLists();
            unit1SpumAnimator = unit1SpumPrefab._anim;
            if (unit1SpumPrefab.ATTACK_List != null && unit1SpumPrefab.ATTACK_List.Count > 0)
            {
                unit1AttackDuration = Mathf.Max(0.1f, unit1SpumPrefab.ATTACK_List[0].length);
            }
        }

        // SPUM 프리팹의 원래 sortingOrder를 유지

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit1SpumPrefab != null;
    }

    bool SetupUnit3Sprites()
    {
        unit3IdleResourceName = ResolveUnit3ResourceName("Unit_003_idle", "unit_003_idle");
        unit3AttackResourceName = ResolveUnit3ResourceName("Unit_003_attack", "unit_003_attack");
        if (string.IsNullOrEmpty(unit3IdleResourceName))
        {
            Debug.LogWarning("Unit 3 idle sprite not found: Unit_003_idle or unit_003_idle");
            return false;
        }

        if (spriteRenderer == null)
        {
            return false;
        }

        if (unit3SpriteAnimator == null)
        {
            unit3SpriteAnimator = gameObject.AddComponent<UnitSpriteAnimator>();
        }
        unit3SpriteAnimator.Initialize(spriteRenderer, unit3IdleResourceName, Unit3Fps);
        unit3CurrentResourceName = unit3IdleResourceName;
        if (!string.IsNullOrEmpty(unit3AttackResourceName))
        {
            unit3AttackDuration = GetResourceDuration(unit3AttackResourceName, Unit3Fps);
        }
        spriteRenderer.color = Color.white;
        spriteRenderer.flipX = true;
        return true;
    }

    string ResolveUnit3ResourceName(string primary, string fallback)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(primary);
        if (sprites != null && sprites.Length > 0)
        {
            return primary;
        }
        sprites = Resources.LoadAll<Sprite>(fallback);
        if (sprites != null && sprites.Length > 0)
        {
            return fallback;
        }
        return string.Empty;
    }

    float GetResourceDuration(string resourceName, float fps)
    {
        if (string.IsNullOrEmpty(resourceName) || fps <= 0f) return 0f;
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
        if (sprites == null || sprites.Length == 0) return 0f;
        return sprites.Length / fps;
    }

    bool SetupUnit2SpumVisual()
    {
        GameObject prefab = Resources.Load<GameObject>(Unit2SpumResourceName);
        if (prefab == null)
        {
            return false;
        }

        unit2SpumVisual = Instantiate(prefab, transform);
        unit2SpumVisual.name = "Unit2Visual";
        unit2SpumVisual.transform.localPosition = Vector3.zero;
        unit2SpumVisual.transform.localRotation = Quaternion.identity;
        unit2SpumVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit2SpumPrefab = unit2SpumVisual.GetComponent<SPUM_Prefabs>();
        if (unit2SpumPrefab != null)
        {
            unit2SpumPrefab.OverrideControllerInit();
            unit2SpumPrefab.PopulateAnimationLists();
            unit2SpumAnimator = unit2SpumPrefab._anim;
            if (unit2SpumPrefab.ATTACK_List != null && unit2SpumPrefab.ATTACK_List.Count > 0)
            {
                unit2AttackDuration = Mathf.Max(0.1f, unit2SpumPrefab.ATTACK_List[0].length);
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit2SpumPrefab != null;
    }

    bool TryApplyUnit2PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_002");
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Addons/RetroHeroes/2_Prefab/unit_002");
        }
        if (prefab == null)
        {
            return false;
        }

        unit2PrefabVisual = Instantiate(prefab, transform);
        unit2PrefabVisual.name = "Unit2Prefab";
        unit2PrefabVisual.transform.localPosition = Vector3.zero;
        unit2PrefabVisual.transform.localRotation = Quaternion.identity;
        unit2PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit2SpumPrefab = unit2PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit2SpumPrefab == null)
        {
            unit2PrefabAnimator = unit2PrefabVisual.GetComponentInChildren<Animator>(true);
        }
        if (unit2SpumPrefab != null)
        {
            unit2SpumPrefab.OverrideControllerInit();
            unit2SpumPrefab.PopulateAnimationLists();
            unit2SpumAnimator = unit2SpumPrefab._anim;
            if (unit2SpumPrefab.ATTACK_List != null && unit2SpumPrefab.ATTACK_List.Count > 0)
            {
                unit2AttackDuration = Mathf.Max(0.1f, unit2SpumPrefab.ATTACK_List[0].length);
            }
            unit2SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit2PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        SpriteRenderer[] renderers = unit2PrefabVisual.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer childRenderer in renderers)
        {
            if (childRenderer == null) continue;
            string name = childRenderer.gameObject.name.ToLowerInvariant();
            string spriteName = childRenderer.sprite != null ? childRenderer.sprite.name.ToLowerInvariant() : string.Empty;
            if (name.Contains("shadow") || spriteName.Contains("shadow") || spriteName.Contains("unit_tile"))
            {
                childRenderer.enabled = false;
            }
        }

        unit2PrefabAttackClip = FindUnit2AttackClip(unit2PrefabAnimator);

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return true;
    }

    bool TryApplyUnit7PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_007");
        if (prefab == null)
        {
            return false;
        }

        unit7PrefabVisual = Instantiate(prefab, transform);
        unit7PrefabVisual.name = "Unit7Prefab";
        unit7PrefabVisual.transform.localPosition = Vector3.zero;
        unit7PrefabVisual.transform.localRotation = Quaternion.identity;
        unit7PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit7SpumPrefab = unit7PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit7SpumPrefab != null)
        {
            unit7SpumPrefab.OverrideControllerInit();
            unit7SpumPrefab.PopulateAnimationLists();
            unit7SpumAnimator = unit7SpumPrefab._anim;
            if (unit7SpumPrefab.ATTACK_List != null && unit7SpumPrefab.ATTACK_List.Count > 0)
            {
                unit7SpumAttackDuration = Mathf.Max(0.1f, unit7SpumPrefab.ATTACK_List[0].length);
            }
            unit7SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit7PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit7SpumPrefab != null;
    }

    bool TryApplyUnit15PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_015");
        if (prefab == null)
        {
            return false;
        }

        unit15PrefabVisual = Instantiate(prefab, transform);
        unit15PrefabVisual.name = "Unit15Prefab";
        unit15PrefabVisual.transform.localPosition = Vector3.zero;
        unit15PrefabVisual.transform.localRotation = Quaternion.identity;
        unit15PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit15SpumPrefab = unit15PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit15SpumPrefab != null)
        {
            unit15SpumPrefab.OverrideControllerInit();
            unit15SpumPrefab.PopulateAnimationLists();
            unit15SpumAnimator = unit15SpumPrefab._anim;
            if (unit15SpumPrefab.ATTACK_List != null && unit15SpumPrefab.ATTACK_List.Count > 0)
            {
                unit15SpumAttackDuration = Mathf.Max(0.1f, unit15SpumPrefab.ATTACK_List[0].length);
            }
            unit15SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit15PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit15SpumPrefab != null;
    }

    bool TryApplyUnit19PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_019");
        if (prefab == null)
        {
            return false;
        }

        unit19PrefabVisual = Instantiate(prefab, transform);
        unit19PrefabVisual.name = "Unit19Prefab";
        unit19PrefabVisual.transform.localPosition = Vector3.zero;
        unit19PrefabVisual.transform.localRotation = Quaternion.identity;
        unit19PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit19SpumPrefab = unit19PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit19SpumPrefab != null)
        {
            unit19SpumPrefab.OverrideControllerInit();
            unit19SpumPrefab.PopulateAnimationLists();
            unit19SpumAnimator = unit19SpumPrefab._anim;
            if (unit19SpumPrefab.ATTACK_List != null && unit19SpumPrefab.ATTACK_List.Count > 0)
            {
                unit19SpumAttackDuration = Mathf.Max(0.1f, unit19SpumPrefab.ATTACK_List[0].length);
            }
            unit19SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit19PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit19SpumPrefab != null;
    }

    bool TryApplyUnit13PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_013");
        if (prefab == null)
        {
            return false;
        }

        unit13PrefabVisual = Instantiate(prefab, transform);
        unit13PrefabVisual.name = "Unit13Prefab";
        unit13PrefabVisual.transform.localPosition = Vector3.zero;
        unit13PrefabVisual.transform.localRotation = Quaternion.identity;
        unit13PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit13SpumPrefab = unit13PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit13SpumPrefab != null)
        {
            unit13SpumPrefab.OverrideControllerInit();
            unit13SpumPrefab.PopulateAnimationLists();
            unit13SpumAnimator = unit13SpumPrefab._anim;
            if (unit13SpumPrefab.ATTACK_List != null && unit13SpumPrefab.ATTACK_List.Count > 0)
            {
                unit13SpumAttackDuration = Mathf.Max(0.1f, unit13SpumPrefab.ATTACK_List[0].length);
            }
            unit13SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit13PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit13SpumPrefab != null;
    }

    bool TryApplyUnit14PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_014");
        if (prefab == null)
        {
            return false;
        }

        unit14PrefabVisual = Instantiate(prefab, transform);
        unit14PrefabVisual.name = "Unit14Prefab";
        unit14PrefabVisual.transform.localPosition = Vector3.zero;
        unit14PrefabVisual.transform.localRotation = Quaternion.identity;
        unit14PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit14SpumPrefab = unit14PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit14SpumPrefab != null)
        {
            unit14SpumPrefab.OverrideControllerInit();
            unit14SpumPrefab.PopulateAnimationLists();
            unit14SpumAnimator = unit14SpumPrefab._anim;
            if (unit14SpumPrefab.ATTACK_List != null && unit14SpumPrefab.ATTACK_List.Count > 0)
            {
                unit14SpumAttackDuration = Mathf.Max(0.1f, unit14SpumPrefab.ATTACK_List[0].length);
            }
            unit14SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit14PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit14SpumPrefab != null;
    }

    bool TryApplyUnit23PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_023");
        if (prefab == null)
        {
            return false;
        }

        unit23PrefabVisual = Instantiate(prefab, transform);
        unit23PrefabVisual.name = "Unit23Prefab";
        unit23PrefabVisual.transform.localPosition = Vector3.zero;
        unit23PrefabVisual.transform.localRotation = Quaternion.identity;
        unit23PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit23SpumPrefab = unit23PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit23SpumPrefab != null)
        {
            unit23SpumPrefab.OverrideControllerInit();
            unit23SpumPrefab.PopulateAnimationLists();
            unit23SpumAnimator = unit23SpumPrefab._anim;
            if (unit23SpumPrefab.ATTACK_List != null && unit23SpumPrefab.ATTACK_List.Count > 0)
            {
                unit23SpumAttackDuration = Mathf.Max(0.1f, unit23SpumPrefab.ATTACK_List[0].length);
            }
            unit23SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit23PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit23SpumPrefab != null;
    }

    bool TryApplyUnit25PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_025");
        if (prefab == null)
        {
            return false;
        }

        unit25PrefabVisual = Instantiate(prefab, transform);
        unit25PrefabVisual.name = "Unit25Prefab";
        unit25PrefabVisual.transform.localPosition = Vector3.zero;
        unit25PrefabVisual.transform.localRotation = Quaternion.identity;
        unit25PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit25SpumPrefab = unit25PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit25SpumPrefab != null)
        {
            unit25SpumPrefab.OverrideControllerInit();
            unit25SpumPrefab.PopulateAnimationLists();
            unit25SpumAnimator = unit25SpumPrefab._anim;
            if (unit25SpumPrefab.ATTACK_List != null && unit25SpumPrefab.ATTACK_List.Count > 0)
            {
                unit25SpumAttackDuration = Mathf.Max(0.1f, unit25SpumPrefab.ATTACK_List[0].length);
            }
            unit25SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit25PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit25SpumPrefab != null;
    }

    bool TryApplyUnit28PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_028");
        if (prefab == null)
        {
            return false;
        }

        unit28PrefabVisual = Instantiate(prefab, transform);
        unit28PrefabVisual.name = "Unit28Prefab";
        unit28PrefabVisual.transform.localPosition = Vector3.zero;
        unit28PrefabVisual.transform.localRotation = Quaternion.identity;
        unit28PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit28SpumPrefab = unit28PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit28SpumPrefab != null)
        {
            unit28SpumPrefab.OverrideControllerInit();
            unit28SpumPrefab.PopulateAnimationLists();
            unit28SpumAnimator = unit28SpumPrefab._anim;
            if (unit28SpumPrefab.ATTACK_List != null && unit28SpumPrefab.ATTACK_List.Count > 0)
            {
                unit28SpumAttackDuration = Mathf.Max(0.1f, unit28SpumPrefab.ATTACK_List[0].length);
            }
            unit28SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit28PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit28SpumPrefab != null;
    }

    bool TryInstantiateSpumUnitFromResources(
        string resourcesPath,
        string childObjectName,
        out GameObject visual,
        out SPUM_Prefabs spum,
        out Animator anim,
        out float attackDuration)
    {
        visual = null;
        spum = null;
        anim = null;
        attackDuration = 0.6f;
        GameObject prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab == null)
        {
            return false;
        }

        visual = Instantiate(prefab, transform);
        visual.name = childObjectName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(-1f, 1f, 1f);

        spum = visual.GetComponent<SPUM_Prefabs>();
        if (spum == null)
        {
            return false;
        }
        spum.OverrideControllerInit();
        spum.PopulateAnimationLists();
        anim = spum._anim;
        if (spum.ATTACK_List != null && spum.ATTACK_List.Count > 0)
        {
            attackDuration = Mathf.Max(0.1f, spum.ATTACK_List[0].length);
        }
        SortingGroup sortingGroup = visual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        return true;
    }

    bool TryApplyUnit4PrefabVisual()
    {
        bool ok = TryInstantiateSpumUnitFromResources("Units/unit_004", "Unit4Prefab", out unit4PrefabVisual, out unit4SpumPrefab, out unit4SpumAnimator, out unit4SpumAttackDuration);
        if (ok) unit4SpumState = PlayerState.IDLE;
        return ok;
    }

    bool TryApplyUnit8PrefabVisual()
    {
        bool ok = TryInstantiateSpumUnitFromResources("Units/unit_008", "Unit8Prefab", out unit8PrefabVisual, out unit8SpumPrefab, out unit8SpumAnimator, out unit8SpumAttackDuration);
        if (ok) unit8SpumState = PlayerState.IDLE;
        return ok;
    }

    bool TryApplyUnit10PrefabVisual()
    {
        bool ok = TryInstantiateSpumUnitFromResources("Units/unit_010", "Unit10Prefab", out unit10PrefabVisual, out unit10SpumPrefab, out unit10SpumAnimator, out unit10SpumAttackDuration);
        if (ok) unit10SpumState = PlayerState.IDLE;
        return ok;
    }

    bool TryApplyUnit12PrefabVisual()
    {
        bool ok = TryInstantiateSpumUnitFromResources("Units/unit_012", "Unit12Prefab", out unit12PrefabVisual, out unit12SpumPrefab, out unit12SpumAnimator, out unit12SpumAttackDuration);
        if (ok) unit12SpumState = PlayerState.IDLE;
        return ok;
    }

    bool TryApplyUnit21PrefabVisual()
    {
        bool ok = TryInstantiateSpumUnitFromResources("Units/unit_021", "Unit21Prefab", out unit21PrefabVisual, out unit21SpumPrefab, out unit21SpumAnimator, out unit21SpumAttackDuration);
        if (ok) unit21SpumState = PlayerState.IDLE;
        return ok;
    }

    bool TryApplyUnit11PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_011");
        if (prefab == null)
        {
            return false;
        }

        unit11PrefabVisual = Instantiate(prefab, transform);
        unit11PrefabVisual.name = "Unit11Prefab";
        unit11PrefabVisual.transform.localPosition = Vector3.zero;
        unit11PrefabVisual.transform.localRotation = Quaternion.identity;
        unit11PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit11SpumPrefab = unit11PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit11SpumPrefab != null)
        {
            unit11SpumPrefab.OverrideControllerInit();
            unit11SpumPrefab.PopulateAnimationLists();
            unit11SpumAnimator = unit11SpumPrefab._anim;
            if (unit11SpumPrefab.ATTACK_List != null && unit11SpumPrefab.ATTACK_List.Count > 0)
            {
                unit11SpumAttackDuration = Mathf.Max(0.1f, unit11SpumPrefab.ATTACK_List[0].length);
            }
            unit11SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit11PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit11SpumPrefab != null;
    }

    bool TryApplyUnit20PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_020");
        if (prefab == null)
        {
            return false;
        }

        unit20PrefabVisual = Instantiate(prefab, transform);
        unit20PrefabVisual.name = "Unit20Prefab";
        unit20PrefabVisual.transform.localPosition = Vector3.zero;
        unit20PrefabVisual.transform.localRotation = Quaternion.identity;
        unit20PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit20SpumPrefab = unit20PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit20SpumPrefab != null)
        {
            unit20SpumPrefab.OverrideControllerInit();
            unit20SpumPrefab.PopulateAnimationLists();
            unit20SpumAnimator = unit20SpumPrefab._anim;
            if (unit20SpumPrefab.ATTACK_List != null && unit20SpumPrefab.ATTACK_List.Count > 0)
            {
                unit20SpumAttackDuration = Mathf.Max(0.1f, unit20SpumPrefab.ATTACK_List[0].length);
            }
            unit20SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit20PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit20SpumPrefab != null;
    }

    bool TryApplyUnit22PrefabVisual()
    {
        GameObject prefab = Resources.Load<GameObject>("Units/unit_022");
        if (prefab == null)
        {
            return false;
        }

        unit22PrefabVisual = Instantiate(prefab, transform);
        unit22PrefabVisual.name = "Unit22Prefab";
        unit22PrefabVisual.transform.localPosition = Vector3.zero;
        unit22PrefabVisual.transform.localRotation = Quaternion.identity;
        unit22PrefabVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit22SpumPrefab = unit22PrefabVisual.GetComponent<SPUM_Prefabs>();
        if (unit22SpumPrefab != null)
        {
            unit22SpumPrefab.OverrideControllerInit();
            unit22SpumPrefab.PopulateAnimationLists();
            unit22SpumAnimator = unit22SpumPrefab._anim;
            if (unit22SpumPrefab.ATTACK_List != null && unit22SpumPrefab.ATTACK_List.Count > 0)
            {
                unit22SpumAttackDuration = Mathf.Max(0.1f, unit22SpumPrefab.ATTACK_List[0].length);
            }
            unit22SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit22PrefabVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit22SpumPrefab != null;
    }

    bool SetupUnit5SpumVisual()
    {
        GameObject prefab = Resources.Load<GameObject>(Unit5SpumResourceName);
        if (prefab == null)
        {
            return false;
        }

        unit5SpumVisual = Instantiate(prefab, transform);
        unit5SpumVisual.name = "Unit5Visual";
        unit5SpumVisual.transform.localPosition = Vector3.zero;
        unit5SpumVisual.transform.localRotation = Quaternion.identity;
        unit5SpumVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit5SpumPrefab = unit5SpumVisual.GetComponent<SPUM_Prefabs>();
        if (unit5SpumPrefab != null)
        {
            unit5SpumPrefab.OverrideControllerInit();
            unit5SpumPrefab.PopulateAnimationLists();
            unit5SpumAnimator = unit5SpumPrefab._anim;
            if (unit5SpumPrefab.ATTACK_List != null && unit5SpumPrefab.ATTACK_List.Count > 0)
            {
                unit5AttackDuration = Mathf.Max(0.1f, unit5SpumPrefab.ATTACK_List[0].length);
            }
        }

        // SPUM 프리팹의 원래 sortingOrder를 유지

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit5SpumPrefab != null;
    }

    bool SetupUnit6SpumVisual()
    {
        GameObject prefab = Resources.Load<GameObject>(Unit6SpumResourceName);
        if (prefab == null)
        {
            return false;
        }

        unit6SpumVisual = Instantiate(prefab, transform);
        unit6SpumVisual.name = "Unit6Visual";
        unit6SpumVisual.transform.localPosition = Vector3.zero;
        unit6SpumVisual.transform.localRotation = Quaternion.identity;
        unit6SpumVisual.transform.localScale = new Vector3(-1f, 1f, 1f);

        unit6SpumPrefab = unit6SpumVisual.GetComponent<SPUM_Prefabs>();
        if (unit6SpumPrefab != null)
        {
            unit6SpumPrefab.OverrideControllerInit();
            unit6SpumPrefab.PopulateAnimationLists();
            unit6SpumAnimator = unit6SpumPrefab._anim;
            if (unit6SpumPrefab.ATTACK_List != null && unit6SpumPrefab.ATTACK_List.Count > 0)
            {
                unit6AttackDuration = Mathf.Max(0.1f, unit6SpumPrefab.ATTACK_List[0].length);
            }
            unit6SpumState = PlayerState.IDLE;
        }

        SortingGroup sortingGroup = unit6SpumVisual.GetComponentInChildren<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortAtRoot = true;
            if (spriteRenderer != null)
            {
                sortingGroup.sortingOrder = spriteRenderer.sortingOrder;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        return unit6SpumPrefab != null;
    }

    void UpdateUnit5SpumState(bool isProperlyPlaced)
    {
        if (unit5SpumPrefab == null || unit5SpumAnimator == null)
        {
            return;
        }

        bool canAnimate = isProperlyPlaced;
        unit5SpumAnimator.speed = canAnimate ? 1f : 0f;

        if (canAnimate && unit5SpumState != PlayerState.ATTACK)
        {
            PlayUnit5IdleAnimation();
        }
    }

    void UpdateUnit2SpumState(bool isProperlyPlaced)
    {
        if (unit2SpumPrefab == null || unit2SpumAnimator == null)
        {
            return;
        }

        bool canAnimate = isProperlyPlaced;
        unit2SpumAnimator.speed = canAnimate ? 1f : 0f;

        if (canAnimate && unit2SpumState != PlayerState.ATTACK)
        {
            PlayUnit2IdleAnimation();
        }
    }

    void UpdateUnit3State(bool isProperlyPlaced)
    {
        if (unit3SpriteAnimator == null || string.IsNullOrEmpty(unit3IdleResourceName))
        {
            return;
        }

        bool canAnimate = isProperlyPlaced;
        unit3SpriteAnimator.SetAnimating(canAnimate);
        if (canAnimate && unit3AttackRoutine == null && unit3CurrentResourceName != unit3IdleResourceName)
        {
            unit3SpriteAnimator.Initialize(spriteRenderer, unit3IdleResourceName, Unit3Fps);
            unit3CurrentResourceName = unit3IdleResourceName;
        }
    }

    void UpdateUnit3SpumState(bool canAnimate)
    {
        if (unit3SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit3SpumState != PlayerState.IDLE)
        {
            unit3SpumState = PlayerState.IDLE;
            unit3SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit18SpumState(bool canAnimate)
    {
        if (unit18SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit18SpumState != PlayerState.IDLE)
        {
            unit18SpumState = PlayerState.IDLE;
            unit18SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit24SpumState(bool canAnimate)
    {
        if (unit24SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit24SpumState != PlayerState.IDLE)
        {
            unit24SpumState = PlayerState.IDLE;
            unit24SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void PlayUnit3AttackAnimation()
    {
        if (usesUnit3Prefab && unit3SpumPrefab != null)
        {
            unit3SpumState = PlayerState.ATTACK;
            unit3SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
            if (unit3SpumAnimator != null)
            {
                unit3SpumAnimator.speed = 1f;
            }
            return;
        }

        if (unit3SpriteAnimator == null || string.IsNullOrEmpty(unit3AttackResourceName))
        {
            return;
        }

        if (unit3AttackRoutine != null)
        {
            StopCoroutine(unit3AttackRoutine);
        }
        unit3AttackRoutine = StartCoroutine(Unit3AttackRoutine());
    }

    System.Collections.IEnumerator Unit3AttackRoutine()
    {
        if (unit3CurrentResourceName != unit3AttackResourceName)
        {
            unit3SpriteAnimator.Initialize(spriteRenderer, unit3AttackResourceName, Unit3Fps);
            unit3CurrentResourceName = unit3AttackResourceName;
        }
        unit3SpriteAnimator.SetAnimating(true);
        yield return new WaitForSeconds(Mathf.Max(0.05f, unit3AttackDuration));
        if (IsProperlyPlaced() && unit3CurrentResourceName != unit3IdleResourceName)
        {
            unit3SpriteAnimator.Initialize(spriteRenderer, unit3IdleResourceName, Unit3Fps);
            unit3CurrentResourceName = unit3IdleResourceName;
        }
        unit3AttackRoutine = null;
    }

    void PlayUnit2IdleAnimation()
    {
        if (unit2SpumPrefab == null) return;
        if (unit2SpumState == PlayerState.IDLE) return;
        if (!HasSpumAnimation(unit2SpumPrefab, PlayerState.IDLE))
        {
            return;
        }

        unit2SpumState = PlayerState.IDLE;
        unit2SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
    }

    void PlayUnit2AttackAnimation()
    {
        if (unit2SpumPrefab == null && unit2PrefabAnimator == null) return;

        if (unit2AttackRoutine != null)
        {
            StopCoroutine(unit2AttackRoutine);
        }
        unit2AttackRoutine = StartCoroutine(Unit2AttackRoutine());
    }

    System.Collections.IEnumerator Unit2AttackRoutine()
    {
        if (unit2SpumPrefab == null && unit2PrefabAnimator != null)
        {
            TriggerUnit2PrefabAttack();
            yield return new WaitForSeconds(Mathf.Max(0.1f, unit2AttackDuration));
            unit2AttackRoutine = null;
            yield break;
        }

        if (unit2SpumPrefab == null) yield break;
        if (!HasSpumAnimation(unit2SpumPrefab, PlayerState.ATTACK))
        {
            if (unit2PrefabAnimator != null)
            {
                TriggerUnit2PrefabAttack();
                yield return new WaitForSeconds(Mathf.Max(0.1f, unit2AttackDuration));
            }
            unit2AttackRoutine = null;
            yield break;
        }

        unit2SpumState = PlayerState.ATTACK;
        unit2SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);

        yield return new WaitForSeconds(unit2AttackDuration);

        if (IsProperlyPlaced())
        {
            PlayUnit2IdleAnimation();
        }
        unit2AttackRoutine = null;
    }

    void TriggerUnit2PrefabAttack()
    {
        if (unit2PrefabAnimator == null) return;
        unit2PrefabAnimator.Rebind();
        unit2PrefabAnimator.Update(0f);
        if (unit2PrefabAttackClip != null)
        {
            unit2PrefabAnimator.Play(unit2PrefabAttackClip.name, 0, 0f);
            return;
        }

        AnimatorControllerParameter[] parameters = unit2PrefabAnimator.parameters;
        foreach (AnimatorControllerParameter parameter in parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                parameter.name.ToUpper().Contains("ATTACK"))
            {
                unit2PrefabAnimator.SetTrigger(parameter.name);
                return;
            }
        }
        unit2PrefabAnimator.SetTrigger("ATTACK");
    }

    bool HasSpumAnimation(SPUM_Prefabs prefab, PlayerState state)
    {
        if (prefab == null) return false;
        if (prefab.StateAnimationPairs != null &&
            prefab.StateAnimationPairs.TryGetValue(state.ToString(), out var list) &&
            list != null && list.Count > 0)
        {
            return true;
        }

        switch (state)
        {
            case PlayerState.IDLE:
                return prefab.IDLE_List != null && prefab.IDLE_List.Count > 0;
            case PlayerState.ATTACK:
                return prefab.ATTACK_List != null && prefab.ATTACK_List.Count > 0;
            default:
                return false;
        }
    }

    AnimationClip FindUnit2AttackClip(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return null;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0) return null;

        foreach (AnimationClip clip in clips)
        {
            if (clip == null) continue;
            string name = clip.name.ToLowerInvariant();
            if (name.Contains("attack"))
            {
                return clip;
            }
        }

        return clips[0];
    }

    void UpdateUnit7SpumState(bool canAnimate)
    {
        if (unit7SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit7SpumState != PlayerState.IDLE && HasSpumAnimation(unit7SpumPrefab, PlayerState.IDLE))
        {
            unit7SpumState = PlayerState.IDLE;
            unit7SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit15SpumState(bool canAnimate)
    {
        if (unit15SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit15SpumState != PlayerState.IDLE && HasSpumAnimation(unit15SpumPrefab, PlayerState.IDLE))
        {
            unit15SpumState = PlayerState.IDLE;
            unit15SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit19SpumState(bool canAnimate)
    {
        if (unit19SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit19SpumState != PlayerState.IDLE && HasSpumAnimation(unit19SpumPrefab, PlayerState.IDLE))
        {
            unit19SpumState = PlayerState.IDLE;
            unit19SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit13SpumState(bool canAnimate)
    {
        if (unit13SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit13SpumState != PlayerState.IDLE && HasSpumAnimation(unit13SpumPrefab, PlayerState.IDLE))
        {
            unit13SpumState = PlayerState.IDLE;
            unit13SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit14SpumState(bool canAnimate)
    {
        if (unit14SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit14SpumState != PlayerState.IDLE && HasSpumAnimation(unit14SpumPrefab, PlayerState.IDLE))
        {
            unit14SpumState = PlayerState.IDLE;
            unit14SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit23SpumState(bool canAnimate)
    {
        if (unit23SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit23SpumState != PlayerState.IDLE && HasSpumAnimation(unit23SpumPrefab, PlayerState.IDLE))
        {
            unit23SpumState = PlayerState.IDLE;
            unit23SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit25SpumState(bool canAnimate)
    {
        if (unit25SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit25SpumState != PlayerState.IDLE && HasSpumAnimation(unit25SpumPrefab, PlayerState.IDLE))
        {
            unit25SpumState = PlayerState.IDLE;
            unit25SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit28SpumState(bool canAnimate)
    {
        if (unit28SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit28SpumState != PlayerState.IDLE && HasSpumAnimation(unit28SpumPrefab, PlayerState.IDLE))
        {
            unit28SpumState = PlayerState.IDLE;
            unit28SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateSpumIdleState(ref PlayerState state, SPUM_Prefabs spum, bool canAnimate)
    {
        if (spum == null) return;
        if (!canAnimate) return;

        if (state != PlayerState.IDLE && HasSpumAnimation(spum, PlayerState.IDLE))
        {
            state = PlayerState.IDLE;
            spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit11SpumState(bool canAnimate)
    {
        if (unit11SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit11SpumState != PlayerState.IDLE && HasSpumAnimation(unit11SpumPrefab, PlayerState.IDLE))
        {
            unit11SpumState = PlayerState.IDLE;
            unit11SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit20SpumState(bool canAnimate)
    {
        if (unit20SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit20SpumState != PlayerState.IDLE && HasSpumAnimation(unit20SpumPrefab, PlayerState.IDLE))
        {
            unit20SpumState = PlayerState.IDLE;
            unit20SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void UpdateUnit22SpumState(bool canAnimate)
    {
        if (unit22SpumPrefab == null) return;
        if (!canAnimate) return;

        if (unit22SpumState != PlayerState.IDLE && HasSpumAnimation(unit22SpumPrefab, PlayerState.IDLE))
        {
            unit22SpumState = PlayerState.IDLE;
            unit22SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    void PlayUnit7AttackAnimation()
    {
        if (unit7SpumPrefab == null) return;
        if (!HasSpumAnimation(unit7SpumPrefab, PlayerState.ATTACK)) return;

        unit7SpumState = PlayerState.ATTACK;
        unit7SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
        if (unit7SpumAnimator != null)
        {
            unit7SpumAnimator.speed = 1f;
        }
    }

    void PlayUnit5IdleAnimation()
    {
        if (unit5SpumPrefab == null) return;
        if (unit5SpumState == PlayerState.IDLE) return;

        unit5SpumState = PlayerState.IDLE;
        unit5SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
    }

    void UpdateUnit6SpumState(bool isProperlyPlaced)
    {
        if (unit6SpumPrefab == null || unit6SpumAnimator == null)
        {
            return;
        }

        bool canAnimate = isProperlyPlaced;
        unit6SpumAnimator.speed = canAnimate ? 1f : 0f;

        if (canAnimate && unit6SpumState != PlayerState.ATTACK)
        {
            PlayUnit6IdleAnimation();
        }
    }

    void PlayUnit6IdleAnimation()
    {
        if (unit6SpumPrefab == null) return;
        if (unit6SpumState == PlayerState.IDLE) return;

        unit6SpumState = PlayerState.IDLE;
        unit6SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
    }

    void PlayUnit6AttackAnimation()
    {
        if (unit6SpumPrefab == null) return;

        if (unit6AttackRoutine != null)
        {
            StopCoroutine(unit6AttackRoutine);
        }
        unit6AttackRoutine = StartCoroutine(Unit6AttackRoutine());
    }

    System.Collections.IEnumerator Unit6AttackRoutine()
    {
        unit6SpumState = PlayerState.ATTACK;
        unit6SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);

        yield return new WaitForSeconds(unit6AttackDuration);

        if (IsProperlyPlaced())
        {
            PlayUnit6IdleAnimation();
        }
        unit6AttackRoutine = null;
    }
    void PlayUnit5AttackAnimation()
    {
        if (unit5AttackRoutine != null)
        {
            StopCoroutine(unit5AttackRoutine);
        }
        unit5AttackRoutine = StartCoroutine(Unit5AttackRoutine());
    }

    System.Collections.IEnumerator Unit5AttackRoutine()
    {
        if (!IsProperlyPlaced())
        {
            unit5PendingStrikeTarget = null;
            unit5AttackRoutine = null;
            yield break;
        }

        Sprite[] skillReadyFrames = LoadSpriteFrames(Unit5SkillReadyResourceName);
        float skillReadyDuration = skillReadyFrames.Length > 0
            ? skillReadyFrames.Length / Unit5SkillReadyFps
            : 0f;

        if (unit5SpumPrefab != null)
        {
            unit5SpumState = PlayerState.ATTACK;
            unit5SpumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
        }

        if (unit5PendingStrikeTarget != null)
        {
            FireLightningStrike(unit5PendingStrikeTarget);
            unit5PendingStrikeTarget = null;
        }

        if (skillReadyFrames.Length > 0)
        {
            StartCoroutine(PlayUnit5SkillReadyOnce(skillReadyFrames));
        }

        float waitDuration = unit5SpumPrefab != null
            ? Mathf.Max(unit5AttackDuration, skillReadyDuration)
            : skillReadyDuration;
        if (waitDuration > 0f)
        {
            yield return new WaitForSeconds(waitDuration);
        }

        if (unit5SpumPrefab != null && IsProperlyPlaced())
        {
            PlayUnit5IdleAnimation();
        }

        unit5AttackRoutine = null;
    }

    Vector3 GetUnit5TorsoLocalOffset(Transform parent)
    {
        if (parent == null)
        {
            return new Vector3(0f, 0.2f, 0f);
        }

        SpriteRenderer[] renderers = parent.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return new Vector3(0f, 0.2f, 0f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        Vector3 torsoWorld = bounds.center + new Vector3(0f, bounds.extents.y * 0.15f, 0f);
        return parent.InverseTransformPoint(torsoWorld);
    }

    void ApplyUnit5SkillReadySorting(SpriteRenderer overlayRenderer, Transform parent)
    {
        if (overlayRenderer == null || parent == null)
        {
            return;
        }

        int maxOrder = int.MinValue;
        string layerName = "Default";
        SpriteRenderer[] renderers = parent.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr == overlayRenderer) continue;
            if (sr.sortingOrder > maxOrder)
            {
                maxOrder = sr.sortingOrder;
                layerName = sr.sortingLayerName;
            }
        }

        if (maxOrder == int.MinValue && spriteRenderer != null)
        {
            maxOrder = spriteRenderer.sortingOrder;
            layerName = spriteRenderer.sortingLayerName;
        }

        overlayRenderer.sortingLayerName = layerName;
        overlayRenderer.sortingOrder = maxOrder + 15;
    }

    System.Collections.IEnumerator PlayUnit5SkillReadyOnce(Sprite[] frames)
    {
        Transform parent = unit5SpumVisual != null ? unit5SpumVisual.transform : transform;
        if (parent == null || frames == null || frames.Length == 0) yield break;

        GameObject overlay = new GameObject("Unit5SkillReady");
        overlay.transform.SetParent(parent, false);
        overlay.transform.localPosition = GetUnit5TorsoLocalOffset(parent);
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one;

        SpriteRenderer overlayRenderer = overlay.AddComponent<SpriteRenderer>();
        overlayRenderer.color = Unit5SkillReadyColor;
        ApplyUnit5SkillReadySorting(overlayRenderer, parent);

        float frameTime = 1f / Unit5SkillReadyFps;
        for (int i = 0; i < frames.Length; i++)
        {
            if (overlay == null || overlayRenderer == null) yield break;
            overlayRenderer.sprite = frames[i];
            overlayRenderer.color = Unit5SkillReadyColor;
            yield return new WaitForSeconds(frameTime);
        }

        if (overlay != null)
        {
            Destroy(overlay);
        }
    }

    void UpdateUnit1SpumState(bool isProperlyPlaced)
    {
        if (unit1SpumPrefab == null || unit1SpumAnimator == null)
        {
            return;
        }

        bool canAnimate = isProperlyPlaced;
        unit1SpumAnimator.speed = canAnimate ? 1f : 0f;

        if (canAnimate && unit1SpumState != PlayerState.ATTACK)
        {
            PlayUnit1IdleAnimation();
        }
    }

    void PlayUnit1IdleAnimation()
    {
        if (unit1SpumPrefab == null) return;
        if (unit1SpumState == PlayerState.IDLE) return;

        unit1SpumState = PlayerState.IDLE;
        unit1SpumPrefab.PlayAnimation(PlayerState.IDLE, 0);
    }

    void PlayUnit1AttackAnimation()
    {
        if (unit1SpumPrefab == null) return;

        if (unit1AttackRoutine != null)
        {
            StopCoroutine(unit1AttackRoutine);
        }
        unit1AttackRoutine = StartCoroutine(Unit1AttackRoutine());
    }

    System.Collections.IEnumerator Unit1AttackRoutine()
    {
        unit1SpumState = PlayerState.ATTACK;
        unit1SpumPrefab.PlayAnimation(PlayerState.ATTACK, Unit1SpumAttackIndex);

        yield return new WaitForSeconds(unit1AttackDuration);

        if (IsProperlyPlaced())
        {
            PlayUnit1IdleAnimation();
        }
        unit1AttackRoutine = null;
    }
    
    /// <summary>
    /// 가장 가까운 좀비를 찾습니다
    /// </summary>
    Zombie ResolvePrimaryAttackTarget()
    {
        if (unitNumber == 5)
        {
            return FindRandomZombie();
        }

        if (unitNumber == 28 || unitNumber == 21)
        {
            return FindNearestZombieInView();
        }

        return FindNearestZombie();
    }

    Zombie FindNearestZombie()
    {
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        FilterZombiesByAttackRange();
        Zombie nearestZombie = null;
        float nearestDistance = float.MaxValue;
        Vector3 p = transform.position;
        for (int i = 0; i < _zombieSearchBuffer.Count; i++)
        {
            Zombie zombie = _zombieSearchBuffer[i];
            float distance = Vector2.Distance(p, zombie.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestZombie = zombie;
            }
        }

        return nearestZombie;
    }

    /// <summary>메인 카메라(오쏘) 시야 내만 true — 유닛 28 등</summary>
    static bool IsPositionInMainCameraView(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Bounds b = new Bounds(cam.transform.position, new Vector3(width, height, 0f));
        return worldPos.x >= b.min.x && worldPos.x <= b.max.x
            && worldPos.y >= b.min.y && worldPos.y <= b.max.y;
    }

    /// <summary>시야 내 좀비 중 가장 가까운 한 마리(없으면 null)</summary>
    Zombie FindNearestZombieInView()
    {
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        FilterZombiesByAttackRange();
        Zombie best = null;
        float dMin = float.MaxValue;
        Vector3 p = transform.position;
        for (int i = 0; i < _zombieSearchBuffer.Count; i++)
        {
            Zombie z = _zombieSearchBuffer[i];
            if (!IsPositionInMainCameraView(z.transform.position)) continue;
            float d = Vector2.Distance(p, z.transform.position);
            if (d < dMin)
            {
                dMin = d;
                best = z;
            }
        }
        return best;
    }
    
    /// <summary>
    /// 랜덤한 좀비를 찾습니다
    /// </summary>
    Zombie FindRandomZombie()
    {
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        FilterZombiesByAttackRange();
        if (_zombieSearchBuffer.Count == 0) return null;
        int idx = Random.Range(0, _zombieSearchBuffer.Count);
        return _zombieSearchBuffer[idx];
    }
    
    void FireUnit1Projectile(Zombie target)
    {
        if (target == null) return;

        Vector2 dir = (Vector2)(target.transform.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        else dir.Normalize();

        int primaryDamage = GetDamageWithUpgrade(3);
        SpawnUnit1DirectionalShot(dir, primaryDamage);

        if (UnitEvolutionMilestones.HasUnit1Afterimage(evolutionLevel))
        {
            int afterimageDamage = Mathf.Max(1, Mathf.RoundToInt(primaryDamage * UnitEvolutionMilestones.Unit1AfterimageDamageMul));
            StartCoroutine(SpawnUnit1DelayedShot(dir, afterimageDamage, UnitEvolutionMilestones.Unit1AfterimageDelay));
        }

        if (UnitEvolutionMilestones.HasUnit1SideShots(evolutionLevel))
        {
            int sideDamage = Mathf.Max(1, Mathf.RoundToInt(primaryDamage * UnitEvolutionMilestones.Unit1SideDamageMul));
            Vector2 sideDir = RotateDirection(dir, UnitEvolutionMilestones.Unit1SideAngleDeg);
            SpawnUnit1DirectionalShot(sideDir, sideDamage);
            SpawnUnit1DirectionalShot(RotateDirection(dir, -UnitEvolutionMilestones.Unit1SideAngleDeg), sideDamage);
        }
    }

    System.Collections.IEnumerator SpawnUnit1DelayedShot(Vector2 direction, int damage, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        SpawnUnit1DirectionalShot(direction, damage);
    }

    static Vector2 RotateDirection(Vector2 dir, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos).normalized;
    }

    void SpawnUnit1DirectionalShot(Vector2 direction, int damage)
    {
        GameObject projObj = new GameObject("Unit1Shot");
        projObj.transform.position = transform.position;

        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;
        Sprite attackSprite = Resources.Load<Sprite>("unit_1_attack");
        if (attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sprite = CreateSquareSprite(Color.yellow);
        }
        projObj.transform.localScale = GetScaledProjectileSize(Vector3.one * 0.3f);
        projObj.transform.rotation = Quaternion.FromToRotation(Vector2.right, direction);

        BoxCollider2D collider = projObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one * 0.3f;

        StraightProjectile proj = projObj.AddComponent<StraightProjectile>();
        proj.speed = projectileSpeed;
        proj.direction = direction;
        proj.damage = damage;
        proj.sourceUnitNumber = unitNumber;
        proj.hitEffectScale = GetProjectileScale() * 2f;
    }

    /// <summary>
    /// 총알을 발사합니다
    /// </summary>
    void FireProjectile(Zombie target)
    {
        if (unitNumber == 2)
        {
            FireUnit2Fireball(target);
            return;
        }

        if (unitNumber == 15 && UnitEvolutionMilestones.HasUnit15DoubleMain(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.Unit15SecondMainDamageMul));
            StartCoroutine(FireUnit15DoubleMainRoutine(target, secondDamage));
            return;
        }

        SpawnStandardProjectile(target, GetDamageWithUpgrade(3));
    }

    System.Collections.IEnumerator FireUnit15DoubleMainRoutine(Zombie target, int secondDamage)
    {
        SpawnStandardProjectile(target, GetDamageWithUpgrade(3));
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit15DoubleMainDelay);
        if (target != null && !target.IsExcludedFromCombat)
        {
            SpawnStandardProjectile(target, secondDamage);
        }
    }

    void SpawnStandardProjectile(Zombie target, int shotDamage)
    {
        if (projectilePrefab == null)
        {
            projectilePrefab = CreateProjectilePrefab();
        }

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.SetActive(true);
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.SetTarget(target, shotDamage);
            projScript.sourceUnitNumber = unitNumber;
            if (unitNumber == 15)
            {
                projScript.ConfigureAsUnit15Projectile();
                projScript.unit15SlowChance = UnitEvolutionMilestones.GetUnit15SlowChance(evolutionLevel);
                projScript.unit15FanDirections = UnitEvolutionMilestones.GetUnit15FanDirections(evolutionLevel);
            }
        }
    }

    void FireUnit10Attack(Zombie target)
    {
        if (UnitEvolutionMilestones.HasUnit10TwinShot(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(2) * UnitEvolutionMilestones.Unit10SecondShotDamageMul));
            StartCoroutine(FireUnit10TwinRoutine(target, secondDamage));
            return;
        }
        SpawnUnit10Shot(target, GetDamageWithUpgrade(2));
    }

    System.Collections.IEnumerator FireUnit10TwinRoutine(Zombie target, int secondDamage)
    {
        SpawnUnit10Shot(target, GetDamageWithUpgrade(2));
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit10TwinShotDelay);
        if (target != null && !target.IsExcludedFromCombat)
        {
            SpawnUnit10Shot(target, secondDamage);
        }
    }

    void SpawnUnit10Shot(Zombie target, int shotDamage)
    {
        if (UnitEvolutionMilestones.HasUnit10SplitShot(evolutionLevel))
        {
            GameObject projObj = new GameObject("SplitProjectile");
            projObj.transform.position = transform.position;
            SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 2;
            UnitProjectileVisuals.TryApply(spriteRenderer, 10);
            projObj.transform.localScale = Vector3.one * UnitProjectileVisuals.Unit10ProjectileScale;
            SplitProjectile splitProj = projObj.AddComponent<SplitProjectile>();
            splitProj.speed = projectileSpeed;
            splitProj.damage = shotDamage;
            splitProj.splitDamage = shotDamage;
            splitProj.ownerCharacter = this;
            splitProj.neighborSplashFraction = UnitEvolutionMilestones.HasUnit10NeighborSplash(evolutionLevel)
                ? UnitEvolutionMilestones.Unit10NeighborSplashMul : 0f;
            splitProj.sourceUnitNumber = unitNumber;
            splitProj.SetTarget(target);
            return;
        }

        GameObject straightObj = new GameObject("Unit10Projectile");
        straightObj.transform.position = transform.position;
        SpriteRenderer sr = straightObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        UnitProjectileVisuals.TryApply(sr, 10);
        straightObj.transform.localScale = Vector3.one * UnitProjectileVisuals.Unit10ProjectileScale;
        Projectile proj = straightObj.AddComponent<Projectile>();
        proj.speed = projectileSpeed;
        proj.sourceUnitNumber = unitNumber;
        proj.SetTarget(target, shotDamage);
    }

    public void ApplyUnit10NeighborSplash(Zombie primaryTarget, int baseDamage)
    {
        if (!UnitEvolutionMilestones.HasUnit10NeighborSplash(evolutionLevel) || currentCell == null) return;
        BoardManager bm = FindFirstObjectByType<BoardManager>();
        float cellSize = bm != null ? bm.cellSize : 1f;
        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * UnitEvolutionMilestones.Unit10NeighborSplashMul));
        Vector3 upPos = currentCell.transform.position + Vector3.up * cellSize;
        Vector3 downPos = currentCell.transform.position + Vector3.down * cellSize;
        ApplySplashAtNeighborCell(upPos, splashDamage, primaryTarget);
        ApplySplashAtNeighborCell(downPos, splashDamage, primaryTarget);
    }

    void ApplySplashAtNeighborCell(Vector3 cellCenter, int splashDamage, Zombie exclude)
    {
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        float halfCell = 0.5f;
        for (int i = 0; i < _zombieSearchBuffer.Count; i++)
        {
            Zombie z = _zombieSearchBuffer[i];
            if (z == null || z == exclude) continue;
            Vector2 delta = (Vector2)z.transform.position - (Vector2)cellCenter;
            if (Mathf.Abs(delta.x) <= halfCell && Mathf.Abs(delta.y) <= halfCell)
            {
                z.TakeDamage(splashDamage, unitNumber);
            }
        }
    }

    void FireUnit2Fireball(Zombie target)
    {
        if (UnitEvolutionMilestones.HasUnit2TwinShot(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.Unit2SecondShotDamageMul));
            StartCoroutine(FireUnit2TwinShotRoutine(target, secondDamage));
            return;
        }

        SpawnUnit2Fireball(target, GetDamageWithUpgrade(3));
    }

    System.Collections.IEnumerator FireUnit2TwinShotRoutine(Zombie target, int secondDamage)
    {
        SpawnUnit2Fireball(target, GetDamageWithUpgrade(3));
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit2TwinShotDelay);
        if (target != null && !target.IsExcludedFromCombat)
        {
            SpawnUnit2Fireball(target, secondDamage);
        }
    }

    void SpawnUnit2Fireball(Zombie target, int shotDamage)
    {
        GameObject projObj = new GameObject("Unit2Fireball");
        projObj.transform.position = transform.position;

        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;

        Unit2FireballProjectile proj = projObj.AddComponent<Unit2FireballProjectile>();
        proj.speed = projectileSpeed;
        proj.damage = shotDamage;
        proj.sourceUnitNumber = unitNumber;
        proj.useAreaDamage = true;
        proj.areaSize = UnitEvolutionMilestones.GetUnit2AreaSize(evolutionLevel);
        proj.secondaryBlastMul = UnitEvolutionMilestones.HasUnit2SecondaryBlast(evolutionLevel)
            ? UnitEvolutionMilestones.Unit2SecondaryBlastMul : 0f;
        proj.SetTarget(target);
        projObj.transform.localScale = Vector3.one * 4f;
    }


    /// <summary>
    /// 3번 유닛 전용: 느린 전기 총알을 발사합니다
    /// </summary>
    void FireElectricProjectile(Zombie target)
    {
        if (target == null) return;

        if (UnitEvolutionMilestones.HasUnit3DoubleTap(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(2) * UnitEvolutionMilestones.Unit3SecondShotDamageMul));
            StartCoroutine(FireUnit3DoubleTapRoutine(target, secondDamage));
            return;
        }

        SpawnElectricProjectile(target, GetDamageWithUpgrade(2), 1f);
    }

    System.Collections.IEnumerator FireUnit3DoubleTapRoutine(Zombie target, int secondDamage)
    {
        SpawnElectricProjectile(target, GetDamageWithUpgrade(2), 1f);
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit3DoubleTapDelay);
        if (target != null && !target.IsExcludedFromCombat)
        {
            SpawnElectricProjectile(target, secondDamage, 1f);
        }
    }

    void SpawnElectricProjectile(Zombie target, int primaryDamage, float damageScale)
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(primaryDamage * damageScale));
        GameObject projObj = new GameObject("ElectricProjectile");
        projObj.transform.position = transform.position;
        
        // 스프라이트 렌더러
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        Sprite attackSprite = Resources.Load<Sprite>("unit_003_attack");
        if (attackSprite == null)
        {
            Sprite[] attackSprites = Resources.LoadAll<Sprite>("unit_003_attack");
            if (attackSprites != null && attackSprites.Length > 0)
            {
                attackSprite = attackSprites[0];
            }
        }
        if (attackSprite == null)
        {
            attackSprite = Resources.Load<Sprite>("unit_003");
        }
        if (attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            Color electricColor = new Color(0.3f, 0.9f, 1f);
            spriteRenderer.color = electricColor;
            spriteRenderer.sprite = CreateSquareSprite(electricColor);
        }
        spriteRenderer.sortingOrder = 2;
        
        // 크기는 작게
        projObj.transform.localScale = GetScaledProjectileSize(Vector3.one * 3f);
        
        // ElectricProjectile 컴포넌트 추가
        ElectricProjectile electricProj = projObj.AddComponent<ElectricProjectile>();
        electricProj.speed = projectileSpeed;
        electricProj.damage = damage;
        electricProj.chainDamage = damage;
        electricProj.chainRange = UnitEvolutionMilestones.GetUnit3ChainRange(evolutionLevel);
        electricProj.maxChainCount = UnitEvolutionMilestones.GetUnit3MaxChainCount(evolutionLevel);
        electricProj.chainDamageMultiplier = UnitEvolutionMilestones.GetUnit3ChainDamageMul(evolutionLevel);
        electricProj.sourceUnitNumber = unitNumber;
        electricProj.SetTarget(target);
    }
    
    /// <summary>
    /// 6번 유닛 전용: 얼음 총알 발사
    /// </summary>
    void FireIceProjectile(Zombie target)
    {
        if (target == null) return;

        if (UnitEvolutionMilestones.HasUnit6TwinShot(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit6SecondShotDamageMul));
            StartCoroutine(FireUnit6TwinShotRoutine(target, secondDamage));
            return;
        }

        SpawnIceProjectile(target, GetDamageWithUpgrade(1));
    }

    System.Collections.IEnumerator FireUnit6TwinShotRoutine(Zombie target, int secondDamage)
    {
        SpawnIceProjectile(target, GetDamageWithUpgrade(1));
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit6TwinShotDelay);
        if (target != null && !target.IsExcludedFromCombat)
        {
            SpawnIceProjectile(target, secondDamage);
        }
    }

    void SpawnIceProjectile(Zombie target, int shotDamage)
    {
        GameObject projObj = new GameObject("IceProjectile");
        projObj.transform.position = transform.position;
        
        // 스프라이트 렌더러
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        Sprite attackSprite = Resources.Load<Sprite>("Addons/Elf/0_Unit/0_Sprite/6_Weapons/0_Sword/Elf_Weapon_19");
        if (attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            Color iceColor = new Color(0.4f, 0.8f, 1f);
            spriteRenderer.color = iceColor;
            spriteRenderer.sprite = CreateSquareSprite(iceColor);
        }
        spriteRenderer.sortingOrder = 2;
        
        // 크기
        projObj.transform.localScale = GetScaledProjectileSize(Vector3.one * 2.7f);
        
        IceProjectile iceProj = projObj.AddComponent<IceProjectile>();
        iceProj.speed = 12f;
        iceProj.damage = shotDamage;
        iceProj.sourceUnitNumber = unitNumber;
        iceProj.slowChance = UnitEvolutionMilestones.GetUnit6SlowChance(evolutionLevel);
        iceProj.slowDuration = UnitEvolutionMilestones.GetUnit6SlowDuration(evolutionLevel);
        iceProj.spriteAngleOffset = -90f;
        if (UnitEvolutionMilestones.HasUnit6BonusSlowShot(evolutionLevel))
        {
            int bonusDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit6BonusSlowShotDamageMul));
            iceProj.onAppliedSlow = slowed =>
            {
                if (slowed != null && !slowed.IsExcludedFromCombat)
                {
                    SpawnIceProjectile(slowed, bonusDamage);
                }
            };
        }
        iceProj.SetTarget(target);
    }
    
    /// <summary>
    /// 7번 유닛: 위아래로 넓은 직선 총알 발사
    /// </summary>
    void FireWideVerticalProjectile()
    {
        GameObject projObj = new GameObject("WideVerticalProjectile");
        projObj.transform.position = GetUnit7SpawnPosition();
        
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("unit_024_attack");
        Color projectileColor = new Color(0.6f, 1f, 0.4f);
        if (sprites != null && sprites.Length > 0)
        {
            System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
            spriteRenderer.sprite = sprites[0];
            spriteRenderer.color = projectileColor;
        }
        else
        {
            spriteRenderer.color = projectileColor;
            spriteRenderer.sprite = CreateSquareSprite(projectileColor);
        }
        spriteRenderer.sortingOrder = 2;

        projObj.transform.localScale = Vector3.one * 2f;

        WideVerticalProjectile proj = projObj.AddComponent<WideVerticalProjectile>();
        proj.speed = 1f;
        proj.damage = GetDamageWithUpgrade(1);
        proj.sourceUnitNumber = unitNumber;
        proj.width = 0.3f;
        proj.height = UnitEvolutionMilestones.GetUnit7Height(evolutionLevel);
        proj.knockbackDistance = UnitEvolutionMilestones.GetUnit7Knockback(evolutionLevel);
        proj.SetAnimationFrames(sprites, projectileColor);

        if (UnitEvolutionMilestones.HasUnit7SecondWave(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit7SecondWaveDamageMul));
            StartCoroutine(FireUnit7SecondWaveRoutine(secondDamage, sprites, projectileColor));
        }
    }

    System.Collections.IEnumerator FireUnit7SecondWaveRoutine(int secondDamage, Sprite[] sprites, Color projectileColor)
    {
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit7SecondWaveDelay);
        GameObject projObj = new GameObject("WideVerticalProjectile_Wave2");
        projObj.transform.position = GetUnit7SpawnPosition();
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        if (sprites != null && sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[0];
            spriteRenderer.color = projectileColor;
        }
        spriteRenderer.sortingOrder = 2;
        projObj.transform.localScale = Vector3.one * 2f;
        WideVerticalProjectile proj = projObj.AddComponent<WideVerticalProjectile>();
        proj.speed = 1f;
        proj.damage = secondDamage;
        proj.sourceUnitNumber = unitNumber;
        proj.width = 0.3f;
        proj.height = UnitEvolutionMilestones.GetUnit7Height(evolutionLevel);
        proj.knockbackDistance = UnitEvolutionMilestones.GetUnit7Knockback(evolutionLevel);
        proj.SetAnimationFrames(sprites, projectileColor);
    }

    Vector3 GetUnit7SpawnPosition()
    {
        if (unitNumber != 7)
        {
            return transform.position;
        }

        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        if (cells == null || cells.Length == 0)
        {
            return transform.position;
        }

        float maxX = float.MinValue;
        foreach (BoardCell cell in cells)
        {
            if (cell == null || !cell.isBoardCell) continue;
            if (cell.transform.position.x > maxX)
            {
                maxX = cell.transform.position.x;
            }
        }

        System.Collections.Generic.List<BoardCell> rightmostCells = new System.Collections.Generic.List<BoardCell>();
        float tolerance = 0.1f;
        foreach (BoardCell cell in cells)
        {
            if (cell == null || !cell.isBoardCell) continue;
            if (Mathf.Abs(cell.transform.position.x - maxX) <= tolerance)
            {
                rightmostCells.Add(cell);
            }
        }

        if (rightmostCells.Count == 0)
        {
            return transform.position;
        }

        int index = Random.Range(0, rightmostCells.Count);
        return rightmostCells[index].transform.position;
    }
    
    /// <summary>
    /// 8번 유닛: 타겟 레이저 지짐이
    /// </summary>
    void FireTargetLaser(Zombie target)
    {
        GameObject laserObj = new GameObject("TargetLaser");
        laserObj.transform.position = transform.position;
        
        TargetLaser laser = laserObj.AddComponent<TargetLaser>();
        laser.damagePerTick = GetDamageWithUpgrade(1);
        laser.sourceUnitNumber = unitNumber;
        laser.tickInterval = 1f;
        laser.duration = 3f;
        laser.SetTarget(transform, target.transform);
    }
    
    /// <summary>
    /// 9번 유닛: 화살모양으로 3발 발사
    /// </summary>
    void FireArrowSpread()
    {
        Vector2 centerDir = Vector2.right;
        Vector2 perpendicular = new Vector2(-centerDir.y, centerDir.x);
        int primaryDamage = GetDamageWithUpgrade(3);

        if (UnitEvolutionMilestones.HasUnit9WideSpread(evolutionLevel))
        {
            const float spread15 = 0.15f;
            const float spread30 = 0.30f;
            int outerDamage = Mathf.Max(1, Mathf.RoundToInt(primaryDamage * UnitEvolutionMilestones.Unit9OuterSpreadDamageMul));
            SpawnArrowProjectile(centerDir, primaryDamage);
            SpawnArrowProjectile((centerDir + perpendicular * spread15).normalized, outerDamage);
            SpawnArrowProjectile((centerDir - perpendicular * spread15).normalized, outerDamage);
            SpawnArrowProjectile((centerDir + perpendicular * spread30).normalized, outerDamage);
            SpawnArrowProjectile((centerDir - perpendicular * spread30).normalized, outerDamage);
        }
        else
        {
            const float spread = 0.15f;
            SpawnArrowProjectile(centerDir, primaryDamage);
            SpawnArrowProjectile((centerDir + perpendicular * spread).normalized, primaryDamage);
            SpawnArrowProjectile((centerDir - perpendicular * spread).normalized, primaryDamage);
        }

        if (UnitEvolutionMilestones.HasUnit9TrailingShot(evolutionLevel))
        {
            int trailDamage = Mathf.Max(1, Mathf.RoundToInt(primaryDamage * UnitEvolutionMilestones.Unit9TrailingDamageMul));
            StartCoroutine(SpawnUnit9TrailingArrow(centerDir, trailDamage, UnitEvolutionMilestones.Unit9TrailingDelay));
        }
    }

    System.Collections.IEnumerator SpawnUnit9TrailingArrow(Vector2 direction, int damage, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        SpawnArrowProjectile(direction, damage);
    }
    
    void SpawnArrowProjectile(Vector2 direction, int damage)
    {
        GameObject projObj = new GameObject("ArrowProjectile");
        projObj.transform.position = transform.position;
        
        // 스프라이트 렌더러
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        Sprite attackSprite = Resources.Load<Sprite>("unit_009_attack");
        if (attackSprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("unit_009_attack");
            if (sprites != null && sprites.Length > 0)
            {
                attackSprite = sprites[0];
            }
        }
        if (attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            Color projColor = new Color(1f, 0.8f, 0.2f);
            spriteRenderer.color = projColor;
            spriteRenderer.sprite = CreateSquareSprite(projColor);
        }
        spriteRenderer.sortingOrder = 2;
        
        // 크기는 작게
        projObj.transform.localScale = Vector3.one * 1.5f;

        // 하늘에서 떨어지는 방향(아래)을 기준으로 회전
        projObj.transform.rotation = Quaternion.FromToRotation(Vector2.down, direction);
        
        // 콜라이더 추가
        BoxCollider2D collider = projObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one * 0.3f;
        
        StraightProjectile proj = projObj.AddComponent<StraightProjectile>();
        proj.speed = projectileSpeed;
        proj.direction = direction;
        proj.damage = damage;
        proj.sourceUnitNumber = unitNumber;
        proj.hitEffectScale = Vector3.one * 3f;
    }

    void FireUnit18ArrowStrike()
    {
        int count = UnitEvolutionMilestones.GetUnit18ArrowCount(evolutionLevel);
        Zombie[] targets = FindNearestZombies(count);
        if (targets == null || targets.Length == 0) return;

        for (int i = 0; i < targets.Length; i++)
        {
            Zombie target = targets[i];
            if (target == null) continue;
            if (i >= 2)
            {
                int extraDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.Unit18ExtraArrowDamageMul));
                StartCoroutine(PlayUnit18ArrowDropWithDamage(target, extraDamage));
            }
            else
            {
                StartCoroutine(PlayUnit18ArrowDrop(target));
            }
        }
    }

    System.Collections.IEnumerator PlayUnit18ArrowDropWithDamage(Zombie target, int damage)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>("unit_018_attack");
        if (frames == null || frames.Length == 0 || target == null) yield break;
        System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        GameObject arrowObj = new GameObject("Unit18Arrow");
        arrowObj.transform.position = target.transform.position;
        arrowObj.transform.localScale = Vector3.one * 2f;
        SpriteRenderer renderer = arrowObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 4;
        renderer.sprite = frames[0];
        int lastIndex = Mathf.Min(7, frames.Length - 1);
        for (int i = 0; i <= lastIndex; i++)
        {
            if (renderer == null) yield break;
            renderer.sprite = frames[i];
            yield return new WaitForSeconds(Unit18ArrowFrameTime);
        }
        if (target != null)
        {
            target.TakeDamage(damage, unitNumber);
            target.ApplyStun(UnitEvolutionMilestones.GetUnit18StunDuration(evolutionLevel));
        }
        yield return new WaitForSeconds(UnitEvolutionMilestones.GetUnit18StunDuration(evolutionLevel));
        if (arrowObj != null) Destroy(arrowObj);
    }

    Zombie[] FindNearestZombies(int count)
    {
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        FilterZombiesByAttackRange();
        if (_zombieSearchBuffer.Count == 0) return System.Array.Empty<Zombie>();

        Vector3 p = transform.position;
        _zombieSearchBuffer.Sort((a, b) =>
        {
            float da = Vector2.Distance(p, a.transform.position);
            float db = Vector2.Distance(p, b.transform.position);
            return da.CompareTo(db);
        });

        int actual = Mathf.Min(count, _zombieSearchBuffer.Count);
        Zombie[] result = new Zombie[actual];
        for (int i = 0; i < actual; i++)
        {
            result[i] = _zombieSearchBuffer[i];
        }
        return result;
    }

    /// <summary>메인 카메라 시야 안에 있는 좀비만 대상, 그중 유닛에서 가장 먼 순으로 최대 count명</summary>
    Zombie[] FindFarthestZombiesInView(int count)
    {
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        FilterZombiesByAttackRange();
        for (int i = _zombieSearchBuffer.Count - 1; i >= 0; i--)
        {
            if (!IsPositionInMainCameraView(_zombieSearchBuffer[i].transform.position))
            {
                _zombieSearchBuffer.RemoveAt(i);
            }
        }
        if (_zombieSearchBuffer.Count == 0) return System.Array.Empty<Zombie>();

        Vector3 p = transform.position;
        _zombieSearchBuffer.Sort((a, b) =>
        {
            float da = Vector2.Distance(p, a.transform.position);
            float db = Vector2.Distance(p, b.transform.position);
            return db.CompareTo(da);
        });

        int take = Mathf.Min(count, _zombieSearchBuffer.Count);
        Zombie[] result = new Zombie[take];
        for (int i = 0; i < take; i++)
        {
            result[i] = _zombieSearchBuffer[i];
        }
        return result;
    }

    Zombie[] FindFarthestZombies(int count)
    {
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        if (_zombieSearchBuffer.Count == 0) return System.Array.Empty<Zombie>();

        Vector3 p = transform.position;
        _zombieSearchBuffer.Sort((a, b) =>
        {
            float da = Vector2.Distance(p, a.transform.position);
            float db = Vector2.Distance(p, b.transform.position);
            return db.CompareTo(da);
        });

        int actual = Mathf.Min(count, _zombieSearchBuffer.Count);
        Zombie[] result = new Zombie[actual];
        for (int i = 0; i < actual; i++)
        {
            result[i] = _zombieSearchBuffer[i];
        }
        return result;
    }

    System.Collections.IEnumerator PlayUnit18ArrowDrop(Zombie target)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>("unit_018_attack");
        if (frames == null || frames.Length == 0 || target == null)
        {
            yield break;
        }

        System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));

        GameObject arrowObj = new GameObject("Unit18Arrow");
        arrowObj.transform.position = target.transform.position;
        arrowObj.transform.localScale = Vector3.one * 2f;
        SpriteRenderer renderer = arrowObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 4;
        renderer.sprite = frames[0];
        renderer.color = Color.white;

        int lastIndex = Mathf.Min(7, frames.Length - 1);
        for (int i = 0; i <= lastIndex; i++)
        {
            if (renderer == null) yield break;
            renderer.sprite = frames[i];
            yield return new WaitForSeconds(Unit18ArrowFrameTime);
        }

        if (target != null)
        {
            float dmgMul = UnitEvolutionMilestones.GetUnit18ArrowDamageMul(evolutionLevel);
            int arrowDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * dmgMul));
            target.TakeDamage(arrowDamage, unitNumber);
            target.ApplyStun(UnitEvolutionMilestones.GetUnit18StunDuration(evolutionLevel));
        }

        if (renderer != null)
        {
            renderer.sprite = frames[lastIndex];
        }

        yield return new WaitForSeconds(UnitEvolutionMilestones.GetUnit18StunDuration(evolutionLevel));

        if (arrowObj != null)
        {
            Destroy(arrowObj);
        }
    }

    void FireGiantFistStrike(Zombie target)
    {
        if (target == null) return;

        Vector3 impactPoint = target.transform.position;
        Sprite[] frames = LoadSpriteFrames("unit_017_attack");
        if (frames.Length == 0)
        {
            return;
        }

        float aoeRadius = UnitEvolutionMilestones.GetUnit17AoeRadius(evolutionLevel);
        float hitDelay = Unit17AttackFrameTime * Unit17HitFrameIndex;
        StartCoroutine(PlayUnit17FistStrikeRoutine(impactPoint, frames, hitDelay, aoeRadius, GetDamageWithUpgrade(3)));

        if (UnitEvolutionMilestones.HasUnit17ComboPunch(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.Unit17SecondPunchDamageMul));
            StartCoroutine(PlayUnit17SecondPunchRoutine(impactPoint, frames, hitDelay, aoeRadius, secondDamage));
        }
    }

    System.Collections.IEnumerator PlayUnit17FistStrikeRoutine(Vector3 impactPoint, Sprite[] frames, float hitDelay, float aoeRadius, int damage)
    {
        GameObject effectObj = new GameObject("Unit17Fist");
        effectObj.transform.position = impactPoint;
        SpriteRenderer renderer = effectObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 4;
        renderer.sprite = frames[0];
        renderer.color = Color.white;
        effectObj.transform.localScale = Vector3.one * 2f;
        yield return PlayUnit17FistFrames(effectObj, renderer, frames, hitDelay, aoeRadius, damage);
    }

    System.Collections.IEnumerator PlayUnit17SecondPunchRoutine(Vector3 impactPoint, Sprite[] frames, float hitDelay, float aoeRadius, int damage)
    {
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit17SecondPunchDelay);
        GameObject effectObj = new GameObject("Unit17Fist2");
        effectObj.transform.position = impactPoint;
        SpriteRenderer renderer = effectObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 4;
        renderer.sprite = frames[0];
        renderer.color = Color.white;
        effectObj.transform.localScale = Vector3.one * 2f;
        yield return PlayUnit17FistFrames(effectObj, renderer, frames, hitDelay, aoeRadius, damage);
    }

    System.Collections.IEnumerator PlayUnit17FistFrames(GameObject effectObj, SpriteRenderer renderer, Sprite[] frames, float hitDelay, float aoeRadius, int damage)
    {
        float elapsed = 0f;
        bool hitApplied = false;
        float frameTime = Unit17AttackFrameTime;
        Vector3 hitPoint = effectObj != null ? effectObj.transform.position : Vector3.zero;

        for (int i = 0; i < frames.Length; i++)
        {
            if (renderer == null) yield break;
            renderer.sprite = frames[i];

            elapsed += frameTime;
            if (!hitApplied && elapsed >= hitDelay)
            {
                hitApplied = true;
                ApplyGiantFistDamage(hitPoint, aoeRadius, damage);
            }

            yield return new WaitForSeconds(frameTime);
        }

        if (!hitApplied)
        {
            ApplyGiantFistDamage(hitPoint, aoeRadius, damage);
        }

        if (effectObj != null)
        {
            Destroy(effectObj);
        }
    }

    void ApplyGiantFistDamage(Vector3 center, float radius, int damage)
    {
        CreateGiantFistAoEIndicator(center, radius);
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        for (int i = 0; i < _zombieSearchBuffer.Count; i++)
        {
            Zombie zombie = _zombieSearchBuffer[i];
            float dist = Vector2.Distance(center, zombie.transform.position);
            if (dist <= radius)
            {
                zombie.TakeDamage(damage, unitNumber);
            }
        }
    }

    void FireUnit27Stone(Zombie target)
    {
        GameObject projObj = new GameObject("Unit27Stone");
        projObj.transform.position = transform.position;

        SpriteRenderer renderer = projObj.AddComponent<SpriteRenderer>();
        Sprite stoneSprite = Resources.Load<Sprite>("unit_1_attack");
        Color stoneColor = new Color(0.6f, 1f, 0.4f);
        if (stoneSprite != null)
        {
            renderer.sprite = stoneSprite;
            renderer.color = stoneColor;
        }
        else
        {
            renderer.sprite = CreateSquareSprite(stoneColor);
            renderer.color = stoneColor;
        }
        int targetOrder = 0;
        SpriteRenderer targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null)
        {
            targetOrder = targetRenderer.sortingOrder;
        }
        renderer.sortingOrder = targetOrder + 2;

        Unit27StoneProjectile projectile = projObj.AddComponent<Unit27StoneProjectile>();
        projectile.speed = projectileSpeed;
        projectile.damage = GetDamageWithUpgrade(3);
        projectile.sourceUnitNumber = unitNumber;
        projectile.lifeTime = 10f;
        projectile.roamDuration = 3.5f;
        projectile.roamHitsToChase = UnitEvolutionMilestones.GetUnit27RoamHitsToChase(evolutionLevel);
        projectile.chaseSpeedMultiplier = UnitEvolutionMilestones.GetUnit27ChaseSpeedMul(evolutionLevel);
        projectile.stickyDamageInterval = UnitEvolutionMilestones.GetUnit27StickyInterval(evolutionLevel);
        projectile.chaseRetargetInterval = 0.35f;
        projectile.SetInitialTarget(target);
        projObj.transform.localScale = Vector3.one / 3f;
    }

    void FireUnit13Shuriken(Zombie target)
    {
        if (target == null) return;

        GameObject projObj = new GameObject("Unit13Shuriken");
        projObj.transform.position = transform.position;

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(projObj.transform, false);

        SpriteRenderer renderer = visualObj.AddComponent<SpriteRenderer>();
        Sprite shurikenSprite = Resources.Load<Sprite>("Addons/Elf/0_Unit/0_Sprite/6_Weapons/3_Shield/Elf_Weapon_16");
        Color shurikenColor = new Color(0.85f, 0.85f, 0.9f);
        if (shurikenSprite != null)
        {
            renderer.sprite = shurikenSprite;
            renderer.color = Color.white;
        }
        else
        {
            renderer.sprite = CreateSquareSprite(shurikenColor);
            renderer.color = shurikenColor;
        }

        if (renderer.sprite != null)
        {
            Vector2 pivotOffset = renderer.sprite.bounds.center;
            visualObj.transform.localPosition = new Vector3(-pivotOffset.x, -pivotOffset.y, 0f);
        }

        int targetOrder = 0;
        SpriteRenderer targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null)
        {
            targetOrder = targetRenderer.sortingOrder;
        }
        renderer.sortingOrder = targetOrder + 2;

        projObj.transform.localScale = Vector3.one * 1.5f;

        Vector2 dir = (target.transform.position - transform.position);
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector2.right;
        }

        Unit13ShurikenProjectile projectile = projObj.AddComponent<Unit13ShurikenProjectile>();
        projectile.speed = projectileSpeed;
        projectile.damage = GetDamageWithUpgrade(1);
        projectile.sourceUnitNumber = unitNumber;
        projectile.lifeTime = 15f;
        projectile.hitRadius = 0.65f * UnitEvolutionMilestones.GetUnit13HitRadiusMul(evolutionLevel);
        projectile.pierceDamageMultiplier = UnitEvolutionMilestones.GetUnit13PierceMul(evolutionLevel);
        projectile.maxReturnLegs = UnitEvolutionMilestones.GetUnit13ReturnLegs(evolutionLevel);
        projectile.Launch(transform, dir);
    }

    void FireUnit23PiercingShot(Zombie target)
    {
        if (target == null) return;

        GameObject managerObj = new GameObject("Unit23TrailManager");
        managerObj.transform.position = transform.position;
        Unit23TrailManager trailManager = managerObj.AddComponent<Unit23TrailManager>();
        trailManager.postLastPadDelay = 3f * UnitEvolutionMilestones.GetUnit23PadPersistMul(evolutionLevel);
        trailManager.sequentialDestroyInterval = 0.05f;

        GameObject projObj = new GameObject("Unit23PiercingShot");
        projObj.transform.position = transform.position;

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(projObj.transform, false);

        SpriteRenderer renderer = visualObj.AddComponent<SpriteRenderer>();
        const string shotResourceName = "unit_023_attack";
        Sprite[] shotFrames = Resources.LoadAll<Sprite>(shotResourceName);
        Color fallbackColor = new Color(1f, 0.45f, 0.35f);
        if (shotFrames != null && shotFrames.Length > 0)
        {
            System.Array.Sort(shotFrames, (a, b) => string.CompareOrdinal(a.name, b.name));
            renderer.sprite = shotFrames[0];
            renderer.color = Color.white;
            UnitSpriteAnimator animator = visualObj.AddComponent<UnitSpriteAnimator>();
            animator.Initialize(renderer, shotResourceName, 12f);
            animator.SetAnimating(true);
        }
        else
        {
            renderer.sprite = CreateSquareSprite(fallbackColor);
            renderer.color = fallbackColor;
        }

        if (renderer.sprite != null)
        {
            Vector2 pivotOffset = renderer.sprite.bounds.center;
            visualObj.transform.localPosition = new Vector3(-pivotOffset.x, -pivotOffset.y, 0f);
        }

        int targetOrder = 0;
        SpriteRenderer targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null)
        {
            targetOrder = targetRenderer.sortingOrder;
        }
        renderer.sortingOrder = targetOrder + 2;

        projObj.transform.localScale = Vector3.one * 2.1f;

        Vector2 dir = (target.transform.position - transform.position);
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector2.right;
        }

        Unit23PiercingProjectile projectile = projObj.AddComponent<Unit23PiercingProjectile>();
        projectile.speed = 11f;
        projectile.damage = GetDamageWithUpgrade(1);
        projectile.sourceUnitNumber = unitNumber;
        projectile.hitRadius = 0.6f;
        projectile.spinSpeed = 0f;
        projectile.padSize = 0.9f;
        projectile.padDamage = GetPeriodicDamage(UnitEvolutionMilestones.GetUnit23PadDamageFraction(evolutionLevel));
        projectile.endExplosionOnDestroy = UnitEvolutionMilestones.HasUnit23EndExplosion(evolutionLevel);
        projectile.endExplosionRadius = UnitEvolutionMilestones.Unit23EndExplosionRadius;
        projectile.endExplosionDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit23EndExplosionMul));
        projectile.padDamageInterval = 1f;
        projectile.trailSpawnDistance = 0.35f;
        projectile.padColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        projectile.Launch(dir, transform.position, trailManager);
    }

    void FireUnit12JudgmentLance(Zombie target)
    {
        if (target == null) return;

        int primaryDamage = GetDamageWithUpgrade(3);
        int chainCount = UnitEvolutionMilestones.GetUnit12ChainCount(evolutionLevel);
        float chainRatio = UnitEvolutionMilestones.GetUnit12ChainDamageRatio(evolutionLevel);
        float stunDuration = UnitEvolutionMilestones.GetUnit12StunDuration(evolutionLevel);

        if (UnitEvolutionMilestones.HasUnit12DoubleLance(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(primaryDamage * UnitEvolutionMilestones.Unit12SecondLanceDamageMul));
            StartCoroutine(FireUnit12DoubleLanceRoutine(target, primaryDamage, secondDamage, chainCount, chainRatio, stunDuration));
            return;
        }

        LaunchUnit12JudgmentLance(target, primaryDamage, chainCount, chainRatio, stunDuration);
    }

    System.Collections.IEnumerator FireUnit12DoubleLanceRoutine(
        Zombie target, int primaryDamage, int secondDamage, int chainCount, float chainRatio, float stunDuration)
    {
        LaunchUnit12JudgmentLance(target, primaryDamage, chainCount, chainRatio, stunDuration);
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit12DoubleLanceDelay);
        if (target != null && !target.IsExcludedFromCombat)
        {
            LaunchUnit12JudgmentLance(target, secondDamage, chainCount, chainRatio, stunDuration);
        }
    }

    void LaunchUnit12JudgmentLance(Zombie target, int primaryDamage, int chainCount, float chainRatio, float stunDuration)
    {
        if (target == null) return;
        Unit12JudgmentLance.ClearAll();
        GameObject go = new GameObject("Unit12JudgmentLance");
        Unit12JudgmentLance lance = go.AddComponent<Unit12JudgmentLance>();
        lance.sourceUnitNumber = unitNumber;
        lance.Launch((Vector2)transform.position, target, primaryDamage, chainCount, chainRatio, stunDuration, this);
    }

    void FireUnit26RicochetProjectile(Zombie target)
    {
        if (target == null) return;
        Vector2 dir = (Vector2)(target.transform.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        else dir = dir.normalized;
        int sort = 1;
        if (spriteRenderer != null) sort = spriteRenderer.sortingOrder;

        var go = new GameObject("Unit26RicochetProjectile");
        go.transform.position = transform.position;
        var sr = go.AddComponent<SpriteRenderer>();
        const string res = "unit_026_attack";
        Sprite sp = Resources.Load<Sprite>(res);
        if (sp == null)
        {
            Sprite[] a = Resources.LoadAll<Sprite>(res);
            if (a != null && a.Length > 0)
            {
                System.Array.Sort(a, (x, y) => string.CompareOrdinal(x.name, y.name));
                sp = a[0];
            }
        }
        if (sp != null)
        {
            sr.sprite = sp;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = CreateSquareSprite(new Color(1f, 0.35f, 0.55f));
        }
        sr.sortingOrder = sort + 2;
        go.transform.localScale = GetScaledProjectileSize(GetProjectileScale() * 0.85f);

        var rico = go.AddComponent<Unit26RicochetProjectile>();
        rico.speed = 10f;
        rico.contactDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.GetUnit26ContactDamageMul(evolutionLevel)));
        rico.hitRadius = 0.5f;
        rico.wallCastRadius = 0.14f;
        rico.maxLifetime = 5f * UnitEvolutionMilestones.GetUnit26LifetimeMul(evolutionLevel);
        rico.speedRampPerSec = UnitEvolutionMilestones.HasUnit26SpeedRamp(evolutionLevel) ? UnitEvolutionMilestones.Unit26SpeedRampPerSec : 0f;
        rico.sourceUnitNumber = unitNumber;
        rico.BindOwner(this);
        rico.Launch(dir);
        NotifyUnit26ProjectileStarted();
    }

    public void NotifyUnit26ProjectileStarted()
    {
        if (unitNumber != 26 || isShopPreviewInstance) return;

        unit26ActiveProjectileCount++;
        if (unit26ActiveProjectileCount == 1)
        {
            ApplyUnit26ProjectileOwnerDim();
        }
    }

    public void NotifyUnit26ProjectileEnded()
    {
        if (unitNumber != 26) return;

        unit26ActiveProjectileCount = Mathf.Max(0, unit26ActiveProjectileCount - 1);
        if (unit26ActiveProjectileCount == 0)
        {
            RestoreUnit26ProjectileOwnerVisuals();
        }
    }

    void ApplyUnit26ProjectileOwnerDim()
    {
        CollectUnit26TintRenderers();
        if (unit26TintRenderers == null) return;

        for (int i = 0; i < unit26TintRenderers.Length; i++)
        {
            SpriteRenderer sr = unit26TintRenderers[i];
            if (sr == null) continue;
            unit26TintOriginalColors[i] = sr.color;
            Color black = Color.black;
            black.a = sr.color.a;
            sr.color = black;
        }
    }

    void RestoreUnit26ProjectileOwnerVisuals()
    {
        if (unit26TintRenderers == null || unit26TintOriginalColors == null) return;

        for (int i = 0; i < unit26TintRenderers.Length; i++)
        {
            SpriteRenderer sr = unit26TintRenderers[i];
            if (sr == null) continue;
            sr.color = unit26TintOriginalColors[i];
        }
    }

    void CollectUnit26TintRenderers()
    {
        SpriteRenderer[] all = GetComponentsInChildren<SpriteRenderer>(true);
        var list = new List<SpriteRenderer>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            if (ShouldTintForUnit26Projectile(all[i]))
            {
                list.Add(all[i]);
            }
        }

        unit26TintRenderers = list.ToArray();
        unit26TintOriginalColors = new Color[unit26TintRenderers.Length];
    }

    static bool ShouldTintForUnit26Projectile(SpriteRenderer sr)
    {
        if (sr == null) return false;

        Transform t = sr.transform;
        Transform parent = t.parent;
        if (parent != null && parent.name == "EvolutionStars") return false;
        if (t.name.StartsWith("Star_")) return false;

        string n = t.name;
        if (n.Contains("Marker") || n.Contains("Pad")) return false;
        if (n == "UnitSummonFx" || n == "UnitEvolutionFx") return false;
        if (n == "Unit26Shadow") return false;
        if (n.Contains("SilhouetteOutline")) return false;
        return true;
    }

    void FireUnit25LaserBeam(Zombie target)
    {
        if (target == null) return;

        GameObject beamObj = new GameObject("Unit25LaserBeam");
        Unit25LaserBeam beam = beamObj.AddComponent<Unit25LaserBeam>();
        beam.duration = UnitEvolutionMilestones.GetUnit25BeamDuration(evolutionLevel);
        beam.damageInterval = 0.5f;
        beam.damage = GetDamageWithUpgrade(1);
        beam.beamWidth = 0.6f * UnitEvolutionMilestones.GetUnit25BeamWidthMul(evolutionLevel);
        beam.afterglowOnEnd = UnitEvolutionMilestones.HasUnit25Afterglow(evolutionLevel);
        beam.afterglowDuration = UnitEvolutionMilestones.Unit25AfterglowDuration;
        beam.afterglowDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit25AfterglowDamageMul));
        beam.sourceUnitNumber = unitNumber;
        beam.outerColor = new Color(0.4f, 0.85f, 1f, 0.55f);
        beam.innerColor = new Color(0.95f, 1f, 1f, 1f);
        beam.Launch(transform.position, target.transform.position);
    }

    void FireUnit11IceColumn()
    {
        Zombie r = FindRandomZombie();
        if (r == null)
        {
            return;
        }

        float centerX = r.transform.position.x;
        float plateWidth = Mathf.Max(0.3f, EstimateZombieMaxBodyExtent() * 0.7f * UnitEvolutionMilestones.GetUnit11PlateWidthMul(evolutionLevel));
        float minY;
        float maxY;
        BoardManager bm = FindFirstObjectByType<BoardManager>();
        if (bm == null || !bm.TryGetLaneBounds(out minY, out maxY))
        {
            minY = transform.position.y - 2.5f;
            maxY = transform.position.y + 2.5f;
        }

        float midY = (minY + maxY) * 0.5f;
        float laneHeight = maxY - minY;

        GameObject field = new GameObject("Unit11IceField");
        field.transform.position = new Vector3(centerX, midY, 0f);

        // Ground 타일(대략 -20)보다 위, 좀비 본체(SpriteRenderer 1)보다 뒤
        int iceBaseOrder = -3;
        int iceRimOrder = iceBaseOrder + 1;
        int iceSheenOrder = iceBaseOrder + 2;

        GameObject baseLayer = new GameObject("IceBase");
        baseLayer.transform.SetParent(field.transform, false);
        baseLayer.transform.localPosition = Vector3.zero;
        baseLayer.transform.localScale = new Vector3(plateWidth, laneHeight, 1f);
        SpriteRenderer srBase = baseLayer.AddComponent<SpriteRenderer>();
        Color iceCore = new Color(0.38f, 0.72f, 0.95f, 0.68f);
        srBase.sprite = CreateSquareSprite(iceCore);
        srBase.color = iceCore;
        srBase.sortingOrder = iceBaseOrder;

        GameObject rimLayer = new GameObject("IceFrostRim");
        rimLayer.transform.SetParent(field.transform, false);
        rimLayer.transform.localPosition = Vector3.zero;
        const float innerW = 0.92f;
        const float innerH = 0.9f;
        rimLayer.transform.localScale = new Vector3(plateWidth * innerW, laneHeight * innerH, 1f);
        SpriteRenderer srRim = rimLayer.AddComponent<SpriteRenderer>();
        Color frostRim = new Color(0.7f, 0.9f, 1f, 0.4f);
        srRim.sprite = CreateSquareSprite(frostRim);
        srRim.color = frostRim;
        srRim.sortingOrder = iceRimOrder;

        GameObject sheenLayer = new GameObject("IceSheen");
        sheenLayer.transform.SetParent(field.transform, false);
        sheenLayer.transform.localPosition = new Vector3(0f, laneHeight * 0.1f, 0f);
        sheenLayer.transform.localScale = new Vector3(plateWidth * 0.55f, laneHeight * 0.35f, 1f);
        SpriteRenderer srSheen = sheenLayer.AddComponent<SpriteRenderer>();
        Color sheen = new Color(0.92f, 0.98f, 1f, 0.25f);
        srSheen.sprite = CreateSquareSprite(sheen);
        srSheen.color = sheen;
        srSheen.sortingOrder = iceSheenOrder;

        Unit11IceField iceField = field.AddComponent<Unit11IceField>();
        iceField.Init(centerX, plateWidth, minY, maxY, UnitEvolutionMilestones.GetUnit11HoldDuration(evolutionLevel),
            UnitEvolutionMilestones.GetUnit11FreezeDuration(evolutionLevel) + UnitArchiveAwakening.GetFreezeDurationBonus(unitNumber),
            UnitEvolutionMilestones.GetUnit11MaxFreezeTargets(evolutionLevel));
    }

    Vector3 GetUnit8MouthWorldPosition()
    {
        if (usesUnit8Prefab && unit8PrefabVisual != null)
        {
            Transform h = null;
            foreach (Transform tr in unit8PrefabVisual.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name == "5_Head")
                {
                    h = tr;
                    break;
                }
            }
            if (h == null)
            {
                foreach (Transform tr in unit8PrefabVisual.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name == "6_FaceHair")
                    {
                        h = tr;
                        break;
                    }
                }
            }
            if (h != null)
            {
                return h.TransformPoint(new Vector3(0.1f, -0.1f, 0f));
            }
        }
        return transform.position + new Vector3(0f, 0.9f, 0f);
    }

    void FireUnit8VomitLaser(Zombie target)
    {
        if (target == null) return;

        Vector3 origin = GetUnit8MouthWorldPosition();
        GameObject lo = new GameObject("Unit8VomitLaser");
        Unit20TargetedLaser laser = lo.AddComponent<Unit20TargetedLaser>();
        laser.duration = UnitEvolutionMilestones.GetUnit8LaserDuration(evolutionLevel);
        laser.damageInterval = UnitEvolutionMilestones.GetUnit8TickInterval(evolutionLevel);
        laser.beamWidth = 0.225f;
        laser.outerColor = new Color(0.32f, 0.4f, 0.16f, 0.6f);
        laser.innerColor = new Color(0.52f, 0.78f, 0.32f, 0.95f);
        laser.residualSplashOnEnd = UnitEvolutionMilestones.HasUnit8ResidualSplash(evolutionLevel);
        laser.residualSplashMul = UnitEvolutionMilestones.Unit8ResidualSplashMul;
        laser.residualSplashRadius = UnitEvolutionMilestones.Unit8ResidualSplashRadius;
        laser.sourceUnitNumber = unitNumber;
        laser.Launch(origin, target.transform.position, target, GetDamageWithUpgrade(1));
    }

    void FireUnit21FanField(Zombie target)
    {
        if (target == null) return;
        int sort = 1;
        if (usesUnit21Prefab && unit21PrefabVisual != null)
        {
            SpriteRenderer sp = unit21PrefabVisual.GetComponentInChildren<SpriteRenderer>(true);
            if (sp != null) sort = sp.sortingOrder;
        }
        else if (spriteRenderer != null)
        {
            sort = spriteRenderer.sortingOrder;
        }
        var go = new GameObject("Unit21FanField");
        Unit21FanField f = go.AddComponent<Unit21FanField>();
        float spinDuration = UnitEvolutionMilestones.GetUnit21SpinDuration(evolutionLevel);
        f.Init(transform.position, target.transform.position, 12f, spinDuration, GetDamageWithUpgrade(3), sort,
            UnitEvolutionMilestones.GetUnit21BladeRadiusMul(evolutionLevel), unitNumber);

        if (UnitEvolutionMilestones.HasUnit21SecondVortex(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.Unit21SecondVortexDamageMul));
            StartCoroutine(FireUnit21SecondVortexRoutine(target, spinDuration, secondDamage, sort));
        }
    }

    System.Collections.IEnumerator FireUnit21SecondVortexRoutine(Zombie target, float delay, int damage, int sort)
    {
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit21SecondVortexDelay + delay);
        if (target == null || target.IsExcludedFromCombat) yield break;
        var go = new GameObject("Unit21FanField2");
        Unit21FanField f = go.AddComponent<Unit21FanField>();
        f.Init(transform.position, target.transform.position, 12f, UnitEvolutionMilestones.GetUnit21SpinDuration(evolutionLevel),
            damage, sort, UnitEvolutionMilestones.GetUnit21BladeRadiusMul(evolutionLevel), unitNumber);
    }

    void FireUnit20LightningLasers()
    {
        Zombie[] targets = FindNearestZombies(UnitEvolutionMilestones.GetUnit20TargetCount(evolutionLevel));
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        const float headOffsetY = 1.1f;
        Vector3 ball = transform.position + new Vector3(0f, headOffsetY, 0f);

        int sortOrder = 1;
        if (usesUnit20Prefab && unit20PrefabVisual != null)
        {
            SpriteRenderer sp = unit20PrefabVisual.GetComponentInChildren<SpriteRenderer>(true);
            if (sp != null)
            {
                sortOrder = sp.sortingOrder;
            }
        }
        else if (spriteRenderer != null)
        {
            sortOrder = spriteRenderer.sortingOrder;
        }

        const string unit20AttackResource = "unit_020_attack";
        const int unit20AttackFrameCount = 8;

        GameObject orb = new GameObject("Unit20LightningOrb");
        orb.transform.position = ball;
        GameObject orbVis = new GameObject("Vis");
        orbVis.transform.SetParent(orb.transform, false);
        SpriteRenderer orbR = orbVis.AddComponent<SpriteRenderer>();
        Color fallbackBall = new Color(1f, 0.92f, 0.35f, 0.92f);
        Sprite[] u20Frames = Resources.LoadAll<Sprite>(unit20AttackResource);
        if (u20Frames != null && u20Frames.Length > 0)
        {
            System.Array.Sort(u20Frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            orbR.sprite = u20Frames[0];
            orbR.color = Color.white;
            UnitSpriteAnimator orbAnim = orbVis.AddComponent<UnitSpriteAnimator>();
            orbAnim.Initialize(orbR, unit20AttackResource, 12f, unit20AttackFrameCount);
            orbAnim.SetAnimating(true);
        }
        else
        {
            orbR.sprite = CreateSquareSprite(fallbackBall);
            orbR.color = fallbackBall;
        }
        if (orbR.sprite != null)
        {
            Vector2 c = orbR.sprite.bounds.center;
            orbVis.transform.localPosition = new Vector3(-c.x, -c.y, 0f);
        }
        orbVis.transform.localScale = Vector3.one * 1.5f;
        orbR.sortingOrder = sortOrder + 3;
        Object.Destroy(orb, 3.1f);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            int d = i >= 3
                ? Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit20FourthTargetDamageMul))
                : GetDamageWithUpgrade(1);
            GameObject lo = new GameObject("Unit20TargetedLaser");
            Unit20TargetedLaser laser = lo.AddComponent<Unit20TargetedLaser>();
            laser.duration = UnitEvolutionMilestones.GetUnit20LaserDuration(evolutionLevel);
            laser.damageInterval = 0.5f;
            laser.beamWidth = 0.225f;
            laser.sourceUnitNumber = unitNumber;
            laser.Launch(ball, targets[i].transform.position, targets[i], d);
        }
    }

    float EstimateZombieMaxBodyExtent()
    {
        float m = 0.35f;
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        for (int i = 0; i < _zombieSearchBuffer.Count; i++)
        {
            Zombie z = _zombieSearchBuffer[i];
            SpriteRenderer sr = z.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) continue;
            Vector3 s = sr.bounds.size;
            m = Mathf.Max(m, s.x, s.y);
        }
        return m;
    }

    void FireUnit28VortexProjectile()
    {
        Zombie[] pool = FindFarthestZombiesInView(3);
        if (pool == null || pool.Length == 0)
        {
            return;
        }

        int pick = Random.Range(0, pool.Length);
        Zombie chosen = pool[pick];

        if (chosen == null)
        {
            return;
        }

        Vector3 dest = chosen.transform.position;

        GameObject projObj = new GameObject("Unit28Vortex");
        projObj.transform.position = transform.position;

        const string unit28AttackResource = "unit_028_attack";
        const int unit28AttackFrameCount = 8;
        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(projObj.transform, false);
        SpriteRenderer renderer = visualObj.AddComponent<SpriteRenderer>();
        Color fallbackVortex = new Color(0.65f, 0.35f, 1f);
        Sprite[] u28Frames = Resources.LoadAll<Sprite>(unit28AttackResource);
        if (u28Frames != null && u28Frames.Length > 0)
        {
            System.Array.Sort(u28Frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            renderer.sprite = u28Frames[0];
            renderer.color = Color.white;
            UnitSpriteAnimator u28Anim = visualObj.AddComponent<UnitSpriteAnimator>();
            u28Anim.Initialize(renderer, unit28AttackResource, 12f, unit28AttackFrameCount);
            u28Anim.SetAnimating(true);
        }
        else
        {
            renderer.sprite = CreateSquareSprite(fallbackVortex);
            renderer.color = fallbackVortex;
        }
        if (renderer.sprite != null)
        {
            Vector2 po = renderer.sprite.bounds.center;
            visualObj.transform.localPosition = new Vector3(-po.x, -po.y, 0f);
        }
        int sortOrder = 0;
        SpriteRenderer nearTarget = chosen.GetComponentInChildren<SpriteRenderer>();
        if (nearTarget != null)
        {
            sortOrder = nearTarget.sortingOrder;
        }
        if (spriteRenderer != null)
        {
            sortOrder = Mathf.Max(sortOrder, spriteRenderer.sortingOrder);
        }
        renderer.sortingOrder = sortOrder + 2;
        projObj.transform.localScale = Vector3.one * 2.4f;

        float cellSize = GetBoardCellSizeReference();

        Unit28VortexProjectile vortex = projObj.AddComponent<Unit28VortexProjectile>();
        vortex.flightSpeed = 9f;
        vortex.landStayDuration = UnitEvolutionMilestones.GetUnit28StayDuration(evolutionLevel);
        vortex.pullRadius = cellSize * UnitEvolutionMilestones.Unit28PullRadiusCellFactor;
        vortex.minClusterRadius = cellSize * UnitEvolutionMilestones.Unit28MinClusterRadiusCellFactor;
        vortex.maxPullDistancePerEnemy = cellSize * UnitEvolutionMilestones.Unit28MaxPullDistanceCellFactor;
        vortex.maxPullTargets = UnitEvolutionMilestones.Unit28MaxPullTargets;
        vortex.pullSpeed = UnitEvolutionMilestones.Unit28BasePullSpeed * UnitEvolutionMilestones.GetUnit28PullSpeedMul(evolutionLevel);
        vortex.lingeringAuraOnEnd = UnitEvolutionMilestones.HasUnit28LingeringAura(evolutionLevel);
        vortex.lingerDuration = UnitEvolutionMilestones.Unit28LingerDuration;
        vortex.lingerDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit28LingerDamageMul));
        vortex.contactRadius = cellSize * UnitEvolutionMilestones.Unit28PullRadiusCellFactor;
        vortex.damageInterval = 1f;
        vortex.sourceUnitNumber = unitNumber;
        vortex.Launch(transform.position, dest, GetDamageWithUpgrade(1));
    }

    void FireUnit14BouncingProjectiles()
    {
        Zombie[] targets = FindNearestZombies(UnitEvolutionMilestones.HasUnit14ThirdTarget(evolutionLevel) ? 3 : 2);
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        SpawnUnit14BouncerBurst(targets[0]);
        Zombie second = targets.Length > 1 ? targets[1] : targets[0];
        SpawnUnit14BouncerBurst(second);

        if (UnitEvolutionMilestones.HasUnit14ThirdTarget(evolutionLevel) && targets.Length > 2)
        {
            int thirdDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit14ThirdTargetDamageMul));
            SpawnUnit14Bouncer(targets[2], thirdDamage);
        }
    }

    void SpawnUnit14BouncerBurst(Zombie firstTarget)
    {
        if (firstTarget == null) return;

        if (UnitEvolutionMilestones.HasUnit14TwinBurst(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(1) * UnitEvolutionMilestones.Unit14SecondBurstDamageMul));
            StartCoroutine(SpawnUnit14TwinBurstRoutine(firstTarget, secondDamage));
            return;
        }

        SpawnUnit14Bouncer(firstTarget, GetDamageWithUpgrade(1));
    }

    System.Collections.IEnumerator SpawnUnit14TwinBurstRoutine(Zombie target, int secondDamage)
    {
        SpawnUnit14Bouncer(target, GetDamageWithUpgrade(1));
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit14TwinBurstDelay);
        if (target != null && !target.IsExcludedFromCombat)
        {
            SpawnUnit14Bouncer(target, secondDamage);
        }
    }

    void SpawnUnit14Bouncer(Zombie firstTarget, int shotDamage)
    {
        if (firstTarget == null) return;

        GameObject projObj = new GameObject("Unit14Bouncer");
        projObj.transform.position = transform.position;

        SpriteRenderer renderer = projObj.AddComponent<SpriteRenderer>();
        UnitProjectileVisuals.TryApply(renderer, 14);

        int targetOrder = 0;
        SpriteRenderer targetRenderer = firstTarget.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null)
        {
            targetOrder = targetRenderer.sortingOrder;
        }
        renderer.sortingOrder = targetOrder + 2;

        projObj.transform.localScale = Vector3.one * UnitProjectileVisuals.Unit14ProjectileScale;

        Unit14BouncingProjectile projectile = projObj.AddComponent<Unit14BouncingProjectile>();
        projectile.speed = 18f;
        projectile.damage = shotDamage;
        projectile.sourceUnitNumber = unitNumber;
        projectile.lifeTime = 4f;
        projectile.extraRepeatBounces = UnitEvolutionMilestones.GetUnit14ExtraRepeatBounces(evolutionLevel);
        projectile.Launch(firstTarget);
    }

    void FireUnit24RicochetProjectile(Zombie target)
    {
        if (target == null) return;

        Vector2 dir = (Vector2)(target.transform.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        else dir = dir.normalized;

        int sort = 1;
        if (spriteRenderer != null) sort = spriteRenderer.sortingOrder;

        GameObject go = new GameObject("Unit24RicochetProjectile");
        go.transform.position = transform.position;

        const string attackResource = "unit_024_attack";
        const int attackFrameCount = 6;
        const float attackFps = 12f;
        const float visualScale = 1.3f;

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(go.transform, false);
        SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
        Sprite[] frames = Resources.LoadAll<Sprite>(attackResource);
        if (frames != null && frames.Length > 0)
        {
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            sr.sprite = frames[0];
            sr.color = Color.white;
            UnitSpriteAnimator anim = visualObj.AddComponent<UnitSpriteAnimator>();
            anim.Initialize(sr, attackResource, attackFps, attackFrameCount);
            anim.SetAnimating(true);
        }
        else
        {
            Color fallback = new Color(0.75f, 0.95f, 1f);
            sr.sprite = CreateSquareSprite(fallback);
            sr.color = fallback;
        }

        if (sr.sprite != null)
        {
            Vector2 pivotOffset = sr.sprite.bounds.center;
            visualObj.transform.localPosition = new Vector3(-pivotOffset.x, -pivotOffset.y, 0f);
        }

        sr.sortingOrder = sort + 2;
        visualObj.transform.localScale = GetScaledProjectileSize(GetProjectileScale() * visualScale);

        Unit26RicochetProjectile rico = go.AddComponent<Unit26RicochetProjectile>();
        SpriteRenderer rootSr = go.GetComponent<SpriteRenderer>();
        if (rootSr != null)
        {
            rootSr.enabled = false;
        }

        rico.speed = 4f;
        rico.contactDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.GetUnit24ContactDamageMul(evolutionLevel)));
        rico.hitRadius = 0.5f;
        rico.wallCastRadius = 0.14f;
        rico.reflectsOnAllyBarrier = false;
        rico.rotateToFaceDirection = false;
        rico.maxLifetime = 5f * UnitEvolutionMilestones.GetUnit24LifetimeMul(evolutionLevel);
        rico.speedRampPerSec = UnitEvolutionMilestones.HasUnit24SpeedRamp(evolutionLevel) ? UnitEvolutionMilestones.Unit24SpeedRampPerSec : 0f;
        rico.sourceUnitNumber = unitNumber;
        rico.Launch(dir);
    }

    void CreateGiantFistAoEIndicator(Vector3 center, float radius)
    {
        GameObject aoeObj = new GameObject("Unit17AoE");
        aoeObj.transform.position = center;

        SpriteRenderer renderer = aoeObj.AddComponent<SpriteRenderer>();
        Color aoeColor = new Color(0.6f, 0.2f, 0.8f, 0.35f);
        renderer.sprite = CreateCircleSprite(aoeColor);
        renderer.color = aoeColor;
        renderer.sortingOrder = 2;

        float diameter = radius * 2f;
        aoeObj.transform.localScale = new Vector3(diameter, diameter, 1f);

        Destroy(aoeObj, 0.25f);
    }
    
    /// <summary>
    /// 10번 유닛: 분열 총알 발사
    /// </summary>
    void FireSplitProjectile(Zombie target)
    {
        GameObject projObj = new GameObject("SplitProjectile");
        projObj.transform.position = transform.position;
        
        // 스프라이트 렌더러
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;
        UnitProjectileVisuals.TryApply(spriteRenderer, 10);
        projObj.transform.localScale = Vector3.one * UnitProjectileVisuals.Unit10ProjectileScale;

        SplitProjectile splitProj = projObj.AddComponent<SplitProjectile>();
        splitProj.speed = projectileSpeed;
        splitProj.damage = GetDamageWithUpgrade(2);
        splitProj.splitDamage = GetDamageWithUpgrade(2);
        splitProj.sourceUnitNumber = unitNumber;
        splitProj.SetTarget(target);
    }
    
    System.Collections.IEnumerator FireBurst4()
    {
        int shots = 4;
        float interval = 0.15f;
        
        for (int i = 0; i < shots; i++)
        {
            FireBurstProjectile();
            yield return new WaitForSeconds(interval);
        }
        
    }
    
    void FireBurstProjectile()
    {
        GameObject projObj = new GameObject("BurstProjectile");
        projObj.transform.position = transform.position;
        
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;
        UnitProjectileVisuals.TryApply(spriteRenderer, 10);
        projObj.transform.localScale = Vector3.one * (UnitProjectileVisuals.Unit10ProjectileScale * UnitProjectileVisuals.Unit10ChildProjectileScaleFactor);

        StraightProjectile proj = projObj.AddComponent<StraightProjectile>();
        proj.speed = projectileSpeed;
        proj.direction = Vector2.right;
        proj.damage = GetDamageWithUpgrade(1);
        proj.sourceUnitNumber = unitNumber;
        proj.hitEffectScale = Vector3.one * 0.6f;
    }

    /// <summary>
    /// 11번 유닛: 기절 운석 낙하
    /// </summary>
    void DropStunMeteor(Zombie target)
    {
        Vector3 targetPos = target.transform.position;
        
        // 운석 이펙트 (위에서 아래로)
        Vector3 start = targetPos + new Vector3(0f, 2.5f, 0f);
        CreateMeteorEffect(start, targetPos);
        
        // 운석 데미지
        target.TakeDamage(GetDamageWithUpgrade(2), unitNumber);
        
        // 40% 확률 기절 (2초)
        if (Random.value <= 0.4f)
        {
            target.ApplyStun(2f);
        }
    }
    
    void CreateMeteorEffect(Vector3 from, Vector3 to)
    {
        GameObject effectObj = new GameObject("MeteorEffect");
        LineRenderer line = effectObj.AddComponent<LineRenderer>();
        
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = 0.1f;
        line.endWidth = 0.2f;
        line.sortingOrder = 3;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 0.5f, 0.2f);
        line.endColor = new Color(1f, 0.5f, 0.2f);
        
        Destroy(effectObj, 0.2f);
    }
    
    /// <summary>
    /// 5번 유닛: 번개 낙하 및 범위 데미지
    /// </summary>
    void FireLightningStrike(Zombie target)
    {
        if (target == null) return;

        int strikeCount = UnitEvolutionMilestones.GetUnit5StrikeCount(evolutionLevel);
        float radius = UnitEvolutionMilestones.GetUnit5StrikeRadius(evolutionLevel);
        float hitDelay = LightningFrameTime * LightningHitFrameIndex;
        StartCoroutine(FireUnit5LightningStrikeRoutine(target, strikeCount, radius, hitDelay));
    }

    System.Collections.IEnumerator FireUnit5LightningStrikeRoutine(Zombie primaryTarget, int strikeCount, float radius, float hitDelay)
    {
        int primaryDamage = GetDamageWithUpgrade(2);
        int extraDamage = Mathf.Max(1, Mathf.RoundToInt(primaryDamage * UnitEvolutionMilestones.Unit5ExtraStrikeDamageMul));

        for (int i = 0; i < strikeCount; i++)
        {
            Zombie strikeTarget = i == 0 ? primaryTarget : FindRandomZombie();
            if (strikeTarget == null) strikeTarget = primaryTarget;
            if (strikeTarget == null) yield break;

            Vector3 end = strikeTarget.transform.position;
            Vector3 start = end + new Vector3(0f, 2.5f, 0f);
            CreateLightningEffect(start, end);

            int damage = i == 0 ? primaryDamage : extraDamage;
            StartCoroutine(ApplyLightningDamageAfterDelay(end, radius, hitDelay, damage));

            if (i < strikeCount - 1)
            {
                yield return new WaitForSeconds(UnitEvolutionMilestones.Unit5StrikeStagger);
            }
        }
    }
    
    /// <summary>
    /// 번개 이펙트 생성
    /// </summary>
    void CreateLightningEffect(Vector3 from, Vector3 to)
    {
        Sprite[] frames = LoadSpriteFrames("unit_005_attack");
        if (frames.Length == 0)
        {
            GameObject lineEffectObj = new GameObject("LightningEffect");
            LineRenderer line = lineEffectObj.AddComponent<LineRenderer>();
            
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.sortingOrder = 3;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.5f, 0.9f, 1f);
            line.endColor = new Color(0.5f, 0.9f, 1f);
            
            Destroy(lineEffectObj, 0.2f);
            return;
        }

        GameObject effectObj = new GameObject("LightningEffect");
        effectObj.transform.position = to;
        SpriteRenderer renderer = effectObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 4;
        renderer.sprite = frames[0];
        renderer.color = Color.white;
        effectObj.transform.localScale = Vector3.one * 2f;

        StartCoroutine(PlayLightningFrames(effectObj, renderer, frames, LightningFrameTime, to));
    }

    System.Collections.IEnumerator PlayLightningFrames(GameObject effectObj, SpriteRenderer renderer, Sprite[] frames, float frameTime, Vector3 basePosition)
    {
        int referenceIndex = Mathf.Min(3, frames.Length - 1);
        int hitIndex = Mathf.Min(LightningHitFrameIndex, frames.Length - 1);
        float referenceTop = frames[referenceIndex].bounds.max.y;
        float baseOffset = -frames[hitIndex].bounds.min.y;
        float hitExtra = referenceTop - frames[hitIndex].bounds.max.y;

        for (int i = 0; i < frames.Length; i++)
        {
            if (renderer == null) yield break;
            renderer.sprite = frames[i];
            float offsetY = baseOffset + (referenceTop - frames[i].bounds.max.y) - hitExtra;
            if (effectObj != null)
            {
                effectObj.transform.position = basePosition + new Vector3(0f, offsetY, 0f);
            }
            yield return new WaitForSeconds(frameTime);
        }

        if (effectObj != null)
        {
            Destroy(effectObj);
        }
    }

    System.Collections.IEnumerator ApplyLightningDamageAfterDelay(Vector3 center, float radius, float delay, int damage)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        CreateAoEIndicator(center, radius);

        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        for (int i = 0; i < _zombieSearchBuffer.Count; i++)
        {
            Zombie zombie = _zombieSearchBuffer[i];
            float dist = Vector2.Distance(center, zombie.transform.position);
            if (dist <= radius)
            {
                zombie.TakeDamage(damage, unitNumber);
            }
        }
    }
    
    /// <summary>
    /// 범위 데미지 표시용 임시 도형 생성
    /// </summary>
    void CreateAoEIndicator(Vector3 center, float radius)
    {
        GameObject aoeObj = new GameObject("AoEIndicator");
        aoeObj.transform.position = center;
        
        SpriteRenderer renderer = aoeObj.AddComponent<SpriteRenderer>();
        Color aoeColor = new Color(0.5f, 0.9f, 1f, 0.3f);
        renderer.sprite = CreateCircleSprite(aoeColor);
        renderer.color = aoeColor;
        renderer.sortingOrder = 2;
        
        float diameter = radius * 2f;
        aoeObj.transform.localScale = new Vector3(diameter, diameter, 1f);
        
        Destroy(aoeObj, 0.2f);
    }

    void FireUnit22RootPad()
    {
        Zombie target = FindRandomZombie();
        if (target == null)
        {
            return;
        }

        Vector3 center = target.transform.position;
        float radius = transform.localScale.x * 1.6f * UnitEvolutionMilestones.GetUnit22RadiusMul(evolutionLevel);
        SpawnUnit22RootPadAt(center, radius, GetDamageWithUpgrade(3), UnitEvolutionMilestones.GetUnit22StunDuration(evolutionLevel));

        if (UnitEvolutionMilestones.HasUnit22TwinPad(evolutionLevel))
        {
            int secondDamage = Mathf.Max(1, Mathf.RoundToInt(GetDamageWithUpgrade(3) * UnitEvolutionMilestones.Unit22SecondPadDamageMul));
            StartCoroutine(SpawnUnit22SecondPadRoutine(center, radius, secondDamage));
        }
    }

    void SpawnUnit22RootPadAt(Vector3 center, float radius, int damage, float stunDuration)
    {
        if (unit22RootPad != null) Destroy(unit22RootPad);
        unit22RootPad = new GameObject("Unit22RootPad");
        unit22RootPad.transform.position = center;
        SpriteRenderer renderer = unit22RootPad.AddComponent<SpriteRenderer>();
        Color padColor = new Color(0.2f, 0.8f, 0.2f, Unit22RootPadAlpha);
        renderer.sprite = CreateCircleSprite(padColor);
        renderer.color = padColor;
        renderer.sortingOrder = 1;
        float diameter = radius * 2f;
        unit22RootPad.transform.localScale = new Vector3(diameter, diameter, 1f);
        StartCoroutine(Unit22RootPadRoutine(center, radius, damage, stunDuration));
    }

    System.Collections.IEnumerator SpawnUnit22SecondPadRoutine(Vector3 center, float radius, int damage)
    {
        yield return new WaitForSeconds(UnitEvolutionMilestones.Unit22TwinPadDelay);
        Zombie secondTarget = FindRandomZombie();
        Vector3 secondCenter = secondTarget != null ? secondTarget.transform.position : center;
        SpawnUnit22RootPadAt(secondCenter, radius, damage, UnitEvolutionMilestones.GetUnit22StunDuration(evolutionLevel));
    }

    System.Collections.IEnumerator Unit22RootPadRoutine(Vector3 center, float radius, int damage, float stunDuration)
    {
        yield return new WaitForSeconds(Unit22RootPadDuration);
        Zombie.CopyLivingZombiesTo(_zombieSearchBuffer);
        int maxTargets = UnitEvolutionMilestones.Unit22MaxStunTargets;
        _zombieSearchBuffer.Sort((a, b) =>
        {
            float da = Vector2.Distance(center, a.transform.position);
            float db = Vector2.Distance(center, b.transform.position);
            return da.CompareTo(db);
        });

        int applied = 0;
        for (int i = 0; i < _zombieSearchBuffer.Count && applied < maxTargets; i++)
        {
            Zombie zombie = _zombieSearchBuffer[i];
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float distance = Vector2.Distance(center, zombie.transform.position);
            if (distance > radius) continue;

            zombie.TakeDamage(damage, unitNumber);
            zombie.ApplyStun(stunDuration);
            StartCoroutine(SpawnUnit22RootVine(zombie.transform));
            applied++;
        }
        if (unit22RootPad != null)
        {
            Destroy(unit22RootPad);
            unit22RootPad = null;
        }
    }

    System.Collections.IEnumerator SpawnUnit22RootVine(Transform target)
    {
        if (target == null) yield break;

        GameObject vineObj = new GameObject("Unit22RootVine");
        vineObj.transform.position = target.position;
        vineObj.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer renderer = vineObj.AddComponent<SpriteRenderer>();
        Sprite[] frames = Resources.LoadAll<Sprite>("unit_022_attack");
        if (frames == null || frames.Length == 0)
        {
            Sprite single = Resources.Load<Sprite>("unit_022_attack");
            if (single != null)
            {
                frames = new Sprite[] { single };
            }
        }
        if (frames != null && frames.Length > 0)
        {
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            renderer.sprite = frames[0];
        }
        renderer.sortingOrder = 2;

        float frameTime = 0.06f;
        float timer = 0f;
        int frameIndex = 0;
        float maxFrameTime = frameTime * 8f;

        while (target != null && timer < maxFrameTime)
        {
            if (frames != null && frames.Length > 0)
            {
                renderer.sprite = frames[Mathf.Min(frameIndex, frames.Length - 1)];
            }
            frameIndex++;
            timer += frameTime;
            vineObj.transform.position = GetUnit22RootPosition(target, renderer);
            yield return new WaitForSeconds(frameTime);
        }

        float stunEndTime = Time.time + Unit22RootStunDuration;
        if (frames != null && frames.Length > 0)
        {
            renderer.sprite = frames[Mathf.Min(7, frames.Length - 1)];
        }

        while (target != null && Time.time < stunEndTime)
        {
            vineObj.transform.position = GetUnit22RootPosition(target, renderer);
            yield return null;
        }

        if (vineObj != null)
        {
            Destroy(vineObj);
        }
    }

    Vector3 GetUnit22RootPosition(Transform target, SpriteRenderer rootRenderer)
    {
        if (target == null) return Vector3.zero;

        Vector3 basePos = target.position;
        SpriteRenderer targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null)
        {
            float targetMinY = targetRenderer.bounds.min.y;
            float rootHalfHeight = rootRenderer != null ? rootRenderer.bounds.extents.y : 0f;
            return new Vector3(basePos.x, targetMinY + rootHalfHeight, basePos.z);
        }

        return basePos;
    }

    Sprite CreateCircleSprite(Color color)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float radius = (size - 1) * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        Color transparent = new Color(0f, 0f, 0f, 0f);
        float radiusSquared = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                texture.SetPixel(x, y, pos.sqrMagnitude <= radiusSquared ? color : transparent);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    /// <summary>
    /// 총알 프리팹을 생성합니다
    /// </summary>
    GameObject CreateProjectilePrefab()
    {
        GameObject projObj = new GameObject("Projectile");

        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;

        if (unitNumber == 1)
        {
            Sprite unit1AttackSprite = Resources.Load<Sprite>("unit_1_attack");
            if (unit1AttackSprite != null)
            {
                spriteRenderer.sprite = unit1AttackSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = Color.yellow;
                spriteRenderer.sprite = CreateSquareSprite(Color.yellow);
            }
            projObj.transform.localScale = GetScaledProjectileSize(Vector3.one * 0.3f);
        }
        else if (unitNumber == 15)
        {
            const string attackSpriteName = "unit_015_attack";
            Sprite attackSprite = Resources.Load<Sprite>(attackSpriteName);
            if (attackSprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(attackSpriteName);
                if (sprites != null && sprites.Length > 0)
                {
                    attackSprite = sprites[0];
                }
            }
            if (attackSprite != null)
            {
                spriteRenderer.sprite = attackSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
                spriteRenderer.sprite = CreateSquareSprite(spriteRenderer.color);
            }
            projObj.transform.localScale = GetScaledProjectileSize(Vector3.one * 0.3f);
        }
        else if (unitNumber == 2 || unitNumber == 3 || unitNumber == 6)
        {
            string attackSpriteName = unitNumber == 2 ? "unit_2_attack"
                : unitNumber == 3 ? "unit_3_attack"
                : "unit_6_attack";
            Sprite attackSprite = Resources.Load<Sprite>(attackSpriteName);
            if (attackSprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(attackSpriteName);
                if (sprites != null && sprites.Length > 0)
                {
                    attackSprite = sprites[0];
                }
            }
            if (attackSprite != null)
            {
                spriteRenderer.sprite = attackSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
                spriteRenderer.sprite = CreateSquareSprite(spriteRenderer.color);
            }
            projObj.transform.localScale = GetScaledProjectileSize(GetProjectileScale());
        }
        else if (unitNumber == 26)
        {
            const string attackSpriteName = "unit_026_attack";
            Sprite attackSprite = Resources.Load<Sprite>(attackSpriteName);
            if (attackSprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(attackSpriteName);
                if (sprites != null && sprites.Length > 0)
                {
                    System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
                    attackSprite = sprites[0];
                }
            }
            if (attackSprite != null)
            {
                spriteRenderer.sprite = attackSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
                spriteRenderer.sprite = CreateSquareSprite(spriteRenderer.color);
            }
            projObj.transform.localScale = GetScaledProjectileSize(GetProjectileScale());
        }
        else if (unitNumber == 10)
        {
            UnitProjectileVisuals.TryApply(spriteRenderer, 10);
            projObj.transform.localScale = GetScaledProjectileSize(Vector3.one * UnitProjectileVisuals.Unit10ProjectileScale);
        }
        else
        {
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sprite = CreateSquareSprite(Color.yellow);
            projObj.transform.localScale = GetScaledProjectileSize(Vector3.one * 0.3f);
        }

        Projectile proj = projObj.AddComponent<Projectile>();
        proj.speed = projectileSpeed;
        if (unitNumber == 15)
        {
            proj.ConfigureAsUnit15Projectile();
        }

        Vector3 hitEffectScale = unitNumber == 15
            ? Vector3.one * 0.6f
            : GetProjectileScale() * 2f;
        proj.SetHitEffectFrames(
            AttackHitEffectFire.DefaultResourcePath,
            4,
            0.06f,
            hitEffectScale,
            Color.white);

        projObj.SetActive(false);
        return projObj;
    }

    
    /// <summary>
    /// 오른쪽 방향으로 일직선 투사체를 발사합니다 (주황색용)
    /// </summary>
    void FireStraightProjectile()
    {
        GameObject projObj = new GameObject("StraightProjectile");
        projObj.transform.position = transform.position;
        
        // 스프라이트 렌더러
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;

        // 1번 유닛: Warped Shooting Fx 애니메이션 적용
        ApplyWarpedProjectileVisuals(projObj, unitNumber, new Color(1f, 0.5f, 0f));
        // 오른쪽 발사 방향으로 보이도록 좌우 반전
        spriteRenderer.flipX = true;
        
        // 크기는 작게
        // 캐릭터 크기만큼 발사체 크기 맞춤
        projObj.transform.localScale = transform.localScale;
        
        // Collider 추가 (충돌 감지용)
        BoxCollider2D collider = projObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one * 0.3f;
        
        // StraightProjectile 컴포넌트 추가
        StraightProjectile straightProj = projObj.AddComponent<StraightProjectile>();
        straightProj.speed = projectileSpeed;
        straightProj.direction = Vector2.right; // 오른쪽 방향
        straightProj.damage = GetDamageWithUpgrade(3);
        straightProj.sourceUnitNumber = unitNumber;
        straightProj.hitEffectScale = transform.localScale;
    }

    void ApplyWarpedProjectileVisuals(GameObject projObj, int projectileUnitNumber, Color fallbackColor)
    {
        EnsureProjectileVisualsLoaded();

        SpriteRenderer renderer = projObj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = projObj.AddComponent<SpriteRenderer>();
        }
        renderer.sortingOrder = 2;
        renderer.color = Color.white;

        RuntimeAnimatorController controller = null;
        Sprite fallbackSprite = null;

        if (projectileUnitNumber == 1)
        {
            controller = unit1ProjectileController;
            fallbackSprite = unit1ProjectileFallbackSprite;
        }
        else if (projectileUnitNumber == 2)
        {
            controller = unit2ProjectileController;
            fallbackSprite = unit2ProjectileFallbackSprite;
        }

        if (controller != null)
        {
            Animator animator = projObj.GetComponent<Animator>();
            if (animator == null)
            {
                animator = projObj.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            if (renderer.sprite == null && fallbackSprite != null)
            {
                renderer.sprite = fallbackSprite;
            }
        }
        else
        {
            renderer.color = fallbackColor;
            renderer.sprite = CreateSquareSprite(fallbackColor);
        }
    }

    void EnsureProjectileVisualsLoaded()
    {
        if (projectileVisualsLoaded)
        {
            return;
        }

        unit1ProjectileController = Resources.Load<RuntimeAnimatorController>(
            "Warped Shooting Fx/Pixel Art/Bolt/bolt1 (1)");
        unit2ProjectileController = Resources.Load<RuntimeAnimatorController>(
            "Warped Shooting Fx/Pixel Art/Charged/charged6");

        unit1ProjectileFallbackSprite = Resources.Load<Sprite>(
            "Warped Shooting Fx/Pixel Art/Bolt/bolt1");
        unit2ProjectileFallbackSprite = Resources.Load<Sprite>(
            "Warped Shooting Fx/Pixel Art/Charged/charged1");

        projectileVisualsLoaded = true;
    }
    
    /// <summary>
    /// 관통 레이저를 발사합니다 (빨간색용)
    /// </summary>
    void FireLaser()
    {
        GameObject laserObj = new GameObject("Laser");
        laserObj.transform.position = transform.position;
        
        // 레이저 시각화 (긴 사각형)
        SpriteRenderer spriteRenderer = laserObj.AddComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(1f, 0f, 0f, 0.8f); // 반투명 빨간색
        spriteRenderer.sprite = CreateSquareSprite(Color.red);
        spriteRenderer.sortingOrder = 2;
        
        // 레이저 크기 (가로로 긴 직사각형)
        laserObj.transform.localScale = new Vector3(50f, 0.2f, 1f);
        laserObj.transform.rotation = Quaternion.Euler(0, 0, 0); // 오른쪽 방향
        
        // Laser 컴포넌트 추가
        Laser laser = laserObj.AddComponent<Laser>();
        laser.damage = 999;
        laser.sourceUnitNumber = unitNumber;
        laser.range = 50f;
        laser.duration = 0.3f;
    }
    
    /// <summary>
    /// 위아래로 긴 투사체를 발사합니다 (보라색용)
    /// </summary>
    void FireTallProjectile()
    {
        GameObject projObj = new GameObject("TallProjectile");
        projObj.transform.position = transform.position;
        
        // 스프라이트 렌더러
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(0.5f, 0f, 0.5f); // 보라색
        spriteRenderer.sprite = CreateSquareSprite(new Color(0.5f, 0f, 0.5f));
        spriteRenderer.sortingOrder = 2;
        
        // 위아래로 긴 크기 (세로로 긴 직사각형)
        projObj.transform.localScale = new Vector3(0.3f, 2f, 1f);
        
        // Collider 추가
        BoxCollider2D collider = projObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.3f, 2f);
        
        // StraightProjectile 컴포넌트 추가
        StraightProjectile straightProj = projObj.AddComponent<StraightProjectile>();
        straightProj.speed = projectileSpeed; // 느린 속도 (3f로 설정됨)
        straightProj.direction = Vector2.right; // 오른쪽 방향
        straightProj.damage = GetDamageWithUpgrade(3);
        straightProj.sourceUnitNumber = unitNumber;
        straightProj.hitEffectScale = projObj.transform.localScale;
    }
    
    /// <summary>
    /// 4번 유닛: 골드 아이콘 생성
    /// </summary>
    void TrySpawnGoldIcon()
    {
        if (hasGoldIcon) return;
        if (Time.time < nextGoldSpawnTime) return;

        Sprite goldSprite = GoldSpriteVisual.LoadGoldSprite();
        float hudWorldScale = GoldSpriteVisual.GetHudMatchedWorldScale(goldSprite, Camera.main);
        float parentScale = GoldSpriteVisual.GetLossyScaleCompensation(transform);
        Vector3 visualLocalPos = Vector3.zero;

        GameObject goldObj = new GameObject("GoldIcon");
        goldObj.transform.SetParent(transform, false);
        goldObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        goldObj.transform.localScale = Vector3.one * (hudWorldScale / parentScale);

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(goldObj.transform, false);
        visualObj.transform.localPosition = visualLocalPos;
        visualObj.transform.localRotation = Quaternion.identity;
        visualObj.transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = visualObj.AddComponent<SpriteRenderer>();
        if (goldSprite != null)
        {
            GoldSpriteVisual.ApplySpriteRenderer(spriteRenderer, goldSprite);
        }
        else
        {
            spriteRenderer.sprite = CreateSquareSprite(new Color(1f, 0.85f, 0.1f));
            spriteRenderer.color = new Color(1f, 0.85f, 0.1f);
            spriteRenderer.sortingOrder = 3;
        }

        BoxCollider2D collider = visualObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size = GoldSpriteVisual.GetPickColliderSize(spriteRenderer.sprite);

        GoldIcon goldIcon = goldObj.AddComponent<GoldIcon>();
        goldIcon.Initialize(this, visualObj.transform, collider, visualLocalPos);
        goldIcon.SetGoldAmount(UnitEvolutionMilestones.GetUnit4GoldAmount(evolutionLevel));
        if (UnitEvolutionMilestones.HasUnit4AutoCollect(evolutionLevel))
        {
            goldIcon.EnableAutoCollect(0.35f);
        }

        hasGoldIcon = true;
    }

    /// <summary>
    /// 4번 유닛: 골드 아이콘이 파괴되었을 때(미수집) 상태 정리
    /// </summary>
    public void NotifyGoldIconRemoved()
    {
        hasGoldIcon = false;
    }

    /// <summary>
    /// 4번 유닛: 골드 획득 후 쿨다운 갱신
    /// </summary>
    public void OnGoldCollected()
    {
        hasGoldIcon = false;
        nextGoldSpawnTime = Time.time + goldSpawnCooldown;
    }
}

