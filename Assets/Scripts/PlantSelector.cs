using UnityEngine;

/// <summary>
/// 식물을 선택하고 배치하는 클래스
/// </summary>
public class PlantSelector : MonoBehaviour
{
    [Header("Plant Prefabs")]
    public Plant[] availablePlants;
    
    [Header("Selection")]
    public int selectedPlantIndex = 0;
    
    private GridManager gridManager;
    private Camera mainCamera;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        if (!GameManager.Instance.IsGameActive()) return;
        
        // 마우스 클릭으로 식물 배치
        if (Input.GetMouseButtonDown(0))
        {
            TryPlacePlant();
        }
        
        // 숫자 키로 식물 선택 (1, 2, 3, ...)
        for (int i = 0; i < availablePlants.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedPlantIndex = i;
                Debug.Log($"식물 선택: {availablePlants[i].name}");
            }
        }
    }
    
    /// <summary>
    /// 식물 배치를 시도합니다
    /// </summary>
    void TryPlacePlant()
    {
        if (availablePlants == null || availablePlants.Length == 0) return;
        if (selectedPlantIndex < 0 || selectedPlantIndex >= availablePlants.Length) return;
        
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;
        
        GridCell cell = gridManager.GetCellFromWorldPosition(mousePosition);
        
        if (cell != null && !cell.isOccupied)
        {
            Plant selectedPlant = availablePlants[selectedPlantIndex];
            
            if (GameManager.Instance.SpendSunPoints(selectedPlant.cost))
            {
                cell.PlacePlant(selectedPlant);
            }
            else
            {
                Debug.Log("태양 포인트가 부족합니다!");
            }
        }
    }
}

