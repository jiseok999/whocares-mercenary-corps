using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 24·26: FieldBoundary(필드 벽)에서 반사. 26은 AllyBarrier(아군 방어막)에서도 반사, 24는 방어막 통과.
/// 물리 콜라이더가 없거나 누락된 면은 전투 레인 경계로 보완 반사합니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Unit26RicochetProjectile : MonoBehaviour
{
    public float speed = 9f;
    [Tooltip("겹침 '진입' 1회당 가하는 피해량(업그레이드·특성 배율 없음)")]
    public int contactDamage = 1;
    public int sourceUnitNumber;
    [Tooltip("좀비 Overlap·데미지 반경")]
    public float hitRadius = 0.4f;
    [Tooltip("벽 CircleCast용 반경")]
    public float wallCastRadius = 0.12f;
    public float maxLifetime = 5f;
    public float speedRampPerSec = 0f;
    [Tooltip("아군 방어막(AllyBarrier)에서 반사할지. false면 통과합니다.")]
    public bool reflectsOnAllyBarrier = true;
    [Tooltip("이동 방향에 맞춰 스프라이트를 회전할지")]
    public bool rotateToFaceDirection = true;
    [Tooltip("한 틱에서 벽·모서리 연속 반사 횟(브레이크아웃식 공 튕기기)")]
    public int maxBouncesPerFrame = 12;
    [Tooltip("접촉 후 벽 밖으로 공을 밀어내는 거리(끼임·재관통 방지)")]
    public float wallSeparation = 0.06f;

    const float CorridorBoundsPadding = 0.08f;

    private Vector2 _direction;
    private float _spawnTime;
    private int _fieldMask;
    private int _allyBarrierMask;
    /// <summary>이번 Update 경로의 샘플들에서 hitRadius로 겹친 좀비(집합)</summary>
    private readonly HashSet<Zombie> _overlapThisFrame = new HashSet<Zombie>();
    /// <summary>직전 Update에서 겹쳤던 좀비—프레임마다 TakeDamage를 막고, 겹침 '진입'할 때만 1 대미지</summary>
    private readonly HashSet<Zombie> _overlapLastFrame = new HashSet<Zombie>();
    private SpriteRenderer _sprite;
    private Character _owner;

    public void BindOwner(Character owner)
    {
        _owner = owner;
        if (owner != null && sourceUnitNumber <= 0)
        {
            sourceUnitNumber = owner.unitNumber;
        }
    }

    public void Launch(Vector2 dir)
    {
        _sprite = GetComponent<SpriteRenderer>();
        _direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        _spawnTime = Time.time;

        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board != null)
        {
            board.EnsureFieldWalls();
        }

        _fieldMask = LayerMask.GetMask("FieldBoundary");
        _allyBarrierMask = LayerMask.GetMask("AllyBarrier");
        if (_fieldMask == 0)
        {
            Debug.LogWarning("Unit26Ricochet: FieldBoundary layer mask is 0. Corridor bounds fallback will be used.");
        }

        UpdateFacing();
        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    int BounceQueryMask => _fieldMask | (reflectsOnAllyBarrier ? _allyBarrierMask : 0);

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.time - _spawnTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (speedRampPerSec > 0f)
        {
            speed += speedRampPerSec * Time.deltaTime;
        }

        _overlapThisFrame.Clear();
        float remaining = speed * Time.deltaTime;
        Vector2 p = transform.position;
        int bounces = 0;
        int bounceMask = BounceQueryMask;
        while (remaining > 0.00001f && bounces < maxBouncesPerFrame)
        {
            Vector2 segmentStart = p;

            if (bounceMask != 0)
            {
                RaycastHit2D hit = Physics2D.CircleCast(
                    p,
                    wallCastRadius,
                    _direction,
                    remaining,
                    bounceMask
                );
                if (hit.collider != null)
                {
                    Vector2 n = GetWallNormalForBounce(_direction, hit);
                    Vector2 contactCenter = p + _direction * hit.distance;
                    Vector2 pAfterBounce = contactCenter + n * (wallSeparation + wallCastRadius * 0.02f);
                    ApplyZombiePierceAlongPath(segmentStart, contactCenter);
                    ApplyZombiePierceAlongPath(contactCenter, pAfterBounce);
                    p = pAfterBounce;
                    _direction = Vector2.Reflect(_direction, n);
                    if (_direction.sqrMagnitude < 1e-4f)
                    {
                        Vector2 perp = new Vector2(-n.y, n.x);
                        if (perp.sqrMagnitude < 1e-6f) perp = new Vector2(n.y, -n.x);
                        _direction = perp.normalized;
                    }
                    else
                    {
                        _direction = _direction.normalized;
                    }

                    remaining -= hit.distance;
                    if (remaining < 0f) remaining = 0f;
                    bounces++;
                    continue;
                }
            }

            if (TryClipAndReflectAtCorridorBounds(ref p, ref _direction, remaining, wallSeparation, out float consumed))
            {
                ApplyZombiePierceAlongPath(segmentStart, p);
                remaining -= consumed;
                if (remaining < 0f) remaining = 0f;
                bounces++;
                continue;
            }

            Vector2 nxt = p + _direction * remaining;
            ApplyZombiePierceAlongPath(segmentStart, nxt);
            p = nxt;
            break;
        }

        if (remaining > 0.00001f)
        {
            Vector2 segmentStart = p;
            Vector2 nxt = p + _direction * remaining;
            ApplyZombiePierceAlongPath(segmentStart, nxt);
            p = nxt;
        }

        transform.position = new Vector3(p.x, p.y, 0f);
        UpdateFacing();
        ApplyContactEntryDamage();
    }

    static bool TryClipAndReflectAtCorridorBounds(
        ref Vector2 p,
        ref Vector2 dir,
        float maxDist,
        float separation,
        out float consumed)
    {
        consumed = 0f;
        if (maxDist <= 0.00001f || dir.sqrMagnitude < 1e-6f)
        {
            return false;
        }

        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board == null || !board.TryGetCombatCorridorBounds(out float cx, out float cy, out float w, out float h))
        {
            return false;
        }

        float pad = CorridorBoundsPadding + separation;
        float minX = cx - w * 0.5f + pad;
        float maxX = cx + w * 0.5f - pad;
        float minY = cy - h * 0.5f + pad;
        float maxY = cy + h * 0.5f - pad;
        if (maxX <= minX || maxY <= minY)
        {
            return false;
        }

        Vector2 nd = dir.normalized;
        float tEnter = 0f;
        float tExit = maxDist;
        bool valid = true;

        if (Mathf.Abs(nd.x) > 1e-6f)
        {
            float tx1 = (minX - p.x) / nd.x;
            float tx2 = (maxX - p.x) / nd.x;
            if (tx1 > tx2)
            {
                float tmp = tx1;
                tx1 = tx2;
                tx2 = tmp;
            }

            tEnter = Mathf.Max(tEnter, tx1);
            tExit = Mathf.Min(tExit, tx2);
        }
        else if (p.x < minX || p.x > maxX)
        {
            valid = false;
        }

        if (Mathf.Abs(nd.y) > 1e-6f)
        {
            float ty1 = (minY - p.y) / nd.y;
            float ty2 = (maxY - p.y) / nd.y;
            if (ty1 > ty2)
            {
                float tmp = ty1;
                ty1 = ty2;
                ty2 = tmp;
            }

            tEnter = Mathf.Max(tEnter, ty1);
            tExit = Mathf.Min(tExit, ty2);
        }
        else if (p.y < minY || p.y > maxY)
        {
            valid = false;
        }

        if (!valid || tEnter > tExit || tExit < 0f)
        {
            return false;
        }

        if (tEnter <= 0.00001f)
        {
            return false;
        }

        if (tEnter > maxDist + 0.00001f)
        {
            return false;
        }

        Vector2 contact = p + nd * tEnter;
        Vector2 probe = contact + nd * 0.001f;
        Vector2 normal;
        if (probe.x <= minX + 0.0001f)
        {
            normal = Vector2.right;
        }
        else if (probe.x >= maxX - 0.0001f)
        {
            normal = Vector2.left;
        }
        else if (probe.y <= minY + 0.0001f)
        {
            normal = Vector2.up;
        }
        else if (probe.y >= maxY - 0.0001f)
        {
            normal = Vector2.down;
        }
        else
        {
            normal = -nd;
        }

        consumed = tEnter;
        p = contact + normal * separation;
        dir = Vector2.Reflect(nd, normal);
        if (dir.sqrMagnitude < 1e-6f)
        {
            dir = new Vector2(-nd.y, nd.x);
        }
        dir = dir.normalized;
        return true;
    }

    void OnDestroy()
    {
        if (_owner != null)
        {
            _owner.NotifyUnit26ProjectileEnded();
            _owner = null;
        }
    }

    /// <summary>
    /// 직전 프레임에 겹치지 않다가 이번에 겹친 경우에만 데미지 1(같은 접촉이 이어지는 프레임은 0회).
    /// </summary>
    void ApplyContactEntryDamage()
    {
        foreach (Zombie z in _overlapThisFrame)
        {
            if (z == null) continue;
            if (z.IsExcludedFromCombat) continue;
            if (_overlapLastFrame.Contains(z)) continue;
            z.TakeDamage(contactDamage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(z.transform.position, Vector3.one * 2f);
        }

        _overlapLastFrame.Clear();
        foreach (Zombie z in _overlapThisFrame)
        {
            if (z != null) _overlapLastFrame.Add(z);
        }
    }

    /// <summary>
    /// Unity가 주는 normal이 캐스트 방향과 잘못 맞는 경우(모서리 등)를 맞춤.
    /// 반사면 법선은 "벽이 공을 밀어 내는" 쪽(입사에 반대 방향의 성분이 있어야 함).
    /// </summary>
    static Vector2 GetWallNormalForBounce(Vector2 incoming, RaycastHit2D hit)
    {
        Vector2 n = hit.normal;
        if (n.sqrMagnitude < 1e-6f) n = -incoming.normalized;
        else n = n.normalized;
        if (Vector2.Dot(n, incoming) > 0f) n = -n;
        return n;
    }

    void UpdateFacing()
    {
        if (!rotateToFaceDirection) return;
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
        if (_direction.sqrMagnitude > 0.0001f)
        {
            float ang = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    void ApplyZombiePierceAlongPath(Vector2 from, Vector2 to)
    {
        float d = Vector2.Distance(from, to);
        if (d < 1e-5f)
        {
            CollectZombiesInOverlapAtPoint(from);
            return;
        }
        float step = Mathf.Max(hitRadius * 0.35f, 0.05f);
        int segments = Mathf.CeilToInt(d / step);
        segments = Mathf.Clamp(segments, 1, 64);
        for (int i = 0; i <= segments; i++)
        {
            float t = segments == 0 ? 0f : (float)i / segments;
            CollectZombiesInOverlapAtPoint(Vector2.Lerp(from, to, t));
        }
    }

    void CollectZombiesInOverlapAtPoint(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, hitRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            Zombie z = hits[i].GetComponentInParent<Zombie>();
            if (z == null || z.IsExcludedFromCombat) continue;
            _overlapThisFrame.Add(z);
        }
    }
}
