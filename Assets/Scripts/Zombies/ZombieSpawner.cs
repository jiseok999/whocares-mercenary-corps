using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 좀비를 생성하는 스포너
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    enum SpawnRole
    {
        Tanking = 0,
        GeneralOrFast = 1,
        Ranged = 2
    }

    [Header("Spawn Settings - Legacy")]
    public GameObject[] zombiePrefabs; // 레거시: 프리팹 배열 (우선순위 낮음)
    public ZombieData[] zombieDataList; // 좀비 데이터 배열 (레거시, CSV 우선)
    public float spawnInterval = 5f;
    public float spawnIntervalMin = 3f;
    public float spawnIntervalMax = 7f;
    public int maxZombiesPerRow = 1;
    
    [Header("CSV Data Loader")]
    [Tooltip("CSV에서 좀비 데이터를 로드하는 로더 (필수)")]
    public ZombieDataLoader zombieDataLoader;
    
    [Header("Round Spawn Loader")]
    [Tooltip("CSV에서 라운드별 스폰 데이터를 로드하는 로더 (필수)")]
    public RoundSpawnLoader roundSpawnLoader;
    
    [Header("Spawn Position")]
    public float spawnX = 10f;
    public float[] spawnYPositions; // 각 행의 Y 좌표
    
    [Header("Wave Spawn Zone")]
    [Tooltip("몬스터가 몰려 나오는 영역의 가로 폭(월드 단위)")]
    public float crowdZoneWidth = 2.8f;
    [Tooltip("스폰 위치 간 최소 거리")]
    public float minSpawnSeparation = 0.55f;
    [Tooltip("겹침 방지 배치 최대 시도 횟수(마리당)")]
    public int maxPlacementAttemptsPerZombie = 48;
    [Tooltip("카메라 가장자리에서 안쪽으로 띄우는 여백")]
    public float cameraBoundsInset = 0.35f;
    [Tooltip("마법진 표시 후 적이 나오기까지 대기(초)")]
    public float magicCircleSummonDelay = 0.4f;
    [Tooltip("연속 소환 간격(초)")]
    public float staggerBetweenSummons = 0.06f;

    [Header("Combat Baseline Spawn")]
    [Tooltip("물량 공세 기준 간격(초) — 라운드가 오를수록 자동으로 짧아지고 ±30% 랜덤")]
    public float combatBaselineSpawnInterval = 1.3f;
    [Tooltip("물량 공세에 쓰는 잡몹 UID (보스 UID 불가)")]
    public string combatBaselineMobUid = "mon_pig_1";
    [Tooltip("기준 소환 최소 간격(겹침 방지, 월드)")]
    public float baselineSpawnSeparation = 0.48f;
    [Tooltip("기준 소환 X 구간을 스폰 라인(오른쪽) 쪽으로 당김. 0=변경 없음, 1=가능한 한 뒤")]
    public float baselineSpawnRearShift = 0.42f;
    
    [Header("Round Scaling - Mobs Only")]
    [Tooltip("잡몹 스탯 증가가 시작되는 라운드")]
    public int mobScalingStartRound = EnemyCombatStats.MobScalingStartRound;
    [Tooltip("라운드당 잡몹 체력 증가율")]
    public float mobHealthGrowthPerRound = EnemyCombatStats.MobHealthGrowthPerRound;
    [Tooltip("라운드당 잡몹 공격력 증가율")]
    public float mobDamageGrowthPerRound = EnemyCombatStats.MobDamageGrowthPerRound;
    [Tooltip("라운드당 잡몹 이동속도 증가율")]
    public float mobSpeedGrowthPerRound = EnemyCombatStats.MobSpeedGrowthPerRound;

    public bool IsSpawningWave { get; private set; }
    
    private GridManager gridManager;
    private static Sprite cachedPigSprite;
    private static Sprite cachedEnemy2Sprite;
    private static Sprite cachedEnemy3Sprite;
    private ZombieData[] activeZombieDataList; // 실제 사용할 좀비 데이터 배열
    private bool spawnRowsReady = false;
    private bool initialRoundStarted = false;
    private readonly List<Vector2> _placementBuffer = new List<Vector2>(64);
    private readonly List<Vector2> _baselinePlacementBuffer = new List<Vector2>(32);
    private int _baselineSpawnPatternIndex;
    private readonly Dictionary<string, ZombieData> _scaledDataCache = new Dictionary<string, ZombieData>(64);
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        
        // 보드가 생성된 뒤 행 위치를 다시 찾기
        StartCoroutine(WaitForBoardAndInitRows());
        
        // CSV 로더가 있으면 CSV에서 데이터 로드
        if (zombieDataLoader != null)
        {
            zombieDataLoader.LoadZombieDataFromCSV();
            activeZombieDataList = zombieDataLoader.GetLoadedZombieDataArray();
            
            if (activeZombieDataList != null && activeZombieDataList.Length > 0)
            {
                Debug.Log($"CSV에서 {activeZombieDataList.Length}개의 좀비 데이터를 로드했습니다.");
            }
        }
        
        // CSV 로더가 없거나 데이터가 없으면 기존 zombieDataList 사용
        if (activeZombieDataList == null || activeZombieDataList.Length == 0)
        {
            activeZombieDataList = zombieDataList;
            Debug.Log("기존 ZombieData 배열을 사용합니다.");
        }
        
        if (roundSpawnLoader != null)
        {
            roundSpawnLoader.LoadRoundSpawnData();
        }
        else
        {
            Debug.LogWarning("RoundSpawnLoader가 없습니다. 라운드 웨이브 구성에 CSV를 사용할 수 없습니다.");
        }
    }
    
    System.Collections.IEnumerator WaitForBoardAndInitRows()
    {
        // 보드 생성 대기
        while (!InitializeSpawnRowsFromBoard())
        {
            yield return null;
        }
        
        spawnRowsReady = true;
        TryStartInitialRound();
    }

    void TryStartInitialRound()
    {
        if (initialRoundStarted || !spawnRowsReady) return;
        if (GameManager.Instance == null) return;

        initialRoundStarted = true;
        GameManager.Instance.BeginRound(GameManager.Instance.currentRound);
    }
    
    bool InitializeSpawnRowsFromBoard()
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        if (cells == null || cells.Length == 0) return false;
        
        Dictionary<int, float> rowToY = new Dictionary<int, float>();
        int maxRow = -1;
        
        foreach (BoardCell cell in cells)
        {
            if (cell == null || !cell.isBoardCell) continue;
            string name = cell.gameObject.name;
            // BoardCell_{row}_{col}
            if (!name.StartsWith("BoardCell_")) continue;
            string[] parts = name.Split('_');
            if (parts.Length < 3) continue;
            
            if (int.TryParse(parts[1], out int row))
            {
                if (!rowToY.ContainsKey(row))
                {
                    rowToY[row] = cell.transform.position.y;
                }
                maxRow = Mathf.Max(maxRow, row);
            }
        }
        
        if (maxRow < 0) return false;
        
        float[] rows = new float[maxRow + 1];
        for (int i = 0; i <= maxRow; i++)
        {
            if (rowToY.ContainsKey(i))
            {
                rows[i] = rowToY[i];
            }
        }
        spawnYPositions = rows;
        return true;
    }
    
    void Update()
    {
        if (!spawnRowsReady && !initialRoundStarted)
        {
            TryStartInitialRound();
        }
    }

    /// <summary>
    /// 라운드 동안 웨이브 큐를 20초 전투 시간 안에 순차 소환합니다.
    /// </summary>
    public void SpawnRoundWave(int round)
    {
        StopAllCoroutines();
        _baselinePlacementBuffer.Clear();
        _baselineSpawnPatternIndex = 0;
        StartCoroutine(SpawnRoundWaveRoutine(round));
        StartCoroutine(CombatBaselineSpawnRoutine(round));
    }

    public void CancelRoundSpawn()
    {
        StopAllCoroutines();
        IsSpawningWave = false;
    }

    IEnumerator SpawnRoundWaveRoutine(int round)
    {
        IsSpawningWave = true;

        if (!spawnRowsReady || spawnYPositions == null || spawnYPositions.Length == 0)
        {
            Debug.LogWarning("스폰 행이 준비되지 않아 웨이브를 건너뜁니다.");
            IsSpawningWave = false;
            yield break;
        }

        List<string> wave = RoundWaveController.BuildWaveForRound(round, roundSpawnLoader);
        _placementBuffer.Clear();
        _scaledDataCache.Clear();
        EnsureBossEntriesFirst(wave);
        int spawned = 0;
        int waveCount = wave.Count;

        float combatDuration = GameManager.Instance != null
            ? GameManager.Instance.RoundCombatDuration
            : 20f;
        float spawnWindow = combatDuration * 0.94f;

        for (int i = 0; i < wave.Count; i++)
        {
            yield return WaitWhileCombatPaused();
            if (!HasRoundCombatTimeRemaining())
            {
                break;
            }

            string uid = wave[i];
            float targetTime = GetScheduledSpawnTime(i, waveCount, spawnWindow, uid);
            yield return WaitUntilRoundCombatElapsed(targetTime);

            if (!HasRoundCombatTimeRemaining())
            {
                break;
            }

            if (!TryGetSpawnSeparation(uid, out float separation)) continue;
            if (!TryResolveSpawnPosition(uid, separation, out Vector2 pos)) continue;

            _placementBuffer.Add(pos);
            int row = GetNearestRowIndex(pos.y);

            float telegraph = Mathf.Min(magicCircleSummonDelay, 0.35f);
            float circleSize = separation * 2.4f;
            if (IsBossSpawnUid(uid))
            {
                circleSize *= 1.8f;
                telegraph = Mathf.Max(telegraph, 0.55f);
            }

            GameObject circle = SpawnMagicCircle.SpawnAt(new Vector3(pos.x, pos.y, 0f), circleSize);
            yield return WaitForSecondsRespectingPause(telegraph);

            if (circle != null)
            {
                Destroy(circle);
            }

            yield return WaitWhileCombatPaused();
            if (!HasRoundCombatTimeRemaining())
            {
                break;
            }

            if (TrySpawnZombieAt(uid, pos, row, round))
            {
                spawned++;
            }
            else if (IsBossSpawnUid(uid) && TryGetGuaranteedBossSpawnPosition(out Vector2 fallback)
                && TrySpawnZombieAt(uid, fallback, GetNearestRowIndex(fallback.y), round))
            {
                _placementBuffer.Add(fallback);
                spawned++;
                Debug.LogWarning($"보스 위치 재시도 성공: {uid} @ {fallback}");
            }
            else
            {
                _placementBuffer.RemoveAt(_placementBuffer.Count - 1);
                if (IsBossSpawnUid(uid))
                {
                    Debug.LogError($"보스 스폰 실패: {uid} (라운드 {round})");
                }
            }
        }

        Debug.Log($"라운드 {round} 연속 스폰: {spawned}/{wave.Count}마리 (전투 {combatDuration:0.#}초)");
        IsSpawningWave = false;
    }

    /// <summary>
    /// 전투 시간 동안 웨이브와 별도로 mon_pig_1을 랜덤 간격으로 계속 물량 소환합니다.
    /// 라운드가 오를수록 간격이 짧아지고 한 번에 나오는 수가 늘어납니다.
    /// </summary>
    IEnumerator CombatBaselineSpawnRoutine(int round)
    {
        float baseInterval = Mathf.Max(0.5f, combatBaselineSpawnInterval);
        string uid = string.IsNullOrEmpty(combatBaselineMobUid) ? "mon_pig_1" : combatBaselineMobUid;
        if (IsBossSpawnUid(uid)) uid = "mon_pig_1";

        while (true)
        {
            yield return WaitWhileCombatPaused();
            if (!HasRoundCombatTimeRemaining())
            {
                yield break;
            }

            if (spawnRowsReady && spawnYPositions != null && spawnYPositions.Length > 0)
            {
                int burst = EnemyCombatStats.GetBaselineBurstCount(round);
                for (int i = 0; i < burst; i++)
                {
                    yield return WaitWhileCombatPaused();
                    if (!HasRoundCombatTimeRemaining())
                    {
                        yield break;
                    }
                    yield return SpawnBaselineMobRoutine(uid, round);
                }
            }

            // 라운드 스케일 간격 + 랜덤 흔들림(±30%)으로 불규칙한 물량 공세 연출
            float interval = EnemyCombatStats.GetBaselineSpawnInterval(baseInterval, round);
            interval *= Random.Range(0.7f, 1.3f);
            yield return WaitForSecondsRespectingPause(interval);
        }
    }

    IEnumerator SpawnBaselineMobRoutine(string uid, int round)
    {
        if (IsBossSpawnUid(uid)) yield break;

        float separation = Mathf.Max(0.3f, baselineSpawnSeparation);
        if (!TryFindBaselineSpawnPosition(separation, out Vector2 pos)) yield break;

        _baselinePlacementBuffer.Add(pos);
        if (_baselinePlacementBuffer.Count > 24)
        {
            _baselinePlacementBuffer.RemoveAt(0);
        }

        int row = GetNearestRowIndex(pos.y);

        float telegraph = Mathf.Min(magicCircleSummonDelay, 0.35f);
        float circleSize = separation * 2.4f;
        GameObject circle = SpawnMagicCircle.SpawnAt(new Vector3(pos.x, pos.y, 0f), circleSize);
        yield return WaitForSecondsRespectingPause(telegraph);

        if (circle != null)
        {
            Destroy(circle);
        }

        yield return WaitWhileCombatPaused();
        if (!HasRoundCombatTimeRemaining())
        {
            yield break;
        }

        if (!TrySpawnZombieAt(uid, pos, row, round))
        {
            _baselinePlacementBuffer.RemoveAt(_baselinePlacementBuffer.Count - 1);
        }
    }

    /// <summary>기준 소환: 보드 옆~스폰 라인 전체 + 행/깊이 패턴을 돌려가며 배치.</summary>
    bool TryFindBaselineSpawnPosition(float separation, out Vector2 position)
    {
        position = default;
        if (!TryGetBaselineSpawnBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            return false;
        }

        int pattern = _baselineSpawnPatternIndex++ % 6;
        int depthSlot = pattern % 3;
        bool anchorRow = pattern >= 3;
        GetBaselineDepthBand(depthSlot, minX, maxX, out float bandMinX, out float bandMaxX);

        if (bandMaxX - bandMinX < 0.08f)
        {
            bandMinX = minX;
            bandMaxX = maxX;
        }

        float minDistSq = separation * separation;

        for (int attempt = 0; attempt < maxPlacementAttemptsPerZombie; attempt++)
        {
            float x = Random.Range(bandMinX, bandMaxX);
            float y;
            if (anchorRow && spawnYPositions != null && spawnYPositions.Length > 0)
            {
                int row = Random.Range(0, spawnYPositions.Length);
                y = spawnYPositions[row] + Random.Range(-0.28f, 0.28f);
            }
            else
            {
                y = Random.Range(minY, maxY);
            }

            Vector2 candidate = new Vector2(x, Mathf.Clamp(y, minY, maxY));
            if (!IsInsideSpawnZone(candidate, minX, maxX, minY, maxY)) continue;
            if (!IsFarEnoughForBaseline(candidate, minDistSq)) continue;

            position = candidate;
            return true;
        }

        // 깊이 밴드가 꽉 찼으면 레인 전체에서 한 번 더
        for (int attempt = 0; attempt < maxPlacementAttemptsPerZombie; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY));

            if (!IsInsideSpawnZone(candidate, minX, maxX, minY, maxY)) continue;
            if (!IsFarEnoughForBaseline(candidate, minDistSq)) continue;

            position = candidate;
            return true;
        }

        return false;
    }

    bool TryGetBaselineSpawnBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = maxX = minY = maxY = 0f;

        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board != null && board.TryGetLaneBounds(out minY, out maxY) && board.TryGetCellGridOrigin(out Vector3 origin))
        {
            float boardRightX = origin.x + (board.boardColumns - 1) * board.cellSize + board.cellSize * 0.5f;
            minX = boardRightX + board.cellSize * 0.15f;
            maxX = spawnX;
        }
        else
        {
            GetCrowdZoneBounds(out minX, out maxX, out minY, out maxY);
        }

        if (TryGetCameraWorldBounds(out float camMinX, out float camMaxX, out float camMinY, out float camMaxY))
        {
            maxX = Mathf.Min(maxX, camMaxX);
            minY = Mathf.Max(minY, camMinY);
            maxY = Mathf.Min(maxY, camMaxY);

            float laneWidth = maxX - minX;
            if (laneWidth > 1.2f)
            {
                float expandedMin = camMinX + (camMaxX - camMinX) * 0.38f;
                minX = Mathf.Min(minX, expandedMin);
            }
        }

        if (baselineSpawnRearShift > 0f)
        {
            float laneWidth = maxX - minX;
            float shift = laneWidth * Mathf.Clamp01(baselineSpawnRearShift);
            minX += shift;
            minX = Mathf.Min(minX, maxX - 0.45f);
        }

        minX = Mathf.Min(minX, maxX - 0.35f);
        return minX < maxX && minY < maxY;
    }

    static void GetBaselineDepthBand(int depthSlot, float minX, float maxX, out float bandMinX, out float bandMaxX)
    {
        float width = Mathf.Max(0.1f, maxX - minX);
        switch (depthSlot)
        {
            case 0:
                bandMinX = minX + width * 0.42f;
                bandMaxX = maxX;
                break;
            case 1:
                bandMinX = minX + width * 0.55f;
                bandMaxX = maxX;
                break;
            default:
                bandMinX = minX + width * 0.32f;
                bandMaxX = minX + width * 0.88f;
                break;
        }

        bandMinX = Mathf.Clamp(bandMinX, minX, maxX);
        bandMaxX = Mathf.Clamp(bandMaxX, minX, maxX);
        if (bandMaxX - bandMinX < 0.12f)
        {
            bandMinX = minX;
            bandMaxX = maxX;
        }
    }

    bool IsFarEnoughForBaseline(Vector2 candidate, float minDistSq)
    {
        for (int i = 0; i < _baselinePlacementBuffer.Count; i++)
        {
            if ((_baselinePlacementBuffer[i] - candidate).sqrMagnitude < minDistSq)
            {
                return false;
            }
        }

        for (int i = 0; i < _placementBuffer.Count; i++)
        {
            if ((_placementBuffer[i] - candidate).sqrMagnitude < minDistSq)
            {
                return false;
            }
        }

        return true;
    }

    static float GetScheduledSpawnTime(int index, int waveCount, float spawnWindow, string uid)
    {
        if (waveCount <= 0) return 0f;

        float slot = (index + 0.5f) / waveCount;
        if (IsBossSpawnUid(uid))
        {
            slot = Mathf.Clamp(0.08f + index * 0.04f, 0.08f, 0.35f);
        }

        return slot * spawnWindow;
    }

    /// <summary>라운드 전투 시간이 아직 남아 있으면 true. 일시정지(상점·레벨업 UI)는 종료로 보지 않습니다.</summary>
    static bool HasRoundCombatTimeRemaining()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.gameActive) return false;
        if (gm.IsAwaitingRound30BossKill) return true;
        return gm.RoundCombatRemaining > 0f;
    }

    IEnumerator WaitUntilRoundCombatElapsed(float targetElapsed)
    {
        while (true)
        {
            if (!HasRoundCombatTimeRemaining())
            {
                yield break;
            }

            if (GameManager.Instance.IsCombatPaused())
            {
                yield return null;
                continue;
            }

            if (GameManager.Instance.RoundCombatElapsed >= targetElapsed)
            {
                yield break;
            }

            yield return null;
        }
    }

    IEnumerator WaitWhileCombatPaused()
    {
        while (GameManager.Instance != null
            && GameManager.Instance.gameActive
            && GameManager.Instance.IsCombatPaused())
        {
            yield return null;
        }
    }

    IEnumerator WaitForSecondsRespectingPause(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!HasRoundCombatTimeRemaining())
            {
                yield break;
            }

            if (!GameManager.Instance.IsCombatPaused())
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
    }

    static bool IsBossSpawnUid(string uid)
    {
        return uid == "boss1" || uid == "ooze_boss" || uid == "necromancer_boss";
    }

    bool TryGetSpawnSeparation(string uid, out float separation)
    {
        separation = minSpawnSeparation;
        if (uid == "boss1" || uid == "ooze_boss" || uid == "necromancer_boss")
        {
            separation = minSpawnSeparation * 2.2f;
        }
        return true;
    }

    void EnsureBossEntriesFirst(List<string> wave)
    {
        if (wave == null || wave.Count <= 1) return;

        int bossCount = 0;
        for (int i = 0; i < wave.Count; i++)
        {
            if (IsBossSpawnUid(wave[i])) bossCount++;
        }
        if (bossCount == 0) return;

        var reordered = new List<string>(wave.Count);
        for (int i = 0; i < wave.Count; i++)
        {
            if (IsBossSpawnUid(wave[i])) reordered.Add(wave[i]);
        }
        for (int i = 0; i < wave.Count; i++)
        {
            if (!IsBossSpawnUid(wave[i])) reordered.Add(wave[i]);
        }
        wave.Clear();
        wave.AddRange(reordered);
    }

    bool TryResolveSpawnPosition(string uid, float separation, out Vector2 position)
    {
        if (TryFindNonOverlappingPosition(uid, separation, out position))
        {
            return true;
        }

        if (!IsBossSpawnUid(uid))
        {
            return false;
        }

        return TryGetGuaranteedBossSpawnPosition(out position);
    }

    bool TryGetGuaranteedBossSpawnPosition(out Vector2 position)
    {
        position = default;
        if (!TryGetSpawnZoneBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            return false;
        }

        GetSpawnRoleBand("boss1", minX, maxX, out float roleMinX, out float roleMaxX);
        float centerX = (roleMinX + roleMaxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;
        if (spawnYPositions != null && spawnYPositions.Length > 0)
        {
            centerY = spawnYPositions[spawnYPositions.Length / 2];
        }

        position = new Vector2(centerX, centerY);
        return true;
    }

    bool TryFindNonOverlappingPosition(string uid, float separation, out Vector2 position)
    {
        if (!TryGetSpawnZoneBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            position = default;
            return false;
        }

        GetSpawnRoleBand(uid, minX, maxX, out float roleMinX, out float roleMaxX);

        for (int attempt = 0; attempt < maxPlacementAttemptsPerZombie; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(roleMinX, roleMaxX),
                Random.Range(minY, maxY));

            if (!IsInsideSpawnZone(candidate, minX, maxX, minY, maxY)) continue;
            if (!IsFarEnough(candidate, separation)) continue;

            position = candidate;
            return true;
        }

        // 역할 밴드에서 자리를 못 찾으면 전체 구역에서 한 번 더 시도
        for (int attempt = 0; attempt < maxPlacementAttemptsPerZombie; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY));

            if (!IsInsideSpawnZone(candidate, minX, maxX, minY, maxY)) continue;
            if (!IsFarEnough(candidate, separation)) continue;

            position = candidate;
            return true;
        }

        position = default;
        return false;
    }

    void GetSpawnRoleBand(string uid, float minX, float maxX, out float roleMinX, out float roleMaxX)
    {
        float width = Mathf.Max(0.05f, maxX - minX);
        SpawnRole role = GetSpawnRole(uid);

        if (role == SpawnRole.Tanking)
        {
            roleMinX = minX;
            roleMaxX = minX + width * 0.36f;
        }
        else if (role == SpawnRole.Ranged)
        {
            roleMinX = minX + width * 0.64f;
            roleMaxX = maxX;
        }
        else
        {
            roleMinX = minX + width * 0.28f;
            roleMaxX = minX + width * 0.78f;
        }

        roleMinX = Mathf.Clamp(roleMinX, minX, maxX);
        roleMaxX = Mathf.Clamp(roleMaxX, minX, maxX);
        if (roleMaxX - roleMinX < 0.02f)
        {
            roleMinX = minX;
            roleMaxX = maxX;
        }
    }

    SpawnRole GetSpawnRole(string uid)
    {
        switch (uid)
        {
            case "mon_pig_3":
            case "mon_pig_7":
            case "mon_pig_10":
            case "boss1":
            case "ooze_boss":
            case "necromancer_boss":
                return SpawnRole.Tanking;
            case "mon_pig_2":
            case "mon_pig_6":
                return SpawnRole.Ranged;
            default:
                return SpawnRole.GeneralOrFast;
        }
    }

    static bool IsInsideSpawnZone(Vector2 p, float minX, float maxX, float minY, float maxY)
    {
        return p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY;
    }

    bool TryGetSpawnZoneBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        GetCrowdZoneBounds(out minX, out maxX, out minY, out maxY);

        if (!TryGetCameraWorldBounds(out float camMinX, out float camMaxX, out float camMinY, out float camMaxY))
        {
            return minX < maxX && minY < maxY;
        }

        minX = Mathf.Max(minX, camMinX);
        maxX = Mathf.Min(maxX, camMaxX);
        minY = Mathf.Max(minY, camMinY);
        maxY = Mathf.Min(maxY, camMaxY);

        if (minX >= maxX || minY >= maxY)
        {
            minX = camMinX;
            maxX = camMaxX;
            minY = camMinY;
            maxY = camMaxY;
        }

        return minX < maxX && minY < maxY;
    }

    bool TryGetCameraWorldBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            minX = maxX = minY = maxY = 0f;
            return false;
        }

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 center = cam.transform.position;
        float inset = Mathf.Max(0.05f, cameraBoundsInset);

        minX = center.x - width * 0.5f + inset;
        maxX = center.x + width * 0.5f - inset;
        minY = center.y - height * 0.5f + inset;
        maxY = center.y + height * 0.5f - inset;
        return minX < maxX && minY < maxY;
    }

    bool IsFarEnough(Vector2 candidate, float separation)
    {
        float minDistSq = separation * separation;
        for (int i = 0; i < _placementBuffer.Count; i++)
        {
            if ((_placementBuffer[i] - candidate).sqrMagnitude < minDistSq)
            {
                return false;
            }
        }
        return true;
    }

    void GetCrowdZoneBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        float rightX = spawnX;
        float leftX = spawnX - Mathf.Max(0.5f, crowdZoneWidth);

        minY = float.MaxValue;
        maxY = float.MinValue;
        for (int i = 0; i < spawnYPositions.Length; i++)
        {
            minY = Mathf.Min(minY, spawnYPositions[i]);
            maxY = Mathf.Max(maxY, spawnYPositions[i]);
        }

        float padY = 0.35f;
        minY -= padY;
        maxY += padY;
        minX = leftX;
        maxX = rightX;
    }

    int GetNearestRowIndex(float worldY)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < spawnYPositions.Length; i++)
        {
            float d = Mathf.Abs(spawnYPositions[i] - worldY);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    bool TrySpawnZombieAt(string uid, Vector2 position, int row, int round)
    {
        if (uid == "boss1")
        {
            return SpawnBoss1At(new Vector3(position.x, position.y, 0f), row) != null;
        }
        if (uid == "ooze_boss")
        {
            return SpawnOozeBossAt(new Vector3(position.x, position.y, 0f), row) != null;
        }
        if (uid == "necromancer_boss")
        {
            return SpawnNecromancerBossAt(new Vector3(position.x, position.y, 0f), row) != null;
        }

        ZombieData data = zombieDataLoader != null ? zombieDataLoader.GetZombieDataByUID(uid) : null;
        if (data == null)
        {
            Debug.LogWarning($"웨이브 스폰 실패 — 데이터 없음: {uid}");
            return false;
        }

        ZombieData scaledData = GetScaledZombieData(uid, data, round);
        GameObject go = CreateZombieFromData(scaledData, new Vector3(position.x, position.y, 0f), uid);
        if (go == null) return false;

        Zombie z = go.GetComponent<Zombie>();
        if (z != null) z.SetRow(row);
        return true;
    }

    bool IsBossUid(string uid)
    {
        return EnemyCombatStats.IsBossUid(uid);
    }

    ZombieData GetScaledZombieData(string uid, ZombieData baseData, int round)
    {
        if (IsBossUid(uid))
        {
            ZombieData boss = ScriptableObject.CreateInstance<ZombieData>();
            EnemyCombatStats.ApplyBossBaseStats(boss, uid);
            EnemyCombatStats.ScaleBossForRound(boss, uid, round);
            return boss;
        }

        if (baseData == null) return null;

        string key = $"{round}:{uid}";
        if (_scaledDataCache.TryGetValue(key, out ZombieData cached) && cached != null)
        {
            return cached;
        }

        float linearHpMul = round < mobScalingStartRound
            ? 1f
            : 1f + mobHealthGrowthPerRound * (round - mobScalingStartRound + 1);
        float hpMul = EnemyCombatStats.GlobalHealthScale
            * EnemyCombatStats.GetHealthMilestoneMultiplier(round)
            * linearHpMul;
        float dmgMul = round < mobScalingStartRound
            ? 1f
            : 1f + mobDamageGrowthPerRound * (round - mobScalingStartRound + 1);
        float speedMul = EnemyCombatStats.GetMobSpeedMultiplier(round);

        ZombieData scaled = ScriptableObject.CreateInstance<ZombieData>();
        scaled.zombieName = baseData.zombieName;
        scaled.resourceName = baseData.resourceName;
        scaled.moveSpeed = round < mobScalingStartRound
            ? baseData.moveSpeed
            : Mathf.Max(0.1f, baseData.moveSpeed * speedMul);
        if (EnemyCombatStats.IsBreakthroughMob(uid))
        {
            scaled.moveSpeed *= EnemyCombatStats.BreakthroughMoveSpeedMultiplier;
        }
        scaled.health = Mathf.Max(1, Mathf.RoundToInt(baseData.health * hpMul));
        scaled.maxHealth = Mathf.Max(scaled.health, Mathf.RoundToInt(baseData.maxHealth * hpMul));
        scaled.damage = round < mobScalingStartRound
            ? baseData.damage
            : Mathf.Max(0, Mathf.RoundToInt(baseData.damage * dmgMul));
        scaled.attackRange = baseData.attackRange;
        scaled.attackCooldown = baseData.attackCooldown;

        if (uid == "mon_pig_1")
        {
            scaled.health = Mathf.Max(1, Mathf.RoundToInt(scaled.health * EnemyCombatStats.Mob1HealthMultiplier));
            scaled.maxHealth = Mathf.Max(scaled.health, Mathf.RoundToInt(scaled.maxHealth * EnemyCombatStats.Mob1HealthMultiplier));
        }

        _scaledDataCache[key] = scaled;
        return scaled;
    }
    
    GameObject SpawnBoss1At(Vector3 spawnPosition, int row)
    {
        int round = GameManager.Instance != null ? GameManager.Instance.currentRound : 10;
        ZombieData bossData = GetScaledZombieData("boss1", null, round);

        GameObject bossObj = CreateZombieFromData(bossData, spawnPosition, "boss1");
        if (bossObj != null)
        {
            bossObj.transform.localScale = Vector3.one * 6f;
            Zombie z = bossObj.GetComponent<Zombie>();
            if (z != null) z.SetRow(row);
        }
        return bossObj;
    }

    GameObject SpawnOozeBossAt(Vector3 spawnPosition, int row)
    {
        int round = GameManager.Instance != null ? GameManager.Instance.currentRound : 20;
        ZombieData oozeData = GetScaledZombieData("ooze_boss", null, round);

        GameObject go = CreateZombieFromData(oozeData, spawnPosition, "ooze_boss");
        if (go != null)
        {
            Zombie z = go.GetComponent<Zombie>();
            if (z != null) z.SetRow(row);
        }
        return go;
    }

    GameObject SpawnNecromancerBossAt(Vector3 spawnPosition, int row)
    {
        int round = GameManager.Instance != null ? GameManager.Instance.currentRound : 30;
        ZombieData necroData = GetScaledZombieData("necromancer_boss", null, round);

        GameObject go = CreateZombieFromData(necroData, spawnPosition, "necromancer_boss");
        if (go != null)
        {
            Zombie z = go.GetComponent<Zombie>();
            if (z != null) z.SetRow(row);
        }
        return go;
    }

    
    /// <summary>월드 위치에 UID 좀비 1마리를 생성합니다 (보스 소환 등).</summary>
    public GameObject SpawnZombieUidAtPosition(string uid, Vector3 worldPosition, int row)
    {
        if (zombieDataLoader == null) return null;
        ZombieData data = zombieDataLoader.GetZombieDataByUID(uid);
        if (data == null) return null;
        GameObject go = CreateZombieFromData(data, worldPosition, uid);
        if (go != null)
        {
            Zombie z = go.GetComponent<Zombie>();
            if (z != null) z.SetRow(row);
        }
        return go;
    }

    /// <summary>
    /// 좀비 데이터로부터 좀비를 생성합니다
    /// </summary>
    GameObject CreateZombieFromData(ZombieData data, Vector3 position, string uid = null)
    {
        GameObject zombieObj = new GameObject(data.zombieName);
        zombieObj.transform.position = position;
        
        // 비주얼 루트 생성 (애니메이션 흔들림 방지용)
        GameObject visualObj = new GameObject("ZombieVisual");
        visualObj.transform.SetParent(zombieObj.transform, false);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;
        visualObj.transform.localScale = Vector3.one;

        // SpriteRenderer 추가
        SpriteRenderer spriteRenderer = visualObj.AddComponent<SpriteRenderer>();

        Sprite zombieSprite = LoadZombieSprite(uid);
        if (zombieSprite == null)
        {
            // 안드로이드는 Resources 리소스 이름을 대소문자까지 정확히 구분해서 찾는다
            // (에디터/윈도우는 대소문자를 구분하지 않아 겉으로는 문제없이 보였을 수 있다).
            // 특정 적 전용 스프라이트를 못 찾은 경우, 완전히 안 보이는 것보다는
            // mon_pig_1 스프라이트로라도 대체해서 최소한 적이 보이도록 한다.
            Debug.LogWarning($"좀비 스프라이트를 찾지 못해 기본(mon_pig_1) 스프라이트로 대체합니다: uid={uid}, name={data.zombieName}");
            zombieSprite = LoadPigSprite();
        }
        if (zombieSprite == null)
        {
            Debug.LogWarning($"대체 스프라이트도 찾지 못해 스폰을 건너뜁니다: uid={uid}, name={data.zombieName}");
            Destroy(zombieObj);
            return null;
        }

        spriteRenderer.sprite = zombieSprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 1;
        
        ApplyZombieScale(visualObj.transform, spriteRenderer.sprite, 0.675f);
        
        // Collider 추가
        BoxCollider2D collider = zombieObj.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        
        // Zombie 컴포넌트 추가
        Zombie zombieScript = zombieObj.AddComponent<Zombie>();
        zombieScript.zombieData = data; // 데이터 할당
        zombieScript.zombieUID = uid;
        
        if (uid == "mon_pig_1")
        {
            zombieObj.transform.localScale = Vector3.one * EnemyCombatStats.Mob1ScaleMultiplier;
        }
        else if (uid == "mon_pig_7")
        {
            // 이전 1f 기준 약 2/3 크기
            zombieObj.transform.localScale = Vector3.one * (2f / 3f);
        }
        else if (uid == "mon_pig_8")
        {
            // 기존 (2/3) 대비 1/2
            zombieObj.transform.localScale = Vector3.one * ((2f / 3f) * 0.5f);
        }
        else if (uid == "mon_pig_9")
        {
            zombieObj.transform.localScale = Vector3.one * (1f / 3f);
        }
        else if (uid == "mon_pig_4" || uid == "mon_pig_5" || uid == "mon_pig_6")
        {
            zombieObj.transform.localScale = Vector3.one * 2f;
        }
        else if (uid == "mon_pig_10")
        {
            zombieObj.transform.localScale = Vector3.one * 1.35f;
        }
        else if (uid == "necromancer_boss")
        {
            zombieObj.transform.localScale = Vector3.one * (3.5f * (1f / 5f));
        }
        
        return zombieObj;
    }

    Sprite LoadZombieSprite(string uid)
    {
        if (uid == "mon_pig_1")
        {
            return LoadPigSprite();
        }
        if (uid == "mon_pig_2")
        {
            return LoadEnemy2Sprite();
        }
        if (uid == "mon_pig_3")
        {
            return LoadEnemy3Sprite();
        }
        if (uid == "boss1")
        {
            return LoadFirstSheetSprite("Demon_Boss_walk-export");
        }
        if (uid == "mon_pig_4")
        {
            return LoadFirstSheetSprite("enemy_4_Walk-export");
        }
        if (uid == "mon_pig_5")
        {
            return LoadFirstSheetSprite("enemy_5_Walk-export");
        }
        if (uid == "mon_pig_6")
        {
            return LoadFirstSheetSprite("enemy_6_walk-export");
        }
        if (uid == "mon_pig_7")
        {
            return LoadFirstSheetSprite("enemy_7_Walk-export");
        }
        if (uid == "mon_pig_8")
        {
            return LoadFirstSheetSprite("enemy_8_walk-export");
        }
        if (uid == "mon_pig_9")
        {
            return LoadFirstSheetSprite("enemy_9_walk-export");
        }
        if (uid == "mon_pig_10")
        {
            return LoadFirstSheetSprite("enemy_10_walk-export");
        }
        if (uid == "necromancer_boss")
        {
            return LoadFirstSheetSprite("Necromancer_walk-export");
        }
        if (uid == "ooze_boss")
        {
            return LoadFirstSheetSprite("Ooze_boss_walk-export");
        }

        return LoadPigSprite();
    }

    static Sprite LoadFirstSheetSprite(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return null;

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
        if (sprites == null || sprites.Length == 0) return null;

        System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites[0];
    }

    Sprite LoadPigSprite()
    {
        if (cachedPigSprite != null) return cachedPigSprite;
        cachedPigSprite = LoadFirstSheetSprite("enemy_1_walk");
        return cachedPigSprite;
    }

    Sprite LoadEnemy2Sprite()
    {
        if (cachedEnemy2Sprite != null) return cachedEnemy2Sprite;
        cachedEnemy2Sprite = LoadFirstSheetSprite("enemy_2_Walk-export");
        return cachedEnemy2Sprite;
    }

    Sprite LoadEnemy3Sprite()
    {
        if (cachedEnemy3Sprite != null) return cachedEnemy3Sprite;
        cachedEnemy3Sprite = LoadFirstSheetSprite("enemy_3_Walk");
        if (cachedEnemy3Sprite == null)
        {
            cachedEnemy3Sprite = LoadFirstSheetSprite("enemy_3_Walkt");
        }
        return cachedEnemy3Sprite;
    }

    void ApplyZombieScale(Transform target, Sprite sprite, float targetWorldSize)
    {
        if (target == null)
        {
            return;
        }

        const float enemyScaleMultiplier = 0.5f;
        if (sprite != null && (sprite.name.StartsWith("enemy_1_") || sprite.name.StartsWith("enemy_2_") || sprite.name.StartsWith("enemy_3_") || sprite.name.StartsWith("enemy_7_") || sprite.name.StartsWith("enemy_8_") || sprite.name.StartsWith("enemy_9_") || sprite.name.StartsWith("enemy_10_") || sprite.name.StartsWith("Necromancer_") || sprite.name.StartsWith("Ooze_boss")))
        {
            target.localScale = Vector3.one * enemyScaleMultiplier;
            return;
        }

        float size = 1f;
        if (sprite != null)
        {
            Vector2 spriteSize = sprite.bounds.size;
            size = Mathf.Max(spriteSize.x, spriteSize.y);
        }

        if (size <= 0.0001f)
        {
            size = 1f;
        }

        float scale = (targetWorldSize / size) * enemyScaleMultiplier;
        target.localScale = new Vector3(scale, scale, 1f);
    }
    
    /// <summary>
    /// 특정 행의 좀비 수를 반환합니다
    /// </summary>
    int GetZombieCountInRow(int row)
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        int count = 0;
        
        foreach (Zombie zombie in zombies)
        {
            if (zombie != null && zombie.currentRow == row && !zombie.IsExcludedFromCombat)
            {
                count++;
            }
        }
        
        return count;
    }
}

