using UnityEngine;

/// <summary>
/// 유닛 데이터를 담는 클래스
/// </summary>
[System.Serializable]
public class UnitData
{
    public int unitNumber; // 유닛 번호 (1~28)
    public int grade; // 등급 (1~5)
    public string description; // 설명
    
    public UnitData(int unitNumber, int grade, string description)
    {
        this.unitNumber = unitNumber;
        this.grade = grade;
        this.description = description;
    }
    
    /// <summary>
    /// 유닛 표시 이름을 반환합니다
    /// </summary>
    public string GetUnitName()
    {
        return UnitTraitData.GetDisplayName(unitNumber);
    }
    
    public string GetDescription()
    {
        return GameLocalization.GetUnitGradeDescription(grade, unitNumber);
    }
    
    /// <summary>
    /// 등급 이름을 반환합니다
    /// </summary>
    public string GetGradeName()
    {
        return $"{GameLocalization.GradeNameFormat(grade)}";
    }
}

