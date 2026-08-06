using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 23 투사체가 깔아둔 장판 세그먼트들을 그룹으로 관리.
/// 투사체가 소멸되고 마지막 장판이 설치된 뒤 일정 시간이 지나면
/// 가장 먼저 생성된 것부터 빠르게 순차 삭제한다.
/// </summary>
public class Unit23TrailManager : MonoBehaviour
{
    public float postLastPadDelay = 3f;
    public float sequentialDestroyInterval = 0.05f;

    private readonly List<Unit23TrailPad> pads = new List<Unit23TrailPad>();
    private float lastPadTime = 0f;
    private bool projectileEnded = false;
    private bool destroyPhaseStarted = false;
    private float nextDestroyTime = 0f;

    public void RegisterPad(Unit23TrailPad pad)
    {
        if (pad == null) return;
        pads.Add(pad);
        lastPadTime = Time.time;
    }

    public void NotifyProjectileEnded()
    {
        projectileEnded = true;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        if (!destroyPhaseStarted)
        {
            if (!projectileEnded) return;
            if (pads.Count == 0)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time < lastPadTime + postLastPadDelay) return;

            destroyPhaseStarted = true;
            nextDestroyTime = Time.time;
        }

        if (Time.time < nextDestroyTime) return;

        while (pads.Count > 0 && pads[0] == null)
        {
            pads.RemoveAt(0);
        }

        if (pads.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        Unit23TrailPad head = pads[0];
        pads.RemoveAt(0);
        if (head != null)
        {
            Destroy(head.gameObject);
        }

        nextDestroyTime = Time.time + Mathf.Max(0.01f, sequentialDestroyInterval);
    }
}
