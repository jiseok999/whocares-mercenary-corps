using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 23 전용: 가장 가까운 적 방향으로 발사되어 모든 적을 관통하는 투사체.
/// 화면 밖으로 나가면 즉시 제거되며, 비행 경로를 따라 장판 세그먼트를 계속 스폰한다.
/// 장판들은 Unit23TrailManager가 그룹으로 수명 관리한다.
/// </summary>
public class Unit23PiercingProjectile : MonoBehaviour
{
    public float speed = 11f;
    public int damage = 1;
    public int sourceUnitNumber;
    public float hitRadius = 0.6f;
    public float spinSpeed = 0f;
    public float spriteAngleOffset = 0f;

    public float trailSpawnDistance = 0.35f;
    public float padSize = 0.9f;
    public float padDamageInterval = 1f;
    public int padDamage = 1;
    public Color padColor = new Color(1f, 0.2f, 0.2f, 0.3f);
    public int padSortingOrder = -5;
    public bool endExplosionOnDestroy = false;
    public float endExplosionRadius = 1.4f;
    public int endExplosionDamage = 1;

    private Vector2 direction = Vector2.right;
    private Vector3 lastTrailPosition;
    private bool hasLastTrailPosition = false;
    private Unit23TrailManager trailManager;
    private readonly HashSet<Zombie> hitTargets = new HashSet<Zombie>();

    void Awake()
    {
        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    public void Launch(Vector2 dir, Vector3 startPosition, Unit23TrailManager manager)
    {
        direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
        transform.position = startPosition;
        lastTrailPosition = startPosition;
        hasLastTrailPosition = true;
        trailManager = manager;
        SpawnTrailPadAt(startPosition);
        ApplySpriteRotation();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (spinSpeed > 0.01f)
        {
            transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        }
        else
        {
            ApplySpriteRotation();
        }

        if (IsOutsideCameraBounds())
        {
            NotifyManagerEnded();
            Destroy(gameObject);
            return;
        }

        TrySpawnTrailSegment();
        CheckZombieCollision();
    }

    void OnDestroy()
    {
        if (endExplosionOnDestroy && endExplosionDamage > 0)
        {
            Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
            Vector3 center = transform.position;
            for (int i = 0; i < zombies.Length; i++)
            {
                Zombie z = zombies[i];
                if (z == null || z.IsExcludedFromCombat) continue;
                if (Vector2.Distance(center, z.transform.position) <= endExplosionRadius)
                {
                    z.TakeDamage(endExplosionDamage, sourceUnitNumber);
                }
            }
        }
        NotifyManagerEnded();
    }

    void NotifyManagerEnded()
    {
        if (trailManager != null)
        {
            trailManager.NotifyProjectileEnded();
            trailManager = null;
        }
    }

    void ApplySpriteRotation()
    {
        if (spinSpeed > 0.01f) return;
        if (direction.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    bool IsOutsideCameraBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Bounds bounds = new Bounds(cam.transform.position, new Vector3(width, height, 0f));
        Vector3 p = transform.position;
        return p.x < bounds.min.x || p.x > bounds.max.x ||
               p.y < bounds.min.y || p.y > bounds.max.y;
    }

    void TrySpawnTrailSegment()
    {
        if (!hasLastTrailPosition)
        {
            lastTrailPosition = transform.position;
            hasLastTrailPosition = true;
            SpawnTrailPadAt(transform.position);
            return;
        }

        float step = Mathf.Max(0.05f, trailSpawnDistance);
        while (Vector2.Distance(lastTrailPosition, transform.position) >= step)
        {
            Vector3 toward = (transform.position - lastTrailPosition).normalized * step;
            lastTrailPosition += toward;
            SpawnTrailPadAt(lastTrailPosition);
        }
    }

    void SpawnTrailPadAt(Vector3 pos)
    {
        GameObject padObj = new GameObject("Unit23TrailPad");
        padObj.transform.position = pos;
        Unit23TrailPad pad = padObj.AddComponent<Unit23TrailPad>();
        pad.Configure(padSize, padDamage, padDamageInterval, padColor, padSortingOrder);
        pad.sourceUnitNumber = sourceUnitNumber;
        if (trailManager != null)
        {
            trailManager.RegisterPad(pad);
        }
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
                zombie.TakeDamage(damage, sourceUnitNumber);
                AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
            }
        }
    }
}
