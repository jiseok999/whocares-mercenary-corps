using UnityEngine;

/// <summary>
/// 유닛 드래그 중 놓을 위치 미리보기 — 칸 하이라이트 + 링 마커.
/// </summary>
public static class UnitDragPlacementPreview
{
    public enum Kind
    {
        None,
        SameCell,
        Move,
        Swap,
        Merge,
        Blocked,
        Invalid
    }

    static BoardCell highlightedCell;
    static GameObject markerRoot;
    static SpriteRenderer markerFill;
    static SpriteRenderer markerRing;
    static LineRenderer linkLine;

    static readonly Color MoveColor = new Color(0.35f, 1f, 0.55f, 0.55f);
    static readonly Color SwapColor = new Color(0.45f, 0.88f, 1f, 0.58f);
    static readonly Color MergeColor = new Color(1f, 0.82f, 0.28f, 0.62f);
    static readonly Color BlockedColor = new Color(1f, 0.45f, 0.32f, 0.58f);
    static readonly Color InvalidColor = new Color(1f, 0.28f, 0.28f, 0.62f);
    static readonly Color SameCellColor = new Color(0.92f, 0.92f, 0.92f, 0.28f);

    public static void UpdatePreview(
        Kind kind,
        BoardCell targetCell,
        Vector3 pointerWorldPos,
        Vector3 originWorldPos,
        Vector3 targetWorldPos)
    {
        ClearCellHighlight();

        Color color = GetColor(kind);
        bool showMarker = kind != Kind.None;

        if (targetCell != null && kind != Kind.None)
        {
            highlightedCell = targetCell;
            highlightedCell.SetDragPreview(GetColor(kind), true);
        }

        EnsureMarker();
        if (!showMarker)
        {
            markerRoot.SetActive(false);
            if (linkLine != null) linkLine.enabled = false;
            return;
        }

        markerRoot.SetActive(true);
        Vector3 markerPos = kind == Kind.Invalid ? pointerWorldPos : targetWorldPos;
        markerPos.z = 0f;
        markerRoot.transform.position = markerPos;

        float pulse = 0.92f + Mathf.Sin(Time.unscaledTime * 8f) * 0.06f;
        markerFill.color = color;
        markerRing.color = new Color(
            Mathf.Min(1f, color.r + 0.12f),
            Mathf.Min(1f, color.g + 0.12f),
            Mathf.Min(1f, color.b + 0.12f),
            color.a * 0.95f);
        markerFill.transform.localScale = Vector3.one * (0.34f * pulse);
        markerRing.transform.localScale = Vector3.one * (0.52f * pulse);

        bool showLink = kind == Kind.Move || kind == Kind.Swap || kind == Kind.Merge;
        if (linkLine != null)
        {
            linkLine.enabled = showLink && kind != Kind.SameCell;
            if (linkLine.enabled)
            {
                linkLine.startColor = color;
                linkLine.endColor = new Color(color.r, color.g, color.b, color.a * 0.35f);
                linkLine.SetPosition(0, originWorldPos);
                linkLine.SetPosition(1, targetWorldPos);
            }
        }
    }

    public static void Hide()
    {
        ClearCellHighlight();
        if (markerRoot != null)
        {
            markerRoot.SetActive(false);
        }
        if (linkLine != null)
        {
            linkLine.enabled = false;
        }
    }

    static void ClearCellHighlight()
    {
        if (highlightedCell != null)
        {
            highlightedCell.SetDragPreview(Color.clear, false);
            highlightedCell = null;
        }
    }

    static Color GetColor(Kind kind)
    {
        switch (kind)
        {
            case Kind.Move: return MoveColor;
            case Kind.Swap: return SwapColor;
            case Kind.Merge: return MergeColor;
            case Kind.Blocked: return BlockedColor;
            case Kind.SameCell: return SameCellColor;
            default: return InvalidColor;
        }
    }

    static void EnsureMarker()
    {
        if (markerRoot != null) return;

        markerRoot = new GameObject("UnitDragPreviewMarker");

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(markerRoot.transform, false);
        markerFill = fillObj.AddComponent<SpriteRenderer>();
        markerFill.sprite = SkillEffectVisuals.GetPreviewDiscSprite();
        markerFill.sortingOrder = 120;

        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(markerRoot.transform, false);
        markerRing = ringObj.AddComponent<SpriteRenderer>();
        markerRing.sprite = SkillEffectVisuals.GetPreviewRingSprite();
        markerRing.sortingOrder = 121;

        GameObject lineObj = new GameObject("LinkLine");
        lineObj.transform.SetParent(markerRoot.transform, false);
        linkLine = lineObj.AddComponent<LineRenderer>();
        linkLine.useWorldSpace = true;
        linkLine.positionCount = 2;
        linkLine.startWidth = 0.06f;
        linkLine.endWidth = 0.02f;
        linkLine.material = new Material(Shader.Find("Sprites/Default"));
        linkLine.sortingOrder = 119;
        linkLine.enabled = false;

        markerRoot.SetActive(false);
    }
}
