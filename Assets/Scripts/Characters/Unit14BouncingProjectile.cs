using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 14 전용: 4초 동안 가장 가까운 2명 후보 중에서 튕겨 다니는 투사체
/// - 후보 풀: 현재 위치 기준 가장 가까운 좀비 2명
/// - 아직 맞추지 않은 좀비 우선, 모두 이미 맞췄으면 가장 가까운 좀비 재타격
/// - 좀비가 한 명도 없으면 소멸
/// </summary>
public class Unit14BouncingProjectile : MonoBehaviour
{
    public float speed = 18f;
    public int damage = 1;
    public int sourceUnitNumber;
    public float lifeTime = 4f;
    public float hitRadius = 0.6f;
    public float spinSpeed = 720f;
    public int extraRepeatBounces = 0;

    private float expireTime = 0f;
    private int repeatBouncesUsed = 0;
    private Zombie currentTarget;
    private Vector2 direction = Vector2.right;
    private readonly HashSet<Zombie> hitTargets = new HashSet<Zombie>();

    void Start()
    {
        Unit1ProjectileAfterimage.Attach(gameObject);
        expireTime = Time.time + lifeTime;
    }

    public void Launch(Zombie firstTarget)
    {
        currentTarget = firstTarget;
        UpdateDirectionToCurrentTarget();
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

        if (IsOutsideCameraBounds())
        {
            Destroy(gameObject);
            return;
        }

        if (currentTarget == null)
        {
            Zombie next = PickNextTarget();
            if (next == null)
            {
                Destroy(gameObject);
                return;
            }
            currentTarget = next;
        }

        UpdateDirectionToCurrentTarget();
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);

        if (currentTarget != null &&
            Vector2.Distance(transform.position, currentTarget.transform.position) < hitRadius)
        {
            HitCurrentTarget();
        }
    }

    void UpdateDirectionToCurrentTarget()
    {
        if (currentTarget == null) return;
        Vector2 toTarget = currentTarget.transform.position - transform.position;
        if (toTarget.sqrMagnitude > 0.001f)
        {
            direction = toTarget.normalized;
        }
    }

    void HitCurrentTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.TakeDamage(damage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(currentTarget.transform.position, Vector3.one * 2f);
            hitTargets.Add(currentTarget);
        }

        Zombie next = PickNextTarget();
        if (next == null)
        {
            Destroy(gameObject);
            return;
        }
        currentTarget = next;
        UpdateDirectionToCurrentTarget();
    }

    bool IsOutsideCameraBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 center = cam.transform.position;
        float minX = center.x - width * 0.5f;
        float maxX = center.x + width * 0.5f;
        float minY = center.y - height * 0.5f;
        float maxY = center.y + height * 0.5f;

        Vector3 pos = transform.position;
        return pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY;
    }

    Zombie PickNextTarget()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        if (zombies == null || zombies.Length == 0) return null;

        Zombie nearest1 = null;
        Zombie nearest2 = null;
        float dist1 = float.MaxValue;
        float dist2 = float.MaxValue;

        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float d = Vector2.Distance(transform.position, zombie.transform.position);
            if (d < dist1)
            {
                dist2 = dist1;
                nearest2 = nearest1;
                dist1 = d;
                nearest1 = zombie;
            }
            else if (d < dist2)
            {
                dist2 = d;
                nearest2 = zombie;
            }
        }

        if (nearest1 == null) return null;

        if (nearest1 != null && !hitTargets.Contains(nearest1)) return nearest1;
        if (nearest2 != null && !hitTargets.Contains(nearest2)) return nearest2;

        if (extraRepeatBounces > 0 && repeatBouncesUsed < extraRepeatBounces)
        {
            repeatBouncesUsed++;
            return nearest1;
        }

        return null;
    }
}
