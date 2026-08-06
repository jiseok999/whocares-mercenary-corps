using System.Collections.Generic;
using UnityEngine;

public class Unit19BarrierPad : MonoBehaviour
{
    public int sourceUnitNumber;
    private float width;
    private float height;
    private int damageAmount = 1;
    private float damageInterval = 1f;
    private bool applySlowOnPad = false;
    private readonly Dictionary<Zombie, float> nextDamageTimes = new Dictionary<Zombie, float>();

    public void Configure(float padWidth, float padHeight, int damage, float interval, bool slowOnPad = false)
    {
        width = Mathf.Max(0f, padWidth);
        height = Mathf.Max(0f, padHeight);
        damageAmount = Mathf.Max(0, damage);
        damageInterval = Mathf.Max(0.1f, interval);
        applySlowOnPad = slowOnPad;
    }

    void Update()
    {
        if (width <= 0f || height <= 0f || damageAmount <= 0)
        {
            return;
        }

        float halfX = width * 0.5f;
        float halfY = height * 0.5f;
        Vector2 center = transform.position;

        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie zombie = zombies[i];
            if (zombie == null || zombie.IsExcludedFromCombat) continue;

            Vector2 pos = zombie.transform.position;
            if (Mathf.Abs(pos.x - center.x) > halfX || Mathf.Abs(pos.y - center.y) > halfY)
            {
                continue;
            }

            float nextTime;
            if (!nextDamageTimes.TryGetValue(zombie, out nextTime))
            {
                nextTime = 0f;
            }

            if (Time.time >= nextTime)
            {
                zombie.TakeDamage(damageAmount, sourceUnitNumber);
                if (applySlowOnPad)
                {
                    zombie.ApplySlow(1f);
                }
                nextDamageTimes[zombie] = Time.time + damageInterval;
            }
        }

        if (nextDamageTimes.Count > 0)
        {
            List<Zombie> removeTargets = null;
            foreach (var pair in nextDamageTimes)
            {
                if (pair.Key == null)
                {
                    if (removeTargets == null) removeTargets = new List<Zombie>();
                    removeTargets.Add(pair.Key);
                }
            }

            if (removeTargets != null)
            {
                for (int i = 0; i < removeTargets.Count; i++)
                {
                    nextDamageTimes.Remove(removeTargets[i]);
                }
            }
        }
    }
}
