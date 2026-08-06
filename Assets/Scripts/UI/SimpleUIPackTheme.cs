using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D Simple UI Pack 스타일을 런타임 생성 UI에 적용합니다.
/// </summary>
public static class SimpleUIPackTheme
{
    struct ButtonStyle
    {
        public Sprite normal;
        public Sprite highlighted;
        public Sprite pressed;
        public Sprite disabled;
        public bool IsValid => normal != null;
    }

    static bool stylesLoaded;
    static ButtonStyle primaryButton;
    static ButtonStyle smallButton;
    static ButtonStyle lobbyBlackHorizontalButton;
    static Sprite panelSprite;

    const string PrimaryButtonPrefabPath =
        "Assets/OArielG/2DSimpleUIPack/Examples/Prefabs/VerticalButtons/VerticalBlueButton.prefab";
    const string SmallButtonPrefabPath =
        "Assets/OArielG/2DSimpleUIPack/Examples/Prefabs/SmallButtons/SmallBlackPlayButton.prefab";
    const string PanelSpritePath =
        "Assets/OArielG/2DSimpleUIPack/Sprites/ui-panels.png";
    const string PopupSpriteResourcePath = "popup";
    const string HorizontalButtonsSheetPath =
        "Assets/OArielG/2DSimpleUIPack/Examples/Graphics/ui-large-buttons-horizontal.png";

    const string LobbyBlackNormalName = "ui-large-buttons-horizontal_40";
    const string LobbyBlackPressedName = "ui-large-buttons-horizontal_41";

    public static void ApplyPrimaryButton(Button button)
    {
        EnsureLoaded();
        ApplyButtonStyle(button, primaryButton);
    }

    public static void ApplySmallButton(Button button)
    {
        EnsureLoaded();
        ApplyButtonStyle(button, smallButton);
    }

    public static void ApplyPanel(Image image)
    {
        EnsureLoaded();
        if (image == null || panelSprite == null) return;
        image.sprite = panelSprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
    }

    public static void ApplyPopupBackground(Image image)
    {
        Sprite popupSprite = Resources.Load<Sprite>(PopupSpriteResourcePath);
        if (image == null || popupSprite == null) return;
        image.sprite = popupSprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
    }

    public static void ApplyLobbyBlackHorizontalButton(Button button)
    {
        EnsureLoaded();
        ApplyButtonStyle(button, lobbyBlackHorizontalButton);
    }

    static void ApplyButtonStyle(Button button, ButtonStyle style)
    {
        if (button == null || !style.IsValid) return;

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }
        if (image == null) return;

        image.sprite = style.normal;
        image.color = Color.white;
        button.transition = Selectable.Transition.SpriteSwap;

        SpriteState spriteState = button.spriteState;
        spriteState.highlightedSprite = style.highlighted;
        spriteState.pressedSprite = style.pressed;
        spriteState.selectedSprite = style.highlighted;
        spriteState.disabledSprite = style.disabled;
        button.spriteState = spriteState;
        button.targetGraphic = image;
    }

    static void EnsureLoaded()
    {
        if (stylesLoaded) return;
        stylesLoaded = true;

        primaryButton = LoadButtonStyle(PrimaryButtonPrefabPath);
        smallButton = LoadButtonStyle(SmallButtonPrefabPath);
        panelSprite = LoadSprite(PanelSpritePath);
        lobbyBlackHorizontalButton = LoadLobbyBlackHorizontalStyle();
    }

    static ButtonStyle LoadButtonStyle(string prefabPath)
    {
        GameObject prefab = LoadAsset<GameObject>(prefabPath);
        if (prefab == null) return default;

        Button button = prefab.GetComponentInChildren<Button>(true);
        Image image = button != null ? button.targetGraphic as Image : prefab.GetComponentInChildren<Image>(true);
        if (image == null) return default;

        SpriteState state = button != null ? button.spriteState : default;
        return new ButtonStyle
        {
            normal = image.sprite,
            highlighted = state.highlightedSprite != null ? state.highlightedSprite : image.sprite,
            pressed = state.pressedSprite != null ? state.pressedSprite : image.sprite,
            disabled = state.disabledSprite != null ? state.disabledSprite : image.sprite
        };
    }

    static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = LoadAsset<Sprite>(assetPath);
        if (sprite != null) return sprite;

        // Resources에 동일 경로로 복사된 경우를 대비한 폴백
        string resourcePath = assetPath
            .Replace("Assets/Resources/", string.Empty)
            .Replace("Assets/", string.Empty)
            .Replace(".png", string.Empty);
        return Resources.Load<Sprite>(resourcePath);
    }

    static ButtonStyle LoadLobbyBlackHorizontalStyle()
    {
        Sprite normal = LoadSpriteByName(HorizontalButtonsSheetPath, LobbyBlackNormalName);
        Sprite pressed = LoadSpriteByName(HorizontalButtonsSheetPath, LobbyBlackPressedName);
        if (normal == null)
        {
            return default;
        }
        return new ButtonStyle
        {
            normal = normal,
            highlighted = pressed != null ? pressed : normal,
            pressed = pressed != null ? pressed : normal,
            disabled = normal
        };
    }

    static Sprite LoadSpriteByName(string assetPath, string spriteName)
    {
#if UNITY_EDITOR
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }
#endif
        return null;
    }

    static T LoadAsset<T>(string assetPath) where T : Object
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
        return null;
#endif
    }
}

