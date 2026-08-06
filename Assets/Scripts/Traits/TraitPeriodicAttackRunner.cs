using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조합별 보드 전역 쿨 → 유닛 위치에서 특수 공격 발동.
/// </summary>
public class TraitPeriodicAttackRunner : MonoBehaviour
{
    public static TraitPeriodicAttackRunner Instance { get; private set; }

    readonly Dictionary<string, float> nextFireTime = new Dictionary<string, float>(16);
    readonly List<Character> sourceBuffer = new List<Character>(12);
    int lastRound = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (!GameManager.Instance.IsCombatActive() || GameManager.Instance.IsCombatPaused()) return;
        if (TraitManager.Instance == null) return;

        int round = GameManager.Instance.currentRound;
        if (round != lastRound)
        {
            lastRound = round;
            nextFireTime.Clear();
        }

        IReadOnlyList<string> active = TraitManager.Instance.GetActiveTraits();
        for (int i = 0; i < active.Count; i++)
        {
            TickTrait(active[i]);
        }
    }

    void TickTrait(string traitName)
    {
        if (!TraitPeriodicAttackDefs.TryGetDef(traitName, out TraitPeriodicAttackDefs.TraitDef def)) return;

        int count = TraitManager.Instance.GetTraitCount(traitName);
        int level = TraitPeriodicAttackDefs.GetActiveLevel(def, count);
        if (level <= 0) return;

        TraitPeriodicAttackDefs.TierDef tier = TraitPeriodicAttackDefs.GetTier(def, level);
        if (tier.cooldown <= 0f) return;

        float now = GameManager.Instance != null ? GameManager.Instance.CombatActionTime : Time.time;
        if (nextFireTime.TryGetValue(traitName, out float next) && now < next) return;

        if (tier.procChance < 0.999f && Random.value > tier.procChance)
        {
            ScheduleNext(traitName, tier, false);
            return;
        }

        if (!TryCollectSources(traitName, sourceBuffer) || sourceBuffer.Count == 0)
        {
            ScheduleNext(traitName, tier, false);
            return;
        }

        Character anchor = sourceBuffer[Random.Range(0, sourceBuffer.Count)];
        int baseDamage = Mathf.Max(1, anchor.GetDisplayAttackDamage());
        float synergyDamage = 1f;
        float synergyCooldown = 1f;
        /*
        if (GameManager.Instance != null && GameManager.Instance.HasMatchingUnitOnSynergyCell(traitName))
        {
            synergyDamage = GameManager.SynergyCellEffectMultiplier;
            synergyCooldown = 0.72f;
        }
        */
        float bossTraitMul = GameManager.Instance != null
            ? GameManager.Instance.GetBossTraitPeriodicDamageMultiplier(anchor)
            : 1f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * tier.damageScale * bossTraitMul * synergyDamage));

        TraitAttackVisuals.Execute(this, tier.kind, anchor, tier, damage, synergyDamage);
        TraitProcToast.Show(this, traitName, GameLocalization.GetTraitProcSkillName(tier.kind), anchor);
        ScheduleNext(traitName, tier, true, synergyCooldown);
    }

    void ScheduleNext(string traitName, TraitPeriodicAttackDefs.TierDef tier, bool fired, float synergyCooldownMul = 1f)
    {
        float cd = tier.cooldown * synergyCooldownMul;
        if (!fired && tier.procChance < 0.999f)
        {
            cd *= 0.35f;
        }
        nextFireTime[traitName] = (GameManager.Instance != null ? GameManager.Instance.CombatActionTime : Time.time) + cd;
    }

    static bool TryCollectSources(string traitName, List<Character> buffer)
    {
        buffer.Clear();
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || !unit.IsProperlyPlaced()) continue;
            string[] traits = UnitTraitData.GetTraits(unit.unitNumber);
            if (traits == null) continue;
            for (int t = 0; t < traits.Length; t++)
            {
                if (traits[t] == traitName)
                {
                    buffer.Add(unit);
                    break;
                }
            }
        }
        return buffer.Count > 0;
    }
}
