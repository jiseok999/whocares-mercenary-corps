using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 28: 멀리 있는 적 쪽으로 날아가 착지 후 위치에 잠시 머문다.
/// 고정 반경·최대 5명·클러스터 한계 거리 내에서만 살짝 끌며, 보스는 끌기 면역.
/// </summary>
public class Unit28VortexProjectile : MonoBehaviour
{
    public float flightSpeed = 10f;
    public float landStayDuration = 2f;
    public float pullRadius = 1.3f;
    public float pullSpeed = 2.25f;
    public float minClusterRadius = 0.9f;
    public float maxPullDistancePerEnemy = 1.5f;
    public int maxPullTargets = 5;
    public float contactRadius = 1.1f;
    public float damageInterval = 1f;
    public int sourceUnitNumber;
    public bool lingeringAuraOnEnd = false;
    public float lingerDuration = 1.5f;
    public int lingerDamage = 1;

    const float EdgeBandFraction = 0.22f;
    const float EdgePullDistanceMul = 0.18f;

    Vector3 destination;
    int contactDamage;
    bool landed;
    float landStartTime;
    readonly Dictionary<Zombie, float> nextContactDamage = new Dictionary<Zombie, float>();
    readonly Dictionary<Zombie, float> pullDistanceUsed = new Dictionary<Zombie, float>();
    readonly HashSet<Zombie> edgePulledOnce = new HashSet<Zombie>();
    readonly List<Zombie> pullCandidates = new List<Zombie>(32);
    readonly List<Zombie> nullCleanup = new List<Zombie>();

    void Awake()
    {
        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    public void Launch(Vector3 fromPos, Vector3 targetPos, int damagePerTick)
    {
        transform.position = fromPos;
        destination = targetPos;
        contactDamage = Mathf.Max(0, damagePerTick);
        landed = false;
        edgePulledOnce.Clear();
        pullDistanceUsed.Clear();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        if (!landed)
        {
            float step = flightSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, destination, step);
            if (Vector2.Distance(transform.position, destination) < 0.05f)
            {
                transform.position = destination;
                landed = true;
                landStartTime = Time.time;
            }
        }
        else if (Time.time - landStartTime >= landStayDuration)
        {
            if (lingeringAuraOnEnd && lingerDamage > 0)
            {
                SpawnLingeringAura();
            }
            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        ApplyPull();
        ApplyContactDamage();
        CleanupEdgePulledSet();
        CleanupPullDistanceUsed();
    }

    void ApplyPull()
    {
        if (pullRadius <= 0f || pullSpeed <= 0f || maxPullTargets <= 0) return;

        Vector2 center = transform.position;
        float rSq = pullRadius * pullRadius;

        pullCandidates.Clear();
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie z = zombies[i];
            if (z == null || z.IsExcludedFromCombat || z.ResistsVortexPull()) continue;

            Vector2 p = z.transform.position;
            if ((p - center).sqrMagnitude > rSq) continue;
            pullCandidates.Add(z);
        }

        if (pullCandidates.Count == 0) return;

        pullCandidates.Sort((a, b) =>
        {
            float da = ((Vector2)a.transform.position - center).sqrMagnitude;
            float db = ((Vector2)b.transform.position - center).sqrMagnitude;
            return da.CompareTo(db);
        });

        int count = Mathf.Min(maxPullTargets, pullCandidates.Count);
        float edgeStartDist = pullRadius * (1f - EdgeBandFraction);
        float edgePullDistance = Mathf.Max(0.05f, pullSpeed * EdgePullDistanceMul);

        for (int i = 0; i < count; i++)
        {
            ApplyPullToZombie(pullCandidates[i], center, edgeStartDist, edgePullDistance);
        }
    }

    void ApplyPullToZombie(Zombie z, Vector2 center, float edgeStartDist, float edgePullDistance)
    {
        pullDistanceUsed.TryGetValue(z, out float used);
        if (used >= maxPullDistancePerEnemy) return;

        Vector2 p = z.transform.position;
        Vector2 toCenter = center - p;
        float dist = toCenter.magnitude;
        if (dist <= minClusterRadius || dist <= 0.0001f) return;

        float remaining = maxPullDistancePerEnemy - used;
        float maxStepTowardCenter = dist - minClusterRadius;

        if (dist >= edgeStartDist)
        {
            if (edgePulledOnce.Contains(z)) return;

            float step = Mathf.Min(edgePullDistance, maxStepTowardCenter, remaining);
            if (step <= 0.001f) return;

            z.ApplyVortexPull(p + toCenter.normalized * step);
            edgePulledOnce.Add(z);
            pullDistanceUsed[z] = used + step;
            return;
        }

        float frameStep = Mathf.Min(pullSpeed * Time.deltaTime, maxStepTowardCenter, remaining);
        if (frameStep <= 0.001f) return;

        z.ApplyVortexPull(p + toCenter.normalized * frameStep);
        pullDistanceUsed[z] = used + frameStep;
    }

    void CleanupEdgePulledSet()
    {
        if (edgePulledOnce.Count == 0) return;

        nullCleanup.Clear();
        foreach (Zombie z in edgePulledOnce)
        {
            if (z == null) nullCleanup.Add(z);
        }
        for (int i = 0; i < nullCleanup.Count; i++)
        {
            edgePulledOnce.Remove(nullCleanup[i]);
        }
    }

    void CleanupPullDistanceUsed()
    {
        if (pullDistanceUsed.Count == 0) return;

        nullCleanup.Clear();
        foreach (Zombie z in pullDistanceUsed.Keys)
        {
            if (z == null) nullCleanup.Add(z);
        }
        for (int i = 0; i < nullCleanup.Count; i++)
        {
            pullDistanceUsed.Remove(nullCleanup[i]);
        }
    }

    void SpawnLingeringAura()
    {
        GameObject linger = new GameObject("Unit28LingerAura");
        linger.transform.position = transform.position;
        Unit28LingerAura aura = linger.AddComponent<Unit28LingerAura>();
        aura.Init(pullRadius, lingerDuration, lingerDamage, sourceUnitNumber);
    }

    void ApplyContactDamage()
    {
        if (contactDamage <= 0) return;
        if (contactRadius <= 0f) return;

        float rSq = contactRadius * contactRadius;
        Vector2 c = transform.position;
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie z = zombies[i];
            if (z == null || z.IsExcludedFromCombat) continue;
            if (((Vector2)z.transform.position - c).sqrMagnitude > rSq) continue;

            if (!nextContactDamage.TryGetValue(z, out float nextT)) nextT = 0f;
            if (Time.time >= nextT)
            {
                z.TakeDamage(contactDamage, sourceUnitNumber);
                AttackHitEffectFire.Spawn(z.transform.position, Vector3.one * 2f);
                nextContactDamage[z] = Time.time + damageInterval;
            }
        }

        nullCleanup.Clear();
        foreach (var kv in nextContactDamage)
        {
            if (kv.Key == null) nullCleanup.Add(kv.Key);
        }
        for (int i = 0; i < nullCleanup.Count; i++)
        {
            nextContactDamage.Remove(nullCleanup[i]);
        }
    }
}
