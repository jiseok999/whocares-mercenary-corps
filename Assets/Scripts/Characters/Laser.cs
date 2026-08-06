using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 관통 레이저 (모든 적에게 피해)
/// </summary>
public class Laser : MonoBehaviour
{
    public float duration = 0.3f; // 레이저 지속 시간
    public int damage = 999;
    public float range = 50f; // 레이저 범위
    public int sourceUnitNumber;
    
    private float startTime;
    private HashSet<Zombie> hitZombies = new HashSet<Zombie>(); // 이미 피해를 준 좀비들
    
    void Start()
    {
        startTime = Time.time;
        DamageAllZombiesInPath();
    }
    
    void Update()
    {
        // 상점이 열려있으면 레이저 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }
        
        if (Time.time - startTime >= duration)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 레이저 경로상의 모든 좀비에게 피해를 줍니다
    /// </summary>
    void DamageAllZombiesInPath()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        
        foreach (Zombie zombie in zombies)
        {
            if (zombie != null && !hitZombies.Contains(zombie) && !zombie.IsExcludedFromCombat)
            {
                // 레이저 발사 위치에서 오른쪽 방향으로 범위 내의 좀비들 체크
                Vector2 toZombie = (Vector2)(zombie.transform.position - transform.position);
                
                // 오른쪽 방향과의 각도가 작으면 (거의 일직선)
                if (Vector2.Dot(toZombie.normalized, Vector2.right) > 0.7f && toZombie.magnitude <= range)
                {
                    zombie.TakeDamage(damage, sourceUnitNumber);
                    hitZombies.Add(zombie);
                }
            }
        }
    }
}

