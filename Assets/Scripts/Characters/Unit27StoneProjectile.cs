using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 27 전용: 욘두 화살식 — 화면 안 로밍(지그재그·벽 반사) 후 적 추적.
/// 경로상 적은 생애당 1회, 겹침 유지 중에는 초당 1회 추가 피해.
/// </summary>
public class Unit27StoneProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 3;
    public int sourceUnitNumber;
    public float lifeTime = 10f;

    [Tooltip("로밍(삥삥) 단계 지속 시간(초)")]
    public float roamDuration = 3.5f;
    [Tooltip("로밍 중 이 횟수만큼 적중하면 즉시 추적 단계로 전환")]
    public int roamHitsToChase = 2;
    [Tooltip("추적 단계 속도 배율")]
    public float chaseSpeedMultiplier = 1.25f;
    [Tooltip("추적 단계 재조준 간격(초)")]
    public float chaseRetargetInterval = 0.35f;
    public float stickyDamageInterval = 1f;

    private enum Phase { Roam, Chase }

    private Phase phase = Phase.Roam;
    private float expireTime;
    private float roamEndTime;
    private float nextChaseRetargetTime;
    private float nextWaypointTime;
    private int roamHitCount;

    private Vector2 direction = Vector2.right;
    private Vector2 roamWaypoint;
    private Zombie chaseTarget;
    private Zombie preferredTarget;

    private CircleCollider2D hitCollider;
    private readonly HashSet<Zombie> overlapThisFrame = new HashSet<Zombie>();
    /// <summary>이 투사체가 이동 경로에서 이미 맞춘 적(1회만).</summary>
    private readonly HashSet<Zombie> pathDamagedZombies = new HashSet<Zombie>();
    /// <summary>겹침 유지 중 다음 초당 피해 시각.</summary>
    private readonly Dictionary<Zombie, float> nextStickyDamageTime = new Dictionary<Zombie, float>();

    const float BoundsPadding = 0.35f;
    const float WaypointReachDistance = 0.45f;
    const float WaypointPickInterval = 0.55f;
    const float RoamDirectionBlend = 14f;
    const float RoamJitterStrength = 0.35f;
    const float BoundsBounceSeparation = 0.08f;

    void Start()
    {
        Unit1ProjectileAfterimage.Attach(gameObject);
        expireTime = Time.time + lifeTime;
        roamEndTime = Time.time + roamDuration;
        nextWaypointTime = 0f;
        nextChaseRetargetTime = 0f;

        direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.right;
        }

        PickNewRoamWaypoint();

        hitCollider = gameObject.AddComponent<CircleCollider2D>();
        hitCollider.isTrigger = true;
        hitCollider.radius = Mathf.Max(transform.localScale.x * 0.5f, 1.2f);

        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    public void SetInitialTarget(Zombie target)
    {
        preferredTarget = target;
        chaseTarget = target;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        if (Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }

        overlapThisFrame.Clear();

        if (phase == Phase.Roam)
        {
            UpdateRoamMovement();
            if (roamHitCount >= roamHitsToChase || Time.time >= roamEndTime)
            {
                EnterChasePhase();
            }
        }
        else
        {
            UpdateChaseMovement();
        }

        transform.Rotate(0f, 0f, -420f * Time.deltaTime);

        CollectOverlappingZombies();
        ApplyPathAndStickyDamage();
    }

    void UpdateRoamMovement()
    {
        if (Time.time >= nextWaypointTime)
        {
            PickNewRoamWaypoint();
            nextWaypointTime = Time.time + WaypointPickInterval;
        }

        Vector2 toWaypoint = roamWaypoint - (Vector2)transform.position;
        Vector2 desired = toWaypoint.sqrMagnitude > 0.001f ? toWaypoint.normalized : direction;
        if (toWaypoint.magnitude < WaypointReachDistance)
        {
            PickNewRoamWaypoint();
        }

        Vector2 jitter = Random.insideUnitCircle * RoamJitterStrength;
        desired = (desired + jitter).normalized;

        float blend = 1f - Mathf.Exp(-RoamDirectionBlend * Time.deltaTime);
        direction = Vector2.Lerp(direction, desired, blend).normalized;

        float moveSpeed = speed;
        Vector3 nextPos = transform.position + (Vector3)(direction * moveSpeed * Time.deltaTime);
        ReflectOffCameraBounds(ref direction, ref nextPos);
        transform.position = nextPos;
    }

    void UpdateChaseMovement()
    {
        if (Time.time >= nextChaseRetargetTime)
        {
            chaseTarget = ResolveChaseTarget();
            nextChaseRetargetTime = Time.time + chaseRetargetInterval;
        }

        if (chaseTarget != null && !chaseTarget.IsExcludedFromCombat)
        {
            Vector2 toTarget = chaseTarget.transform.position - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                direction = toTarget.normalized;
            }
        }
        else
        {
            chaseTarget = FindNearestVisibleZombie();
            if (chaseTarget != null)
            {
                Vector2 toTarget = chaseTarget.transform.position - transform.position;
                direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : direction;
            }
        }

        float moveSpeed = speed * chaseSpeedMultiplier;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    void EnterChasePhase()
    {
        phase = Phase.Chase;
        chaseTarget = ResolveChaseTarget();
        nextChaseRetargetTime = Time.time;
        if (chaseTarget != null)
        {
            Vector2 toTarget = chaseTarget.transform.position - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                direction = toTarget.normalized;
            }
        }
    }

    Zombie ResolveChaseTarget()
    {
        if (preferredTarget != null && !preferredTarget.IsExcludedFromCombat)
        {
            return preferredTarget;
        }

        Zombie nearest = FindNearestVisibleZombie();
        if (nearest != null)
        {
            return nearest;
        }

        return FindNearestZombie();
    }

    void PickNewRoamWaypoint()
    {
        if (!TryGetCameraBounds(out Bounds bounds))
        {
            roamWaypoint = (Vector2)transform.position + Random.insideUnitCircle * 3f;
            return;
        }

        float minX = bounds.min.x + BoundsPadding;
        float maxX = bounds.max.x - BoundsPadding;
        float minY = bounds.min.y + BoundsPadding;
        float maxY = bounds.max.y - BoundsPadding;
        if (maxX <= minX || maxY <= minY)
        {
            roamWaypoint = bounds.center;
            return;
        }

        roamWaypoint = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    void ReflectOffCameraBounds(ref Vector2 dir, ref Vector3 pos)
    {
        if (!TryGetCameraBounds(out Bounds bounds))
        {
            return;
        }

        float minX = bounds.min.x + BoundsPadding;
        float maxX = bounds.max.x - BoundsPadding;
        float minY = bounds.min.y + BoundsPadding;
        float maxY = bounds.max.y - BoundsPadding;

        bool reflected = false;
        if (pos.x < minX)
        {
            pos.x = minX + BoundsBounceSeparation;
            dir.x = Mathf.Abs(dir.x);
            reflected = true;
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX - BoundsBounceSeparation;
            dir.x = -Mathf.Abs(dir.x);
            reflected = true;
        }

        if (pos.y < minY)
        {
            pos.y = minY + BoundsBounceSeparation;
            dir.y = Mathf.Abs(dir.y);
            reflected = true;
        }
        else if (pos.y > maxY)
        {
            pos.y = maxY - BoundsBounceSeparation;
            dir.y = -Mathf.Abs(dir.y);
            reflected = true;
        }

        if (reflected)
        {
            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Random.insideUnitCircle.normalized;
            }
            else
            {
                dir = dir.normalized;
            }

            PickNewRoamWaypoint();
            nextWaypointTime = Time.time + WaypointPickInterval * 0.5f;
        }
    }

    float GetHitRadius()
    {
        return hitCollider != null
            ? hitCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y)
            : Mathf.Max(transform.localScale.x * 0.5f, 1.0f);
    }

    void CollectOverlappingZombies()
    {
        float hitRadius = GetHitRadius();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null) continue;

            Zombie zombie = col.GetComponentInParent<Zombie>();
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            overlapThisFrame.Add(zombie);
        }

        if (overlapThisFrame.Count > 0)
        {
            return;
        }

        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie zombie = zombies[i];
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float distance = Vector2.Distance(transform.position, zombie.transform.position);
            if (distance < hitRadius)
            {
                overlapThisFrame.Add(zombie);
            }
        }
    }

    void ApplyPathAndStickyDamage()
    {
        float now = Time.time;

        foreach (Zombie zombie in overlapThisFrame)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;

            if (!pathDamagedZombies.Contains(zombie))
            {
                DealDamage(zombie);
                pathDamagedZombies.Add(zombie);
                nextStickyDamageTime[zombie] = now + stickyDamageInterval;

                if (phase == Phase.Roam)
                {
                    roamHitCount++;
                }
            }
            else if (!nextStickyDamageTime.TryGetValue(zombie, out float nextTick))
            {
                nextStickyDamageTime[zombie] = now + stickyDamageInterval;
            }
            else if (now >= nextTick)
            {
                DealDamage(zombie);
                nextStickyDamageTime[zombie] = now + stickyDamageInterval;
            }
        }

        PruneStickyTimers();
        PrunePathDamageSet();
    }

    void DealDamage(Zombie zombie)
    {
        if (zombie == null || zombie.IsExcludedFromCombat) return;
        zombie.TakeDamage(damage, sourceUnitNumber);
        AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
    }

    void PruneStickyTimers()
    {
        List<Zombie> remove = null;
        foreach (KeyValuePair<Zombie, float> kvp in nextStickyDamageTime)
        {
            if (kvp.Key == null || !overlapThisFrame.Contains(kvp.Key))
            {
                if (remove == null) remove = new List<Zombie>();
                remove.Add(kvp.Key);
            }
        }

        if (remove == null) return;
        for (int i = 0; i < remove.Count; i++)
        {
            nextStickyDamageTime.Remove(remove[i]);
        }
    }

    void PrunePathDamageSet()
    {
        List<Zombie> remove = null;
        foreach (Zombie zombie in pathDamagedZombies)
        {
            if (zombie == null)
            {
                if (remove == null) remove = new List<Zombie>();
                remove.Add(zombie);
            }
        }

        if (remove == null) return;
        for (int i = 0; i < remove.Count; i++)
        {
            pathDamagedZombies.Remove(remove[i]);
            nextStickyDamageTime.Remove(remove[i]);
        }
    }

    Zombie FindNearestVisibleZombie()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        if (zombies == null || zombies.Length == 0) return null;

        if (!TryGetCameraBounds(out Bounds bounds))
        {
            return FindNearestZombie();
        }

        Zombie best = null;
        float bestDist = float.MaxValue;
        Vector2 pos = transform.position;
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            Vector3 zPos = zombie.transform.position;
            if (zPos.x < bounds.min.x || zPos.x > bounds.max.x ||
                zPos.y < bounds.min.y || zPos.y > bounds.max.y)
            {
                continue;
            }

            float d = Vector2.SqrMagnitude((Vector2)zPos - pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = zombie;
            }
        }

        return best != null ? best : FindNearestZombie();
    }

    Zombie FindNearestZombie()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        if (zombies == null || zombies.Length == 0) return null;

        Zombie best = null;
        float bestDist = float.MaxValue;
        Vector2 pos = transform.position;
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float d = Vector2.SqrMagnitude((Vector2)zombie.transform.position - pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = zombie;
            }
        }

        return best;
    }

    bool TryGetCameraBounds(out Bounds bounds)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            bounds = new Bounds();
            return false;
        }

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 center = cam.transform.position;
        bounds = new Bounds(center, new Vector3(width, height, 0f));
        return true;
    }
}
