using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좀비 기본 클래스
/// </summary>
public class Zombie : MonoBehaviour
{
    [Header("Zombie Data")]
    public ZombieData zombieData; // 좀비 데이터
    [Tooltip("스폰 시 전달되는 좀비 UID")]
    public string zombieUID;
    
    [Header("Zombie Stats")]
    public int health = 100;
    public int maxHealth = 100;
    public float moveSpeed = 1f;
    public int damage = 10;
    
    [Header("Attack")]
    public float attackRange = 0.5f;
    public float attackCooldown = 1f;
    
    public int currentRow = 0;
    private float lastAttackTime = 0f;
    private Plant targetPlant = null;
    private Barrier targetBarrier = null;
    private bool isDead = false;
    /// <summary>외부 스크립트(투사체 등)에서 사망 여부 확인용</summary>
    public bool IsDead => isDead;
    private Transform hpBarFill;
    private Transform hpBarContainer;
    private SpriteRenderer hpBarShellRenderer;
    private SpriteRenderer hpBarTrackRenderer;
    private SpriteRenderer hpBarFillRenderer;
    private TextMesh bossHpText;
    private bool usesBossHpBar;
    private TextMesh bossLabelText;
    private Transform bossLabelTransform;
    private float bossLabelBaseY;
    private float bossLabelBobPhase;
    private Transform bossOutlineHost;
    static readonly Color BossOutlineColor = new Color(1f, 0.18f, 0.18f, 1f);
    static readonly Color BossLabelColor = new Color(1f, 0.18f, 0.14f, 1f);
    static readonly Color BossHpFillColor = new Color(1f, 0.12f, 0.10f, 1f);
    static readonly Color BossHpTrackColor = new Color(0.42f, 0.06f, 0.06f, 1f);
    static readonly Color BossHpShellColor = new Color(1f, 0.42f, 0.34f, 1f);
    static readonly Color BossHpTextColor = new Color(1f, 0.92f, 0.88f, 1f);
    const int BossHpBarSortingShell = 80;
    const int BossHpBarSortingTrack = 81;
    const int BossHpBarSortingFill = 82;
    const int BossHpBarSortingText = 84;
    const float BossLabelBobAmplitude = 0.07f;
    const float BossLabelBobSpeed = 2.6f;
    private float baseMoveSpeed = 1f;
    private float slowEndTime = 0f;
    private float stunEndTime = 0f;
    private float burnEndTime = 0f;
    private float burnNextTickTime = 0f;
    private int burnDamagePerTick = 0;
    private float calamityMarkEndTime = 0f;
    private float calamityMarkDamageBonus = 0f;
    private bool calamityMarkChainOnDeath;
    private int calamityMarkChainDamage;
    private int calamityMarkSourceUnit;
    private GameObject calamityMarkOverlay;
    private float calamityMarkBlinkTimer;
    private bool calamityMarkBlinkOn;
    static readonly Color CalamityMarkTint = new Color(0.82f, 0.28f, 0.62f, 1f);
    private bool wardenCritConsumed = false; // 파수꾼 첫 타 치명타(좀비당 1회)
    private bool unit11FreezeActive;
    private GameObject unit11FreezeOverlay;
    private float unit11FreezeBlinkTimer;
    private bool unit11FreezeBlinkOn;
    private float knockbackOffsetX = 0f;
    private float knockbackVelocity = 0f;
    private const float KnockbackDamping = 8f;
    private SpriteRenderer spriteRenderer;
    private UnitSpriteAnimator spriteAnimator;
    private Color originalColor = Color.red;
    private float slowBlinkTimer = 0f;
    private float slowBlinkInterval = 0.2f;
    private bool slowBlinkOn = false;
    private float stunBlinkTimer = 0f;
    private float stunBlinkInterval = 0.2f;
    private bool stunBlinkOn = false;
    private float damageFlashEndTime = 0f;
    private const float DamageFlashDuration = 0.08f;
    private float baseY;
    private float baseX;
    private float baseRotationZ;
    private string currentMonPig1Animation;
    private bool combatPauseVisualsActive;
    private Color combatPauseSavedColor = Color.white;
    private Coroutine deathCoroutine;
    private const float monPig1Fps = 6f;
    /// <summary>유닛 12 경로 끌기: true면 MoveLeft(좌이동) 스킵</summary>
    private bool unit12PathDragged;

    [Header("Enemy2 (mon_pig_2) ball shot")]
    [Tooltip("공격 모션에서 enemy_2_Attack-export_2 프레임에 가깝게 공 발사 (기본 ≈ 6FPS 시 2프레임)")]
    [SerializeField] private float enemy2BallSpawnDelay = 0.33f;
    private float enemy2NextSpecialAt;
    private bool enemy2SpecialMoveLock;
    private Coroutine enemy2SpecialRoutine;
    private float enemy2CachedCellSize = -1f;

    private float pig5NextSteerAt;
    private float pig5SteerY;
    private ZombieSpawner pig5CachedSpawner;

    private float pig6NextShieldAt;
    private bool pig6ShieldCastLock;
    private Coroutine pig6ShieldRoutine;

    /// <summary>에너미6이 다른 적 좀비에게 부여하는 보호막(추가 체력)</summary>
    private int enemy6ShieldHp;
    private float enemy6ShieldExpireTime;

    /// <summary>에너미9: 첫 번째 기절 부활 가능 여부(1회만)</summary>
    private bool enemy9ReviveAvailable = true;
    /// <summary>enemy_9_death-export_3 유지 중 — 타깃·피격 제외</summary>
    private bool enemy9CorpsePhase;
    private Coroutine enemy9ReviveRoutine;

    private const string Enemy9Walk = "enemy_9_walk-export";
    private const string Enemy9Attack = "enemy_9_attack-export";
    private const string Enemy9DeathSheet = "enemy_9_death-export";
    private const string Enemy9DeathHoldSpriteName = "enemy_9_death-export_3";

    /// <summary>에너미10: 투명화 구간 — 타깃·피격 제외</summary>
    private bool enemy10StealthPhase;
    private float enemy10NextStealthEvalAt;
    private float enemy10StealthEndAt;

    private const string Enemy10Walk = "enemy_10_walk-export";
    private const string Enemy10Attack = "enemy_10_attack-export";
    private const string Enemy10DeathSheet = "enemy_10_death-export";

    /// <summary>에너미9 시체 프레임 유지 중 · 에너미10 투명화 중 — 플레이어 타깃·데미지 제외</summary>
    public bool IsExcludedFromCombat => enemy9CorpsePhase || enemy10StealthPhase;

    /// <summary>보스·정예(돼지/네크로 등) 여부 — 즉사/처형 효과 제외용</summary>
    public bool IsBossLike() => UsesEnemySheetAnimation();

    [Header("Movement Sway")]
    public float swayRotationDegrees = 5f;
    public float swaySpeed = 6f;
    
    [Header("Crowd (겹침 방지)")]
    [Tooltip("0 = 끔(적끼리 겹침 허용). 0이 아니면 겹침 시 매 프레임 base를 서로 밀어 냄(클수록 빠르게 풀림)")]
    [SerializeField] [Range(0f, 1f)] private float crowdSeparationStiffness = 0f;
    private const int CrowdSeparationPasses = 2;
    private static readonly List<Zombie> s_activeZombies = new List<Zombie>(32);
    private static int s_lastZombieSeparationFrame = -1;

    /// <summary>
    /// 살아있는 좀비만 리스트에 채웁니다. 라운드 후반 대량 스폰 시 FindObjectsByType 전역 검색 대신 사용합니다.
    /// </summary>
    public static void CopyLivingZombiesTo(List<Zombie> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        for (int i = 0; i < s_activeZombies.Count; i++)
        {
            Zombie z = s_activeZombies[i];
            if (z != null && !z.IsDead && !z.IsExcludedFromCombat)
            {
                buffer.Add(z);
            }
        }
    }
    
    private const string Boss1Walk = "Demon_Boss_walk-export";
    private const string Boss1AttackBarrier = "Demon_Boss_attack1-export";
    private const string Boss1AttackShove = "Demon_Boss_attack4-export";
    private const string Boss1Death = "Demon_Boss_death-export";

    private const string OozeBossWalk = "Ooze_boss_walk-export";
    private const string OozeBossAttackBarrier = "Ooze_boss_attack4-export";
    private const string OozeBossAttackVertical = "Ooze_boss_attack1-export";
    private const string OozeBossAttackSummon = "Ooze_boss_attack2-export";
    private const string OozeBossDeathPrimary = "Ooze_boss_death-export";
    private const string OozeBossDeathFallback = "Ooze_boss_death-export";

    private float boss1NextVerticalAt;
    private float boss1NextShoveAt;
    private bool boss1ActionLock;
    private Coroutine boss1VertRoutine;
    private Coroutine boss1ShoveRoutine;
    private const int Boss1KnockColumns = 2;
    private const float Boss1ShoveLateralRange = 3.2f;

    private float oozeBossNextSpecialAt;
    private bool oozeBossActionLock;
    private Coroutine oozeBossRoutine;
    private ZombieSpawner oozeBossCachedSpawner;

    /// <summary>라운드30 네크로맨서 보스</summary>
    private Coroutine necromancerPatternRoutine;
    private float necromancerNextPatternAt;
    private bool necromancerSiegeMode;
    private bool necromancerSiegeIdleDone;
    private ZombieSpawner necromancerCachedSpawner;

    private const string NecromancerWalk = "Necromancer_walk-export";
    private const string NecromancerIdle = "Necromancer_idle-export";
    private const string NecromancerDeath = "Necromancer_death-export";
    private const string NecromancerAtk1 = "Necromancer_attack1-export";
    private const string NecromancerAtk2 = "Necromancer_attack2-export";
    private const string NecromancerAtk3 = "Necromancer_attack3-export";
    private const string NecromancerAtk4 = "Necromancer_attack4-export";
    
    void OnEnable()
    {
        s_activeZombies.Add(this);
        wardenCritConsumed = false;
    }

    /// <summary>파수꾼 첫 타 치명타용: 아직 치명타가 적용되지 않았으면 true 반환 후 소비.</summary>
    public bool TryConsumeWardenCrit()
    {
        if (wardenCritConsumed) return false;
        wardenCritConsumed = true;
        return true;
    }
    
    void OnDisable()
    {
        if (enemy2SpecialRoutine != null)
        {
            StopCoroutine(enemy2SpecialRoutine);
            enemy2SpecialRoutine = null;
        }
        enemy2SpecialMoveLock = false;
        s_activeZombies.Remove(this);
        if (boss1VertRoutine != null)
        {
            StopCoroutine(boss1VertRoutine);
            boss1VertRoutine = null;
        }
        if (boss1ShoveRoutine != null)
        {
            StopCoroutine(boss1ShoveRoutine);
            boss1ShoveRoutine = null;
        }
        boss1ActionLock = false;
        if (oozeBossRoutine != null)
        {
            StopCoroutine(oozeBossRoutine);
            oozeBossRoutine = null;
        }
        oozeBossActionLock = false;
        if (pig6ShieldRoutine != null)
        {
            StopCoroutine(pig6ShieldRoutine);
            pig6ShieldRoutine = null;
        }
        pig6ShieldCastLock = false;
        if (enemy9ReviveRoutine != null)
        {
            StopCoroutine(enemy9ReviveRoutine);
            enemy9ReviveRoutine = null;
        }
        enemy9CorpsePhase = false;
        enemy10StealthPhase = false;
        if (necromancerPatternRoutine != null)
        {
            StopCoroutine(necromancerPatternRoutine);
            necromancerPatternRoutine = null;
        }
        ClearBossOutline();
    }
    
    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            if (UsesEnemySheetAnimation())
            {
                spriteRenderer.color = Color.white;
                originalColor = Color.white;
            }
        }

        InitializeMovementAnimation();
        EnsureWalkSpriteVisible();
        
        // 데이터가 있으면 데이터에서 속성 가져오기
        if (zombieData != null)
        {
            maxHealth = zombieData.maxHealth > 0 ? zombieData.maxHealth : zombieData.health;
            health = zombieData.health;
            moveSpeed = zombieData.moveSpeed;
            damage = zombieData.damage;
            attackRange = zombieData.attackRange;
            attackCooldown = zombieData.attackCooldown;
            
            // 리소스명이 있으면 스프라이트 로드 시도 (나중에 구현 가능)
            // 현재는 기본 사각형 사용
        }
        else
        {
            // 데이터가 없으면 기본값 사용
            health = maxHealth;
        }

        if (IsNecromancerBoss())
        {
            maxHealth = zombieData != null && zombieData.maxHealth > 0 ? zombieData.maxHealth : 50;
            health = zombieData != null && zombieData.health > 0 ? zombieData.health : maxHealth;
            damage = 0;
        }
        
        if (IsBoss1())
        {
            float t = UnityEngine.Random.Range(2f, 3f);
            boss1NextVerticalAt = Time.time + t;
            boss1NextShoveAt = Time.time + t;
        }

        if (IsOozeBoss())
        {
            oozeBossNextSpecialAt = Time.time + UnityEngine.Random.Range(3f, 5f);
            oozeBossCachedSpawner = FindFirstObjectByType<ZombieSpawner>();
        }
        
        baseMoveSpeed = moveSpeed * EnemyCombatStats.MoveSpeedWorldScale;
        baseY = transform.position.y;
        baseX = transform.position.x;
        baseRotationZ = transform.eulerAngles.z;
        
        CreateHPBar();
        UpdateHPBar();
        CreateBossLabel();
        CreateBossOutline();

        if (IsMonPig2())
        {
            CacheEnemy2LaneCellSize();
            enemy2NextSpecialAt = Time.time + UnityEngine.Random.Range(4f, 6f);
        }
        if (IsMonPig5())
        {
            pig5NextSteerAt = Time.time + UnityEngine.Random.Range(2f, 4f);
            pig5SteerY = UnityEngine.Random.Range(-0.45f, 0.45f);
            pig5CachedSpawner = FindFirstObjectByType<ZombieSpawner>();
        }
        if (IsMonPig10())
        {
            pig5NextSteerAt = Time.time + UnityEngine.Random.Range(2f, 4f);
            pig5SteerY = UnityEngine.Random.Range(-0.45f, 0.45f);
            pig5CachedSpawner = FindFirstObjectByType<ZombieSpawner>();
            enemy10StealthPhase = false;
            enemy10NextStealthEvalAt = Time.time + UnityEngine.Random.Range(3f, 5f);
        }
        if (IsMonPig6())
        {
            pig6NextShieldAt = Time.time + UnityEngine.Random.Range(10f, 12f);
        }
        if (IsNecromancerBoss())
        {
            necromancerCachedSpawner = FindFirstObjectByType<ZombieSpawner>();
            necromancerNextPatternAt = Time.time + UnityEngine.Random.Range(3f, 5f);
        }
    }
    
    void Update()
    {
        if (isDead) return;
        
        // 전투 일시정지(쉬는 시간·보스 보상 등) 중이면 동작·애니메이션 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            ApplyCombatPauseVisuals();
            return;
        }

        if (combatPauseVisualsActive)
        {
            ClearCombatPauseVisuals();
        }

        UpdateBurn();
        if (isDead) return;
        if (!IsCalamityMarked() && calamityMarkOverlay != null)
        {
            ClearCalamityMarkState();
        }

        if (enemy9CorpsePhase && IsMonPig9())
        {
            return;
        }

        if (IsAnyBoss())
        {
            UpdateBossForwardOnly();
            return;
        }

        if (IsMonPig5())
        {
            UpdateEnemy5();
            return;
        }

        if (IsMonPig10())
        {
            UpdateEnemy10();
            return;
        }

        if (IsMonPig6())
        {
            UpdateEnemy6();
            return;
        }

        TryStartEnemy2PeriodicShove();
        
        bool isMoving = false;
        bool isAttackingBarrier = false;

        // 방어막 공격
        if (targetBarrier != null && !IsStunned() && IsBarrierInRange(targetBarrier))
        {
            isAttackingBarrier = true;
            SetMonPig1AttackAnimation();
            AttackBarrier();
        }
        // 식물을 공격 중인지 확인
        else if (targetPlant != null && !IsStunned() && Vector2.Distance(transform.position, targetPlant.transform.position) <= attackRange)
        {
            AttackPlant();
        }
        else
        {
            // 왼쪽으로 이동
            if (!enemy2SpecialMoveLock)
            {
                MoveLeft();
            }
            FindTargetPlant();
            FindTargetBarrier();
            isMoving = !enemy2SpecialMoveLock;
        }
        
        UpdateSlowEffect();
        if (!isAttackingBarrier && !(IsMonPig2() && enemy2SpecialMoveLock))
        {
            UpdateMovementAnimation(isMoving);
        }
    }
    
    bool IsAnyBoss()
    {
        if (!string.IsNullOrEmpty(zombieUID) && EnemyCombatStats.IsBossUid(zombieUID))
        {
            return true;
        }
        if (zombieData == null) return false;
        return zombieData.zombieName == "Boss1"
            || zombieData.zombieName == "OozeBoss"
            || zombieData.zombieName == "NecromancerBoss";
    }

    bool ResistsFreezeAndKnockback() => IsAnyBoss();

    bool IsBoss1()
    {
        if (zombieUID == "boss1") return true;
        return zombieData != null && zombieData.zombieName == "Boss1";
    }

    bool IsNecromancerBoss()
    {
        if (zombieUID == "necromancer_boss") return true;
        return zombieData != null && zombieData.zombieName == "NecromancerBoss";
    }

    bool IsOozeBoss()
    {
        if (zombieUID == "ooze_boss") return true;
        return zombieData != null && zombieData.zombieName == "OozeBoss";
    }

    void UpdateOozeBoss()
    {
        FindTargetBarrier();

        if (targetBarrier != null && !IsStunned() && IsBarrierInRange(targetBarrier))
        {
            if (HasSpriteFrames(OozeBossAttackBarrier))
            {
                SetEnemySheetAnimation(OozeBossAttackBarrier, true);
            }
            AttackBarrier();
            UpdateSlowEffect();
            return;
        }

        if (!IsStunned() && !unit11FreezeActive)
        {
            TryOozeBossPeriodicAction();
        }

        if (!oozeBossActionLock)
        {
            MoveLeft();
        }

        UpdateSlowEffect();

        if (oozeBossRoutine != null)
        {
            return;
        }

        if (!oozeBossActionLock && HasSpriteFrames(OozeBossWalk))
        {
            SetEnemySheetAnimation(OozeBossWalk, true);
        }
        else if (HasSpriteFrames(OozeBossWalk))
        {
            SetEnemySheetAnimation(OozeBossWalk, false);
        }
    }

    void TryOozeBossPeriodicAction()
    {
        if (oozeBossRoutine != null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;
        if (Time.time < oozeBossNextSpecialAt) return;
        if (IsStunned() || unit11FreezeActive) return;
        if (targetBarrier != null && IsBarrierInRange(targetBarrier)) return;

        oozeBossNextSpecialAt = Time.time + UnityEngine.Random.Range(3f, 5f);

        if (oozeBossCachedSpawner == null)
        {
            oozeBossCachedSpawner = FindFirstObjectByType<ZombieSpawner>();
        }

        bool doVertical = UnityEngine.Random.value < 0.5f;
        if (doVertical)
        {
            oozeBossRoutine = StartCoroutine(OozeBossVerticalRoutine());
        }
        else
        {
            oozeBossRoutine = StartCoroutine(OozeBossSummonRoutine());
        }
    }

    IEnumerator OozeBossVerticalRoutine()
    {
        oozeBossActionLock = true;
        ZombieSpawner sp = oozeBossCachedSpawner != null ? oozeBossCachedSpawner : FindFirstObjectByType<ZombieSpawner>();
        if (sp == null || sp.spawnYPositions == null || sp.spawnYPositions.Length < 2)
        {
            oozeBossActionLock = false;
            oozeBossRoutine = null;
            yield break;
        }

        int maxR = sp.spawnYPositions.Length - 1;
        int steps = UnityEngine.Random.Range(1, 3);
        int dir = UnityEngine.Random.value < 0.5f ? -1 : 1;
        int newRow = Mathf.Clamp(currentRow + dir * steps, 0, maxR);
        if (newRow == currentRow)
        {
            dir = -dir;
            newRow = Mathf.Clamp(currentRow + dir * steps, 0, maxR);
        }
        if (newRow == currentRow)
        {
            oozeBossActionLock = false;
            oozeBossRoutine = null;
            yield break;
        }

        if (HasSpriteFrames(OozeBossAttackVertical))
        {
            SetEnemySheetAnimation(OozeBossAttackVertical, true);
        }

        float atkDur = GetAnimationDuration(OozeBossAttackVertical, monPig1Fps);
        if (atkDur < 0.08f)
        {
            atkDur = 0.35f;
        }
        yield return new WaitForSeconds(atkDur * 0.35f);

        float endY = sp.spawnYPositions[newRow];
        float startY = baseY;
        const float moveDur = 0.42f;
        float t = 0f;
        while (t < moveDur)
        {
            t += Time.deltaTime;
            baseY = Mathf.Lerp(startY, endY, t / moveDur);
            Vector3 p = transform.position;
            p.x = baseX;
            p.y = baseY;
            transform.position = p;
            yield return null;
        }

        currentRow = newRow;
        baseY = endY;
        {
            Vector3 p = transform.position;
            p.x = baseX;
            p.y = baseY;
            transform.position = p;
        }

        oozeBossActionLock = false;
        oozeBossRoutine = null;
    }

    IEnumerator OozeBossSummonRoutine()
    {
        oozeBossActionLock = true;
        if (HasSpriteFrames(OozeBossAttackSummon))
        {
            SetEnemySheetAnimation(OozeBossAttackSummon, true);
        }

        float dur = GetAnimationDuration(OozeBossAttackSummon, monPig1Fps);
        if (dur < 0.08f)
        {
            dur = 0.5f;
        }
        yield return new WaitForSeconds(dur * 0.4f);

        ZombieSpawner sp = oozeBossCachedSpawner != null ? oozeBossCachedSpawner : FindFirstObjectByType<ZombieSpawner>();
        if (sp != null)
        {
            float cell = GetEnemy2LaneCellSize();
            Vector3 pos = new Vector3(baseX - Mathf.Max(cell * 1.1f, 0.55f), baseY, 0f);
            sp.SpawnZombieUidAtPosition("mon_pig_7", pos, currentRow);
        }

        yield return new WaitForSeconds(Mathf.Max(dur * 0.6f, 0.1f));

        oozeBossActionLock = false;
        oozeBossRoutine = null;
    }
    
    void UpdateBossForwardOnly()
    {
        UpdateBossLabel();
        UpdateSlowEffect();

        if (IsStunned() || unit11FreezeActive)
        {
            SetBossWalkAnimation(false);
            SyncBossOutlineIfNeeded();
            return;
        }

        MoveLeft();
        SetBossWalkAnimation(true);
        SyncBossOutlineIfNeeded();
    }

    void SyncBossOutlineIfNeeded()
    {
        if (isDead || !IsAnyBoss() || bossOutlineHost == null) return;
        SpriteSilhouetteOutline.Sync(bossOutlineHost);
    }

    void SetBossWalkAnimation(bool animate)
    {
        string walk = ResolveWalkResource();
        if (!string.IsNullOrEmpty(walk) && HasSpriteFrames(walk))
        {
            SetEnemySheetAnimation(walk, animate);
        }
    }

    void CreateBossOutline()
    {
        if (!IsAnyBoss()) return;

        GameObject hostObj = new GameObject("BossOutlineHost");
        hostObj.transform.SetParent(transform, false);
        bossOutlineHost = hostObj.transform;
        SpriteSilhouetteOutline.SetTarget(bossOutlineHost, transform, ShouldBossOutlineRenderer, BossOutlineColor);
    }

    void ClearBossOutline()
    {
        if (bossOutlineHost != null)
        {
            SpriteSilhouetteOutline.Clear(bossOutlineHost);
            bossOutlineHost = null;
        }
    }

    static bool ShouldBossOutlineRenderer(SpriteRenderer renderer)
    {
        if (renderer == null || !renderer.enabled || renderer.sprite == null) return false;
        if (renderer.color.a < 0.05f) return false;
        if (SpriteSilhouetteOutline.IsOutlineRenderer(renderer)) return false;

        Transform walk = renderer.transform;
        while (walk != null)
        {
            string nodeName = walk.name;
            if (nodeName == "BossLabel" ||
                nodeName == "SilhouetteOutlineRoot" ||
                nodeName.Contains("HPBar"))
            {
                return false;
            }
            walk = walk.parent;
        }
        return true;
    }

    void CreateBossLabel()
    {
        if (!IsAnyBoss()) return;

        GameObject labelRoot = new GameObject("BossLabel");
        labelRoot.transform.SetParent(transform, false);
        bossLabelTransform = labelRoot.transform;

        float yOffset = 0.55f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float spriteHeight = spriteRenderer.bounds.size.y;
            float parentScaleY = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
            yOffset = (spriteHeight / parentScaleY) * 0.5f + 0.16f;
            if (IsBoss1())
            {
                yOffset -= 0.12f;
            }
        }
        bossLabelBaseY = yOffset;
        bossLabelBobPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        bossLabelTransform.localPosition = new Vector3(0f, yOffset, 0f);

        Vector3 parentScale = transform.localScale;
        if (!Mathf.Approximately(parentScale.x, 0f) && !Mathf.Approximately(parentScale.y, 0f))
        {
            bossLabelTransform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);
        }

        int baseOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 1;
        CreateBossLabelTextLayer(labelRoot.transform, "BossLabelGlow", "BOSS",
            new Color(1f, 0.08f, 0.08f, 0.28f), 0.10f, new Vector3(0f, -0.015f, 0.01f), baseOrder + 23);
        CreateBossLabelTextLayer(labelRoot.transform, "BossLabelShadow", "BOSS",
            new Color(0.18f, 0f, 0f, 0.85f), 0.092f, new Vector3(0.02f, -0.025f, 0.005f), baseOrder + 24);
        bossLabelText = CreateBossLabelTextLayer(labelRoot.transform, "BossLabelText", "BOSS",
            BossLabelColor, 0.088f, Vector3.zero, baseOrder + 26);
    }

    static TextMesh CreateBossLabelTextLayer(
        Transform parent,
        string objectName,
        string text,
        Color color,
        float characterSize,
        Vector3 localPosition,
        int sortingOrder)
    {
        GameObject textObj = new GameObject(objectName);
        textObj.transform.SetParent(parent, false);
        textObj.transform.localPosition = localPosition;

        TextMesh label = textObj.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 38;
        label.characterSize = characterSize;
        label.color = color;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAlignment.Center;
        label.anchor = TextAnchor.MiddleCenter;

        MeshRenderer labelRenderer = textObj.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = sortingOrder;
        }

        return label;
    }

    void UpdateBossLabel()
    {
        if (bossLabelTransform == null || isDead) return;

        float bob = Mathf.Sin(Time.time * BossLabelBobSpeed + bossLabelBobPhase) * BossLabelBobAmplitude;
        bossLabelTransform.localPosition = new Vector3(0f, bossLabelBaseY + bob, 0f);

        if (bossLabelText != null)
        {
            float pulse = 0.9f + Mathf.Sin(Time.time * 4.4f + bossLabelBobPhase * 1.35f) * 0.1f;
            Color color = BossLabelColor;
            color.a = pulse;
            bossLabelText.color = color;
        }
    }

    void UpdateBoss1()
    {
        FindTargetBarrier();
        
        if (targetBarrier != null && !IsStunned() && IsBarrierInRange(targetBarrier))
        {
            if (HasSpriteFrames(Boss1AttackBarrier))
            {
                SetEnemySheetAnimation(Boss1AttackBarrier, true);
            }
            AttackBarrier();
            UpdateSlowEffect();
            return;
        }
        
        if (!IsStunned() && !unit11FreezeActive)
        {
            TryStartBoss1VerticalMove();
            TryStartBoss1Shove();
        }
        if (!boss1ActionLock)
        {
            MoveLeft();
        }
        
        UpdateSlowEffect();
        if (boss1ShoveRoutine != null)
        {
            return;
        }
        if (boss1VertRoutine != null)
        {
            if (HasSpriteFrames(Boss1Walk))
            {
                SetEnemySheetAnimation(Boss1Walk, true);
            }
            return;
        }
        if (!boss1ActionLock && HasSpriteFrames(Boss1Walk))
        {
            SetEnemySheetAnimation(Boss1Walk, true);
        }
        else if (HasSpriteFrames(Boss1Walk))
        {
            SetEnemySheetAnimation(Boss1Walk, false);
        }
    }

    bool IsMonPig5()
    {
        if (zombieUID == "mon_pig_5") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미5")
        {
            return true;
        }
        return false;
    }

    bool IsMonPig6()
    {
        if (zombieUID == "mon_pig_6") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미6")
        {
            return true;
        }
        return false;
    }

    void UpdateEnemy5()
    {
        if (unit11FreezeActive)
        {
            if (HasSpriteFrames(ResolveWalkResource()))
            {
                SetEnemySheetAnimation(ResolveWalkResource(), false);
            }
            UpdateSlowEffect();
            return;
        }

        FindTargetPlant();
        FindTargetBarrier();

        bool barrierAttack = targetBarrier != null && !IsStunned() && IsBarrierInRange(targetBarrier);
        bool plantAttack = !barrierAttack && targetPlant != null && !IsStunned()
            && Vector2.Distance(transform.position, targetPlant.transform.position) <= attackRange;

        if (barrierAttack)
        {
            SetMonPig1AttackAnimation();
            AttackBarrier();
        }
        else if (plantAttack)
        {
            SetMonPig1AttackAnimation();
            AttackPlant();
        }
        else
        {
            MoveEnemy5Diagonal();
            FindTargetPlant();
            FindTargetBarrier();
            if (HasSpriteFrames(ResolveWalkResource()))
            {
                SetEnemySheetAnimation(ResolveWalkResource(), true);
            }
        }

        UpdateSlowEffect();
    }

    void UpdateEnemy10()
    {
        UpdateEnemy10StealthClock();

        if (unit11FreezeActive)
        {
            if (HasSpriteFrames(ResolveWalkResource()))
            {
                SetEnemySheetAnimation(ResolveWalkResource(), false);
            }
            UpdateSlowEffect();
            ApplyEnemy10StealthSpriteAlpha();
            return;
        }

        FindTargetPlant();
        FindTargetBarrier();

        bool barrierAttack = targetBarrier != null && !IsStunned() && IsBarrierInRange(targetBarrier);
        bool plantAttack = !barrierAttack && targetPlant != null && !IsStunned()
            && Vector2.Distance(transform.position, targetPlant.transform.position) <= attackRange;

        if (barrierAttack)
        {
            SetMonPig1AttackAnimation();
            AttackBarrier();
        }
        else if (plantAttack)
        {
            SetMonPig1AttackAnimation();
            AttackPlant();
        }
        else
        {
            MoveEnemy5Diagonal();
            FindTargetPlant();
            FindTargetBarrier();
            if (HasSpriteFrames(ResolveWalkResource()))
            {
                SetEnemySheetAnimation(ResolveWalkResource(), true);
            }
        }

        UpdateSlowEffect();
        ApplyEnemy10StealthSpriteAlpha();
    }

    void UpdateEnemy10StealthClock()
    {
        if (enemy10StealthPhase)
        {
            if (Time.time >= enemy10StealthEndAt)
            {
                enemy10StealthPhase = false;
                enemy10NextStealthEvalAt = Time.time + UnityEngine.Random.Range(3f, 5f);
            }
        }
        else if (Time.time >= enemy10NextStealthEvalAt)
        {
            enemy10StealthPhase = true;
            enemy10StealthEndAt = Time.time + UnityEngine.Random.Range(2f, 3f);
        }
    }

    void ApplyEnemy10StealthSpriteAlpha()
    {
        if (!IsMonPig10() || spriteRenderer == null || !enemy10StealthPhase) return;
        Color c = spriteRenderer.color;
        c.a = 0.5f;
        spriteRenderer.color = c;
    }

    void UpdateEnemy6()
    {
        if (unit11FreezeActive)
        {
            if (HasSpriteFrames(ResolveWalkResource()))
            {
                SetEnemySheetAnimation(ResolveWalkResource(), false);
            }
            UpdateSlowEffect();
            return;
        }

        TryTriggerEnemy6ShieldIfReady();

        FindTargetPlant();
        FindTargetBarrier();

        bool barrierAttack = !pig6ShieldCastLock && targetBarrier != null && !IsStunned() && IsBarrierInRange(targetBarrier);
        bool plantAttack = !pig6ShieldCastLock && !barrierAttack && targetPlant != null && !IsStunned()
            && Vector2.Distance(transform.position, targetPlant.transform.position) <= attackRange;

        if (pig6ShieldCastLock)
        {
            UpdateSlowEffect();
            return;
        }

        if (barrierAttack)
        {
            SetMonPig1AttackAnimation();
            AttackBarrier();
        }
        else if (plantAttack)
        {
            SetMonPig1AttackAnimation();
            AttackPlant();
        }
        else
        {
            MoveLeft();
            FindTargetPlant();
            FindTargetBarrier();
            if (HasSpriteFrames(ResolveWalkResource()))
            {
                SetEnemySheetAnimation(ResolveWalkResource(), true);
            }
        }

        UpdateSlowEffect();
    }

    void TryTriggerEnemy6ShieldIfReady()
    {
        if (!IsMonPig6() || isDead) return;
        if (pig6ShieldRoutine != null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;
        if (IsStunned() || unit11FreezeActive) return;
        if (Time.time < pig6NextShieldAt) return;

        FindTargetBarrier();
        if (targetBarrier != null && IsBarrierInRange(targetBarrier))
        {
            return;
        }

        Zombie zTarget = FindRandomOtherZombieAheadInLaneForEnemy6();
        if (zTarget != null)
        {
            pig6ShieldRoutine = StartCoroutine(Enemy6ShieldRoutine(zTarget));
            return;
        }

        pig6NextShieldAt = Time.time + UnityEngine.Random.Range(10f, 12f);
    }

    Zombie FindRandomOtherZombieAheadInLaneForEnemy6()
    {
        float tol = GetZombieLaneYTolerance();
        float zx = transform.position.x;
        float zy = baseY;
        List<Zombie> candidates = new List<Zombie>();
        for (int i = 0; i < s_activeZombies.Count; i++)
        {
            Zombie z = s_activeZombies[i];
            if (z == null || z.IsDead) continue;
            if (z == this) continue;
            if (Mathf.Abs(z.transform.position.y - zy) > tol) continue;
            if (z.transform.position.x < zx - 0.06f)
            {
                candidates.Add(z);
            }
        }
        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    IEnumerator Enemy6ShieldRoutine(Zombie target)
    {
        pig6ShieldCastLock = true;
        string atkRes = "enemy_6_attack-export";
        if (HasSpriteFrames(atkRes))
        {
            SetEnemySheetAnimation(atkRes, true);
        }

        float dur = HasSpriteFrames(atkRes) ? GetAnimationDuration(atkRes, monPig1Fps) : 0f;
        if (dur < 0.15f)
        {
            dur = 0.65f;
        }

        Vector3 from = transform.position + Vector3.up * 0.15f;
        Vector3 toPos = target != null ? target.transform.position + Vector3.up * 0.15f : from + Vector3.left * 2f;
        SpawnEnemy6LaserEffect(from, toPos);

        float mid = Mathf.Clamp(dur * 0.48f, 0.12f, Mathf.Max(0.15f, dur - 0.06f));
        yield return new WaitForSeconds(mid);
        if (target != null && target.gameObject != null && !target.IsDead)
        {
            target.ApplyEnemy6Shield(20, 20f);
        }

        float remain = Mathf.Max(0.05f, dur - mid);
        yield return new WaitForSeconds(remain);

        pig6ShieldCastLock = false;
        pig6ShieldRoutine = null;
        pig6NextShieldAt = Time.time + UnityEngine.Random.Range(10f, 12f);

        string walk = ResolveWalkResource();
        if (!string.IsNullOrEmpty(walk) && HasSpriteFrames(walk))
        {
            SetEnemySheetAnimation(walk, true);
        }
    }

    void SpawnEnemy6LaserEffect(Vector3 fromWorld, Vector3 toWorld)
    {
        GameObject lineObj = new GameObject("Enemy6Laser");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, fromWorld);
        lr.SetPosition(1, toWorld);
        lr.startWidth = 0.07f;
        lr.endWidth = 0.11f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.textureMode = LineTextureMode.Stretch;
        lr.startColor = new Color(0.35f, 0.82f, 1f, 0.95f);
        lr.endColor = new Color(0.65f, 0.95f, 1f, 0.55f);
        lr.sortingOrder = 24;
        UnityEngine.Object.Destroy(lineObj, 0.4f);
    }

    void MoveEnemy5Diagonal()
    {
        if (IsStunned())
        {
            return;
        }
        if (unit12PathDragged)
        {
            return;
        }

        if (Time.time >= pig5NextSteerAt)
        {
            pig5NextSteerAt = Time.time + UnityEngine.Random.Range(2f, 4f);
            pig5SteerY = UnityEngine.Random.Range(-0.55f, 0.55f);
        }

        float speed = baseMoveSpeed;
        if (IsSlowed())
        {
            speed = baseMoveSpeed * (2f / 3f);
        }

        Vector2 d = new Vector2(-1f, pig5SteerY).normalized;
        baseX += d.x * speed * Time.deltaTime;
        baseY += d.y * speed * Time.deltaTime;
        if (knockbackOffsetX > 0f)
        {
            baseX += knockbackOffsetX;
            knockbackOffsetX = 0f;
            knockbackVelocity = 0f;
        }

        Vector3 pos = transform.position;
        pos.x = baseX;
        pos.y = baseY;
        transform.position = pos;
        transform.rotation = Quaternion.Euler(0f, 0f, baseRotationZ);

        SyncPig5RowBounds();
        ClampToBarrierIfNeeded();

        if (transform.position.x < -10f)
        {
            GameManager.Instance.OnZombieReachedEnd(this);
        }
    }

    void SyncPig5RowBounds()
    {
        if (pig5CachedSpawner == null)
        {
            pig5CachedSpawner = FindFirstObjectByType<ZombieSpawner>();
        }
        if (pig5CachedSpawner != null && pig5CachedSpawner.spawnYPositions != null && pig5CachedSpawner.spawnYPositions.Length > 0)
        {
            float[] ys = pig5CachedSpawner.spawnYPositions;
            float minYp = float.MaxValue;
            float maxYp = float.MinValue;
            foreach (float y in ys)
            {
                minYp = Mathf.Min(minYp, y);
                maxYp = Mathf.Max(maxYp, y);
            }
            float pad = 0.12f;
            baseY = Mathf.Clamp(baseY, minYp - pad, maxYp + pad);

            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < ys.Length; i++)
            {
                float dd = Mathf.Abs(baseY - ys[i]);
                if (dd < bestD)
                {
                    bestD = dd;
                    best = i;
                }
            }
            currentRow = best;
            return;
        }

        BoardManager bm = FindFirstObjectByType<BoardManager>();
        if (bm != null && bm.TryGetLaneBounds(out float laneMin, out float laneMax))
        {
            baseY = Mathf.Clamp(baseY, laneMin + 0.08f, laneMax - 0.08f);
        }
    }
    
    void AttackBarrierBoss1()
    {
        if (targetBarrier == null || !targetBarrier.IsAlive())
        {
            targetBarrier = null;
            return;
        }
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return;
        }
        targetBarrier.TakeDamage(2);
        lastAttackTime = Time.time;
    }
    
    void TryStartBoss1VerticalMove()
    {
        if (boss1VertRoutine != null || boss1ShoveRoutine != null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;
        if (Time.time < boss1NextVerticalAt) return;
        if (IsStunned() || unit11FreezeActive) return;
        if (targetBarrier != null && IsBarrierInRange(targetBarrier)) return;
        ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
        if (spawner == null || spawner.spawnYPositions == null || spawner.spawnYPositions.Length < 2) return;
        int maxR = spawner.spawnYPositions.Length - 1;
        int delta = UnityEngine.Random.value < 0.5f ? -1 : 1;
        int newRow = Mathf.Clamp(currentRow + delta, 0, maxR);
        if (newRow == currentRow)
        {
            newRow = Mathf.Clamp(currentRow - delta, 0, maxR);
        }
        if (newRow == currentRow)
        {
            boss1NextVerticalAt = Time.time + UnityEngine.Random.Range(2f, 3f);
            return;
        }
        boss1VertRoutine = StartCoroutine(Boss1VerticalStepRoutine(newRow, spawner));
    }
    
    void TryStartBoss1Shove()
    {
        if (boss1VertRoutine != null || boss1ShoveRoutine != null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;
        if (Time.time < boss1NextShoveAt) return;
        if (IsStunned() || unit11FreezeActive) return;
        if (targetBarrier != null && IsBarrierInRange(targetBarrier)) return;
        boss1ShoveRoutine = StartCoroutine(Boss1ShoveRoutine());
    }
    
    IEnumerator Boss1VerticalStepRoutine(int newRow, ZombieSpawner spawner)
    {
        boss1ActionLock = true;
        float endY = spawner.spawnYPositions[newRow];
        float startY = baseY;
        if (HasSpriteFrames(Boss1Walk))
        {
            SetEnemySheetAnimation(Boss1Walk, true);
        }
        const float duration = 0.42f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            baseY = Mathf.Lerp(startY, endY, t / duration);
            Vector3 p = transform.position;
            p.x = baseX;
            p.y = baseY;
            transform.position = p;
            yield return null;
        }
        currentRow = newRow;
        baseY = endY;
        {
            Vector3 p = transform.position;
            p.x = baseX;
            p.y = baseY;
            transform.position = p;
        }
        boss1NextVerticalAt = Time.time + UnityEngine.Random.Range(2f, 3f);
        boss1ActionLock = false;
        boss1VertRoutine = null;
    }
    
    IEnumerator Boss1ShoveRoutine()
    {
        boss1ActionLock = true;
        if (HasSpriteFrames(Boss1AttackShove))
        {
            SetEnemySheetAnimation(Boss1AttackShove, true);
        }
        float dur = GetAnimationDuration(Boss1AttackShove, monPig1Fps);
        if (dur < 0.2f) dur = 0.6f;
        yield return new WaitForSeconds(dur * 0.4f);
        Character ally = FindBoss1NearestAllyInRow();
        if (ally != null)
        {
            ally.Boss1KnockTowardEnemy(Boss1KnockColumns);
        }
        yield return new WaitForSeconds(dur * 0.6f);
        boss1ActionLock = false;
        boss1NextShoveAt = Time.time + UnityEngine.Random.Range(2f, 3f);
        boss1ShoveRoutine = null;
    }
    
    Character FindBoss1NearestAllyInRow()
    {
        Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character best = null;
        float bestD = Boss1ShoveLateralRange;
        foreach (Character ch in characters)
        {
            if (ch == null) continue;
            BoardCell cell = ch.GetCurrentBoardCell();
            if (cell == null || !cell.isBoardCell) continue;
            if (!TryParseBoardCellNameForBoss(cell.gameObject.name, out int br, out int _))
            {
                continue;
            }
            if (br != currentRow) continue;
            float d = Vector2.Distance(new Vector2(baseX, baseY), ch.transform.position);
            if (d < bestD)
            {
                bestD = d;
                best = ch;
            }
        }
        return best;
    }
    
    static bool TryParseBoardCellNameForBoss(string cellName, out int row, out int col)
    {
        row = col = 0;
        if (string.IsNullOrEmpty(cellName) || !cellName.StartsWith("BoardCell_")) return false;
        string[] p = cellName.Split('_');
        if (p.Length < 3) return false;
        return int.TryParse(p[1], out row) && int.TryParse(p[2], out col);
    }
    
    void ApplyCombatPauseVisuals()
    {
        if (spriteAnimator != null)
        {
            spriteAnimator.SetAnimating(false);
        }

        EnsureSpriteRenderer();
        if (!combatPauseVisualsActive && spriteRenderer != null)
        {
            combatPauseSavedColor = spriteRenderer.color;
            combatPauseVisualsActive = true;
            spriteRenderer.color = new Color(
                combatPauseSavedColor.r * 0.58f,
                combatPauseSavedColor.g * 0.60f,
                combatPauseSavedColor.b * 0.68f,
                combatPauseSavedColor.a);
        }
    }

    void ClearCombatPauseVisuals()
    {
        if (!combatPauseVisualsActive) return;

        combatPauseVisualsActive = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = combatPauseSavedColor;
        }
    }

    void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    void LateUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.frameCount == s_lastZombieSeparationFrame) return;
        s_lastZombieSeparationFrame = Time.frameCount;
        RunZombieSeparation();
    }
    
    void FindTargetBarrier()
    {
        if (Barrier.Instance != null && Barrier.Instance.IsAlive())
        {
            targetBarrier = Barrier.Instance;
        }
        else
        {
            targetBarrier = null;
        }
    }
    
    bool IsBarrierInRange(Barrier barrier)
    {
        if (barrier == null) return false;
        if (IsMonPig2())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        if (IsMonPig3())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        if (IsMonPig5())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        if (IsMonPig6())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        if (IsMonPig7())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        if (IsMonPig8())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        if (IsOozeBoss())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        if (IsNecromancerBoss())
        {
            Bounds zb = GetBodyBounds();
            Bounds bb = barrier.GetWorldBounds();
            const float flushEast = 0.12f;
            if (zb.min.x > bb.max.x + flushEast)
            {
                return false;
            }
            float reachWest = bb.min.x - attackRange - 0.08f;
            return zb.max.x >= reachWest;
        }
        float dx = transform.position.x - barrier.transform.position.x;
        return dx >= -0.1f && dx <= attackRange;
    }
    
    float GetZombieLaneYTolerance()
    {
        BoardManager bm = FindFirstObjectByType<BoardManager>();
        if (bm != null && bm.cellSize > 0.01f)
        {
            return Mathf.Max(0.22f, bm.cellSize * 0.55f);
        }
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null && gm.cellSize > 0.01f)
        {
            return Mathf.Max(0.22f, gm.cellSize * 0.55f);
        }
        ZombieSpawner sp = FindFirstObjectByType<ZombieSpawner>();
        if (sp != null && sp.spawnYPositions != null && sp.spawnYPositions.Length >= 2)
        {
            float dy = Mathf.Abs(sp.spawnYPositions[1] - sp.spawnYPositions[0]);
            return Mathf.Max(0.22f, dy * 0.48f);
        }
        return 0.42f;
    }

    bool IsPlantOnSameLaneAsZombie(Plant plant)
    {
        if (plant == null) return false;
        return Mathf.Abs(plant.transform.position.y - baseY) <= GetZombieLaneYTolerance();
    }

    /// <summary>
    /// 왼쪽으로 이동합니다
    /// </summary>
    void MoveLeft()
    {
        if (IsStunned())
        {
            return;
        }
        if (unit12PathDragged) return;
        
        float speed = baseMoveSpeed;
        if (IsSlowed())
        {
            speed = baseMoveSpeed * (2f / 3f); // 이동속도 1/3 감소
        }
        
        Vector3 pos = transform.position;
        baseX += -speed * Time.deltaTime;
        if (knockbackOffsetX > 0f)
        {
            baseX += knockbackOffsetX;
            knockbackOffsetX = 0f;
            knockbackVelocity = 0f;
        }
        pos.x = baseX;
        pos.y = baseY;
        transform.position = pos;

        transform.rotation = Quaternion.Euler(0f, 0f, baseRotationZ);
        ClampToBarrierIfNeeded();
        
        // 화면 밖으로 나가면 제거 (게임 오버 체크는 게임 매니저에서)
        if (transform.position.x < -10f)
        {
            GameManager.Instance.OnZombieReachedEnd(this);
        }
    }
    
    /// <summary>
    /// 공격 범위 내의 식물을 찾습니다
    /// </summary>
    void FindTargetPlant()
    {
        Plant[] plants = FindObjectsByType<Plant>(FindObjectsSortMode.None);
        Plant closestPlant = null;
        float closestDistance = attackRange;
        
        foreach (Plant plant in plants)
        {
            if (!IsPlantOnSameLaneAsZombie(plant))
            {
                continue;
            }
            float distance = Vector2.Distance(transform.position, plant.transform.position);
            if (distance < closestDistance && transform.position.x > plant.transform.position.x)
            {
                closestDistance = distance;
                closestPlant = plant;
            }
        }
        
        targetPlant = closestPlant;
    }
    
    /// <summary>
    /// 식물을 공격합니다
    /// </summary>
    void AttackPlant()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (targetPlant != null)
            {
                targetPlant.TakeDamage(damage);
                lastAttackTime = Time.time;
            }
            else
            {
                targetPlant = null;
            }
        }
    }
    
    void AttackBarrier()
    {
        if (targetBarrier == null || !targetBarrier.IsAlive())
        {
            targetBarrier = null;
            return;
        }
        
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            int barrierDamage = (IsMonPig2() || IsMonPig3()) ? 1 : damage;
            targetBarrier.TakeDamage(barrierDamage);
            lastAttackTime = Time.time;
        }
    }
    
    /// <summary>
    /// 에너미6이 다른 적 좀비에게 부여하는 보호막(추가 체력 흡수).
    /// </summary>
    public void ApplyEnemy6Shield(int shieldHp, float durationSec)
    {
        enemy6ShieldHp = Mathf.Max(enemy6ShieldHp, shieldHp);
        enemy6ShieldExpireTime = Time.time + durationSec;
    }

    void ClearExpiredEnemy6ShieldIfNeeded()
    {
        if (enemy6ShieldHp <= 0) return;
        if (Time.time >= enemy6ShieldExpireTime)
        {
            enemy6ShieldHp = 0;
        }
    }

    /// <summary>
    /// 데미지를 받습니다
    /// </summary>
    public void TakeDamage(int damageAmount, int sourceUnitNumber = 0)
    {
        if (damageAmount <= 0) return;
        if (IsExcludedFromCombat) return;
        ClearExpiredEnemy6ShieldIfNeeded();

        int dmg = damageAmount;
        if (sourceUnitNumber > 0 && IsCalamityMarked())
        {
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * (1f + calamityMarkDamageBonus)));
        }
        if (enemy6ShieldHp > 0 && Time.time < enemy6ShieldExpireTime)
        {
            int absorbed = Mathf.Min(dmg, enemy6ShieldHp);
            enemy6ShieldHp -= absorbed;
            dmg -= absorbed;
            if (dmg <= 0)
            {
                UpdateHPBar();
                return;
            }
        }

        int appliedDamage = Mathf.Min(dmg, health);
        health -= dmg;
        UpdateHPBar();
        if (spriteRenderer != null)
        {
            damageFlashEndTime = Time.time + DamageFlashDuration;
            spriteRenderer.color = Color.red;
        }
        float yOffset = 0.3f;
        string layerName = "Default";
        int order = 0;
        if (spriteRenderer != null)
        {
            yOffset = spriteRenderer.bounds.extents.y + 0.1f;
            layerName = spriteRenderer.sortingLayerName;
            order = spriteRenderer.sortingOrder;
        }
        if (appliedDamage > 0)
        {
            UnitDamageLedger.Record(sourceUnitNumber, appliedDamage);
            DamagePopup.Create(transform.position + Vector3.up * yOffset, appliedDamage, layerName, order);
            GameSfxPlayer.PlayEnemyHitSmall();
        }
        
        if (health <= 0)
        {
            if (IsMonPig9() && enemy9ReviveAvailable && enemy9ReviveRoutine == null)
            {
                health = 0;
                UpdateHPBar();
                enemy9ReviveRoutine = StartCoroutine(Enemy9KnockdownReviveRoutine());
                return;
            }
            Die();
        }
    }
    
    /// <summary>
    /// 뒤로 살짝 밀려납니다
    /// </summary>
    public void ApplyKnockback(float distance)
    {
        if (IsExcludedFromCombat || ResistsFreezeAndKnockback()) return;
        knockbackOffsetX = Mathf.Max(knockbackOffsetX, distance);
        knockbackVelocity = Mathf.Max(knockbackVelocity, distance * 6f);
    }

    /// <summary>
    /// 유닛 28 토네이도 등: 투사체 쪽으로 끌릴 때 위치(내부 baseX/baseY와 동기화).
    /// MoveLeft가 매 프레임 base로 덮어쓰므로 끌림은 반드시 이 API로 갱신해야 함.
    /// </summary>
    public bool ResistsVortexPull() => ResistsFreezeAndKnockback();

    public void ApplyVortexPull(Vector2 worldPos)
    {
        if (isDead) return;
        if (IsExcludedFromCombat) return;
        if (ResistsVortexPull()) return;
        baseX = worldPos.x;
        baseY = worldPos.y;
        Vector3 p = transform.position;
        p.x = baseX;
        p.y = baseY;
        transform.position = p;
        ClampToBarrierIfNeeded();
    }

    /// <summary>유닛 12: 경로 밀쳐내기용 base 좌표 보정(끌림이 아닌 대상)</summary>
    public void ApplyNudge2D(Vector2 worldDelta)
    {
        if (isDead) return;
        if (IsExcludedFromCombat || ResistsFreezeAndKnockback()) return;
        baseX += worldDelta.x;
        baseY += worldDelta.y;
        Vector3 p = transform.position;
        p.x = baseX;
        p.y = baseY;
        transform.position = p;
        ClampToBarrierIfNeeded();
    }

    public void SetUnit12PathDragState(bool on)
    {
        unit12PathDragged = on;
    }
    
    /// <summary>
    /// 냉기 디버프를 적용합니다
    /// </summary>
    public void ApplySlow(float duration)
    {
        if (IsExcludedFromCombat) return;
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
    }

    /// <summary>
    /// 화상(도트 피해)을 적용합니다. 1초마다 dmgPerTick 피해, duration 동안 지속.
    /// </summary>
    public void ApplyBurn(int dmgPerTick, float duration)
    {
        if (isDead || IsExcludedFromCombat) return;
        if (dmgPerTick <= 0 || duration <= 0f) return;

        burnDamagePerTick = Mathf.Max(burnDamagePerTick, dmgPerTick);
        burnEndTime = Mathf.Max(burnEndTime, Time.time + duration);
        if (burnNextTickTime <= Time.time)
        {
            burnNextTickTime = Time.time + 1f;
        }
    }

    /// <summary>재앙 조합 — 낙인: duration 동안 아군 피해 증폭.</summary>
    public void ApplyCalamityMark(float duration, float damageBonus, bool chainOnDeath, int chainDamage, int sourceUnitNumber)
    {
        if (isDead || IsExcludedFromCombat) return;
        if (duration <= 0f || damageBonus <= 0f) return;

        calamityMarkEndTime = Mathf.Max(calamityMarkEndTime, Time.time + duration);
        calamityMarkDamageBonus = Mathf.Max(calamityMarkDamageBonus, damageBonus);
        if (chainOnDeath)
        {
            calamityMarkChainOnDeath = true;
            calamityMarkChainDamage = Mathf.Max(calamityMarkChainDamage, chainDamage);
        }
        if (sourceUnitNumber > 0)
        {
            calamityMarkSourceUnit = sourceUnitNumber;
        }
        EnsureCalamityMarkOverlay();
    }

    public bool IsCalamityMarked()
    {
        return !isDead && Time.time < calamityMarkEndTime && calamityMarkDamageBonus > 0f;
    }

    void EnsureCalamityMarkOverlay()
    {
        if (calamityMarkOverlay != null) return;
        calamityMarkOverlay = new GameObject("CalamityMarkOverlay");
        calamityMarkOverlay.transform.SetParent(transform, false);
        calamityMarkOverlay.transform.localPosition = Vector3.zero;
        CalamityMarkOverlay overlay = calamityMarkOverlay.AddComponent<CalamityMarkOverlay>();
        overlay.Init(spriteRenderer);
    }

    void ClearCalamityMarkState()
    {
        calamityMarkEndTime = 0f;
        calamityMarkDamageBonus = 0f;
        calamityMarkChainOnDeath = false;
        calamityMarkChainDamage = 0;
        calamityMarkSourceUnit = 0;
        calamityMarkBlinkTimer = 0f;
        calamityMarkBlinkOn = false;
        if (calamityMarkOverlay != null)
        {
            Destroy(calamityMarkOverlay);
            calamityMarkOverlay = null;
        }
    }

    void UpdateBurn()
    {
        if (Time.time >= burnEndTime)
        {
            burnDamagePerTick = 0;
            return;
        }
        if (burnDamagePerTick <= 0) return;

        if (Time.time >= burnNextTickTime)
        {
            burnNextTickTime = Time.time + 1f;
            TakeDamage(burnDamagePerTick);
        }
    }
    
    /// <summary>
    /// 기절 디버프를 적용합니다
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (IsExcludedFromCombat || ResistsFreezeAndKnockback()) return;
        stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);
    }

    /// <summary>
    /// 유닛 11 빙결: 2초 기절(이동불가) + unit_011_attack 0~7 프레임 재생 후 7에서 유지, 해제 시 제거
    /// </summary>
    public void ApplyUnit11Freeze(float duration)
    {
        if (isDead) return;
        if (IsExcludedFromCombat || ResistsFreezeAndKnockback()) return;
        ApplyStun(duration);
        CancelInvoke(nameof(EndUnit11FreezeVisual));
        // Unit11IceField는 충돌이 있는 동안 매 프레임 호출 — 오버레이를 갱신하면 VFX 타이머가 매 프레임 0으로 돌아감
        if (unit11FreezeActive && unit11FreezeOverlay != null)
        {
            float stunLeft = stunEndTime - Time.time;
            Invoke(nameof(EndUnit11FreezeVisual), Mathf.Max(0.01f, stunLeft));
            return;
        }
        if (unit11FreezeOverlay != null)
        {
            Destroy(unit11FreezeOverlay);
            unit11FreezeOverlay = null;
        }

        unit11FreezeActive = true;
        Sprite[] allFrames = Resources.LoadAll<Sprite>("unit_011_attack");
        if (allFrames == null || allFrames.Length == 0)
        {
            Invoke(nameof(EndUnit11FreezeVisual), duration);
            return;
        }

        System.Array.Sort(allFrames, (a, b) =>
        {
            int ca = TrailingIntFromSpriteName(a);
            int cb = TrailingIntFromSpriteName(b);
            if (ca != cb)
            {
                return ca - cb;
            }
            return string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty);
        });
        int n = Mathf.Min(8, allFrames.Length);
        if (n == 0)
        {
            Invoke(nameof(EndUnit11FreezeVisual), duration);
            return;
        }
        var frames = new Sprite[n];
        System.Array.Copy(allFrames, frames, n);
        if (frames[0] == null)
        {
            Invoke(nameof(EndUnit11FreezeVisual), duration);
            return;
        }

        unit11FreezeOverlay = new GameObject("Unit11FreezeOverlay");
        SpriteRenderer oSr = unit11FreezeOverlay.AddComponent<SpriteRenderer>();
        oSr.color = Color.white;
        const float s = 3f;
        if (spriteRenderer != null)
        {
            Transform v = spriteRenderer.transform;
            unit11FreezeOverlay.transform.SetParent(v, false);
            float p = Mathf.Max(1e-4f, Mathf.Max(Mathf.Abs(v.lossyScale.x), Mathf.Abs(v.lossyScale.y)));
            float localMul = s / p;
            unit11FreezeOverlay.transform.localScale = Vector3.one * localMul;
            float z0 = v.position.z;
            Vector3 feetW = new Vector3(
                spriteRenderer.bounds.center.x,
                spriteRenderer.bounds.min.y,
                z0);
            Vector3 localFeet = v.InverseTransformPoint(feetW);
            localFeet.z = 0f;
            unit11FreezeOverlay.AddComponent<Unit11AttackFreezeVfx>().Init(localMul, localFeet, frames);
        }
        else
        {
            unit11FreezeOverlay.transform.SetParent(transform, false);
            float p = Mathf.Max(1e-4f, Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
            float localMul = s / p;
            unit11FreezeOverlay.transform.localScale = Vector3.one * localMul;
            unit11FreezeOverlay.AddComponent<Unit11AttackFreezeVfx>().Init(localMul, Vector3.zero, frames);
        }
        int ord = 0;
        if (spriteRenderer != null) ord = spriteRenderer.sortingOrder;
        oSr.sortingOrder = ord + 10;
        Invoke(nameof(EndUnit11FreezeVisual), duration);
    }

    void EndUnit11FreezeVisual()
    {
        unit11FreezeActive = false;
        if (unit11FreezeOverlay != null)
        {
            Destroy(unit11FreezeOverlay);
            unit11FreezeOverlay = null;
        }
    }
    
    /// <summary>
    /// 현재 냉기 상태인지 확인합니다
    /// </summary>
    bool IsSlowed()
    {
        return Time.time < slowEndTime;
    }
    
    /// <summary>
    /// 현재 기절 상태인지 확인합니다
    /// </summary>
    bool IsStunned()
    {
        return Time.time < stunEndTime;
    }
    
    /// <summary>
    /// 냉기 상태 시 파란색 반짝임 효과
    /// </summary>
    void UpdateSlowEffect()
    {
        if (spriteRenderer == null) return;

        if (Time.time < damageFlashEndTime)
        {
            spriteRenderer.color = Color.red;
            return;
        }

        if (unit11FreezeActive)
        {
            unit11FreezeBlinkTimer += Time.deltaTime;
            if (unit11FreezeBlinkTimer >= slowBlinkInterval)
            {
                unit11FreezeBlinkTimer = 0f;
                unit11FreezeBlinkOn = !unit11FreezeBlinkOn;
                spriteRenderer.color = unit11FreezeBlinkOn
                    ? new Color(0.4f, 0.8f, 1f)
                    : originalColor;
            }
            return;
        }

        unit11FreezeBlinkTimer = 0f;
        unit11FreezeBlinkOn = false;
        
        if (IsStunned())
        {
            stunBlinkTimer += Time.deltaTime;
            if (stunBlinkTimer >= stunBlinkInterval)
            {
                stunBlinkTimer = 0f;
                stunBlinkOn = !stunBlinkOn;
                spriteRenderer.color = stunBlinkOn ? Color.gray : originalColor;
            }
            return;
        }
        
        if (IsSlowed())
        {
            slowBlinkTimer += Time.deltaTime;
            if (slowBlinkTimer >= slowBlinkInterval)
            {
                slowBlinkTimer = 0f;
                slowBlinkOn = !slowBlinkOn;
                spriteRenderer.color = slowBlinkOn ? new Color(0.4f, 0.8f, 1f) : originalColor;
            }
        }
        else if (IsCalamityMarked())
        {
            calamityMarkBlinkTimer += Time.deltaTime;
            if (calamityMarkBlinkTimer >= slowBlinkInterval)
            {
                calamityMarkBlinkTimer = 0f;
                calamityMarkBlinkOn = !calamityMarkBlinkOn;
                spriteRenderer.color = calamityMarkBlinkOn ? CalamityMarkTint : originalColor;
            }
        }
        else
        {
            if (spriteRenderer.color != originalColor)
            {
                spriteRenderer.color = originalColor;
            }
            slowBlinkTimer = 0f;
            slowBlinkOn = false;
            calamityMarkBlinkTimer = 0f;
            calamityMarkBlinkOn = false;
            stunBlinkTimer = 0f;
            stunBlinkOn = false;
        }
    }
    
    /// <summary>
    /// 좀비가 죽습니다
    /// </summary>
    void Die()
    {
        if (isDead) return;

        if (IsCalamityMarked() && calamityMarkChainOnDeath && calamityMarkChainDamage > 0)
        {
            TraitAttackVisuals.ExplodeCalamityMarkChain(
                transform.position,
                calamityMarkChainDamage,
                calamityMarkSourceUnit);
        }
        ClearCalamityMarkState();

        if (enemy2SpecialRoutine != null)
        {
            StopCoroutine(enemy2SpecialRoutine);
            enemy2SpecialRoutine = null;
        }
        enemy2SpecialMoveLock = false;
        if (boss1VertRoutine != null)
        {
            StopCoroutine(boss1VertRoutine);
            boss1VertRoutine = null;
        }
        if (boss1ShoveRoutine != null)
        {
            StopCoroutine(boss1ShoveRoutine);
            boss1ShoveRoutine = null;
        }
        boss1ActionLock = false;
        if (oozeBossRoutine != null)
        {
            StopCoroutine(oozeBossRoutine);
            oozeBossRoutine = null;
        }
        oozeBossActionLock = false;
        if (pig6ShieldRoutine != null)
        {
            StopCoroutine(pig6ShieldRoutine);
            pig6ShieldRoutine = null;
        }
        pig6ShieldCastLock = false;
        if (enemy9ReviveRoutine != null)
        {
            StopCoroutine(enemy9ReviveRoutine);
            enemy9ReviveRoutine = null;
        }
        enemy9CorpsePhase = false;
        enemy10StealthPhase = false;
        if (necromancerPatternRoutine != null)
        {
            StopCoroutine(necromancerPatternRoutine);
            necromancerPatternRoutine = null;
        }
        
        isDead = true;
        ClearBossOutline();
        CancelInvoke(nameof(EndUnit11FreezeVisual));
        unit11FreezeActive = false;
        if (unit11FreezeOverlay != null)
        {
            Destroy(unit11FreezeOverlay);
            unit11FreezeOverlay = null;
        }
        GameManager.Instance.OnZombieKilled(this);

        if (UsesEnemySheetAnimation())
        {
            string deathResource = ResolveDeathResource();
            if (!string.IsNullOrEmpty(deathResource))
            {
                Vector3 lockedLocalPos = spriteRenderer != null
                    ? spriteRenderer.transform.localPosition
                    : Vector3.zero;
                if (spriteAnimator != null)
                {
                    spriteAnimator.EnableBottomAnchor(false);
                }
                SetEnemySheetAnimation(deathResource, true);
                if (spriteRenderer != null)
                {
                    Sprite[] deathFrames = Resources.LoadAll<Sprite>(deathResource);
                    if (deathFrames != null && deathFrames.Length > 0)
                    {
                        spriteRenderer.sprite = deathFrames[0];
                    }
                    spriteRenderer.transform.localPosition = lockedLocalPos;
                }
                if (deathCoroutine == null)
                {
                    deathCoroutine = StartCoroutine(DestroyAfterAnimation(deathResource, monPig1Fps));
                }
                float deathDuration = GetAnimationDuration(deathResource, monPig1Fps);
                if (deathDuration > 0f)
                {
                    Destroy(gameObject, deathDuration);
                }
                return;
            }
        }

        Destroy(gameObject);
    }
    
    /// <summary>
    /// HP 바를 생성합니다
    /// </summary>
    void CreateHPBar()
    {
        usesBossHpBar = IsAnyBoss();

        GameObject barContainer = new GameObject(usesBossHpBar ? "BossHPBar" : "HPBar");
        barContainer.transform.SetParent(transform, false);
        hpBarContainer = barContainer.transform;
        ApplyHpBarContainerLayout();

        int baseOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 1;
        int shellOrder = usesBossHpBar ? baseOrder + BossHpBarSortingShell : baseOrder + 10;
        int trackOrder = usesBossHpBar ? baseOrder + BossHpBarSortingTrack : baseOrder + 11;
        int fillOrder = usesBossHpBar ? baseOrder + BossHpBarSortingFill : baseOrder + 12;

        GameObject shellObj = new GameObject("HPBarShell");
        shellObj.transform.SetParent(barContainer.transform, false);
        hpBarShellRenderer = shellObj.AddComponent<SpriteRenderer>();
        hpBarShellRenderer.sprite = usesBossHpBar
            ? WarmGaugeSprite.GetWorldBossHpShell()
            : WarmGaugeSprite.GetWorldEnemyHpShell();
        hpBarShellRenderer.color = usesBossHpBar ? BossHpShellColor : Color.white;
        hpBarShellRenderer.sortingOrder = shellOrder;

        GameObject trackObj = new GameObject("HPBarTrack");
        trackObj.transform.SetParent(barContainer.transform, false);
        hpBarTrackRenderer = trackObj.AddComponent<SpriteRenderer>();
        hpBarTrackRenderer.sprite = usesBossHpBar
            ? WarmGaugeSprite.GetWorldBossHpTrack()
            : WarmGaugeSprite.GetWorldEnemyHpTrack();
        hpBarTrackRenderer.color = usesBossHpBar ? BossHpTrackColor : Color.white;
        hpBarTrackRenderer.sortingOrder = trackOrder;

        GameObject fillObj = new GameObject("HPBarFill");
        fillObj.transform.SetParent(barContainer.transform, false);
        hpBarFillRenderer = fillObj.AddComponent<SpriteRenderer>();
        hpBarFillRenderer.sprite = usesBossHpBar
            ? WarmGaugeSprite.GetWorldBossHpFill()
            : WarmGaugeSprite.GetWorldEnemyHpFill();
        hpBarFillRenderer.color = usesBossHpBar ? BossHpFillColor : Color.white;
        hpBarFillRenderer.sortingOrder = fillOrder;
        fillObj.transform.localPosition = new Vector3(0f, 0f, -0.001f);

        hpBarFill = fillObj.transform;

        if (usesBossHpBar)
        {
            GameObject textObj = new GameObject("BossHpText");
            textObj.transform.SetParent(barContainer.transform, false);
            textObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);

            bossHpText = textObj.AddComponent<TextMesh>();
            bossHpText.fontSize = 32;
            bossHpText.characterSize = 0.11f;
            bossHpText.color = BossHpTextColor;
            bossHpText.fontStyle = FontStyle.Bold;
            bossHpText.alignment = TextAlignment.Center;
            bossHpText.anchor = TextAnchor.MiddleCenter;

            MeshRenderer hpTextRenderer = textObj.GetComponent<MeshRenderer>();
            if (hpTextRenderer != null)
            {
                hpTextRenderer.sortingOrder = baseOrder + BossHpBarSortingText;
            }
        }
    }

    void ApplyHpBarContainerLayout()
    {
        if (hpBarContainer == null) return;

        float yOffset = -2.08f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float spriteHeight = spriteRenderer.bounds.size.y;
            float parentScaleY = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
            float localSpriteHeight = spriteHeight / parentScaleY;
            yOffset = usesBossHpBar
                ? -(localSpriteHeight * 0.42f) - 0.28f
                : -(localSpriteHeight * 0.35f) - 0.2f;
            if (IsBoss1())
            {
                yOffset += 0.12f;
            }
        }

        hpBarContainer.localPosition = new Vector3(0f, yOffset, 0f);

        Vector3 parentScale = transform.localScale;
        if (!Mathf.Approximately(parentScale.x, 0f) && !Mathf.Approximately(parentScale.y, 0f))
        {
            hpBarContainer.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);
        }
    }

    /// <summary>루트 스케일 변경 후 보스 HP 바 위치·크기를 다시 맞춥니다.</summary>
    public void RefreshHpBarLayout()
    {
        if (hpBarContainer == null)
        {
            Transform found = transform.Find(usesBossHpBar ? "BossHPBar" : "HPBar");
            if (found == null) return;
            hpBarContainer = found;
        }

        ApplyHpBarContainerLayout();
        UpdateHPBar();
    }

    void UpdateHPBar()
    {
        if (hpBarFill == null || maxHealth <= 0) return;

        float ratio = Mathf.Clamp01((float)health / maxHealth);
        float barWidth = usesBossHpBar
            ? WarmGaugeSprite.WorldBossHpBarWidth
            : WarmGaugeSprite.WorldEnemyHpBarWidth;
        hpBarFill.localScale = new Vector3(ratio, 1f, 1f);
        hpBarFill.localPosition = new Vector3(-barWidth * 0.5f + (barWidth * ratio) * 0.5f, 0f, -0.001f);

        if (bossHpText != null)
        {
            bossHpText.text = $"{health:N0} / {maxHealth:N0}";
        }

        int baseOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 1;
        if (usesBossHpBar)
        {
            if (hpBarShellRenderer != null)
            {
                hpBarShellRenderer.sortingOrder = baseOrder + BossHpBarSortingShell;
                hpBarShellRenderer.color = BossHpShellColor;
            }
            if (hpBarTrackRenderer != null)
            {
                hpBarTrackRenderer.sortingOrder = baseOrder + BossHpBarSortingTrack;
                hpBarTrackRenderer.color = BossHpTrackColor;
            }
            if (hpBarFillRenderer != null)
            {
                hpBarFillRenderer.sortingOrder = baseOrder + BossHpBarSortingFill;
                hpBarFillRenderer.color = BossHpFillColor;
            }
            if (bossHpText != null)
            {
                MeshRenderer hpTextRenderer = bossHpText.GetComponent<MeshRenderer>();
                if (hpTextRenderer != null) hpTextRenderer.sortingOrder = baseOrder + BossHpBarSortingText;
            }
        }
        else
        {
            if (hpBarShellRenderer != null) hpBarShellRenderer.sortingOrder = baseOrder + 10;
            if (hpBarTrackRenderer != null) hpBarTrackRenderer.sortingOrder = baseOrder + 11;
            if (hpBarFillRenderer != null) hpBarFillRenderer.sortingOrder = baseOrder + 12;
        }
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
    
    /// <summary>
    /// 좀비가 속한 행을 설정합니다
    /// </summary>
    public void SetRow(int row)
    {
        currentRow = row;
    }

    void EnsureWalkSpriteVisible()
    {
        if (spriteRenderer == null || !UsesEnemySheetAnimation()) return;
        if (spriteRenderer.sprite != null) return;

        string walk = ResolveWalkResource();
        if (string.IsNullOrEmpty(walk) || !HasSpriteFrames(walk)) return;
        SetEnemySheetAnimation(walk, false);
    }

    void InitializeMovementAnimation()
    {
        if (spriteRenderer == null) return;
        if (!UsesEnemySheetAnimation()) return;

        string resourceName = ResolveWalkResource();
        if (string.IsNullOrEmpty(resourceName)) return;

        SetEnemySheetAnimation(resourceName, false);
    }

    void UpdateMovementAnimation(bool isMoving)
    {
        if (unit11FreezeActive)
        {
            if (spriteAnimator == null) return;
            string walkResource = ResolveWalkResource();
            if (string.IsNullOrEmpty(walkResource))
            {
                spriteAnimator.SetAnimating(false);
                return;
            }
            SetEnemySheetAnimation(walkResource, false);
            return;
        }
        if (spriteAnimator == null) return;
        if (isMoving)
        {
            string walkResource = ResolveWalkResource();
            SetEnemySheetAnimation(walkResource, true);
        }
        else if (currentMonPig1Animation == ResolveWalkResource())
        {
            spriteAnimator.SetAnimating(false);
        }
    }

    bool IsMonPig1()
    {
        if (zombieUID == "mon_pig_1") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "이족 오크")
        {
            return true;
        }
        return false;
    }

    bool IsMonPig2()
    {
        if (zombieUID == "mon_pig_2") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "오물 오크")
        {
            return true;
        }
        return false;
    }

    bool IsMonPig3()
    {
        if (zombieUID == "mon_pig_3") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미3")
        {
            return true;
        }
        return false;
    }

    bool UsesEnemySheetAnimation()
    {
        return IsMonPig1() || IsMonPig2() || IsMonPig3() || IsBoss1() || IsOozeBoss() || IsMonPig4() || IsMonPig5() || IsMonPig6() || IsMonPig7() || IsMonPig8() || IsMonPig9() || IsMonPig10() || IsNecromancerBoss();
    }

    bool IsMonPig7()
    {
        if (zombieUID == "mon_pig_7") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미7")
        {
            return true;
        }
        return false;
    }

    bool IsMonPig8()
    {
        if (zombieUID == "mon_pig_8") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미8")
        {
            return true;
        }
        return false;
    }

    bool IsMonPig9()
    {
        if (zombieUID == "mon_pig_9") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미9")
        {
            return true;
        }
        return false;
    }

    bool IsMonPig10()
    {
        if (zombieUID == "mon_pig_10") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미10")
        {
            return true;
        }
        return false;
    }

    bool IsMonPig4()
    {
        if (zombieUID == "mon_pig_4") return true;
        if (string.IsNullOrEmpty(zombieUID) && zombieData != null && zombieData.zombieName == "에너미4")
        {
            return true;
        }
        return false;
    }

    void CacheEnemy2LaneCellSize()
    {
        if (enemy2CachedCellSize > 0.0001f) return;
        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board != null && board.cellSize > 0.0001f)
        {
            enemy2CachedCellSize = board.cellSize;
            return;
        }
        GridManager grid = FindFirstObjectByType<GridManager>();
        if (grid != null && grid.cellSize > 0.0001f)
        {
            enemy2CachedCellSize = grid.cellSize;
            return;
        }
        enemy2CachedCellSize = 1f;
    }

    float GetEnemy2LaneCellSize()
    {
        if (enemy2CachedCellSize < 0.0001f)
        {
            CacheEnemy2LaneCellSize();
        }
        return enemy2CachedCellSize;
    }

    void TryStartEnemy2PeriodicShove()
    {
        if (!IsMonPig2() || isDead) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;
        if (IsStunned() || unit11FreezeActive) return;
        if (enemy2SpecialRoutine != null) return;
        if (targetBarrier != null && IsBarrierInRange(targetBarrier)) return;
        if (targetPlant != null && Vector2.Distance(transform.position, targetPlant.transform.position) <= attackRange)
        {
            return;
        }
        if (Time.time < enemy2NextSpecialAt) return;

        enemy2NextSpecialAt = Time.time + UnityEngine.Random.Range(4f, 6f);
        enemy2SpecialRoutine = StartCoroutine(Enemy2PeriodicShoveRoutine());
    }

    IEnumerator Enemy2PeriodicShoveRoutine()
    {
        enemy2SpecialMoveLock = true;
        string atk = ResolveAttackResource();
        if (!string.IsNullOrEmpty(atk))
        {
            SetEnemySheetAnimation(atk, true);
        }

        float dur = string.IsNullOrEmpty(atk) ? 0.5f : GetAnimationDuration(atk, monPig1Fps);
        if (dur < 0.15f)
        {
            dur = 0.5f;
        }

        float ballDelay = Mathf.Clamp(enemy2BallSpawnDelay, 0.06f, Mathf.Max(0.12f, dur - 0.08f));
        yield return new WaitForSeconds(ballDelay);
        SpawnEnemy2BallProjectile();
        float remain = dur - ballDelay;
        if (remain > 0.02f)
        {
            yield return new WaitForSeconds(remain);
        }

        enemy2SpecialMoveLock = false;
        string walk = ResolveWalkResource();
        if (!string.IsNullOrEmpty(walk))
        {
            SetEnemySheetAnimation(walk, true);
        }
        enemy2SpecialRoutine = null;
    }

    void SpawnEnemy2BallProjectile()
    {
        float cell = GetEnemy2LaneCellSize();
        GameObject ball = new GameObject("Enemy2Ball");
        ball.transform.position = transform.position + Vector3.left * 0.38f;
        SpriteRenderer sr = ball.AddComponent<SpriteRenderer>();
        Sprite ballSprite = Resources.Load<Sprite>("unit_1_attack");
        if (ballSprite == null)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>("unit_1_attack");
            if (frames != null && frames.Length > 0)
            {
                ballSprite = frames[0];
            }
        }
        if (ballSprite != null)
        {
            sr.sprite = ballSprite;
        }
        sr.color = new Color(1f, 0.55f, 0.12f, 1f);
        sr.sortingOrder = 8;
        float scaleMul = Mathf.Clamp(cell * 0.42f, 0.25f, 1.2f);
        ball.transform.localScale = Vector3.one * scaleMul;
        Unit1ProjectileAfterimage.Attach(ball);

        Enemy2BallProjectile proj = ball.AddComponent<Enemy2BallProjectile>();
        proj.Init(this, currentRow, baseX, cell);
    }

    public void ApplyEnemy2BoostForward(float laneCellSize)
    {
        if (isDead) return;

        float step = laneCellSize > 0.01f ? laneCellSize : 1f;
        baseX -= step;
        Vector3 pos = transform.position;
        pos.x = baseX;
        pos.y = baseY;
        transform.position = pos;
        ClampToBarrierIfNeeded();
    }

    public void ShowEnemy2BoostMessage(Color color)
    {
        float yOffset = 0.3f;
        EnsureSpriteRenderer();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            yOffset = spriteRenderer.bounds.extents.y + 0.12f;
        }

        string layerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        int order = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
        DamagePopup.CreateMessage(transform.position + Vector3.up * yOffset, "1칸 점프!", color, layerName, order);
    }

    string ResolveWalkResource()
    {
        if (IsOozeBoss())
        {
            if (HasSpriteFrames(OozeBossWalk)) return OozeBossWalk;
            return null;
        }
        if (IsBoss1())
        {
            if (HasSpriteFrames(Boss1Walk)) return Boss1Walk;
            return null;
        }
        if (IsNecromancerBoss())
        {
            if (HasSpriteFrames(NecromancerWalk)) return NecromancerWalk;
            return null;
        }
        if (IsMonPig10())
        {
            if (HasSpriteFrames(Enemy10Walk)) return Enemy10Walk;
            return null;
        }
        if (IsMonPig9())
        {
            if (HasSpriteFrames(Enemy9Walk)) return Enemy9Walk;
            return null;
        }
        if (IsMonPig8())
        {
            if (HasSpriteFrames("enemy_8_walk-export")) return "enemy_8_walk-export";
            return null;
        }
        if (IsMonPig7())
        {
            if (HasSpriteFrames("enemy_7_Walk-export")) return "enemy_7_Walk-export";
            return null;
        }
        if (IsMonPig6())
        {
            if (HasSpriteFrames("enemy_6_walk-export")) return "enemy_6_walk-export";
            return null;
        }
        if (IsMonPig5())
        {
            if (HasSpriteFrames("enemy_5_Walk-export")) return "enemy_5_Walk-export";
            return null;
        }
        if (IsMonPig4())
        {
            if (HasSpriteFrames("enemy_4_Walk-export")) return "enemy_4_Walk-export";
            return null;
        }
        if (IsMonPig3())
        {
            if (HasSpriteFrames("enemy_3_Walk")) return "enemy_3_Walk";
            if (HasSpriteFrames("enemy_3_Walkt")) return "enemy_3_Walkt";
            return null;
        }
        if (IsMonPig2())
        {
            if (HasSpriteFrames("enemy_2_Walk-export")) return "enemy_2_Walk-export";
            if (HasSpriteFrames("enemy_2_Walk")) return "enemy_2_Walk";
            return null;
        }
        return ResolveMonPig1WalkResource();
    }

    string ResolveMonPig1WalkResource()
    {
        if (HasSpriteFrames("enemy_1_wark")) return "enemy_1_wark";
        if (HasSpriteFrames("enemy_1_walk")) return "enemy_1_walk";
        return null;
    }

    string ResolveAttackResource()
    {
        if (IsNecromancerBoss())
        {
            if (HasSpriteFrames(NecromancerAtk1)) return NecromancerAtk1;
            return null;
        }
        if (IsMonPig10())
        {
            if (HasSpriteFrames(Enemy10Attack)) return Enemy10Attack;
            return null;
        }
        if (IsMonPig9())
        {
            if (HasSpriteFrames(Enemy9Attack)) return Enemy9Attack;
            return null;
        }
        if (IsMonPig8())
        {
            if (HasSpriteFrames("enemy_8_attack-export")) return "enemy_8_attack-export";
            return null;
        }
        if (IsMonPig7())
        {
            if (HasSpriteFrames("enemy_7_Attack-export")) return "enemy_7_Attack-export";
            return null;
        }
        if (IsMonPig6())
        {
            if (HasSpriteFrames("enemy_6_attack-export")) return "enemy_6_attack-export";
            return null;
        }
        if (IsMonPig5())
        {
            if (HasSpriteFrames("enemy_5_Attack-export")) return "enemy_5_Attack-export";
            return null;
        }
        if (IsMonPig4())
        {
            if (HasSpriteFrames("enemy_4_Attack-export")) return "enemy_4_Attack-export";
            return null;
        }
        if (IsMonPig3())
        {
            if (HasSpriteFrames("enemy_3_Attack")) return "enemy_3_Attack";
            return null;
        }
        if (IsMonPig2())
        {
            if (HasSpriteFrames("enemy_2_Attack-export")) return "enemy_2_Attack-export";
            if (HasSpriteFrames("enemy_2_Attack")) return "enemy_2_Attack";
            return null;
        }
        return ResolveMonPig1AttackResource();
    }

    string ResolveMonPig1AttackResource()
    {
        if (HasSpriteFrames("enemy_1_attack")) return "enemy_1_attack";
        return null;
    }

    string ResolveDeathResource()
    {
        if (IsOozeBoss())
        {
            if (HasSpriteFrames(OozeBossDeathPrimary)) return OozeBossDeathPrimary;
            if (HasSpriteFrames(OozeBossDeathFallback)) return OozeBossDeathFallback;
            return null;
        }
        if (IsBoss1())
        {
            if (HasSpriteFrames(Boss1Death)) return Boss1Death;
            return null;
        }
        if (IsNecromancerBoss())
        {
            if (HasSpriteFrames(NecromancerDeath)) return NecromancerDeath;
            return null;
        }
        if (IsMonPig10())
        {
            if (HasSpriteFrames(Enemy10DeathSheet)) return Enemy10DeathSheet;
            return null;
        }
        if (IsMonPig9())
        {
            if (HasSpriteFrames(Enemy9DeathSheet)) return Enemy9DeathSheet;
            return null;
        }
        if (IsMonPig8())
        {
            if (HasSpriteFrames("enemy_8_death-export")) return "enemy_8_death-export";
            return null;
        }
        if (IsMonPig7())
        {
            if (HasSpriteFrames("enemy_7_Death-export")) return "enemy_7_Death-export";
            return null;
        }
        if (IsMonPig6())
        {
            if (HasSpriteFrames("enemy_6_death-export")) return "enemy_6_death-export";
            return null;
        }
        if (IsMonPig5())
        {
            if (HasSpriteFrames("enemy_5_Death-export")) return "enemy_5_Death-export";
            return null;
        }
        if (IsMonPig4())
        {
            if (HasSpriteFrames("enemy_4_Death-export")) return "enemy_4_Death-export";
            return null;
        }
        if (IsMonPig3())
        {
            if (HasSpriteFrames("enemy_3_Death")) return "enemy_3_Death";
            return null;
        }
        if (IsMonPig2())
        {
            if (HasSpriteFrames("enemy_2_Death-export")) return "enemy_2_Death-export";
            if (HasSpriteFrames("enemy_2_Death")) return "enemy_2_Death";
            return null;
        }
        return ResolveMonPig1DeathResource();
    }

    string ResolveMonPig1DeathResource()
    {
        if (HasSpriteFrames("enemy_1_death")) return "enemy_1_death";
        return null;
    }

    Sprite FindEnemy9DeathHoldSprite()
    {
        Sprite[] a = Resources.LoadAll<Sprite>(Enemy9DeathSheet);
        if (a == null) return null;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != null && a[i].name == Enemy9DeathHoldSpriteName)
            {
                return a[i];
            }
        }
        return null;
    }

    Sprite[] LoadEnemy9DeathFramesSorted()
    {
        Sprite[] a = Resources.LoadAll<Sprite>(Enemy9DeathSheet);
        if (a == null || a.Length == 0) return null;
        System.Array.Sort(a, (x, y) => string.CompareOrdinal(x.name, y.name));
        return a;
    }

    IEnumerator Enemy9KnockdownReviveRoutine()
    {
        enemy9CorpsePhase = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (spriteAnimator != null)
        {
            spriteAnimator.SetAnimating(false);
        }
        Sprite hold = FindEnemy9DeathHoldSprite();
        if (hold != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = hold;
            spriteRenderer.color = Color.white;
        }
        yield return new WaitForSeconds(2f);

        Sprite[] frames = LoadEnemy9DeathFramesSorted();
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
        {
            float perFrame = Mathf.Clamp(0.55f / frames.Length, 0.04f, 0.14f);
            for (int i = frames.Length - 1; i >= 0; i--)
            {
                if (frames[i] != null)
                {
                    spriteRenderer.sprite = frames[i];
                }
                yield return new WaitForSeconds(perFrame);
            }
        }

        enemy9CorpsePhase = false;
        if (col != null) col.enabled = true;
        health = maxHealth;
        UpdateHPBar();
        enemy9ReviveAvailable = false;
        enemy9ReviveRoutine = null;

        string walk = ResolveWalkResource();
        if (!string.IsNullOrEmpty(walk) && HasSpriteFrames(walk))
        {
            SetEnemySheetAnimation(walk, true);
        }
    }

    void SetMonPig1AttackAnimation()
    {
        if (!UsesEnemySheetAnimation()) return;
        string attackResource = ResolveAttackResource();
        if (string.IsNullOrEmpty(attackResource)) return;
        SetEnemySheetAnimation(attackResource, true);
    }

    void SetEnemySheetAnimation(string resourceName, bool animate)
    {
        if (spriteRenderer == null) return;
        if (string.IsNullOrEmpty(resourceName)) return;

        if (spriteAnimator == null && spriteRenderer != null)
        {
            spriteAnimator = spriteRenderer.gameObject.AddComponent<UnitSpriteAnimator>();
        }

        if (currentMonPig1Animation != resourceName)
        {
            spriteAnimator.Initialize(spriteRenderer, resourceName, monPig1Fps);
            string deathResource = ResolveDeathResource();
            if (string.IsNullOrEmpty(deathResource) || resourceName != deathResource)
            {
                spriteAnimator.EnableBottomAnchor(true);
            }
            currentMonPig1Animation = resourceName;
        }
        spriteAnimator.SetAnimating(animate);
    }

    System.Collections.IEnumerator DestroyAfterAnimation(string resourceName, float fps)
    {
        float duration = GetAnimationDuration(resourceName, fps);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
        Destroy(gameObject);
    }

    float GetAnimationDuration(string resourceName, float fps)
    {
        if (string.IsNullOrEmpty(resourceName) || fps <= 0f) return 0f;
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
        if (sprites == null || sprites.Length == 0) return 0f;
        return sprites.Length / fps;
    }

    bool HasSpriteFrames(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return false;
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
        return sprites != null && sprites.Length > 0;
    }

    static int TrailingIntFromSpriteName(Sprite s)
    {
        if (s == null) return 0;
        return TrailingIntFromName(s.name);
    }

    static int TrailingIntFromName(string n)
    {
        if (string.IsNullOrEmpty(n)) return 0;
        int e = n.Length - 1;
        while (e >= 0 && !char.IsDigit(n[e])) e--;
        if (e < 0) return 0;
        int i = e;
        while (i >= 0 && char.IsDigit(n[i])) i--;
        return int.Parse(n.Substring(i + 1, e - i));
    }
    
    void RunZombieSeparation()
    {
        for (int pass = 0; pass < CrowdSeparationPasses; pass++)
        {
            int n = s_activeZombies.Count;
            for (int i = 0; i < n; i++)
            {
                Zombie a = s_activeZombies[i];
                if (a == null) continue;
                for (int j = i + 1; j < n; j++)
                {
                    Zombie b = s_activeZombies[j];
                    if (b == null) continue;
                    ResolveZombiePair(a, b);
                }
            }
        }
        int n2 = s_activeZombies.Count;
        for (int i = 0; i < n2; i++)
        {
            Zombie z = s_activeZombies[i];
            if (z != null && !z.isDead)
            {
                z.ClampToBarrierIfNeeded();
            }
        }
    }

    /// <summary>
    /// 벽(방어막) 서쪽 면을 통과하지 못하게 보정. 이미 벽 서쪽(전장 안)으로 들어간 좀비는 건드리지 않음.
    /// </summary>
    void ClampToBarrierIfNeeded()
    {
        if (isDead) return;
        Barrier barrier = Barrier.Instance;
        if (barrier == null || !barrier.IsAlive()) return;
        Bounds barrierBounds = barrier.GetWorldBounds();
        Bounds zb = GetBodyBounds();
        const float westFreeEpsilon = 0.02f;
        if (zb.max.x <= barrierBounds.min.x - westFreeEpsilon)
        {
            return;
        }
        const float attackSlop = 0.1f;
        float minAllowedMinX = barrierBounds.min.x - attackSlop;
        if (zb.min.x >= minAllowedMinX)
        {
            return;
        }
        float delta = minAllowedMinX - zb.min.x;
        baseX += delta;
        Vector3 p = transform.position;
        p.x = baseX;
        p.y = baseY;
        transform.position = p;
    }

    static bool WestNudgeBlockedByBarrier(Zombie west, float westEach)
    {
        if (west == null || westEach <= 0f || west.isDead) return false;
        Barrier barrier = Barrier.Instance;
        if (barrier == null || !barrier.IsAlive()) return false;
        Bounds barrierBounds = barrier.GetWorldBounds();
        Bounds zb = west.GetBodyBounds();
        if (zb.max.x <= barrierBounds.min.x - 0.02f)
        {
            return false;
        }
        const float attackSlop = 0.1f;
        float minAllowedMinX = barrierBounds.min.x - attackSlop;
        return zb.min.x - westEach < minAllowedMinX;
    }
    
    static void ResolveZombiePair(Zombie a, Zombie b)
    {
        if (a == null || b == null) return;
        if (a.isDead || b.isDead) return;
        float k = 0.5f * (a.crowdSeparationStiffness + b.crowdSeparationStiffness);
        if (k < 0.0001f) return;
        
        Bounds ba = a.GetBodyBounds();
        Bounds bb = b.GetBodyBounds();
        if (!ba.Intersects(bb)) return;
        
        float minX = Mathf.Max(ba.min.x, bb.min.x);
        float maxX = Mathf.Min(ba.max.x, bb.max.x);
        float minY = Mathf.Max(ba.min.y, bb.min.y);
        float maxY = Mathf.Min(ba.max.y, bb.max.y);
        float ox = maxX - minX;
        float oy = maxY - minY;
        if (ox <= 0f || oy <= 0f) return;
        
        const float padding = 0.035f;
        float eachH = (0.5f * ox + 0.5f * padding) * k;
        float eachV = (0.5f * oy + 0.5f * padding) * k;
        bool useHorizontal = ox < oy;
        if (useHorizontal)
        {
            Zombie west = ba.center.x < bb.center.x ? a : b;
            if (WestNudgeBlockedByBarrier(west, eachH))
            {
                useHorizontal = false;
            }
        }
        if (useHorizontal)
        {
            float each = eachH;
            if (ba.center.x < bb.center.x)
            {
                a.ApplySeparationNudge(-each, 0f);
                b.ApplySeparationNudge(each, 0f);
            }
            else
            {
                a.ApplySeparationNudge(each, 0f);
                b.ApplySeparationNudge(-each, 0f);
            }
        }
        else
        {
            float each = eachV;
            if (ba.center.y < bb.center.y)
            {
                a.ApplySeparationNudge(0f, -each);
                b.ApplySeparationNudge(0f, each);
            }
            else
            {
                a.ApplySeparationNudge(0f, each);
                b.ApplySeparationNudge(0f, -each);
            }
        }
    }
    
    void ApplySeparationNudge(float dx, float dy)
    {
        if (isDead || ResistsFreezeAndKnockback()) return;
        baseX += dx;
        baseY += dy;
        Vector3 p = transform.position;
        p.x = baseX;
        p.y = baseY;
        transform.position = p;
        ClampToBarrierIfNeeded();
    }
    
    public Bounds GetBodyBounds()
    {
        if (spriteRenderer != null) return spriteRenderer.bounds;
        Collider2D c2 = GetComponent<Collider2D>();
        if (c2 != null) return c2.bounds;
        return new Bounds(new Vector3(baseX, baseY, 0f), new Vector3(0.5f, 0.5f, 0.1f));
    }

    void UpdateNecromancerBoss()
    {
        FindTargetBarrier();
        UpdateSlowEffect();
        if (necromancerPatternRoutine != null)
        {
            return;
        }

        if (targetBarrier != null && !IsStunned() && IsBarrierInRange(targetBarrier))
        {
            if (HasSpriteFrames(NecromancerAtk3))
            {
                SetEnemySheetAnimation(NecromancerAtk3, true);
            }
            AttackBarrier();
            return;
        }

        float cell = NecromancerGetCellSize();
        bool siege = NecromancerIsInSiegeRange(cell);

        if (!siege)
        {
            if (necromancerSiegeMode)
            {
                necromancerSiegeMode = false;
                necromancerSiegeIdleDone = false;
            }
            MoveLeft();
            if (HasSpriteFrames(NecromancerWalk))
            {
                SetEnemySheetAnimation(NecromancerWalk, true);
            }
            if (Time.time >= necromancerNextPatternAt)
            {
                int p = UnityEngine.Random.Range(1, 5);
                necromancerPatternRoutine = StartCoroutine(NecromancerPatternSequence(p, false));
            }
            return;
        }

        necromancerSiegeMode = true;
        if (!necromancerSiegeIdleDone)
        {
            necromancerPatternRoutine = StartCoroutine(NecromancerSiegeIdleIntro());
            return;
        }

        if (Time.time >= necromancerNextPatternAt)
        {
            int p = UnityEngine.Random.Range(1, 5);
            necromancerPatternRoutine = StartCoroutine(NecromancerPatternSequence(p, true));
        }
    }

    float NecromancerGetCellSize()
    {
        BoardManager bm = FindFirstObjectByType<BoardManager>();
        if (bm != null && bm.cellSize > 0.01f)
        {
            return bm.cellSize;
        }
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null && gm.cellSize > 0.01f)
        {
            return gm.cellSize;
        }
        return 1f;
    }

    bool NecromancerIsInSiegeRange(float cell)
    {
        Barrier barrier = Barrier.Instance;
        if (barrier == null || !barrier.IsAlive())
        {
            return false;
        }
        Bounds bb = barrier.GetWorldBounds();
        Bounds zb = GetBodyBounds();
        float gap = bb.min.x - zb.max.x;
        return gap >= -0.15f && gap <= cell * 1.15f;
    }

    IEnumerator NecromancerSiegeIdleIntro()
    {
        if (HasSpriteFrames(NecromancerIdle))
        {
            SetEnemySheetAnimation(NecromancerIdle, true);
        }
        float dur = Mathf.Max(0.12f, GetAnimationDuration(NecromancerIdle, monPig1Fps));
        yield return new WaitForSeconds(dur);
        if (spriteAnimator != null)
        {
            spriteAnimator.SetAnimating(false);
        }
        necromancerSiegeIdleDone = true;
        necromancerNextPatternAt = Time.time + 0.35f;
        necromancerPatternRoutine = null;
    }

    IEnumerator NecromancerPatternSequence(int pattern, bool siegeSpam)
    {
        string atk = pattern == 1 ? NecromancerAtk1
            : pattern == 2 ? NecromancerAtk2
            : pattern == 3 ? NecromancerAtk3 : NecromancerAtk4;
        if (HasSpriteFrames(atk))
        {
            SetEnemySheetAnimation(atk, true);
        }
        float dur = Mathf.Max(0.1f, GetAnimationDuration(atk, monPig1Fps));
        yield return new WaitForSeconds(dur * 0.55f);
        NecromancerApplyPatternEffect(pattern);
        yield return new WaitForSeconds(dur * 0.45f);
        if (spriteAnimator != null)
        {
            spriteAnimator.SetAnimating(false);
        }
        necromancerNextPatternAt = Time.time + (siegeSpam
            ? UnityEngine.Random.Range(1.5f, 3f)
            : UnityEngine.Random.Range(3f, 5f));
        necromancerPatternRoutine = null;
    }

    void NecromancerApplyPatternEffect(int pattern)
    {
        ZombieSpawner sp = necromancerCachedSpawner != null ? necromancerCachedSpawner : FindFirstObjectByType<ZombieSpawner>();
        float cell = NecromancerGetCellSize();
        Vector3 spawnPos = new Vector3(transform.position.x - Mathf.Max(0.35f, cell * 0.55f), baseY, 0f);

        if (pattern == 1)
        {
            if (sp != null)
            {
                sp.SpawnZombieUidAtPosition("mon_pig_9", spawnPos, currentRow);
            }
            return;
        }
        if (pattern == 4)
        {
            if (sp != null)
            {
                sp.SpawnZombieUidAtPosition("mon_pig_10", spawnPos, currentRow);
            }
            return;
        }
        if (pattern == 2)
        {
            Character ch = NecromancerPickRandomBoardCharacter();
            if (ch == null)
            {
                return;
            }
            GameObject orb = new GameObject("NecromancerHexOrb");
            orb.transform.position = transform.position + Vector3.up * 0.12f;
            SpriteRenderer sr = orb.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 30;
            Sprite s = NecromancerLoadRandomProjectileSprite();
            if (s != null)
            {
                sr.sprite = s;
                orb.transform.localScale = Vector3.one * 1.15f;
            }
            else
            {
                sr.sprite = NecromancerCreateFallbackOrbSprite();
            }
            NecromancerHexOrb hex = orb.AddComponent<NecromancerHexOrb>();
            hex.target = ch;
            return;
        }
        if (pattern == 3)
        {
            GameObject slash = new GameObject("NecromancerBarrierSlash");
            slash.transform.position = transform.position + Vector3.up * 0.08f;
            SpriteRenderer sr = slash.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 28;
            Sprite[] frames = Resources.LoadAll<Sprite>(NecromancerAtk3);
            if (frames != null && frames.Length > 0)
            {
                System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
                sr.sprite = frames[0];
            }
            slash.transform.localScale = Vector3.one * 1.2f;
            NecromancerBarrierSlash bs = slash.AddComponent<NecromancerBarrierSlash>();
            bs.damage = 2;
        }
    }

    Character NecromancerPickRandomBoardCharacter()
    {
        Character[] all = FindObjectsByType<Character>(FindObjectsSortMode.None);
        List<Character> list = new List<Character>(8);
        for (int i = 0; i < all.Length; i++)
        {
            Character c = all[i];
            if (c != null && c.IsProperlyPlaced())
            {
                list.Add(c);
            }
        }
        if (list.Count == 0)
        {
            return null;
        }
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    static readonly string[] kNecromancerOrbSheets =
    {
        "unit_001_attack", "unit_002_attack", "unit_005_attack", "unit_003_attack"
    };

    Sprite NecromancerLoadRandomProjectileSprite()
    {
        int idx = UnityEngine.Random.Range(0, kNecromancerOrbSheets.Length);
        for (int k = 0; k < kNecromancerOrbSheets.Length; k++)
        {
            string name = kNecromancerOrbSheets[(idx + k) % kNecromancerOrbSheets.Length];
            Sprite[] a = Resources.LoadAll<Sprite>(name);
            if (a != null && a.Length > 0)
            {
                System.Array.Sort(a, (x, y) => string.CompareOrdinal(x.name, y.name));
                return a[0];
            }
        }
        return null;
    }

    Sprite NecromancerCreateFallbackOrbSprite()
    {
        Texture2D t = new Texture2D(8, 8);
        Color g = new Color(0.35f, 0.92f, 0.45f, 1f);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                t.SetPixel(x, y, g);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
    }
}

