using UnityEngine;



/// <summary>

/// 유닛 각성 — 진화 마일스톤과 연계된 영구 3단계 강화.

/// </summary>

public static class UnitArchiveAwakening

{

    const int UnitCount = 28;

    const int MaxLevel = 3;



    static readonly int[] Levels = new int[UnitCount];

    static bool loaded;



    public static void EnsureLoaded()

    {

        if (loaded) return;

        MetaProgressPrefs.LoadAwakeningLevels(Levels);

        loaded = true;

    }



    public static void Reload()

    {

        loaded = false;

        EnsureLoaded();

    }



    public static int GetLevel(int unitNumber) => GetLevelInternal(unitNumber);



    public static int GetMaxLevel() => MaxLevel;



    public static int GetAttackBonus(int unitNumber) => GetLevel(unitNumber) * 2;



    public static float GetProcChanceBonus(int unitNumber) => GetLevel(unitNumber) * 0.04f;



    public static float GetFreezeDurationBonus(int unitNumber)

    {

        if (unitNumber != 11) return 0f;

        return GetLevel(unitNumber) * 0.08f;

    }



    public static float GetGoldCooldownReduction(int unitNumber)

    {

        if (unitNumber != 4) return 0f;

        return GetLevel(unitNumber) * 0.1f;

    }



    public static string GetTierLabel(int tierIndex) => GameLocalization.ArchiveAwakeningTierFormat(tierIndex + 1);



    public static string GetTierDescription(int unitNumber, int tierIndex)

    {

        var defs = UnitEvolutionMilestones.GetMilestoneDefinitions(unitNumber);

        if (tierIndex < 0 || tierIndex >= defs.Count) return GameLocalization.ArchiveAwakeningGenericEffect;

        return defs[tierIndex].description;

    }



    public static string GetUnlockHint(int unitNumber, int targetLevel)

    {

        if (targetLevel <= 1) return GameLocalization.ArchiveAwakeningNeedUpgradeUnlock;

        if (targetLevel == 2) return GameLocalization.ArchiveAwakeningNeedStatLevels;

        return GameLocalization.ArchiveAwakeningNeedEvolution5;

    }



    public static bool CanUpgrade(int unitNumber)

    {

        if (unitNumber < 1 || unitNumber > UnitCount) return false;

        if (!UnitArchiveUnlockData.IsUnlocked(unitNumber)) return false;

        return GetLevel(unitNumber) < MaxLevel && MeetsRequirement(unitNumber, GetLevel(unitNumber) + 1);

    }



    public static bool MeetsRequirement(int unitNumber, int targetLevel)

    {

        if (targetLevel <= 1) return UnitArchiveUnlockData.IsUnlocked(unitNumber);

        if (targetLevel == 2)

        {

            return GetLevel(unitNumber) >= 1

                   && TotalStatLevels(unitNumber) >= 3;

        }

        if (targetLevel == 3)

        {

            return GetLevel(unitNumber) >= 2

                   && UnitArchiveUnlockData.HasLifetimeEvolution5(unitNumber);

        }

        return false;

    }



    public static int GetUpgradeCost(int unitNumber)

    {

        int grade = UnitCombatStats.GetGradeForUnit(unitNumber);

        int baseCost = GetBaseCostForGrade(grade);

        int current = GetLevel(unitNumber);

        return Mathf.RoundToInt(baseCost * 2f * (1f + current * 0.75f));

    }



    public static bool TryUpgrade(int unitNumber)

    {

        if (!CanUpgrade(unitNumber)) return false;

        EnsureLoaded();

        int cost = GetUpgradeCost(unitNumber);

        if (!MetaCurrency.TrySpendMedals(cost)) return false;

        Levels[unitNumber - 1]++;

        MetaProgressPrefs.SaveAwakeningLevels(Levels);

        return true;

    }



    public static bool HasAny(int unitNumber) => GetLevel(unitNumber) > 0;



    static int TotalStatLevels(int unitNumber)

    {

        return CharacterUpgradeData.GetAttackLevel(unitNumber)

               + CharacterUpgradeData.GetMobilityLevel(unitNumber)

               + CharacterUpgradeData.GetSpecialtyLevel(unitNumber);

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



    static int GetLevelInternal(int unitNumber)

    {

        if (unitNumber < 1 || unitNumber > UnitCount) return 0;

        EnsureLoaded();

        return Levels[unitNumber - 1];

    }

}

