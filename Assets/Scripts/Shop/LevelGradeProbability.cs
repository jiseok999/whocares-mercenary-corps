using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨별 등급 확률 데이터를 관리하는 클래스
/// </summary>
public static class LevelGradeProbability
{
    public const int MaxBalancedLevel = 30;

    /// <summary>
    /// 레벨별 등급 확률 테이블 (등급별 확률 %, 각 레벨 합계 100)
    /// 5등급(가장 흔함) -> 1등급(가장 희귀), 1~30레벨에 걸쳐 점진 성장
    /// 등급 첫 등장: 4등급 Lv3 / 3등급 Lv5 / 2등급 Lv10 / 1등급 Lv15
    /// </summary>
    private static Dictionary<int, Dictionary<int, float>> probabilityTable = new Dictionary<int, Dictionary<int, float>>
    {
        { 1,  new Dictionary<int, float> { { 5, 100f } } },
        { 2,  new Dictionary<int, float> { { 5, 100f } } },
        { 3,  new Dictionary<int, float> { { 5, 90f }, { 4, 10f } } },
        { 4,  new Dictionary<int, float> { { 5, 82f }, { 4, 18f } } },
        { 5,  new Dictionary<int, float> { { 5, 74f }, { 4, 25f }, { 3, 1f } } },
        { 6,  new Dictionary<int, float> { { 5, 67f }, { 4, 30f }, { 3, 3f } } },
        { 7,  new Dictionary<int, float> { { 5, 60f }, { 4, 34f }, { 3, 6f } } },
        { 8,  new Dictionary<int, float> { { 5, 54f }, { 4, 36f }, { 3, 10f } } },
        { 9,  new Dictionary<int, float> { { 5, 48f }, { 4, 37f }, { 3, 15f } } },
        { 10, new Dictionary<int, float> { { 5, 43f }, { 4, 37f }, { 3, 19f }, { 2, 1f } } },
        { 11, new Dictionary<int, float> { { 5, 38f }, { 4, 36f }, { 3, 23f }, { 2, 3f } } },
        { 12, new Dictionary<int, float> { { 5, 34f }, { 4, 35f }, { 3, 26f }, { 2, 5f } } },
        { 13, new Dictionary<int, float> { { 5, 30f }, { 4, 33f }, { 3, 29f }, { 2, 8f } } },
        { 14, new Dictionary<int, float> { { 5, 27f }, { 4, 31f }, { 3, 31f }, { 2, 11f } } },
        { 15, new Dictionary<int, float> { { 5, 24f }, { 4, 29f }, { 3, 32f }, { 2, 14f }, { 1, 1f } } },
        { 16, new Dictionary<int, float> { { 5, 21f }, { 4, 27f }, { 3, 33f }, { 2, 17f }, { 1, 2f } } },
        { 17, new Dictionary<int, float> { { 5, 19f }, { 4, 25f }, { 3, 33f }, { 2, 20f }, { 1, 3f } } },
        { 18, new Dictionary<int, float> { { 5, 17f }, { 4, 23f }, { 3, 33f }, { 2, 22f }, { 1, 5f } } },
        { 19, new Dictionary<int, float> { { 5, 15f }, { 4, 21f }, { 3, 32f }, { 2, 25f }, { 1, 7f } } },
        { 20, new Dictionary<int, float> { { 5, 13f }, { 4, 19f }, { 3, 31f }, { 2, 28f }, { 1, 9f } } },
        { 21, new Dictionary<int, float> { { 5, 12f }, { 4, 17f }, { 3, 30f }, { 2, 30f }, { 1, 11f } } },
        { 22, new Dictionary<int, float> { { 5, 11f }, { 4, 16f }, { 3, 28f }, { 2, 32f }, { 1, 13f } } },
        { 23, new Dictionary<int, float> { { 5, 10f }, { 4, 15f }, { 3, 27f }, { 2, 33f }, { 1, 15f } } },
        { 24, new Dictionary<int, float> { { 5, 9f },  { 4, 14f }, { 3, 25f }, { 2, 34f }, { 1, 18f } } },
        { 25, new Dictionary<int, float> { { 5, 8f },  { 4, 13f }, { 3, 24f }, { 2, 35f }, { 1, 20f } } },
        { 26, new Dictionary<int, float> { { 5, 7f },  { 4, 12f }, { 3, 23f }, { 2, 35f }, { 1, 23f } } },
        { 27, new Dictionary<int, float> { { 5, 7f },  { 4, 11f }, { 3, 21f }, { 2, 35f }, { 1, 26f } } },
        { 28, new Dictionary<int, float> { { 5, 6f },  { 4, 10f }, { 3, 20f }, { 2, 35f }, { 1, 29f } } },
        { 29, new Dictionary<int, float> { { 5, 6f },  { 4, 9f },  { 3, 19f }, { 2, 34f }, { 1, 32f } } },
        { 30, new Dictionary<int, float> { { 5, 5f },  { 4, 8f },  { 3, 18f }, { 2, 34f }, { 1, 35f } } }
    };
    
    /// <summary>
    /// 레벨에 따른 등급 확률을 반환합니다 (30레벨 이상은 30레벨 확률 사용)
    /// </summary>
    public static Dictionary<int, float> GetProbabilitiesForLevel(int level)
    {
        int lookupLevel = Mathf.Clamp(level, 1, MaxBalancedLevel);
        
        if (probabilityTable.ContainsKey(lookupLevel))
        {
            return probabilityTable[lookupLevel];
        }
        
        // 기본값 (레벨 1과 동일)
        return probabilityTable[1];
    }
    
    /// <summary>
    /// 레벨에 따라 등급을 랜덤하게 선택합니다
    /// </summary>
    public static int SelectGradeByLevel(int level)
    {
        Dictionary<int, float> probabilities = GetProbabilitiesForLevel(level);
        
        // 확률 합계 계산
        float totalProbability = 0f;
        foreach (float prob in probabilities.Values)
        {
            totalProbability += prob;
        }
        
        // 랜덤 값 생성 (0 ~ totalProbability)
        float randomValue = Random.Range(0f, totalProbability);
        
        // 확률에 따라 등급 선택
        float cumulative = 0f;
        foreach (KeyValuePair<int, float> kvp in probabilities)
        {
            cumulative += kvp.Value;
            if (randomValue <= cumulative)
            {
                return kvp.Key;
            }
        }
        
        // 기본값 (1등급)
        return 1;
    }
}

