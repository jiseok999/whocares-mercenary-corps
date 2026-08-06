/// <summary>진화 마일스톤·레벨업 카드 문구 로컬라이제이션.</summary>
public static partial class GameLocalization
{
    public static string EvolutionTraitSectionUnlock => Pick("진화 특성 (해금)", "Evolution traits (unlock)", "進化特性 (解放)");
    public static string EvolutionTraitSectionBoost => Pick("진화 특성 ▶ 강화!", "Evolution traits ▶ Boost!", "進化特性 ▶ 強化!");
    public static string EvolutionTraitSectionDefault => Pick("진화 특성", "Evolution traits", "進化特性");
    public static string EvolutionRibbonUnlock => Pick("▶ 진화 특성 해금!", "▶ Evolution trait unlock!", "▶ 進化特性解放!");
    public static string LevelUpEvolutionBadge => Pick("진화 ▲", "Evolve ▲", "進化 ▲");
    public static string EvolutionAttackIntervalLabel => Pick("공격 주기", "Attack interval", "攻撃周期");
    public static string EvolutionNewBadge9 => "★★ NEW";
    public static string EvolutionNewBadge6 => "★ NEW";
    public static string EvolutionNewBadge4 => "NEW";
    public static string LevelUpCombatAbility => Pick("전투 능력", "Combat stats", "戦闘能力");
    public static string LevelUpAttackPattern => Pick("공격 패턴", "Attack pattern", "攻撃パターン");
    public static string LevelUpEvolutionEffect => Pick("진화 효과", "Evolution effect", "進化効果");
    public static string LevelUpAttackStat => Pick("공격", "Attack", "攻撃");
    public static string LevelUpHealthStat => Pick("체력", "HP", "体力");
    public static string FieldTooltipPatternPrefix => Pick("패턴:", "Pattern:", "パターン:");
    public static string StatTraitsLabel => Pick("특성", "Traits", "特性");

    public static string GetEvolutionMilestoneDescription(int unitNumber, int requiredLevel)
    {
        switch (unitNumber)
        {
            case 1:
                switch (requiredLevel)
                {
                    case 4: return Pick("잔상 투사체 +1 (75%)", "+1 afterimage shot (75%)", "残像弾+1 (75%)");
                    case 6: return Pick("공격 주기 −8%", "Attack interval −8%", "攻撃周期−8%");
                    case 9: return Pick("양옆 탄환 +2 (60%) · 잔상 유지", "+2 side shots (60%) · afterimage persists", "左右弾+2 (60%)·残像維持");
                }
                break;
            case 2:
                switch (requiredLevel)
                {
                    case 4: return Pick("2차 폭발 (70%)", "Secondary blast (70%)", "2次爆発 (70%)");
                    case 6: return Pick("스플래시 3.0 → 3.5", "Splash 3.0 → 3.5", "スプラッシュ3.0→3.5");
                    case 9: return Pick("쌍발 (2발째 70%) · 공격 주기 −6%", "Twin shot (2nd 70%) · interval −6%", "2連射(2発目70%)·周期−6%");
                }
                break;
            case 3:
                switch (requiredLevel)
                {
                    case 4: return Pick("연쇄 범위↑ · 연쇄 +1 (최대 2연쇄)", "Chain range↑ · +1 chain (max 2)", "連鎖範囲↑·連鎖+1(最大2)");
                    case 6: return Pick("더블탭 (2발째 80%)", "Double tap (2nd 80%)", "ダブルタップ(2発目80%)");
                    case 9: return Pick("공격 주기 −10% · 연쇄 피해 100%", "Interval −10% · chain damage 100%", "周期−10%·連鎖100%");
                }
                break;
            case 4:
                switch (requiredLevel)
                {
                    case 4: return Pick("골드 생성 4초 → 3.5초", "Gold spawn 4s → 3.5s", "ゴールド生成4秒→3.5秒");
                    case 6: return Pick("골드 획득 +2 (총 3)", "Gold gain +2 (total 3)", "ゴールド+2(合計3)");
                    case 9: return Pick("골드 자동 수집", "Auto-collect gold", "ゴールド自動収集");
                }
                break;
            case 5:
                switch (requiredLevel)
                {
                    case 4: return Pick("2번째 낙뢰 (70%)", "2nd lightning strike (70%)", "2回目落雷(70%)");
                    case 6: return Pick("낙뢰 범위 1.2 → 1.4", "Strike radius 1.2 → 1.4", "落雷範囲1.2→1.4");
                    case 9: return Pick("3연속 낙뢰 · 공격 주기 −8%", "Triple lightning · interval −8%", "3連落雷·周期−8%");
                }
                break;
            case 6:
                switch (requiredLevel)
                {
                    case 4: return Pick("쌍발 (2발째 75%)", "Twin shot (2nd 75%)", "2連射(2発目75%)");
                    case 6: return Pick("둔화 65% · 2.5초", "Slow 65% · 2.5s", "鈍化65%·2.5秒");
                    case 9: return Pick("공격 주기 −10% · 둔화 시 추가탄 50%", "Interval −10% · +50% slow bonus shot", "周期−10%·鈍化時追加弾50%");
                }
                break;
            case 7:
                switch (requiredLevel)
                {
                    case 4: return Pick("세로 폭 +20%", "Vertical width +20%", "縦幅+20%");
                    case 6: return Pick("넉백 +15%", "Knockback +15%", "ノックバック+15%");
                    case 9: return Pick("2파 연속 (2파째 65%)", "2-wave burst (2nd 65%)", "2波連続(2波目65%)");
                }
                break;
            case 8:
                switch (requiredLevel)
                {
                    case 4: return Pick("레이저 지속 3.5초", "Laser lasts 3.5s", "レーザー3.5秒");
                    case 6: return Pick("틱 +15% · 공격 주기 −8%", "Tick +15% · interval −8%", "Tick+15%·周期−8%");
                    case 9: return Pick("종료 시 잔여 스플래시 (50%)", "End splash (50%)", "終了時残スプラッシュ(50%)");
                }
                break;
            case 9:
                switch (requiredLevel)
                {
                    case 4: return Pick("5방향 화살 (바깥 70%)", "5-way arrows (outer 70%)", "5方向矢(外側70%)");
                    case 6: return Pick("공격 주기 −8%", "Attack interval −8%", "攻撃周期−8%");
                    case 9: return Pick("지연 추적탄 +1 (90%)", "+1 delayed homing shot (90%)", "遅延追尾弾+1(90%)");
                }
                break;
            case 10:
                switch (requiredLevel)
                {
                    case 4: return Pick("상·하 칸 공격속도 강화 상승", "Stronger top/bottom tile APS buff", "上下マス攻速強化UP");
                    case 6: return Pick("상·하 칸 공격속도 강화 상승", "Stronger top/bottom tile APS buff", "上下マス攻速強化UP");
                    case 9: return Pick("상·하 칸 공격속도 최대", "Max top/bottom tile APS buff", "上下マス攻速最大");
                }
                break;
            case 11:
                switch (requiredLevel)
                {
                    case 4: return Pick("빙결 2.5초", "Freeze 2.5s", "氷結2.5秒");
                    case 6: return Pick("장판 폭 +25%", "Pad width +25%", "床幅+25%");
                    case 9: return Pick("잔빙결 1.2초 · 공격 주기 −10%", "Lingering freeze 1.2s · interval −10%", "残氷結1.2秒·周期−10%");
                }
                break;
            case 12:
                switch (requiredLevel)
                {
                    case 4: return Pick("연쇄 3명 · 연쇄 60%", "Chain 3 targets · 60% damage", "連鎖3体·60%");
                    case 6: return Pick("공격 주기 −12%", "Attack interval −12%", "攻撃周期−12%");
                    case 9: return Pick("쌍창 (2창째 70%) · 기절 0.5초", "Twin lance (2nd 70%) · 0.5s stun", "双槍(2本目70%)·スタン0.5秒");
                }
                break;
            case 13:
                switch (requiredLevel)
                {
                    case 4: return Pick("관통 피해 35%", "Pierce damage 35%", "貫通ダメージ35%");
                    case 6: return Pick("히트 판정 +15%", "Hitbox +15%", "ヒット判定+15%");
                    case 9: return Pick("왕복 2회", "2 return trips", "往復2回");
                }
                break;
            case 14:
                switch (requiredLevel)
                {
                    case 4: return Pick("대상당 연속 2발 (2발째 80%)", "2 hits per target (2nd 80%)", "対象ごと2発(2発目80%)");
                    case 6: return Pick("3번째 대상 추가 (65%)", "+3rd target (65%)", "3体目追加(65%)");
                    case 9: return Pick("공격 주기 −10% · 튕김 +1회", "Interval −10% · +1 bounce", "周期−10%·跳弾+1");
                }
                break;
            case 15:
                switch (requiredLevel)
                {
                    case 4: return Pick("분열 6방향", "6-way split", "6方向分裂");
                    case 6: return Pick("둔화 확률 70%", "70% slow chance", "鈍化確率70%");
                    case 9: return Pick("본탄 더블 (2발째 75%)", "Main shot double (2nd 75%)", "本体弾2連(2発目75%)");
                }
                break;
            case 16:
                switch (requiredLevel)
                {
                    case 4: return Pick("공격력·공격속도 강화 상승", "Higher ATK/APS aura", "攻撃力·攻速強化UP");
                    case 6: return Pick("공격력·공격속도 강화 상승", "Higher ATK/APS aura", "攻撃力·攻速強化UP");
                    case 9: return Pick("좌·우 2칸까지 확장", "Aura extends 2 tiles left/right", "左右2マスまで拡張");
                }
                break;
            case 17:
                switch (requiredLevel)
                {
                    case 4: return Pick("범위 1.6 → 1.8", "Radius 1.6 → 1.8", "範囲1.6→1.8");
                    case 6: return Pick("공격 주기 −8%", "Attack interval −8%", "攻撃周期−8%");
                    case 9: return Pick("연속 주먹 (2타째 70%)", "Combo punch (2nd 70%)", "連続拳(2打目70%)");
                }
                break;
            case 18:
                switch (requiredLevel)
                {
                    case 4: return Pick("3번째 화살 (70%)", "+3rd arrow (70%)", "3本目の矢(70%)");
                    case 6: return Pick("기절 3.5초", "Stun 3.5s", "スタン3.5秒");
                    case 9: return Pick("화살 피해 +15%", "Arrow damage +15%", "矢ダメージ+15%");
                }
                break;
            case 19:
                switch (requiredLevel)
                {
                    case 4: return Pick("장판 확대 · 틱 피해 +20%", "Larger pad · tick +20%", "床拡大·Tick+20%");
                    case 6: return Pick("장판 추가 확대 · 틱 0.85초", "Even larger · tick 0.85s", "さらに拡大·Tick0.85秒");
                    case 9: return Pick("최대 장판 · 이동 −15%", "Max pad · move speed −15%", "最大床·移動−15%");
                }
                break;
            case 20:
                switch (requiredLevel)
                {
                    case 4: return Pick("4번째 대상 (75%)", "+4th target (75%)", "4体目(75%)");
                    case 6: return Pick("레이저 3.5초", "Laser 3.5s", "レーザー3.5秒");
                    case 9: return Pick("공격 주기 −8%", "Attack interval −8%", "攻撃周期−8%");
                }
                break;
            case 21:
                switch (requiredLevel)
                {
                    case 4: return Pick("선풍 3.5초", "Gust field 3.5s", "突風3.5秒");
                    case 6: return Pick("반경 +20%", "Radius +20%", "半径+20%");
                    case 9: return Pick("2단계 소용돌이 (65%)", "Stage-2 vortex (65%)", "2段階渦(65%)");
                }
                break;
            case 22:
                switch (requiredLevel)
                {
                    case 4: return Pick("반경 +20%", "Radius +20%", "半径+20%");
                    case 6: return Pick("기절 2.5초", "Stun 2.5s", "スタン2.5秒");
                    case 9: return Pick("쌍뿌리 (2번째 70%)", "Twin roots (2nd 70%)", "双根(2本目70%)");
                }
                break;
            case 23:
                switch (requiredLevel)
                {
                    case 4: return Pick("장판 피해 40%", "Pad damage 40%", "床ダメージ40%");
                    case 6: return Pick("장판 지속 +35%", "Pad duration +35%", "床持続+35%");
                    case 9: return Pick("종료 폭발 (80%)", "End explosion (80%)", "終了爆発(80%)");
                }
                break;
            case 24:
                switch (requiredLevel)
                {
                    case 4: return Pick("반사 수명 +25%", "Reflect lifetime +25%", "反射寿命+25%");
                    case 6: return Pick("접촉 피해 +15%", "Contact damage +15%", "接触ダメージ+15%");
                    case 9: return Pick("추적 가속", "Homing acceleration", "追尾加速");
                }
                break;
            case 25:
                switch (requiredLevel)
                {
                    case 4: return Pick("빔 3.5초", "Beam 3.5s", "ビーム3.5秒");
                    case 6: return Pick("두께 +25%", "Width +25%", "太さ+25%");
                    case 9: return Pick("잔광 1.5초 (40%)", "Afterglow 1.5s (40%)", "残光1.5秒(40%)");
                }
                break;
            case 26:
                switch (requiredLevel)
                {
                    case 4: return Pick("반사 수명 +25%", "Reflect lifetime +25%", "反射寿命+25%");
                    case 6: return Pick("접촉 피해 +15%", "Contact damage +15%", "接触ダメージ+15%");
                    case 9: return Pick("추적 가속", "Homing acceleration", "追尾加速");
                }
                break;
            case 27:
                switch (requiredLevel)
                {
                    case 4: return Pick("로밍 3회 후 추적", "Homing after 3 roams", "3回ローミング後追尾");
                    case 6: return Pick("접촉 틱 0.85초", "Contact tick 0.85s", "接触Tick0.85秒");
                    case 9: return Pick("추적 +20% · 공격 주기 −8%", "Homing +20% · interval −8%", "追尾+20%·周期−8%");
                }
                break;
            case 28:
                switch (requiredLevel)
                {
                    case 4: return Pick("체류 2.5초", "Linger 2.5s", "滞留2.5秒");
                    case 6: return Pick("끌기 +20%", "Pull +20%", "引き寄せ+20%");
                    case 9: return Pick("잔류 오라 1.5초 (50% 피해)", "Lingering aura 1.5s (50% dmg)", "残留オーラ1.5秒(50%ダメージ)");
                }
                break;
        }
        return string.Empty;
    }
}
