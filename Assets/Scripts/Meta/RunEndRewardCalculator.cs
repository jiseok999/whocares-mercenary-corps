/// <summary>
/// 런 종료 시 용병 훈장 보상 계산·지급.
/// </summary>
public static class RunEndRewardCalculator
{
    public struct Summary
    {
        public int roundMedals;
        public int milestoneMedals;
        public int firstMeetMedals;
        public int totalMedals;
        public int firstMeetUnitCount;
    }

    public static Summary LastSummary { get; private set; }

    public static Summary ApplyRewards(int roundReached)
    {
        int round = roundReached < 1 ? 1 : roundReached;
        int roundMedals = round * 2;
        int milestoneMedals = 0;
        if (round >= 10) milestoneMedals += 20;
        if (round >= 20) milestoneMedals += 40;
        if (round >= 30) milestoneMedals += 80;

        int firstMeetCount = RunEncounterTracker.CountFirstMeetBonusUnits();
        int firstMeetMedals = firstMeetCount * 5;

        UnitArchiveUnlockData.DiscoverAll(RunEncounterTracker.GetEncounteredThisRun());
        RunEncounterTracker.ApplyLifetimeProgress();
        UnitArchiveUnlockData.RefreshUpgradeUnlocks();

        int total = roundMedals + milestoneMedals + firstMeetMedals;
        if (total > 0)
        {
            MetaCurrency.AddMedals(total);
        }

        LastSummary = new Summary
        {
            roundMedals = roundMedals,
            milestoneMedals = milestoneMedals,
            firstMeetMedals = firstMeetMedals,
            totalMedals = total,
            firstMeetUnitCount = firstMeetCount
        };
        return LastSummary;
    }
}
