using UnityEngine;

/// <summary>
/// 상점 용병 말풍선 비주얼. Resources/ShopMercenaryDialogueBubbleTheme 에셋으로 Inspector에서 편집 가능.
/// 에셋이 없으면 아래 기본값이 사용됩니다.
/// </summary>
[CreateAssetMenu(
    fileName = "ShopMercenaryDialogueBubbleTheme",
    menuName = "UI/Shop Mercenary Dialogue Bubble Theme")]
public class ShopMercenaryDialogueBubbleTheme : ScriptableObject
{
    [Header("말풍선 본체")]
    public Color fillColor = new Color(1f, 0.99f, 0.96f, 1f);
    public Color borderColor = new Color(0.12f, 0.11f, 0.14f, 1f);
    [Range(1f, 4f)] public float borderThickness = 2f;
    [Range(4, 24)] public int cornerRadius = 16;

    [Header("꼬리 (아래로 향함)")]
    [Range(10f, 32f)] public float tailWidth = 20f;
    [Range(8f, 24f)] public float tailHeight = 14f;
    [Range(0f, 8f)] public float tailOverlap = 2f;

    [Header("그림자")]
    public bool useShadow = true;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.2f);
    public Vector2 shadowOffset = new Vector2(2f, -3f);

    [Header("텍스트")]
    public Color textColor = new Color(0.1f, 0.1f, 0.12f, 1f);
    [Range(11, 20)] public int fontSize = 15;
    [Range(1f, 1.4f)] public float lineSpacing = 1.08f;

    [Header("크기 · 여백")]
    [Range(160f, 320f)] public float bubbleWidth = 224f;
    [Range(48f, 80f)] public float minBodyHeight = 52f;
    [Range(80f, 160f)] public float maxBodyHeight = 112f;
    [Range(8f, 24f)] public float paddingHorizontal = 14f;
    [Range(6f, 20f)] public float paddingTop = 12f;
    [Range(6f, 20f)] public float paddingBottom = 10f;
    [Range(0f, 24f)] public float offsetAboveSlot = 6f;

    static ShopMercenaryDialogueBubbleTheme cached;
    static ShopMercenaryDialogueBubbleTheme runtimeFallback;

    public static ShopMercenaryDialogueBubbleTheme Active
    {
        get
        {
            if (cached != null) return cached;

            cached = Resources.Load<ShopMercenaryDialogueBubbleTheme>("ShopMercenaryDialogueBubbleTheme");
            if (cached != null) return cached;

            if (runtimeFallback == null)
            {
                runtimeFallback = CreateInstance<ShopMercenaryDialogueBubbleTheme>();
            }

            return runtimeFallback;
        }
    }

#if UNITY_EDITOR
    public static void ResetCacheForEditor()
    {
        cached = null;
    }
#endif
}
