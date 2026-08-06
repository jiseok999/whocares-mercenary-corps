using System;
using System.Collections.Generic;

/// <summary>
/// 현재 라운드 아군 유닛(번호별) 딜량 집계.
/// </summary>
public static class UnitDamageLedger
{
    public struct Entry
    {
        public int unitNumber;
        public int totalDamage;
        public float share;
    }

    static readonly Dictionary<int, int> totals = new Dictionary<int, int>(32);
    static int trackedRound = -1;
    static int roundTotalDamage;

    public static int RoundTotalDamage => roundTotalDamage;

    public static void ResetForRound(int round)
    {
        trackedRound = round;
        totals.Clear();
        roundTotalDamage = 0;
    }

    public static void Record(int unitNumber, int appliedDamage)
    {
        if (unitNumber <= 0 || appliedDamage <= 0) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsCombatActive()) return;

        int round = GameManager.Instance != null ? GameManager.Instance.currentRound : trackedRound;
        if (round != trackedRound)
        {
            ResetForRound(round);
        }

        if (!totals.TryGetValue(unitNumber, out int current))
        {
            current = 0;
        }

        totals[unitNumber] = current + appliedDamage;
        roundTotalDamage += appliedDamage;
    }

    public static int GetTotalForUnit(int unitNumber)
    {
        return totals.TryGetValue(unitNumber, out int value) ? value : 0;
    }

    public static void CopySortedEntries(List<Entry> buffer, int maxCount)
    {
        buffer.Clear();
        if (totals.Count == 0 || maxCount <= 0) return;

        foreach (KeyValuePair<int, int> pair in totals)
        {
            buffer.Add(new Entry
            {
                unitNumber = pair.Key,
                totalDamage = pair.Value,
                share = roundTotalDamage > 0 ? pair.Value / (float)roundTotalDamage : 0f
            });
        }

        buffer.Sort((a, b) =>
        {
            int cmp = b.totalDamage.CompareTo(a.totalDamage);
            if (cmp != 0) return cmp;
            return a.unitNumber.CompareTo(b.unitNumber);
        });

        if (buffer.Count > maxCount)
        {
            buffer.RemoveRange(maxCount, buffer.Count - maxCount);
        }

        for (int i = 0; i < buffer.Count; i++)
        {
            Entry entry = buffer[i];
            entry.share = roundTotalDamage > 0 ? entry.totalDamage / (float)roundTotalDamage : 0f;
            buffer[i] = entry;
        }
    }
}
