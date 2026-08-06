using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 툴팁용 스탯 아이콘을 절차적으로 생성하는 유틸(외부 리소스 의존 없음).
/// 안티앨리어싱 적용, 타입별 1회 생성 후 캐싱.
/// </summary>
public static class StatIconFactory
{
    public enum IconType { Damage, Health, AttackSpeed, Range }

    static readonly Dictionary<IconType, Sprite> cache = new Dictionary<IconType, Sprite>();
    const int Size = 44;
    const int SS = 3; // 슈퍼샘플링 (안티앨리어싱)

    public static Sprite Get(IconType type)
    {
        if (cache.TryGetValue(type, out Sprite s) && s != null) return s;

        Color color;
        switch (type)
        {
            case IconType.Damage: color = new Color(1f, 0.48f, 0.30f); break;       // 주황빨강
            case IconType.Health: color = new Color(0.45f, 0.85f, 0.52f); break;     // 초록
            case IconType.AttackSpeed: color = new Color(1f, 0.82f, 0.30f); break;   // 골드
            default: color = new Color(0.88f, 0.73f, 0.48f); break;                  // 탄
        }

        Sprite sprite = Build(type, color);
        cache[type] = sprite;
        return sprite;
    }

    static Sprite Build(IconType type, Color color)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] px = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float cov = 0f;
                for (int sy = 0; sy < SS; sy++)
                {
                    for (int sx = 0; sx < SS; sx++)
                    {
                        float fx = (x + (sx + 0.5f) / SS) / Size;
                        float fy = (y + (sy + 0.5f) / SS) / Size;
                        if (Inside(type, fx, fy)) cov += 1f;
                    }
                }
                cov /= SS * SS;
                Color c = color;
                c.a = cov;
                px[y * Size + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool Inside(IconType t, float x, float y)
    {
        switch (t)
        {
            case IconType.Health: return Heart(x, y);
            case IconType.AttackSpeed: return Bolt(x, y);
            case IconType.Range: return Target(x, y);
            default: return Sword(x, y);
        }
    }

    // 하트 (체력)
    static bool Heart(float x, float y)
    {
        float u = (x - 0.5f) * 2.6f;
        float v = (y - 0.56f) * 2.6f;
        float f = u * u + v * v - 1f;
        return f * f * f - u * u * v * v * v <= 0f;
    }

    // 번개 (공격속도)
    static readonly Vector2[] BoltPoly =
    {
        new Vector2(0.60f, 0.96f),
        new Vector2(0.20f, 0.50f),
        new Vector2(0.44f, 0.50f),
        new Vector2(0.38f, 0.05f),
        new Vector2(0.82f, 0.54f),
        new Vector2(0.52f, 0.54f),
    };

    static bool Bolt(float x, float y)
    {
        return PointInPoly(BoltPoly, x, y);
    }

    // 검 (공격력)
    static bool Sword(float x, float y)
    {
        // 칼날
        if (x >= 0.44f && x <= 0.56f && y >= 0.32f && y <= 0.82f) return true;
        // 칼끝(삼각형)
        if (PointInTriangle(x, y, new Vector2(0.5f, 0.96f), new Vector2(0.43f, 0.82f), new Vector2(0.57f, 0.82f))) return true;
        // 가드(가로)
        if (x >= 0.30f && x <= 0.70f && y >= 0.25f && y <= 0.32f) return true;
        // 손잡이
        if (x >= 0.46f && x <= 0.54f && y >= 0.10f && y <= 0.25f) return true;
        // 폼멜
        if (x >= 0.44f && x <= 0.56f && y >= 0.06f && y <= 0.11f) return true;
        return false;
    }

    // 과녁 (사거리)
    static bool Target(float x, float y)
    {
        float dx = x - 0.5f;
        float dy = y - 0.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        if (Mathf.Abs(r - 0.40f) < 0.065f) return true;
        if (Mathf.Abs(r - 0.22f) < 0.060f) return true;
        if (r < 0.085f) return true;
        return false;
    }

    static bool PointInTriangle(float px, float py, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(px, py, a, b);
        float d2 = Sign(px, py, b, c);
        float d3 = Sign(px, py, c, a);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    static float Sign(float px, float py, Vector2 a, Vector2 b)
    {
        return (px - b.x) * (a.y - b.y) - (a.x - b.x) * (py - b.y);
    }

    static bool PointInPoly(Vector2[] poly, float x, float y)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if (((poly[i].y > y) != (poly[j].y > y)) &&
                (x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
