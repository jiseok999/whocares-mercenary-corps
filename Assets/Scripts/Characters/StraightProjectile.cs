using UnityEngine;

/// <summary>
/// 일직선으로 이동하는 투사체 (오른쪽 방향)
/// </summary>
public class StraightProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 3;
    public int sourceUnitNumber;
    public Vector2 direction = Vector2.right; // 오른쪽 방향

    /// <summary>히트 시 AttackHitEffectFire 스케일</summary>
    public Vector3 hitEffectScale = Vector3.one * 2f;

    void Awake()
    {
        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    void Update()
    {
        // 상점이 열려있으면 투사체 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        // 좀비와 충돌 체크 (거리 기반)
        CheckZombieCollision();

        // 방향으로 이동
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // 화면 밖으로 나가면 제거
        Camera cam = Camera.main;
        if (cam != null)
        {
            float cameraRight = cam.transform.position.x + (cam.orthographicSize * (Screen.width / (float)Screen.height));
            if (transform.position.x > cameraRight + 5f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 좀비와의 충돌을 체크합니다 (거리 기반)
    /// </summary>
    void CheckZombieCollision()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        float hitRadius = Mathf.Max(transform.localScale.x * 0.5f, 1.0f); // 충돌 반경 (충분히 크게)

        foreach (Zombie zombie in zombies)
        {
            if (zombie != null && !zombie.IsExcludedFromCombat)
            {
                float distance = Vector2.Distance(transform.position, zombie.transform.position);
                if (distance < hitRadius)
                {
                    zombie.TakeDamage(damage, sourceUnitNumber);
                    AttackHitEffectFire.Spawn(zombie.transform.position, hitEffectScale);
                    // 투사체 제거 (관통하지 않음)
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
