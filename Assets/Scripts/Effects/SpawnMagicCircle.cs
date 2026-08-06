using UnityEngine;

/// <summary>
/// 적 소환 위치에 표시되는 마법진. 소환 완료 시 외부에서 Destroy 합니다.
/// </summary>
public class SpawnMagicCircle : MonoBehaviour
{
    private static Sprite cachedRingSprite;

    private SpriteRenderer outerRenderer;
    private SpriteRenderer innerRenderer;
    private float pulseTimer;
    private float baseScale = 1f;

    public static GameObject SpawnAt(Vector3 worldPosition, float diameter = 1f)
    {
        GameObject root = new GameObject("SpawnMagicCircle");
        root.transform.position = worldPosition;

        SpawnMagicCircle circle = root.AddComponent<SpawnMagicCircle>();
        circle.BuildVisuals(Mathf.Max(0.4f, diameter));
        return root;
    }

    void BuildVisuals(float diameter)
    {
        baseScale = diameter;

        GameObject outerObj = new GameObject("OuterRing");
        outerObj.transform.SetParent(transform, false);
        outerRenderer = outerObj.AddComponent<SpriteRenderer>();
        outerRenderer.sprite = GetRingSprite();
        outerRenderer.color = new Color(0.55f, 0.25f, 1f, 0.75f);
        outerRenderer.sortingOrder = 0;

        GameObject innerObj = new GameObject("InnerGlow");
        innerObj.transform.SetParent(transform, false);
        innerRenderer = innerObj.AddComponent<SpriteRenderer>();
        innerRenderer.sprite = GetRingSprite();
        innerRenderer.color = new Color(0.75f, 0.45f, 1f, 0.35f);
        innerRenderer.sortingOrder = 1;

        transform.localScale = Vector3.one * baseScale;
        innerObj.transform.localScale = Vector3.one * 0.65f;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        pulseTimer += Time.deltaTime * 4f;
        float pulse = 0.92f + Mathf.Sin(pulseTimer) * 0.08f;
        transform.localScale = Vector3.one * (baseScale * pulse);
        transform.Rotate(0f, 0f, 45f * Time.deltaTime);

        if (outerRenderer != null)
        {
            Color c = outerRenderer.color;
            c.a = 0.55f + Mathf.Sin(pulseTimer * 1.3f) * 0.2f;
            outerRenderer.color = c;
        }
    }

    public static Sprite GetRingSprite()
    {
        if (cachedRingSprite != null) return cachedRingSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.48f;
        float innerR = size * 0.32f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float a = 0f;
                if (dist <= outerR && dist >= innerR)
                {
                    float t = Mathf.InverseLerp(outerR, innerR, dist);
                    a = Mathf.Lerp(0.25f, 1f, t);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedRingSprite;
    }
}
