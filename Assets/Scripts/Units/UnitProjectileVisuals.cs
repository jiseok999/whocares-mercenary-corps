using UnityEngine;

/// <summary>
/// 전용 공격 스프라이트가 없어 기본 사각형을 쓰던 유닛(10, 14) 투사체 비주얼.
/// </summary>
public static class UnitProjectileVisuals
{
    /// <summary>10번 기본 직선탄 localScale (이전 0.3 대비 약 2배).</summary>
    public const float Unit10ProjectileScale = 0.6f;
    /// <summary>10번 분열·연사 보조탄 비율(본탄 대비).</summary>
    public const float Unit10ChildProjectileScaleFactor = 0.75f;
    /// <summary>14번 튕김 투사체 localScale (이전 0.5 대비 약 1.8배).</summary>
    public const float Unit14ProjectileScale = 0.9f;

    static readonly string[] Unit10Resources =
    {
        "unit_009_attack",
        "unit_1_attack",
        "unit_002_attack",
        "Warped Shooting Fx/Pixel Art/Bolt/bolt1"
    };

    static readonly string[] Unit14Resources =
    {
        "unit_009_attack",
        "unit_015_attack",
        "unit_040_attack"
    };

    public static bool TryApply(SpriteRenderer renderer, int unitNumber)
    {
        if (renderer == null)
        {
            return false;
        }

        string[] chain = null;
        Color fallback = Color.white;
        if (unitNumber == 10)
        {
            chain = Unit10Resources;
            fallback = new Color(0.72f, 0.92f, 1f);
        }
        else if (unitNumber == 14)
        {
            chain = Unit14Resources;
            fallback = new Color(1f, 0.95f, 0.45f);
        }
        else
        {
            return false;
        }

        for (int i = 0; i < chain.Length; i++)
        {
            Sprite sprite = LoadFirstSprite(chain[i]);
            if (sprite != null)
            {
                renderer.sprite = sprite;
                renderer.color = Color.white;
                return true;
            }
        }

        renderer.sprite = CreateSquareSprite(fallback);
        renderer.color = fallback;
        return false;
    }

    static Sprite LoadFirstSprite(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }

        Sprite single = Resources.Load<Sprite>(resourceName);
        if (single != null)
        {
            return single;
        }

        Sprite[] sheet = Resources.LoadAll<Sprite>(resourceName);
        if (sheet == null || sheet.Length == 0)
        {
            return null;
        }

        System.Array.Sort(sheet, (a, b) => string.CompareOrdinal(a.name, b.name));
        return sheet[0];
    }

    static Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
