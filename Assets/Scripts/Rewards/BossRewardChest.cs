using UnityEngine;

/// <summary>
/// 보스 처치 보상 상자 (클릭 시 보상 선택지 표시)
/// </summary>
public class BossRewardChest : MonoBehaviour
{
    private GameManager owner;
    private BoxCollider2D boxCollider;

    public void SetOwner(GameManager gameManager)
    {
        owner = gameManager;
    }

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void OnMouseDown()
    {
        TryOpen();
    }

    void Update()
    {
        // Input System 마우스/터치 클릭 처리
        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            TryOpenByScreenPoint(mousePos);
        }

        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            TryOpenByScreenPoint(touchPos);
        }
    }

    void TryOpenByScreenPoint(Vector3 screenPos)
    {
        if (boxCollider == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
        Vector2 hitPoint = new Vector2(worldPos.x, worldPos.y);
        Collider2D hit = Physics2D.OverlapPoint(hitPoint);
        if (hit == boxCollider)
        {
            TryOpen();
        }
    }

    void TryOpen()
    {
        if (owner != null)
        {
            owner.TryOpenBossRewardFromChest();
        }
        Destroy(gameObject);
    }
}
