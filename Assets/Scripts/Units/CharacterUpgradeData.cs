using UnityEngine;

/// <summary>
/// 용병단 아카이브 — 유닛 영구 강화(공격·기동·특화).
/// </summary>
public static class CharacterUpgradeData
{
    public enum UpgradeSlot { Attack, Mobility, Specialty }

    const int UnitCount = 28;

    static readonly int[] AttackLevels = new int[UnitCount];
    static readonly int[] MobilityLevels = new int[UnitCount];
    static readonly int[] SpecialtyLevels = new int[UnitCount];
    static bool loaded;

    public static void EnsureLoaded()
    {
        if (loaded) return;
        MetaProgressPrefs.LoadAttackLevels(AttackLevels);
        MetaProgressPrefs.LoadMobilityLevels(MobilityLevels);
        MetaProgressPrefs.LoadSpecialtyLevels(SpecialtyLevels);
        loaded = true;
    }

    public static void Reload()
    {
        loaded = false;
        EnsureLoaded();
    }

    public static int GetAttackLevel(int unitNumber) => GetLevel(AttackLevels, unitNumber);
    public static int GetMobilityLevel(int unitNumber) => GetLevel(MobilityLevels, unitNumber);
    public static int GetSpecialtyLevel(int unitNumber) => GetLevel(SpecialtyLevels, unitNumber);

    /// <summary>레거시 호환 — 공격 레벨과 동일.</summary>
    public static int GetLevel(int unitNumber) => GetAttackLevel(unitNumber);

    public static int GetAttackBonus(int unitNumber) => GetAttackBonusForLevel(GetAttackLevel(unitNumber));

    public static int GetAttackBonusForLevel(int level)
    {
        int sum = 0;
        for (int i = 1; i <= level; i++)
        {
            sum += GetAttackIncrementForLevel(i);
        }
        return sum;
    }

    public static int GetAttackIncrementForLevel(int targetLevel)
    {
        switch (targetLevel)
        {
            case 1: return 2;
            case 2: return 3;
            default: return 5;
        }
    }

    public static float GetMobilityIntervalMultiplier(int unitNumber)
    {
        if (!UnitCombatStats.CanAutoAttack(unitNumber)) return 1f;
        return GetMobilityIntervalMultiplierForLevel(GetMobilityLevel(unitNumber));
    }

    public static float GetMobilityIntervalMultiplierForLevel(int level)
    {
        return Mathf.Clamp(1f - GetMobilityReduction(level), 0.5f, 1f);
    }

    public static float GetRangeBonus(int unitNumber)
    {
        if (UnitArchiveSpecialty.GetType(unitNumber) != UnitArchiveSpecialty.Type.Range) return 0f;
        return GetRangeBonusForLevel(GetSpecialtyLevel(unitNumber));
    }

    public static float GetRangeBonusForLevel(int level)
    {
        float sum = 0f;
        for (int i = 1; i <= level; i++)
        {
            sum += GetRangeIncrementForLevel(i);
        }
        return sum;
    }

    static float GetRangeIncrementForLevel(int targetLevel)
    {
        switch (targetLevel)
        {
            case 1: return 0.3f;
            case 2: return 0.4f;
            default: return 0.5f;
        }
    }

    public static float GetEconomyCooldownMultiplier(int unitNumber)
    {
        if (UnitArchiveSpecialty.GetType(unitNumber) != UnitArchiveSpecialty.Type.Economy) return 1f;
        return GetEconomyCooldownMultiplierForLevel(GetSpecialtyLevel(unitNumber));
    }

    public static float GetEconomyCooldownMultiplierForLevel(int level)
    {
        return Mathf.Clamp(1f - GetEconomyReduction(level), 0.55f, 1f);
    }

    static float GetEconomyReduction(int level)
    {
        float sum = 0f;
        for (int i = 1; i <= level; i++)
        {
            sum += GetEconomyIncrementForLevel(i);
        }
        return sum;
    }

    static float GetEconomyIncrementForLevel(int targetLevel)
    {
        switch (targetLevel)
        {
            case 1: return 0.03f;
            case 2: return 0.04f;
            default: return 0.05f;
        }
    }

    static float GetMobilityReduction(int level)
    {
        float sum = 0f;
        for (int i = 1; i <= level; i++)
        {
            sum += GetMobilityIncrementForLevel(i);
        }
        return sum;
    }

    static float GetMobilityIncrementForLevel(int targetLevel)
    {
        switch (targetLevel)
        {
            case 1: return 0.02f;
            case 2: return 0.03f;
            default: return 0.04f;
        }
    }

    public static int GetMaxAttackLevel(int unitNumber) => GetMaxAttackLevelForGrade(UnitCombatStats.GetGradeForUnit(unitNumber));
    public static int GetMaxMobilityLevel(int unitNumber) => GetMaxMobilityLevelForGrade(UnitCombatStats.GetGradeForUnit(unitNumber));
    public static int GetMaxSpecialtyLevel(int unitNumber) => GetMaxSpecialtyLevelForGrade(UnitCombatStats.GetGradeForUnit(unitNumber));

    public static int GetMaxAttackLevelForGrade(int grade)
    {
        if (grade >= 5) return 3;
        if (grade >= 3) return 4;
        return 5;
    }

    public static int GetMaxMobilityLevelForGrade(int grade) => grade >= 3 ? 3 : 2;

    public static int GetMaxSpecialtyLevelForGrade(int grade) => grade >= 3 ? 3 : 2;

    public static int GetUpgradeCost(int unitNumber, UpgradeSlot slot, int currentLevel)
    {
        int grade = UnitCombatStats.GetGradeForUnit(unitNumber);
        int baseCost = GetBaseCostForGrade(grade);
        if (slot == UpgradeSlot.Specialty)
        {
            baseCost = Mathf.RoundToInt(baseCost * 1.15f);
        }
        return Mathf.RoundToInt(baseCost * (1f + currentLevel * 0.6f));
    }

    static int GetBaseCostForGrade(int grade)
    {
        switch (grade)
        {
            case 5: return 30;
            case 4: return 50;
            case 3: return 80;
            case 2: return 120;
            default: return 180;
        }
    }

    public static bool CanUpgrade(int unitNumber, UpgradeSlot slot)
    {
        if (unitNumber < 1 || unitNumber > UnitCount) return false;
        if (!UnitArchiveUnlockData.IsUnlocked(unitNumber)) return false;

        EnsureLoaded();
        switch (slot)
        {
            case UpgradeSlot.Attack:
                return GetAttackLevel(unitNumber) < GetMaxAttackLevel(unitNumber);
            case UpgradeSlot.Mobility:
                if (!UnitCombatStats.CanAutoAttack(unitNumber)) return false;
                return GetMobilityLevel(unitNumber) < GetMaxMobilityLevel(unitNumber);
            case UpgradeSlot.Specialty:
                if (!UnitArchiveSpecialty.HasSpecialty(unitNumber)) return false;
                return GetSpecialtyLevel(unitNumber) < GetMaxSpecialtyLevel(unitNumber);
            default:
                return false;
        }
    }

    public static bool TryUpgrade(int unitNumber, UpgradeSlot slot)
    {
        if (!CanUpgrade(unitNumber, slot)) return false;

        EnsureLoaded();
        int current = GetCurrentLevel(unitNumber, slot);
        int cost = GetUpgradeCost(unitNumber, slot, current);
        if (!MetaCurrency.TrySpendMedals(cost)) return false;

        switch (slot)
        {
            case UpgradeSlot.Attack:
                AttackLevels[unitNumber - 1] = current + 1;
                MetaProgressPrefs.SaveAttackLevels(AttackLevels);
                break;
            case UpgradeSlot.Mobility:
                MobilityLevels[unitNumber - 1] = current + 1;
                MetaProgressPrefs.SaveMobilityLevels(MobilityLevels);
                break;
            case UpgradeSlot.Specialty:
                SpecialtyLevels[unitNumber - 1] = current + 1;
                MetaProgressPrefs.SaveSpecialtyLevels(SpecialtyLevels);
                break;
        }
        return true;
    }

    public static bool HasAnyUpgrade(int unitNumber)
    {
        return GetAttackLevel(unitNumber) > 0
               || GetMobilityLevel(unitNumber) > 0
               || GetSpecialtyLevel(unitNumber) > 0;
    }

    static int GetCurrentLevel(int unitNumber, UpgradeSlot slot)
    {
        switch (slot)
        {
            case UpgradeSlot.Attack: return GetAttackLevel(unitNumber);
            case UpgradeSlot.Mobility: return GetMobilityLevel(unitNumber);
            default: return GetSpecialtyLevel(unitNumber);
        }
    }

    static int GetLevel(int[] levels, int unitNumber)
    {
        if (unitNumber < 1 || unitNumber > UnitCount) return 0;
        EnsureLoaded();
        return levels[unitNumber - 1];
    }
}
