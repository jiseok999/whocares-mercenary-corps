using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특수 스킬 발동 시 절차적으로 생성하는 필드 이펙트
/// </summary>
public static class SkillEffectVisuals
{
    const int SortOrder = 55;

    static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    static Material defaultLineMaterial;

    public static void Play(int skillId, Vector3 worldPos, MonoBehaviour host)
    {
        if (host == null) return;

        switch (skillId)
        {
            case 1:
                host.StartCoroutine(FreezeEffect(GetFieldCenter(worldPos)));
                break;
            case 2:
                host.StartCoroutine(FireHellEffect(GetFieldCenter(worldPos)));
                break;
            case 3:
                host.StartCoroutine(BlackHoleEffect(worldPos, 2f, 1f));
                break;
            case 4:
                host.StartCoroutine(LightningEffect(worldPos, 2f));
                break;
        }
    }

    static Vector3 GetFieldCenter(Vector3 fallback)
    {
        Camera cam = Camera.main;
        if (cam == null) return fallback;

        Vector3 center = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.45f, -cam.transform.position.z));
        center.z = 0f;
        return center;
    }

    static IEnumerator FreezeEffect(Vector3 center)
    {
        // 서리 충격파
        GameObject ring = CreateDiscObject("FreezeRing", center, new Color(0.55f, 0.88f, 1f, 0.55f), 0.4f);
        yield return AnimateExpandFade(ring, 0.5f, 14f, 0.55f);

        // 얼음 결정 파편
        for (int i = 0; i < 10; i++)
        {
            float angle = i * (360f / 10f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            GameObject shard = CreateDiscObject("IceShard", center + dir * 0.4f, new Color(0.75f, 0.95f, 1f, 0.85f), 0.18f);
            hostStartMoveFade(shard, center + dir * 3.5f, 0.45f);
        }

        // 전장 서리 막
        GameObject frost = CreateDiscObject("FreezeField", center, new Color(0.65f, 0.90f, 1f, 0.22f), 8f);
        yield return FadeSprite(frost, 0.22f, 0f, 0.9f);
        Object.Destroy(frost);
    }

    static IEnumerator FireHellEffect(Vector3 center)
    {
        Camera cam = Camera.main;
        float span = cam != null ? cam.orthographicSize * cam.aspect * 1.6f : 10f;

        for (int i = 0; i < 7; i++)
        {
            float x = center.x + Random.Range(-span * 0.5f, span * 0.5f);
            Vector3 basePos = new Vector3(x, center.y - 2f, 0f);
            GameObject pillar = CreateDiscObject("FirePillar", basePos, new Color(1f, 0.45f, 0.12f, 0.75f), 0.35f);
            pillar.transform.localScale = new Vector3(0.5f, 0.2f, 1f);
            AnimateFirePillar(pillar, basePos + Vector3.up * 2.2f);
        }

        GameObject heat = CreateDiscObject("FireHeat", center, new Color(1f, 0.55f, 0.15f, 0.35f), 9f);
        yield return AnimateExpandFade(heat, 0.35f, 11f, 0.35f);
    }

    static IEnumerator BlackHoleEffect(Vector3 center, float radius, float duration)
    {
        GameObject core = CreateDiscObject("BlackHoleCore", center, new Color(0.18f, 0.05f, 0.28f, 0.92f), radius * 0.55f);
        GameObject ring = CreateRingObject("BlackHoleRing", center, new Color(0.55f, 0.25f, 0.85f, 0.65f), radius * 1.1f, radius * 0.75f);

        float elapsed = 0f;
        while (elapsed < duration + 0.25f)
        {
            elapsed += Time.deltaTime;
            float pulse = 0.9f + Mathf.Sin(elapsed * 14f) * 0.08f;
            if (core != null) core.transform.localScale = Vector3.one * pulse;
            if (ring != null) ring.transform.Rotate(0f, 0f, 220f * Time.deltaTime);
            yield return null;
        }

        if (core != null) Object.Destroy(core);
        if (ring != null) Object.Destroy(ring);

        GameObject collapse = CreateDiscObject("BlackHoleCollapse", center, new Color(0.35f, 0.1f, 0.5f, 0.5f), radius * 0.4f);
        yield return AnimateExpandFade(collapse, 0.2f, radius * 2.5f, 0.5f);
    }

    static IEnumerator LightningEffect(Vector3 center, float radius)
    {
        Vector3 top = center + Vector3.up * 5f;
        GameObject boltObj = new GameObject("LightningBolt");
        LineRenderer line = boltObj.AddComponent<LineRenderer>();
        ConfigureLine(line, new Color(1f, 0.92f, 0.45f, 1f), 0.14f, 0.04f);
        BuildLightningPath(line, top, center, 7);

        GameObject flash = CreateDiscObject("LightningFlash", center, new Color(1f, 0.85f, 0.35f, 0.7f), radius * 0.5f);
        GameObject ring = CreateRingObject("LightningRing", center, new Color(1f, 0.78f, 0.25f, 0.8f), radius * 1.2f, radius * 0.85f);

        yield return FadeSprite(boltObj, 1f, 0f, 0.12f);
        Object.Destroy(boltObj);

        yield return AnimateExpandFade(ring, 0.18f, radius * 2.2f, 0.75f);
        yield return FadeSprite(flash, 0.7f, 0f, 0.25f);
        if (flash != null) Object.Destroy(flash);
    }

    static void hostStartMoveFade(GameObject obj, Vector3 target, float duration)
    {
        if (obj == null) return;
        SkillEffectRunner runner = obj.AddComponent<SkillEffectRunner>();
        runner.Run(MoveAndFade(obj, target, duration));
    }

    static void AnimateFirePillar(GameObject pillar, Vector3 peak)
    {
        if (pillar == null) return;
        SkillEffectRunner runner = pillar.AddComponent<SkillEffectRunner>();
        runner.Run(FirePillarRoutine(pillar, peak));
    }

    static IEnumerator FirePillarRoutine(GameObject pillar, Vector3 peak)
    {
        Vector3 start = pillar.transform.position;
        float duration = 0.45f;
        float elapsed = 0f;
        SpriteRenderer sr = pillar.GetComponent<SpriteRenderer>();
        Color baseColor = sr != null ? sr.color : Color.white;

        while (elapsed < duration && pillar != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            pillar.transform.position = Vector3.Lerp(start, peak, t);
            pillar.transform.localScale = new Vector3(0.55f + t * 0.35f, 0.25f + t * 1.6f, 1f);
            if (sr != null)
            {
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
            }
            yield return null;
        }

        if (pillar != null) Object.Destroy(pillar);
    }

    static IEnumerator MoveAndFade(GameObject obj, Vector3 target, float duration)
    {
        if (obj == null) yield break;
        Vector3 start = obj.transform.position;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        Color baseColor = sr != null ? sr.color : Color.white;
        float elapsed = 0f;

        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.position = Vector3.Lerp(start, target, t);
            if (sr != null)
            {
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
            }
            yield return null;
        }

        if (obj != null) Object.Destroy(obj);
    }

    static IEnumerator AnimateExpandFade(GameObject obj, float duration, float targetScale, float startAlpha)
    {
        if (obj == null) yield break;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        Color baseColor = sr != null ? sr.color : Color.white;
        float elapsed = 0f;

        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0.3f, targetScale, t);
            obj.transform.localScale = Vector3.one * scale;
            if (sr != null)
            {
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(startAlpha, 0f, t));
            }
            yield return null;
        }

        if (obj != null) Object.Destroy(obj);
    }

    static IEnumerator FadeSprite(GameObject obj, float startAlpha, float endAlpha, float duration)
    {
        if (obj == null) yield break;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            if (lr != null)
            {
                Color c = lr.startColor;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float a = Mathf.Lerp(startAlpha, endAlpha, t);
                    c.a = a;
                    lr.startColor = c;
                    lr.endColor = c;
                    yield return null;
                }
            }
            Object.Destroy(obj);
            yield break;
        }

        Color baseColor = sr.color;
        float e = 0f;
        while (e < duration && obj != null)
        {
            e += Time.deltaTime;
            float t = e / duration;
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(startAlpha, endAlpha, t));
            yield return null;
        }

        if (obj != null) Object.Destroy(obj);
    }

    static GameObject CreateDiscObject(string name, Vector3 position, Color color, float diameter)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSoftDiscSprite(color);
        sr.color = color;
        sr.sortingOrder = SortOrder;
        obj.transform.localScale = Vector3.one * diameter;
        return obj;
    }

    static GameObject CreateRingObject(string name, Vector3 position, Color color, float outerDiameter, float innerRatio)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite(color, innerRatio / outerDiameter);
        sr.color = color;
        sr.sortingOrder = SortOrder + 1;
        obj.transform.localScale = Vector3.one * outerDiameter;
        return obj;
    }

    static void ConfigureLine(LineRenderer line, Color color, float startWidth, float endWidth)
    {
        line.material = GetLineMaterial();
        line.sortingOrder = SortOrder + 2;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.useWorldSpace = true;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
    }

    static void BuildLightningPath(LineRenderer line, Vector3 top, Vector3 bottom, int segments)
    {
        line.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(top, bottom, t);
            if (i > 0 && i < segments)
            {
                p.x += Random.Range(-0.35f, 0.35f);
            }
            line.SetPosition(i, p);
        }
    }

    static Material GetLineMaterial()
    {
        if (defaultLineMaterial != null) return defaultLineMaterial;
        defaultLineMaterial = new Material(Shader.Find("Sprites/Default"));
        return defaultLineMaterial;
    }

    static Sprite GetSoftDiscSprite(Color tint)
    {
        string key = $"disc_{ColorUtility.ToHtmlStringRGBA(tint)}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(1f - dist / radius);
                a = a * a;
                tex.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, a));
            }
        }
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        spriteCache[key] = sprite;
        return sprite;
    }

    static Sprite GetRingSprite(Color tint, float innerRadiusRatio)
    {
        string key = $"ring_{ColorUtility.ToHtmlStringRGBA(tint)}_{innerRadiusRatio:0.00}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float outer = size * 0.48f;
        float inner = outer * Mathf.Clamp01(innerRadiusRatio);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = dist <= outer && dist >= inner ? 1f : 0f;
                if (a > 0f)
                {
                    float edge = Mathf.Min(dist - inner, outer - dist);
                    a = Mathf.Clamp01(edge / 2f);
                }
                tex.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, a));
            }
        }
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetPreviewDiscSprite() => GetSoftDiscSprite(Color.white);

    public static Sprite GetPreviewRingSprite() => GetRingSprite(Color.white, 0.88f);

    /// <summary>코루틴 실행용 임시 컴포넌트</summary>
    sealed class SkillEffectRunner : MonoBehaviour
    {
        public void Run(IEnumerator routine)
        {
            StartCoroutine(routine);
        }
    }
}
