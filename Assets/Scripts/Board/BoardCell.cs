using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 보드의 개별 칸을 나타내는 클래스
/// </summary>
public class BoardCell : MonoBehaviour
{
    public bool isBoardCell = true; // true면 메인보드, false면 대기칸
    public bool isOccupied = false;
    public Character currentCharacter = null;
    SpriteRenderer buffHighlightRenderer;
    SpriteRenderer dragPreviewRenderer;
    readonly List<SpriteRenderer> buffIconRenderers = new List<SpriteRenderer>(8);
    const float BuffHighlightScale = 0.8f; // 칸보다 살짝 작게(여백) 표시
    const float DragPreviewScale = 0.88f;
    
    /// <summary>
    /// 칸에 캐릭터를 배치합니다
    /// </summary>
    public bool PlaceCharacter(Character character)
    {
        if (isOccupied) return false;
        
        currentCharacter = character;
        isOccupied = true;
        character.SetCell(this);

        if (isBoardCell && character.unitNumber >= 1)
        {
            RunEncounterTracker.RegisterBoardPlacement(character.unitNumber);
        }

        NotifyPlacementChanged();
        
        return true;
    }
    
    /// <summary>
    /// 칸에서 캐릭터를 제거합니다
    /// </summary>
    public void RemoveCharacter()
    {
        if (currentCharacter != null)
        {
            currentCharacter.SetCell(null);
            currentCharacter = null;
            isOccupied = false;
        }
        NotifyPlacementChanged();
    }
    
    /// <summary>
    /// 캐릭터를 배치할 수 있는지 확인합니다
    /// </summary>
    public bool CanPlaceCharacter()
    {
        return !isOccupied;
    }

    void NotifyPlacementChanged()
    {
        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.UpdatePlacementCountUI();
        }
    }

    public void SetBuffHighlight(Color color, bool enabled)
    {
        if (!enabled)
        {
            if (buffHighlightRenderer != null)
            {
                buffHighlightRenderer.enabled = false;
            }
            HideAllBuffIcons();
            return;
        }

        EnsureBuffHighlightRenderer();
        if (buffHighlightRenderer == null) return;

        buffHighlightRenderer.color = color;
        buffHighlightRenderer.enabled = true;
        buffHighlightRenderer.transform.localScale = Vector3.one * (GetCellSize() * BuffHighlightScale);
    }

    public void SetDragPreview(Color color, bool enabled)
    {
        if (!enabled)
        {
            if (dragPreviewRenderer != null)
            {
                dragPreviewRenderer.enabled = false;
            }
            return;
        }

        EnsureDragPreviewRenderer();
        if (dragPreviewRenderer == null) return;

        dragPreviewRenderer.color = color;
        dragPreviewRenderer.enabled = true;
        dragPreviewRenderer.transform.localScale = Vector3.one * (GetCellSize() * DragPreviewScale);
    }

    float GetCellSize()
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();
        return board != null ? Mathf.Max(0.01f, board.cellSize) : 1f;
    }

    public void SetBuffIndicators(Color highlightColor, bool highlightEnabled, List<Sprite> iconSprites, List<Color> iconColors)
    {
        SetBuffHighlight(highlightColor, highlightEnabled);
        if (!highlightEnabled || iconColors == null || iconColors.Count == 0)
        {
            HideAllBuffIcons();
            return;
        }

        EnsureBuffIconCount(iconColors.Count);
        float iconSize = 0.22f;
        float spacing = 0.24f;
        float startX = -((iconColors.Count - 1) * spacing) * 0.5f;
        float y = 0.34f;

        for (int i = 0; i < buffIconRenderers.Count; i++)
        {
            SpriteRenderer sr = buffIconRenderers[i];
            if (sr == null) continue;
            if (i >= iconColors.Count)
            {
                sr.enabled = false;
                continue;
            }

            sr.enabled = true;
            Sprite icon = (iconSprites != null && i < iconSprites.Count) ? iconSprites[i] : null;
            sr.sprite = icon != null ? icon : CreateSquareSprite(Color.white);
            sr.color = iconColors[i];
            Transform t = sr.transform;
            t.localPosition = new Vector3(startX + i * spacing, y, 0f);
            if (icon != null)
            {
                Vector2 spriteSize = icon.bounds.size;
                float baseSize = Mathf.Max(spriteSize.x, spriteSize.y);
                float scale = baseSize > 0.0001f ? iconSize / baseSize : iconSize;
                t.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                t.localScale = new Vector3(iconSize, iconSize, 1f);
            }
        }
    }

    void EnsureDragPreviewRenderer()
    {
        if (dragPreviewRenderer != null) return;

        Transform t = transform.Find("DragPreview");
        if (t != null)
        {
            dragPreviewRenderer = t.GetComponent<SpriteRenderer>();
            if (dragPreviewRenderer != null) return;
        }

        GameObject go = new GameObject("DragPreview");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        dragPreviewRenderer = go.AddComponent<SpriteRenderer>();
        dragPreviewRenderer.sprite = CreateSquareSprite(Color.white);
        dragPreviewRenderer.sortingOrder = 3;
        dragPreviewRenderer.enabled = false;
    }

    void EnsureBuffHighlightRenderer()
    {
        if (buffHighlightRenderer != null) return;

        Transform t = transform.Find("BuffHighlight");
        if (t != null)
        {
            buffHighlightRenderer = t.GetComponent<SpriteRenderer>();
            if (buffHighlightRenderer != null) return;
        }

        GameObject go = new GameObject("BuffHighlight");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        buffHighlightRenderer = go.AddComponent<SpriteRenderer>();
        buffHighlightRenderer.sprite = CreateSquareSprite(Color.white);
        buffHighlightRenderer.sortingOrder = 0;
        buffHighlightRenderer.enabled = false;
    }

    void EnsureBuffIconCount(int count)
    {
        while (buffIconRenderers.Count < count)
        {
            int idx = buffIconRenderers.Count;
            GameObject go = new GameObject($"BuffIcon_{idx}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite(Color.white);
            sr.sortingOrder = 1;
            sr.enabled = false;
            buffIconRenderers.Add(sr);
        }
    }

    void HideAllBuffIcons()
    {
        for (int i = 0; i < buffIconRenderers.Count; i++)
        {
            if (buffIconRenderers[i] != null)
            {
                buffIconRenderers[i].enabled = false;
            }
        }
    }

    static Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}

