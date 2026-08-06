using UnityEngine;

/// <summary>
/// 유닛 2 전용 불덩이 투사체
/// </summary>
public class Unit2FireballProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public int sourceUnitNumber;
    public bool useAreaDamage = true;
    public float areaSize = 3f;
    public float secondaryBlastMul = 0f;

    private Zombie target;
    private Vector2 lastDirection = Vector2.right;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames = System.Array.Empty<Sprite>();
    private float frameTimer = 0f;
    private int frameIndex = 0;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        frames = Resources.LoadAll<Sprite>("unit_002_attack");
        if (frames == null || frames.Length == 0)
        {
            frames = System.Array.Empty<Sprite>();
        }
        else
        {
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            spriteRenderer.sprite = frames[0];
            spriteRenderer.color = Color.white;
        }

        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        UpdateAnimation();

        if (target != null)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;
            lastDirection = direction;
        }

        if (lastDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        transform.position += (Vector3)(lastDirection * speed * Time.deltaTime);
        DestroyIfOutOfScreen();

        if (target != null && Vector2.Distance(transform.position, target.transform.position) < 0.5f)
        {
            HitTarget(target.transform.position);
        }
        else if (target == null)
        {
            CheckHitAnyZombie();
        }
    }

    public void SetTarget(Zombie targetZombie)
    {
        target = targetZombie;
    }

    void UpdateAnimation()
    {
        if (frames.Length == 0 || spriteRenderer == null)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer < 0.06f)
        {
            return;
        }

        frameTimer -= 0.06f;
        int maxIndex = Mathf.Min(3, frames.Length - 1);
        spriteRenderer.sprite = frames[frameIndex];
        frameIndex++;
        if (frameIndex > maxIndex)
        {
            frameIndex = 0;
        }
    }

    void HitTarget(Vector3 center)
    {
        if (useAreaDamage && areaSize > 0f)
        {
            ApplyAreaDamage(center);
        }
        else if (target != null)
        {
            target.TakeDamage(damage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(center, Vector3.one * 2f);
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
                if (useAreaDamage && areaSize > 0f)
                {
                    ApplyAreaDamage(zombie.transform.position);
                }
                else
                {
                    zombie.TakeDamage(damage, sourceUnitNumber);
                    AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
                }
                Destroy(gameObject);
                return;
            }
        }
    }

    void ApplyAreaDamage(Vector3 center)
    {
        AttackHitEffectFire.Spawn(center, Vector3.one * 3f);
        SpawnAreaIndicator(center);
        ApplyAreaDamageAt(center, damage, areaSize);
        if (secondaryBlastMul > 0f)
        {
            int secondaryDamage = Mathf.Max(1, Mathf.RoundToInt(damage * secondaryBlastMul));
            ApplyAreaDamageAt(center, secondaryDamage, areaSize * 0.65f);
        }
    }

    void ApplyAreaDamageAt(Vector3 center, int hitDamage, float size)
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        float halfSize = size * 0.5f;
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            Vector2 delta = (Vector2)zombie.transform.position - (Vector2)center;
            if (Mathf.Abs(delta.x) <= halfSize && Mathf.Abs(delta.y) <= halfSize)
            {
                zombie.TakeDamage(hitDamage, sourceUnitNumber);
            }
        }
    }

    void SpawnAreaIndicator(Vector3 center)
    {
        GameObject indicator = new GameObject("Unit2AreaIndicator");
        indicator.transform.position = center;

        SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
        Color color = new Color(1f, 0f, 0f, 0.25f);
        renderer.sprite = CreateSquareSprite(color);
        renderer.color = color;
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
}
