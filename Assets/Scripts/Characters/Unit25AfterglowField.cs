using UnityEngine;

public class Unit25AfterglowField : MonoBehaviour
{
    Vector3 origin;
    Vector3 end;
    float halfWidth;
    float expireTime;
    int damage;
    int sourceUnitNumber;
    float nextTick;

    public void Init(Vector3 from, Vector3 to, float beamWidth, float duration, int tickDamage, int sourceUnit = 0)
    {
        origin = from;
        end = to;
        halfWidth = beamWidth * 0.5f;
        damage = tickDamage;
        sourceUnitNumber = sourceUnit;
        expireTime = Time.time + duration;
        nextTick = Time.time;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }
        if (Time.time < nextTick) return;
        nextTick = Time.time + 0.5f;
        ApplyDamage();
    }

    void ApplyDamage()
    {
        Vector2 a = origin;
        Vector2 b = end;
        Vector2 ab = b - a;
        float abLenSq = ab.sqrMagnitude;
        if (abLenSq < 0.0001f) return;
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie z = zombies[i];
            if (z == null || z.IsExcludedFromCombat) continue;
            Vector2 p = z.transform.position;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLenSq);
            Vector2 closest = a + ab * t;
            if (Vector2.Distance(p, closest) <= halfWidth)
            {
                z.TakeDamage(damage, sourceUnitNumber);
            }
        }
    }
}
