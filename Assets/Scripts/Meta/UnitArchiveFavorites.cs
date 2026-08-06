using UnityEngine;



/// <summary>

/// 용병단 아카이브 — 즐겨찾기 유닛(영구).

/// </summary>

public static class UnitArchiveFavorites

{

    const int UnitCount = 28;



    static int favoriteMask = -1;



    public static void EnsureLoaded()

    {

        if (favoriteMask >= 0) return;

        favoriteMask = MetaProgressPrefs.LoadFavoriteMask();

    }



    public static bool IsFavorite(int unitNumber) => HasBit(favoriteMask, unitNumber);



    public static void Toggle(int unitNumber)

    {

        if (unitNumber < 1 || unitNumber > UnitCount) return;

        EnsureLoaded();

        int bit = 1 << (unitNumber - 1);

        favoriteMask ^= bit;

        MetaProgressPrefs.SaveFavoriteMask(favoriteMask);

    }



    public static void Reload()

    {

        favoriteMask = -1;

        EnsureLoaded();

    }



    static bool HasBit(int mask, int unitNumber)

    {

        if (unitNumber < 1 || unitNumber > UnitCount) return false;

        EnsureLoaded();

        return (mask & (1 << (unitNumber - 1))) != 0;

    }

}

