using UnityEngine;

/// <summary>
/// 유닛 상단의 골드 아이콘 (클릭 시 골드 획득)
/// </summary>
public class GoldIcon : MonoBehaviour
{
    private Character owner;
    private bool collected;
    private int goldAmount = 1;
    private bool autoCollect;
    private float autoCollectDelay;
    private float spawnTime;
    private BoxCollider2D boxCollider;
    private Transform visualTransform;
    private Vector3 baseVisualLocalPosition;
    private const float BobAmplitude = 0.06f;
    private const float BobSpeed = 3f;

    public void Initialize(Character character, Transform visual, BoxCollider2D pickCollider, Vector3 baseVisualLocalPos)
    {
        owner = character;
        visualTransform = visual;
        boxCollider = pickCollider;
        baseVisualLocalPosition = baseVisualLocalPos;
        spawnTime = Time.time;
    }

    public void SetGoldAmount(int amount)
    {
        goldAmount = Mathf.Max(1, amount);
    }

    public void EnableAutoCollect(float delaySec)
    {
        autoCollect = true;
        autoCollectDelay = Mathf.Max(0.05f, delaySec);
    }

    void Awake()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        if (visualTransform == null)
        {
            Transform visual = transform.Find("Visual");
            if (visual != null)
            {
                visualTransform = visual;
                baseVisualLocalPosition = visual.localPosition;
                if (boxCollider == null)
                {
                    boxCollider = visual.GetComponent<BoxCollider2D>();
                }
            }
        }
    }

    void OnDestroy()
    {
        if (!collected && owner != null)
        {
            owner.NotifyGoldIconRemoved();
        }
    }

    void OnMouseDown()
    {
        TryCollect();
    }

    void Update()
    {
        if (autoCollect && !collected && Time.time >= spawnTime + autoCollectDelay)
        {
            TryCollect();
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            TryHandleClickFromScreen(mousePos);
        }

        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            TryHandleClickFromScreen(touchPos);
        }
#endif
    }

    void LateUpdate()
    {
        if (visualTransform == null)
        {
            return;
        }

        float offset = Mathf.Sin(Time.time * BobSpeed) * BobAmplitude;
        Vector3 bobPos = baseVisualLocalPosition + new Vector3(0f, offset, 0f);
        if (Camera.main != null && Camera.main.orthographic)
        {
            float pixelsPerUnit = Screen.height / (2f * Camera.main.orthographicSize);
            if (pixelsPerUnit > 0.001f)
            {
                float snap = 1f / pixelsPerUnit;
                bobPos.y = Mathf.Round(bobPos.y / snap) * snap;
            }
        }

        visualTransform.localPosition = bobPos;
    }

    /// <summary>
    /// 월드 좌표 클릭이 골드 아이콘에 닿으면 수집하고 true를 반환합니다.
    /// </summary>
    public static bool TryHandleClick(Vector3 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            GoldIcon icon = hits[i].GetComponent<GoldIcon>();
            if (icon == null)
            {
                icon = hits[i].GetComponentInParent<GoldIcon>();
            }

            if (icon != null && !icon.collected)
            {
                icon.TryCollect();
                return true;
            }
        }

        return false;
    }

    public static Vector3 ScreenToWorldPoint2D(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Vector3.zero;
        }

        Vector3 pos = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(pos);
        world.z = 0f;
        return world;
    }

    public static bool TryHandleClickFromScreen(Vector2 screenPos)
    {
        return TryHandleClick(ScreenToWorldPoint2D(screenPos));
    }

    void TryCollect()
    {
        if (collected)
        {
            return;
        }

        collected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(goldAmount);
        }

        Character collectOwner = owner;
        owner = null;
        if (collectOwner != null)
        {
            collectOwner.OnGoldCollected();
        }

        Destroy(gameObject);
    }
}
