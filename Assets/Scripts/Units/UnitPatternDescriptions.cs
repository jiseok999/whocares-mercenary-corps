/// <summary>
/// 상점 툴팁 등에 표시할 유닛별 전투 패턴 설명 (Character.TryAttack / 전용 컴포넌트 기준)
/// </summary>
public static class UnitPatternDescriptions
{
    public static string GetPatternDescription(int unitNumber)
    {
        return GameLocalization.GetUnitPatternDescription(unitNumber);
    }

    public static bool HasPattern(int unitNumber)
    {
        return unitNumber >= 1 && unitNumber <= 28;
    }
}
