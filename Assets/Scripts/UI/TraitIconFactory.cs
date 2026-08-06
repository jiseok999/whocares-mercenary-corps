using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조합(특성) 아이콘 스프라이트 로드·캐싱
/// </summary>
public static class TraitIconFactory
{
    static readonly Dictionary<string, string> ResourceNames = new Dictionary<string, string>
    {
        { "어둠", "Ability_dark" },
        { "종말", "Ability_end" },
        { "재앙", "Ability_end" },
        { "악마", "Ability_evil" },
        { "불", "Ability_fire" },
        { "도깨비", "Ability_goblin" },
        { "얼음", "Ability_ice" },
        { "기사단", "Ability_knight" },
        { "빛", "Ability_light" },
        { "자연", "Ability_nature" },
        { "펭귄", "Ability_penguin" },
        { "도적단", "Ability_thief" },
        { "번개", "Ability_thunder" },
        { "파수꾼", "Ability_warden" },
        { "바람", "Ability_wind" },
        { "마법사", "Ability_wizard" }
    };

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    static Sprite defaultSprite;

    public static Sprite Get(string traitName)
    {
        if (string.IsNullOrEmpty(traitName))
        {
            return GetDefault();
        }

        if (Cache.TryGetValue(traitName, out Sprite cached) && cached != null)
        {
            return cached;
        }

        if (ResourceNames.TryGetValue(traitName, out string resourceName))
        {
            Sprite loaded = Resources.Load<Sprite>(resourceName);
            Cache[traitName] = loaded;
            if (loaded != null)
            {
                return loaded;
            }
        }

        return GetDefault();
    }

    static Sprite GetDefault()
    {
        if (defaultSprite != null)
        {
            return defaultSprite;
        }

        defaultSprite = Resources.Load<Sprite>("Ability_icons1_15");
        return defaultSprite;
    }
}
