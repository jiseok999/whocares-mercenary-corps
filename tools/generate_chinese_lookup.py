# Generates GameLocalizationChineseLookup.cs — uses Japanese gloss + phrase rules + manual overrides.
import re, glob, os, unicodedata

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Scripts", "UI")
OUT = os.path.join(ROOT, "GameLocalizationChineseLookup.cs")

def unescape(s):
    # Only decode C# backslash escapes; never unicode_escape (corrupts ★, —, etc.).
    out = []
    i = 0
    while i < len(s):
        if s[i] == "\\" and i + 1 < len(s):
            n = s[i + 1]
            if n == "n":
                out.append("\n"); i += 2; continue
            if n == "r":
                out.append("\r"); i += 2; continue
            if n == "t":
                out.append("\t"); i += 2; continue
            if n == '"':
                out.append('"'); i += 2; continue
            if n == "\\":
                out.append("\\"); i += 2; continue
        out.append(s[i])
        i += 1
    return "".join(out)

def is_cjkish(c):
    o = ord(c)
    return (
        0x4E00 <= o <= 0x9FFF
        or 0x3400 <= o <= 0x4DBF
        or 0xF900 <= o <= 0xFAFF
        or 0x3040 <= o <= 0x30FF
        or 0xAC00 <= o <= 0xD7AF
        or 0x3000 <= o <= 0x303F
        or 0xFF00 <= o <= 0xFFEF
    )

def extract_pairs(path):
    text = open(path, encoding="utf-8").read()
    pat = re.compile(r'Pick\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"')
    fmt = re.compile(
        r'Format\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,'
    )
    pairs = {}
    for m in pat.finditer(text):
        en, ja = unescape(m.group(2)), unescape(m.group(3))
        pairs.setdefault(en, ja)
    for m in fmt.finditer(text):
        en, ja = unescape(m.group(2)), unescape(m.group(3))
        pairs.setdefault(en, ja)
    return pairs

def cjk_ratio(s):
    if not s:
        return 0
    cjk = sum(1 for c in s if unicodedata.name(c, "").startswith(("CJK", "HIRAGANA", "KATAKANA")))
    return cjk / len(s)

# Longest-first phrase replacements (English fragment -> Traditional Chinese)
PHRASES = [
    ("Attack interval −", "攻擊間隔 −"),
    ("Attack interval", "攻擊間隔"),
    ("Attack speed", "攻擊速度"),
    ("Attack pattern", "攻擊模式"),
    ("Evolution traits", "進化特性"),
    ("Evolution effect", "進化效果"),
    ("Combat stats", "戰鬥能力"),
    ("Special Skills", "特殊技能"),
    ("Board tile", "部署格"),
    ("Boss Reward", "首領獎勵"),
    ("Boss Party", "首領隊伍"),
    ("Boss Appears!", "首領出現！"),
    ("Break Time", "等待時間"),
    ("Round {0}", "第 {0} 回合"),
    ("Level {0}", "等級 {0}"),
    ("Grade {0}", "{0}級"),
    ("Unit {0}", "單位 {0}"),
    ("Synergy Effects", "羈絆效果"),
    ("Synergy count ×2", "羈絆貢獻 ×2"),
    ("Synergy  {0}", "羈絆  {0}"),
    ("Party Incoming", "隊伍來襲"),
    ("Deploy Units · Use Shop", "部署單位 · 使用商店"),
    ("Start Round 1", "開始第 1 回合"),
    ("Next Round", "下一回合"),
    ("Tap to acquire", "點擊獲得"),
    ("Not enough gold!", "金幣不足！"),
    ("Cannot purchase during combat.", "戰鬥中無法購買。"),
    ("Cannot refresh the shop during combat.", "戰鬥中無法刷新商店。"),
    ("Deploy at least one unit on the board before starting.", "開始前請在棋盘上至少部署 1 名單位。"),
    ("Buy units from the shop", "在商店購買單位"),
    ("and deploy them freely on the board.", "並自由部署到棋盘上。"),
    ("and deploy them on the board.", "並部署到棋盘上。"),
    ("once every", "每"),
    ("Attack", "攻擊"),
    ("Evolution", "進化"),
    ("Pattern", "模式"),
    ("Synergy", "羈絆"),
    ("Boss", "首領"),
    ("Party", "隊伍"),
    ("Round", "回合"),
    ("Level", "等級"),
    ("Grade", "級"),
    ("Unit", "單位"),
    ("Gold", "金幣"),
    ("Deploy", "部署"),
    ("Shop", "商店"),
    ("Refresh", "刷新"),
    ("Upgrade", "強化"),
    ("Awakening", "覺醒"),
    ("Milestone", "里程碑"),
    ("Range", "射程"),
    ("Damage", "傷害"),
    ("Freeze", "冰凍"),
    ("Slow", "減速"),
    ("Stun", "暈眩"),
    ("Laser", "雷射"),
    ("Splash", "濺射"),
    ("Chain", "連鎖"),
    ("Pierce", "穿透"),
    ("Knockback", "擊退"),
    ("Homing", "追蹤"),
    ("Interval", "間隔"),
    ("Radius", "半徑"),
    ("Pad", "地板"),
    ("Tick", "跳傷"),
    ("Contact", "接觸"),
    ("Reflect", "反射"),
    ("Execution", "處決"),
    ("Calamity", "災厄"),
    ("Demon", "惡魔"),
    ("Goblin", "鬼怪"),
    ("Mage", "法師"),
    ("Warden", "守衛"),
    ("Order", "騎士團"),
    ("Rogue", "盜賊"),
    ("Penguin", "企鵝"),
    ("Fire", "火"),
    ("Ice", "冰"),
    ("Lightning", "雷"),
    ("Nature", "自然"),
    ("Wind", "風"),
    ("Light", "光"),
    ("Dark", "暗"),
    ("Used", "已使用"),
    ("None", "無"),
    ("All", "全部"),
    ("Close", "關閉"),
    ("Settings", "設定"),
    ("Language", "語言"),
    ("Experience", "經驗值"),
    ("Fullscreen", "全螢幕"),
    ("Volume", "音量"),
    ("Notice", "通知"),
    ("Unknown", "未知"),
    ("Active", "生效中"),
    ("Unlock", "解鎖"),
    ("Permanent", "永久"),
    ("Total", "總計"),
    ("Combat", "戰鬥"),
    ("Normal", "一般"),
    ("Balanced", "平衡"),
    ("Swarm", "數量"),
    ("Bruiser", "坦度"),
    ("Breakthrough", "突破"),
    ("Ranged", "遠程"),
    ("Enemies", "敵人"),
    ("Enemy", "敵人"),
    ("weak", "弱"),
    ("nearest", "最近"),
    ("random", "隨機"),
    ("every", "每"),
    ("seconds", "秒"),
    ("second", "秒"),
    ("tile", "格"),
    ("tiles", "格"),
    ("row", "行"),
    ("column", "列"),
    ("left", "左"),
    ("right", "右"),
    ("top", "上"),
    ("bottom", "下"),
    ("center", "中央"),
    ("corner", "角落"),
    ("entire", "整個"),
    ("field", "戰場"),
    ("battle", "戰鬥"),
    ("board", "棋盘"),
    ("bench", "候補"),
    ("pool", "池"),
    ("locked", "鎖定"),
    ("registered", "已登錄"),
    ("owned", "持有"),
    ("medals", "獎章"),
    ("survival", "生存"),
    ("time", "時間"),
    ("result", "結果"),
    ("defeat", "失敗"),
    ("clear", "通關"),
    ("demo", "試玩"),
    ("continue", "繼續"),
    ("restart", "重新開始"),
    ("title", "標題"),
    ("screen", "畫面"),
    ("choose", "選擇"),
    ("select", "選擇"),
    ("starting", "起始"),
    ("three", "三"),
    ("one", "一"),
    ("first", "第一"),
    ("reroll", "重抽"),
    ("cards", "卡片"),
    ("card", "卡片"),
    ("acquired", "獲得"),
    ("purchase", "購買"),
    ("purchased", "已購買"),
    ("evolve", "進化"),
    ("elite", "精英"),
    ("ability", "能力"),
    ("strong", "強力"),
    ("improved", "提升"),
    ("basic", "基本"),
    ("top-tier", "頂級"),
    ("very", "非常"),
    ("special", "特殊"),
    ("effect", "效果"),
    ("effects", "效果"),
    ("aura", "光環"),
    ("buff", "增益"),
    ("buffs", "增益"),
    ("debuff", "減益"),
    ("speed", "速度"),
    ("power", "威力"),
    ("boost", "強化"),
    ("bonus", "加成"),
    ("reward", "獎勵"),
    ("rewards", "獎勵"),
    ("warning", "警告"),
    ("appears", "出現"),
    ("incoming", "來襲"),
    ("spawn", "生成"),
    ("collect", "收集"),
    ("auto-collect", "自動收集"),
    ("gain", "獲得"),
    ("required", "需要"),
    ("unable", "無法"),
    ("load", "讀取"),
    ("info", "資訊"),
    ("information", "資訊"),
    ("description", "說明"),
    ("detail", "詳情"),
    ("preview", "預覽"),
    ("review", "回顧"),
    ("records", "紀錄"),
    ("encounter", "遭遇"),
    ("battle to unlock", "戰鬥後解鎖"),
    ("lifetime", "生涯"),
    ("reach", "達到"),
    ("star", "星"),
    ("stars", "星"),
    ("need", "需要"),
    ("more", "更多"),
    ("unique", "不同"),
    ("buys", "購買"),
    ("lock", "鎖定"),
    ("fixed", "固定"),
    ("remaining", "剩餘"),
    ("empty", "空"),
    ("no", "沒有"),
    ("not", "未"),
    ("yet", "尚未"),
    ("active synergy", "生效中的羈絆"),
    ("unit effect", "單位效果"),
    ("unit effects", "單位效果"),
    ("tile effects", "格子效果"),
    ("favorites", "最愛"),
    ("filter", "篩選"),
    ("roster", "名單"),
    ("archive", "圖鑑"),
    ("mercenary", "傭兵"),
    ("codex", "圖鑑"),
    ("traits", "特性"),
    ("trait", "特性"),
    ("skill", "技能"),
    ("skills", "技能"),
    ("cooldown", "冷卻"),
    ("armed", "待機"),
    ("meter", "計量"),
    ("footer", "頁尾"),
    ("header", "標題"),
    ("hint", "提示"),
    ("guide", "指南"),
    ("toast", "提示"),
    ("overlay", "覆蓋"),
    ("break", "休息"),
    ("prep", "準備"),
    ("wait", "等待"),
    ("countdown", "倒數"),
    ("timer", "計時"),
    ("progress", "進度"),
    ("wave", "波次"),
    ("waves", "波次"),
    ("slot", "欄位"),
    ("slots", "欄位"),
    ("icon", "圖示"),
    ("label", "標籤"),
    ("button", "按鈕"),
    ("panel", "面板"),
    ("list", "列表"),
    ("category", "分類"),
    ("badge", "徽章"),
    ("count", "數量"),
    ("format", "格式"),
    ("chance", "機率"),
    ("probability", "機率"),
    ("odds", "機率"),
    ("percent", "百分比"),
    ("chance)", "機率)"),
    ("max", "最大"),
    ("cumulative", "累計"),
    ("multiplier", "倍率"),
    ("×", "×"),
    ("−", "−"),
    ("→", "→"),
    ("·", "·"),
    ("▶", "▶"),
    ("★", "★"),
    ("!", "！"),
]

def _is_ascii_word(s):
    return bool(re.fullmatch(r"[a-zA-Z][a-zA-Z\-']*", s))

def phrase_translate(en):
    result = en
    for eng, chi in PHRASES:
        if not eng:
            continue
        if _is_ascii_word(eng):
            pattern = r"(?<![a-zA-Z])" + re.escape(eng) + r"(?![a-zA-Z])"
            result = re.sub(pattern, chi, result, flags=re.IGNORECASE)
        elif eng in result:
            result = result.replace(eng, chi)
    return result

def ja_to_tw(ja):
    # Katakana-heavy names: keep manual; kanji-heavy: use as TW base
    repl = {
        "ユニット": "單位", "レベル": "等級", "グレード": "級", "ショップ": "商店",
        "ボス": "首領", "パーティ": "隊伍", "ラウンド": "回合", "攻撃": "攻擊",
        "防御": "防禦", "速度": "速度", "間隔": "間隔", "範囲": "範圍",
        "連鎖": "連鎖", "氷結": "冰凍", "鈍化": "減速", "スタン": "暈眩",
        "進化": "進化", "覚醒": "覺醒", "特性": "特性", "シナジー": "羈絆",
        "魔術師": "法師", "番人": "守衛", "鬼": "鬼怪", "騎士団": "騎士團",
        "災厄": "災厄", "悪魔": "惡魔", "盗賊団": "盜賊團", "ペンギン": "企鵝",
        "炎": "火", "氷": "冰", "雷": "雷", "自然": "自然", "風": "風",
        "光": "光", "闇": "暗", "待機": "等待", "通知": "通知",
        "設定": "設定", "言語": "語言", "全画面": "全螢幕",
    }
    result = ja
    for jp, tw in repl.items():
        result = result.replace(jp, tw)
    return result

def to_cn(tw):
    table = str.maketrans({
        "體": "体", "設": "设", "聲": "声", "遊": "游", "戲": "戏",
        "語": "语", "簡": "简", "聯": "联", "絡": "络", "經": "经",
        "驗": "验", "級": "级", "單": "单", "組": "组", "與": "与",
        "為": "为", "時": "时", "間": "间", "無": "无", "敵": "敌",
        "獲": "获", "購": "购", "買": "买", "開": "开", "關": "关",
        "閉": "闭", "選": "选", "擇": "择", "進": "进", "團": "团",
        "傭": "佣", "檔": "档", "庫": "库", "顯": "显", "畫": "画",
        "質": "质", "滿": "满", "螢": "萤", "專": "专", "屬": "属",
        "術": "术", "護": "护", "衛": "卫", "騎": "骑", "災": "灾",
        "惡": "恶", "盜": "盗", "賊": "贼", "電": "电", "凍": "冻",
        "結": "结", "擊": "击", "傷": "伤", "範": "范", "圍": "围",
        "遠": "远", "離": "离", "後": "后", "復": "复", "動": "动",
        "態": "态", "標": "标", "準": "准", "確": "确", "認": "认",
        "識": "识", "說": "说", "資": "资", "訊": "讯", "載": "载",
        "讀": "读", "總": "总", "計": "计", "數": "数", "個": "个",
        "種": "种", "類": "类", "條": "条", "並": "并", "還": "还",
        "這": "这", "裡": "里", "們": "们", "對": "对", "將": "将",
        "從": "从", "來": "来", "發": "发", "現": "现", "實": "实",
        "際": "际", "應": "应", "該": "该", "變": "变", "換": "换",
        "擴": "扩", "續": "续", "維": "维", "陣": "阵", "營": "营",
        "場": "场", "線": "线", "網": "网", "連": "连", "鎖": "锁",
        "鏈": "链", "彈": "弹", "藥": "药", "劑": "剂", "覺": "觉",
        "鑑": "鉴", "圖": "图", "絆": "绊", "盤": "盘", "碼": "码",
        "錄": "录", "註": "注", "冊": "册", "帳": "帐", "戶": "户",
        "領": "领", "獎": "奖", "勵": "励", "勁": "劲", "強": "强",
        "弱": "弱", "極": "极", "頂": "顶", "級": "级", "歷": "历",
        "史": "史", "紀": "纪", "錄": "录", "詳": "详", "細": "细",
        "說": "说", "明": "明", "額": "额", "外": "外", "內": "内",
        "裝": "装", "備": "备", "欄": "栏", "位": "位", "置": "置",
        "點": "点", "擊": "击", "獲": "获", "得": "得", "敗": "败",
        "勝": "胜", "鬥": "斗", "戰": "战", "術": "术", "師": "师",
        "獸": "兽", "屍": "尸", "骷": "骷", "髏": "髅", "龍": "龙",
        "鳥": "鸟", "魚": "鱼", "馬": "马", "車": "车", "門": "门",
        "關": "关", "開": "开", "閃": "闪", "電": "电", "雲": "云",
        "霧": "雾", "雨": "雨", "雪": "雪", "風": "风", "土": "土",
        "木": "木", "金": "金", "銀": "银", "銅": "铜", "鐵": "铁",
        "鋼": "钢", "石": "石", "晶": "晶", "寶": "宝", "珠": "珠",
        "玉": "玉", "王": "王", "皇": "皇", "帝": "帝", "神": "神",
        "聖": "圣", "靈": "灵", "魂": "魂", "鬼": "鬼", "妖": "妖",
        "怪": "怪", "精": "精", "靈": "灵", "龜": "龟", "蛇": "蛇",
        "虎": "虎", "狼": "狼", "熊": "熊", "鷹": "鹰", "獅": "狮",
        "象": "象", "鼠": "鼠", "兔": "兔", "貓": "猫", "狗": "狗",
        "豬": "猪", "牛": "牛", "羊": "羊", "雞": "鸡", "鴨": "鸭",
        "企": "企", "鵝": "鹅", "鯨": "鲸", "鯊": "鲨", "蝦": "虾",
        "蟹": "蟹", "貝": "贝", "螺": "螺", "蟲": "虫", "蝶": "蝶",
        "蜂": "蜂", "蟻": "蚁", "蜘": "蜘", "蛛": "蛛", "蠍": "蝎",
        "蜈": "蜈", "蚣": "蚣", "蛙": "蛙",
        "鳳": "凤", "凰": "凰",
    })
    return tw.translate(table)

def cs_escape(s):
    out = []
    for c in s:
        if c == "\\":
            out.append("\\\\")
        elif c == '"':
            out.append('\\"')
        elif c == "\n":
            out.append("\\n")
        elif c == "\r":
            out.append("\\r")
        elif c == "\t":
            out.append("\\t")
        elif ord(c) < 32 or (ord(c) > 127 and not is_cjkish(c)):
            out.append(f"\\u{ord(c):04x}")
        else:
            out.append(c)
    return "".join(out)

def translate(en, ja, manual):
    if en in manual:
        return manual[en]
    if ja and cjk_ratio(ja) >= 0.25:
        tw = ja_to_tw(ja)
        if cjk_ratio(tw) >= 0.2:
            return tw, to_cn(tw)
    tw = phrase_translate(en)
    if tw != en:
        return tw, to_cn(tw)
    return en, en

def main():
    en_ja = {}
    for f in glob.glob(os.path.join(ROOT, "GameLocalization*.cs")):
        if "Coordinator" in f or "ChineseLookup" in f:
            continue
        for en, ja in extract_pairs(f).items():
            en_ja.setdefault(en, ja)

    manual = {}

    def m(en, tw, cn=None):
        manual[en] = (tw, cn if cn is not None else to_cn(tw))

    # Overrides for katakana JP names / rich UI
    m("Start Game", "開始遊戲", "开始游戏")
    m("Ddujjon Goblin", "豆豆鬼怪", "豆豆鬼怪")
    m("Suspiciously Perfect Penguin", "可疑的完美企鵝", "可疑的完美企鹅")
    m("① Buy <color=#FFC94D>1 unit</color> in the shop  →  ② Deploy on the <color=#FFC94D>board</color>  →  ③ Press <color=#FFC94D>Start Round 1</color> or wait for countdown",
      "① 在商店購買<color=#FFC94D>1名單位</color> → ② 部署到<color=#FFC94D>棋盘</color> → ③ 按下<color=#FFC94D>開始第1回合</color>或等待倒數",
      "① 在商店购买<color=#FFC94D>1名单位</color> → ② 部署到<color=#FFC94D>棋盘</color> → ③ 按下<color=#FFC94D>开始第1回合</color>或等待倒数")
    m("✓ Unit purchased  →  ② Deploy freely on the <color=#FFC94D>board</color>  →  ③ Press <color=#FFC94D>Start Round 1</color> or wait for countdown",
      "✓ 已購買單位 → ② 自由部署到<color=#FFC94D>棋盘</color> → ③ 按下<color=#FFC94D>開始第1回合</color>或等待倒數",
      "✓ 已购买单位 → ② 自由部署到<color=#FFC94D>棋盘</color> → ③ 按下<color=#FFC94D>开始第1回合</color>或等待倒数")
    m("Round {0}  <color=#FFC94D>{1}</color>", "第 {0} 回合  <color=#FFC94D>{1}</color>", "第 {0} 回合  <color=#FFC94D>{1}</color>")
    m("Deploy Units · Use Shop", "部署單位 · 使用商店", "部署单位 · 使用商店")
    m("Round 1 Prep", "第 1 回合準備", "第 1 回合准备")
    m("Start Round 1  ▶", "開始第 1 回合  ▶", "开始第 1 回合  ▶")
    m("Next Round  ▶", "下一回合  ▶", "下一回合  ▶")
    m("▶ Evolution trait unlock!", "▶ 進化特性解鎖！", "▶ 进化特性解锁！")
    m("The full version will bring more units, synergies,\nand powerful enemies.\nStay tuned for future updates!",
      "完整版將帶來更多單位、羈絆\n與更強大的敵人。\n敬請期待後續更新！",
      "完整版将带来更多单位、羁绊\n与更强大的敌人。\n敬请期待后续更新！")
    m("You cleared all {0} rounds!\nThank you for defending the wall to the end.",
      "你通關了全部 {0} 回合！\n感謝你守護城牆到最後。",
      "你通关了全部 {0} 回合！\n感谢你守护城墙到最后。")
    m("Your wall has fallen.", "你的城牆已陷落。", "你的城墙已陷落。")
    m("Demo Clear!", "試玩通關！", "试玩通关！")
    m("Battle Result", "戰鬥結果", "战斗结果")
    m("OPTIONS", "OPTIONS", "OPTIONS")
    m("ROUND {0}", "ROUND {0}", "ROUND {0}")
    m("NEW", "NEW", "NEW")
    m("—", "—", "—")
    m("N/A", "N/A", "N/A")
    m("/s", "/s", "/s")
    m("s", "s", "s")
    m("Used", "已使用", "已使用")
    m("Reroll Used", "已重抽", "已重抽")
    m("Experience", "經驗值", "经验值")
    m("Settings", "設定", "设置")
    m("Paused", "暫停", "暂停")
    m("Continue", "繼續", "继续")
    m("Restart", "重新開始", "重新开始")
    m("Fullscreen", "全螢幕", "全屏")
    m("Language", "語言", "语言")
    m("Sound", "音效", "音效")
    m("Master Volume", "主音量", "主音量")
    m("BGM Volume", "BGM 音量", "BGM 音量")
    m("Display", "畫面 / 顯示", "画面 / 显示")
    m("Party Bonus Alerts", "隊伍加成提示", "队伍加成提示")
    m("Mercenary Archive", "傭兵檔案", "佣兵档案")
    m("Quit Game", "離開遊戲", "离开游戏")
    m("Normal Combat", "一般戰鬥", "一般战斗")
    m("Notice", "通知", "通知")
    m("Evolve", "進化", "进化")
    m("None", "無", "无")
    m("Acquired!", "獲得!", "获得!")
    m("Close", "關閉", "关闭")
    m("All", "全部", "全部")
    m("Try Again", "再挑戰", "再挑战")
    m("Play Again", "再玩一次", "再玩一次")

    lines = []
    lines.append("using System.Collections.Generic;")
    lines.append("using System.Text;")
    lines.append("")
    lines.append("/// <summary>English 키 → 繁體/簡體 중국어 조회.</summary>")
    lines.append("public static class GameLocalizationChineseLookup")
    lines.append("{")
    lines.append("    struct Entry { public string Tw; public string Cn; }")
    lines.append("    static readonly Dictionary<string, Entry> Map = new Dictionary<string, Entry>();")
    lines.append("")
    lines.append("    static GameLocalizationChineseLookup()")
    lines.append("    {")
    for en in sorted(en_ja.keys(), key=str.lower):
        tw, cn = translate(en, en_ja.get(en, ""), manual)
        lines.append(f'        Map["{cs_escape(en)}"] = new Entry {{ Tw = "{cs_escape(tw)}", Cn = "{cs_escape(cn)}" }};')
    lines.append("    }")
    lines.append("")
    lines.append("    public static string GetTraditional(string en)")
    lines.append("    {")
    lines.append("        if (string.IsNullOrEmpty(en)) return en;")
    lines.append("        return Map.TryGetValue(en, out Entry e) ? e.Tw : en;")
    lines.append("    }")
    lines.append("")
    lines.append("    public static string GetSimplified(string en)")
    lines.append("    {")
    lines.append("        if (string.IsNullOrEmpty(en)) return en;")
    lines.append("        return Map.TryGetValue(en, out Entry e) ? e.Cn : en;")
    lines.append("    }")
    lines.append("")
    lines.append("    public static string ToSimplified(string zhTw)")
    lines.append("    {")
    lines.append("        if (string.IsNullOrEmpty(zhTw)) return zhTw;")
    lines.append("        var sb = new StringBuilder(zhTw.Length);")
    lines.append("        foreach (char c in zhTw) sb.Append(ConvertCharToSimplified(c));")
    lines.append("        return sb.ToString();")
    lines.append("    }")
    lines.append("")
    lines.append("    static char ConvertCharToSimplified(char c) => ChineseSimplifiedChars.Convert(c);")
    lines.append("}")
    lines.append("")
    lines.append("static class ChineseSimplifiedChars")
    lines.append("{")
    lines.append("    public static char Convert(char c)")
    lines.append("    {")
    lines.append("        switch (c)")
    lines.append("        {")
    simplified_pairs = [
        ('體','体'),('設','设'),('聲','声'),('遊','游'),('戲','戏'),('語','语'),('聯','联'),('絡','络'),
        ('經','经'),('驗','验'),('級','级'),('單','单'),('組','组'),('與','与'),('為','为'),('時','时'),
        ('間','间'),('無','无'),('敵','敌'),('獲','获'),('購','购'),('買','买'),('開','开'),('關','关'),
        ('閉','闭'),('選','选'),('擇','择'),('進','进'),('團','团'),('傭','佣'),('檔','档'),('庫','库'),
        ('顯','显'),('畫','画'),('質','质'),('滿','满'),('螢','萤'),('專','专'),('屬','属'),('術','术'),
        ('護','护'),('衛','卫'),('騎','骑'),('災','灾'),('惡','恶'),('盜','盗'),('賊','贼'),('電','电'),
        ('凍','冻'),('結','结'),('擊','击'),('傷','伤'),('範','范'),('圍','围'),('遠','远'),('離','离'),
        ('後','后'),('復','复'),('動','动'),('態','态'),('標','标'),('準','准'),('確','确'),('認','认'),
        ('識','识'),('說','说'),('資','资'),('訊','讯'),('載','载'),('讀','读'),('總','总'),('計','计'),
        ('數','数'),('個','个'),('種','种'),('類','类'),('條','条'),('並','并'),('還','还'),('這','这'),
        ('裡','里'),('們','们'),('對','对'),('將','将'),('從','从'),('來','来'),('發','发'),('現','现'),
        ('實','实'),('際','际'),('應','应'),('該','该'),('變','变'),('換','换'),('擴','扩'),('續','续'),
        ('維','维'),('陣','阵'),('營','营'),('場','场'),('線','线'),('網','网'),('連','连'),('鎖','锁'),
        ('鏈','链'),('彈','弹'),('藥','药'),('劑','剂'),('覺','觉'),('鑑','鉴'),('圖','图'),('絆','绊'),
        ('盤','盘'),('碼','码'),('錄','录'),('註','注'),('冊','册'),('帳','帐'),('戶','户'),('領','领'),
        ('獎','奖'),('勵','励'),('強','强'),('頂','顶'),('歷','历'),('紀','纪'),('詳','详'),('細','细'),
        ('額','额'),('內','内'),('裝','装'),('備','备'),('欄','栏'),('點','点'),('敗','败'),('勝','胜'),
        ('鬥','斗'),('戰','战'),('師','师'),('獸','兽'),('屍','尸'),('龍','龙'),('鳥','鸟'),('魚','鱼'),
        ('馬','马'),('車','车'),('門','门'),('閃','闪'),('雲','云'),('霧','雾'),('銀','银'),('銅','铜'),
        ('鐵','铁'),('鋼','钢'),('寶','宝'),('聖','圣'),('靈','灵'),('龜','龟'),('鷹','鹰'),('獅','狮'),
        ('貓','猫'),('豬','猪'),('雞','鸡'),('鴨','鸭'),('鯨','鲸'),('鯊','鲨'),('蝦','虾'),('貝','贝'),
        ('蟲','虫'),('蟻','蚁'),('蠍','蝎'),('鳳','凤'),('國','国'),('學','学'),('會','会'),
        ('問','问'),('題','题'),
        ('頭','头'),('臉','脸'),('腦','脑'),('腳','脚'),('愛','爱'),('歡','欢'),('樂','乐'),
        ('難','难'),('輕','轻'),('舊','旧'),('廣','广'),('廠','厂'),('廳','厅'),('縣','县'),('鄉','乡'),
        ('鎮','镇'),('區','区'),('東','东'),('軌','轨'),('話','话'),('視','视'),
        ('聽','听'),('寫','写'),('劇','剧'),('館','馆'),('樓','楼'),
        ('層','层'),('號','号'),('鍵','键'),('機','机'),
    ]
    seen_tw = {}
    for tw, cn in simplified_pairs:
        seen_tw.setdefault(tw, cn)
    for tw, cn in seen_tw.items():
        lines.append(f"            case '\\u{ord(tw):04x}': return '\\u{ord(cn):04x}';")
    lines.append("            default: return c;")
    lines.append("        }")
    lines.append("    }")
    lines.append("}")

    open(OUT, "w", encoding="utf-8").write("\n".join(lines) + "\n")
    print(f"Wrote {len(en_ja)} entries")

if __name__ == "__main__":
    main()
