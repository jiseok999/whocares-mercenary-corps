using UnityEngine;

/// <summary>
/// 적 몬스터·보스 기준 스탯 및 라운드 스케일 (아군 UnitCombatStats·조합 밸런스 기준)
/// </summary>
public static class EnemyCombatStats
{
    public const int MobScalingStartRound = 4;
    // 체력 +30% / 이동속도 -23% 트레이드오프 (탱키하지만 느린 적)
    public const float GlobalHealthScale = 0.65f;
    public const int HealthMilestoneInterval = 3;
    public const float HealthMilestoneMultiplier = 1.25f;
    public const float MobHealthGrowthPerRound = 0.025f;
    public const float MobDamageGrowthPerRound = 0.04f;
    public const float MobSpeedGrowthPerRound = 0.008f;
    public const float MaxMobSpeedMultiplier = 1.12f;

    /// <summary>CSV moveSpeed를 월드 이동속도(유닛/초)로 변환하는 배율.</summary>
    public const float MoveSpeedWorldScale = 0.285f;

    public const float BossHealthGrowthPerRound = 0.0175f;

    // 베이스라인(mon_pig_1) 물량 공세 스케일
    public const float BaselineIntervalDecayPerRound = 0.035f;
    public const float BaselineIntervalMin = 0.65f;
    public const int BaselineBurstRoundStep = 8;
    public const int BaselineBurstMax = 4;

    /// <summary>1번 적(mon_pig_1) 크기·체력 추가 배율.</summary>
    public const float Mob1ScaleMultiplier = 2f / 3f;
    public const float Mob1HealthMultiplier = 2f / 3f;

    /// <summary>돌파(mon_pig_4·8) 이동속도 추가 배율.</summary>
    public const float BreakthroughMoveSpeedMultiplier = 1.5f;

    public static bool IsBreakthroughMob(string uid)
    {
        return uid == "mon_pig_4" || uid == "mon_pig_8";
    }

    public static bool IsBossUid(string uid)
    {
        return uid == "boss1" || uid == "ooze_boss" || uid == "necromancer_boss";
    }

    /// <summary>3·6·9… 라운드 최초 진입 시 토스트를 띄울 구간인지.</summary>
    public static bool IsHealthMilestoneEntryRound(int round)
    {
        return round >= HealthMilestoneInterval && round % HealthMilestoneInterval == 0;
    }

    /// <summary>3라운드마다 1.25배씩 누적되는 체력 구간 배율 (1~2라운드 = ×1).</summary>
    public static float GetHealthMilestoneMultiplier(int round)
    {
        int tier = round / HealthMilestoneInterval;
        if (tier <= 0) return 1f;
        return Mathf.Pow(HealthMilestoneMultiplier, tier);
    }

    /// <summary>잡몹 라운드별 선형 체력 성장(4라운드~).</summary>
    public static float GetMobLinearHealthMultiplier(int round)
    {
        if (round < MobScalingStartRound) return 1f;
        int scaledRounds = round - MobScalingStartRound + 1;
        return 1f + MobHealthGrowthPerRound * scaledRounds;
    }

    /// <summary>잡몹 라운드 스케일 배율(체력) — 절반 기준 + 3라운드 구간 + 선형 성장.</summary>
    public static float GetMobHealthMultiplier(int round)
    {
        return GlobalHealthScale
            * GetHealthMilestoneMultiplier(round)
            * GetMobLinearHealthMultiplier(round);
    }

    public static float GetMobDamageMultiplier(int round)
    {
        if (round < MobScalingStartRound) return 1f;
        int scaledRounds = round - MobScalingStartRound + 1;
        return 1f + MobDamageGrowthPerRound * scaledRounds;
    }

    public static float GetMobSpeedMultiplier(int round)
    {
        if (round < MobScalingStartRound) return 1f;
        int scaledRounds = round - MobScalingStartRound + 1;
        float mul = 1f + MobSpeedGrowthPerRound * scaledRounds;
        return Mathf.Min(mul, MaxMobSpeedMultiplier);
    }

    /// <summary>보스 등장 라운드 기준 추가 체력 배율.</summary>
    public static float GetBossHealthMultiplier(int round, string bossUid)
    {
        int introRound = GetBossIntroRound(bossUid);
        if (round <= introRound) return 1f;
        int extra = round - introRound;
        return 1f + BossHealthGrowthPerRound * extra;
    }

    public static int GetBossIntroRound(string bossUid)
    {
        switch (bossUid)
        {
            case "boss1": return 10;
            case "ooze_boss": return 20;
            case "necromancer_boss": return 30;
            default: return 10;
        }
    }

    public static void ApplyBossBaseStats(ZombieData data, string bossUid)
    {
        if (data == null) return;

        switch (bossUid)
        {
            case "boss1":
                data.zombieName = "Boss1";
                data.resourceName = "Demon_Boss_walk-export";
                data.moveSpeed = 0.85f;
                data.health = 1040;
                data.maxHealth = 1040;
                data.damage = 0;
                data.attackRange = 0.6f;
                data.attackCooldown = 1f;
                break;

            case "ooze_boss":
                data.zombieName = "OozeBoss";
                data.resourceName = "Ooze_boss_walk-export";
                data.moveSpeed = 0.55f;
                data.health = 1680;
                data.maxHealth = 1680;
                data.damage = 2;
                data.attackRange = 1.2f;
                data.attackCooldown = 1f;
                break;

            case "necromancer_boss":
                data.zombieName = "NecromancerBoss";
                data.resourceName = "Necromancer_walk-export";
                data.moveSpeed = 0.88f;
                data.health = 2320;
                data.maxHealth = 2320;
                data.damage = 3;
                data.attackRange = 1.5f;
                data.attackCooldown = 1f;
                break;
        }
    }

    public static void ScaleBossForRound(ZombieData data, string bossUid, int round)
    {
        if (data == null) return;

        float hpMul = GetBossHealthMultiplier(round, bossUid)
            * GlobalHealthScale
            * GetHealthMilestoneMultiplier(round);
        data.health = Mathf.Max(1, Mathf.RoundToInt(data.health * hpMul));
        data.maxHealth = Mathf.Max(data.health, Mathf.RoundToInt(data.maxHealth * hpMul));

        // 후반 보스 벽 압박 소폭 상승
        if (round > GetBossIntroRound(bossUid) && data.damage > 0)
        {
            int bonus = (round - GetBossIntroRound(bossUid)) / 5;
            data.damage = Mathf.Max(1, data.damage + bonus);
        }
    }

    /// <summary>파티별 라운드 수량 보정 — 초반은 완만, 후반으로 갈수록 가속.</summary>
    public static int ScalePartySpawnCount(int baseCount, int round)
    {
        int roundBonus = Mathf.Max(0, round - 4);
        int extra = Mathf.FloorToInt(
            Mathf.Sqrt(Mathf.Max(1, roundBonus)) * 1.2f
            + roundBonus * 0.18f);
        return Mathf.Max(1, baseCount + extra);
    }

    /// <summary>베이스라인(mon_pig_1) 물량 스폰 간격 — 라운드가 오를수록 짧아집니다.</summary>
    public static float GetBaselineSpawnInterval(float baseInterval, int round)
    {
        float interval = baseInterval - BaselineIntervalDecayPerRound * Mathf.Max(0, round - 1);
        return Mathf.Max(BaselineIntervalMin, interval);
    }

    /// <summary>베이스라인 1회 소환 수 — 8라운드마다 +1마리 (최대 4).</summary>
    public static int GetBaselineBurstCount(int round)
    {
        int burst = 1 + Mathf.Max(0, round - 1) / BaselineBurstRoundStep;
        return Mathf.Clamp(burst, 1, BaselineBurstMax);
    }

    public static string GetRoleLabel(string uid)
    {
        switch (uid)
        {
            case "mon_pig_1":
            case "mon_pig_5": return "물량";
            case "mon_pig_2":
            case "mon_pig_6": return "원거리";
            case "mon_pig_3":
            case "mon_pig_7":
            case "mon_pig_10": return "탱커";
            case "mon_pig_4":
            case "mon_pig_8": return "돌파";
            case "mon_pig_9": return "부활";
            case "boss1": return "기동 보스";
            case "ooze_boss": return "점액 보스";
            case "necromancer_boss": return "네크로 보스";
            default: return "일반";
        }
    }
}
