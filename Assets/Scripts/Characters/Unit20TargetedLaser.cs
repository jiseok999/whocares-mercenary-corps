using UnityEngine;

/// <summary>
/// 유닛 20: 한 지점(번개 구슬)에서 지정 좀비 위치까지만 쏘는 직선 레이저(관통 없음, 대상 1기만).
/// </summary>
public class Unit20TargetedLaser : MonoBehaviour
{
    public float duration = 3f;
    public float damageInterval = 0.5f;
    public int damage = 1;
    public int sourceUnitNumber;
    public float beamWidth = 0.225f;
    public Color outerColor = new Color(1f, 0.95f, 0.35f, 0.55f);
    public Color innerColor = new Color(1f, 1f, 0.85f, 1f);
    public bool residualSplashOnEnd = false;
    public float residualSplashMul = 0.5f;
    public float residualSplashRadius = 1.2f;

    private Vector3 originPoint;
    private Vector3 endPoint;
    private Zombie onlyTarget;
    private float expireTime;
    private float nextDamageTime;
    private LineRenderer outerLine;
    private LineRenderer innerLine;

    public void Launch(Vector3 fromPos, Vector3 atTarget, Zombie target, int damageValue)
    {
        onlyTarget = target;
        originPoint = fromPos;
        endPoint = atTarget;
        damage = damageValue;
        expireTime = Time.time + duration;
        nextDamageTime = Time.time;
        transform.position = fromPos;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        BuildLineRenderers();
        ApplyBeamDamage();
        nextDamageTime = Time.time + damageInterval;
    }

    void BuildLineRenderers()
    {
        outerLine = gameObject.AddComponent<LineRenderer>();
        SetupLine(outerLine, beamWidth, outerColor, 3);

        GameObject coreObj = new GameObject("Core");
        coreObj.transform.SetParent(transform, false);
        innerLine = coreObj.AddComponent<LineRenderer>();
        SetupLine(innerLine, beamWidth * 0.38f, innerColor, 4);
    }

    void SetupLine(LineRenderer line, float width, Color color, int order)
    {
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, originPoint);
        line.SetPosition(1, endPoint);
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = order;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.time >= expireTime)
        {
            if (residualSplashOnEnd && damage > 0)
            {
                SpawnResidualSplash(endPoint);
            }
            Destroy(gameObject);
            return;
        }

        if (onlyTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateBeamGeometry();
        UpdateBeamFlicker();
        if (Time.time >= nextDamageTime)
        {
            ApplyBeamDamage();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    void UpdateBeamGeometry()
    {
        endPoint = onlyTarget.transform.position;
        if (outerLine != null)
        {
            outerLine.SetPosition(0, originPoint);
            outerLine.SetPosition(1, endPoint);
        }
        if (innerLine != null)
        {
            innerLine.SetPosition(0, originPoint);
            innerLine.SetPosition(1, endPoint);
        }
    }

    void UpdateBeamFlicker()
    {
        if (outerLine == null || innerLine == null) return;
        float pulse = 0.85f + Mathf.PingPong(Time.time * 7f, 0.28f);
        float ow = beamWidth * pulse;
        outerLine.startWidth = ow;
        outerLine.endWidth = ow;
        float iw = beamWidth * 0.38f * pulse;
        innerLine.startWidth = iw;
        innerLine.endWidth = iw;
    }

    void ApplyBeamDamage()
    {
        if (damage <= 0) return;
        if (onlyTarget == null) return;
        float half = beamWidth * 0.5f;
        Vector2 a = originPoint, b = endPoint;
        Vector2 p = onlyTarget.transform.position;
        Vector2 onSeg = ClosestPointOnSegment(p, a, b);
        if (Vector2.Distance(p, onSeg) > half) return;
        onlyTarget.TakeDamage(damage, sourceUnitNumber);
    }

    static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return a + ab * t;
    }

    void SpawnResidualSplash(Vector3 center)
    {
        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(damage * residualSplashMul));
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie z = zombies[i];
            if (z == null || z.IsExcludedFromCombat) continue;
            if (Vector2.Distance(center, z.transform.position) <= residualSplashRadius)
            {
                z.TakeDamage(splashDamage, sourceUnitNumber);
            }
        }
    }
}
