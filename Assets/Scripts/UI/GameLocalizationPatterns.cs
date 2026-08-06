/// <summary>유닛 전투 패턴 설명 로컬라이제이션.</summary>
public static partial class GameLocalization
{
    public static string PatternInfoUnavailable => Pick(
        "패턴 정보 없음", "No pattern info", "パターン情報なし");

    public static string GetUnitPatternDescription(int unitNumber)
    {
        switch (unitNumber)
        {
            case 1: return Pick(
                "가장 가까운 적을 향해 2초에 1회씩 돌맹이를 던져 공격합니다.",
                "Throws rocks at the nearest enemy once every 2s.",
                "最も近い敵に2秒に1回石を投げて攻撃。");
            case 2: return Pick(
                "가장 가까운 적에게 3초에 1회씩 불덩이를 던지며, 명중 시 주변에 추가 피해를 줍니다.",
                "Throws a fireball at the nearest enemy every 3s; hits deal splash damage.",
                "最も近い敵に3秒に1回火球。命中時に周囲へ追加ダメージ。");
            case 3: return Pick(
                "가장 가까운 적에게 3.3초에 1회씩 전기탄을 쏴 공격합니다.",
                "Fires an electric shot at the nearest enemy every 3.3s.",
                "最も近い敵に3.3秒に1回電気弾を発射。");
            case 4: return Pick(
                "4초에 1회씩 골드 아이콘을 생성하며, 클릭하면 골드를 획득합니다.",
                "Spawns a gold icon every 4s; click to collect gold.",
                "4秒に1回ゴールドアイコン生成。クリックで獲得。");
            case 5: return Pick(
                "무작위 적 위치에 3초에 1회씩 번개를 떨어뜨려 공격합니다.",
                "Strikes a random enemy location with lightning every 3s.",
                "ランダムな敵位置に3秒に1回落雷。");
            case 6: return Pick(
                "가장 가까운 적에게 3.3초에 1회씩 얼음탄을 쏴 공격하며, 확률로 둔화를 겁니다.",
                "Fires ice at the nearest enemy every 3.3s; may slow on hit.",
                "最も近い敵に3.3秒に1回氷弾。確率で鈍化。");
            case 7: return Pick(
                "16초에 1회씩 전장 우측 열에서 직선 공격을 합니다.",
                "Every 16s, attacks in a straight line from the right column.",
                "16秒に1回、戦場右列から直線攻撃。");
            case 8: return Pick(
                "가장 가까운 적에게 7초에 1회씩 레이저를 쏴 공격합니다.",
                "Fires a laser at the nearest enemy every 7s.",
                "最も近い敵に7秒に1回レーザー。");
            case 9: return Pick(
                "가장 가까운 적을 기준으로 3초에 1회씩 세 방향 화살을 쏩니다.",
                "Every 3s, fires three arrows based on the nearest enemy.",
                "最も近い敵基準で3秒に1回3方向の矢。");
            case 10: return Pick(
                "자동 공격을 하지 않으며, 위·아래 파란 칸의 아군 공격속도가 빨라집니다.",
                "Does not auto-attack; boosts ally attack speed on blue tiles above/below.",
                "自動攻撃なし。上下の青マスの味方攻速UP。");
            case 11: return Pick(
                "10초에 1회씩 무작위 열에 빙결 장판을 깔아 적을 빙결시킵니다.",
                "Every 10s, places a freeze pad on a random column.",
                "10秒に1回ランダム列に氷結床を設置。");
            case 12: return Pick(
                "가장 가까운 적에게 5초에 1회씩 심판의 창을 던져 공격합니다.",
                "Throws a judgment lance at the nearest enemy every 5s.",
                "最も近い敵に5秒に1回審判の槍を投げる。");
            case 13: return Pick(
                "3초에 1회씩 관통 표창을 던져 공격합니다.",
                "Throws a piercing shuriken every 3s.",
                "3秒に1回貫通手裏剣を投げる。");
            case 14: return Pick(
                "가장 가까운 두 적에게 2초에 1회씩 튕기는 공격을 합니다.",
                "Every 2s, bounces attacks between the two nearest enemies.",
                "最も近い2体に2秒に1回跳弾攻撃。");
            case 15: return Pick(
                "3초에 1회씩 직선탄을 쏘며, 명중 시 분열탄을 뿌립니다.",
                "Every 3s, fires a straight shot that splits on hit.",
                "3秒に1回直線弾。命中時に分裂。");
            case 16: return Pick(
                "같은 행의 좌·우 아군에게 공격력·공격속도 강화를 부여하며, 범위는 빨간 칸으로 표시됩니다.",
                "Buffs allies left/right on the same row; range shown as red tiles.",
                "同じ行の左右味方に攻撃力·攻速強化。範囲は赤マス表示。");
            case 17: return Pick(
                "5초에 1회씩 적 위치에 주먹 공격을 합니다.",
                "Punches enemy positions every 5s.",
                "5秒に1回敵位置に拳攻撃。");
            case 18: return Pick(
                "가장 가까운 두 적에게 8초에 1회씩 기절 화살을 쏩니다.",
                "Fires stun arrows at the two nearest enemies every 8s.",
                "最も近い2体に8秒に1回スタン矢。");
            case 19: return Pick(
                "전장 우측 보라 장판 안의 적에게 1초에 1회씩 피해를 줍니다.",
                "Damages enemies on the purple pad on the right every 1s.",
                "戦場右側の紫床内の敵に1秒に1回ダメージ。");
            case 20: return Pick(
                "가장 가까운 세 적에게 5초에 1회씩 레이저를 쏩니다.",
                "Fires lasers at the three nearest enemies every 5s.",
                "最も近い3体に5秒に1回レーザー。");
            case 21: return Pick(
                "7초에 1회씩 가장 가까운 적 위치에 선풍 필드를 둡니다.",
                "Places a gust field on the nearest enemy every 7s.",
                "7秒に1回最も近い敵位置に突風フィールド。");
            case 22: return Pick(
                "4초에 1회씩 무작위 위치에 뿌리 장판을 깔아 공격합니다.",
                "Every 4s, roots a random location for damage.",
                "4秒に1回ランダム位置に根の床で攻撃。");
            case 23: return Pick(
                "5초에 1회씩 관통탄을 쏴 공격합니다.",
                "Fires a piercing shot every 5s.",
                "5秒に1回貫通弾を発射。");
            case 24: return Pick(
                "3초에 1회씩 반사 투사체를 쏴 공격합니다.",
                "Fires a reflective projectile every 3s.",
                "3秒に1回反射投射体を発射。");
            case 25: return Pick(
                "가장 가까운 적에게 5초에 1회씩 레이저 빔을 쏩니다.",
                "Fires a laser beam at the nearest enemy every 5s.",
                "最も近い敵に5秒に1回レーザービーム。");
            case 26: return Pick(
                "5초에 1회씩 반사 공을 쏴 공격합니다.",
                "Fires a ricochet ball every 5s.",
                "5秒に1回反射ボールを発射。");
            case 27: return Pick(
                "10초에 1회씩 로밍 돌을 던져 공격합니다.",
                "Throws a roaming stone every 10s.",
                "10秒に1回ローミング石を投げる。");
            case 28: return Pick(
                "10초에 1회씩 먼 적 위치에 소용돌이를 둡니다.",
                "Places a vortex on a distant enemy every 10s.",
                "10秒に1回遠い敵位置に渦を設置。");
            default: return PatternInfoUnavailable;
        }
    }
}
