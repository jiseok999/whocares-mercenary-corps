using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// CSV 파일에서 좀비 데이터를 로드하는 클래스
/// </summary>
public class ZombieDataLoader : MonoBehaviour
{
    [Header("CSV Settings")]
    [Tooltip("CSV 파일 경로 (Assets 폴더 기준 또는 절대 경로)")]
    public string csvFilePath = "zombies.csv";
    
    [Tooltip("CSV 파일을 사용할지 여부")]
    public bool useCSV = true;
    
    [Header("Fallback Data")]
    [Tooltip("CSV를 사용하지 않거나 로드 실패 시 사용할 기본 데이터")]
    public ZombieData[] fallbackZombieDataList;
    
    private List<ZombieData> loadedZombieDataList = new List<ZombieData>();
    private Dictionary<string, ZombieData> uidToZombieDataDict = new Dictionary<string, ZombieData>();
    
    /// <summary>
    /// CSV 파일에서 좀비 데이터를 로드합니다
    /// </summary>
    public List<ZombieData> LoadZombieDataFromCSV()
    {
        loadedZombieDataList.Clear();
        uidToZombieDataDict.Clear();
        
        if (!useCSV)
        {
            Debug.Log("CSV 사용이 비활성화되어 있습니다. Fallback 데이터를 사용합니다.");
            if (fallbackZombieDataList != null)
            {
                loadedZombieDataList.AddRange(fallbackZombieDataList);
            }
            return loadedZombieDataList;
        }
        
        // 1순위: 실제 파일 경로 (에디터·PC 스탠드얼론에서 동작.
        //         프로젝트 루트의 zombies.csv를 수정하면 즉시 반영되는 기존 워크플로 유지)
        List<Dictionary<string, string>> csvData = null;

        string fullPath = GetCSVFilePath();
        if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
        {
            csvData = CSVReader.ReadCSV(fullPath);
        }

        // 2순위: Resources 폴더의 TextAsset
        //
        // 안드로이드/iOS 빌드에서는 StreamingAssets가 APK 내부에 압축되어 들어가기 때문에
        // Application.streamingAssetsPath가 "jar:file:///....apk!/assets" 형태의 URL이 되고,
        // File.Exists()는 무조건 false를 반환하며 File.ReadAllText()도 아예 불가능하다.
        // 즉 위의 파일 경로 방식은 모바일에서 100% 실패한다.
        // → 그 경우 Assets/Resources/zombies.csv 를 TextAsset으로 읽어서 동일하게 파싱한다.
        if (csvData == null || csvData.Count == 0)
        {
            string resourceName = Path.GetFileNameWithoutExtension(csvFilePath);
            csvData = CSVReader.ReadCSVFromResources(resourceName);

            if (csvData != null && csvData.Count > 0)
            {
                Debug.Log($"좀비 CSV를 Resources에서 로드했습니다: Resources/{resourceName}");
            }
        }

        if (csvData == null || csvData.Count == 0)
        {
            Debug.LogError(
                "좀비 CSV를 어떤 경로에서도 로드하지 못했습니다 — 적이 한 마리도 스폰되지 않습니다! " +
                $"(파일 경로: {(string.IsNullOrEmpty(fullPath) ? "없음" : fullPath)}, " +
                $"Resources: {Path.GetFileNameWithoutExtension(csvFilePath)}) " +
                "Assets/Resources 폴더에 zombies.csv 가 있는지 확인하세요.");
            if (fallbackZombieDataList != null)
            {
                loadedZombieDataList.AddRange(fallbackZombieDataList);
            }
            return loadedZombieDataList;
        }

        // CSV 데이터를 ZombieData로 변환
        foreach (Dictionary<string, string> row in csvData)
        {
            // UID 추출
            string uid = null;
            string uidKey = GetKeyIgnoreCase(row, "uid") ?? GetKeyIgnoreCase(row, "UID");
            if (uidKey != null && row.ContainsKey(uidKey))
            {
                uid = row[uidKey].Trim();
            }
            
            ZombieData zombieData = CreateZombieDataFromRow(row);
            if (zombieData != null)
            {
                loadedZombieDataList.Add(zombieData);
                
                // UID가 있으면 딕셔너리에 저장
                if (!string.IsNullOrEmpty(uid))
                {
                    uidToZombieDataDict[uid] = zombieData;
                }
            }
        }
        
        Debug.Log($"CSV에서 {loadedZombieDataList.Count}개의 좀비 데이터를 로드했습니다.");
        
        return loadedZombieDataList;
    }
    
    /// <summary>
    /// CSV 행 데이터로부터 ZombieData를 생성합니다
    /// </summary>
    ZombieData CreateZombieDataFromRow(Dictionary<string, string> row)
    {
        // ScriptableObject는 런타임에 직접 생성할 수 없으므로
        // 일반 클래스로 데이터를 저장하거나, 런타임용 데이터 클래스를 사용합니다
        // 여기서는 런타임용 ZombieData를 생성합니다
        
        ZombieData zombieData = ScriptableObject.CreateInstance<ZombieData>();
        
        // 기본값 설정
        zombieData.damage = 10;
        zombieData.attackRange = 0.5f;
        zombieData.attackCooldown = 1.0f;
        
        // CSV 컬럼명에 맞춰 데이터 파싱 (대소문자 무시, 한글 컬럼명도 지원)
        
        // 좀비 이름 (name, 좀비 이름)
        string nameKey = GetKeyIgnoreCase(row, "name") ?? GetKeyIgnoreCase(row, "좀비 이름");
        if (nameKey != null && row.ContainsKey(nameKey))
        {
            zombieData.zombieName = row[nameKey];
        }
        
        // UID는 저장하지 않지만 로그용으로 사용 가능 (선택사항)
        string uidKey = GetKeyIgnoreCase(row, "uid") ?? GetKeyIgnoreCase(row, "UID");
        if (uidKey != null && row.ContainsKey(uidKey))
        {
            // UID는 현재 ZombieData에 필드가 없으므로 로그만 출력
            Debug.Log($"좀비 UID: {row[uidKey]}");
        }
        
        // 리소스명 (resourceName, model)
        string modelKey = GetKeyIgnoreCase(row, "resourceName") ?? GetKeyIgnoreCase(row, "model");
        if (modelKey != null && row.ContainsKey(modelKey))
        {
            zombieData.resourceName = row[modelKey];
        }
        
        // 이동 속도 (moveSpeed, Speed)
        string speedKey = GetKeyIgnoreCase(row, "moveSpeed") ?? GetKeyIgnoreCase(row, "Speed") ?? GetKeyIgnoreCase(row, "speed");
        if (speedKey != null && row.ContainsKey(speedKey))
        {
            if (float.TryParse(row[speedKey], out float moveSpeed))
            {
                zombieData.moveSpeed = moveSpeed;
            }
        }
        
        // 체력 (health, Hp, HP)
        string hpKey = GetKeyIgnoreCase(row, "health") ?? GetKeyIgnoreCase(row, "Hp") ?? GetKeyIgnoreCase(row, "HP");
        if (hpKey != null && row.ContainsKey(hpKey))
        {
            if (int.TryParse(row[hpKey], out int health))
            {
                zombieData.health = health;
            }
        }
        
        // 최대 체력 (maxHealth, MaxHealth)
        string maxHpKey = GetKeyIgnoreCase(row, "maxHealth") ?? GetKeyIgnoreCase(row, "MaxHealth");
        if (maxHpKey != null && row.ContainsKey(maxHpKey))
        {
            if (int.TryParse(row[maxHpKey], out int maxHealth))
            {
                zombieData.maxHealth = maxHealth;
            }
        }
        else
        {
            // maxHealth가 없으면 health 값을 사용
            zombieData.maxHealth = zombieData.health;
        }
        
        // 공격력 (damage, Damage)
        string damageKey = GetKeyIgnoreCase(row, "damage") ?? GetKeyIgnoreCase(row, "Damage");
        if (damageKey != null && row.ContainsKey(damageKey))
        {
            if (int.TryParse(row[damageKey], out int damage))
            {
                zombieData.damage = damage;
            }
        }
        
        // 공격 범위 (attackRange, AttackRange)
        string rangeKey = GetKeyIgnoreCase(row, "attackRange") ?? GetKeyIgnoreCase(row, "AttackRange");
        if (rangeKey != null && row.ContainsKey(rangeKey))
        {
            if (float.TryParse(row[rangeKey], out float attackRange))
            {
                zombieData.attackRange = attackRange;
            }
        }
        
        // 공격 쿨다운 (attackCooldown, AttackCooldown)
        string cooldownKey = GetKeyIgnoreCase(row, "attackCooldown") ?? GetKeyIgnoreCase(row, "AttackCooldown");
        if (cooldownKey != null && row.ContainsKey(cooldownKey))
        {
            if (float.TryParse(row[cooldownKey], out float attackCooldown))
            {
                zombieData.attackCooldown = attackCooldown;
            }
        }
        
        return zombieData;
    }
    
    /// <summary>
    /// 키가 존재하는지 확인합니다 (대소문자 무시)
    /// </summary>
    bool HasKey(Dictionary<string, string> dict, string key)
    {
        foreach (string k in dict.Keys)
        {
            if (string.Equals(k, key, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
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
        // 절대 경로인 경우
        if (Path.IsPathRooted(csvFilePath))
        {
            return csvFilePath;
        }
        
        // 상대 경로인 경우
        // Unity에서는 Assets 폴더나 프로젝트 루트를 기준으로 찾습니다
        // 먼저 프로젝트 루트에서 찾기
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string fullPath = Path.Combine(projectRoot, csvFilePath);
        
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        
        // Assets 폴더에서 찾기
        fullPath = Path.Combine(Application.dataPath, csvFilePath);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        
        // StreamingAssets 폴더에서 찾기
        fullPath = Path.Combine(Application.streamingAssetsPath, csvFilePath);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        
        return null;
    }
    
    /// <summary>
    /// 로드된 좀비 데이터 리스트를 반환합니다
    /// </summary>
    public ZombieData[] GetLoadedZombieDataArray()
    {
        return loadedZombieDataList.ToArray();
    }
    
    /// <summary>
    /// UID로 좀비 데이터를 찾습니다
    /// </summary>
    public ZombieData GetZombieDataByUID(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            return null;
        }
        
        // UID 딕셔너리에서 찾기
        if (uidToZombieDataDict.ContainsKey(uid))
        {
            return uidToZombieDataDict[uid];
        }
        
        // 딕셔너리에 없으면 zombieName으로 매칭 시도 (레거시 지원)
        foreach (ZombieData data in loadedZombieDataList)
        {
            if (data.zombieName == uid)
            {
                return data;
            }
        }
        
        return null;
    }
}

