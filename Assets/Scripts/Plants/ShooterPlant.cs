using UnityEngine;

/// <summary>
/// 투사체를 발사하는 식물
/// </summary>
public class ShooterPlant : Plant
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    protected override void TryAttack()
    {
        Zombie target = FindNearestZombieInRange();
        
        if (target != null)
        {
            FireProjectile(target);
            lastAttackTime = Time.time;
        }
    }
    
    /// <summary>
    /// 투사체를 발사합니다
    /// </summary>
    void FireProjectile(Zombie target)
    {
        if (projectilePrefab == null) return;
        
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.SetTarget(target, damage);
        }
    }
}

