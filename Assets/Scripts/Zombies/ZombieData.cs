using UnityEngine;

/// <summary>
/// 좀비 데이터 (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "New Zombie Data", menuName = "Game/Zombie Data")]
public class ZombieData : ScriptableObject
{
    [Header("Zombie Info")]
    [Tooltip("좀비 이름")]
    public string zombieName = "Zombie";
    
    [Header("Visual")]
    [Tooltip("리소스명 (스프라이트 경로, 없으면 기본 사각형 사용)")]
    public string resourceName = "";
    
    [Header("Stats")]
    [Tooltip("이동 속도")]
    public float moveSpeed = 1f;
    
    [Tooltip("체력")]
    public int health = 100;
    
    [Tooltip("최대 체력")]
    public int maxHealth = 100;
    
    [Header("Combat")]
    [Tooltip("공격력")]
    public int damage = 10;
    
    [Tooltip("공격 범위")]
    public float attackRange = 0.5f;
    
    [Tooltip("공격 쿨다운")]
    public float attackCooldown = 1f;
    
    void OnEnable()
    {
        // maxHealth가 0이면 health로 설정
        if (maxHealth == 0 && health > 0)
        {
            maxHealth = health;
        }
    }
}

