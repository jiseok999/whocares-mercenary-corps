using System.Collections.Generic;

/// <summary>
/// 특수 스킬 해제/업그레이드 데이터
/// </summary>
public static class SkillProgressData
{
    private static HashSet<int> unlocked = new HashSet<int> { 1 };
    private static Dictionary<int, int> upgradeLevels = new Dictionary<int, int>();
    
    public static bool IsUnlocked(int id)
    {
        return unlocked.Contains(id);
    }
    
    public static int GetUpgradeLevel(int id)
    {
        if (!upgradeLevels.ContainsKey(id)) return 0;
        return upgradeLevels[id];
    }
    
    public static bool TryUnlock(int id, GameManager gm)
    {
        if (IsUnlocked(id)) return false;
        if (gm == null) return false;
        if (gm.SpendSilver(1))
        {
            unlocked.Add(id);
            return true;
        }
        return false;
    }
    
    public static bool TryUpgrade(int id, GameManager gm)
    {
        if (!IsUnlocked(id)) return false;
        if (gm == null) return false;
        if (gm.SpendSilver(1))
        {
            int current = GetUpgradeLevel(id);
            upgradeLevels[id] = current + 1;
            return true;
        }
        return false;
    }
}


