using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 13 전용: 모든 적을 관통하며 카메라 밖으로 나가기 직전 돌아오는 거대 표창
/// </summary>
public class Unit13ShurikenProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public int sourceUnitNumber;
    public float lifeTime = 15f;
    public float edgeMargin = 0.5f;
    public float spinSpeed = 1080f;
    public float hitRadius = 0.65f;
    public float pierceDamageMultiplier = 0.30f;
    public float returnArriveDistance = 0.6f;
    public int maxReturnLegs = 1;

    private Vector2 direction = Vector2.right;
    private Transform owner;
    private Vector3 fallbackReturnPos;
    private bool returning = false;
    private int completedReturnLegs = 0;
    private float expireTime = 0f;
    private readonly HashSet<Zombie> hitTargets = new HashSet<Zombie>();

    void Start()
    {
        Unit1ProjectileAfterimage.Attach(gameObject);
        expireTime = Time.time + lifeTime;
    }

    public void Launch(Transform ownerTransform, Vector2 dir)
    {
        owner = ownerTransform;
        fallbackReturnPos = ownerTransform != null ? ownerTransform.position : transform.position;
        direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
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

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);

        if (!returning)
        {
            if (IsNearCameraEdge())
            {
                returning = true;
                hitTargets.Clear();
                UpdateReturnDirection();
            }
        }
        else
        {
            Vector3 targetPos = GetReturnTargetPosition();
            if (Vector2.Distance(transform.position, targetPos) <= returnArriveDistance)
            {
                completedReturnLegs++;
                if (completedReturnLegs >= maxReturnLegs)
                {
                    Destroy(gameObject);
                    return;
                }
                returning = false;
                hitTargets.Clear();
                direction = -direction;
                return;
            }
            UpdateReturnDirection();
        }

        CheckZombieCollision();
    }

    Vector3 GetReturnTargetPosition()
    {
        return owner != null ? owner.position : fallbackReturnPos;
    }

    void UpdateReturnDirection()
    {
        Vector3 target = GetReturnTargetPosition();
        Vector2 toTarget = target - transform.position;
        if (toTarget.sqrMagnitude > 0.001f)
        {
            direction = toTarget.normalized;
        }
    }

    bool IsNearCameraEdge()
    {
        if (!TryGetCameraBounds(out Bounds bounds)) return false;
        Vector3 pos = transform.position;
        return pos.x <= bounds.min.x + edgeMargin ||
               pos.x >= bounds.max.x - edgeMargin ||
               pos.y <= bounds.min.y + edgeMargin ||
               pos.y >= bounds.max.y - edgeMargin;
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
        bounds = new Bounds(cam.transform.position, new Vector3(width, height, 0f));
        return true;
    }

    void CheckZombieCollision()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        float radius = Mathf.Max(hitRadius, transform.localScale.x * 0.5f);

        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || hitTargets.Contains(zombie) || zombie.IsExcludedFromCombat) continue;
            float distance = Vector2.Distance(transform.position, zombie.transform.position);
            if (distance < radius)
            {
                hitTargets.Add(zombie);
                int hitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * pierceDamageMultiplier));
                zombie.TakeDamage(hitDamage, sourceUnitNumber);
                AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
            }
        }
    }
}
