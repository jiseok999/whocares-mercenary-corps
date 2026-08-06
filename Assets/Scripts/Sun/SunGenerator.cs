using UnityEngine;
using System.Collections;

/// <summary>
/// 태양을 자동으로 생성하는 클래스
/// </summary>
public class SunGenerator : MonoBehaviour
{
    [Header("Sun Generation")]
    public GameObject sunPrefab;
    public float generateInterval = 5f;
    public float generateIntervalMin = 3f;
    public float generateIntervalMax = 7f;
    
    [Header("Spawn Area")]
    public float spawnXMin = -4f;
    public float spawnXMax = 4f;
    public float spawnYMin = -2f;
    public float spawnYMax = 2f;
    
    private float lastGenerateTime = 0f;
    private float nextGenerateTime = 0f;
    
    void Start()
    {
        nextGenerateTime = Random.Range(generateIntervalMin, generateIntervalMax);
    }
    
    void Update()
    {
        if (Time.time >= lastGenerateTime + nextGenerateTime && GameManager.Instance.IsGameActive())
        {
            GenerateSun();
            lastGenerateTime = Time.time;
            nextGenerateTime = Random.Range(generateIntervalMin, generateIntervalMax);
        }
    }
    
    /// <summary>
    /// 태양을 생성합니다
    /// </summary>
    void GenerateSun()
    {
        if (sunPrefab == null) return;
        
        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnXMin, spawnXMax),
            Random.Range(spawnYMin, spawnYMax),
            0f
        );
        
        Instantiate(sunPrefab, spawnPosition, Quaternion.identity);
    }
}

