using UnityEngine;

/// <summary>
/// 보드 오른쪽 방어막
/// </summary>
public class Barrier : MonoBehaviour
{
    public static Barrier Instance { get; private set; }
    
    public int maxHealth = 100;
    public int health = 100;

    [Header("Hit Effects")]
    public Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float hitFlashDuration = 0.08f;
    public float hitShakeDuration = 0.12f;
    public float hitShakeAmount = 0.05f;
    
    private TextMesh hpText;
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Vector3 originalLocalPosition;
    private Coroutine hitEffectRoutine;
    
    void Awake()
    {
        Instance = this;
        SetupAllyBounceCollider();
    }

    /// <summary>유닛 26 투시체(적 1회 히트 이후) 반사용 — Default와 분리해 좀비와 구분</summary>
    void SetupAllyBounceCollider()
    {
        int layer = LayerMask.NameToLayer("AllyBarrier");
        if (layer >= 0) gameObject.layer = layer;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = false;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 world = sr.bounds.size;
            Vector3 ls = transform.lossyScale;
            float sx = Mathf.Max(1e-5f, Mathf.Abs(ls.x));
            float sy = Mathf.Max(1e-5f, Mathf.Abs(ls.y));
            box.size = new Vector2(world.x / sx, world.y / sy);
        }
        else
        {
            box.size = new Vector2(0.3f, 2.5f);
        }
    }
    
    void Start()
    {
        health = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        originalLocalPosition = transform.localPosition;
        CreateHpText();
        UpdateHpText();
    }
    
    public bool IsAlive()
    {
        return health > 0;
    }

    /// <summary>좀비가 벽을 넘지 않도록 할 때 사용하는 월드 AABB (콜라이더 우선)</summary>
    public Bounds GetWorldBounds()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null && box.enabled)
        {
            return box.bounds;
        }
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds;
        }
        return new Bounds(transform.position, new Vector3(0.3f, 2.5f, 0.1f));
    }
    
    public void TakeDamage(int amount)
    {
        // 벽이 받는 피해는 공격 주체와 무관하게 타격당 1로 고정
        health -= Mathf.Clamp(amount, 0, 1);
        UpdateHpText();
        PlayHitEffect();
        
        if (health <= 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBarrierDestroyed();
            }
            Destroy(gameObject);
        }
    }

    /// <summary>벽 체력 회복(최대치 초과 불가). 기사단 벽 수호용.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || health <= 0) return;
        health = Mathf.Min(maxHealth, health + amount);
        UpdateHpText();
    }
    
    void CreateHpText()
    {
        GameObject textObj = new GameObject("BarrierHPText");
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, 0f);
        
        Vector3 parentScale = transform.localScale;
        if (!Mathf.Approximately(parentScale.x, 0f) && !Mathf.Approximately(parentScale.y, 0f))
        {
            textObj.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);
        }
        
        hpText = textObj.AddComponent<TextMesh>();
        hpText.text = "100";
        hpText.fontSize = 24;
        hpText.characterSize = 0.12f;
        hpText.color = Color.white;
        hpText.alignment = TextAlignment.Center;
        hpText.anchor = TextAnchor.MiddleCenter;
        
        MeshRenderer renderer = textObj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10;
        }
    }
    
    void UpdateHpText()
    {
        if (hpText != null)
        {
            hpText.text = health.ToString();
        }
    }

    void PlayHitEffect()
    {
        if (hitEffectRoutine != null)
        {
            StopCoroutine(hitEffectRoutine);
        }
        hitEffectRoutine = StartCoroutine(HitEffectRoutine());
    }

    System.Collections.IEnumerator HitEffectRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitFlashColor;
        }

        float elapsed = 0f;
        while (elapsed < hitShakeDuration)
        {
            float offset = Random.Range(-hitShakeAmount, hitShakeAmount);
            transform.localPosition = originalLocalPosition + new Vector3(offset, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPosition;

        if (spriteRenderer != null)
        {
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = originalColor;
        }
        hitEffectRoutine = null;
    }
}


