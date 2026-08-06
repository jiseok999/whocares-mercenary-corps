using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 11: 랜덤 적 x열에 세로로 좁은 빙결 장판. 닿은 좀비 중 최대 N명만 빙결(앞줄 x 우선).
/// </summary>
public class Unit11IceField : MonoBehaviour
{
    float mMinX;
    float mMaxX;
    float mMinY;
    float mMaxY;
    float mMidY;
    float endTime;
    float freezeDuration = 2f;
    int maxFreezeTargets = 5;

    readonly List<Zombie> scratch = new List<Zombie>(16);

    public void Init(float centerX, float worldWidth, float minLaneY, float maxLaneY, float holdDuration, float freezeSec = 2f, int maxTargets = 5)
    {
        float half = Mathf.Max(0.01f, worldWidth * 0.5f);
        mMinX = centerX - half;
        mMaxX = centerX + half;
        mMinY = minLaneY;
        mMaxY = maxLaneY;
        mMidY = (minLaneY + maxLaneY) * 0.5f;
        endTime = Time.time + holdDuration;
        freezeDuration = freezeSec;
        maxFreezeTargets = Mathf.Max(1, maxTargets);
    }

    void Update()
    {
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        scratch.Clear();
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie z = zombies[i];
            if (z == null || z.IsExcludedFromCombat) continue;
            Vector2 p = z.transform.position;
            if (p.x < mMinX || p.x > mMaxX || p.y < mMinY || p.y > mMaxY) continue;
            scratch.Add(z);
        }

        if (scratch.Count == 0) return;

        scratch.Sort(CompareFreezePriority);
        int count = Mathf.Min(maxFreezeTargets, scratch.Count);
        for (int i = 0; i < count; i++)
        {
            scratch[i].ApplyUnit11Freeze(freezeDuration);
        }
    }

    int CompareFreezePriority(Zombie a, Zombie b)
    {
        Vector2 pa = a.transform.position;
        Vector2 pb = b.transform.position;
        int byX = pa.x.CompareTo(pb.x);
        if (byX != 0) return byX;
        float da = Mathf.Abs(pa.y - mMidY);
        float db = Mathf.Abs(pb.y - mMidY);
        return da.CompareTo(db);
    }
}
