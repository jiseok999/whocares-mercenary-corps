using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 설정 패널용 언어 선택 드롭다운(콤보박스).
/// </summary>
public class LanguageDropdownWidget : MonoBehaviour
{
    static readonly GameLanguage[] Languages =
    {
        GameLanguage.Korean,
        GameLanguage.English,
        GameLanguage.Japanese,
        GameLanguage.TraditionalChinese,
        GameLanguage.SimplifiedChinese,
        GameLanguage.Thai,
        GameLanguage.Spanish,
        GameLanguage.Portuguese,
        GameLanguage.French,
        GameLanguage.Italian,
        GameLanguage.German
    };

    Text captionText;
    GameObject listPanel;
    RectTransform listRect;
    Image arrowImage;
    RectTransform rootRect;
    bool isOpen;
    bool ignoreOutsideClickThisFrame;

    public static LanguageDropdownWidget Create(Transform parent, float topY, float width = 520f, float height = 44f)
    {
        GameObject root = new GameObject("LanguageDropdown");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(40f, -topY);
        rect.sizeDelta = new Vector2(width, height);

        LanguageDropdownWidget widget = root.AddComponent<LanguageDropdownWidget>();
        widget.rootRect = rect;
        widget.Build(width, height);
        widget.SetLanguage(GameLocalization.CurrentLanguage, notify: false);
        return widget;
    }

    void Build(float width, float height)
    {
        GameObject buttonObj = new GameObject("Toggle");
        buttonObj.transform.SetParent(transform, false);
        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.type = Image.Type.Sliced;
        buttonBg.sprite = PauseOptionsMenuView.GetRoundedSprite(
            "langDropdown",
            new Color(0.14f, 0.11f, 0.08f, 0.96f),
            14,
            new Color(1f, 0.85f, 0.55f, 0.28f),
            1.8f);
        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.05f, 1f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.88f, 1f);
        button.colors = colors;
        button.onClick.AddListener(ToggleList);
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        GameObject captionObj = new GameObject("Caption");
        captionObj.transform.SetParent(buttonObj.transform, false);
        captionText = captionObj.AddComponent<Text>();
        captionText.font = UIFontProvider.Get();
        captionText.fontSize = 20;
        captionText.fontStyle = FontStyle.Bold;
        captionText.color = new Color(0.93f, 0.86f, 0.74f, 1f);
        captionText.alignment = TextAnchor.MiddleLeft;
        captionText.raycastTarget = false;
        RectTransform captionRect = captionObj.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(16f, 0f);
        captionRect.offsetMax = new Vector2(-36f, 0f);

        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(buttonObj.transform, false);
        arrowImage = arrowObj.AddComponent<Image>();
        arrowImage.color = new Color(1f, 0.82f, 0.45f, 0.95f);
        arrowImage.raycastTarget = false;
        RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-14f, 0f);
        arrowRect.sizeDelta = new Vector2(14f, 8f);

        listPanel = new GameObject("ListPanel");
        listPanel.transform.SetParent(transform, false);
        Image listBg = listPanel.AddComponent<Image>();
        listBg.type = Image.Type.Sliced;
        listBg.sprite = PauseOptionsMenuView.GetRoundedSprite(
            "langDropdownList",
            new Color(0.10f, 0.08f, 0.06f, 0.98f),
            12,
            new Color(1f, 0.85f, 0.55f, 0.22f),
            1.5f);
        listRect = listPanel.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.anchoredPosition = new Vector2(0f, -height - 4f);
        listRect.sizeDelta = new Vector2(0f, Languages.Length * 40f + 8f);

        for (int i = 0; i < Languages.Length; i++)
        {
            CreateListItem(listPanel.transform, Languages[i], i);
        }

        listPanel.SetActive(false);
    }

    void CreateListItem(Transform parent, GameLanguage language, int index)
    {
        GameObject itemObj = new GameObject($"Item_{language}");
        itemObj.transform.SetParent(parent, false);
        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(1f, 1f, 1f, 0.02f);
        Button itemButton = itemObj.AddComponent<Button>();
        itemButton.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = itemButton.colors;
        cb.normalColor = new Color(1f, 1f, 1f, 0.02f);
        cb.highlightedColor = new Color(1f, 0.82f, 0.45f, 0.18f);
        cb.pressedColor = new Color(1f, 0.72f, 0.28f, 0.28f);
        itemButton.colors = cb;
        GameLanguage captured = language;
        itemButton.onClick.AddListener(() =>
        {
            ignoreOutsideClickThisFrame = true;
            SetLanguage(captured, notify: true);
            CloseList();
        });

        RectTransform itemRect = itemObj.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.anchoredPosition = new Vector2(0f, -4f - index * 40f);
        itemRect.sizeDelta = new Vector2(-8f, 36f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(itemObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = GameLocalization.GetLanguageDisplayName(language);
        text.font = UIFontProvider.Get();
        text.fontSize = 18;
        text.color = new Color(0.93f, 0.86f, 0.74f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);
    }

    public void SetLanguage(GameLanguage language, bool notify)
    {
        if (captionText != null)
        {
            captionText.font = UIFontProvider.Get();
            captionText.text = GameLocalization.GetLanguageDisplayName(language);
        }

        if (notify)
        {
            GameLocalization.SetLanguage(language);
        }
    }

    public void RefreshCaption()
    {
        SetLanguage(GameLocalization.CurrentLanguage, notify: false);
    }

    void ToggleList()
    {
        if (isOpen) CloseList();
        else OpenList();
    }

    void OpenList()
    {
        isOpen = true;
        ignoreOutsideClickThisFrame = true;
        if (listPanel != null)
        {
            listPanel.SetActive(true);
            listPanel.transform.SetAsLastSibling();
        }
        transform.SetAsLastSibling();
        if (arrowImage != null) arrowImage.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
    }

    void CloseList()
    {
        isOpen = false;
        if (listPanel != null) listPanel.SetActive(false);
        if (arrowImage != null) arrowImage.transform.localRotation = Quaternion.identity;
    }

    void LateUpdate()
    {
        if (ignoreOutsideClickThisFrame)
        {
            ignoreOutsideClickThisFrame = false;
            return;
        }

        if (!isOpen) return;
        if (!TryGetPointerDown(out Vector2 screenPos)) return;

        if (!IsPointerOverWidget(screenPos))
        {
            CloseList();
        }
    }

    bool IsPointerOverWidget(Vector2 screenPos)
    {
        if (rootRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(rootRect, screenPos, null))
        {
            return true;
        }

        if (listPanel != null && listPanel.activeSelf && listRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(listRect, screenPos, null))
        {
            return true;
        }

        return false;
    }

    static bool TryGetPointerDown(out Vector2 screenPos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        screenPos = Vector2.zero;
        return false;
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }

        screenPos = Vector2.zero;
        return false;
#endif
    }
}
