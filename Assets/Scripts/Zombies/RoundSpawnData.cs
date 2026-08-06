using System.Collections.Generic;

/// <summary>
/// 라운드별 스폰 정보를 담는 클래스
/// </summary>
[System.Serializable]
public class RoundSpawnData
{
    public int round;
    public List<SpawnInfo> spawns = new List<SpawnInfo>();
    
    public RoundSpawnData(int round)
    {
        this.round = round;
    }
}

/// <summary>
/// 개별 스폰 정보
/// </summary>
[System.Serializable]
public class SpawnInfo
{
    public string zombieUID; // 좀비 UID (mon_pig_1, mon_pig_2 등)
    public float spawnInterval; // 스폰 간격 (초)
    public int minCount; // 최소 스폰 개수
    public int maxCount; // 최대 스폰 개수
    
    // 내부 타이머
    public float lastSpawnTime = 0f;
    
    public SpawnInfo(string uid, float interval, int min, int max)
    {
        zombieUID = uid;
        spawnInterval = interval;
        minCount = min;
        maxCount = max;
    }
}

