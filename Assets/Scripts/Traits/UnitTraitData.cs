using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 표시 이름 및 조합(조합1·조합2)
/// </summary>
public static class UnitTraitData
{
    private static readonly Dictionary<int, string[]> unitTraits = new Dictionary<int, string[]>
    {
        { 1, new[] { "도깨비", "자연" } },
        { 2, new[] { "마법사", "불" } },
        { 3, new[] { "파수꾼", "번개" } },
        { 4, new[] { "기사단", "자연" } },
        { 5, new[] { "도깨비", "번개" } },
        { 6, new[] { "도적단", "얼음" } },
        { 7, new[] { "마법사", "바람" } },
        { 8, new[] { "악마", "자연" } },
        { 9, new[] { "도깨비", "빛" } },
        { 10, new[] { "기사단", "바람" } },
        { 11, new[] { "악마", "얼음" } },
        { 12, new[] { "기사단", "번개" } },
        { 13, new[] { "도적단", "불" } },
        { 14, new[] { "재앙", "빛" } },
        { 15, new[] { "마법사", "얼음" } },
        { 16, new[] { "도깨비", "불" } },
        { 17, new[] { "도깨비", "어둠" } },
        { 18, new[] { "파수꾼", "자연" } },
        { 19, new[] { "마법사", "어둠" } },
        { 20, new[] { "악마", "번개" } },
        { 21, new[] { "기사단", "빛" } },
        { 22, new[] { "마법사", "자연" } },
        { 23, new[] { "재앙", "불" } },
        { 24, new[] { "파수꾼", "바람" } },
        { 25, new[] { "재앙", "얼음" } },
        { 26, new[] { "펭귄", "얼음" } },
        { 27, new[] { "도깨비", "바람" } },
        { 28, new[] { "재앙", "어둠" } }
    };

    public static string GetDisplayName(int unitNumber)
    {
        return GameLocalization.GetUnitDisplayName(unitNumber);
    }

    /// <summary>표에서 이름이 빨간색으로 표기된 유닛(10, 12, 21)</summary>
    public static bool IsEliteHighlightName(int unitNumber)
    {
        return unitNumber == 10 || unitNumber == 12 || unitNumber == 21;
    }

    public static Color GetDisplayNameColor(int unitNumber)
    {
        return IsEliteHighlightName(unitNumber)
            ? new Color(0.95f, 0.28f, 0.28f, 1f)
            : Color.white;
    }

    public static string[] GetTraits(int unitNumber)
    {
        if (!unitTraits.ContainsKey(unitNumber)) return new string[0];
        return unitTraits[unitNumber];
    }
}
