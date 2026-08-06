using UnityEngine;

/// <summary>
/// 영구 메타 진행(PlayerPrefs) 로드/저장.
/// </summary>
public static class MetaProgressPrefs
{
    public const string MedalsKey = "meta_medals";
    public const string UnlockMaskKey = "meta_unit_unlock_mask";
    public const string DiscoverMaskKey = "meta_unit_discover_mask";
    public const string UpgradeUnlockMaskKey = "meta_unit_upgrade_unlock_mask";
    public const string BoardPlacedMaskKey = "meta_board_placed_mask";
    public const string Evo3MaskKey = "meta_evo3_mask";
    public const string Evo5MaskKey = "meta_evo5_mask";
    public const string AttackLevelsKey = "meta_attack_levels";
    public const string MobilityLevelsKey = "meta_mobility_levels";
    public const string SpecialtyLevelsKey = "meta_specialty_levels";
    public const string FavoriteMaskKey = "meta_favorite_mask";
    public const string AwakeningLevelsKey = "meta_awakening_levels";

    const int UnitCount = 28;

    public static int LoadMedals()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MedalsKey, 0));
    }

    public static void SaveMedals(int amount)
    {
        PlayerPrefs.SetInt(MedalsKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public static int LoadDiscoverMask()
    {
        if (PlayerPrefs.HasKey(DiscoverMaskKey))
        {
            return PlayerPrefs.GetInt(DiscoverMaskKey, 0);
        }

        return PlayerPrefs.GetInt(UnlockMaskKey, 0);
    }

    public static void SaveDiscoverMask(int mask)
    {
        PlayerPrefs.SetInt(DiscoverMaskKey, mask);
        PlayerPrefs.SetInt(UnlockMaskKey, mask);
        PlayerPrefs.Save();
    }

    public static int LoadUpgradeUnlockMask()
    {
        if (PlayerPrefs.HasKey(UpgradeUnlockMaskKey))
        {
            return PlayerPrefs.GetInt(UpgradeUnlockMaskKey, 0);
        }

        return PlayerPrefs.GetInt(UnlockMaskKey, 0);
    }

    public static void SaveUpgradeUnlockMask(int mask)
    {
        PlayerPrefs.SetInt(UpgradeUnlockMaskKey, mask);
        PlayerPrefs.Save();
    }

    public static int LoadBoardPlacedMask() => PlayerPrefs.GetInt(BoardPlacedMaskKey, 0);

    public static void SaveBoardPlacedMask(int mask)
    {
        PlayerPrefs.SetInt(BoardPlacedMaskKey, mask);
        PlayerPrefs.Save();
    }

    public static int LoadEvo3Mask() => PlayerPrefs.GetInt(Evo3MaskKey, 0);

    public static void SaveEvo3Mask(int mask)
    {
        PlayerPrefs.SetInt(Evo3MaskKey, mask);
        PlayerPrefs.Save();
    }

    public static int LoadEvo5Mask() => PlayerPrefs.GetInt(Evo5MaskKey, 0);

    public static void SaveEvo5Mask(int mask)
    {
        PlayerPrefs.SetInt(Evo5MaskKey, mask);
        PlayerPrefs.Save();
    }

    public static void LoadAttackLevels(int[] buffer) => LoadLevelArray(AttackLevelsKey, buffer);

    public static void SaveAttackLevels(int[] levels) => SaveLevelArray(AttackLevelsKey, levels);

    public static void LoadMobilityLevels(int[] buffer) => LoadLevelArray(MobilityLevelsKey, buffer);

    public static void SaveMobilityLevels(int[] levels) => SaveLevelArray(MobilityLevelsKey, levels);

    public static void LoadSpecialtyLevels(int[] buffer) => LoadLevelArray(SpecialtyLevelsKey, buffer);

    public static void SaveSpecialtyLevels(int[] levels) => SaveLevelArray(SpecialtyLevelsKey, levels);

    public static int LoadFavoriteMask() => PlayerPrefs.GetInt(FavoriteMaskKey, 0);

    public static void SaveFavoriteMask(int mask)
    {
        PlayerPrefs.SetInt(FavoriteMaskKey, mask);
        PlayerPrefs.Save();
    }

    public static void LoadAwakeningLevels(int[] buffer) => LoadLevelArray(AwakeningLevelsKey, buffer);

    public static void SaveAwakeningLevels(int[] levels) => SaveLevelArray(AwakeningLevelsKey, levels);

    static void LoadLevelArray(string key, int[] buffer)
    {
        EnsureBuffer(buffer);
        string raw = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }
            return;
        }

        string[] parts = raw.Split('|');
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i < parts.Length && int.TryParse(parts[i], out int value) ? Mathf.Max(0, value) : 0;
        }
    }

    static void SaveLevelArray(string key, int[] levels)
    {
        EnsureBuffer(levels);
        string[] parts = new string[UnitCount];
        for (int i = 0; i < UnitCount; i++)
        {
            parts[i] = levels[i].ToString();
        }
        PlayerPrefs.SetString(key, string.Join("|", parts));
        PlayerPrefs.Save();
    }

    static void EnsureBuffer(int[] buffer)
    {
        if (buffer == null || buffer.Length != UnitCount)
        {
            throw new System.ArgumentException($"Level buffer must be length {UnitCount}.");
        }
    }
}
