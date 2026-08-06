using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 HUD 캔버스 정렬 순서 — 배치 UI는 토스트·팝업보다 뒤에 그립니다.
/// </summary>
public static class GameUiSortingLayers
{
    public const int PlacementUi = 8;
    public const int GuideOverlay = 72;

    const string PlacementLayerName = "PlacementUiLayer";
    const string GuideOverlayLayerName = "GuideOverlayLayer";

    public static Transform EnsurePlacementLayer(Transform canvasRoot)
    {
        return EnsureLayer(canvasRoot, PlacementLayerName, PlacementUi);
    }

    public static Transform EnsureGuideOverlayLayer(Transform canvasRoot)
    {
        return EnsureLayer(canvasRoot, GuideOverlayLayerName, GuideOverlay);
    }

    static Transform EnsureLayer(Transform canvasRoot, string layerName, int sortingOrder)
    {
        if (canvasRoot == null) return null;

        Transform existing = canvasRoot.Find(layerName);
        if (existing != null)
        {
            ApplySorting(existing.gameObject, sortingOrder);
            return existing;
        }

        GameObject layerObj = new GameObject(layerName);
        layerObj.transform.SetParent(canvasRoot, false);
        RectTransform rect = layerObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        ApplySorting(layerObj, sortingOrder, graphicRaycaster: true);
        layerObj.transform.SetAsFirstSibling();
        return layerObj.transform;
    }

    public static void ApplySorting(GameObject go, int sortingOrder, bool graphicRaycaster = false)
    {
        if (go == null) return;

        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = go.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (graphicRaycaster && go.GetComponent<GraphicRaycaster>() == null)
        {
            go.AddComponent<GraphicRaycaster>();
        }
    }
}
