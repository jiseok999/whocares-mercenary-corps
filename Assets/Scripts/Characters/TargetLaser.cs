using UnityEngine;

/// <summary>
/// 특정 타겟을 지지는 레이저 (지속 피해)
/// </summary>
public class TargetLaser : MonoBehaviour
{
    public float duration = 3f;
    public float tickInterval = 1f;
    public int damagePerTick = 1;
    public int sourceUnitNumber;
    
    private Transform source;
    private Transform target;
    private float startTime;
    private float nextTickTime;
    private LineRenderer line;
    
    void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.sortingOrder = 3;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 0.4f, 0.4f);
        line.endColor = new Color(1f, 0.4f, 0.4f);
    }
    
    public void SetTarget(Transform sourceTransform, Transform targetTransform)
    {
        source = sourceTransform;
        target = targetTransform;
        startTime = Time.time;
        nextTickTime = Time.time;
    }
    
    void Update()
    {
        // 상점이 열려있으면 레이저 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }
        
        if (source == null || target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // 레이저 시각화
        line.SetPosition(0, source.position);
        line.SetPosition(1, target.position);
        
        // 지속시간 체크
        if (Time.time - startTime >= duration)
        {
            Destroy(gameObject);
            return;
        }
        
        // 주기적으로 데미지
        if (Time.time >= nextTickTime)
        {
            Zombie zombie = target.GetComponent<Zombie>();
            if (zombie != null)
            {
                zombie.TakeDamage(damagePerTick, sourceUnitNumber);
            }
            nextTickTime = Time.time + tickInterval;
        }
    }
}


