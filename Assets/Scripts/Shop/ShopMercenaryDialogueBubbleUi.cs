using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 용병 고용 대사 말풍선 UI (본체 + 꼬리 + 그림자).
/// </summary>
public static class ShopMercenaryDialogueBubbleUi
{
    static readonly Dictionary<string, Sprite> TailSpriteCache = new Dictionary<string, Sprite>();

    public static GameObject Create(Transform shopSlot, string line, ShopMercenaryDialogueBubbleTheme theme = null)
    {
        if (shopSlot == null || string.IsNullOrEmpty(line)) return null;

        theme = theme ?? ShopMercenaryDialogueBubbleTheme.Active;

        Transform existing = shopSlot.Find("MercenaryDialogue");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        float textWidth = theme.bubbleWidth - theme.paddingHorizontal * 2f;
        float textHeight = MeasureTextHeight(theme, line, textWidth);
        float bodyHeight = Mathf.Clamp(
            theme.paddingTop + textHeight + theme.paddingBottom,
            theme.minBodyHeight,
            theme.maxBodyHeight);
        float totalHeight = bodyHeight + theme.tailHeight - theme.tailOverlap;

        GameObject root = new GameObject("MercenaryDialogue");
        root.transform.SetParent(shopSlot, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, theme.offsetAboveSlot);
        rootRect.sizeDelta = new Vector2(theme.bubbleWidth, totalHeight);

        if (theme.useShadow)
        {
            CreateBodyImage(root.transform, "Shadow", theme, bodyHeight, theme.shadowColor, theme.shadowOffset);
            CreateTailImage(root.transform, "TailShadow", theme, bodyHeight, theme.shadowColor, theme.shadowOffset);
        }

        CreateBodyImage(root.transform, "Body", theme, bodyHeight, theme.fillColor, Vector2.zero);
        CreateTailImage(root.transform, "Tail", theme, bodyHeight, theme.fillColor, Vector2.zero);
        CreateText(root.transform, theme, line, bodyHeight, textWidth);

        Transform shadow = root.transform.Find("Shadow");
        Transform tailShadow = root.transform.Find("TailShadow");
        Transform body = root.transform.Find("Body");
        Transform tail = root.transform.Find("Tail");
        Transform text = root.transform.Find("Text");
        int order = 0;
        if (shadow != null) shadow.SetSiblingIndex(order++);
        if (tailShadow != null) tailShadow.SetSiblingIndex(order++);
        if (body != null) body.SetSiblingIndex(order++);
        if (tail != null) tail.SetSiblingIndex(order++);
        if (text != null) text.SetSiblingIndex(order);

        root.transform.SetAsLastSibling();
        return root;
    }

    static void CreateBodyImage(
        Transform parent,
        string name,
        ShopMercenaryDialogueBubbleTheme theme,
        float bodyHeight,
        Color fillColor,
        Vector2 offset)
    {
        GameObject bodyObj = new GameObject(name);
        bodyObj.transform.SetParent(parent, false);

        RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 1f);
        bodyRect.anchorMax = new Vector2(0.5f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = offset;
        bodyRect.sizeDelta = new Vector2(theme.bubbleWidth, bodyHeight);

        Image bodyImage = bodyObj.AddComponent<Image>();
        bodyImage.raycastTarget = false;
        bodyImage.type = Image.Type.Sliced;
        bodyImage.sprite = WarmRoundedSprite.Get(
            fillColor,
            theme.cornerRadius,
            name == "Shadow" ? Color.clear : theme.borderColor,
            name == "Shadow" ? 0f : theme.borderThickness);
        bodyImage.color = Color.white;
    }

    static void CreateTailImage(
        Transform parent,
        string name,
        ShopMercenaryDialogueBubbleTheme theme,
        float bodyHeight,
        Color fillColor,
        Vector2 offset)
    {
        GameObject tailObj = new GameObject(name);
        tailObj.transform.SetParent(parent, false);

        int tailPixelWidth = Mathf.Max(8, Mathf.RoundToInt(theme.tailWidth));
        int tailPixelHeight = Mathf.Max(6, Mathf.RoundToInt(theme.tailHeight));
        bool isShadow = name.Contains("Shadow");
        Color border = isShadow ? Color.clear : theme.borderColor;
        float borderThickness = isShadow ? 0f : theme.borderThickness;

        RectTransform tailRect = tailObj.AddComponent<RectTransform>();
        tailRect.anchorMin = new Vector2(0.5f, 1f);
        tailRect.anchorMax = new Vector2(0.5f, 1f);
        tailRect.pivot = new Vector2(0.5f, 1f);
        tailRect.anchoredPosition = new Vector2(offset.x, -(bodyHeight - theme.tailOverlap) + offset.y);
        tailRect.sizeDelta = new Vector2(theme.tailWidth, theme.tailHeight);

        Image tailImage = tailObj.AddComponent<Image>();
        tailImage.raycastTarget = false;
        tailImage.sprite = GetTailSprite(tailPixelWidth, tailPixelHeight, fillColor, border, borderThickness);
        tailImage.color = Color.white;
    }

    static void CreateText(
        Transform parent,
        ShopMercenaryDialogueBubbleTheme theme,
        string line,
        float bodyHeight,
        float textWidth)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        Text bodyText = textObj.AddComponent<Text>();
        bodyText.text = line;
        bodyText.font = UIFontProvider.Get();
        bodyText.fontSize = theme.fontSize;
        bodyText.lineSpacing = theme.lineSpacing;
        bodyText.color = theme.textColor;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        bodyText.raycastTarget = false;
        bodyText.supportRichText = false;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -theme.paddingTop);
        textRect.sizeDelta = new Vector2(textWidth, bodyHeight - theme.paddingTop - theme.paddingBottom);
    }

    static float MeasureTextHeight(ShopMercenaryDialogueBubbleTheme theme, string text, float width)
    {
        if (string.IsNullOrEmpty(text)) return theme.fontSize;

        Font font = UIFontProvider.Get();
        if (font == null) return theme.fontSize * 2f;

        TextGenerationSettings settings = new TextGenerationSettings
        {
            font = font,
            fontSize = theme.fontSize,
            fontStyle = FontStyle.Normal,
            richText = false,
            lineSpacing = theme.lineSpacing,
            scaleFactor = 1f,
            verticalOverflow = VerticalWrapMode.Overflow,
            horizontalOverflow = HorizontalWrapMode.Wrap,
            generationExtents = new Vector2(Mathf.Max(1f, width), 0f),
            textAnchor = TextAnchor.UpperLeft,
            alignByGeometry = false,
            resizeTextForBestFit = false,
            updateBounds = false,
            color = theme.textColor,
        };

        return new TextGenerator().GetPreferredHeight(text, settings);
    }

    static Sprite GetTailSprite(int width, int height, Color fill, Color border, float borderThickness)
    {
        string key = $"{width}|{height}|{fill}|{border}|{borderThickness:F2}";
        if (TailSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        float halfBase = (width - 1) * 0.5f;
        float apexX = halfBase;
        float apexY = 0f;
        float baseY = height - 1f;
        float borderPx = Mathf.Max(1f, borderThickness);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!IsInsideDownTriangle(x + 0.5f, y + 0.5f, 0f, baseY, width - 1f, baseY, apexX, apexY))
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float dist = DistanceToDownTriangleEdges(x + 0.5f, y + 0.5f, 0f, baseY, width - 1f, baseY, apexX, apexY);
                texture.SetPixel(x, y, dist <= borderPx ? border : fill);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 1f),
            100f);
        TailSpriteCache[key] = sprite;
        return sprite;
    }

    static bool IsInsideDownTriangle(float px, float py, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        float d1 = Sign(px, py, x1, y1, x2, y2);
        float d2 = Sign(px, py, x2, y2, x3, y3);
        float d3 = Sign(px, py, x3, y3, x1, y1);
        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    static float Sign(float px, float py, float ax, float ay, float bx, float by)
    {
        return (px - bx) * (ay - by) - (ax - bx) * (py - by);
    }

    static float DistanceToDownTriangleEdges(float px, float py, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        return Mathf.Min(
            DistancePointToSegment(px, py, x1, y1, x2, y2),
            Mathf.Min(
                DistancePointToSegment(px, py, x2, y2, x3, y3),
                DistancePointToSegment(px, py, x3, y3, x1, y1)));
    }

    static float DistancePointToSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float abx = bx - ax;
        float aby = by - ay;
        float apx = px - ax;
        float apy = py - ay;
        float abLenSq = abx * abx + aby * aby;
        if (abLenSq <= 0.0001f)
        {
            return Vector2.Distance(new Vector2(px, py), new Vector2(ax, ay));
        }

        float t = Mathf.Clamp01((apx * abx + apy * aby) / abLenSq);
        float cx = ax + abx * t;
        float cy = ay + aby * t;
        return Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
    }
}
