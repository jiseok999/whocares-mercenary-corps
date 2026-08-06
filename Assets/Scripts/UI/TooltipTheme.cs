using UnityEngine;
using UnityEngine.UI;

public static class TooltipTheme
{
    private static Sprite cachedBackgroundSprite;

    public static void ApplyStandardBackground(Image target, bool withOutline = true)
    {
        if (target == null) return;

        target.sprite = GetStandardBackgroundSprite();
        target.color = Color.white;
        target.type = Image.Type.Simple;
        target.raycastTarget = false;

        if (withOutline)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(1f, 0.85f, 0.25f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }

    public static Sprite GetStandardBackgroundSprite()
    {
        if (cachedBackgroundSprite != null) return cachedBackgroundSprite;

        const int width = 72;
        const int height = 40;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 outerBorder = new Color32(246, 198, 66, 255);
        Color32 innerBorder = new Color32(117, 84, 27, 255);
        Color32 fillTop = new Color32(23, 33, 25, 246);
        Color32 fillBottom = new Color32(10, 14, 12, 246);

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color fill = Color.Lerp(fillBottom, fillTop, t);
            for (int x = 0; x < width; x++)
            {
                bool isOuter = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                bool isInner = x == 1 || y == 1 || x == width - 2 || y == height - 2;
                if (isOuter)
                {
                    texture.SetPixel(x, y, outerBorder);
                }
                else if (isInner)
                {
                    texture.SetPixel(x, y, innerBorder);
                }
                else
                {
                    texture.SetPixel(x, y, fill);
                }
            }
        }

        texture.Apply();
        cachedBackgroundSprite = Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            1f
        );
        return cachedBackgroundSprite;
    }
}
