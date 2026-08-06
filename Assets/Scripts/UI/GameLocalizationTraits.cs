using UnityEngine;

/// <summary>조합(시너지) 툴팁·특수공격 설명 로컬라이제이션.</summary>
public static partial class GameLocalization
{
    public static string TraitEffectNotActive => Pick("아직 발동 안 됨", "Not active yet", "まだ発動していません");
    public static string TraitActivatesFromFormat(int count) => Format(
        "{0}개부터 발동합니다.", "Activates at {0} units.", "{0}体から発動します。", count);
    public static string TraitBreakpointCountFormat(int count) => Format("{0}개", "{0} units", "{0}体", count);
    public static string TraitBreakpointActive => Pick("(적용 중)", "(Active)", "(適用中)");
    public static string TraitAttackDefaultFormat(string timing) => Format(
        "{0} 특수 공격을 발동합니다.", "Triggers a special attack {0}.", "{0} 特殊攻撃を発動します。", timing);
    public static string TraitCalamityMarkChain => Pick(", 처치 시 연쇄 폭발", ", chain explosion on kill", "、撃破時連鎖爆発");

    public static string FormatAttackTiming(float cooldown, float procChance)
    {
        string sec = cooldown >= 1f && Mathf.Abs(cooldown - Mathf.Round(cooldown)) < 0.001f
            ? $"{(int)Mathf.Round(cooldown)}"
            : $"{cooldown:0.#}";
        if (procChance >= 0.999f)
        {
            return Format("{0}초에 1회씩", "once every {0}s", "{0}秒に1回", sec);
        }

        int pct = Mathf.RoundToInt(procChance * 100f);
        return Format("{0}초에 1회씩({1}% 확률)", "once every {0}s ({1}% chance)", "{0}秒に1回({1}%確率)", sec, pct);
    }

    public static string GetAttackPatternPlainText(
        TraitPeriodicAttackDefs.AttackKind kind,
        int targetCount,
        float extraParam,
        float cooldown,
        float procChance)
    {
        string timing = FormatAttackTiming(cooldown, procChance);
        int targets = Mathf.Max(1, targetCount);
        switch (kind)
        {
            case TraitPeriodicAttackDefs.AttackKind.MageLaser:
                return Format(
                    "가장 가까운 적에게 {0} 비전 레이저를 쏩니다.",
                    "Fires an arcane laser at the nearest enemy {0}.",
                    "最も近い敵に{0} 秘術レーザーを放つ。",
                    timing);
            case TraitPeriodicAttackDefs.AttackKind.WardenArrowRain:
                return Format(
                    "가까운 적 {1}명에게 {0} 수호의 화살을 쏩니다.",
                    "Fires guardian arrows at {1} nearby enemies {0}.",
                    "近くの敵{1}体に{0} 守護の矢を放つ。",
                    timing, targets);
            case TraitPeriodicAttackDefs.AttackKind.GoblinCoinBurst:
                return Format(
                    "가까운 적 {1}명에게 {0} 유도탄을 쏩니다.",
                    "Fires homing shots at {1} nearby enemies {0}.",
                    "近くの敵{1}体に{0} 誘導弾を放つ。",
                    timing, targets);
            case TraitPeriodicAttackDefs.AttackKind.KnightLanceWave:
                return Format(
                    "전방으로 {0} 창술 파동을 날립니다.",
                    "Launches a lance wave forward {0}.",
                    "前方に{0} 槍術波動を放つ。",
                    timing);
            case TraitPeriodicAttackDefs.AttackKind.CalamityPulse:
                return Format(
                    "주변에 {0} 재앙의 파동을 펼칩니다.",
                    "Unleashes a calamity pulse nearby {0}.",
                    "周囲に{0} 災厄の波動を広げる。",
                    timing);
            case TraitPeriodicAttackDefs.AttackKind.CalamityMark:
                {
                    int bonusPct = Mathf.RoundToInt(Mathf.Max(0f, extraParam) * 100f);
                    string chain = extraParam >= 0.24f ? TraitCalamityMarkChain : string.Empty;
                    return Format(
                        "가까운 적 {1}명에게 {0} 낙인을 새깁니다(낙인 적 피해 +{2}%{3}).",
                        "Marks {1} nearby enemies {0} (+{2}% damage taken{3}).",
                        "近くの敵{1}体に{0} 刻印を付与(刻印対象ダメージ+{2}%{3})。",
                        timing, targets, bonusPct, chain);
                }
            case TraitPeriodicAttackDefs.AttackKind.DemonExecution:
                return Format(
                    "약한 적 {1}명에게 {0} 처형의 손을 발동합니다.",
                    "Triggers execution on {1} weak enemies {0}.",
                    "弱い敵{1}体に{0} 処刑の手を発動。",
                    timing, targets);
            case TraitPeriodicAttackDefs.AttackKind.RogueShuriken:
                return Format(
                    "적에게 {0} 표창을 연속 투척합니다.",
                    "Throws shuriken in rapid succession {0}.",
                    "敵に{0} 手裏剣を連続投擲。",
                    timing);
            case TraitPeriodicAttackDefs.AttackKind.PenguinIceShard:
                return Format(
                    "가장 가까운 적에게 {0} 빙하 가시를 쏩니다.",
                    "Fires an ice shard at the nearest enemy {0}.",
                    "最も近い敵に{0} 氷河の棘を放つ。",
                    timing);
            case TraitPeriodicAttackDefs.AttackKind.FireMeteorShower:
                return targetCount >= 4
                    ? Format(
                        "적 주변에 {0} 불덩이 {1}발과 잔불을 떨어뜨립니다.",
                        "Drops {1} fireballs and embers near enemies {0}.",
                        "敵の周囲に{0} 火球{1}発と残火を落とす。",
                        timing, targetCount)
                    : Format(
                        "적 주변에 {0} 불덩이 {1}발을 떨어뜨립니다.",
                        "Drops {1} fireballs near enemies {0}.",
                        "敵の周囲に{0} 火球{1}発を落とす。",
                        timing, Mathf.Max(2, targetCount));
            case TraitPeriodicAttackDefs.AttackKind.IceFreeze:
                return targetCount >= 2
                    ? Format(
                        "적 {1}명에게 {0} 서리 손길을 겁니다.",
                        "Chills {1} enemies {0}.",
                        "敵{1}体に{0} 霜の手をかける。",
                        timing, targetCount)
                    : Format(
                        "적에게 {0} 서리 손길을 겁니다.",
                        "Chills an enemy {0}.",
                        "敵に{0} 霜の手をかける。",
                        timing);
            case TraitPeriodicAttackDefs.AttackKind.LightningChain:
                return Format(
                    "적에게 {0} 연쇄 번개를 내립니다.",
                    "Calls chain lightning {0}.",
                    "敵に{0} 連鎖落雷を放つ。",
                    timing);
            case TraitPeriodicAttackDefs.AttackKind.NatureVine:
                return Format(
                    "적 {1}명에게 {0} 덩굴을 휘두릅니다.",
                    "Lashes {1} enemies with vines {0}.",
                    "敵{1}体に{0} ツタを振る。",
                    timing, targets);
            case TraitPeriodicAttackDefs.AttackKind.WindGust:
                return Format(
                    "전방에 {0} 돌풍 {1}발을 쏩니다.",
                    "Fires {1} gusts forward {0}.",
                    "前方に{0} 突風{1}発を放つ。",
                    timing, Mathf.Max(2, targetCount));
            case TraitPeriodicAttackDefs.AttackKind.LightBeam:
                return Format(
                    "가장 먼 적에게 {0} 성광 빔을 쏩니다.",
                    "Fires a holy beam at the farthest enemy {0}.",
                    "最も遠い敵に{0} 聖光ビームを放つ。",
                    timing);
            case TraitPeriodicAttackDefs.AttackKind.DarkVortex:
                return Format(
                    "적 주변에 {0} 그림자 소용돌이를 일으킵니다.",
                    "Creates a shadow vortex near enemies {0}.",
                    "敵の周囲に{0} 影の渦を起こす。",
                    timing);
            default:
                return TraitAttackDefaultFormat(timing);
        }
    }

    public static string FormatTraitBreakpointHeading(int breakpoint, bool isActive, bool isReached)
    {
        string heading = $"<size=18><b>{TraitBreakpointCountFormat(breakpoint)}</b></size>";
        if (isActive)
        {
            return $"<color=#{UnitCombatStats.ColName}>{heading}</color>"
                 + $"  <size=13><color=#{UnitCombatStats.ColHealth}>{TraitBreakpointActive}</color></size>";
        }
        if (isReached)
        {
            return $"<color=#{UnitCombatStats.ColSpeed}>{heading}</color>";
        }
        return $"<color=#6E6552>{heading}</color>";
    }

    public static string GetTraitProcSkillName(TraitPeriodicAttackDefs.AttackKind kind)
    {
        switch (kind)
        {
            case TraitPeriodicAttackDefs.AttackKind.MageLaser: return Pick("비전 레이저", "Arcane Laser", "秘術レーザー");
            case TraitPeriodicAttackDefs.AttackKind.WardenArrowRain: return Pick("수호의 화살", "Guardian Arrow", "守護の矢");
            case TraitPeriodicAttackDefs.AttackKind.GoblinCoinBurst: return Pick("도깨비 유도탄", "Goblin Homing Shot", "鬼誘導弾");
            case TraitPeriodicAttackDefs.AttackKind.KnightLanceWave: return Pick("창술 파동", "Lance Wave", "槍術波動");
            case TraitPeriodicAttackDefs.AttackKind.CalamityPulse: return Pick("재앙의 파동", "Calamity Pulse", "災厄の波動");
            case TraitPeriodicAttackDefs.AttackKind.CalamityMark: return Pick("재앙의 낙인", "Calamity Mark", "災厄の刻印");
            case TraitPeriodicAttackDefs.AttackKind.DemonExecution: return Pick("처형의 손", "Hand of Execution", "処刑の手");
            case TraitPeriodicAttackDefs.AttackKind.RogueShuriken: return Pick("표창 연격", "Shuriken Barrage", "手裏剣連撃");
            case TraitPeriodicAttackDefs.AttackKind.PenguinIceShard: return Pick("빙하 가시", "Glacial Shard", "氷河の棘");
            case TraitPeriodicAttackDefs.AttackKind.FireMeteorShower: return Pick("화염 낙하", "Meteor Fall", "火炎落下");
            case TraitPeriodicAttackDefs.AttackKind.IceFreeze: return Pick("서리 손길", "Frost Touch", "霜の手");
            case TraitPeriodicAttackDefs.AttackKind.LightningChain: return Pick("연쇄 번개", "Chain Lightning", "連鎖落雷");
            case TraitPeriodicAttackDefs.AttackKind.NatureVine: return Pick("덩굴 타격", "Vine Lash", "ツタ打撃");
            case TraitPeriodicAttackDefs.AttackKind.WindGust: return Pick("돌풍", "Gust", "突風");
            case TraitPeriodicAttackDefs.AttackKind.LightBeam: return Pick("성광", "Holy Light", "聖光");
            case TraitPeriodicAttackDefs.AttackKind.DarkVortex: return Pick("그림자 소용돌이", "Shadow Vortex", "影の渦");
            default: return string.Empty;
        }
    }
}
