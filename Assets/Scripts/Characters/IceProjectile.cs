using System;
using UnityEngine;

/// <summary>
/// 얼음 투사체: 명중 시 데미지 + 확률적으로 냉기 디버프
/// </summary>
public class IceProjectile : MonoBehaviour
{
    public float speed = 3f;
    public int damage = 1;
    public int sourceUnitNumber;
    public float slowChance = 0.5f;
    public float slowDuration = 2f;
    public float spriteAngleOffset = 0f;
    public Action<Zombie> onAppliedSlow;
    
    private Zombie target;
    private Vector2 lastDirection = Vector2.right;
    
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
        
        // 타겟이 있으면 타겟을 향해 이동, 없으면 마지막 방향으로 이동
        if (target != null)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;
            lastDirection = direction;
        }
        
        transform.position += (Vector3)(lastDirection * speed * Time.deltaTime);

        if (lastDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg + spriteAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        // 화면 밖으로 나가면 제거
        DestroyIfOutOfScreen();
        
        // 타겟에 도달했는지 확인 (히트 판정 여유)
        if (target != null && Vector2.Distance(transform.position, target.transform.position) < 0.5f)
        {
            HitTarget();
        }
        else if (target == null)
        {
            // 타겟이 없어도 다른 좀비에 맞으면 데미지
            CheckHitAnyZombie();
        }
    }
    
    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(Zombie targetZombie)
    {
        target = targetZombie;
    }
    
    /// <summary>
    /// 타겟에 명중
    /// </summary>
    void HitTarget()
    {
        if (target != null)
        {
            target.TakeDamage(damage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(target.transform.position, Vector3.one * 2f);

            // 50% 확률로 냉기 디버프 적용
            if (UnityEngine.Random.value <= slowChance)
            {
                target.ApplySlow(slowDuration);
                onAppliedSlow?.Invoke(target);
            }
        }
        
        Destroy(gameObject);
    }
    
    void CheckHitAnyZombie()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        float hitRadius = 0.5f;
        
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float distance = Vector2.Distance(transform.position, zombie.transform.position);
            if (distance < hitRadius)
            {
                zombie.TakeDamage(damage, sourceUnitNumber);
                AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);

                // 50% 확률로 냉기 디버프 적용
                if (UnityEngine.Random.value <= slowChance)
                {
                    zombie.ApplySlow(slowDuration);
                }
                
                Destroy(gameObject);
                return;
            }
        }
    }
    
    void DestroyIfOutOfScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        float camHalfWidth = cam.orthographicSize * (Screen.width / (float)Screen.height);
        float camHalfHeight = cam.orthographicSize;
        float left = cam.transform.position.x - camHalfWidth;
        float right = cam.transform.position.x + camHalfWidth;
        float bottom = cam.transform.position.y - camHalfHeight;
        float top = cam.transform.position.y + camHalfHeight;
        
        if (transform.position.x < left - 1f || transform.position.x > right + 1f ||
            transform.position.y < bottom - 1f || transform.position.y > top + 1f)
        {
            Destroy(gameObject);
        }
    }
}


