using System.Text;

/// <summary>유닛 표시 이름 로컬라이제이션.</summary>
public static partial class GameLocalization
{
    public static string GetUnitDisplayName(int unitNumber)
    {
        switch (unitNumber)
        {
            case 1: return Pick("두쫀 도깨비", "Ddujjon Goblin", "ドゥッジョン鬼");
            case 2: return Pick("견습 화염술사", "Apprentice Pyromancer", "見習い火炎術師");
            case 3: return Pick("천둥 저격수", "Thunder Sniper", "雷狙撃手");
            case 4: return Pick("동전 줍는 거지", "Coin-Scraping Beggar", "コイン拾い乞食");
            case 5: return Pick("붉은 도깨비", "Red Goblin", "赤鬼");
            case 6: return Pick("서리 암살자", "Frost Assassin", "霜暗殺者");
            case 7: return Pick("질풍 마도사", "Gale Warlock", "疾風魔導士");
            case 8: return Pick("배탈 난 구울", "Ravenous Ghoul", "貪るグール");
            case 9: return Pick("별의 도깨비", "Star Goblin", "星の鬼");
            case 10: return Pick("윙즈 남작", "Baron Wings", "ウィングス男爵");
            case 11: return Pick("길 잃은 귀신", "Lost Ghost", "道迷い幽霊");
            case 12: return Pick("저스티스 백작", "Count Justice", "ジャスティス伯爵");
            case 13: return Pick("무영 자객", "Shadowless Rogue", "無影刺客");
            case 14: return Pick("황금 기사", "Golden Knight", "黄金騎士");
            case 15: return Pick("빙결 견습 마도사", "Apprentice Frost Mage", "氷結見習い魔導士");
            case 16: return Pick("고블린 킹", "Goblin King", "ゴブリンキング");
            case 17: return Pick("장난꾸러기 도깨비", "Mischief Goblin", "いたずら鬼");
            case 18: return Pick("겁쟁이 사냥꾼", "Cowardly Hunter", "臆病な狩人");
            case 19: return Pick("숲의 마녀", "Forest Witch", "森の魔女");
            case 20: return Pick("데몬", "Demon", "デーモン");
            case 21: return Pick("캔디 공작", "Duke Candy", "キャンディ公爵");
            case 22: return Pick("숲의 정령", "Forest Spirit", "森の精霊");
            case 23: return Pick("관통의 붉은 기사", "Piercing Red Knight", "貫通の赤騎士");
            case 24: return Pick("다크 엘프 궁수", "Dark Elf Archer", "ダークエルフ弓使い");
            case 25: return Pick("정밀의 백기사", "Precision White Knight", "精密の白騎士");
            case 26: return Pick("수상할 정도로 완벽한 펭귄", "Suspiciously Perfect Penguin", "怪しく完璧なペンギン");
            case 27: return Pick("푸른 도깨비", "Blue Goblin", "青鬼");
            case 28: return Pick("심연의 흑기사", "Abyss Black Knight", "深淵の黒騎士");
            default: return UnitFallbackNameFormat(unitNumber);
        }
    }

    public static string GetTraitDisplayName(string traitKey)
    {
        switch (traitKey)
        {
            case "마법사": return Pick("마법사", "Mage", "魔術師");
            case "파수꾼": return Pick("파수꾼", "Warden", "番人");
            case "도깨비": return Pick("도깨비", "Goblin", "鬼");
            case "기사단": return Pick("기사단", "Order", "騎士団");
            case "재앙": return Pick("재앙", "Calamity", "災厄");
            case "악마": return Pick("악마", "Demon", "悪魔");
            case "도적단": return Pick("도적단", "Rogue", "盗賊団");
            case "펭귄": return Pick("펭귄", "Penguin", "ペンギン");
            case "불": return Pick("불", "Fire", "炎");
            case "얼음": return Pick("얼음", "Ice", "氷");
            case "번개": return Pick("번개", "Lightning", "雷");
            case "자연": return Pick("자연", "Nature", "自然");
            case "바람": return Pick("바람", "Wind", "風");
            case "빛": return Pick("빛", "Light", "光");
            case "어둠": return Pick("어둠", "Dark", "闇");
            default: return traitKey ?? string.Empty;
        }
    }

    public static string GetUnitGradeDescription(int grade, int unitNumber)
    {
        string name = GetUnitDisplayName(unitNumber);
        switch (grade)
        {
            case 5: return Format(
                "5등급 {0}. 기본적인 전투 능력을 갖춘 유닛입니다.",
                "Grade 5 {0}. A unit with basic combat ability.",
                "5級 {0}. 基本的な戦闘能力を持つユニット。",
                name);
            case 4: return Format(
                "4등급 {0}. 향상된 전투 능력을 가진 유닛입니다.",
                "Grade 4 {0}. A unit with improved combat ability.",
                "4級 {0}. 強化された戦闘能力を持つユニット。",
                name);
            case 3: return Format(
                "3등급 {0}. 강력한 전투 능력을 자랑하는 유닛입니다.",
                "Grade 3 {0}. A unit with strong combat ability.",
                "3級 {0}. 強力な戦闘能力を誇るユニット。",
                name);
            case 2: return Format(
                "2등급 {0}. 매우 강력한 전투 능력을 가진 유닛입니다.",
                "Grade 2 {0}. A unit with very strong combat ability.",
                "2級 {0}. 非常に強力な戦闘能力を持つユニット。",
                name);
            default: return Format(
                "1등급 {0}. 최고급 전투 능력을 갖춘 엘리트 유닛입니다.",
                "Grade 1 {0}. An elite unit with top-tier combat ability.",
                "1級 {0}. 最高級の戦闘能力を持つエリートユニット。",
                name);
        }
    }

    public static string FormatTraitList(string[] traits)
    {
        if (traits == null || traits.Length == 0) return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < traits.Length; i++)
        {
            if (i > 0) sb.Append(" · ");
            sb.Append(GetTraitDisplayName(traits[i]));
        }
        return sb.ToString();
    }
}
