/// <summary>
/// 용병단 아카이브 — UI용 해금/특화 문구.
/// </summary>
public static class UnitArchiveUiText
{
    public static string GetUnlockHint(int unitNumber)
    {
        if (UnitArchiveUnlockData.IsUnlocked(unitNumber)) return string.Empty;
        if (!UnitArchiveUnlockData.IsDiscovered(unitNumber)) return GameLocalization.ArchiveLockedHint;

        switch (UnitArchiveUnlockRules.GetRequirement(unitNumber))
        {
            case UnitArchiveUnlockRules.Requirement.BoardPlaced:
                return GameLocalization.ArchiveUnlockBoardPlaced;
            case UnitArchiveUnlockRules.Requirement.Evolution3:
                return GameLocalization.ArchiveUnlockEvolution3;
            case UnitArchiveUnlockRules.Requirement.Evolution5:
                return GameLocalization.ArchiveUnlockEvolution5;
            default:
                return GameLocalization.ArchiveDiscoveredOnly;
        }
    }

    public static string GetSpecialtyLabel(int unitNumber)
    {
        switch (UnitArchiveSpecialty.GetType(unitNumber))
        {
            case UnitArchiveSpecialty.Type.Range:
                return GameLocalization.ArchiveSpecialtyRange;
            case UnitArchiveSpecialty.Type.Economy:
                return GameLocalization.ArchiveSpecialtyEconomy;
            default:
                return string.Empty;
        }
    }

    public static string FormatCardLevels(int unitNumber)
    {
        int specialty = CharacterUpgradeData.GetSpecialtyLevel(unitNumber);
        string specialtyKey = UnitArchiveSpecialty.HasSpecialty(unitNumber) ? $" · S{specialty}" : string.Empty;
        int awakening = UnitArchiveAwakening.GetLevel(unitNumber);
        string awakeningKey = awakening > 0 ? $" · ★{awakening}" : string.Empty;
        return $"A{CharacterUpgradeData.GetAttackLevel(unitNumber)} · M{CharacterUpgradeData.GetMobilityLevel(unitNumber)}{specialtyKey}{awakeningKey}";
    }
}
