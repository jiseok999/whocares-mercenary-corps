using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 유닛 전투 스탯 (표시·실제 전투 일관) — 등급·역할·조합 밸런스 기준 단일 소스
/// </summary>
public static class UnitCombatStats
{
    public const int MaxEvolutionLevel = 9;

    public enum EvolutionStarTier { White, Blue, Pink }

    // 1진화 x1.0 → 9진화 x2.2, 8구간 지수 분배 (2.2^((n-1)/8))
    static readonly float[] EvolutionStatMultipliers =
    {
        1.000f, 1.104f, 1.220f, 1.347f, 1.483f, 1.633f, 1.797f, 1.979f, 2.200f
    };

    // ── 등급별 기준 (5등급 1~6 → 1등급 24~28) ──
    // 공격력: 등급↑ + 역할(저격·범위·서포트) 미세 조정
    static readonly int[] BaseDamageByUnit =
    {
        12,  8, 14,  8, 12, 11, // 1~6  (5등급) — 2 범위딜 밸런스↓(기존 11의 ~70%), 3 저격↑, 4 경제↓
        15, 13, 16, 15, 13, 16, // 7~12 (4등급) — 8 지속딜↓, 12 엘리트↑
        16, 18, 15, 10, 19, 21, // 13~18(3등급) — 13 어쌔신: 9번 1진화 스펙 / 15 분열↓, 16 서포트↓
        20, 23, 25, 23, 26,     // 19~23(2등급) — 19 장판형
        30, 28, 31, 32, 33      // 24~28(1등급)
    };

    // 체력: 등급↑, 서포트·탱커(4,16,19,27)↑, 저격(3)↓
    static readonly int[] BaseHealthByUnit =
    {
        90, 85,  80, 130, 88,  82, // 5등급
       105, 110, 100, 112, 102, 115, // 4등급
       135, 140, 125, 155, 130, 128, // 3등급
       175, 158, 168, 162, 160,     // 2등급
       195, 205, 188, 215, 210      // 1등급
    };

    // 공격 주기(초): 낮을수록 빠름. 0 = 자동 공격 없음
    static readonly float[] AttackIntervalByUnit =
    {
        2f, 3f, 3.33f, 0f, 3f, 3.33f, // 5등급 — 1·3·6 공속 1.5배
        16f, 7f, 3f, 0f, 10f, 5f, // 4등급 — 7 질풍 마도사: 공격 주기 2배 / 10 윙즈 남작: 공격 없음
        3f, 3f, 2f, 0f, 5f, 8f, // 3등급 — 15 공속 1.5배 / 18 겁쟁이 사냥꾼: 느린 공격 주기
        0f, 7f, 5f, 4f, 5f,     // 2등급 — 21 선풍 필드↓ / 23 관통탄↓
        3f, 5f, 5f, 10f, 10f    // 1등급 — 27 초고화력 저속 / 28 소용돌이 CC↓
    };

    // 사거리(월드 유닛, cellSize≈1칸). 0=전역. 유닛 컨셉별 차등 — 짧은 중거리 5~5.5 / 중거리 6~7.5 / 원거리 8~9.5 / 초원거리 10
    static readonly float[] AttackRangeByUnit =
    {
        5.0f, 6.5f, 10f,   0f,  9.0f,  5.5f, // 5 — 1 보드앞줄 / 3 저격 / 5 무작위 낙뢰
        9.5f, 5.0f,  7.0f,  8.5f,  9.0f,  7.5f, // 4 — 7 레인 직선 / 8 구토빔 / 11 열 장판
        7.5f, 6.0f,  7.5f,  0f,  8.0f, 10f,   // 3 — 13 관통 / 18 파수꾼
         0f, 8.5f,  9.5f,  8.5f,  8.0f,     // 2 — 21 선풍필드 / 22 무작위 장판
        10f,  7.0f,  6.5f, 10f,  10f      // 1 — 24·27·28 초원거리 / 25 정밀빔
    };

    public static void GetEvolutionStarDisplay(int evolutionLevel, out int starCount, out EvolutionStarTier tier)
    {
        int level = Mathf.Clamp(evolutionLevel, 1, MaxEvolutionLevel);
        if (level <= 3)
        {
            starCount = level;
            tier = EvolutionStarTier.White;
        }
        else if (level <= 6)
        {
            starCount = level - 3;
            tier = EvolutionStarTier.Blue;
        }
        else
        {
            starCount = level - 6;
            tier = EvolutionStarTier.Pink;
        }
    }

    public static Color GetEvolutionStarColor(EvolutionStarTier tier)
    {
        switch (tier)
        {
            case EvolutionStarTier.Blue: return new Color(0.45f, 0.78f, 1f, 1f);
            case EvolutionStarTier.Pink: return new Color(1f, 0.45f, 0.85f, 1f);
            default: return Color.white;
        }
    }

    public static string GetEvolutionStarColorHex(EvolutionStarTier tier)
    {
        switch (tier)
        {
            case EvolutionStarTier.Blue: return "6FB8FF";
            case EvolutionStarTier.Pink: return "FF74D9";
            default: return "FFFFFF";
        }
    }

    public static int GetGradeForUnit(int unitNumber)
    {
        if (unitNumber <= 6) return 5;
        if (unitNumber <= 12) return 4;
        if (unitNumber <= 18) return 3;
        if (unitNumber <= 23) return 2;
        if (unitNumber <= 28) return 1;
        return 5;
    }

    public static int GetBaseDamageForUnit(int unitNumber) => GetFromArray(BaseDamageByUnit, unitNumber, 12);
    public static int GetBaseHealthForUnit(int unitNumber) => GetFromArray(BaseHealthByUnit, unitNumber, 90);
    public static float GetAttackIntervalForUnit(int unitNumber) => GetFromArray(AttackIntervalByUnit, unitNumber, 3f);
    public static float GetAttackRangeForUnit(int unitNumber) => GetFromArray(AttackRangeByUnit, unitNumber, 10f);

    public static bool CanAutoAttack(int unitNumber) => GetAttackIntervalForUnit(unitNumber) > 0f;

    static T GetFromArray<T>(T[] arr, int unitNumber, T fallback)
    {
        if (unitNumber < 1 || unitNumber > arr.Length) return fallback;
        return arr[unitNumber - 1];
    }

    public static float GetEvolutionStatMultiplier(int evolutionLevel)
    {
        int idx = Mathf.Clamp(evolutionLevel - 1, 0, EvolutionStatMultipliers.Length - 1);
        return EvolutionStatMultipliers[idx];
    }

    public static float GetEvolutionDamageMultiplier(int evolutionLevel) => GetEvolutionStatMultiplier(evolutionLevel);

    public static int GetEvolutionDamageBonus(int evolutionLevel)
    {
        int baseDamage = 12;
        int scaled = CalculateScaledDamage(baseDamage, evolutionLevel);
        return Mathf.Max(0, scaled - baseDamage);
    }

    public static int CalculateScaledDamage(int baseDamage, int evolutionLevel)
    {
        float mul = GetEvolutionStatMultiplier(evolutionLevel);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mul));
    }

    public static int CalculateCoreDamage(int unitNumber, int evolutionLevel)
    {
        return CalculateScaledDamage(GetBaseDamageForUnit(unitNumber), evolutionLevel);
    }

    public static int CalculateCoreHealth(int unitNumber, int evolutionLevel)
    {
        return CalculateScaledDamage(GetBaseHealthForUnit(unitNumber), evolutionLevel);
    }

    public static float GetAttacksPerSecond(int unitNumber)
    {
        float interval = GetAttackIntervalForUnit(unitNumber);
        return interval > 0f ? 1f / interval : 0f;
    }

    public static int CalculateDisplayDamage(int unitNumber, int evolutionLevel)
    {
        int total = CalculateCoreDamage(unitNumber, evolutionLevel);
        if (unitNumber < 1 || unitNumber > 28) return total;

        int upgradeBonus = CharacterUpgradeData.GetAttackBonus(unitNumber);
        int traitBonus = TraitManager.Instance != null ? TraitManager.Instance.GetDamageBonusForUnit(unitNumber) : 0;
        float traitMul = TraitManager.Instance != null ? TraitManager.Instance.GetDamageMultiplierForUnit(unitNumber) : 1f;
        return Mathf.Max(1, Mathf.RoundToInt((total + upgradeBonus + traitBonus) * traitMul));
    }

    public static string FormatAttackSpeedLine(int unitNumber)
    {
        float interval = GetAttackIntervalForUnit(unitNumber);
        if (interval <= 0f) return $"{GameLocalization.StatAttackSpeed}: —";
        float aps = 1f / interval;
        return $"{GameLocalization.StatAttackSpeed}: {interval:0.#}{GameLocalization.StatSecondUnit}/회 ({aps:0.##}{GameLocalization.StatPerSecond})";
    }

    public static string FormatRangeLine(int unitNumber)
    {
        float range = GetAttackRangeForUnit(unitNumber);
        return range <= 0f ? GameLocalization.StatRangeFormat(0f) : GameLocalization.StatRangeFormat(range);
    }

    public static string FormatDamageLine(int unitNumber, int evolutionLevel)
    {
        int total = CalculateDisplayDamage(unitNumber, evolutionLevel);
        float mul = GetEvolutionStatMultiplier(evolutionLevel);
        return GameLocalization.StatAttackFormat(total, mul);
    }

    public static string FormatCombatStatsBlock(int unitNumber, int evolutionLevel)
    {
        return string.Join("\n",
            FormatDamageLine(unitNumber, evolutionLevel),
            FormatAttackSpeedLine(unitNumber),
            FormatRangeLine(unitNumber));
    }

    public static string FormatEvolutionStars(int evolutionLevel)
    {
        GetEvolutionStarDisplay(evolutionLevel, out int count, out EvolutionStarTier tier);
        string hex = GetEvolutionStarColorHex(tier);
        return $"<color=#{hex}>{new string('★', count)}</color>";
    }

    public static string BuildFieldTooltipText(int unitNumber, int evolutionLevel)
    {
        return string.Join("\n",
            UnitTraitData.GetDisplayName(unitNumber),
            $"{GameLocalization.GradeNameFormat(GetGradeForUnit(unitNumber))} · {FormatEvolutionStars(evolutionLevel)}",
            FormatCombatStatsBlock(unitNumber, evolutionLevel),
            $"{GameLocalization.FieldTooltipPatternPrefix} {HighlightPatternTiming(UnitPatternDescriptions.GetPatternDescription(unitNumber))}");
    }

    // ───────────────── 리치 텍스트(툴팁 색상/크기 강조) ─────────────────
    // 공통 색상 팔레트(웜톤)
    public const string ColLabel = "D8C7A6";   // 라벨(크림)
    public const string ColName = "FFC94D";    // 유닛명(골드)
    public const string ColDamage = "FF7A4D";  // 공격력(주황빨강)
    public const string ColHealth = "74D98A";  // 체력(초록)
    public const string ColSpeed = "FFD24D";   // 공격속도(골드)
    public const string ColRange = "E0B97A";   // 사거리(탄)
    public const string ColSub = "9D8A66";     // 보조 수치(흐린 탄)
    public const string ColPatternTitle = "FFB066";
    public const string ColPatternBody = "E8DCC4";

    public static string GetGradeColorHex(int grade)
    {
        switch (grade)
        {
            case 1: return "FFB54D"; // 1등급 골드
            case 2: return "C79BFF"; // 2등급 보라
            case 3: return "6FB8FF"; // 3등급 파랑
            case 4: return "7FD98A"; // 4등급 초록
            default: return "B8C0CC"; // 5등급 은색
        }
    }

    public static string BuildFieldHeaderRich(int unitNumber, int evolutionLevel)
    {
        string name = UnitTraitData.GetDisplayName(unitNumber);
        int grade = GetGradeForUnit(unitNumber);
        string gradeHex = GetGradeColorHex(grade);
        string stars = FormatEvolutionStars(evolutionLevel);
        return $"<size=25><b><color=#{ColName}>{name}</color></b></size>\n" +
               $"<color=#{gradeHex}><b>{GameLocalization.GradeNameFormat(grade)}</b></color>  {stars}";
    }

    public static string BuildDamageRowRich(int unitNumber, int evolutionLevel)
    {
        int dmg = CalculateDisplayDamage(unitNumber, evolutionLevel);
        float mul = GetEvolutionStatMultiplier(evolutionLevel);
        return $"<color=#{ColLabel}>{GameLocalization.StatAttack}</color>   <b><color=#{ColDamage}>{dmg}</color></b> <size=13><color=#{ColSub}>x{mul:0.00}</color></size>";
    }

    public static string BuildAttackSpeedRowRich(int unitNumber, int evolutionLevel = 1)
    {
        float interval = GetEffectiveAttackIntervalForDisplay(unitNumber, evolutionLevel);
        if (interval <= 0f)
        {
            return $"<color=#{ColLabel}>{GameLocalization.StatAttackSpeed}</color>   <b><color=#{ColSpeed}>—</color></b>";
        }
        float aps = 1f / interval;
        string sec = GameLocalization.StatSecondUnit;
        return $"<color=#{ColLabel}>{GameLocalization.StatAttackSpeed}</color>   <b><color=#{ColSpeed}>{interval:0.#}{sec}</color></b> <size=13><color=#{ColSub}>({aps:0.##}{GameLocalization.StatPerSecond})</color></size>";
    }

    public static float GetEffectiveAttackIntervalForDisplay(int unitNumber, int evolutionLevel)
    {
        float interval = UnitEvolutionMilestones.HasMilestones(unitNumber)
            ? UnitEvolutionMilestones.GetEffectiveAttackInterval(unitNumber, evolutionLevel)
            : GetAttackIntervalForUnit(unitNumber);
        if (interval <= 0f) return interval;
        interval *= CharacterUpgradeData.GetMobilityIntervalMultiplier(unitNumber);
        return interval;
    }

    public static float GetEffectiveRangeForDisplay(int unitNumber)
    {
        float range = GetAttackRangeForUnit(unitNumber);
        if (range <= 0f) return range;
        return range + CharacterUpgradeData.GetRangeBonus(unitNumber);
    }

    public static string BuildRangeRowRich(int unitNumber)
    {
        float range = GetEffectiveRangeForDisplay(unitNumber);
        if (range <= 0f)
        {
            return $"<color=#{ColLabel}>{GameLocalization.StatRange}</color>   <b><color=#{ColRange}>{GameLocalization.StatRangeGlobal}</color></b>";
        }
        return $"<color=#{ColLabel}>{GameLocalization.StatRange}</color>   <b><color=#{ColRange}>{range:0.#}</color></b>";
    }

    public static string BuildHealthRowRich(int unitNumber, int evolutionLevel)
    {
        int hp = CalculateCoreHealth(unitNumber, evolutionLevel);
        return $"<color=#{ColLabel}>{GameLocalization.StatHealth}</color>   <b><color=#{ColHealth}>{hp}</color></b>";
    }

    static string BuildLevelUpSectionTitle(string label)
    {
        return $"<size=16><color=#{ColPatternTitle}><b>{label}</b></color></size>";
    }

    // 카드 설명 본문 — 어두운 배경에서 잘 읽히도록 밝은 크림색
    const string ColCardPatternBody = "FFF2D8";

    static string BuildLevelUpStatDivider()
    {
        return $"<color=#{ColSub}>────────────────</color>";
    }

    static readonly Regex PatternTimingHighlightRegex = new Regex(@"\d+(?:\.\d+)?초에 1회씩(?:\(\d+% 확률\))?", RegexOptions.Compiled);

    public static string HighlightPatternTiming(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return PatternTimingHighlightRegex.Replace(text,
            m => $"<color=#{ColSpeed}><b>{m.Value}</b></color>");
    }

    public static string BuildPatternRich(int unitNumber)
    {
        string desc = HighlightPatternTiming(UnitPatternDescriptions.GetPatternDescription(unitNumber));
        return $"<color=#{ColPatternTitle}><b>{GameLocalization.StatPattern}</b></color>\n<color=#{ColPatternBody}>{desc}</color>";
    }

    public static string BuildTraitsSectionTitleRich()
    {
        return $"<color=#{ColPatternTitle}><b>{GameLocalization.StatSynergy}</b></color>";
    }

    public static string BuildTraitNameRich(string traitName)
    {
        if (string.IsNullOrEmpty(traitName))
        {
            return string.Empty;
        }

        return $"<color=#{ColPatternBody}>{GameLocalization.GetTraitDisplayName(traitName)}</color>";
    }

    public static string BuildTraitsLineRich(int unitNumber)
    {
        string[] traits = UnitTraitData.GetTraits(unitNumber);
        if (traits == null || traits.Length == 0)
        {
            return $"<color=#{ColLabel}>{GameLocalization.StatSynergy}</color>  <color=#{ColSub}>{GameLocalization.StatNone}</color>";
        }

        return $"<color=#{ColLabel}>{GameLocalization.StatSynergy}</color>  <color=#{ColPatternBody}>{GameLocalization.FormatTraitList(traits)}</color>";
    }

    public static string BuildLevelUpCardPreviewRich(int unitNumber, bool isNewUnit, int currentEvolutionLevel, int resultEvolutionLevel)
    {
        string milestoneSection = UnitEvolutionMilestones.BuildMilestoneCardSectionRich(
            unitNumber, isNewUnit, currentEvolutionLevel, resultEvolutionLevel);
        bool hasMilestones = !string.IsNullOrEmpty(milestoneSection);

        if (isNewUnit)
        {
            var parts = new List<string>
            {
                BuildLevelUpSectionTitle(GameLocalization.LevelUpCombatAbility),
                BuildDamageRowRich(unitNumber, 1),
                BuildHealthRowRich(unitNumber, 1),
                BuildAttackSpeedRowRich(unitNumber, 1),
                BuildRangeRowRich(unitNumber)
            };
            if (hasMilestones)
            {
                parts.Add(BuildLevelUpStatDivider());
                parts.Add(milestoneSection);
            }
            parts.Add(BuildLevelUpStatDivider());
            parts.Add(BuildLevelUpSectionTitle(GameLocalization.LevelUpAttackPattern));
            parts.Add($"<color=#{ColCardPatternBody}>{HighlightPatternTiming(UnitPatternDescriptions.GetPatternDescription(unitNumber))}</color>");
            return string.Join("\n", parts);
        }

        int curDmg = CalculateDisplayDamage(unitNumber, currentEvolutionLevel);
        int nextDmg = CalculateDisplayDamage(unitNumber, resultEvolutionLevel);
        int curHp = CalculateCoreHealth(unitNumber, currentEvolutionLevel);
        int nextHp = CalculateCoreHealth(unitNumber, resultEvolutionLevel);

        var evolveParts = new List<string>
        {
            BuildLevelUpSectionTitle(GameLocalization.LevelUpEvolutionEffect),
            $"{FormatEvolutionStars(currentEvolutionLevel)} → {FormatEvolutionStars(resultEvolutionLevel)}",
            $"<color=#{ColLabel}>{GameLocalization.LevelUpAttackStat}</color>  <color=#{ColSub}>{curDmg}</color> → <b><color=#{ColDamage}>{nextDmg}</color></b>  <color=#7FFF7F>(+{nextDmg - curDmg})</color>",
            $"<color=#{ColLabel}>{GameLocalization.LevelUpHealthStat}</color>  <color=#{ColSub}>{curHp}</color> → <b><color=#{ColHealth}>{nextHp}</color></b>  <color=#7FFF7F>(+{nextHp - curHp})</color>"
        };
        if (hasMilestones)
        {
            evolveParts.Add(BuildLevelUpStatDivider());
            evolveParts.Add(milestoneSection);
        }
        evolveParts.Add(BuildLevelUpStatDivider());
        evolveParts.Add(BuildLevelUpSectionTitle(GameLocalization.LevelUpAttackPattern));
        evolveParts.Add($"<color=#{ColCardPatternBody}>{HighlightPatternTiming(UnitPatternDescriptions.GetPatternDescription(unitNumber))}</color>");
        return string.Join("\n", evolveParts);
    }

    public static string BuildShopTooltipText(UnitData unitData, int evolutionLevel)
    {
        if (unitData == null) return string.Empty;

        int unitNumber = unitData.unitNumber;
        string[] traits = UnitTraitData.GetTraits(unitNumber);
        string traitLine = traits != null && traits.Length > 0
            ? GameLocalization.FormatTraitList(traits)
            : GameLocalization.StatNone;
        int grade = GetGradeForUnit(unitNumber);
        string gradeHex = GetGradeColorHex(grade);

        string header =
            $"<size=24><b><color=#{ColName}>{unitData.GetUnitName()}</color></b></size>\n" +
            $"<color=#{gradeHex}><b>{unitData.GetGradeName()}</b></color>  {FormatEvolutionStars(evolutionLevel)}";

        string stats = string.Join("\n",
            BuildDamageRowRich(unitNumber, evolutionLevel),
            BuildAttackSpeedRowRich(unitNumber, evolutionLevel),
            BuildRangeRowRich(unitNumber));

        string permanent = BuildPermanentUpgradeLineRich(unitNumber);
        if (!string.IsNullOrEmpty(permanent))
        {
            stats = stats + "\n" + permanent;
        }

        string traitInfo = $"<color=#{ColLabel}>{GameLocalization.StatTraitsLabel}</color>  <color=#{ColPatternBody}>{traitLine}</color>";
        string desc = $"<size=15><color=#{ColPatternBody}>{GameLocalization.GetUnitGradeDescription(grade, unitNumber)}</color></size>";

        return string.Join("\n",
            header,
            stats,
            traitInfo,
            desc,
            BuildPatternRich(unitNumber));
    }

    public const string ColPermanent = "7FD98A";

    public static string BuildPermanentUpgradeLineRich(int unitNumber)
    {
        if (!CharacterUpgradeData.HasAnyUpgrade(unitNumber) && !UnitArchiveAwakening.HasAny(unitNumber)) return string.Empty;

        var parts = new System.Collections.Generic.List<string>();
        int attack = CharacterUpgradeData.GetAttackBonus(unitNumber);
        if (attack > 0) parts.Add(GameLocalization.ArchivePermanentAttackFormat(attack));

        if (UnitCombatStats.CanAutoAttack(unitNumber))
        {
            float mobilityPct = (1f - CharacterUpgradeData.GetMobilityIntervalMultiplier(unitNumber)) * 100f;
            if (mobilityPct > 0.05f)
            {
                parts.Add(GameLocalization.ArchivePermanentMobilityFormat(mobilityPct));
            }
        }

        switch (UnitArchiveSpecialty.GetType(unitNumber))
        {
            case UnitArchiveSpecialty.Type.Range:
            {
                float rangeBonus = CharacterUpgradeData.GetRangeBonus(unitNumber);
                if (rangeBonus > 0.01f)
                {
                    parts.Add(GameLocalization.ArchivePermanentRangeFormat(rangeBonus));
                }
                break;
            }
            case UnitArchiveSpecialty.Type.Economy:
            {
                float economyPct = (1f - CharacterUpgradeData.GetEconomyCooldownMultiplier(unitNumber)) * 100f;
                if (economyPct > 0.05f)
                {
                    parts.Add(GameLocalization.ArchivePermanentEconomyFormat(economyPct));
                }
                break;
            }
        }

        int awakeningAttack = UnitArchiveAwakening.GetAttackBonus(unitNumber);
        if (awakeningAttack > 0)
        {
            parts.Add(GameLocalization.ArchivePermanentAwakeningFormat(awakeningAttack));
        }

        if (parts.Count == 0) return string.Empty;
        return $"<color=#{ColPermanent}><b>{GameLocalization.ArchivePermanentLabel}</b></color>  <color=#{ColPatternBody}>{string.Join(" · ", parts)}</color>";
    }
}
