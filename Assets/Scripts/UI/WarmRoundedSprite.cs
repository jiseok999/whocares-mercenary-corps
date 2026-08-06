using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 안티앨리어싱된 둥근 사각형 스프라이트를 절차적으로 생성하는 공용 유틸.
/// 파라미터 조합으로 자동 키를 만들어 캐싱하므로 동일 스타일은 텍스처를 재사용합니다.
/// (9-slice border 포함, 선택적 테두리 지원)
/// </summary>
public static class WarmRoundedSprite
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(Color fill, int cornerRadius, Color borderColor, float borderThickness)
    {
        string key = $"{fill}|{cornerRadius}|{borderColor}|{borderThickness}";
        if (cache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        int r = Mathf.Max(2, cornerRadius);
        int size = r * 2 + 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float half = size / 2f;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float dx = Mathf.Abs(px) - (half - r);
                float dy = Mathf.Abs(py) - (half - r);
                float qx = Mathf.Max(dx, 0f);
                float qy = Mathf.Max(dy, 0f);
                float dist = Mathf.Sqrt(qx * qx + qy * qy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;

                float outerCoverage = Mathf.Clamp01(0.5f - dist);
                float innerCoverage = Mathf.Clamp01(0.5f - (dist + borderThickness));

                Color rgb = Color.Lerp(borderColor, fill, innerCoverage);
                float alpha = outerCoverage * Mathf.Lerp(borderColor.a, fill.a, innerCoverage);
                pixels[y * size + x] = new Color(rgb.r, rgb.g, rgb.b, alpha);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(r, r, r, r));
        cache[key] = sprite;
        return sprite;
    }
}
