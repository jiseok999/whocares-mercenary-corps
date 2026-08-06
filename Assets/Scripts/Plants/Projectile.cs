using UnityEngine;

/// <summary>
/// 식물이 발사하는 투사체
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public int sourceUnitNumber;
    public bool useAreaDamage = false;
    public float areaSize = 0f;

    public string hitEffectControllerPath;
    public string hitEffectSpritePath;
    public Vector3 hitEffectScale = Vector3.one;
    public Color hitEffectColor = Color.white;
    public int hitEffectMaxFrame = -1;
    public float hitEffectFrameTime = 0.06f;
    
    private Zombie target;
    private Vector2 direction;
    private Vector2 lastDirection = Vector2.right;
    private SpriteRenderer spriteRenderer;
    private Sprite[] animFrames = System.Array.Empty<Sprite>();
    private int animFrameIndex = 0;
    private float animTimer = 0f;
    private const float AnimFrameTime = 0.06f;
    private int animMaxIndex = -1;
    private bool isUnit15Projectile = false;
    private bool hasSpawnedFan = false;
    private bool isFanProjectile = false;
    private Zombie fanIgnoreTarget;
    public float unit15SlowChance = 0.5f;
    public int unit15FanDirections = 4;
    
    void Update()
    {
        // 상점이 열려있으면 투사체 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        UpdateAnimation();
        
        // 타겟이 있으면 타겟을 향해 이동, 없으면 마지막 방향으로 이동
        if (target != null)
        {
            direction = (target.transform.position - transform.position).normalized;
            lastDirection = direction;
        }

        // unit_015_attack은 날아가는 방향으로 회전
        if (isUnit15Projectile)
        {
            if (lastDirection.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        
        transform.position += (Vector3)(lastDirection * speed * Time.deltaTime);
        
        // 화면 밖으로 나가면 제거
        DestroyIfOutOfScreen();
        
        // 타겟에 도달했는지 확인 (히트 판정 여유)
        if (target != null && Vector2.Distance(transform.position, target.transform.position) < 0.5f)
        {
            if (target.IsExcludedFromCombat)
            {
                target = null;
            }
            else
            {
                HitTarget();
                return;
            }
        }
        if (target == null)
        {
            CheckHitAnyZombie();
        }
    }
    
    /// <summary>
    /// 타겟을 설정합니다
    /// </summary>
    public void SetTarget(Zombie targetZombie, int damageAmount)
    {
        target = targetZombie;
        damage = damageAmount;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        TryDetectUnit15FromSpriteName();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Unit1ProjectileAfterimage.Attach(gameObject);
            return;
        }

        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    /// <summary>
    /// 15번 투사체(unit_015_attack). 스프라이트 시트 이름이 unit_6_attack_0 등으로 잡혀 있어 Awake 자동 판별만으로는 실패할 수 있음.
    /// </summary>
    public void ConfigureAsUnit15Projectile()
    {
        isUnit15Projectile = true;
        LoadUnit15AnimationFrames();
    }

    void TryDetectUnit15FromSpriteName()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        string spriteName = spriteRenderer.sprite.name;
        if (spriteName.Contains("015") || spriteName.StartsWith("unit_015"))
        {
            ConfigureAsUnit15Projectile();
        }
    }

    void LoadUnit15AnimationFrames()
    {
        animFrames = Resources.LoadAll<Sprite>("unit_015_attack");
        if (animFrames == null || animFrames.Length == 0)
        {
            Sprite single = Resources.Load<Sprite>("unit_015_attack");
            if (single != null)
            {
                animFrames = new Sprite[] { single };
            }
        }

        if (animFrames == null || animFrames.Length == 0)
        {
            return;
        }

        System.Array.Sort(animFrames, (a, b) => string.CompareOrdinal(a.name, b.name));
        animMaxIndex = Mathf.Min(3, animFrames.Length - 1);
        animFrameIndex = 0;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = animFrames[0];
        }
    }

    void UpdateAnimation()
    {
        if (spriteRenderer == null || animFrames.Length == 0 || animMaxIndex < 0)
        {
            return;
        }

        animTimer += Time.deltaTime;
        if (animTimer < AnimFrameTime)
        {
            return;
        }

        animTimer -= AnimFrameTime;
        spriteRenderer.sprite = animFrames[animFrameIndex];
        animFrameIndex++;
        if (animFrameIndex > animMaxIndex)
        {
            animFrameIndex = 0;
        }
    }

    public void SetHitEffect(string controllerPath, string spritePath, Vector3 scale, Color color)
    {
        hitEffectControllerPath = controllerPath;
        hitEffectSpritePath = spritePath;
        hitEffectScale = scale;
        hitEffectColor = color;
        hitEffectMaxFrame = -1;
    }

    public void SetHitEffectFrames(string spritePath, int maxFrame, float frameTime, Vector3 scale, Color color)
    {
        hitEffectControllerPath = null;
        hitEffectSpritePath = spritePath;
        hitEffectScale = scale;
        hitEffectColor = color;
        hitEffectMaxFrame = Mathf.Max(0, maxFrame);
        hitEffectFrameTime = Mathf.Max(0.01f, frameTime);
    }
    
    /// <summary>
    /// 타겟에 명중했을 때
    /// </summary>
    void HitTarget()
    {
        if (target != null)
        {
            if (useAreaDamage && areaSize > 0f)
            {
                ApplyAreaDamage(target.transform.position);
            }
            else
            {
                // TakeDamage를 호출하여 Die() 메서드가 제대로 호출되도록 함 (골드 추가 등을 위해)
                target.TakeDamage(damage, sourceUnitNumber);
            }
            ApplyUnit15Extras(target, target.transform.position);
            SpawnHitEffect(target.transform.position);
        }
        
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
                if (isFanProjectile && fanIgnoreTarget != null && zombie == fanIgnoreTarget)
                {
                    continue;
                }
                if (useAreaDamage && areaSize > 0f)
                {
                    ApplyAreaDamage(zombie.transform.position);
                }
                else
                {
                    zombie.TakeDamage(damage, sourceUnitNumber);
                }
                ApplyUnit15Extras(zombie, zombie.transform.position);
                SpawnHitEffect(zombie.transform.position);
                Destroy(gameObject);
                return;
            }
        }
    }

    void ApplyUnit15Extras(Zombie hitZombie, Vector3 hitPosition)
    {
        if (isUnit15Projectile && !isFanProjectile)
        {
            if (Random.value < unit15SlowChance)
            {
                hitZombie.ApplySlow(3f);
            }
            if (!hasSpawnedFan)
            {
                fanIgnoreTarget = hitZombie;
                SpawnUnit15FanProjectiles(hitPosition);
                hasSpawnedFan = true;
            }
        }
        else if (isUnit15Projectile && isFanProjectile)
        {
            if (fanIgnoreTarget != null && hitZombie == fanIgnoreTarget)
            {
                return;
            }
            if (Random.value < unit15SlowChance)
            {
                hitZombie.ApplySlow(3f);
            }
        }
    }

    void ApplyAreaDamage(Vector3 center)
    {
        SpawnAreaIndicator(center);
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        float halfSize = areaSize * 0.5f;

        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            Vector2 delta = (Vector2)zombie.transform.position - (Vector2)center;
            if (Mathf.Abs(delta.x) <= halfSize && Mathf.Abs(delta.y) <= halfSize)
            {
                zombie.TakeDamage(damage, sourceUnitNumber);
            }
        }
    }

    void SpawnAreaIndicator(Vector3 center)
    {
        GameObject indicator = new GameObject("AreaDamageIndicator");
        indicator.transform.position = center;

        SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite(new Color(1f, 0f, 0f, 0.25f));
        renderer.color = new Color(1f, 0f, 0f, 0.25f);
        renderer.sortingOrder = 2;

        indicator.transform.localScale = new Vector3(areaSize, areaSize, 1f);
        Destroy(indicator, 0.2f);
    }

    Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
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

    void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectMaxFrame >= 0 && !string.IsNullOrEmpty(hitEffectSpritePath))
        {
            AttackHitEffectFire.SpawnSpriteFrames(
                hitEffectSpritePath,
                position,
                hitEffectScale,
                hitEffectMaxFrame,
                hitEffectFrameTime,
                hitEffectColor);
            return;
        }

        if (string.IsNullOrEmpty(hitEffectControllerPath) && string.IsNullOrEmpty(hitEffectSpritePath))
        {
            return;
        }

        RuntimeAnimatorController controller = null;
        Sprite fallbackSprite = null;

        if (!string.IsNullOrEmpty(hitEffectControllerPath))
        {
            controller = Resources.Load<RuntimeAnimatorController>(hitEffectControllerPath);
        }
        Sprite[] sprites = null;
        if (controller == null && !string.IsNullOrEmpty(hitEffectSpritePath))
        {
            sprites = Resources.LoadAll<Sprite>(hitEffectSpritePath);
            if (sprites != null && sprites.Length > 0)
            {
                System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
                fallbackSprite = sprites[0];
            }
            else
            {
                fallbackSprite = Resources.Load<Sprite>(hitEffectSpritePath);
            }
        }
        if (controller == null && fallbackSprite == null)
        {
            return;
        }

        GameObject effectObj = new GameObject("HitEffect");
        effectObj.transform.position = position;
        effectObj.transform.localScale = hitEffectScale;

        SpriteRenderer renderer = effectObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 3;
        renderer.color = hitEffectColor;

        if (controller != null)
        {
            Animator animator = effectObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            if (fallbackSprite != null)
            {
                renderer.sprite = fallbackSprite;
            }
        }
        else
        {
            renderer.sprite = fallbackSprite;
        }

        Destroy(effectObj, 0.6f);
    }

    void SpawnUnit15FanProjectiles(Vector3 center)
    {
        float[] angles = unit15FanDirections >= 6
            ? new[] { 15f, 30f, 45f, -15f, -30f, -45f }
            : new[] { 20f, 40f, -20f, -40f };
        Vector3 fanScale = transform.localScale * 0.65f;
        int sortOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 2;

        foreach (float angle in angles)
        {
            GameObject projObj = new GameObject("Unit15FanProjectile");
            projObj.transform.position = center;
            projObj.transform.localScale = fanScale;

            SpriteRenderer sr = projObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortOrder;
            if (animFrames != null && animFrames.Length > 0)
            {
                sr.sprite = animFrames[0];
                sr.color = Color.white;
            }
            else if (spriteRenderer != null)
            {
                sr.sprite = spriteRenderer.sprite;
                sr.color = spriteRenderer.color;
            }

            Projectile proj = projObj.AddComponent<Projectile>();
            proj.speed = speed;
            proj.damage = damage;
            proj.useAreaDamage = false;
            proj.areaSize = 0f;
            proj.ConfigureAsUnit15Projectile();
            proj.SetFanMode(angle, fanIgnoreTarget);
            proj.SetHitEffectFrames(
                AttackHitEffectFire.DefaultResourcePath,
                4,
                0.06f,
                fanScale * 2f,
                Color.white);
        }
    }

    public void SetFanMode(float angleDeg, Zombie ignoreTarget)
    {
        isFanProjectile = true;
        hasSpawnedFan = true;
        fanIgnoreTarget = ignoreTarget;
        target = null;
        lastDirection = new Vector2(
            Mathf.Cos(angleDeg * Mathf.Deg2Rad),
            Mathf.Sin(angleDeg * Mathf.Deg2Rad));
        if (lastDirection.sqrMagnitude > 0.001f)
        {
            lastDirection.Normalize();
        }
        else
        {
            lastDirection = Vector2.right;
        }
    }
}

