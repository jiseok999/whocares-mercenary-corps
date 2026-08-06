using UnityEngine;

/// <summary>
/// 용병단 아카이브 — 유닛 발견/강화 해금(영구).
/// </summary>
public static class UnitArchiveUnlockData
{
    const int UnitCount = 28;

    static int discoverMask = -1;
    static int upgradeMask = -1;
    static int boardPlacedMask = -1;
    static int evo3Mask = -1;
    static int evo5Mask = -1;

    public static void EnsureLoaded()
    {
        if (discoverMask >= 0) return;
        discoverMask = MetaProgressPrefs.LoadDiscoverMask();
        upgradeMask = MetaProgressPrefs.LoadUpgradeUnlockMask();
        boardPlacedMask = MetaProgressPrefs.LoadBoardPlacedMask();
        evo3Mask = MetaProgressPrefs.LoadEvo3Mask();
        evo5Mask = MetaProgressPrefs.LoadEvo5Mask();
    }

    public static bool IsDiscovered(int unitNumber) => HasBit(discoverMask, unitNumber);

    /// <summary>강화 가능(메달 사용) 여부.</summary>
    public static bool IsUnlocked(int unitNumber) => HasBit(upgradeMask, unitNumber);

    public static bool HasLifetimeBoardPlacement(int unitNumber) => HasBit(boardPlacedMask, unitNumber);

    public static bool HasLifetimeEvolution3(int unitNumber) => HasBit(evo3Mask, unitNumber);

    public static bool HasLifetimeEvolution5(int unitNumber) => HasBit(evo5Mask, unitNumber);

    public static void Discover(int unitNumber)
    {
        if (TrySetBit(ref discoverMask, unitNumber))
        {
            MetaProgressPrefs.SaveDiscoverMask(discoverMask);
        }
    }

    public static void DiscoverAll(System.Collections.Generic.IEnumerable<int> unitNumbers)
    {
        EnsureLoaded();
        bool changed = false;
        foreach (int unitNumber in unitNumbers)
        {
            if (TrySetBit(ref discoverMask, unitNumber))
            {
                changed = true;
            }
        }
        if (changed)
        {
            MetaProgressPrefs.SaveDiscoverMask(discoverMask);
        }
    }

    public static void MarkBoardPlaced(int unitNumber)
    {
        if (TrySetBit(ref boardPlacedMask, unitNumber))
        {
            MetaProgressPrefs.SaveBoardPlacedMask(boardPlacedMask);
        }
    }

    public static void MarkEvolution3(int unitNumber)
    {
        if (TrySetBit(ref evo3Mask, unitNumber))
        {
            MetaProgressPrefs.SaveEvo3Mask(evo3Mask);
        }
    }

    public static void MarkEvolution5(int unitNumber)
    {
        if (TrySetBit(ref evo5Mask, unitNumber))
        {
            MetaProgressPrefs.SaveEvo5Mask(evo5Mask);
        }
    }

    public static void RefreshUpgradeUnlocks()
    {
        EnsureLoaded();
        bool changed = false;
        for (int unitNumber = 1; unitNumber <= UnitCount; unitNumber++)
        {
            if (!IsDiscovered(unitNumber)) continue;
            if (!UnitArchiveUnlockRules.CanUpgradeUnit(unitNumber)) continue;
            if (TrySetBit(ref upgradeMask, unitNumber))
            {
                changed = true;
            }
        }
        if (changed)
        {
            MetaProgressPrefs.SaveUpgradeUnlockMask(upgradeMask);
        }
    }

    public static void Reload()
    {
        discoverMask = -1;
        upgradeMask = -1;
        boardPlacedMask = -1;
        evo3Mask = -1;
        evo5Mask = -1;
        EnsureLoaded();
    }

    static bool HasBit(int mask, int unitNumber)
    {
        if (unitNumber < 1 || unitNumber > UnitCount) return false;
        EnsureLoaded();
        return (mask & (1 << (unitNumber - 1))) != 0;
    }

    static bool TrySetBit(ref int mask, int unitNumber)
    {
        if (unitNumber < 1 || unitNumber > UnitCount) return false;
        EnsureLoaded();
        int bit = 1 << (unitNumber - 1);
        if ((mask & bit) != 0) return false;
        mask |= bit;
        return true;
    }
}
