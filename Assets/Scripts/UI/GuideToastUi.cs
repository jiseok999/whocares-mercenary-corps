using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 내 안내·알림 토스트의 공통 웜톤 UI를 생성·갱신합니다.
/// </summary>
public static class GuideToastUi
{
    public enum Style
    {
        Guide,
        Info,
        Warning,
    }

    public const float RoundHudTopOffset = 8f;
    public const float RoundHudHeight = 200f;
    public const float BelowRoundHudGap = 12f;

    public static float GetAnchoredYBelowRoundHud(RectTransform roundHudRect)
    {
        if (roundHudRect == null)
        {
            return -(RoundHudTopOffset + RoundHudHeight + BelowRoundHudGap);
        }

        return roundHudRect.anchoredPosition.y - roundHudRect.sizeDelta.y - BelowRoundHudGap;
    }

    public sealed class Handle
    {
        internal GameObject root;
        internal CanvasGroup canvasGroup;
        internal Image shellImage;
        internal Image accentImage;
        internal Image badgeImage;
        internal Text badgeText;
        internal Text titleText;
        internal Text bodyText;
        internal RectTransform rootRect;

        public GameObject Root => root;
        public bool IsActive => root != null && root.activeSelf;

        public float Alpha => canvasGroup != null ? canvasGroup.alpha : 1f;

        public void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        public void ConfigureBelowRoundHud(Vector2? sizeOverride = null, RectTransform roundHudAnchor = null)
        {
            if (rootRect == null) return;

            if (roundHudAnchor != null && rootRect.parent != roundHudAnchor)
            {
                rootRect.SetParent(roundHudAnchor, false);
            }

            if (roundHudAnchor != null)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0f);
                rootRect.anchorMax = new Vector2(0.5f, 0f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.anchoredPosition = new Vector2(0f, -BelowRoundHudGap);
            }
            else
            {
                rootRect.anchorMin = new Vector2(0.5f, 1f);
                rootRect.anchorMax = new Vector2(0.5f, 1f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.anchoredPosition = new Vector2(0f, GetAnchoredYBelowRoundHud(null));
            }

            rootRect.sizeDelta = sizeOverride ?? new Vector2(640f, 96f);
        }

        public void SetStyle(Style style)
        {
            if (root == null) return;

            StylePalette palette = StylePalette.For(style);
            shellImage.sprite = WarmRoundedSprite.Get(
                palette.ShellFill,
                14,
                palette.ShellBorder,
                1.5f);
            shellImage.type = Image.Type.Sliced;
            accentImage.color = palette.Accent;
            badgeImage.sprite = WarmRoundedSprite.Get(
                palette.BadgeFill,
                8,
                palette.BadgeBorder,
                1f);
            badgeImage.type = Image.Type.Sliced;
            badgeText.text = palette.BadgeLabel;
            badgeText.color = palette.BadgeText;
            titleText.color = palette.Title;
            bodyText.color = palette.Body;
        }

        public void SetContent(string title, string body, bool showBadge = true)
        {
            if (root == null) return;

            bool hasTitle = !string.IsNullOrEmpty(title);
            bool hasBody = !string.IsNullOrEmpty(body);

            titleText.gameObject.SetActive(hasTitle);
            bodyText.gameObject.SetActive(hasBody);
            badgeImage.gameObject.SetActive(showBadge && (hasTitle || hasBody));

            if (hasTitle)
            {
                titleText.text = title;
            }

            if (hasBody)
            {
                bodyText.text = body;
            }

            bool showBadgeRow = showBadge && (hasTitle || hasBody);
            float headerTop = 14f;
            float headerHeight = 22f;
            float titleLeft = showBadgeRow ? 78f : 18f;

            if (showBadgeRow)
            {
                badgeImage.gameObject.SetActive(true);
            }

            if (hasTitle && hasBody)
            {
                titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
                titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
                titleText.rectTransform.pivot = new Vector2(0f, 1f);
                titleText.rectTransform.anchoredPosition = new Vector2(titleLeft, -headerTop);
                titleText.rectTransform.sizeDelta = new Vector2(-(titleLeft + 18f), headerHeight);
                titleText.alignment = TextAnchor.MiddleLeft;

                RectTransform bodyRect = bodyText.rectTransform;
                bodyRect.anchorMin = new Vector2(0f, 0f);
                bodyRect.anchorMax = new Vector2(1f, 1f);
                bodyRect.offsetMin = new Vector2(18f, 16f);
                bodyRect.offsetMax = new Vector2(-18f, -(headerTop + headerHeight + 10f));
            }
            else if (hasBody)
            {
                titleText.alignment = TextAnchor.UpperLeft;
                RectTransform bodyRect = bodyText.rectTransform;
                bodyRect.anchorMin = Vector2.zero;
                bodyRect.anchorMax = Vector2.one;
                bodyRect.offsetMin = new Vector2(18f, 16f);
                bodyRect.offsetMax = new Vector2(-18f, -(showBadgeRow ? 42f : 18f));
            }
            else if (hasTitle)
            {
                titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
                titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
                titleText.rectTransform.offsetMin = new Vector2(titleLeft, 16f);
                titleText.rectTransform.offsetMax = new Vector2(-18f, showBadgeRow ? -42f : -18f);
                titleText.alignment = TextAnchor.MiddleLeft;
            }
        }

        public void Show()
        {
            if (root == null) return;
            SetAlpha(1f);
            root.SetActive(true);
            GameUiSortingLayers.ApplySorting(root, GameUiSortingLayers.GuideOverlay);
            root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (root == null) return;
            root.SetActive(false);
            SetAlpha(1f);
        }
    }

    struct StylePalette
    {
        public Color ShellFill;
        public Color ShellBorder;
        public Color Accent;
        public Color BadgeFill;
        public Color BadgeBorder;
        public Color BadgeText;
        public Color Title;
        public Color Body;
        public string BadgeLabel;

        public static StylePalette For(Style style)
        {
            switch (style)
            {
                case Style.Info:
                    return new StylePalette
                    {
                        ShellFill = new Color(0.13f, 0.10f, 0.07f, 0.96f),
                        ShellBorder = new Color(1f, 0.78f, 0.24f, 0.55f),
                        Accent = new Color(1f, 0.82f, 0.22f, 1f),
                        BadgeFill = new Color(0.58f, 0.38f, 0.08f, 1f),
                        BadgeBorder = new Color(1f, 0.86f, 0.42f, 0.65f),
                        BadgeText = new Color(1f, 0.97f, 0.86f, 1f),
                        Title = new Color(1f, 0.93f, 0.68f, 1f),
                        Body = new Color(0.98f, 0.94f, 0.84f, 1f),
                        BadgeLabel = "전투",
                    };
                case Style.Warning:
                    return new StylePalette
                    {
                        ShellFill = new Color(0.16f, 0.08f, 0.07f, 0.96f),
                        ShellBorder = new Color(1f, 0.45f, 0.32f, 0.55f),
                        Accent = new Color(0.95f, 0.32f, 0.24f, 1f),
                        BadgeFill = new Color(0.62f, 0.14f, 0.12f, 1f),
                        BadgeBorder = new Color(1f, 0.55f, 0.42f, 0.65f),
                        BadgeText = new Color(1f, 0.92f, 0.88f, 1f),
                        Title = new Color(1f, 0.72f, 0.62f, 1f),
                        Body = new Color(0.98f, 0.90f, 0.88f, 1f),
                        BadgeLabel = "주의",
                    };
                default:
                    return new StylePalette
                    {
                        ShellFill = new Color(0.145f, 0.108f, 0.078f, 0.94f),
                        ShellBorder = new Color(1f, 0.82f, 0.45f, 0.55f),
                        Accent = new Color(0.96f, 0.62f, 0.18f, 1f),
                        BadgeFill = new Color(0.68f, 0.44f, 0.10f, 1f),
                        BadgeBorder = new Color(1f, 0.86f, 0.42f, 0.65f),
                        BadgeText = new Color(1f, 0.97f, 0.90f, 1f),
                        Title = new Color(1f, 0.79f, 0.30f, 1f),
                        Body = new Color(0.95f, 0.88f, 0.76f, 1f),
                        BadgeLabel = "안내",
                    };
            }
        }
    }

    public static Handle Create(Transform parent, string objectName = "GuideToast")
    {
        var handle = new Handle();

        handle.root = new GameObject(objectName);
        handle.root.transform.SetParent(parent, false);

        handle.canvasGroup = handle.root.AddComponent<CanvasGroup>();
        handle.canvasGroup.alpha = 1f;
        handle.canvasGroup.blocksRaycasts = false;
        handle.canvasGroup.interactable = false;

        handle.shellImage = handle.root.AddComponent<Image>();
        handle.shellImage.raycastTarget = false;

        handle.rootRect = handle.root.GetComponent<RectTransform>();

        GameObject accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(handle.root.transform, false);
        handle.accentImage = accentObj.AddComponent<Image>();
        handle.accentImage.raycastTarget = false;
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 4f);

        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(handle.root.transform, false);
        handle.badgeImage = badgeObj.AddComponent<Image>();
        handle.badgeImage.raycastTarget = false;
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(16f, -14f);
        badgeRect.sizeDelta = new Vector2(54f, 22f);

        GameObject badgeTextObj = new GameObject("BadgeText");
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        handle.badgeText = badgeTextObj.AddComponent<Text>();
        handle.badgeText.font = UIFontProvider.Get();
        handle.badgeText.fontSize = 13;
        handle.badgeText.fontStyle = FontStyle.Bold;
        handle.badgeText.alignment = TextAnchor.MiddleCenter;
        handle.badgeText.raycastTarget = false;
        RectTransform badgeTextRect = badgeTextObj.GetComponent<RectTransform>();
        badgeTextRect.anchorMin = Vector2.zero;
        badgeTextRect.anchorMax = Vector2.one;
        badgeTextRect.offsetMin = Vector2.zero;
        badgeTextRect.offsetMax = Vector2.zero;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(handle.root.transform, false);
        handle.titleText = titleObj.AddComponent<Text>();
        handle.titleText.font = UIFontProvider.Get();
        handle.titleText.fontSize = 20;
        handle.titleText.fontStyle = FontStyle.Bold;
        handle.titleText.alignment = TextAnchor.UpperLeft;
        handle.titleText.raycastTarget = false;
        handle.titleText.supportRichText = true;

        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(handle.root.transform, false);
        handle.bodyText = bodyObj.AddComponent<Text>();
        handle.bodyText.font = UIFontProvider.Get();
        handle.bodyText.fontSize = 18;
        handle.bodyText.alignment = TextAnchor.UpperLeft;
        handle.bodyText.raycastTarget = false;
        handle.bodyText.supportRichText = true;
        handle.bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        handle.bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        handle.SetStyle(Style.Guide);
        handle.root.SetActive(false);
        return handle;
    }
}
