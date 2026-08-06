using UnityEngine;

/// <summary>
/// 위아래로 넓은 직선 총알 (오른쪽 방향)
/// </summary>
public class WideVerticalProjectile : MonoBehaviour
{
    public float speed = 1f;
    public int damage = 1;
    public int sourceUnitNumber;
    public float width = 0.3f;
    public float height = 2f;
    public float knockbackDistance = 0.2f;
    
    private System.Collections.Generic.HashSet<Zombie> hitZombies = new System.Collections.Generic.HashSet<Zombie>();
    private SpriteRenderer spriteRenderer;
    private Color tintColor = Color.white;
    private Sprite[] animationFrames = System.Array.Empty<Sprite>();
    private int frameIndex = 0;
    private float frameTimer = 0f;
    private const float FrameTime = 0.06f;
    
    void Update()
    {
        // 상점이 열려있으면 투사체 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        UpdateAnimation();
        
        // 오른쪽으로 이동
        transform.position += Vector3.right * speed * Time.deltaTime;
        
        // 충돌 체크
        CheckZombieHit();
        
        // 화면 밖으로 나가면 제거
        DestroyIfOutOfScreen();
    }

    public void SetAnimationFrames(Sprite[] frames, Color color)
    {
        tintColor = color;
        if (frames == null || frames.Length == 0)
        {
            animationFrames = System.Array.Empty<Sprite>();
            return;
        }

        animationFrames = (Sprite[])frames.Clone();
        System.Array.Sort(animationFrames, (a, b) => string.CompareOrdinal(a.name, b.name));
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = animationFrames[0];
            spriteRenderer.color = tintColor;
        }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    void UpdateAnimation()
    {
        if (spriteRenderer == null || animationFrames.Length == 0)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer < FrameTime)
        {
            return;
        }

        frameTimer -= FrameTime;
        int maxIndex = Mathf.Min(5, animationFrames.Length - 1);
        spriteRenderer.sprite = animationFrames[frameIndex];
        spriteRenderer.color = tintColor;
        frameIndex++;
        if (frameIndex > maxIndex)
        {
            frameIndex = 0;
        }
    }
    
    void CheckZombieHit()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        Vector3 pos = transform.position;
        
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            
            Vector3 zPos = zombie.transform.position;
            if (Mathf.Abs(zPos.x - pos.x) <= width * 0.5f &&
                Mathf.Abs(zPos.y - pos.y) <= height * 0.5f)
            {
                if (hitZombies.Contains(zombie))
                {
                    continue;
                }
                
                zombie.TakeDamage(damage, sourceUnitNumber);
                AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
                if (knockbackDistance > 0f)
                {
                    zombie.ApplyKnockback(knockbackDistance);
                }
                hitZombies.Add(zombie);
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


