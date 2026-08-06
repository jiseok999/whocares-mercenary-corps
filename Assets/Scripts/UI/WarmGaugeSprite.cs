using System.Collections.Generic;

using UnityEngine;



/// <summary>

/// 웜톤 UI용 가로 게이지(트랙·필) 스프라이트를 절차적으로 생성합니다.

/// </summary>

public static class WarmGaugeSprite

{

    public enum FillStyle

    {

        Gold,

        EnemyHealth,

    }



    enum BarSliceMode

    {

        None,

        HorizontalCaps,

    }



    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();



    const float WorldBarPixelsPerUnit = 100f;

    const int WorldEnemyHpBarWidthPx = 72;

    const int WorldEnemyHpBarHeightPx = 10;

    const int WorldBossHpBarWidthPx = 172;

    const int WorldBossHpBarHeightPx = 21;



    public static float WorldEnemyHpBarWidth => WorldEnemyHpBarWidthPx / WorldBarPixelsPerUnit;

    public static float WorldEnemyHpBarHeight => WorldEnemyHpBarHeightPx / WorldBarPixelsPerUnit;

    public static float WorldBossHpBarWidth => WorldBossHpBarWidthPx / WorldBarPixelsPerUnit;

    public static float WorldBossHpBarHeight => WorldBossHpBarHeightPx / WorldBarPixelsPerUnit;



    public static Sprite GetWorldEnemyHpShell()

    {

        return GetOrCreate("world_enemy_hp_shell_v2", WorldEnemyHpBarWidthPx + 6, WorldEnemyHpBarHeightPx + 4,

            (WorldEnemyHpBarHeightPx + 4) / 2,

            new Color(0.55f, 0.36f, 0.20f, 1f),

            new Color(0.30f, 0.18f, 0.11f, 1f),

            new Color(1f, 0.78f, 0.32f, 1f),

            0.45f,

            WorldBarPixelsPerUnit,

            BarSliceMode.None);

    }



    public static Sprite GetWorldEnemyHpTrack()

    {

        return GetOrCreate("world_enemy_hp_track_v2", WorldEnemyHpBarWidthPx, WorldEnemyHpBarHeightPx,

            WorldEnemyHpBarHeightPx / 2,

            new Color(0.22f, 0.14f, 0.10f, 1f),

            new Color(0.12f, 0.08f, 0.06f, 1f),

            new Color(0.40f, 0.26f, 0.18f, 0.5f),

            0.35f,

            WorldBarPixelsPerUnit,

            BarSliceMode.None);

    }



    public static Sprite GetWorldBossHpShell()

    {

        return GetOrCreate("world_boss_hp_shell_v2", WorldBossHpBarWidthPx + 12, WorldBossHpBarHeightPx + 8,

            (WorldBossHpBarHeightPx + 8) / 2,

            new Color(0.55f, 0.06f, 0.06f, 1f),

            new Color(0.22f, 0.02f, 0.02f, 1f),

            new Color(1f, 0.35f, 0.28f, 1f),

            0.72f,

            WorldBarPixelsPerUnit,

            BarSliceMode.None);

    }



    public static Sprite GetWorldBossHpTrack()

    {

        return GetOrCreate("world_boss_hp_track_v2", WorldBossHpBarWidthPx, WorldBossHpBarHeightPx,

            WorldBossHpBarHeightPx / 2,

            new Color(0.24f, 0.04f, 0.04f, 1f),

            new Color(0.10f, 0.02f, 0.02f, 1f),

            new Color(0.72f, 0.10f, 0.10f, 0.55f),

            0.45f,

            WorldBarPixelsPerUnit,

            BarSliceMode.None);

    }



    public static Sprite GetWorldBossHpFill()

    {

        return GetOrCreate("world_boss_hp_fill_v2", WorldBossHpBarWidthPx, WorldBossHpBarHeightPx,

            WorldBossHpBarHeightPx / 2,

            new Color(1f, 0.22f, 0.16f, 1f),

            new Color(0.92f, 0.06f, 0.06f, 1f),

            new Color(1f, 0.48f, 0.32f, 1f),

            0.65f,

            WorldBarPixelsPerUnit,

            BarSliceMode.None);

    }



    public static Sprite GetWorldEnemyHpFill()

    {

        return GetOrCreate("world_enemy_hp_fill_v2", WorldEnemyHpBarWidthPx, WorldEnemyHpBarHeightPx,

            WorldEnemyHpBarHeightPx / 2,

            new Color(0.45f, 0.98f, 0.38f, 1f),

            new Color(0.10f, 0.72f, 0.18f, 1f),

            new Color(0.92f, 1f, 0.82f, 1f),

            0.55f,

            WorldBarPixelsPerUnit,

            BarSliceMode.None);

    }



    public static Sprite GetTrackShell()

    {

        return WarmRoundedSprite.Get(

            new Color(0.18f, 0.12f, 0.09f, 0.98f),

            8,

            new Color(0.96f, 0.62f, 0.18f, 0.45f),

            1.4f);

    }



    public static Sprite GetTrackInner(int barHeight = 14)

    {

        return GetOrCreate($"track_inner_v2_{barHeight}", 96, barHeight, barHeight / 2,

            new Color(0.18f, 0.12f, 0.09f, 1f),

            new Color(0.10f, 0.07f, 0.05f, 1f),

            new Color(0.50f, 0.34f, 0.22f, 0.45f),

            0.55f,

            100f,

            BarSliceMode.HorizontalCaps);

    }



    public static Sprite GetFill(FillStyle style)

    {

        switch (style)

        {

            case FillStyle.EnemyHealth:

                return GetOrCreate("fill_enemy_hp_v2", 32, 6, 3,

                    new Color(0.45f, 0.98f, 0.38f, 1f),

                    new Color(0.10f, 0.72f, 0.18f, 1f),

                    new Color(0.92f, 1f, 0.82f, 1f),

                    0.5f,

                    100f,

                    BarSliceMode.None);

            default:

                return GetOrCreate("fill_gold_v2", 96, 14, 7,

                    new Color(1f, 0.88f, 0.22f, 1f),

                    new Color(0.95f, 0.48f, 0.04f, 1f),

                    new Color(1f, 0.98f, 0.62f, 1f),

                    0.6f,

                    100f,

                    BarSliceMode.None);

        }

    }



    public static Color GetEnemyHealthTint(float healthRatio)

    {

        healthRatio = Mathf.Clamp01(healthRatio);

        if (healthRatio <= 0.25f)

        {

            return Color.Lerp(new Color(1f, 0.28f, 0.22f, 1f), new Color(1f, 0.48f, 0.22f, 1f), healthRatio / 0.25f);

        }



        if (healthRatio <= 0.55f)

        {

            return Color.Lerp(new Color(1f, 0.82f, 0.18f, 1f), new Color(1f, 0.48f, 0.22f, 1f), (healthRatio - 0.25f) / 0.30f);

        }



        return Color.Lerp(new Color(0.62f, 1f, 0.42f, 1f), new Color(1f, 0.82f, 0.18f, 1f), (healthRatio - 0.55f) / 0.45f);

    }



    public static Color GetBossHealthTint(float healthRatio)

    {

        healthRatio = Mathf.Clamp01(healthRatio);

        if (healthRatio <= 0.25f)

        {

            return Color.Lerp(new Color(1f, 0.12f, 0.12f, 1f), new Color(1f, 0.35f, 0.12f, 1f), healthRatio / 0.25f);

        }



        if (healthRatio <= 0.55f)

        {

            return Color.Lerp(new Color(1f, 0.72f, 0.15f, 1f), new Color(1f, 0.35f, 0.12f, 1f), (healthRatio - 0.25f) / 0.30f);

        }



        return Color.Lerp(new Color(1f, 0.55f, 0.22f, 1f), new Color(1f, 0.88f, 0.35f, 1f), (healthRatio - 0.55f) / 0.45f);

    }




    static Sprite GetOrCreate(string key, int width, int height, int radius,

        Color topColor, Color bottomColor, Color highlightColor, float highlightStrength,

        float pixelsPerUnit = 100f, BarSliceMode sliceMode = BarSliceMode.HorizontalCaps)

    {

        string cacheKey = $"{key}|{width}|{height}|{radius}|{pixelsPerUnit}|{sliceMode}";

        if (cache.TryGetValue(cacheKey, out Sprite cached) && cached != null)

        {

            return cached;

        }



        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)

        {

            wrapMode = TextureWrapMode.Clamp,

            filterMode = FilterMode.Bilinear

        };



        float r = Mathf.Min(radius, height * 0.5f - 0.5f);



        for (int y = 0; y < height; y++)

        {

            float v = height <= 1 ? 0.5f : y / (height - 1f);

            Color rowBase = Color.Lerp(bottomColor, topColor, v);



            for (int x = 0; x < width; x++)

            {

                float dist = SignedDistanceToRoundedRect(x + 0.5f, y + 0.5f, width - 1f, height - 1f, r);

                if (dist > 0.5f)

                {

                    tex.SetPixel(x, y, Color.clear);

                    continue;

                }



                float edge = dist > -0.85f ? Mathf.Clamp01(1f + dist * 2.2f) : 1f;

                Color c = rowBase;

                c.a = 1f;



                float highlightBand = Mathf.Clamp01(1f - Mathf.Abs(v - 0.68f) * 4.5f);

                Color highlight = highlightColor;

                highlight.a = 1f;

                c.r = Mathf.Lerp(c.r, highlight.r, highlightBand * highlightStrength * edge);

                c.g = Mathf.Lerp(c.g, highlight.g, highlightBand * highlightStrength * edge);

                c.b = Mathf.Lerp(c.b, highlight.b, highlightBand * highlightStrength * edge);

                c.a *= edge;



                tex.SetPixel(x, y, c);

            }

        }



        tex.Apply();



        Vector4 border = Vector4.zero;

        if (sliceMode == BarSliceMode.HorizontalCaps)

        {

            int cap = Mathf.Clamp(radius, 1, Mathf.Max(1, (width / 2) - 1));

            border = new Vector4(cap, 0f, 0f, cap);

        }



        Sprite sprite = Sprite.Create(

            tex,

            new Rect(0, 0, width, height),

            new Vector2(0.5f, 0.5f),

            pixelsPerUnit,

            0,

            SpriteMeshType.FullRect,

            border);

        cache[cacheKey] = sprite;

        return sprite;

    }



    static float SignedDistanceToRoundedRect(float px, float py, float width, float height, float radius)

    {

        float cx = width * 0.5f;

        float cy = height * 0.5f;

        float hx = width * 0.5f;

        float hy = height * 0.5f;

        float dx = Mathf.Abs(px - cx) - (hx - radius);

        float dy = Mathf.Abs(py - cy) - (hy - radius);

        float ax = Mathf.Max(dx, 0f);

        float ay = Mathf.Max(dy, 0f);

        float outside = Mathf.Sqrt(ax * ax + ay * ay) - radius;

        float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);

        return outside + inside;

    }

}


