using UnityEngine;

/// <summary>
/// 유닛별 영구 강화 3번째 슬롯(역할 특화) 종류.
/// </summary>
public static class UnitArchiveSpecialty
{
    public enum Type { None, Range, Economy }

    public static Type GetType(int unitNumber)
    {
        if (unitNumber == 4) return Type.Economy;
        if (HasGuardTrait(unitNumber)) return Type.Range;
        return Type.None;
    }

    static bool HasGuardTrait(int unitNumber)
    {
        string[] traits = UnitTraitData.GetTraits(unitNumber);
        if (traits == null) return false;
        for (int i = 0; i < traits.Length; i++)
        {
            if (traits[i] == "파수꾼") return true;
        }
        return false;
    }

    public static bool HasSpecialty(int unitNumber) => GetType(unitNumber) != Type.None;
}
