using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 식물의 기본 클래스
/// </summary>
public class Plant : MonoBehaviour
{
    [Header("Plant Stats")]
    public int health = 100;
    public int maxHealth = 100;
    public int cost = 50;
    
    [Header("Combat")]
    public int damage = 10;
    public float attackRange = 5f;
    public float attackCooldown = 1f;
    
    public GridCell currentCell;
    protected float lastAttackTime = 0f;
    protected bool isDead = false;

    static readonly List<Zombie> s_zombieSearchScratch = new List<Zombie>(256);
    
    protected virtual void Start()
    {
        health = maxHealth;
    }
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            TryAttack();
        }
    }
    
    /// <summary>
    /// 공격을 시도합니다
    /// </summary>
    protected virtual void TryAttack()
    {
        // 자식 클래스에서 구현
    }
    
    /// <summary>
    /// 데미지를 받습니다
    /// </summary>
    public virtual void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 식물이 죽습니다
    /// </summary>
    protected virtual void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        if (currentCell != null)
        {
            currentCell.RemovePlant();
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 범위 내의 가장 가까운 좀비를 찾습니다
    /// </summary>
    protected Zombie FindNearestZombieInRange()
    {
        Zombie nearestZombie = null;
        float nearestDistance = attackRange;

        Zombie.CopyLivingZombiesTo(s_zombieSearchScratch);

        foreach (Zombie zombie in s_zombieSearchScratch)
        {
            // 같은 행에 있는 좀비만 공격
            if (currentCell != null && zombie.currentRow == currentCell.row)
            {
                float distance = Vector2.Distance(transform.position, zombie.transform.position);
                
                // 좀비가 앞쪽(오른쪽)에 있는지 확인
                if (zombie.transform.position.x > transform.position.x && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestZombie = zombie;
                }
            }
        }

        return nearestZombie;
    }
}
