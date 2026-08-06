using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 12: 가장 가까운 3명 중 1명 방향으로(랜덤) 선이 그어지고, S→벽 구간에 최대 2명 끌기.
/// 그 외 적: ‘랜스 투사체(스프라이트)’ AABB가 좀비 AABB/콜라이더와 겹친 순간(스윕) 1회 데미지+넉백.
/// </summary>
public class Unit12PathDragController : MonoBehaviour
{
    [Tooltip("경로 폭(반) — 이 이내를 '경로 위'로 봄 (끌 2명 선정)")]
    public float pathHalfWidth = 0.42f;
    public float pullSpeed = 7.5f;
    public float maxRunTime = 10f;

    [Header("Lance (New_Weapon_13)")]
    [Tooltip("SPUM Resources 기준, 기본 날(-Y)이 아래일 때 d 방향에 맞춤(Apply에서 +180°로 반대 방향)")]
    public float lanceRotationOffsetDeg = 90f;
    public float lanceFlightWorldSpeed = 20f;
    [Tooltip("보이지 않는 벽(FieldBoundary) 맞은 뒤 랜스를 유지하는 시간(초)")]
    public float lanceStuckInWallDuration = 2f;
    public float lanceLocalScale = 1f;
    [Tooltip("Zombie Spawner 기본(1)보다 앞에 그리기")]
    public int lanceSortOrder = 50;
    [Tooltip("접촉 시(끌림이 아닌) 유닛 방향(비행 -D)으로 푸시(월드). 끌림 2명은 푸시 없음")]
    public float lanceBackKnockWorld = 0.4f;
    [Tooltip("랜스 접촉 1인당 1회")]
    public int lanceContactDamage = 1;
    public int sourceUnitNumber;

    Vector2 _S;
    Vector2 _W;
    Vector2 _D;
    float _lineLen;
    float _age;
    float _lanceTraveled;
    bool _lanceGone;
    bool _lanceStuckInWall;
    float _lanceStuckEndTime;
    Transform _lance;
    SpriteRenderer _lanceSpriteRenderer;
    const string LanceResource = "Addons/Ver300/0_Unit/0_Sprite/8_Weapons/1_Spear/New_Weapon_13";

    Bounds _lanceAabbEndOfLastFrame;
    bool _haveLanceAabbForSweep;
    readonly HashSet<Zombie> _lanceTouched = new HashSet<Zombie>();

    readonly List<Zombie> _pulled = new List<Zombie>(2);
    static readonly List<(Zombie z, float t)> s_sortBuf = new List<(Zombie, float)>(64);

    /// <param name="lineFrom">투사체(유닛) 출발</param>
    /// <param name="lineTo">FieldBoundary hit 또는 추정 끝점(보이지 않는 벽)</param>
    public void Begin(Vector2 lineFrom, Vector2 lineTo)
    {
        _S = lineFrom;
        _W = lineTo;
        Vector2 raw = _W - _S;
        _lineLen = raw.magnitude;
        if (_lineLen < 0.01f)
        {
            _D = Vector2.right;
            _W = _S + _D * 8f;
            _lineLen = 8f;
        }
        else
        {
            _D = raw / _lineLen;
        }
        _age = 0f;
        _lanceTouched.Clear();
        _haveLanceAabbForSweep = false;
        _pulled.Clear();
        RebuildPulled();
        for (int i = 0; i < _pulled.Count; i++)
        {
            if (_pulled[i] != null) _pulled[i].SetUnit12PathDragState(true);
        }
        _lanceTraveled = 0f;
        _lanceGone = false;
        _lanceStuckInWall = false;
        CreateLanceVisual();
    }

    void CreateLanceVisual()
    {
        if (_lance != null) return;
        Sprite sp = Resources.Load<Sprite>(LanceResource);
        if (sp == null)
        {
            var folder = Resources.LoadAll<Sprite>("Addons/Ver300/0_Unit/0_Sprite/8_Weapons/1_Spear");
            if (folder != null)
            {
                for (int i = 0; i < folder.Length; i++)
                {
                    if (folder[i] != null && folder[i].name == "New_Weapon_13") { sp = folder[i]; break; }
                }
            }
        }
        if (sp == null) return;
        var goL = new GameObject("LanceVfx");
        goL.transform.SetParent(transform, false);
        goL.transform.position = new Vector3(_S.x, _S.y, 0f);
        var sr = goL.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = Color.white;
        sr.sortingOrder = lanceSortOrder;
        goL.transform.localScale = Vector3.one * lanceLocalScale;
        _lance = goL.transform;
        _lanceSpriteRenderer = sr;
        ApplyLanceRotation();
    }

    void ApplyLanceRotation()
    {
        if (_lance == null) return;
        float z = Mathf.Atan2(_D.y, _D.x) * Mathf.Rad2Deg + lanceRotationOffsetDeg + 180f;
        _lance.rotation = Quaternion.Euler(0f, 0f, z);
    }

    void UpdateLance()
    {
        if (_lanceGone) return;
        if (_lance == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (_lanceStuckInWall)
        {
            if (Time.time >= _lanceStuckEndTime)
            {
                _lanceGone = true;
                if (_lance != null) Destroy(_lance.gameObject);
                _lance = null;
            }
            return;
        }

        _lanceTraveled += lanceFlightWorldSpeed * Time.deltaTime;
        bool reach = _lanceTraveled + 0.01f >= _lineLen;
        float t = Mathf.Min(_lanceTraveled, _lineLen - 0.01f);
        if (t < 0f) t = 0f;
        Vector2 nextLance = reach ? _W : new Vector2(_S.x + _D.x * t, _S.y + _D.y * t);
        _lance.position = new Vector3(nextLance.x, nextLance.y, 0f);
        ApplyLanceRotation();
        if (reach && !_lanceStuckInWall)
        {
            _lanceStuckInWall = true;
            _lanceStuckEndTime = Time.time + Mathf.Max(0.05f, lanceStuckInWallDuration);
        }
    }

    /// <summary>좀비 Update/MoveLeft가 먼저 돈 뒤(같은 프레임) 호출—넉백 직후에 보행이 baseX에 더해지며 '앞으로' 튀는 느낌 방지</summary>
    void ProcessLanceSpriteOverlapHits()
    {
        if (_lanceSpriteRenderer == null) return;
        int dmg = Mathf.Max(0, lanceContactDamage);
        Bounds bNow = _lanceSpriteRenderer.bounds;
        Bounds sweepAabb = bNow;
        if (_haveLanceAabbForSweep) sweepAabb.Encapsulate(_lanceAabbEndOfLastFrame);
        _lanceAabbEndOfLastFrame = bNow;
        _haveLanceAabbForSweep = true;

        var all = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int k = 0; k < all.Length; k++)
        {
            Zombie z = all[k];
            if (z == null || z.IsExcludedFromCombat) continue;
            if (_lanceTouched.Contains(z)) continue;
            if (!TryGetZombieVisualOrColliderBounds(z, out Bounds zb)) continue;
            if (!sweepAabb.Intersects(zb)) continue;
            _lanceTouched.Add(z);
            if (dmg > 0) z.TakeDamage(dmg, sourceUnitNumber);
            if (!pulledSetContains(z) && lanceBackKnockWorld > 0f) z.ApplyNudge2D(-_D * lanceBackKnockWorld);
        }
    }

    static bool TryGetZombieVisualOrColliderBounds(Zombie z, out Bounds b)
    {
        b = new Bounds();
        if (z == null) return false;
        var srZ = z.GetComponentInChildren<SpriteRenderer>(true);
        if (srZ != null && srZ.bounds.size.sqrMagnitude > 0.0001f)
        {
            b = srZ.bounds;
            return true;
        }
        var col = z.GetComponentInChildren<Collider2D>(true);
        if (col != null)
        {
            b = col.bounds;
            return true;
        }
        b = new Bounds(z.transform.position, new Vector3(0.4f, 0.5f, 0.1f));
        return true;
    }

    void Update()
    {
        UpdateLance();
    }

    public static void ClearAll()
    {
        var arr = FindObjectsByType<Unit12PathDragController>(FindObjectsSortMode.None);
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null) Destroy(arr[i].gameObject);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < _pulled.Count; i++)
        {
            if (_pulled[i] != null) _pulled[i].SetUnit12PathDragState(false);
        }
    }

    void RebuildPulled()
    {
        s_sortBuf.Clear();
        float wSqr = pathHalfWidth * pathHalfWidth;
        Vector2 a = _S, b = _W;
        Zombie[] all = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Zombie z = all[i];
            if (z == null || z.IsExcludedFromCombat) continue;
            float sD = SqrPointSegment2D((Vector2)z.transform.position, a, b, out float t01);
            if (sD > wSqr) continue;
            if (t01 < 0.01f || t01 > 0.99f) continue;
            s_sortBuf.Add((z, t01));
        }
        s_sortBuf.Sort((p, q) => p.t.CompareTo(q.t));
        _pulled.Clear();
        for (int i = 0; i < s_sortBuf.Count && _pulled.Count < 2; i++)
        {
            Zombie z = s_sortBuf[i].z;
            if (z != null) _pulled.Add(z);
        }
    }

    static float SqrPointSegment2D(Vector2 p, Vector2 a, Vector2 b, out float t01)
    {
        Vector2 ab = b - a;
        float l2 = ab.sqrMagnitude;
        if (l2 < 1e-8f)
        {
            t01 = 0f;
            return (p - a).sqrMagnitude;
        }
        t01 = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
        Vector2 proj = a + ab * t01;
        return (p - proj).sqrMagnitude;
    }

    void LateUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (_lance != null && !_lanceGone && _lanceSpriteRenderer != null)
        {
            ProcessLanceSpriteOverlapHits();
        }
        _age += Time.deltaTime;
        if (_age > maxRunTime && (_lance == null || _lanceGone)) { Destroy(gameObject); return; }
        for (int i = _pulled.Count - 1; i >= 0; i--)
        {
            if (_pulled[i] == null) { _pulled.RemoveAt(i); continue; }
        }
        // 끌 좀비가 없어도 랜스가 날아가/벽에 꽂혀 있는 동안엔 부모를 지우지 않는다(자식 랜스가 같이 Destroy 됨)
        if (_pulled.Count == 0)
        {
            if (_lance != null && !_lanceGone) return;
            if (_age > 0.45f) Destroy(gameObject);
            return;
        }

        ApplyPullStep();
    }

    void ApplyPullStep()
    {
        for (int i = _pulled.Count - 1; i >= 0; i--)
        {
            Zombie z = _pulled[i];
            if (z == null) { _pulled.RemoveAt(i); continue; }
            var p = (Vector2)z.transform.position;
            float along = Vector2.Dot(p - _S, _D);
            if (along >= _lineLen - 0.12f)
            {
                z.SetUnit12PathDragState(false);
                z.ApplyVortexPull(_S + _D * Mathf.Max(0f, _lineLen - 0.08f));
                _pulled.RemoveAt(i);
                continue;
            }
            var target = p + _D * (pullSpeed * Time.deltaTime);
            if (Vector2.Dot(target - _S, _D) > _lineLen - 0.1f)
            {
                target = _S + _D * (_lineLen - 0.1f);
            }
            z.ApplyVortexPull(target);
        }
    }

    bool pulledSetContains(Zombie z)
    {
        for (int i = 0; i < _pulled.Count; i++) if (_pulled[i] == z) return true;
        return false;
    }
}
