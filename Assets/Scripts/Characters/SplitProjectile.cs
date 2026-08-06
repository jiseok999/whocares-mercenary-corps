using UnityEngine;

/// <summary>
/// 타겟에 맞으면 4갈래로 분열되는 투사체
/// </summary>
public class SplitProjectile : MonoBehaviour
{
    public float speed = 3f;
    public int damage = 2;
    public int sourceUnitNumber;
    public int splitDamage = 2;
    public Character ownerCharacter;
    public float neighborSplashFraction = 0f;

    private Zombie target;
    private Vector2 lastDirection = Vector2.right;
    private Zombie excludedTarget;
    
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
        
        // 타겟에 도달했는지 확인 (히트 판정 여유)
        if (target != null && Vector2.Distance(transform.position, target.transform.position) < 0.5f)
        {
            HitTarget(target);
        }
        else if (target == null)
        {
            // 타겟이 없어도 다른 좀비에 맞으면 분열
            CheckHitAnyZombie();
        }
        
        // 화면 밖으로 나가면 제거
        DestroyIfOutOfScreen();
    }
    
    public void SetTarget(Zombie targetZombie)
    {
        target = targetZombie;
    }
    
    void HitTarget(Zombie hitZombie)
    {
        if (hitZombie != null)
        {
            hitZombie.TakeDamage(damage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(hitZombie.transform.position, Vector3.one * 2f);
            excludedTarget = hitZombie;
            if (ownerCharacter != null && neighborSplashFraction > 0f)
            {
                ownerCharacter.ApplyUnit10NeighborSplash(hitZombie, damage);
            }
        }

        SplitIntoFour(excludedTarget);
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
                excludedTarget = zombie;
                SplitIntoFour(excludedTarget);
                Destroy(gameObject);
                return;
            }
        }
    }
    
    void SplitIntoFour(Zombie excluded)
    {
        SpawnSplitProjectile(new Vector2(1f, 1f).normalized, excluded);
        SpawnSplitProjectile(new Vector2(1f, -1f).normalized, excluded);
        SpawnSplitProjectile(new Vector2(-1f, 1f).normalized, excluded);
        SpawnSplitProjectile(new Vector2(-1f, -1f).normalized, excluded);
    }
    
    void SpawnSplitProjectile(Vector2 direction, Zombie excluded)
    {
        GameObject projObj = new GameObject("SplitChildProjectile");
        projObj.transform.position = transform.position;
        
        SpriteRenderer spriteRenderer = projObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;
        UnitProjectileVisuals.TryApply(spriteRenderer, 10);

        projObj.transform.localScale = Vector3.one * (UnitProjectileVisuals.Unit10ProjectileScale * UnitProjectileVisuals.Unit10ChildProjectileScaleFactor);
        
        SplitChildProjectile proj = projObj.AddComponent<SplitChildProjectile>();
        proj.speed = speed;
        proj.direction = direction;
        proj.damage = splitDamage;
        proj.sourceUnitNumber = sourceUnitNumber;
        proj.excludedTarget = excluded;
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
    
    Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}


