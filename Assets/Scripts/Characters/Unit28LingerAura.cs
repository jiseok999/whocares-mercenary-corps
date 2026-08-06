using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 28 — 9진화 잔류 오라: 범위 피해만 (끌기 없음).
/// </summary>
public class Unit28LingerAura : MonoBehaviour
{
    float radius;
    float expireTime;
    int damage;
    int sourceUnitNumber;
    float damageInterval = 1f;
    readonly Dictionary<Zombie, float> nextDamage = new Dictionary<Zombie, float>();
    readonly List<Zombie> nullCleanup = new List<Zombie>();

    public void Init(float effectRadius, float duration, int tickDamage, int sourceUnit = 0)
    {
        radius = effectRadius;
        damage = tickDamage;
        sourceUnitNumber = sourceUnit;
        expireTime = Time.time + duration;
    }

    void LateUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }

        ApplyDamage();
    }

    void ApplyDamage()
    {
        Vector2 center = transform.position;
        float rSq = radius * radius;
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie z = zombies[i];
            if (z == null || z.IsExcludedFromCombat) continue;
            Vector2 p = z.transform.position;
            if ((p - center).sqrMagnitude > rSq) continue;
            if (!nextDamage.TryGetValue(z, out float nextT)) nextT = 0f;
            if (Time.time >= nextT)
            {
                z.TakeDamage(damage, sourceUnitNumber);
                nextDamage[z] = Time.time + damageInterval;
            }
        }

        nullCleanup.Clear();
        foreach (var kv in nextDamage)
        {
            if (kv.Key == null) nullCleanup.Add(kv.Key);
        }
        for (int i = 0; i < nullCleanup.Count; i++)
        {
            nextDamage.Remove(nullCleanup[i]);
        }
    }
}
