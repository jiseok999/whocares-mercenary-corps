using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전 유닛(1~28) 진화 마일스톤 (4·6·9성) — 패턴 강화 + 카드/툴팁 문구.
/// </summary>
public static class UnitEvolutionMilestones
{
    public struct MilestoneDef
    {
        public int requiredLevel;
        public string description;
        public bool isMajor;
    }

    public const string ColMilestoneNew4 = "7FFF7F";
    public const string ColMilestoneNew6 = "FFA500";
    public const string ColMilestoneNew9 = "FFD700";

    public static bool HasMilestones(int unitNumber) => GetMilestoneDefinitions(unitNumber).Count > 0;

    public static bool WillUnlockMajorMilestone(int unitNumber, int currentEvolutionLevel, int resultEvolutionLevel)
    {
        if (!HasMilestones(unitNumber)) return false;
        int cur = Mathf.Clamp(currentEvolutionLevel, 1, UnitCombatStats.MaxEvolutionLevel);
        int next = Mathf.Clamp(resultEvolutionLevel, 1, UnitCombatStats.MaxEvolutionLevel);
        List<MilestoneDef> defs = GetMilestoneDefinitions(unitNumber);
        for (int i = 0; i < defs.Count; i++)
        {
            MilestoneDef def = defs[i];
            if (def.requiredLevel > cur && def.requiredLevel <= next) return true;
        }
        return false;
    }

    public static int CountUnlockedMilestonesAtLevel(int unitNumber, int evolutionLevel)
    {
        List<MilestoneDef> defs = GetMilestoneDefinitions(unitNumber);
        int evo = Mathf.Clamp(evolutionLevel, 1, UnitCombatStats.MaxEvolutionLevel);
        int count = 0;
        for (int i = 0; i < defs.Count; i++)
        {
            if (defs[i].requiredLevel <= evo) count++;
        }
        return count;
    }

    public static float GetAttackSpeedMultiplier(int unitNumber, int evolutionLevel)
    {
        int evo = Mathf.Clamp(evolutionLevel, 1, UnitCombatStats.MaxEvolutionLevel);
        switch (unitNumber)
        {
            case 1: return evo >= 6 ? 1.087f : 1f;
            case 2: return evo >= 9 ? 1.064f : 1f;
            case 3: return evo >= 9 ? 1.111f : 1f;
            case 5: return evo >= 9 ? 1.087f : 1f;
            case 6: return evo >= 9 ? 1.111f : 1f;
            case 8: return evo >= 6 ? 1.087f : 1f;
            case 9: return evo >= 6 ? 1.087f : 1f;
            case 11: return evo >= 9 ? 1.111f : 1f;
            case 12: return evo >= 6 ? 1.136f : 1f;
            case 14: return evo >= 9 ? 1.111f : 1f;
            case 17: return evo >= 6 ? 1.087f : 1f;
            case 20: return evo >= 9 ? 1.087f : 1f;
            case 27: return evo >= 9 ? 1.087f : 1f;
            default: return 1f;
        }
    }

    public static float GetEffectiveAttackInterval(int unitNumber, int evolutionLevel)
    {
        float baseInterval = UnitCombatStats.GetAttackIntervalForUnit(unitNumber);
        if (baseInterval <= 0f) return baseInterval;
        float mul = GetAttackSpeedMultiplier(unitNumber, evolutionLevel);
        return baseInterval / Mathf.Max(0.001f, mul);
    }

    // ── 유닛 1 ──
    public static bool HasUnit1Afterimage(int evo) => evo >= 4;
    public static bool HasUnit1SideShots(int evo) => evo >= 9;
    public const float Unit1AfterimageDelay = 0.12f;
    public const float Unit1AfterimageDamageMul = 0.75f;
    public const float Unit1SideAngleDeg = 12f;
    public const float Unit1SideDamageMul = 0.60f;

    // ── 유닛 2 ──
    public static bool HasUnit2SecondaryBlast(int evo) => evo >= 4;
    public static float GetUnit2AreaSize(int evo) => evo >= 6 ? 3.5f : 3f;
    public static bool HasUnit2TwinShot(int evo) => evo >= 9;
    public const float Unit2SecondaryBlastMul = 0.70f;
    public const float Unit2TwinShotDelay = 0.1f;
    public const float Unit2SecondShotDamageMul = 0.70f;

    // ── 유닛 3 ──
    public static float GetUnit3ChainRange(int evo) => evo >= 4 ? 6.5f : 5f;
    public static int GetUnit3MaxChainCount(int evo) => evo >= 4 ? 2 : 1;
    public static float GetUnit3ChainDamageMul(int evo) => evo >= 9 ? 1f : 0.85f;
    public static bool HasUnit3DoubleTap(int evo) => evo >= 6;
    public const float Unit3DoubleTapDelay = 0.2f;
    public const float Unit3SecondShotDamageMul = 0.80f;

    // ── 유닛 4 ──
    public static float GetUnit4GoldCooldown(int evo) => evo >= 4 ? 3.5f : 4f;
    public static int GetUnit4GoldAmount(int evo) => evo >= 6 ? 3 : 1;
    public static bool HasUnit4AutoCollect(int evo) => evo >= 9;

    // ── 유닛 5 ──
    public static int GetUnit5StrikeCount(int evo)
    {
        if (evo >= 9) return 3;
        if (evo >= 4) return 2;
        return 1;
    }
    public static float GetUnit5StrikeRadius(int evo) => evo >= 6 ? 1.4f : 1.2f;
    public const float Unit5ExtraStrikeDamageMul = 0.70f;
    public const float Unit5StrikeStagger = 0.15f;

    // ── 유닛 6 ──
    public static bool HasUnit6TwinShot(int evo) => evo >= 4;
    public const float Unit6TwinShotDelay = 0.1f;
    public const float Unit6SecondShotDamageMul = 0.75f;
    public static float GetUnit6SlowChance(int evo) => evo >= 6 ? 0.65f : 0.5f;
    public static float GetUnit6SlowDuration(int evo) => evo >= 6 ? 2.5f : 2f;
    public static bool HasUnit6BonusSlowShot(int evo) => evo >= 9;
    public const float Unit6BonusSlowShotDamageMul = 0.50f;

    // ── 유닛 7 ──
    public static float GetUnit7Height(int evo) => evo >= 4 ? 2.4f : 2f;
    public static float GetUnit7Knockback(int evo) => evo >= 6 ? 0.575f : 0.5f;
    public static bool HasUnit7SecondWave(int evo) => evo >= 9;
    public const float Unit7SecondWaveDelay = 0.18f;
    public const float Unit7SecondWaveDamageMul = 0.65f;

    // ── 유닛 8 ──
    public static float GetUnit8LaserDuration(int evo) => evo >= 4 ? 3.5f : 3f;
    public static float GetUnit8TickInterval(int evo) => evo >= 6 ? 0.425f : 0.5f;
    public static bool HasUnit8ResidualSplash(int evo) => evo >= 9;
    public const float Unit8ResidualSplashMul = 0.50f;
    public const float Unit8ResidualSplashRadius = 1.2f;

    // ── 유닛 9 ──
    public static bool HasUnit9WideSpread(int evo) => evo >= 4;
    public static bool HasUnit9TrailingShot(int evo) => evo >= 9;
    public const float Unit9OuterSpreadDamageMul = 0.70f;
    public const float Unit9TrailingDelay = 0.15f;
    public const float Unit9TrailingDamageMul = 0.90f;

    // ── 유닛 10 ──
    public static float GetUnit10PadAttackSpeedMul(int evo) =>
        GetLinearEvolutionMultiplier(evo, 1.05f, 1.10f);

    public static bool HasUnit10SplitShot(int evo) => evo >= 4;
    public static bool HasUnit10NeighborSplash(int evo) => evo >= 6;
    public static bool HasUnit10TwinShot(int evo) => evo >= 9;
    public const float Unit10NeighborSplashMul = 0.10f;
    public const float Unit10TwinShotDelay = 0.08f;
    public const float Unit10SecondShotDamageMul = 0.75f;

    // ── 유닛 11 ──
    public const int Unit11MaxFreezeTargets = 3;
    public static int GetUnit11MaxFreezeTargets(int evo) => evo >= 6 ? 4 : Unit11MaxFreezeTargets;
    public static float GetUnit11FreezeDuration(int evo) => evo >= 4 ? 2.5f : 2f;
    public static float GetUnit11PlateWidthMul(int evo) => evo >= 6 ? 1.25f : 1f;
    public static float GetUnit11HoldDuration(int evo) => evo >= 9 ? 1.2f : 1f;

    // ── 유닛 12 ──
    public static int GetUnit12ChainCount(int evo) => evo >= 4 ? 3 : 2;
    public static float GetUnit12ChainDamageRatio(int evo) => evo >= 4 ? 0.60f : 0.50f;
    public static float GetUnit12StunDuration(int evo) => evo >= 9 ? 0.5f : 0.35f;
    public static bool HasUnit12DoubleLance(int evo) => evo >= 9;
    public const float Unit12DoubleLanceDelay = 0.25f;
    public const float Unit12SecondLanceDamageMul = 0.70f;

    // ── 유닛 13 ──
    public static float GetUnit13PierceMul(int evo) => evo >= 4 ? 0.35f : 0.30f;
    public static float GetUnit13HitRadiusMul(int evo) => evo >= 6 ? 1.15f : 1f;
    public static int GetUnit13ReturnLegs(int evo) => evo >= 9 ? 2 : 1;

    // ── 유닛 14 ──
    public static bool HasUnit14TwinBurst(int evo) => evo >= 4;
    public const float Unit14TwinBurstDelay = 0.08f;
    public const float Unit14SecondBurstDamageMul = 0.80f;
    public static bool HasUnit14ThirdTarget(int evo) => evo >= 6;
    public const float Unit14ThirdTargetDamageMul = 0.65f;
    public static int GetUnit14ExtraRepeatBounces(int evo) => evo >= 9 ? 1 : 0;

    // ── 유닛 15 ──
    public static int GetUnit15FanDirections(int evo) => evo >= 4 ? 6 : 4;
    public static float GetUnit15SlowChance(int evo) => evo >= 6 ? 0.70f : 0.5f;
    public static bool HasUnit15DoubleMain(int evo) => evo >= 9;
    public const float Unit15DoubleMainDelay = 0.1f;
    public const float Unit15SecondMainDamageMul = 0.75f;

    // ── 유닛 16 ──
    public static int GetUnit16AuraCellSpan(int evo) => evo >= 9 ? 2 : 1;
    public static float GetUnit16AuraDamageMul(int evo) =>
        evo >= 1 ? GetLinearEvolutionMultiplier(evo, 1.05f, 1.10f) : 1f;
    public static float GetUnit16AuraAttackSpeedMul(int evo) =>
        evo >= 1 ? GetLinearEvolutionMultiplier(evo, 1.01f, 1.05f) : 1f;

    // ── 유닛 17 ──
    public static float GetUnit17AoeRadius(int evo) => evo >= 4 ? 1.8f : 1.6f;
    public static bool HasUnit17ComboPunch(int evo) => evo >= 9;
    public const float Unit17SecondPunchDelay = 0.2f;
    public const float Unit17SecondPunchDamageMul = 0.70f;

    // ── 유닛 18 ──
    public static int GetUnit18ArrowCount(int evo) => evo >= 4 ? 3 : 2;
    public static float GetUnit18StunDuration(int evo) => evo >= 6 ? 3.5f : 3f;
    public static float GetUnit18ArrowDamageMul(int evo) => evo >= 9 ? 1.15f : 1f;
    public const float Unit18ExtraArrowDamageMul = 0.70f;

    // ── 유닛 19 ──
    public static float GetUnit19PadWidthMul(int evo)
    {
        if (evo >= 9) return 1.0f;
        if (evo >= 6) return 0.78f;
        if (evo >= 4) return 0.58f;
        return 0.40f;
    }

    public static float GetUnit19DamageTickMul(int evo) => evo >= 4 ? 1.20f : 1f;
    public static float GetUnit19TickInterval(int evo) => evo >= 6 ? 0.85f : 1f;
    public static bool HasUnit19SlowOnPad(int evo) => evo >= 9;
    public const float Unit19SlowAmount = 0.15f;

    // ── 유닛 20 ──
    public static int GetUnit20TargetCount(int evo) => evo >= 4 ? 4 : 3;
    public static float GetUnit20LaserDuration(int evo) => evo >= 6 ? 3.5f : 3f;
    public const float Unit20FourthTargetDamageMul = 0.75f;

    // ── 유닛 21 ──
    public static float GetUnit21SpinDuration(int evo) => evo >= 4 ? 3.5f : 3f;
    public static float GetUnit21BladeRadiusMul(int evo) => evo >= 6 ? 1.20f : 1f;
    public static bool HasUnit21SecondVortex(int evo) => evo >= 9;
    public const float Unit21SecondVortexDelay = 0.35f;
    public const float Unit21SecondVortexDamageMul = 0.65f;

    // ── 유닛 22 ──
    public static float GetUnit22RadiusMul(int evo) => evo >= 4 ? 1.20f : 1f;
    public const int Unit22MaxStunTargets = 4;
    public static float GetUnit22StunDuration(int evo) => evo >= 6 ? 2.5f : 2f;
    public static bool HasUnit22TwinPad(int evo) => evo >= 9;
    public const float Unit22TwinPadDelay = 0.25f;
    public const float Unit22SecondPadDamageMul = 0.70f;

    // ── 유닛 23 ──
    public static float GetUnit23PadDamageFraction(int evo) => evo >= 4 ? 0.40f : 0.30f;
    public static float GetUnit23PadPersistMul(int evo) => evo >= 6 ? 1.35f : 1f;
    public static bool HasUnit23EndExplosion(int evo) => evo >= 9;
    public const float Unit23EndExplosionRadius = 1.4f;
    public const float Unit23EndExplosionMul = 0.80f;

    // ── 유닛 24 ──
    public static float GetUnit24LifetimeMul(int evo) => evo >= 4 ? 1.25f : 1f;
    public static float GetUnit24ContactDamageMul(int evo) => evo >= 6 ? 1.15f : 1f;
    public static bool HasUnit24SpeedRamp(int evo) => evo >= 9;
    public const float Unit24SpeedRampPerSec = 0.12f;

    // ── 유닛 25 ──
    public static float GetUnit25BeamDuration(int evo) => evo >= 4 ? 3.5f : 3f;
    public static float GetUnit25BeamWidthMul(int evo) => evo >= 6 ? 1.25f : 1f;
    public static bool HasUnit25Afterglow(int evo) => evo >= 9;
    public const float Unit25AfterglowDuration = 1.5f;
    public const float Unit25AfterglowDamageMul = 0.40f;

    // ── 유닛 26 ──
    public static float GetUnit26LifetimeMul(int evo) => evo >= 4 ? 1.25f : 1f;
    public static float GetUnit26ContactDamageMul(int evo) => evo >= 6 ? 1.15f : 1f;
    public static bool HasUnit26SpeedRamp(int evo) => evo >= 9;
    public const float Unit26SpeedRampPerSec = 0.12f;

    // ── 유닛 27 ──
    public static int GetUnit27RoamHitsToChase(int evo) => evo >= 4 ? 3 : 2;
    public static float GetUnit27StickyInterval(int evo) => evo >= 6 ? 0.85f : 1f;
    public static float GetUnit27ChaseSpeedMul(int evo) => evo >= 9 ? 1.50f : 1.25f;

    // ── 유닛 28 (소용돌이 CC 밸런스) ──
    public const float Unit28PullRadiusCellFactor = 1.3f;
    public const float Unit28MinClusterRadiusCellFactor = 0.9f;
    public const float Unit28MaxPullDistanceCellFactor = 1.5f;
    public const int Unit28MaxPullTargets = 5;
    public const float Unit28BasePullSpeed = 2.25f;
    public static float GetUnit28StayDuration(int evo) => evo >= 4 ? 2.5f : 2f;
    public static float GetUnit28PullSpeedMul(int evo) => evo >= 6 ? 1.20f : 1f;
    public static bool HasUnit28LingeringAura(int evo) => evo >= 9;
    public const float Unit28LingerDuration = 1.5f;
    public const float Unit28LingerDamageMul = 0.50f;

    public static List<MilestoneDef> GetMilestoneDefinitions(int unitNumber)
    {
        var list = new List<MilestoneDef>(3);
        switch (unitNumber)
        {
            case 1: list.Add(M(1, 4, true)); list.Add(M(1, 6, false)); list.Add(M(1, 9, true)); break;
            case 2: list.Add(M(2, 4, true)); list.Add(M(2, 6, false)); list.Add(M(2, 9, true)); break;
            case 3: list.Add(M(3, 4, true)); list.Add(M(3, 6, true)); list.Add(M(3, 9, true)); break;
            case 4: list.Add(M(4, 4, true)); list.Add(M(4, 6, true)); list.Add(M(4, 9, true)); break;
            case 5: list.Add(M(5, 4, true)); list.Add(M(5, 6, false)); list.Add(M(5, 9, true)); break;
            case 6: list.Add(M(6, 4, true)); list.Add(M(6, 6, true)); list.Add(M(6, 9, true)); break;
            case 7: list.Add(M(7, 4, false)); list.Add(M(7, 6, false)); list.Add(M(7, 9, true)); break;
            case 8: list.Add(M(8, 4, true)); list.Add(M(8, 6, true)); list.Add(M(8, 9, true)); break;
            case 9: list.Add(M(9, 4, true)); list.Add(M(9, 6, false)); list.Add(M(9, 9, true)); break;
            case 10: list.Add(M(10, 4, true)); list.Add(M(10, 6, true)); list.Add(M(10, 9, true)); break;
            case 11: list.Add(M(11, 4, true)); list.Add(M(11, 6, false)); list.Add(M(11, 9, true)); break;
            case 12: list.Add(M(12, 4, true)); list.Add(M(12, 6, false)); list.Add(M(12, 9, true)); break;
            case 13: list.Add(M(13, 4, false)); list.Add(M(13, 6, false)); list.Add(M(13, 9, true)); break;
            case 14: list.Add(M(14, 4, true)); list.Add(M(14, 6, true)); list.Add(M(14, 9, true)); break;
            case 15: list.Add(M(15, 4, true)); list.Add(M(15, 6, false)); list.Add(M(15, 9, true)); break;
            case 16: list.Add(M(16, 4, true)); list.Add(M(16, 6, true)); list.Add(M(16, 9, true)); break;
            case 17: list.Add(M(17, 4, false)); list.Add(M(17, 6, false)); list.Add(M(17, 9, true)); break;
            case 18: list.Add(M(18, 4, true)); list.Add(M(18, 6, true)); list.Add(M(18, 9, true)); break;
            case 19: list.Add(M(19, 4, true)); list.Add(M(19, 6, true)); list.Add(M(19, 9, true)); break;
            case 20: list.Add(M(20, 4, true)); list.Add(M(20, 6, true)); list.Add(M(20, 9, false)); break;
            case 21: list.Add(M(21, 4, true)); list.Add(M(21, 6, false)); list.Add(M(21, 9, true)); break;
            case 22: list.Add(M(22, 4, false)); list.Add(M(22, 6, true)); list.Add(M(22, 9, true)); break;
            case 23: list.Add(M(23, 4, false)); list.Add(M(23, 6, true)); list.Add(M(23, 9, true)); break;
            case 24: list.Add(M(24, 4, false)); list.Add(M(24, 6, false)); list.Add(M(24, 9, true)); break;
            case 25: list.Add(M(25, 4, true)); list.Add(M(25, 6, false)); list.Add(M(25, 9, true)); break;
            case 26: list.Add(M(26, 4, false)); list.Add(M(26, 6, false)); list.Add(M(26, 9, true)); break;
            case 27: list.Add(M(27, 4, true)); list.Add(M(27, 6, true)); list.Add(M(27, 9, true)); break;
            case 28: list.Add(M(28, 4, true)); list.Add(M(28, 6, false)); list.Add(M(28, 9, true)); break;
        }
        return list;
    }

    static MilestoneDef M(int unitNumber, int level, bool major) =>
        new MilestoneDef
        {
            requiredLevel = level,
            description = GameLocalization.GetEvolutionMilestoneDescription(unitNumber, level),
            isMajor = major
        };

    public static string BuildMilestoneCardSectionRich(int unitNumber, bool isNewUnit, int currentEvolutionLevel, int resultEvolutionLevel)
    {
        if (!HasMilestones(unitNumber)) return string.Empty;

        List<MilestoneDef> defs = GetMilestoneDefinitions(unitNumber);
        if (defs.Count == 0) return string.Empty;

        bool willUnlockMajor = WillUnlockMajorMilestone(unitNumber, currentEvolutionLevel, resultEvolutionLevel);
        string sectionTitle = isNewUnit
            ? GameLocalization.EvolutionTraitSectionUnlock
            : willUnlockMajor
                ? GameLocalization.EvolutionTraitSectionBoost
                : GameLocalization.EvolutionTraitSectionDefault;
        string titleColor = willUnlockMajor ? ColMilestoneNew9 : UnitCombatStats.ColPatternTitle;
        int titleSize = willUnlockMajor ? 18 : 16;

        var lines = new List<string>(defs.Count + 2)
        {
            $"<size={titleSize}><color=#{titleColor}><b>{sectionTitle}</b></color></size>"
        };

        if (isNewUnit)
        {
            for (int i = 0; i < defs.Count; i++)
            {
                MilestoneDef def = defs[i];
                string star = UnitCombatStats.FormatEvolutionStars(def.requiredLevel).Replace("\n", "");
                string lineColor = def.requiredLevel >= 9 ? ColMilestoneNew9 : def.requiredLevel >= 6 ? ColMilestoneNew6 : ColMilestoneNew4;
                int size = def.requiredLevel >= 9 ? 17 : def.requiredLevel >= 6 ? 16 : 15;
                lines.Add($"<size={size}><color=#{lineColor}><b>{star}</b></color></size>  <color=#{UnitCombatStats.ColPatternBody}>{def.description}</color>");
            }
            return string.Join("\n", lines);
        }

        int cur = Mathf.Clamp(currentEvolutionLevel, 1, UnitCombatStats.MaxEvolutionLevel);
        int next = Mathf.Clamp(resultEvolutionLevel, 1, UnitCombatStats.MaxEvolutionLevel);

        for (int i = 0; i < defs.Count; i++)
        {
            MilestoneDef def = defs[i];
            if (def.requiredLevel > next) continue;

            bool isNew = def.requiredLevel > cur;
            string star = UnitCombatStats.FormatEvolutionStars(def.requiredLevel);
            if (isNew)
            {
                lines.Add(FormatNewMilestoneLine(def, star));
            }
            else
            {
                lines.Add($"{star}  <color=#{UnitCombatStats.ColPatternBody}>{def.description}</color>");
            }
        }

        float curInterval = GetEffectiveAttackInterval(unitNumber, cur);
        float nextInterval = GetEffectiveAttackInterval(unitNumber, next);
        if (curInterval > 0f && nextInterval > 0f && !Mathf.Approximately(curInterval, nextInterval))
        {
            lines.Add($"<color=#{UnitCombatStats.ColLabel}>{GameLocalization.EvolutionAttackIntervalLabel}</color>  " +
                      $"<color=#{UnitCombatStats.ColSub}>{curInterval:0.#}{GameLocalization.StatSecondUnit}</color> → " +
                      $"<b><color=#{UnitCombatStats.ColSpeed}>{nextInterval:0.#}{GameLocalization.StatSecondUnit}</color></b>");
        }

        return string.Join("\n", lines);
    }

    static string FormatNewMilestoneLine(MilestoneDef def, string star)
    {
        string color = def.requiredLevel >= 9 ? ColMilestoneNew9 : def.requiredLevel >= 6 ? ColMilestoneNew6 : ColMilestoneNew4;
        int badgeSize = def.requiredLevel >= 9 ? 20 : def.requiredLevel >= 6 ? 18 : 17;
        int bodySize = def.requiredLevel >= 9 ? 17 : def.requiredLevel >= 6 ? 16 : 15;
        string badge = def.requiredLevel >= 9
            ? GameLocalization.EvolutionNewBadge9
            : def.requiredLevel >= 6
                ? GameLocalization.EvolutionNewBadge6
                : GameLocalization.EvolutionNewBadge4;
        return $"<size={badgeSize}><color=#{color}><b>▶ {badge}</b></color></size> {star}\n" +
               $"<size={bodySize}><color=#{UnitCombatStats.ColPatternBody}><b>{def.description}</b></color></size>";
    }

    /// <summary>1진→9진 선형 보간. 진화마다 소수점 단위로 버프 배율 상승.</summary>
    static float GetLinearEvolutionMultiplier(int evolutionLevel, float multiplierAt1, float multiplierAt9)
    {
        int maxEvo = UnitCombatStats.MaxEvolutionLevel;
        int evo = Mathf.Clamp(evolutionLevel, 1, maxEvo);
        if (maxEvo <= 1 || Mathf.Approximately(multiplierAt1, multiplierAt9))
        {
            return multiplierAt1;
        }

        float t = (evo - 1f) / (maxEvo - 1f);
        return Mathf.Lerp(multiplierAt1, multiplierAt9, t);
    }
}
