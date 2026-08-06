using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 등급별 유닛 풀 고정 — 서로 다른 N종 획득 시 해당 등급 후보가 그 종류로만 제한됩니다.
/// A: 서로 다른 종류 기준 / D: 등급별 임계값 차등 / 보유 유닛 기준으로 동기화됩니다.
/// </summary>
public class ShopGradePoolLock
{
    public struct GradePoolStatus
    {
        public int grade;
        public int distinctCount;
        public int threshold;
        public bool isLocked;
        public List<int> unitNumbers;
    }

    readonly Dictionary<int, HashSet<int>> _distinctByGrade = new Dictionary<int, HashSet<int>>(5);

    /// <summary>등급별 풀 고정에 필요한 서로 다른 종류 수 (D).</summary>
    public static int GetLockThreshold(int grade)
    {
        switch (grade)
        {
            case 5:
            case 4:
            case 3:
                return 3;
            case 2:
            case 1:
                return 2;
            default:
                return 3;
        }
    }

    public void Reset()
    {
        _distinctByGrade.Clear();
    }

    public void RegisterPurchase(int unitNumber, int grade)
    {
        if (unitNumber <= 0 || grade <= 0) return;
        if (!_distinctByGrade.TryGetValue(grade, out HashSet<int> set))
        {
            set = new HashSet<int>();
            _distinctByGrade[grade] = set;
        }
        set.Add(unitNumber);
    }

    public int GetDistinctCount(int grade)
    {
        return _distinctByGrade.TryGetValue(grade, out HashSet<int> set) ? set.Count : 0;
    }

    public bool IsLocked(int grade)
    {
        return GetDistinctCount(grade) >= GetLockThreshold(grade);
    }

    public bool ContainsType(int grade, int unitNumber)
    {
        return _distinctByGrade.TryGetValue(grade, out HashSet<int> set) && set.Contains(unitNumber);
    }

    public List<int> GetDistinctUnitNumbers(int grade)
    {
        var result = new List<int>();
        if (!_distinctByGrade.TryGetValue(grade, out HashSet<int> set)) return result;
        result.AddRange(set);
        result.Sort();
        return result;
    }

    public List<GradePoolStatus> GetActiveStatuses()
    {
        var result = new List<GradePoolStatus>(5);
        for (int grade = 5; grade >= 1; grade--)
        {
            int count = GetDistinctCount(grade);
            if (count <= 0) continue;

            result.Add(new GradePoolStatus
            {
                grade = grade,
                distinctCount = count,
                threshold = GetLockThreshold(grade),
                isLocked = count >= GetLockThreshold(grade),
                unitNumbers = GetDistinctUnitNumbers(grade)
            });
        }
        return result;
    }

    public static string FormatUnitNames(IReadOnlyList<int> unitNumbers, string separator = " · ")
    {
        if (unitNumbers == null || unitNumbers.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < unitNumbers.Count; i++)
        {
            if (i > 0) sb.Append(separator);
            sb.Append(UnitTraitData.GetDisplayName(unitNumbers[i]));
        }
        return sb.ToString();
    }
}
