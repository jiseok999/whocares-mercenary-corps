using UnityEngine;

/// <summary>
/// 용병 훈장(메타 재화).
/// </summary>
public static class MetaCurrency
{
    static int cachedMedals = -1;

    public static void EnsureLoaded()
    {
        if (cachedMedals >= 0) return;
        cachedMedals = MetaProgressPrefs.LoadMedals();
    }

    public static int GetMedals()
    {
        EnsureLoaded();
        return cachedMedals;
    }

    public static void AddMedals(int amount)
    {
        if (amount <= 0) return;
        EnsureLoaded();
        cachedMedals += amount;
        MetaProgressPrefs.SaveMedals(cachedMedals);
    }

    public static bool TrySpendMedals(int amount)
    {
        if (amount <= 0) return true;
        EnsureLoaded();
        if (cachedMedals < amount) return false;
        cachedMedals -= amount;
        MetaProgressPrefs.SaveMedals(cachedMedals);
        return true;
    }

    public static void Reload()
    {
        cachedMedals = MetaProgressPrefs.LoadMedals();
    }
}
