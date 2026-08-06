using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 타이틀 등에서 사용하는 게임 종료 확인 팝업 (웜톤 카드 UI).
/// </summary>
public class QuitConfirmDialogView : MonoBehaviour
{
    const float CardW = 560f;
    const float CardH = 280f;
    static readonly Color Accent = new Color(1f, 0.78f, 0.30f, 1f);

    GameObject rootPanel;
    Text titleText;
    Text messageText;
    Text confirmButtonText;
    Text cancelButtonText;

    public static QuitConfirmDialogView Create(Transform canvasParent, int sortingOrder = 350)
    {
        GameObject rootPanelObj = new GameObject("QuitConfirmDialog");
        rootPanelObj.transform.SetParent(canvasParent, false);

        Canvas panelCanvas = rootPanelObj.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = sortingOrder;
        rootPanelObj.AddComponent<GraphicRaycaster>();

        Image dim = rootPanelObj.AddComponent<Image>();
        dim.color = new Color(0.06f, 0.04f, 0.025f, 0.82f);
        RectTransform rootRect = rootPanelObj.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject shadowObj = new GameObject("CardShadow");
        shadowObj.transform.SetParent(rootPanelObj.transform, false);
        Image shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.type = Image.Type.Sliced;
        shadowImage.raycastTarget = false;
        shadowImage.sprite = PauseOptionsMenuView.GetRoundedSprite(
            "quitShadow", new Color(0f, 0f, 0f, 0.35f), 40, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(CardW + 36f, CardH + 36f);
        shadowRect.anchoredPosition = new Vector2(0f, -10f);

        GameObject cardObj = new GameObject("ConfirmCard");
        cardObj.transform.SetParent(rootPanelObj.transform, false);
        Image cardImage = cardObj.AddComponent<Image>();
        cardImage.type = Image.Type.Sliced;
        cardImage.sprite = PauseOptionsMenuView.GetRoundedSprite(
            "quitCard", new Color(0.145f, 0.108f, 0.078f, 0.99f), 30, new Color(1f, 0.85f, 0.55f, 0.12f), 2.5f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(CardW, CardH);
        cardRect.anchoredPosition = Vector2.zero;

        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(cardObj.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.type = Image.Type.Sliced;
        accentImage.raycastTarget = false;
        accentImage.sprite = PauseOptionsMenuView.GetRoundedSprite(
            "quitAccent", Accent, 4, new Color(0f, 0f, 0f, 0f), 0f);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 1f);
        accentRect.anchoredPosition = new Vector2(40f, -28f);
        accentRect.sizeDelta = new Vector2(56f, 6f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.text = GameLocalization.QuitConfirmTitle;
        title.font = UIFontProvider.Get();
        title.fontSize = 36;
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, 0.96f, 0.89f, 1f);
        title.alignment = TextAnchor.UpperLeft;
        title.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -24f);
        titleRect.sizeDelta = new Vector2(420f, 52f);

        CreateDivider(cardObj.transform, 88f);

        GameObject messageObj = new GameObject("Message");
        messageObj.transform.SetParent(cardObj.transform, false);
        Text message = messageObj.AddComponent<Text>();
        message.text = GameLocalization.QuitConfirmMessage;
        message.font = UIFontProvider.Get();
        message.fontSize = 24;
        message.color = new Color(0.93f, 0.86f, 0.74f, 1f);
        message.alignment = TextAnchor.MiddleCenter;
        message.horizontalOverflow = HorizontalWrapMode.Wrap;
        message.verticalOverflow = VerticalWrapMode.Overflow;
        message.raycastTarget = false;
        RectTransform messageRect = messageObj.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = new Vector2(0f, 18f);
        messageRect.sizeDelta = new Vector2(480f, 72f);

        QuitConfirmDialogView view = rootPanelObj.AddComponent<QuitConfirmDialogView>();
        view.rootPanel = rootPanelObj;
        view.titleText = title;
        view.messageText = message;

        view.cancelButtonText = CreateActionButton(
            cardObj.transform,
            "CancelButton",
            GameLocalization.QuitConfirmCancel,
            new Vector2(-148f, 36f),
            new Vector2(240f, 54f),
            new Color(0.36f, 0.27f, 0.20f, 1f),
            view.Close);
        view.confirmButtonText = CreateActionButton(
            cardObj.transform,
            "ConfirmButton",
            GameLocalization.QuitConfirmYes,
            new Vector2(148f, 36f),
            new Vector2(240f, 54f),
            new Color(0.95f, 0.60f, 0.16f, 1f),
            view.OnConfirm);

        CreateCloseButton(cardObj.transform, view.Close);

        GameLocalization.LanguageChanged += view.OnLanguageChanged;
        rootPanelObj.SetActive(false);
        return view;
    }

    void OnDestroy()
    {
        GameLocalization.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        RefreshLocalizedTexts();
    }

    public void Open()
    {
        RefreshLocalizedTexts();
        rootPanel.SetActive(true);
        rootPanel.transform.SetAsLastSibling();
    }

    public void Close()
    {
        rootPanel.SetActive(false);
    }

    void OnConfirm()
    {
        Close();
        RequestQuit();
    }

    public static void RequestQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void RefreshLocalizedTexts()
    {
        Font font = UIFontProvider.Get();
        if (titleText != null)
        {
            titleText.font = font;
            titleText.text = GameLocalization.QuitConfirmTitle;
        }

        if (messageText != null)
        {
            messageText.font = font;
            messageText.text = GameLocalization.QuitConfirmMessage;
        }

        if (cancelButtonText != null)
        {
            cancelButtonText.font = font;
            cancelButtonText.text = GameLocalization.QuitConfirmCancel;
        }

        if (confirmButtonText != null)
        {
            confirmButtonText.font = font;
            confirmButtonText.text = GameLocalization.QuitConfirmYes;
        }
    }

    static void CreateDivider(Transform parent, float topY)
    {
        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(parent, false);
        Image divider = dividerObj.AddComponent<Image>();
        divider.color = new Color(1f, 0.85f, 0.55f, 0.12f);
        divider.raycastTarget = false;
        RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(0f, 1f);
        dividerRect.pivot = new Vector2(0f, 1f);
        dividerRect.anchoredPosition = new Vector2(40f, -topY);
        dividerRect.sizeDelta = new Vector2(CardW - 80f, 1.5f);
    }

    static void CreateCloseButton(Transform parent, Action onClose)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent, false);
        Image closeImage = closeObj.AddComponent<Image>();
        closeImage.type = Image.Type.Sliced;
        closeImage.sprite = PauseOptionsMenuView.GetRoundedSprite(
            "quitCloseBg", new Color(1f, 0.85f, 0.55f, 0.09f), 18, new Color(1f, 0.85f, 0.55f, 0.16f), 1.5f);
        Button closeButton = closeObj.AddComponent<Button>();
        closeButton.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = closeButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.7f, 0.7f, 1f);
        cb.pressedColor = new Color(0.85f, 0.45f, 0.45f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.1f;
        closeButton.colors = cb;
        closeButton.onClick.AddListener(() => onClose?.Invoke());
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-22f, -22f);
        closeRect.sizeDelta = new Vector2(44f, 44f);

        for (int i = 0; i < 2; i++)
        {
            GameObject barObj = new GameObject(i == 0 ? "XBar1" : "XBar2");
            barObj.transform.SetParent(closeObj.transform, false);
            Image bar = barObj.AddComponent<Image>();
            bar.color = new Color(1f, 0.93f, 0.82f, 0.95f);
            bar.raycastTarget = false;
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(22f, 3.2f);
            barRect.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
        }
    }

    static Text CreateActionButton(
        Transform parent,
        string name,
        string buttonLabel,
        Vector2 anchoredPosition,
        Vector2 size,
        Color baseColor,
        Action onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.sprite = PauseOptionsMenuView.GetRoundedSprite(
            "quitBtn_" + name, Color.white, 14, new Color(0f, 0f, 0f, 0f), 0f);
        buttonImage.color = baseColor;
        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = buttonLabel;
        text.font = UIFontProvider.Get();
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.98f, 0.93f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return text;
    }
}
