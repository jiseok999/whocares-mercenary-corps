using UnityEngine;

/// <summary>
/// 재앙 낙인 — 적 머리 위 종말 아이콘 + 링 펄스.
/// </summary>
public class CalamityMarkOverlay : MonoBehaviour
{
    SpriteRenderer iconRenderer;
    SpriteRenderer ringRenderer;
    float pulseTime;

    static Sprite cachedIcon;
    static Sprite cachedRing;

    public void Init(SpriteRenderer hostRenderer)
    {
        float yOff = 0.55f;
        int sortOrder = 40;
        if (hostRenderer != null)
        {
            yOff = hostRenderer.bounds.extents.y + 0.22f;
            sortOrder = hostRenderer.sortingOrder + 6;
        }

        if (iconRenderer == null)
        {
            GameObject iconObj = new GameObject("CalamityMarkIcon");
            iconObj.transform.SetParent(transform, false);
            iconObj.transform.localPosition = new Vector3(0f, yOff, 0f);
            iconRenderer = iconObj.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = GetIconSprite();
            iconRenderer.color = new Color(0.95f, 0.35f, 0.55f, 0.92f);
            iconRenderer.sortingOrder = sortOrder + 1;
        }

        if (ringRenderer == null)
        {
            GameObject ringObj = new GameObject("CalamityMarkRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, yOff, 0f);
            ringRenderer = ringObj.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = GetRingSprite();
            ringRenderer.color = new Color(0.55f, 0.12f, 0.42f, 0.55f);
            ringRenderer.sortingOrder = sortOrder;
        }

        transform.localScale = Vector3.one * 0.42f;
    }

    void Update()
    {
        pulseTime += Time.deltaTime * 4.2f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(pulseTime);
        if (iconRenderer != null)
        {
            iconRenderer.transform.localScale = Vector3.one * (0.85f + pulse * 0.18f);
            Color c = iconRenderer.color;
            c.a = 0.78f + pulse * 0.2f;
            iconRenderer.color = c;
        }
        if (ringRenderer != null)
        {
            float ringScale = 1.05f + pulse * 0.35f;
            ringRenderer.transform.localScale = new Vector3(ringScale, ringScale, 1f);
            Color rc = ringRenderer.color;
            rc.a = 0.35f + pulse * 0.25f;
            ringRenderer.color = rc;
        }
    }

    static Sprite GetIconSprite()
    {
        if (cachedIcon != null) return cachedIcon;
        cachedIcon = Resources.Load<Sprite>("Ability_end");
        if (cachedIcon == null)
        {
            cachedIcon = Resources.Load<Sprite>("Ability_evil");
        }
        return cachedIcon;
    }

    static Sprite GetRingSprite()
    {
        if (cachedRing != null) return cachedRing;
        cachedRing = CreateRingSprite();
        return cachedRing;
    }

    static Sprite CreateRingSprite()
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outer = size * 0.48f;
        float inner = size * 0.34f;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= outer && dist >= inner ? white : clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
