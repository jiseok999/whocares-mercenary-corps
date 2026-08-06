using UnityEngine;

/// <summary>
/// 태양 아이템 (클릭하면 태양 포인트를 획득)
/// </summary>
public class Sun : MonoBehaviour
{
    [Header("Sun Settings")]
    public int sunValue = 25;
    public float lifetime = 10f;
    public float fallSpeed = 2f;
    
    private float startTime;
    private bool isCollected = false;
    
    void Start()
    {
        startTime = Time.time;
    }
    
    void Update()
    {
        // 일정 시간 후 자동 사라짐
        if (Time.time >= startTime + lifetime && !isCollected)
        {
            Destroy(gameObject);
        }
        
        // 떨어지는 효과 (옵션)
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }
    
    void OnMouseDown()
    {
        if (!isCollected)
        {
            Collect();
        }
    }
    
    /// <summary>
    /// 태양을 수집합니다
    /// </summary>
    void Collect()
    {
        isCollected = true;
        GameManager.Instance.AddSunPoints(sunValue);
        Destroy(gameObject);
    }
}

