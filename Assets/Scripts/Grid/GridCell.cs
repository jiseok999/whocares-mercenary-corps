using UnityEngine;

/// <summary>
/// 그리드의 개별 셀을 나타내는 클래스
/// </summary>
public class GridCell : MonoBehaviour
{
    public int row;
    public int column;
    public bool isOccupied = false;
    public Plant plant = null;
    
    /// <summary>
    /// 이 셀에 식물을 배치합니다
    /// </summary>
    public bool PlacePlant(Plant plantPrefab)
    {
        if (isOccupied) return false;
        
        plant = Instantiate(plantPrefab, transform.position, Quaternion.identity);
        plant.transform.SetParent(transform);
        plant.currentCell = this;
        isOccupied = true;
        
        return true;
    }
    
    /// <summary>
    /// 이 셀에서 식물을 제거합니다
    /// </summary>
    public void RemovePlant()
    {
        if (plant != null)
        {
            Destroy(plant.gameObject);
            plant = null;
            isOccupied = false;
        }
    }
}

