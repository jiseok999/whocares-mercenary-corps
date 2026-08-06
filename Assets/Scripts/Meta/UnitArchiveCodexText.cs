using System.Text;

using UnityEngine;



/// <summary>

/// 아카이브 도감 — 조합·진화·설명 통합 텍스트.

/// </summary>

public static class UnitArchiveCodexText

{

    public static string BuildPlainText(int unitNumber, bool discovered)

    {

        if (!discovered) return GameLocalization.ArchiveLockedHint;



        var sb = new StringBuilder();

        string[] traits = UnitTraitData.GetTraits(unitNumber);

        if (traits.Length > 0)

        {

            sb.AppendLine(GameLocalization.ArchiveCodexTraitsFormat(GameLocalization.FormatTraitList(traits)));

        }



        sb.AppendLine(GameLocalization.ArchiveCodexGradeFormat(UnitCombatStats.GetGradeForUnit(unitNumber)));



        string desc = GetUnitSummary(unitNumber);

        if (!string.IsNullOrEmpty(desc))

        {

            sb.AppendLine();

            sb.AppendLine(desc);

        }



        var defs = UnitEvolutionMilestones.GetMilestoneDefinitions(unitNumber);

        if (defs.Count > 0)

        {

            sb.AppendLine();

            sb.AppendLine(GameLocalization.ArchiveCodexEvolutionHeader);

            for (int i = 0; i < defs.Count; i++)

            {

                UnitEvolutionMilestones.MilestoneDef def = defs[i];

                sb.AppendLine(GameLocalization.ArchiveCodexEvolutionLineFormat(def.requiredLevel, def.description));

            }

        }



        int awakening = UnitArchiveAwakening.GetLevel(unitNumber);

        if (awakening > 0)

        {

            sb.AppendLine();

            sb.AppendLine(GameLocalization.ArchiveCodexAwakeningHeader);

            for (int i = 0; i < awakening; i++)

            {

                sb.AppendLine(GameLocalization.ArchiveCodexAwakeningLineFormat(

                    i + 1, UnitArchiveAwakening.GetTierDescription(unitNumber, i)));

            }

        }



        return sb.ToString().TrimEnd();

    }



    static string GetUnitSummary(int unitNumber)

    {

        int grade = UnitCombatStats.GetGradeForUnit(unitNumber);

        return GameLocalization.ArchiveCodexSummaryFormat(grade, UnitTraitData.GetDisplayName(unitNumber));

    }

}

