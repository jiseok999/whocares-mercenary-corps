using UnityEngine;

/// <summary>
/// 적에게 투사체 피해가 들어갔을 때 재생하는 불 타격 이펙트 (스프라이트 시트).
/// </summary>
public static class AttackHitEffectFire
{
    public const string DefaultResourcePath = "attack_effect_fire";

    public static void Spawn(Vector3 position, Vector3 scale)
    {
        Spawn(position, scale, Color.white);
    }

    public static void Spawn(Vector3 position, Vector3 scale, Color tint)
    {
        SpawnSpriteFrames(DefaultResourcePath, position, scale, 4, 0.06f, tint);
    }

    public static void SpawnSpriteFrames(
        string resourcePath,
        Vector3 position,
        Vector3 scale,
        int maxFrameIndex,
        float frameTime,
        Color tint)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        Sprite single = null;
        if (sprites == null || sprites.Length == 0)
        {
            single = Resources.Load<Sprite>(resourcePath);
            if (single == null)
            {
                return;
            }
        }
        else
        {
            System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        }

        GameObject effectObj = new GameObject("HitEffect");
        effectObj.transform.position = position;
        effectObj.transform.localScale = scale;

        SpriteRenderer renderer = effectObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 3;
        renderer.color = tint;

        if (sprites != null && sprites.Length > 0 && maxFrameIndex >= 0)
        {
            renderer.sprite = sprites[0];
            SpriteOnceAnimator onceAnimator = effectObj.AddComponent<SpriteOnceAnimator>();
            onceAnimator.Initialize(renderer, sprites, maxFrameIndex, frameTime);
        }
        else if (single != null)
        {
            renderer.sprite = single;
        }
        else
        {
            Object.Destroy(effectObj);
            return;
        }

        Object.Destroy(effectObj, 0.6f);
    }
}
