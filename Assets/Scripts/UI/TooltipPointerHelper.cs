using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 툴팁 표시/숨김에 공통으로 쓰는 포인터·UI 판별 헬퍼
/// </summary>
public static class TooltipPointerHelper
{
    static readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>(16);

    public static Vector2 GetScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
        return Input.mousePosition;
    }

    public static bool IsPointerOverUi()
    {
        if (EventSystem.current == null) return false;

        raycastBuffer.Clear();
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = GetScreenPosition()
        };
        EventSystem.current.RaycastAll(eventData, raycastBuffer);
        for (int i = 0; i < raycastBuffer.Count; i++)
        {
            Graphic graphic = raycastBuffer[i].gameObject.GetComponent<Graphic>();
            if (graphic != null && graphic.raycastTarget && graphic.enabled)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsScreenPointInsideRect(RectTransform rect, Vector2 screenPos)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, uiCam);
    }

    /// <summary>
    /// 포인터 아래 최상위 UI가 target 계층(또는 tooltipRoot)에 속하는지 확인합니다.
    /// </summary>
    public static bool IsPointerHittingTransform(Transform target, Transform tooltipRoot = null)
    {
        if (target == null) return false;
        if (EventSystem.current == null) return true;

        raycastBuffer.Clear();
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = GetScreenPosition()
        };
        EventSystem.current.RaycastAll(eventData, raycastBuffer);
        if (raycastBuffer.Count <= 0) return false;

        for (int i = 0; i < raycastBuffer.Count; i++)
        {
            Transform hit = raycastBuffer[i].gameObject.transform;
            if (tooltipRoot != null && hit.IsChildOf(tooltipRoot)) continue;
            if (hit.IsChildOf(target) || hit == target) return true;
            return false;
        }

        return false;
    }
}
