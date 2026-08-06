using System;

/// <summary>
/// UI 문자열 로컬라이제이션 (한국어 / English / 日本語 / 中文 / ไทย / ES / PT / FR / IT / DE).
/// </summary>
public static partial class GameLocalization
{
    public static event Action LanguageChanged;

    public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Korean;

    public static void Initialize()
    {
        CurrentLanguage = GameSettingsPrefs.LoadLanguage();
        GameLocalizationCoordinator.EnsureSubscribed();
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language) return;
        CurrentLanguage = language;
        GameSettingsPrefs.SaveLanguage(language);
        LanguageChanged?.Invoke();
    }

    public static string GetLanguageDisplayName(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.English: return "English";
            case GameLanguage.Japanese: return "日本語";
            case GameLanguage.TraditionalChinese: return "繁體中文";
            case GameLanguage.SimplifiedChinese: return "简体中文";
            case GameLanguage.Thai: return "ไทย";
            case GameLanguage.Spanish: return "Español";
            case GameLanguage.Portuguese: return "Português";
            case GameLanguage.French: return "Français";
            case GameLanguage.Italian: return "Italiano";
            case GameLanguage.German: return "Deutsch";
            default: return "한국어";
        }
    }

    public static string Format(string ko, string en, string ja, params object[] args)
    {
        return Format(ko, en, ja, null, null, args);
    }

    public static string Format(string ko, string en, string ja, string zhTw, string zhCn, params object[] args)
    {
        string template = Pick(ko, en, ja, zhTw, zhCn);
        return args != null && args.Length > 0 ? string.Format(template, args) : template;
    }

    // ── Title / Settings ──
    public static string TitleGameStart => Pick("게임 시작", "Start Game", "ゲーム開始");
    public static string TitleMercenaryArchive => Pick("용병단 아카이브", "Mercenary Archive", "傭兵団アーカイブ");
    public static string TitleSettings => Pick("설정", "Settings", "設定");
    public static string TitleQuit => Pick("게임 종료", "Quit Game", "ゲーム終了");
    public static string QuitConfirmTitle => Pick("게임 종료", "Quit Game", "ゲーム終了");
    public static string QuitConfirmMessage => Pick(
        "게임을 종료하시겠습니까?",
        "Are you sure you want to quit the game?",
        "ゲームを終了しますか？");
    public static string QuitConfirmYes => Pick("종료", "Quit", "終了");
    public static string QuitConfirmCancel => Pick("취소", "Cancel", "キャンセル");
    public static string SettingsTitle => Pick("설정", "Settings", "設定");
    public static string PauseTitle => Pick("일시정지", "Paused", "一時停止");
    public static string SettingsSubtitle => "OPTIONS";
    public static string SettingsSoundSection => Pick("사운드", "Sound", "サウンド");
    public static string SettingsMasterVolume => Pick("마스터 볼륨", "Master Volume", "マスター音量");
    public static string SettingsBgmVolume => Pick("BGM 볼륨", "BGM Volume", "BGM音量");
    public static string SettingsDisplaySection => Pick("화면 / 표시", "Display", "画面 / 表示");
    public static string SettingsFullscreen => Pick("전체화면", "Fullscreen", "全画面");
    public static string SettingsPartyToast => Pick("파티 보너스 알림", "Party Bonus Alerts", "パーティボーナス通知");
    public static string SettingsLanguageSection => Pick("언어", "Language", "言語");
    public static string SettingsContinue => Pick("계속하기", "Continue", "続ける");
    public static string SettingsRestart => Pick("재시작", "Restart", "再スタート");

    // ── HUD ──
    public static string HudExperience => Pick("경험치", "Experience", "経験値");
    public static string HudRefresh => Pick("새로고침", "Refresh", "更新");
    public static string HudSpecialSkills => Pick("특수 스킬", "Special Skills", "特殊スキル");
    public static string HudSkillUsed => Pick("사용됨", "Used", "使用済");
    public static string HudNormalCombat => Pick("일반 전투", "Normal Combat", "通常戦闘");
    public static string HudBreakTime => Pick("대기 시간", "Break Time", "待機時間");
    public static string HudBossAppears => Pick("보스 출현!", "Boss Appears!", "ボス出現！");
    public static string HudRoundFormat(int round) => Format("ROUND {0}", "ROUND {0}", "ROUND {0}", round);
    public static string HudLevelFormat(int level) => Format("레벨 {0}", "Level {0}", "レベル {0}", level);
    public static string HudSunPointsFormat(int points) => Format("태양: {0}", "Sun: {0}", "太陽: {0}", points);
    public static string HudPlacementFormat(int placed, int max, bool isFull)
    {
        string placedColor = isFull ? "8CEB9E" : "F5E8AE";
        const int numberSize = 33;
        string label = Pick("배치", "Deploy", "配置");
        return $"<color=#B8A88A>{label}</color>  <size={numberSize}><color=#{placedColor}>{placed}</color><color=#8A8070>/{max}</color></size>";
    }

    // ── Break overlay ──
    public static string BreakRound1Prep => Pick("1라운드 준비", "Round 1 Prep", "第1ラウンド準備");
    public static string BreakSubtitle => Pick("유닛 배치 · 상점 이용", "Deploy Units · Use Shop", "ユニット配置 · ショップ利用");
    public static string BreakGuideFirst => Pick("상점에서 유닛을 구매하고\n보드에 자유롭게 배치하세요",
        "Buy units from the shop\nand deploy them freely on the board.",
        "ショップでユニットを購入し\nボードに自由に配置してください");
    public static string BreakGuideNormal => Pick("상점에서 유닛을 구매하고\n보드에 배치하세요",
        "Buy units from the shop\nand deploy them on the board.",
        "ショップでユニットを購入し\nボードに配置してください");
    public static string BreakStartRound1 => Pick("1라운드 시작  ▶", "Start Round 1  ▶", "第1ラウンド開始  ▶");
    public static string BreakNextRound => Pick("다음 라운드로  ▶", "Next Round  ▶", "次のラウンドへ  ▶");

    // ── Toasts / notices ──
    public static string ToastNotice => Pick("알림", "Notice", "通知");
    public static string ToastPartySpawn => Pick("파티 출현", "Party Incoming", "パーティ出現");
    public static string ToastDeployFirst => Pick("배치칸에 유닛을 1기 이상 배치한 뒤 시작하세요",
        "Deploy at least one unit on the board before starting.",
        "配置欄にユニットを1体以上配置してから開始してください");
    public static string ToastEnemiesRush => Pick("시작과 동시에 적들이 바로 몰려옵니다!",
        "Enemies rush in as soon as the round starts!",
        "開始と同時に敵が押し寄せます！");
    public static string ToastEnemyHpBoost => Pick("적 체력 강화", "Enemy HP Boost", "敵HP強化");

    // ── Shop ──
    public static string ShopEvolve => Pick("진화", "Evolve", "進化");
    public static string ShopNoTrait => Pick("없음", "None", "なし");
    public static string ShopRefreshDuringCombat => Pick("전투 중에는 상점을 새로고침할 수 없습니다",
        "Cannot refresh the shop during combat.", "戦闘中はショップを更新できません");
    public static string ShopBuyDuringCombat => Pick("전투 중에는 구매할 수 없습니다",
        "Cannot purchase during combat.", "戦闘中は購入できません");
    public static string ShopUnitSoldOut => Pick("더 이상 해당 유닛은 구매할 수 없습니다",
        "This unit can no longer be purchased.", "このユニットはこれ以上購入できません");
    public static string ShopNotEnoughGold(int price) => Format("골드가 부족합니다! ({0}골드 필요)",
        "Not enough gold! ({0} gold required)", "ゴールドが足りません！（{0}ゴールド必要）", price);
    public static string ShopCannotEvolveMore => Pick("더 이상 해당 유닛은 진화할 수 없습니다",
        "This unit can no longer evolve.", "このユニットはこれ以上進化できません");
    public static string ShopBenchFallback => Pick("배치칸이 가득 차 대기칸에 배치되었습니다",
        "Board full — unit placed on the bench.", "配置欄がいっぱいのため待機欄に配置しました");
    public static string ShopNoSpaceForUnit => Pick("배치칸과 대기칸이 모두 차 유닛을 획득할 수 없습니다",
        "Board and bench are full — cannot acquire unit.", "配置欄と待機欄がいっぱいでユニットを獲得できません");
    public static string ShopGradePoolLocked(int grade, string names) => Format("{0}등급 상점 풀이 고정되었습니다 — {1}",
        "Grade {0} shop pool locked — {1}", "グレード{0}ショッププール固定 — {1}", grade, names);

    // ── Grade ──
    public static string GradeNameFormat(int grade) => Format("{0}등급", "Grade {0}", "グレード{0}", grade);

    // ── Level-up selection ──
    public static string LevelUpStartTitle => Pick("시작 유닛 선택", "Choose Starting Unit", "開始ユニット選択");
    public static string LevelUpStartSubtitle => "CHOOSE YOUR FIRST UNIT";
    public static string LevelUpStartHint => Pick("첫 유닛 1개를 선택하세요", "Select your first unit.", "最初のユニットを1体選んでください");
    public static string LevelUpTitleFormat(int level) => Format("레벨 {0} 달성!", "Level {0} Reached!", "レベル{0}達成！", level);
    public static string LevelUpSubtitle => "LEVEL UP REWARD";
    public static string LevelUpHint => Pick("유닛 3개 중 하나를 선택하세요", "Choose one of three units.", "3体のユニットから1体を選んでください");
    public static string LevelUpReroll => Pick("카드 다시 고르기 (1회)", "Reroll Cards (1x)", "カード再選択 (1回)");
    public static string LevelUpRerollUsed => Pick("리롤 사용됨", "Reroll Used", "リロール使用済");
    public static string LevelUpGradeOdds => Pick("등급 확률", "Grade Odds", "グレード確率");
    public static string LevelUpRegisteredPool => Pick("등록된 풀", "Registered Pool", "登録プール");
    public static string LevelUpOwnedPoolHint => Pick("보유 유닛 풀", "Owned Unit Pool", "所持ユニットプール");
    public static string LevelUpNoRegisteredUnits => Pick("아직 등록된 유닛이 없습니다", "No registered units yet.", "登録されたユニットがまだありません");
    public static string LevelUpTapToAcquire => Pick("터치하여 획득", "Tap to acquire", "タップで獲得");
    public static string LevelUpAcquiredBurst => Pick("획득!", "Acquired!", "獲得!");

    // ── Board buff panel ──
    public static string BoardBuffSynergyLabel => Pick("조합 시너지", "Synergy Effects", "シナジー効果");
    public static string BoardBuffToggleShort => Pick("시너지", "Synergy", "シナジー");
    public static string BoardBuffHeaderHint => Pick("보스 · 조합 시너지 · 유닛 효과 칸",
        "Boss · synergy · unit effect tiles", "ボス · シナジー · ユニット効果マス");
    public static string BoardBuffHeaderCountFormat(int count) => Format(
        "총 {0}개 적용 중", "{0} active", "合計 {0}件 適用中", count);
    public static string BoardBuffEmpty => Pick(
        "현재 적용 중인 시너지·보스·유닛 효과가 없습니다.",
        "No active synergy, boss, or unit tile effects.",
        "適用中のシナジー·ボス·ユニット効果はありません。");
    public static string BoardBuffCategoryBoss => Pick("보스 보상", "Boss Reward", "ボス報酬");
    public static string BoardBuffCategorySynergy => Pick("조합 시너지", "Synergy", "シナジー");
    public static string BoardBuffCategoryUnit => Pick("유닛 효과", "Unit Effect", "ユニット効果");

    public static class BoardBuffCategoryKey
    {
        public const string Boss = "boss";
        public const string Synergy = "synergy";
        public const string UnitEffect = "unit";
    }

    public static string GetBoardBuffCategoryLabel(string key)
    {
        switch (key)
        {
            case BoardBuffCategoryKey.Synergy: return BoardBuffCategorySynergy;
            case BoardBuffCategoryKey.UnitEffect: return BoardBuffCategoryUnit;
            default: return BoardBuffCategoryBoss;
        }
    }

    // ── Trait panel ──
    public static string TraitToggleTraits => Pick("조합", "Synergies", "シナジー");
    public static string TraitToggleUnitInfo => Pick("유닛 정보", "Unit Info", "ユニット情報");

    // ── Shop grade pool panel ──
    public static string ShopGradePoolLockedLabel => Pick("풀 고정", "Pool Locked", "プール固定");
    public static string ShopGradePoolProgressFormat(int count, int threshold) => Format(
        "풀 {0}/{1}", "Pool {0}/{1}", "プール {0}/{1}", count, threshold);
    public static string ShopGradePoolRemainingHint(int remaining) => Format(
        "({0}종 더 구매 시 고정)", "({0} more unique buys to lock)", "({0}種類購入で固定)", remaining);

    // ── Round wave tooltip ──
    public static string RoundWaveTooltipTitleFormat(int round, string partyName) => Format(
        "{0}라운드  <color=#FFC94D>{1}</color>",
        "Round {0}  <color=#FFC94D>{1}</color>",
        "第{0}ラウンド  <color=#FFC94D>{1}</color>",
        round, partyName);

    // ── Boss tile buffs ──
    public static string BossBuffTitleCenterPower => Pick("중앙 공명", "Center Resonance", "中央共鳴");
    public static string BossBuffTitleFrontlineHaste => Pick("전열 돌파", "Frontline Break", "前線突破");
    public static string BossBuffTitleBacklinePower => Pick("후열 망원", "Backline Scope", "後列望遠");
    public static string BossBuffTitleLeftShield => Pick("상단 일제", "Top Volley", "上段一斉");
    public static string BossBuffTitleRightHaste => Pick("하단 기동", "Bottom Mobility", "下段機動");
    public static string BossBuffTitleMiddleColumnPower => Pick("중앙 열 증폭", "Center Column Amp", "中央列増幅");
    public static string BossBuffTitleCornerPower => Pick("코너 포인트", "Corner Point", "コーナーポイント");
    public static string BossBuffTitleUnknown => Pick("알 수 없는 버프", "Unknown Buff", "不明なバフ");

    public static string BossBuffEffectCenterPower => Pick("조합 기여 ×2", "Synergy count ×2", "シナジー寄与 ×2");
    public static string BossBuffEffectBacklinePower => Pick("사거리 +1.5", "Range +1.5", "射程 +1.5");
    public static string BossBuffEffectCornerPower => Pick("사거리 +1.0, 공격력 +12%", "Range +1.0, ATK +12%", "射程 +1.0, 攻撃力 +12%");
    public static string BossBuffEffectFrontlineHaste => Pick("공격속도 +10%", "Attack speed +10%", "攻撃速度 +10%");
    public static string BossBuffEffectLeftShield => Pick("공격력 +10%", "Attack +10%", "攻撃力 +10%");
    public static string BossBuffEffectRightHaste => Pick("공격속도 +8%", "Attack speed +8%", "攻撃速度 +8%");
    public static string BossBuffEffectMiddleColumnPower => Pick("공격력 +10%, 특수공격 +20%", "Attack +10%, special +20%", "攻撃力 +10%, 特殊攻撃 +20%");

    public static string BossBuffDescCenterPower => Pick("가운데 1칸 · 조합 2명분 집계", "Center tile · synergy counts ×2", "中央1マス · シナジー2人分集計");
    public static string BossBuffDescBacklinePower => Pick("좌측 열 · 사거리 +1.5", "Left column · range +1.5", "左列 · 射程 +1.5");
    public static string BossBuffDescCornerPower => Pick("4모서리 · 사거리 +1.0, 공격력 +12%", "4 corners · range +1.0, ATK +12%", "4隅 · 射程 +1.0, 攻撃力 +12%");
    public static string BossBuffDescFrontlineHaste => Pick("우측 열 · 공격속도 +10%", "Right column · attack speed +10%", "右列 · 攻撃速度 +10%");
    public static string BossBuffDescLeftShield => Pick("위쪽 행 · 공격력 +10%", "Top row · attack +10%", "上段 · 攻撃力 +10%");
    public static string BossBuffDescRightHaste => Pick("아래쪽 행 · 공격속도 +8%", "Bottom row · attack speed +8%", "下段 · 攻撃速度 +8%");
    public static string BossBuffDescMiddleColumnPower => Pick("가운데 열 · 공격력 +10%, 특수공격 +20%", "Center column · ATK +10%, special +20%", "中央列 · 攻撃力 +10%, 特殊攻撃 +20%");

    public static string BossBuffLocCenterCell(int row, int col) => Format(
        "중앙 1칸 (행 {0}, 열 {1})", "Center tile (row {0}, col {1})", "中央1マス (行{0}, 列{1})", row, col);
    public static string BossBuffLocFrontlineColumn(int col) => Format(
        "전열 · 가장 우측 열 전체 (열 {0})", "Front · rightmost column (col {0})", "前線 · 最右列全体 (列{0})", col);
    public static string BossBuffLocBacklineColumn => Pick("후열 · 가장 좌측 열 전체 (열 1)", "Back · leftmost column (col 1)", "後列 · 最左列全体 (列1)");
    public static string BossBuffLocTopRow => Pick("상단 행 전체 (행 1)", "Top row (row 1)", "上段行全体 (行1)");
    public static string BossBuffLocBottomRow(int row) => Format(
        "하단 행 전체 (행 {0})", "Bottom row (row {0})", "下段行全体 (行{0})", row);
    public static string BossBuffLocCenterColumn(int col) => Format(
        "중앙 열 전체 (열 {0})", "Center column (col {0})", "中央列全体 (列{0})", col);
    public static string BossBuffLocCorners => Pick("네 모서리 칸", "Four corner tiles", "四隅マス");
    public static string BossBuffLocBoard => Pick("배치칸", "Board tile", "配置マス");

    // ── Damage meter HUD ──
    public static string DamageMeterTitle => Pick("전투 딜", "Battle Damage", "戦闘ダメージ");
    public static string DamageMeterExpand => Pick("Top 5", "Top 5", "Top5");
    public static string DamageMeterCollapse => Pick("접기", "Collapse", "折りたたむ");
    public static string DamageMeterEmptyRow => Pick("—", "—", "—");
    public static string DamageMeterPercent(float percent) => Format("{0:0}%", "{0:0}%", "{0:0}%", percent);
    public static string DamageMeterFooter(int round, int totalDamage) => Format(
        "R{0} · 총 {1:N0}", "R{0} · Total {1:N0}", "R{0} · 合計 {1:N0}", round, totalDamage);

    // ── Party types ──
    public static string PartySwarm => Pick("물량 파티", "Swarm Party", "物量パーティ");
    public static string PartyBruiser => Pick("떡대 파티", "Bruiser Party", "タンクパーティ");
    public static string PartyBreakthrough => Pick("돌파 파티", "Breakthrough Party", "突破パーティ");
    public static string PartyRanged => Pick("원거리 파티", "Ranged Party", "遠距離パーティ");
    public static string PartyBalanced => Pick("밸런스 파티", "Balanced Party", "バランスパーティ");
    public static string PartyBoss => Pick("보스 파티", "Boss Party", "ボスパーティ");
    public static string PartyUnknown => Pick("알 수 없는 파티", "Unknown Party", "不明なパーティ");
    public static string PartySwarmHud => Pick("물량", "Swarm", "物量");
    public static string PartyBruiserHud => Pick("떡대", "Bruiser", "タンク");
    public static string PartyBreakthroughHud => Pick("돌파", "Breakthrough", "突破");
    public static string PartyRangedHud => Pick("원거리", "Ranged", "遠距離");
    public static string PartyBalancedHud => Pick("밸런스", "Balanced", "バランス");
    public static string PartyBossHud => Pick("보스", "Boss", "ボス");
    public static string PartyUnknownHud => Pick("파티", "Party", "パーティ");
    public static string PartyInfoUnavailable => Pick("파티 정보를 불러올 수 없습니다",
        "Unable to load party info.", "パーティ情報を読み込めません");
    public static string PartySwarmDetail => Pick("물량형 특수 몹 다수. 잡몹 증원과 겹쳐 적 수가 가장 많습니다.",
        "Many swarm special enemies. Reinforcements make this the largest wave.", "物量型特殊モブ多数。雑魚増援と重なり敵数が最多。");
    public static string PartyBruiserDetail => Pick("탱커 특수 몹 전열 + 원거리 후방. 구간별 등장 적이 달라집니다.",
        "Tank specials in front, ranged in back. Enemy mix varies by phase.", "タンク特殊モブ前衛＋遠距離後方。区間ごとに敵構成が異なります。");
    public static string PartyBreakthroughDetail => Pick("고속 돌파 특수 몹 중심. 21R 이후 8·9번(초고속·부활) 등장.",
        "Fast breakthrough specials. After R21, types 8·9 (ultra-fast, revive) appear.", "高速突破特殊モブ中心。21R以降8·9番（超高速・復活）登場。");
    public static string PartyRangedDetail => Pick("원거리(2·6) 특수 몹 화력 + 탱커 전열. 11R부터 6·7번 추가.",
        "Ranged specials (2·6) plus tank front. Types 6·7 added from R11.", "遠距離(2·6)特殊モブ火力＋タンク前衛。11Rから6·7番追加。");
    public static string PartyBalancedDetail => Pick("특수 몹 역할 고른 혼합. 현재 구간 허용 적만 등장.",
        "Balanced mix of special roles. Only enemies allowed in the current phase.", "特殊モブ役割の均等混合。現区間で許可された敵のみ登場。");
    public static string PartyBossDetail => Pick("보스 1 + 구간별 특수 몹 호위. 10R·20R·30R 보스 단계.",
        "One boss plus phase escorts. Boss stages at R10, R20, R30.", "ボス1体＋区間別特殊モブ護衛。10R·20R·30Rボス段階。");
    public static string PartyTooltipFormat(int specialCount, string detail) => Format(
        "특수 몹: {0}마리 + 잡몹 상시 증원\n{1}",
        "Specials: {0} + constant minion reinforcements\n{1}",
        "特殊モブ: {0}体 + 雑魚常時増援\n{1}", specialCount, detail);

    public static string StatAttackSpeed => Pick("공격속도", "Attack Speed", "攻撃速度");
    public static string StatAttack => Pick("공격력", "Attack", "攻撃力");
    public static string StatHealth => Pick("체력", "HP", "HP");
    public static string StatPattern => Pick("패턴", "Pattern", "パターン");
    public static string StatSynergy => Pick("조합", "Synergy", "シナジー");
    public static string StatNone => Pick("없음", "None", "なし");
    public static string StatPerSecond => Pick("/초", "/s", "/秒");
    public static string StatSecondUnit => Pick("초", "s", "秒");
    public static string StatRange => Pick("사거리", "Range", "射程");
    public static string StatRangeGlobal => Pick("전장 전역", "Entire Battlefield", "戦場全域");
    public static string StatAttackFormat(int total, float mul) => Format("공격력: {0} (진화 x{1:0.00})",
        "Attack: {0} (Evolution x{1:0.00})", "攻撃力: {0} (進化 x{1:0.00})", total, mul);
    public static string StatRangeFormat(float range) => range <= 0f
        ? Format("사거리: {0}", "Range: {0}", "射程: {0}", StatRangeGlobal)
        : Format("사거리: {0:0.#}", "Range: {0:0.#}", "射程: {0:0.#}", range);

    // ── Placement ──
    public static string PlacementInvalid => Pick("배치할 수 없습니다", "Cannot deploy here.", "配置できません");
    public static string PlacementNotBoard => Pick("배치칸이 아닌 곳에는 이동할 수 없습니다",
        "Cannot move to a non-board tile.", "配置欄以外には移動できません");

    // ── Result screens ──
    public static string ResultBattleTitle => Pick("전투 결과", "Battle Result", "戦闘結果");
    public static string ResultWallFallen => Pick("아군 성벽이 무너졌습니다", "Your wall has fallen.", "味方の城壁が崩れました");
    public static string ResultRoundReached => Pick("도달 라운드", "Round Reached", "到達ラウンド");
    public static string ResultSurvivalTime => Pick("생존 시간", "Survival Time", "生存時間");
    public static string ResultKills => Pick("처치한 적", "Enemies Defeated", "撃破数");
    public static string ResultLevelReached => Pick("도달 레벨", "Level Reached", "到達レベル");
    public static string ResultGoldHeld => Pick("보유 골드", "Gold Held", "所持ゴールド");
    public static string ResultRetry => Pick("다시 도전", "Try Again", "再挑戦");
    public static string ResultReturnTitle => Pick("타이틀로", "Title Screen", "タイトルへ");
    public static string ResultDemoClearTitle => Pick("체험판 클리어!", "Demo Clear!", "体験版クリア！");
    public static string ResultDemoThanksFormat(int rounds) => Format(
        "{0}개의 모든 라운드를 막아냈습니다!\n끝까지 성벽을 지켜주셔서 감사합니다.",
        "You cleared all {0} rounds!\nThank you for defending the wall to the end.",
        "全{0}ラウンドを防ぎました！\n最後まで城壁を守ってくれてありがとうございます。", rounds);
    public static string ToastEnemyHpBoostBody(int round, float step, float cumulative) => Format(
        "{0}라운드 진입 — 적 체력이 {1:0.##}배 증가했습니다. (누적 ×{2:0.##})",
        "Round {0} — Enemy HP ×{1:0.##} (Total ×{2:0.##})",
        "第{0}ラウンド — 敵HP {1:0.##}倍 (累計 ×{2:0.##})", round, step, cumulative);
    public static string ResultDemoClearBody => Pick(
        "정식 버전에서는 더 많은 유닛과 시너지,\n그리고 강력한 적들이 기다리고 있어요.\n앞으로의 업데이트를 기대해주세요!",
        "The full version will bring more units, synergies,\nand powerful enemies.\nStay tuned for future updates!",
        "正式版ではより多くのユニットとシナジー、\nそして強力な敵が待っています。\n今後のアップデートをお楽しみに！");
    public static string ResultPlayAgain => Pick("다시 플레이", "Play Again", "もう一度プレイ");
    public static string ResultMedalsEarned => Pick("획득 훈장", "Medals Earned", "獲得勲章");
    public static string ResultMedalsFormat(int amount) => Format("+{0}", "+{0}", "+{0}", amount);
    public static string ResultMedalsRoundLabel(int round) => Format("라운드 ({0}R×2)", "Round ({0}×2)", "ラウンド ({0}R×2)", round);
    public static string ResultMedalsMilestoneLabel(int round)
    {
        if (round < 10) return Pick("마일스톤", "Milestone", "マイルストーン");
        if (round < 20) return Pick("마일스톤 (10R+)", "Milestone (R10+)", "マイルストーン (10R+)");
        if (round < 30) return Pick("마일스톤 (10·20R+)", "Milestone (R10·20+)", "マイルストーン (10·20R+)");
        return Pick("마일스톤 (10·20·30R+)", "Milestone (R10·20·30+)", "マイルストーン (10·20·30R+)");
    }
    public static string ResultMedalsFirstMeetLabel(int count) => Format("첫 발견 ({0}종)", "First meet ({0} units)", "初遭遇 ({0}体)", count);

    // ── Mercenary Archive ──
    public static string ArchiveTitle => Pick("용병단 아카이브", "Mercenary Archive", "傭兵団アーカイブ");
    public static string ArchiveMedalsFormat(int amount) => Format("훈장 {0}", "Medals {0}", "勲章 {0}", amount);
    public static string ArchiveLockedHint => Pick("전투에서 만나면 기록됩니다", "Encounter in battle to unlock.", "戦闘で出会うと記録されます");
    public static string ArchiveAttackSlot => Pick("공격", "Attack", "攻撃");
    public static string ArchiveMobilitySlot => Pick("기동", "Mobility", "機動");
    public static string ArchiveMobilityDisabled => Pick("해당 없음", "N/A", "該当なし");
    public static string ArchiveUpgrade => Pick("강화", "Upgrade", "強化");
    public static string ArchiveClose => Pick("닫기", "Close", "閉じる");
    public static string ArchiveFilterAll => Pick("전체", "All", "全体");
    public static string ArchiveLevelFormat(int current, int max) => Format("Lv{0}/{1}", "Lv{0}/{1}", "Lv{0}/{1}", current, max);
    public static string ArchiveCostFormat(int cost) => Format("{0} 훈장", "{0} medals", "{0} 勲章", cost);
    public static string ArchivePreviewAttackFormat(int before, int after) => Format("공격 {0} → {1}", "ATK {0} → {1}", "攻撃 {0} → {1}", before, after);
    public static string ArchivePreviewSpeedFormat(string before, string after) => Format("공속 {0} → {1}", "APS {0} → {1}", "攻速 {0} → {1}", before, after);
    public static string ArchivePreviewRangeFormat(float before, float after) => Format("사거리 {0:0.#} → {1:0.#}", "Range {0:0.#} → {1:0.#}", "射程 {0:0.#} → {1:0.#}", before, after);
    public static string ArchivePreviewEconomyFormat(float before, float after) => Format("골드 쿨 −{0:0.#}% → −{1:0.#}%", "Gold CD −{0:0.#}% → −{1:0.#}%", "ゴールド−{0:0.#}% → −{1:0.#}%", before, after);
    public static string ArchiveGradeFilterFormat(int grade) => Format("{0}등급", "Grade {0}", "{0}級", grade);
    public static string ArchiveBadge => "ARCHIVE";
    public static string ArchiveRosterSection => Pick("유닛 기록", "Unit Roster", "ユニット記録");
    public static string ArchiveUpgradeSection => Pick("영구 강화", "Permanent Upgrades", "永久強化");
    public static string ArchivePreviewSection => Pick("1진화 미리보기", "Evolution 1 Preview", "1進化プレビュー");
    public static string ArchiveMaxLabel => "MAX";
    public static string ArchiveSpecialtyRange => Pick("사거리", "Range", "射程");
    public static string ArchiveSpecialtyEconomy => Pick("골드", "Gold", "ゴールド");
    public static string ArchivePermanentLabel => Pick("+영구", "+Permanent", "+永久");
    public static string ArchivePermanentAttackFormat(int value) => Format("공격+{0}", "ATK+{0}", "攻撃+{0}", value);
    public static string ArchivePermanentMobilityFormat(float percent) => Format("기동−{0:0.#}%", "Mob −{0:0.#}%", "機動−{0:0.#}%", percent);
    public static string ArchivePermanentRangeFormat(float value) => Format("사거리+{0:0.#}", "Range+{0:0.#}", "射程+{0:0.#}", value);
    public static string ArchivePermanentEconomyFormat(float percent) => Format("골드−{0:0.#}%", "Gold −{0:0.#}%", "ゴールド−{0:0.#}%", percent);
    public static string ArchiveUnlockBoardPlaced => Pick("보드 배치 1회 필요", "Deploy on board once", "ボード配置が1回必要");
    public static string ArchiveUnlockEvolution3 => Pick("3진화 달성 필요", "Reach 3-star evolution", "3進化が必要");
    public static string ArchiveUnlockEvolution5 => Pick("5진화 달성 필요", "Reach 5-star evolution", "5進化が必要");
    public static string ArchiveDiscoveredOnly => Pick("기록됨 · 강화 잠김", "Recorded · upgrades locked", "記録済 · 強化ロック");

    public static string ArchiveFilterFavorites => Pick("★ 즐겨찾기", "★ Favorites", "★ お気に入り");
    public static string ArchiveFilterTraits => Pick("조합", "Synergy", "シナジー");
    public static string ArchiveTabUpgrade => Pick("강화", "Upgrades", "強化");
    public static string ArchiveTabCodex => Pick("도감", "Codex", "図鑑");
    public static string ArchiveTabAwakening => Pick("각성", "Awakening", "覚醒");
    public static string ArchiveAwakeningSection => Pick("유닛 각성", "Unit Awakening", "ユニット覚醒");
    public static string ArchiveAwakeningTierFormat(int tier) => Format("각성 {0}", "Awakening {0}", "覚醒 {0}", tier);
    public static string ArchiveAwakeningLevelFormat(int current, int max) => Format("{0}/{1}단", "Tier {0}/{1}", "{0}/{1}段", current, max);
    public static string ArchiveAwakeningEffectFormat(int attackBonus) => Format("공격 +{0} (누적)", "ATK +{0} (total)", "攻撃 +{0} (累計)", attackBonus);
    public static string ArchiveAwakeningGenericEffect => Pick("진화 패턴 영구 보정", "Permanent evolution pattern boost", "進化パターン永久補正");
    public static string ArchiveAwakeningNeedUpgradeUnlock => Pick("강화 해금 후 가능", "Unlock upgrades first", "強化解錠後に可能");
    public static string ArchiveAwakeningNeedStatLevels => Pick("영구 강화 3레벨 이상 필요", "Need 3+ permanent upgrade levels", "永久強化3以上が必要");
    public static string ArchiveAwakeningNeedEvolution5 => Pick("5진화 달성(누적) 필요", "Need lifetime 5-star evolution", "5進化達成(累計)が必要");
    public static string ArchiveAwakeningNextTierFormat(int tier) => Format("각성 {0} 해금", "Unlock Awakening {0}", "覚醒 {0} 解放", tier);
    public static string ArchiveCodexTraitsFormat(string traits) => Format("조합  {0}", "Synergy  {0}", "シナジー  {0}", traits);
    public static string ArchiveCodexGradeFormat(int grade) => Format("{0}등급 유닛", "Grade {0} unit", "{0}級ユニット", grade);
    public static string ArchiveCodexEvolutionHeader => Pick("진화 특성 (4·6·9)", "Evolution traits (4·6·9)", "進化特性 (4·6·9)");
    public static string ArchiveCodexEvolutionLineFormat(int level, string desc) => Format("{0}★  {1}", "{0}★  {1}", "{0}★  {1}", level, desc);
    public static string ArchiveCodexAwakeningHeader => Pick("각성 효과", "Awakening effects", "覚醒効果");
    public static string ArchiveCodexAwakeningLineFormat(int tier, string desc) => Format("각성{0}  {1}", "Awk {0}  {1}", "覚醒{0}  {1}", tier, desc);
    public static string ArchiveCodexSummaryFormat(int grade, string name) => Format(
        "{0}등급 {1} — 전투에서 만난 기록과 진화·각성 정보를 확인할 수 있습니다.",
        "Grade {0} {1} — review encounter records, evolution, and awakening info.",
        "{0}級 {1} — 遭遇記録と進化・覚醒情報を確認できます。", grade, name);
    public static string ArchivePermanentAwakeningFormat(int value) => Format("각성+{0}", "Awk+{0}", "覚醒+{0}", value);
    public static string ArchiveFavoriteToggle => Pick("즐겨찾기", "Favorite", "お気に入り");

    // ── Boss reward ──
    public static string BossRewardTitle => Pick("보스 처치 보상", "Boss Defeat Reward", "ボス撃破報酬");
    public static string BossRewardSubtitle => Pick("보스 격파!", "Boss Defeated!", "ボス撃破！");
    public static string BossRewardAllClaimed => Pick("이번 게임에서 획득 가능한\n보스 보상을 모두 획득했습니다.",
        "All boss rewards available\nthis run have been claimed.", "今回のゲームで獲得可能な\nボス報酬をすべて獲得しました。");
    public static string BossRewardConfirm => Pick("확인", "OK", "確認");
    public static string BossRewardPickOne => Pick("하나를 선택하세요", "Choose one.", "1つ選んでください");
    public static string ResultDemoBadge => "DEMO COMPLETE";

    // ── First round guide toast ──
    public static string FirstRoundGuideTitle => Pick("1라운드 준비", "Round 1 Prep", "第1ラウンド準備");
    public static string FirstRoundGuideBodyPurchase => Pick(
        "① 상점에서 유닛 <color=#FFC94D>1마리 구매</color>  →  ② <color=#FFC94D>배치칸</color>에 배치  →  ③ <color=#FFC94D>1라운드 시작</color> 버튼 또는 카운트다운",
        "① Buy <color=#FFC94D>1 unit</color> in the shop  →  ② Deploy on the <color=#FFC94D>board</color>  →  ③ Press <color=#FFC94D>Start Round 1</color> or wait for countdown",
        "① ショップでユニット<color=#FFC94D>1体購入</color>  →  ② <color=#FFC94D>配置欄</color>に配置  →  ③ <color=#FFC94D>第1ラウンド開始</color>ボタンまたはカウントダウン");
    public static string FirstRoundGuideBodyDeploy => Pick(
        "✓ 유닛 구매 완료  →  ② <color=#FFC94D>배치칸</color>에 자유롭게 배치  →  ③ <color=#FFC94D>1라운드 시작</color> 버튼 또는 카운트다운",
        "✓ Unit purchased  →  ② Deploy freely on the <color=#FFC94D>board</color>  →  ③ Press <color=#FFC94D>Start Round 1</color> or wait for countdown",
        "✓ ユニット購入済  →  ② <color=#FFC94D>配置欄</color>に自由配置  →  ③ <color=#FFC94D>第1ラウンド開始</color>ボタンまたはカウントダウン");

    // ── Trait unit info row ──
    public static string TraitUnitInfoRowFormat(string unitName, float atkSpeed, int evolution, int attack) => Format(
        "{0} | 공속 {1:0.0}s | 진화 {2} | 공격력 {3}",
        "{0} | APS {1:0.0}s | Evo {2} | ATK {3}",
        "{0} | 攻速 {1:0.0}s | 進化 {2} | 攻撃力 {3}",
        unitName, atkSpeed, evolution, attack);
    public static string TraitTooltipOwnedFormat(int count) => Format(
        "보유 {0}", "Owned {0}", "所持 {0}", count);

    // ── Board buff unit effects ──
    public static string BoardBuffPenguinAuraTitle => Pick("펭귄 인접 오라", "Penguin Adjacent Aura", "ペンギン隣接オーラ");
    public static string BoardBuffPenguinAuraLocationFormat(int penguinCount, int cellCount) => Format(
        "펭귄 {0}기 · 인접 오라 칸 {1}칸",
        "{0} penguin(s) · {1} adjacent aura tile(s)",
        "ペンギン{0}体 · 隣接オーラ{1}マス",
        penguinCount, cellCount);
    public static string BoardBuffUnitAuraSideFormat(string unitName) => Format(
        "{0} — 좌·우 오라", "{0} — Left/Right Aura", "{0} — 左右オーラ", unitName);
    public static string BoardBuffUnitAuraVerticalFormat(string unitName) => Format(
        "{0} — 상·하 오라", "{0} — Top/Bottom Aura", "{0} — 上下オーラ", unitName);
    public static string BoardBuffUnitBarrierFormat(string unitName) => Format(
        "{0} — 방어벽 장판", "{0} — Barrier Pad", "{0} — 防御壁床", unitName);
    public static string BoardBuffUnit16LocationFormat(int row, int col, int span) => Format(
        "기준 (행 {0}, 열 {1}) · 좌·우 {2}칸",
        "Anchor (row {0}, col {1}) · {2} tile(s) left/right",
        "基準 (行{0}, 列{1}) · 左右{2}マス",
        row, col, span);
    public static string BoardBuffUnit10LocationFormat(int row, int col) => Format(
        "기준 (행 {0}, 열 {1}) · 위·아래 1칸",
        "Anchor (row {0}, col {1}) · 1 tile above/below",
        "基準 (行{0}, 列{1}) · 上下1マス",
        row, col);
    public static string BoardBuffUnit19Location => Pick(
        "전장 우측 방어벽 (보라 장판)",
        "Right battlefield barrier (purple pad)",
        "戦場右側防御壁 (紫床)");
    public static string Unit10PadEffectFormat(string percent) => Format(
        "위·아래 파란 칸의 아군 공격속도 {0} 강화를 부여하며, 범위는 파란 칸으로 표시됩니다.",
        "Allies on blue tiles above/below gain {0} attack speed; range shown as blue tiles.",
        "上下の青マスの味方に攻撃速度{0}を付与。範囲は青マスで表示。",
        percent);
    public static string Unit16AuraEffectFormat(int span, string buffText) => Format(
        "같은 행 좌·우 {0}칸 아군에게 {1}강화를 부여하며, 범위는 빨간 칸으로 표시됩니다.",
        "Allies within {0} tile(s) left/right on the same row gain {1}buffs; range shown as red tiles.",
        "同じ行の左右{0}マスの味方に{1}強化を付与。範囲は赤マスで表示。",
        span, buffText);
    public static string Unit16AuraBuffAttack(string percent) => Format("공격력 {0}", "ATK {0}", "攻撃力 {0}", percent);
    public static string Unit16AuraBuffAttackSpeed(string percent) => Format("공격속도 {0}", "APS {0}", "攻速 {0}", percent);
    public static string Unit19BarrierEffectFormat(string timing) => Format(
        "전장 우측 보라 장판 안의 적에게 {0} 피해를 줍니다.",
        "Enemies on the purple pad on the right take damage {0}.",
        "戦場右側の紫床内の敵に{0}ダメージ。",
        timing);
    public static string TimingOncePer(float seconds)
    {
        bool whole = seconds >= 1f && UnityEngine.Mathf.Abs(seconds - UnityEngine.Mathf.Round(seconds)) < 0.001f;
        if (whole)
        {
            return Format("{0}초에 1회씩", "once every {0}s", "{0}秒に1回", (int)UnityEngine.Mathf.Round(seconds));
        }

        return Format("{0:0.##}초에 1회씩", "once every {0:0.##}s", "{0:0.##}秒に1回", seconds);
    }
    public static string UnitFallbackNameFormat(int unitNumber) => Format(
        "유닛 {0}", "Unit {0}", "ユニット {0}", unitNumber);

    public static bool MatchesEnglishKey(string enKey, string candidate)
    {
        if (string.IsNullOrEmpty(candidate)) return false;
        if (candidate == enKey) return true;
        if (enKey == "Used" && (candidate == "사용됨" || candidate == "Used" || candidate == "使用済"))
        {
            return true;
        }
        if (GameLocalizationChineseLookup.GetTraditional(enKey) == candidate) return true;
        if (GameLocalizationChineseLookup.GetSimplified(enKey) == candidate) return true;
        return GameLocalizationExtraLookup.MatchesEnglishKey(enKey, candidate);
    }

    static string Pick(string ko, string en, string ja, string zhTw = null, string zhCn = null)
    {
        switch (CurrentLanguage)
        {
            case GameLanguage.English: return en;
            case GameLanguage.Japanese: return ja;
            case GameLanguage.TraditionalChinese:
                if (!string.IsNullOrEmpty(zhTw)) return zhTw;
                return GameLocalizationChineseLookup.GetTraditional(en);
            case GameLanguage.SimplifiedChinese:
                if (!string.IsNullOrEmpty(zhCn)) return zhCn;
                if (!string.IsNullOrEmpty(zhTw)) return GameLocalizationChineseLookup.ToSimplified(zhTw);
                return GameLocalizationChineseLookup.GetSimplified(en);
            case GameLanguage.Thai:
            case GameLanguage.Spanish:
            case GameLanguage.Portuguese:
            case GameLanguage.French:
            case GameLanguage.Italian:
            case GameLanguage.German:
                return GameLocalizationExtraLookup.Get(en, CurrentLanguage);
            default: return ko;
        }
    }
}
