using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// CSV 파일에서 라운드별 스폰 데이터를 로드하는 클래스
/// </summary>
public class RoundSpawnLoader : MonoBehaviour
{
    [Header("CSV Settings")]
    [Tooltip("라운드 스폰 CSV 파일 경로")]
    public string csvFilePath = "rounds.csv";
    
    [Tooltip("CSV 파일을 사용할지 여부")]
    public bool useCSV = true;
    
    private Dictionary<int, RoundSpawnData> roundSpawnDataDict = new Dictionary<int, RoundSpawnData>();
    
    /// <summary>
    /// CSV 파일에서 라운드 스폰 데이터를 로드합니다
    /// </summary>
    public void LoadRoundSpawnData()
    {
        roundSpawnDataDict.Clear();
        
        if (!useCSV)
        {
            Debug.Log("라운드 스폰 CSV 사용이 비활성화되어 있습니다.");
            return;
        }
        
        // 1순위: 실제 파일 경로 (에디터·PC 스탠드얼론)
        List<Dictionary<string, string>> csvData = null;

        string fullPath = GetCSVFilePath();
        if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
        {
            csvData = CSVReader.ReadCSV(fullPath);
        }

        // 2순위: Resources 폴더의 TextAsset
        // 안드로이드/iOS 빌드에서는 StreamingAssets가 APK 안에 압축되어 있어
        // File.Exists / File.ReadAllText로는 읽을 수 없으므로 반드시 이 경로가 필요하다.
        if (csvData == null || csvData.Count == 0)
        {
            string resourceName = Path.GetFileNameWithoutExtension(csvFilePath);
            csvData = CSVReader.ReadCSVFromResources(resourceName);

            if (csvData != null && csvData.Count > 0)
            {
                Debug.Log($"라운드 스폰 CSV를 Resources에서 로드했습니다: Resources/{resourceName}");
            }
        }

        if (csvData == null || csvData.Count == 0)
        {
            Debug.LogWarning(
                "라운드 스폰 CSV를 어떤 경로에서도 로드하지 못했습니다. " +
                $"(파일 경로: {(string.IsNullOrEmpty(fullPath) ? "없음" : fullPath)}, " +
                $"Resources: {Path.GetFileNameWithoutExtension(csvFilePath)})");
            return;
        }

        // CSV 데이터를 RoundSpawnData로 변환
        foreach (Dictionary<string, string> row in csvData)
        {
            RoundSpawnData roundData = CreateRoundSpawnDataFromRow(row);
            if (roundData != null)
            {
                roundSpawnDataDict[roundData.round] = roundData;
                Debug.Log($"라운드 {roundData.round} 로드: {roundData.spawns.Count}개의 스폰 설정");
            }
        }
        
        Debug.Log($"라운드 스폰 CSV에서 {roundSpawnDataDict.Count}개의 라운드 데이터를 로드했습니다.");
    }
    
    /// <summary>
    /// CSV 행 데이터로부터 RoundSpawnData를 생성합니다
    /// </summary>
    RoundSpawnData CreateRoundSpawnDataFromRow(Dictionary<string, string> row)
    {
        // round 컬럼 찾기
        string roundKey = GetKeyIgnoreCase(row, "round") ?? GetKeyIgnoreCase(row, "Round");
        if (roundKey == null || !row.ContainsKey(roundKey))
        {
            Debug.LogWarning("round 컬럼을 찾을 수 없습니다.");
            return null;
        }
        
        if (!int.TryParse(row[roundKey], out int round))
        {
            Debug.LogWarning($"round 값을 파싱할 수 없습니다: {row[roundKey]}");
            return null;
        }
        
        RoundSpawnData roundData = new RoundSpawnData(round);
        
        // Spawn_1, Spawn_2, ... 최대 10개까지 지원
        for (int i = 1; i <= 10; i++)
        {
            string spawnUIDKey = $"Spawn_{i}";
            string spawnTimeKey = $"Spawn_{i}_time";
            string spawnMinKey = $"Spawn_{i}_min";
            string spawnMaxKey = $"Spawn_{i}_max";
            
            // UID가 있는지 확인
            string uidKey = GetKeyIgnoreCase(row, spawnUIDKey);
            if (uidKey == null || !row.ContainsKey(uidKey) || string.IsNullOrWhiteSpace(row[uidKey]))
            {
                continue; // 이 Spawn은 없음
            }
            
            string zombieUID = row[uidKey].Trim();
            
            // time, min, max 파싱
            float spawnInterval = 2f; // 기본값
            int minCount = 1; // 기본값
            int maxCount = 1; // 기본값
            
            string timeKey = GetKeyIgnoreCase(row, spawnTimeKey);
            if (timeKey != null && row.ContainsKey(timeKey) && !string.IsNullOrWhiteSpace(row[timeKey]))
            {
                if (float.TryParse(row[timeKey], out float interval))
                {
                    spawnInterval = interval;
                }
            }
            
            string minKey = GetKeyIgnoreCase(row, spawnMinKey);
            if (minKey != null && row.ContainsKey(minKey) && !string.IsNullOrWhiteSpace(row[minKey]))
            {
                if (int.TryParse(row[minKey], out int min))
                {
                    minCount = min;
                }
            }
            
            string maxKey = GetKeyIgnoreCase(row, spawnMaxKey);
            if (maxKey != null && row.ContainsKey(maxKey) && !string.IsNullOrWhiteSpace(row[maxKey]))
            {
                if (int.TryParse(row[maxKey], out int max))
                {
                    maxCount = max;
                }
            }
            
            // SpawnInfo 생성
            SpawnInfo spawnInfo = new SpawnInfo(zombieUID, spawnInterval, minCount, maxCount);
            roundData.spawns.Add(spawnInfo);
        }
        
        return roundData;
    }
    
    /// <summary>
    /// 대소문자를 무시하고 키를 찾습니다
    /// </summary>
    string GetKeyIgnoreCase(Dictionary<string, string> dict, string key)
    {
        foreach (string k in dict.Keys)
        {
            if (string.Equals(k, key, System.StringComparison.OrdinalIgnoreCase))
            {
                return k;
            }
        }
        return null;
    }
    
    /// <summary>
    /// CSV 파일의 전체 경로를 반환합니다
    /// </summary>
    string GetCSVFilePath()
    {
        if (Path.IsPathRooted(csvFilePath))
        {
            return csvFilePath;
        }
        
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string fullPath = Path.Combine(projectRoot, csvFilePath);
        
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        
        fullPath = Path.Combine(Application.dataPath, csvFilePath);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        
        fullPath = Path.Combine(Application.streamingAssetsPath, csvFilePath);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        
        return null;
    }
    
    /// <summary>
    /// 특정 라운드의 스폰 데이터를 반환합니다
    /// </summary>
    public RoundSpawnData GetRoundSpawnData(int round)
    {
        if (roundSpawnDataDict.ContainsKey(round))
        {
            return roundSpawnDataDict[round];
        }
        
        // 라운드가 없으면 가장 가까운 낮은 라운드 데이터 사용
        for (int i = round; i >= 1; i--)
        {
            if (roundSpawnDataDict.ContainsKey(i))
            {
                Debug.Log($"라운드 {round} 데이터가 없어서 라운드 {i} 데이터를 사용합니다.");
                return roundSpawnDataDict[i];
            }
        }
        
        Debug.LogWarning($"라운드 {round}의 스폰 데이터를 찾을 수 없습니다. (로드된 라운드: {roundSpawnDataDict.Count}개)");
        return null;
    }
}

