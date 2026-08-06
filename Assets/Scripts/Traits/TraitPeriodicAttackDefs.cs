using UnityEngine;

/// <summary>
/// 조합별 주기 특수 공격 정의 (조합당 보드 전역 쿨 1개).
/// </summary>
public static class TraitPeriodicAttackDefs
{
    public enum AttackKind
    {
        MageLaser,
        WardenArrowRain,
        GoblinCoinBurst,
        KnightLanceWave,
        CalamityPulse,
        CalamityMark,
        DemonExecution,
        RogueShuriken,
        PenguinIceShard,
        FireMeteorShower,
        IceFreeze,
        LightningChain,
        NatureVine,
        WindGust,
        LightBeam,
        DarkVortex
    }

    public struct TierDef
    {
        public float cooldown;
        public float procChance;
        public float damageScale;
        public int targetCount;
        public float extraParam;
        public AttackKind kind;
        public string skillName;
        public string toastLine;
    }

    public struct TraitDef
    {
        public string traitName;
        public int[] breakpoints;
        public TierDef[] tiers;
    }

    static readonly TraitDef[] All =
    {
        Def("마법사", new[] { 2, 4 },
            Tier(2.0f, 1f, 1.0f, 1, 0f, AttackKind.MageLaser, "비전 레이저", "가장 가까운 적에게 파란 레이저"),
            Tier(1.5f, 1f, 1.3f, 1, 0f, AttackKind.MageLaser, "비전 레이저", "더 자주, 더 강한 레이저")),

        Def("파수꾼", new[] { 2, 3 },
            Tier(3.0f, 0.40f, 1.0f, 2, 0f, AttackKind.WardenArrowRain, "수호의 화살", "가까운 적에게 초록 화살"),
            Tier(2.5f, 0.60f, 1.2f, 3, 0f, AttackKind.WardenArrowRain, "수호의 화살", "화살이 더 많이, 더 자주")),

        Def("도깨비", new[] { 2, 4, 6 },
            Tier(4.0f, 1f, 0.32f, 3, 0f, AttackKind.GoblinCoinBurst, "도깨비 유도탄", "가까운 적 3명에게 약한 유도탄"),
            Tier(3.0f, 1f, 0.38f, 3, 0f, AttackKind.GoblinCoinBurst, "도깨비 유도탄", "유도탄이 조금 더 강해짐"),
            Tier(2.5f, 1f, 0.44f, 3, 0f, AttackKind.GoblinCoinBurst, "도깨비 유도탄", "유도탄이 더 빠르고 강해짐")),

        Def("기사단", new[] { 2, 4 },
            Tier(5.0f, 1f, 1.0f, 1, 0f, AttackKind.KnightLanceWave, "창술 파동", "앞쪽으로 창 파동"),
            Tier(4.0f, 1f, 1.35f, 1, 0f, AttackKind.KnightLanceWave, "창술 파동", "더 넓고 강한 창 파동")),

        Def("재앙", new[] { 2, 4 },
            Tier(4.0f, 1f, 0.08f, 2, 0.15f, AttackKind.CalamityMark, "재앙의 낙인", "가까운 적에 낙인"),
            Tier(3.0f, 1f, 0.12f, 3, 0.25f, AttackKind.CalamityMark, "재앙의 낙인", "낙인·처치 시 연쇄")),

        Def("악마", new[] { 2, 3 },
            Tier(4.0f, 0.50f, 1.0f, 2, 0.12f, AttackKind.DemonExecution, "처형의 손", "약한 적을 노려 처형"),
            Tier(3.2f, 0.65f, 1.0f, 3, 0.18f, AttackKind.DemonExecution, "처형의 손", "처형 범위가 넓어짐")),

        Def("도적단", new[] { 2 },
            Tier(2.5f, 0.45f, 0.9f, 2, 0f, AttackKind.RogueShuriken, "표창 연격", "표창 2발 연속 투척")),

        Def("펭귄", new[] { 1 },
            Tier(6.0f, 1f, 0.85f, 1, 1.6f, AttackKind.PenguinIceShard, "빙하 가시", "가까운 적에게 얼음 가시")),

        Def("불", new[] { 2, 4 },
            Tier(3.0f, 1f, 1.0f, 2, 0.85f, AttackKind.FireMeteorShower, "화염 낙하", "적 주변에 불덩이 2발 낙하"),
            Tier(2.4f, 1f, 1.3f, 4, 1.15f, AttackKind.FireMeteorShower, "화염 낙하", "불덩이 4발 + 잔불 지대")),

        Def("얼음", new[] { 2, 4 },
            Tier(4.0f, 0.60f, 0.9f, 1, 0f, AttackKind.IceFreeze, "서리 손길", "적을 느리게 하고 얼림"),
            Tier(3.2f, 0.75f, 1.1f, 2, 0f, AttackKind.IceFreeze, "서리 손길", "적 2명까지 얼림")),

        Def("번개", new[] { 2, 4 },
            Tier(3.0f, 1f, 0.85f, 1, 0f, AttackKind.LightningChain, "연쇄 번개", "번개가 옆 적으로 튐"),
            Tier(2.4f, 1f, 1.0f, 2, 0f, AttackKind.LightningChain, "연쇄 번개", "번개가 더 많이 튐")),

        Def("자연", new[] { 2, 4 },
            Tier(5.0f, 1f, 0.9f, 2, 0f, AttackKind.NatureVine, "덩굴 타격", "덩굴로 적을 묶음"),
            Tier(4.0f, 1f, 1.15f, 3, 0f, AttackKind.NatureVine, "덩굴 타격", "적 3명까지 덩굴")),

        Def("바람", new[] { 2, 4 },
            Tier(3.5f, 0.50f, 0.95f, 2, 0f, AttackKind.WindGust, "돌풍", "바람 투사체 2발"),
            Tier(2.8f, 0.65f, 1.15f, 3, 0f, AttackKind.WindGust, "돌풍", "바람 투사체 3발")),

        Def("빛", new[] { 2, 3 },
            Tier(4.0f, 1f, 1.0f, 1, 0f, AttackKind.LightBeam, "성광", "멀리 성광 레이저"),
            Tier(3.2f, 1f, 1.25f, 1, 0f, AttackKind.LightBeam, "성광", "더 강한 성광")),

        Def("어둠", new[] { 2, 3 },
            Tier(4.0f, 0.55f, 1.0f, 1, 0f, AttackKind.DarkVortex, "그림자 소용돌이", "적을 끌어당기며 피해"),
            Tier(3.2f, 0.70f, 1.2f, 2, 0f, AttackKind.DarkVortex, "그림자 소용돌이", "더 넓게 끌어당김"))
    };

    static TierDef Tier(float cd, float proc, float dmg, int targets, float extra, AttackKind kind, string skill, string toast)
    {
        return new TierDef
        {
            cooldown = cd,
            procChance = proc,
            damageScale = dmg,
            targetCount = targets,
            extraParam = extra,
            kind = kind,
            skillName = skill,
            toastLine = toast
        };
    }

    static TraitDef Def(string name, int[] bp, params TierDef[] tiers)
    {
        return new TraitDef { traitName = name, breakpoints = bp, tiers = tiers };
    }

    public static bool TryGetDef(string traitName, out TraitDef def)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].traitName == traitName)
            {
                def = All[i];
                return true;
            }
        }
        def = default;
        return false;
    }

    public static int GetActiveLevel(TraitDef def, int count)
    {
        if (def.breakpoints == null) return 0;
        int level = 0;
        for (int i = 0; i < def.breakpoints.Length; i++)
        {
            if (count >= def.breakpoints[i]) level = i + 1;
        }
        return level;
    }

    public static TierDef GetTier(TraitDef def, int level)
    {
        if (level <= 0 || def.tiers == null || def.tiers.Length == 0)
        {
            return default;
        }
        int idx = Mathf.Clamp(level - 1, 0, def.tiers.Length - 1);
        return def.tiers[idx];
    }

    public static string GetBreakpointText(string traitName)
    {
        if (!TryGetDef(traitName, out TraitDef def) || def.breakpoints == null) return string.Empty;
        return string.Join(" ", def.breakpoints);
    }

    static string GetTierPatternPlainText(TierDef tier)
    {
        return GameLocalization.GetAttackPatternPlainText(
            tier.kind, tier.targetCount, tier.extraParam, tier.cooldown, tier.procChance);
    }

    static string FormatTierPatternLine(TierDef tier, bool emphasize)
    {
        string line = UnitCombatStats.HighlightPatternTiming(GetTierPatternPlainText(tier));
        if (!emphasize)
        {
            return $"<color=#6E6552>{line}</color>";
        }

        return $"<color=#{UnitCombatStats.ColPatternBody}>{line}</color>";
    }

    static string FormatBreakpointHeading(int breakpoint, bool isActive, bool isReached)
    {
        return GameLocalization.FormatTraitBreakpointHeading(breakpoint, isActive, isReached);
    }

    /// <summary>대제목 아래 — 한 문장 패턴 설명.</summary>
    static string FormatTierDetail(TierDef tier, bool emphasize)
    {
        return FormatTierPatternLine(tier, emphasize);
    }

    public static string GetEffectSummary(string traitName, int count)
    {
        if (!TryGetDef(traitName, out TraitDef def)) return string.Empty;
        int level = GetActiveLevel(def, count);
        if (level <= 0) return GameLocalization.TraitEffectNotActive;
        TierDef tier = GetTier(def, level);
        return UnitCombatStats.HighlightPatternTiming(GetTierPatternPlainText(tier));
    }

    public static string BuildTooltipBody(string traitName, int count)
    {
        if (!TryGetDef(traitName, out TraitDef def)) return string.Empty;
        int level = GetActiveLevel(def, count);
        var sb = new System.Text.StringBuilder();

        if (level <= 0)
        {
            sb.Append($"<color=#9C9488>{GameLocalization.TraitActivatesFromFormat(def.breakpoints[0])}</color>\n\n");
        }

        int tierCount = Mathf.Min(def.breakpoints.Length, def.tiers.Length);
        for (int i = 0; i < tierCount; i++)
        {
            if (i > 0) sb.Append('\n');

            int breakpoint = def.breakpoints[i];
            bool isActive = level == i + 1;
            bool isReached = count >= breakpoint;
            sb.Append(FormatBreakpointHeading(breakpoint, isActive, isReached));
            sb.Append('\n');
            sb.Append(FormatTierDetail(def.tiers[i], isActive || isReached));
        }

        /*
        string synergyLine = GameManager.GetSynergyCellTooltipLine(traitName);
        if (!string.IsNullOrEmpty(synergyLine))
        {
            sb.Append($"\n\n<color=#{UnitCombatStats.ColRange}>{synergyLine}</color>");
        }
        */
        return sb.ToString();
    }
}
