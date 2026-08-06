using System.Collections;
using UnityEngine;

/// <summary>
/// 조합 특수 공격용 — 지정 좀비를 추적하는 유도탄 (도깨비탄 등).
/// </summary>
public class TraitHomingProjectile : MonoBehaviour
{
    public Zombie target;
    public int damage = 1;
    public int sourceUnitNumber;
    public float speed = 14f;
    public float hitRadius = 0.42f;
    public float maxLifetime = 4f;
    public Color tint = Color.white;

    float spawnTime;
    float pulsePhase;
    SpriteRenderer coreRenderer;
    SpriteRenderer glowRenderer;
    Transform visualRoot;

    public static TraitHomingProjectile Spawn(Vector3 from, Zombie targetZombie, int damageValue, Color color, float scale = 0.28f, int sourceUnitNumber = 0)
    {
        if (targetZombie == null) return null;

        GameObject go = new GameObject("TraitHomingProjectile");
        go.transform.position = from;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one * scale;

        Sprite coin = GetCoinSprite();
        float worldScale = coin != null ? GoldSpriteVisual.GetHudMatchedWorldScale(coin) * 0.42f : scale;

        SpriteRenderer glow = visual.AddComponent<SpriteRenderer>();
        glow.sprite = coin ?? GetFallbackDiscSprite();
        glow.color = new Color(color.r, color.g, color.b, 0.38f);
        glow.sortingOrder = 4;
        glow.transform.localScale = Vector3.one * 1.55f;

        GameObject coreObj = new GameObject("Core");
        coreObj.transform.SetParent(visual.transform, false);
        SpriteRenderer core = coreObj.AddComponent<SpriteRenderer>();
        core.sprite = coin ?? GetFallbackDiscSprite();
        core.color = Color.Lerp(color, Color.white, 0.25f);
        core.sortingOrder = 6;

        TraitHomingProjectile proj = go.AddComponent<TraitHomingProjectile>();
        proj.target = targetZombie;
        proj.damage = damageValue;
        proj.sourceUnitNumber = sourceUnitNumber;
        proj.tint = color;
        proj.coreRenderer = core;
        proj.glowRenderer = glow;
        proj.visualRoot = visual.transform;
        proj.spawnTime = Time.time;
        proj.pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        proj.transform.localScale = Vector3.one * worldScale;

        Unit1ProjectileAfterimage afterimage = Unit1ProjectileAfterimage.Attach(visual);
        if (afterimage != null)
        {
            afterimage.spawnDistance = 0.1f;
            afterimage.ghostLifetime = 0.16f;
            afterimage.startAlpha = 0.34f;
        }

        SpawnLaunchBurst(from, color);
        return proj;
    }

    static void SpawnLaunchBurst(Vector3 at, Color color)
    {
        for (int i = 0; i < 5; i++)
        {
            float angle = i * (360f / 5f) * Mathf.Deg2Rad;
            Vector3 pos = at + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.12f;
            GameObject spark = new GameObject("GoblinLaunchSpark");
            spark.transform.position = pos;
            spark.transform.localScale = Vector3.one * 0.18f;
            SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
            sr.sprite = GetFallbackDiscSprite();
            sr.color = new Color(color.r, color.g, color.b, 0.9f);
            sr.sortingOrder = 5;
            var host = TraitPeriodicAttackRunner.Instance;
            if (host != null) host.StartCoroutine(FadeSpark(spark, sr, at + (pos - at) * 2.2f, 0.22f));
        }
    }

    static IEnumerator FadeSpark(GameObject obj, SpriteRenderer sr, Vector3 end, float duration)
    {
        if (obj == null || sr == null) yield break;
        Vector3 start = obj.transform.position;
        Color baseColor = sr.color;
        float t = 0f;
        while (t < duration && obj != null && sr != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            obj.transform.position = Vector3.Lerp(start, end, p);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - p));
            obj.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.06f, p);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    static Sprite _coinSprite;
    static Sprite _fallbackDisc;

    static Sprite GetCoinSprite()
    {
        if (_coinSprite == null)
        {
            _coinSprite = GoldSpriteVisual.LoadGoldSprite();
        }
        return _coinSprite;
    }

    static Sprite GetFallbackDiscSprite()
    {
        if (_fallbackDisc != null) return _fallbackDisc;

        const int size = 24;
        Texture2D tex = new Texture2D(size, size);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                tex.SetPixel(x, y, d <= 1f ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        _fallbackDisc = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _fallbackDisc;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.time - spawnTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || target.IsExcludedFromCombat)
        {
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + Mathf.Sin((Time.time - spawnTime) * 14f + pulsePhase) * 0.12f;
        if (visualRoot != null)
        {
            visualRoot.localScale = Vector3.one * pulse;
        }
        if (glowRenderer != null)
        {
            glowRenderer.color = new Color(tint.r, tint.g, tint.b, 0.28f + Mathf.PingPong(Time.time * 6f, 0.18f));
        }

        Vector3 to = target.transform.position - transform.position;
        float dist = to.magnitude;
        if (dist <= hitRadius)
        {
            target.TakeDamage(damage, sourceUnitNumber);
            SpawnHitBurst(target.transform.position, tint);
            Destroy(gameObject);
            return;
        }

        Vector3 step = to.normalized * (speed * Time.deltaTime);
        transform.position += step;

        if (step.sqrMagnitude > 0.0001f && visualRoot != null)
        {
            float angle = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg;
            visualRoot.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    static void SpawnHitBurst(Vector3 at, Color color)
    {
        AttackHitEffectFire.Spawn(at, Vector3.one * 1.35f, Color.Lerp(color, Color.white, 0.35f));

        GameObject ring = new GameObject("GoblinHitRing");
        ring.transform.position = at;
        ring.transform.localScale = Vector3.one * 0.25f;
        SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
        sr.sprite = GetFallbackDiscSprite();
        sr.color = new Color(color.r, color.g, color.b, 0.75f);
        sr.sortingOrder = 5;
        var host = TraitPeriodicAttackRunner.Instance;
        if (host != null) host.StartCoroutine(ExpandFadeRing(ring, sr, 0.95f, 0.28f));
    }

    static IEnumerator ExpandFadeRing(GameObject obj, SpriteRenderer sr, float endScale, float duration)
    {
        if (obj == null || sr == null) yield break;
        Vector3 start = obj.transform.localScale;
        Vector3 end = Vector3.one * endScale;
        Color baseColor = sr.color;
        float t = 0f;
        while (t < duration && obj != null && sr != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            obj.transform.localScale = Vector3.Lerp(start, end, p);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - p));
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }
}
