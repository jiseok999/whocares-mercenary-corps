using UnityEngine;

/// <summary>
/// HUD 골드 아이콘(GameSceneController: nativeSize / 5)과 동일한 화면 크기의 월드 스프라이트 설정.
/// </summary>
public static class GoldSpriteVisual
{
    public const float HudIconSizeDivisor = 5f;
    public const string GoldResourceName = "gold";

    public static Sprite LoadGoldSprite()
    {
        return Resources.Load<Sprite>(GoldResourceName);
    }

    /// <summary>
    /// orthographic 카메라 기준, HUD 골드 아이콘과 같은 화면 픽셀 크기가 되도록 하는 월드 localScale(부모 스케일 1 기준).
    /// </summary>
    public static float GetHudMatchedWorldScale(Sprite sprite, Camera cam = null)
    {
        if (sprite == null)
        {
            return 0.2f;
        }

        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null || !cam.orthographic || Screen.height <= 0)
        {
            return sprite.rect.width / sprite.pixelsPerUnit / HudIconSizeDivisor;
        }

        float pixelsPerWorldUnit = Screen.height / (2f * cam.orthographicSize);
        float targetScreenPixels = sprite.rect.width / HudIconSizeDivisor;
        float worldWidth = targetScreenPixels / pixelsPerWorldUnit;
        return worldWidth / (sprite.rect.width / sprite.pixelsPerUnit);
    }

    public static float GetLossyScaleCompensation(Transform parent)
    {
        if (parent == null)
        {
            return 1f;
        }

        float s = parent.lossyScale.x;
        return Mathf.Max(0.001f, s);
    }

    public static void ApplySpriteRenderer(SpriteRenderer renderer, Sprite sprite, int sortingOrder = 3)
    {
        if (renderer == null)
        {
            return;
        }

        if (sprite != null)
        {
            renderer.sprite = sprite;
            renderer.color = Color.white;
        }

        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;
    }

    /// <summary>
    /// Visual 자식(스케일 1)에 붙일 BoxCollider2D 로컬 크기. transform 스케일은 부모에서 적용됩니다.
    /// </summary>
    public static Vector2 GetPickColliderSize(Sprite sprite)
    {
        if (sprite == null)
        {
            return new Vector2(0.5f, 0.5f);
        }

        Vector2 size = sprite.bounds.size * 1.15f;
        return new Vector2(
            Mathf.Max(0.35f, size.x),
            Mathf.Max(0.35f, size.y));
    }
}
