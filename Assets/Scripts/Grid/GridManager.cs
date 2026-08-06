using UnityEngine;

/// <summary>
/// 게임의 그리드 시스템을 관리하는 클래스
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 5;
    public int columns = 9;
    public float cellSize = 1f;
    public Vector2 gridStartPosition = Vector2.zero;
    
    [Header("Prefabs")]
    public GameObject gridCellPrefab;
    
    private GridCell[,] grid;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        CreateGrid();
    }
    
    /// <summary>
    /// 그리드를 생성합니다
    /// </summary>
    void CreateGrid()
    {
        grid = new GridCell[rows, columns];
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2 position = gridStartPosition + new Vector2(col * cellSize, row * cellSize);
                GameObject cellObj = Instantiate(gridCellPrefab, position, Quaternion.identity);
                cellObj.name = $"GridCell_{row}_{col}";
                cellObj.transform.SetParent(transform);
                
                GridCell cell = cellObj.GetComponent<GridCell>();
                if (cell == null)
                {
                    cell = cellObj.AddComponent<GridCell>();
                }
                
                cell.row = row;
                cell.column = col;
                grid[row, col] = cell;
            }
        }
    }
    
    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다
    /// </summary>
    public GridCell GetCellFromWorldPosition(Vector2 worldPosition)
    {
        int col = Mathf.FloorToInt((worldPosition.x - gridStartPosition.x) / cellSize);
        int row = Mathf.FloorToInt((worldPosition.y - gridStartPosition.y) / cellSize);
        
        if (row >= 0 && row < rows && col >= 0 && col < columns)
        {
            return grid[row, col];
        }
        
        return null;
    }
    
    /// <summary>
    /// 특정 행의 모든 셀을 반환합니다
    /// </summary>
    public GridCell[] GetRowCells(int row)
    {
        if (row < 0 || row >= rows) return null;
        
        GridCell[] rowCells = new GridCell[columns];
        for (int col = 0; col < columns; col++)
        {
            rowCells[col] = grid[row, col];
        }
        return rowCells;
    }
    
    /// <summary>
    /// 그리드 셀을 직접 가져옵니다
    /// </summary>
    public GridCell GetCell(int row, int col)
    {
        if (row >= 0 && row < rows && col >= 0 && col < columns)
        {
            return grid[row, col];
        }
        return null;
    }
}

