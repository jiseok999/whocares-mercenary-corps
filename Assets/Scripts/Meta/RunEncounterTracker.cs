using System.Collections.Generic;

/// <summary>
/// 현재 런에서 만난 유닛·배치·진화 추적(세션 전용).
/// </summary>
public static class RunEncounterTracker
{
    static readonly HashSet<int> EncounteredThisRun = new HashSet<int>();
    static readonly HashSet<int> BoardPlacedThisRun = new HashSet<int>();
    static readonly Dictionary<int, int> MaxEvolutionThisRun = new Dictionary<int, int>();
    static readonly HashSet<int> DiscoveredAtRunStart = new HashSet<int>();

    public static void BeginRun()
    {
        EncounteredThisRun.Clear();
        BoardPlacedThisRun.Clear();
        MaxEvolutionThisRun.Clear();
        DiscoveredAtRunStart.Clear();
        UnitArchiveUnlockData.EnsureLoaded();
        for (int unitNumber = 1; unitNumber <= 28; unitNumber++)
        {
            if (UnitArchiveUnlockData.IsDiscovered(unitNumber))
            {
                DiscoveredAtRunStart.Add(unitNumber);
            }
        }
    }

    public static void RegisterEncounter(int unitNumber)
    {
        if (unitNumber < 1 || unitNumber > 28) return;
        EncounteredThisRun.Add(unitNumber);
        RegisterEvolution(unitNumber, 1);
    }

    public static void RegisterBoardPlacement(int unitNumber)
    {
        if (unitNumber < 1 || unitNumber > 28) return;
        EncounteredThisRun.Add(unitNumber);
        BoardPlacedThisRun.Add(unitNumber);
    }

    public static void RegisterEvolution(int unitNumber, int evolutionLevel)
    {
        if (unitNumber < 1 || unitNumber > 28) return;
        EncounteredThisRun.Add(unitNumber);
        if (MaxEvolutionThisRun.TryGetValue(unitNumber, out int current))
        {
            MaxEvolutionThisRun[unitNumber] = evolutionLevel > current ? evolutionLevel : current;
        }
        else
        {
            MaxEvolutionThisRun[unitNumber] = evolutionLevel;
        }
    }

    public static IReadOnlyCollection<int> GetEncounteredThisRun() => EncounteredThisRun;

    public static int CountFirstMeetBonusUnits()
    {
        int count = 0;
        foreach (int unitNumber in EncounteredThisRun)
        {
            if (!DiscoveredAtRunStart.Contains(unitNumber))
            {
                count++;
            }
        }
        return count;
    }

    public static void ApplyLifetimeProgress()
    {
        foreach (int unitNumber in BoardPlacedThisRun)
        {
            UnitArchiveUnlockData.MarkBoardPlaced(unitNumber);
        }

        foreach (KeyValuePair<int, int> pair in MaxEvolutionThisRun)
        {
            if (pair.Value >= 3) UnitArchiveUnlockData.MarkEvolution3(pair.Key);
            if (pair.Value >= 5) UnitArchiveUnlockData.MarkEvolution5(pair.Key);
        }
    }
}
