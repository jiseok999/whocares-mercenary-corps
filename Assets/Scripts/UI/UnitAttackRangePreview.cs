using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 벤치·보드 유닛 호버 시 공격 사거리를 칸 단위 반투명 직사각형으로 표시합니다.
/// </summary>
public class UnitAttackRangePreview : MonoBehaviour
{
    static UnitAttackRangePreview instance;
    static BoardManager cachedBoardManager;
    static Sprite squareSprite;

    Transform cellPadsRoot;
    readonly List<SpriteRenderer> cellPads = new List<SpriteRenderer>(64);
    readonly List<Vector3> cellCentersBuffer = new List<Vector3>(64);

    Character activeCharacter;
    float pulseTime;
    int activePadCount;

    const int CellPadSortOrder = -3;
    const float CellPadScale = 0.92f;

    static readonly Color CellFillColor = new Color(0.95f, 0.78f, 0.28f, 0.16f);

    public static void EnsureExists()
    {
        if (instance != null) return;

        GameObject host = new GameObject("UnitAttackRangePreview");
        instance = host.AddComponent<UnitAttackRangePreview>();
    }

    public static void Hide()
    {
        if (instance != null)
        {
            instance.HidePreview();
        }
    }

    public static void ShowFor(Character character)
    {
        if (instance == null) EnsureExists();
        if (instance == null) return;
        instance.ShowForCharacter(character);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        GameObject root = new GameObject("CellPads");
        root.transform.SetParent(transform, false);
        cellPadsRoot = root.transform;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Update()
    {
        if (activeCharacter == null || activePadCount <= 0) return;

        pulseTime += Time.deltaTime;
        float alphaMul = 0.9f + Mathf.Sin(pulseTime * 6f) * 0.08f;
        Color color = CellFillColor;
        color.a *= alphaMul;

        for (int i = 0; i < activePadCount; i++)
        {
            SpriteRenderer pad = cellPads[i];
            if (pad == null || !pad.enabled) continue;
            pad.color = color;
        }
    }

    void ShowForCharacter(Character character)
    {
        if (character == null || character.unitNumber < 1)
        {
            HidePreview();
            return;
        }

        if (activeCharacter != null && activeCharacter != character)
        {
            activeCharacter.SetRangePreviewMarkerHighlight(false);
        }

        activeCharacter = character;
        BoardManager boardManager = GetBoardManager();
        if (boardManager == null)
        {
            HidePreview();
            return;
        }

        float range = character.attackRange;
        bool global = range <= 0f;

        cellCentersBuffer.Clear();
        CollectRangeCells(character.transform.position, range, global, boardManager, cellCentersBuffer);
        ShowCellPads(cellCentersBuffer, Mathf.Max(0.01f, boardManager.cellSize));

        if (character.unitNumber == 10 || character.unitNumber == 16 || character.unitNumber == 19)
        {
            character.SetRangePreviewMarkerHighlight(true);
        }

        gameObject.SetActive(cellCentersBuffer.Count > 0);
        pulseTime = 0f;
    }

    static void CollectRangeCells(Vector3 unitPos, float range, bool global, BoardManager boardManager, List<Vector3> results)
    {
        if (!boardManager.TryGetCellGridOrigin(out Vector3 gridOrigin)) return;
        if (!boardManager.TryGetCombatCorridorBounds(out float centerX, out float _, out float corridorWidth, out float _))
        {
            return;
        }

        float cellSize = boardManager.cellSize;
        float corridorRightX = centerX + corridorWidth * 0.5f;
        int maxCol = Mathf.Max(0, Mathf.FloorToInt((corridorRightX - gridOrigin.x + cellSize * 0.25f) / cellSize));
        int minCol = boardManager.boardColumns;
        float rangeSq = global ? float.MaxValue : range * range;
        float tolerance = cellSize * cellSize * 0.15f;

        if (minCol > maxCol) return;

        for (int col = minCol; col <= maxCol; col++)
        {
            for (int row = 0; row < boardManager.boardRows; row++)
            {
                Vector3 cellCenter = gridOrigin + new Vector3(col * cellSize, -row * cellSize, 0f);
                if (global)
                {
                    results.Add(cellCenter);
                    continue;
                }

                float distSq = (cellCenter - unitPos).sqrMagnitude;
                if (distSq <= rangeSq + tolerance)
                {
                    results.Add(cellCenter);
                }
            }
        }
    }

    void ShowCellPads(List<Vector3> centers, float cellSize)
    {
        int count = centers.Count;
        EnsureCellPadPool(count);
        float scale = cellSize * CellPadScale;
        Color color = CellFillColor;

        for (int i = 0; i < count; i++)
        {
            SpriteRenderer pad = cellPads[i];
            pad.enabled = true;
            pad.sprite = GetSquareSprite();
            pad.color = color;
            pad.sortingOrder = CellPadSortOrder;
            pad.transform.position = centers[i];
            pad.transform.localScale = new Vector3(scale, scale, 1f);
        }

        for (int i = count; i < cellPads.Count; i++)
        {
            cellPads[i].enabled = false;
        }

        activePadCount = count;
    }

    void EnsureCellPadPool(int requiredCount)
    {
        while (cellPads.Count < requiredCount)
        {
            GameObject padObj = new GameObject("RangeCell_" + cellPads.Count);
            padObj.transform.SetParent(cellPadsRoot, false);
            SpriteRenderer renderer = padObj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = CellPadSortOrder;
            cellPads.Add(renderer);
        }
    }

    void HidePreview()
    {
        if (activeCharacter != null)
        {
            activeCharacter.SetRangePreviewMarkerHighlight(false);
            activeCharacter = null;
        }

        activePadCount = 0;
        for (int i = 0; i < cellPads.Count; i++)
        {
            if (cellPads[i] != null)
            {
                cellPads[i].enabled = false;
            }
        }

        gameObject.SetActive(false);
    }

    static BoardManager GetBoardManager()
    {
        if (cachedBoardManager != null) return cachedBoardManager;
        cachedBoardManager = FindFirstObjectByType<BoardManager>();
        return cachedBoardManager;
    }

    static Sprite GetSquareSprite()
    {
        if (squareSprite != null) return squareSprite;

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return squareSprite;
    }
}
