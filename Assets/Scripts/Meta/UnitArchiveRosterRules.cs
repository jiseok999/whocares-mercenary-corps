/// <summary>
/// 용병단 아카이브 로스터 표시 규칙.
/// </summary>
public static class UnitArchiveRosterRules
{
    /// <summary>상점 미노출·히든 유닛 — 아카이브 목록에 표시하지 않음.</summary>
    public static bool IsListedInRoster(int unitNumber)
    {
        return unitNumber != 4 && unitNumber != 26;
    }

    public static int GetFirstListedUnit()
    {
        for (int unitNumber = 1; unitNumber <= 28; unitNumber++)
        {
            if (IsListedInRoster(unitNumber)) return unitNumber;
        }
        return 1;
    }

    public static void EnsureListedSelection(ref int unitNumber)
    {
        if (IsListedInRoster(unitNumber)) return;
        unitNumber = GetFirstListedUnit();
    }
}
