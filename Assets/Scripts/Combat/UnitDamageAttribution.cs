using UnityEngine;

/// <summary>
/// 아군 딜 귀속 유닛 번호 조회.
/// </summary>
public static class UnitDamageAttribution
{
    public static int From(Character character)
    {
        return character != null ? character.unitNumber : 0;
    }

    public static int ForTraitSkill(int skillId)
    {
        for (int i = 0; i < TraitSpecialSkillRegistry.AllBindings.Count; i++)
        {
            TraitSpecialSkillRegistry.SkillBinding binding = TraitSpecialSkillRegistry.AllBindings[i];
            if (binding.skillId != skillId) continue;
            return ResolveTraitAnchorUnit(binding.traitName);
        }

        return 0;
    }

    public static int ResolveTraitAnchorUnit(string traitName)
    {
        if (string.IsNullOrEmpty(traitName)) return 0;

        Character[] units = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character best = null;
        int bestEvo = -1;

        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || !unit.IsProperlyPlaced()) continue;

            string[] traits = UnitTraitData.GetTraits(unit.unitNumber);
            if (traits == null) continue;

            bool matched = false;
            for (int t = 0; t < traits.Length; t++)
            {
                if (traits[t] == traitName)
                {
                    matched = true;
                    break;
                }
            }

            if (!matched) continue;

            if (best == null || unit.evolutionLevel > bestEvo)
            {
                best = unit;
                bestEvo = unit.evolutionLevel;
            }
        }

        return From(best);
    }
}
