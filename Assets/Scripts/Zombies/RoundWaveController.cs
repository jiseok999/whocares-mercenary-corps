using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 라운드 시작 시 한 번에 스폰할 몬스터 목록을 구성합니다 (rounds.csv 기준).
/// </summary>
public static class RoundWaveController
{
    enum SpawnRole
    {
        Tanking = 0,
        GeneralOrFast = 1,
        Ranged = 2
    }

    enum PartyType
    {
        Swarm,
        Bruiser,
        Breakthrough,
        Ranged,
        Balanced,
        Boss
    }

    static readonly PartyType[] NonBossPartyPool =
    {
        PartyType.Swarm,
        PartyType.Bruiser,
        PartyType.Breakthrough,
        PartyType.Ranged,
        PartyType.Balanced
    };

    static readonly Dictionary<int, PartyType> CachedPartyByRound = new Dictionary<int, PartyType>();
    static readonly HashSet<int> GeneratedBlockStarts = new HashSet<int>();
    static PartyType? _lastNonBossParty;

    /// <summary>1~10: 1~4번, 11~20: +5~7번, 21~: +8~10번 적 사용 가능</summary>
    static int GetMaxEnemyNumber(int round)
    {
        if (round <= 10) return 4;
        if (round <= 20) return 7;
        return 10;
    }

    static int GetEnemyTier(int round)
    {
        if (round <= 10) return 1;
        if (round <= 20) return 2;
        return 3;
    }

    static bool TryParseMobNumber(string uid, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(uid) || !uid.StartsWith("mon_pig_")) return false;
        return int.TryParse(uid.Substring("mon_pig_".Length), out number);
    }

    static bool IsMobAllowedForRound(string uid, int round)
    {
        if (!TryParseMobNumber(uid, out int mobNumber)) return true;
        return mobNumber >= 1 && mobNumber <= GetMaxEnemyNumber(round);
    }

    static void AddMob(List<string> wave, int round, string uid, int baseCount)
    {
        if (!IsMobAllowedForRound(uid, round)) return;
        AddMany(wave, uid, ScaledCount(baseCount, round));
    }

    public static List<string> BuildWaveForRound(int round, RoundSpawnLoader loader)
    {
        if (round <= 3)
        {
            CachedPartyByRound[round] = PartyType.Balanced;
            var earlyWave = BuildEarlyTutorialWave(round);
            ApplyRolePriorityOrdering(earlyWave);
            Debug.Log($"라운드 {round} 파티 타입: {PartyType.Balanced}, 소환 수: {earlyWave.Count}");
            return earlyWave;
        }

        PartyType party = GetPartyTypeForRound(round);
        List<string> wave = BuildPartyWave(round, party);
        if (party != PartyType.Boss)
        {
            ApplyRolePriorityOrdering(wave);
        }
        Debug.Log($"라운드 {round} 파티 타입: {party}, 소환 수: {wave.Count}");
        return wave;
    }

    public static string GetPartyDisplayNameForRound(int round)
    {
        return GetPartyDisplayName(GetPartyTypeForRound(round));
    }

    /// <summary>라운드 HUD 상단에 표시할 짧은 파티명 (언어별 길이·레이아웃 대응).</summary>
    public static string GetPartyHudDisplayNameForRound(int round)
    {
        return GetPartyHudDisplayName(GetPartyTypeForRound(round));
    }

    public static string GetPartyTooltipForRound(int round)
    {
        PartyType party = GetPartyTypeForRound(round);
        int totalCount = BuildPartyWave(round, party).Count;
        string detail = GetPartyDetailText(party);
        return GameLocalization.PartyTooltipFormat(totalCount, detail);
    }

    public static string GetPartyIconResourceForRound(int round)
    {
        return GetPartyIconResource(GetPartyTypeForRound(round));
    }

    /// <summary>에디터/디버그용 — min~max 평균으로 예상 구성을 반환합니다.</summary>
    public static Dictionary<string, int> GetExpectedComposition(int round, RoundSpawnLoader loader)
    {
        var result = new Dictionary<string, int>();
        List<string> wave = BuildPartyWave(round, GetPartyTypeForRound(round));
        for (int i = 0; i < wave.Count; i++)
        {
            string uid = wave[i];
            if (result.ContainsKey(uid))
            {
                result[uid] += 1;
            }
            else
            {
                result[uid] = 1;
            }
        }
        return result;
    }

    static List<string> BuildLegacyCsvWave(int round, RoundSpawnLoader loader)
    {
        var wave = new List<string>();
        if (loader == null)
        {
            Debug.LogWarning("RoundSpawnLoader가 없어 웨이브를 구성할 수 없습니다.");
            return wave;
        }

        RoundSpawnData data = loader.GetRoundSpawnData(round);
        if (data == null || data.spawns.Count == 0)
        {
            Debug.LogWarning($"라운드 {round} 스폰 데이터가 없습니다.");
            return wave;
        }

        foreach (SpawnInfo spawn in data.spawns)
        {
            if (spawn == null || string.IsNullOrEmpty(spawn.zombieUID)) continue;
            int count = Random.Range(spawn.minCount, spawn.maxCount + 1);
            AddMany(wave, spawn.zombieUID, count);
        }

        return wave;
    }

    static PartyType GetPartyTypeForRound(int round)
    {
        if (round <= 3)
        {
            CachedPartyByRound[round] = PartyType.Balanced;
            return PartyType.Balanced;
        }

        if (CachedPartyByRound.TryGetValue(round, out PartyType cached))
        {
            return cached;
        }

        EnsureBlockPlanGenerated(round);
        if (CachedPartyByRound.TryGetValue(round, out cached))
        {
            return cached;
        }

        // 안전 fallback (예상 경로에서는 도달하지 않음)
        PartyType fallback = (round % 10 == 0) ? PartyType.Boss : PickRandomNonBossParty();
        CachedPartyByRound[round] = fallback;
        return fallback;
    }

    static void EnsureBlockPlanGenerated(int round)
    {
        int safeRound = Mathf.Max(1, round);
        int blockStart = ((safeRound - 1) / 10) * 10 + 1;
        if (GeneratedBlockStarts.Contains(blockStart))
        {
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            int targetRound = blockStart + i;
            PartyType selected = i == 9
                ? PartyType.Boss
                : PickRandomNonBossParty();
            CachedPartyByRound[targetRound] = selected;
        }

        GeneratedBlockStarts.Add(blockStart);
    }

    static PartyType PickRandomNonBossParty()
    {
        PartyType pick = NonBossPartyPool[Random.Range(0, NonBossPartyPool.Length)];

        if (_lastNonBossParty.HasValue && NonBossPartyPool.Length > 1)
        {
            int guard = 0;
            while (pick == _lastNonBossParty.Value && guard < 8)
            {
                pick = NonBossPartyPool[Random.Range(0, NonBossPartyPool.Length)];
                guard++;
            }
        }

        _lastNonBossParty = pick;
        return pick;
    }

    static List<string> BuildEarlyTutorialWave(int round)
    {
        var wave = new List<string>(4);
        switch (round)
        {
            case 1:
                AddMany(wave, "mon_pig_1", 1);
                break;
            case 2:
                AddMany(wave, "mon_pig_1", 2);
                break;
            default:
                AddMany(wave, "mon_pig_1", 2);
                AddMany(wave, "mon_pig_4", 1);
                break;
        }
        return wave;
    }

    static List<string> BuildPartyWave(int round, PartyType party)
    {
        var wave = new List<string>(64);
        bool shouldShuffle = true;
        int tier = GetEnemyTier(round);

        switch (party)
        {
            case PartyType.Swarm:
                BuildSwarmWave(wave, round, tier);
                break;

            case PartyType.Bruiser:
                BuildBruiserWave(wave, round, tier);
                break;

            case PartyType.Breakthrough:
                BuildBreakthroughWave(wave, round, tier);
                break;

            case PartyType.Ranged:
                BuildRangedWave(wave, round, tier);
                break;

            case PartyType.Balanced:
                BuildBalancedWave(wave, round, tier);
                break;

            case PartyType.Boss:
                AddBossWave(round, wave);
                shouldShuffle = false;
                break;
        }

        if (shouldShuffle)
        {
            Shuffle(wave);
        }
        return wave;
    }

    /// <summary>물량 컨셉 — 물량형 특수 몹 다수 (베이스라인 1번과 시너지)</summary>
    static void BuildSwarmWave(List<string> wave, int round, int tier)
    {
        switch (tier)
        {
            case 1:
                AddMob(wave, round, "mon_pig_1", 8);
                AddMob(wave, round, "mon_pig_4", 3);
                break;
            case 2:
                AddMob(wave, round, "mon_pig_5", 6);
                AddMob(wave, round, "mon_pig_4", 3);
                AddMob(wave, round, "mon_pig_1", 3);
                break;
            default:
                AddMob(wave, round, "mon_pig_5", 5);
                AddMob(wave, round, "mon_pig_8", 4);
                AddMob(wave, round, "mon_pig_4", 3);
                break;
        }
    }

    /// <summary>떡대 컨셉 — 탱커 특수 몹 전열 + 원거리 후방</summary>
    static void BuildBruiserWave(List<string> wave, int round, int tier)
    {
        switch (tier)
        {
            case 1:
                AddMob(wave, round, "mon_pig_3", 4);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_4", 2);
                break;
            case 2:
                AddMob(wave, round, "mon_pig_3", 3);
                AddMob(wave, round, "mon_pig_7", 2);
                AddMob(wave, round, "mon_pig_6", 2);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_5", 2);
                break;
            default:
                AddMob(wave, round, "mon_pig_3", 3);
                AddMob(wave, round, "mon_pig_7", 2);
                AddMob(wave, round, "mon_pig_10", 2);
                AddMob(wave, round, "mon_pig_6", 2);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_5", 2);
                break;
        }
    }

    /// <summary>돌파 컨셉 — 고속 돌파 특수 몹 중심</summary>
    static void BuildBreakthroughWave(List<string> wave, int round, int tier)
    {
        switch (tier)
        {
            case 1:
                AddMob(wave, round, "mon_pig_4", 7);
                AddMob(wave, round, "mon_pig_3", 1);
                break;
            case 2:
                AddMob(wave, round, "mon_pig_4", 6);
                AddMob(wave, round, "mon_pig_5", 3);
                AddMob(wave, round, "mon_pig_7", 1);
                break;
            default:
                AddMob(wave, round, "mon_pig_8", 6);
                AddMob(wave, round, "mon_pig_4", 4);
                AddMob(wave, round, "mon_pig_5", 2);
                AddMob(wave, round, "mon_pig_9", 1);
                break;
        }
    }

    /// <summary>원거리 컨셉 — 원거리 특수 몹 화력 + 탱커 전열</summary>
    static void BuildRangedWave(List<string> wave, int round, int tier)
    {
        switch (tier)
        {
            case 1:
                AddMob(wave, round, "mon_pig_2", 6);
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_4", 1);
                break;
            case 2:
                AddMob(wave, round, "mon_pig_2", 5);
                AddMob(wave, round, "mon_pig_6", 4);
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_7", 1);
                break;
            default:
                AddMob(wave, round, "mon_pig_2", 6);
                AddMob(wave, round, "mon_pig_6", 4);
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_7", 1);
                AddMob(wave, round, "mon_pig_10", 1);
                break;
        }
    }

    /// <summary>밸런스 컨셉 — 특수 몹 역할 고르게 혼합</summary>
    static void BuildBalancedWave(List<string> wave, int round, int tier)
    {
        switch (tier)
        {
            case 1:
                AddMob(wave, round, "mon_pig_4", 3);
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_2", 3);
                break;
            case 2:
                AddMob(wave, round, "mon_pig_5", 3);
                AddMob(wave, round, "mon_pig_4", 2);
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_6", 2);
                break;
            default:
                AddMob(wave, round, "mon_pig_5", 3);
                AddMob(wave, round, "mon_pig_4", 2);
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_6", 2);
                AddMob(wave, round, "mon_pig_9", 1);
                AddMob(wave, round, "mon_pig_10", 1);
                break;
        }
    }

    static void AddBossWave(int round, List<string> wave)
    {
        string bossUid = "boss1";
        if (round >= 30) bossUid = "necromancer_boss";
        else if (round >= 20) bossUid = "ooze_boss";

        AddMany(wave, bossUid, 1);

        int tier = GetEnemyTier(round);
        switch (tier)
        {
            case 1:
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_4", 2);
                break;
            case 2:
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_7", 1);
                AddMob(wave, round, "mon_pig_6", 2);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_4", 2);
                AddMob(wave, round, "mon_pig_5", 1);
                break;
            default:
                AddMob(wave, round, "mon_pig_3", 2);
                AddMob(wave, round, "mon_pig_7", 1);
                AddMob(wave, round, "mon_pig_6", 2);
                AddMob(wave, round, "mon_pig_2", 2);
                AddMob(wave, round, "mon_pig_4", 2);
                AddMob(wave, round, "mon_pig_9", 1);
                AddMob(wave, round, "mon_pig_10", 1);
                break;
        }
    }

    static void ApplyRolePriorityOrdering(List<string> wave)
    {
        if (wave == null || wave.Count <= 1) return;

        var tanking = new List<string>(wave.Count);
        var generalOrFast = new List<string>(wave.Count);
        var ranged = new List<string>(wave.Count);

        for (int i = 0; i < wave.Count; i++)
        {
            string uid = wave[i];
            switch (GetSpawnRole(uid))
            {
                case SpawnRole.Tanking:
                    tanking.Add(uid);
                    break;
                case SpawnRole.Ranged:
                    ranged.Add(uid);
                    break;
                default:
                    generalOrFast.Add(uid);
                    break;
            }
        }

        Shuffle(tanking);
        Shuffle(generalOrFast);
        Shuffle(ranged);

        wave.Clear();
        wave.AddRange(tanking);
        wave.AddRange(generalOrFast);
        wave.AddRange(ranged);
    }

    static SpawnRole GetSpawnRole(string uid)
    {
        switch (uid)
        {
            case "mon_pig_3":
            case "mon_pig_7":
            case "mon_pig_10":
            case "boss1":
            case "ooze_boss":
            case "necromancer_boss":
                return SpawnRole.Tanking;
            case "mon_pig_9":
                return SpawnRole.GeneralOrFast;
            case "mon_pig_2":
            case "mon_pig_6":
                return SpawnRole.Ranged;
            default:
                return SpawnRole.GeneralOrFast;
        }
    }

    static int ScaledCount(int baseCount, int round)
    {
        return EnemyCombatStats.ScalePartySpawnCount(baseCount, round);
    }

    static void AddMany(List<string> wave, string uid, int count)
    {
        int safeCount = Mathf.Max(0, count);
        for (int i = 0; i < safeCount; i++)
        {
            wave.Add(uid);
        }
    }

    static void Shuffle(List<string> wave)
    {
        for (int i = wave.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (wave[i], wave[j]) = (wave[j], wave[i]);
        }
    }

    static string GetPartyDisplayName(PartyType party)
    {
        switch (party)
        {
            case PartyType.Swarm: return GameLocalization.PartySwarm;
            case PartyType.Bruiser: return GameLocalization.PartyBruiser;
            case PartyType.Breakthrough: return GameLocalization.PartyBreakthrough;
            case PartyType.Ranged: return GameLocalization.PartyRanged;
            case PartyType.Balanced: return GameLocalization.PartyBalanced;
            case PartyType.Boss: return GameLocalization.PartyBoss;
            default: return GameLocalization.PartyUnknown;
        }
    }

    static string GetPartyHudDisplayName(PartyType party)
    {
        switch (party)
        {
            case PartyType.Swarm: return GameLocalization.PartySwarmHud;
            case PartyType.Bruiser: return GameLocalization.PartyBruiserHud;
            case PartyType.Breakthrough: return GameLocalization.PartyBreakthroughHud;
            case PartyType.Ranged: return GameLocalization.PartyRangedHud;
            case PartyType.Balanced: return GameLocalization.PartyBalancedHud;
            case PartyType.Boss: return GameLocalization.PartyBossHud;
            default: return GameLocalization.PartyUnknownHud;
        }
    }

    static string GetPartyDetailText(PartyType party)
    {
        switch (party)
        {
            case PartyType.Swarm:
                return GameLocalization.PartySwarmDetail;
            case PartyType.Bruiser:
                return GameLocalization.PartyBruiserDetail;
            case PartyType.Breakthrough:
                return GameLocalization.PartyBreakthroughDetail;
            case PartyType.Ranged:
                return GameLocalization.PartyRangedDetail;
            case PartyType.Balanced:
                return GameLocalization.PartyBalancedDetail;
            case PartyType.Boss:
                return GameLocalization.PartyBossDetail;
            default:
                return GameLocalization.PartyInfoUnavailable;
        }
    }

    static string GetPartyIconResource(PartyType party)
    {
        switch (party)
        {
            case PartyType.Balanced: return "round_normal";
            case PartyType.Swarm: return "round_many";
            case PartyType.Ranged: return "round_magic";
            case PartyType.Bruiser: return "round_armer";
            case PartyType.Breakthrough: return "round_penetration";
            case PartyType.Boss: return "round_boss";
            default: return string.Empty;
        }
    }
}
