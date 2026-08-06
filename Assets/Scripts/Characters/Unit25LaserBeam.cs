using UnityEngine;

/// <summary>
/// 유닛 25 전용: 발사 시점의 시작 위치에서 타겟 방향으로 화면 경계까지 뻗는 긴 레이저.
/// 지정 시간 동안 유지되며, 주기적으로 빔 위의 모든 좀비에게 데미지를 준다.
/// </summary>
public class Unit25LaserBeam : MonoBehaviour
{
    public float duration = 3f;
    public float damageInterval = 0.5f;
    public int damage = 1;
    public int sourceUnitNumber;
    public float beamWidth = 0.6f;
    public Color outerColor = new Color(0.4f, 0.85f, 1f, 0.55f);
    public Color innerColor = new Color(0.95f, 1f, 1f, 1f);
    public bool afterglowOnEnd = false;
    public float afterglowDuration = 1.5f;
    public int afterglowDamage = 1;

    private Vector3 originPoint;
    private Vector3 endPoint;
    private float expireTime;
    private float nextDamageTime;
    private LineRenderer outerLine;
    private LineRenderer innerLine;

    public void Launch(Vector3 fromPos, Vector3 targetPos)
    {
        originPoint = fromPos;
        endPoint = ExtendToScreenEdge(fromPos, targetPos);
        expireTime = Time.time + duration;
        nextDamageTime = Time.time;
        transform.position = fromPos;

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
        SetupLine(innerLine, beamWidth * 0.4f, innerColor, 4);
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

    Vector3 ExtendToScreenEdge(Vector3 from, Vector3 to)
    {
        Camera cam = Camera.main;
        Vector2 dir = (Vector2)(to - from);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        if (cam == null)
        {
            return from + (Vector3)(dir * 20f);
        }

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Bounds bounds = new Bounds(cam.transform.position, new Vector3(width, height, 0f));

        float tMax = float.PositiveInfinity;
        if (Mathf.Abs(dir.x) > 0.0001f)
        {
            float tx = (dir.x > 0f ? bounds.max.x - from.x : bounds.min.x - from.x) / dir.x;
            if (tx > 0f) tMax = Mathf.Min(tMax, tx);
        }
        if (Mathf.Abs(dir.y) > 0.0001f)
        {
            float ty = (dir.y > 0f ? bounds.max.y - from.y : bounds.min.y - from.y) / dir.y;
            if (ty > 0f) tMax = Mathf.Min(tMax, ty);
        }
        if (float.IsPositiveInfinity(tMax) || tMax <= 0f)
        {
            return from + (Vector3)(dir * 20f);
        }
        return from + (Vector3)(dir * tMax);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        if (Time.time >= expireTime)
        {
            if (afterglowOnEnd && afterglowDamage > 0)
            {
                SpawnAfterglowField();
            }
            Destroy(gameObject);
            return;
        }

        UpdateBeamFlicker();

        if (Time.time >= nextDamageTime)
        {
            ApplyBeamDamage();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    void UpdateBeamFlicker()
    {
        if (outerLine == null || innerLine == null) return;

        float pulse = 0.85f + Mathf.PingPong(Time.time * 6f, 0.3f);
        float outerW = beamWidth * pulse;
        outerLine.startWidth = outerW;
        outerLine.endWidth = outerW;

        float innerW = beamWidth * 0.4f * pulse;
        innerLine.startWidth = innerW;
        innerLine.endWidth = innerW;
    }

    void SpawnAfterglowField()
    {
        GameObject linger = new GameObject("Unit25Afterglow");
        linger.transform.position = (originPoint + endPoint) * 0.5f;
        Unit25AfterglowField field = linger.AddComponent<Unit25AfterglowField>();
        field.Init(originPoint, endPoint, beamWidth, afterglowDuration, afterglowDamage, sourceUnitNumber);
    }

    void ApplyBeamDamage()
    {
        if (damage <= 0) return;

        float halfWidth = beamWidth * 0.5f;
        Vector2 a = originPoint;
        Vector2 b = endPoint;
        Vector2 ab = b - a;
        float abLenSq = ab.sqrMagnitude;
        if (abLenSq < 0.0001f) return;

        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie z = zombies[i];
            if (z == null || z.IsExcludedFromCombat) continue;
            Vector2 p = z.transform.position;
            Vector2 ap = p - a;
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / abLenSq);
            Vector2 closest = a + ab * t;
            if (Vector2.Distance(p, closest) <= halfWidth)
            {
                z.TakeDamage(damage, sourceUnitNumber);
            }
        }
    }
}
