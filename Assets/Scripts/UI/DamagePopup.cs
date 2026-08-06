using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    private const float DefaultLifetime = 0.6f;
    private const float RiseDistance = 0.5f;
    private const int DefaultFontSize = 48;
    private const int MessageFontSize = 34;
    private const float DefaultCharacterSize = 0.2f;
    private static readonly Color DefaultColor = Color.white;

    private Canvas canvas;
    private Text text;
    private string sortingLayerName = "Default";
    private int sortingOrder;
    private float lifetime = DefaultLifetime;
    private float timer;
    private Vector3 startPosition;
    private Color startColor;

    public static DamagePopup Create(Vector3 position, int damageAmount, string layerName = null, int order = 0)
    {
        GameObject popupObj = new GameObject("DamagePopup");
        popupObj.transform.position = position;
        DamagePopup popup = popupObj.AddComponent<DamagePopup>();
        popup.Initialize(damageAmount.ToString(), DefaultColor, DefaultFontSize, layerName, order);
        return popup;
    }

    public static DamagePopup CreateMessage(Vector3 position, string message, Color color, string layerName = null, int order = 0)
    {
        GameObject popupObj = new GameObject("MessagePopup");
        popupObj.transform.position = position;
        DamagePopup popup = popupObj.AddComponent<DamagePopup>();
        popup.Initialize(message, color, MessageFontSize, layerName, order);
        return popup;
    }

    void Initialize(string label, Color color, int fontSize, string layerName, int order)
    {
        startPosition = transform.position;
        startColor = color;
        sortingLayerName = string.IsNullOrEmpty(layerName) ? "Default" : layerName;
        sortingOrder = order;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = sortingLayerName;
        canvas.sortingOrder = sortingOrder + 10;

        RectTransform canvasTransform = canvas.GetComponent<RectTransform>();
        canvasTransform.sizeDelta = new Vector2(2f, 1f);
        canvasTransform.localScale = Vector3.one * 0.01f;

        GameObject textObj = new GameObject("DamageText");
        textObj.transform.SetParent(canvas.transform, false);

        text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = startColor;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform textTransform = text.GetComponent<RectTransform>();
        textTransform.sizeDelta = new Vector2(200f, 100f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = lifetime <= 0f ? 1f : Mathf.Clamp01(timer / lifetime);

        transform.position = startPosition + Vector3.up * (RiseDistance * t);

        if (text != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(1f, 0f, t);
            text.color = color;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
