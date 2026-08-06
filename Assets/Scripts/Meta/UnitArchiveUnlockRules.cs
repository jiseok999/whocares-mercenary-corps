/// <summary>
/// 용병단 아카이브 — 유닛별 강화 해금 조건.
/// </summary>
public static class UnitArchiveUnlockRules
{
    public enum Requirement
    {
        Encounter,
        BoardPlaced,
        Evolution3,
        Evolution5
    }

    public static Requirement GetRequirement(int unitNumber)
    {
        int grade = UnitCombatStats.GetGradeForUnit(unitNumber);
        if (grade >= 4) return Requirement.Encounter;
        if (grade == 3) return Requirement.BoardPlaced;
        if (grade == 2) return Requirement.Evolution3;
        return Requirement.Evolution5;
    }

    public static bool IsRequirementMet(int unitNumber, Requirement requirement)
    {
        if (!UnitArchiveUnlockData.IsDiscovered(unitNumber)) return false;

        switch (requirement)
        {
            case Requirement.Encounter:
                return true;
            case Requirement.BoardPlaced:
                return UnitArchiveUnlockData.HasLifetimeBoardPlacement(unitNumber);
            case Requirement.Evolution3:
                return UnitArchiveUnlockData.HasLifetimeEvolution3(unitNumber);
            case Requirement.Evolution5:
                return UnitArchiveUnlockData.HasLifetimeEvolution5(unitNumber);
            default:
                return false;
        }
    }

    public static bool CanUpgradeUnit(int unitNumber)
    {
        return IsRequirementMet(unitNumber, GetRequirement(unitNumber));
    }
}
