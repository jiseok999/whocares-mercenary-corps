using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 속성 조합(특성) 발동 시 노출되는 특수 스킬 매핑.
/// UI 비활성: GameSceneController.EnableTraitSpecialSkillBar = false
/// </summary>
public static class TraitSpecialSkillRegistry
{
    public struct SkillBinding
    {
        public string traitName;
        public int skillId;
        public string iconResource;
    }

    static readonly SkillBinding[] bindings =
    {
        new SkillBinding { traitName = "얼음", skillId = 1, iconResource = "Ability_ice" },
        new SkillBinding { traitName = "불",   skillId = 2, iconResource = "Ability_fire" },
        new SkillBinding { traitName = "어둠", skillId = 3, iconResource = "Ability_dark" },
        new SkillBinding { traitName = "번개", skillId = 4, iconResource = "Ability_thunder" },
    };

    static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

    public static IReadOnlyList<SkillBinding> AllBindings => bindings;

    public static List<SkillBinding> GetActiveBindings()
    {
        var result = new List<SkillBinding>();
        if (TraitManager.Instance == null) return result;

        for (int i = 0; i < bindings.Length; i++)
        {
            if (TraitManager.Instance.GetTraitActiveLevel(bindings[i].traitName) >= 1)
            {
                result.Add(bindings[i]);
            }
        }

        return result;
    }

    public static string BuildActiveSignature()
    {
        if (TraitManager.Instance == null) return string.Empty;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < bindings.Length; i++)
        {
            int level = TraitManager.Instance.GetTraitActiveLevel(bindings[i].traitName);
            if (level >= 1)
            {
                sb.Append(bindings[i].traitName).Append(':').Append(level).Append('|');
            }
        }

        return sb.ToString();
    }

    public static Sprite GetIcon(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return null;
        if (iconCache.TryGetValue(resourceName, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite icon = Resources.Load<Sprite>(resourceName);
        iconCache[resourceName] = icon;
        return icon;
    }

    /// <summary>스킬 범위 미리보기·발동에 쓰이는 월드 반경</summary>
    public static float GetSkillRadius(int skillId)
    {
        switch (skillId)
        {
            case 1:
            case 2:
                return 5f;
            case 3:
            case 4:
                return 2f;
            default:
                return 1f;
        }
    }
}
