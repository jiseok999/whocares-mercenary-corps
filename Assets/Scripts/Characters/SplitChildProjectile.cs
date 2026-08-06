using UnityEngine;

/// <summary>
/// 분열된 대각선 투사체 (특정 대상은 관통)
/// </summary>
public class SplitChildProjectile : MonoBehaviour
{
    public float speed = 3f;
    public int damage = 2;
    public int sourceUnitNumber;
    public Vector2 direction = Vector2.right;
    public Zombie excludedTarget;
    
    void Update()
    {
        // 상점이 열려있으면 투사체 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }
        
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        
        CheckZombieHit();
        DestroyIfOutOfScreen();
    }
    
    void CheckZombieHit()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        float hitRadius = Mathf.Max(transform.localScale.x * 0.5f, 0.5f);
        
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            if (zombie == excludedTarget) continue;
            
            float distance = Vector2.Distance(transform.position, zombie.transform.position);
            if (distance < hitRadius)
            {
                zombie.TakeDamage(damage, sourceUnitNumber);
                AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
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


