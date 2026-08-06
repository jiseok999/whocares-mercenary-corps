using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 옵션 팝업에서 사용하는 슬라이딩 스위치(토글) 비주얼.
/// 노브 위치와 트랙 색을 토글 상태에 맞춰 부드럽게 보간합니다.
/// 게임이 일시정지(Time.timeScale = 0) 상태에서도 동작하도록 unscaled time을 사용합니다.
/// </summary>
public class PauseSwitchVisual : MonoBehaviour
{
    public Toggle toggle;
    public RectTransform knob;
    public Image track;
    public Color onColor = new Color(0.96f, 0.62f, 0.18f, 1f);
    public Color offColor = new Color(0.36f, 0.28f, 0.21f, 1f);
    public Color knobColor = new Color(1f, 0.98f, 0.92f, 1f);
    public float knobOnX;
    public float knobOffX;
    public float animSpeed = 12f;

    private Image knobImage;
    private float current;

    void Awake()
    {
        if (knob != null)
        {
            knobImage = knob.GetComponent<Image>();
        }
    }

    void OnEnable()
    {
        current = (toggle != null && toggle.isOn) ? 1f : 0f;
        Apply(current);
    }

    void Update()
    {
        if (toggle == null) return;
        float target = toggle.isOn ? 1f : 0f;
        current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * animSpeed);
        Apply(current);
    }

    void Apply(float k)
    {
        if (knob != null)
        {
            Vector2 p = knob.anchoredPosition;
            p.x = Mathf.Lerp(knobOffX, knobOnX, k);
            knob.anchoredPosition = p;
        }
        if (track != null)
        {
            track.color = Color.Lerp(offColor, onColor, k);
        }
        if (knobImage != null)
        {
            knobImage.color = knobColor;
        }
    }
}
