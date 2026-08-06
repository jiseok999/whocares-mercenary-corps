using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 23 투사체가 지나간 경로에 깔리는 장판 세그먼트.
/// 자체 수명은 없으며 Unit23TrailManager가 순차적으로 파괴한다.
/// 위에 올라온 좀비에게 주기적으로 피해를 준다(데미지는 발사 유닛 스탯에서 설정).
/// </summary>
public class Unit23TrailPad : MonoBehaviour
{
    public int sourceUnitNumber;
    private float size = 0.9f;
    private int damageAmount = 1;
    private float damageInterval = 1f;
    private SpriteRenderer padRenderer;
    private Color baseColor = new Color(1f, 0.2f, 0.2f, 0.3f);
    private readonly Dictionary<Zombie, float> nextDamageTimes = new Dictionary<Zombie, float>();

    public void Configure(float padSize, int damage, float interval, Color color, int sortingOrder)
    {
        size = Mathf.Max(0.05f, padSize);
        damageAmount = Mathf.Max(0, damage);
        damageInterval = Mathf.Max(0.1f, interval);
        baseColor = color;

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(transform, false);
        padRenderer = visualObj.AddComponent<SpriteRenderer>();
        padRenderer.sprite = CreateCircleSprite();
        padRenderer.color = baseColor;
        padRenderer.sortingOrder = sortingOrder;
        visualObj.transform.localScale = Vector3.one * size;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        if (damageAmount <= 0) return;

        float halfSize = size * 0.5f;
        Vector2 center = transform.position;
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie zombie = zombies[i];
            if (zombie == null || zombie.IsExcludedFromCombat) continue;

            Vector2 p = zombie.transform.position;
            if (Mathf.Abs(p.x - center.x) > halfSize) continue;
            if (Mathf.Abs(p.y - center.y) > halfSize) continue;

            float next;
            if (!nextDamageTimes.TryGetValue(zombie, out next)) next = 0f;
            if (Time.time >= next)
            {
                zombie.TakeDamage(damageAmount, sourceUnitNumber);
                nextDamageTimes[zombie] = Time.time + damageInterval;
            }
        }

        CleanupDeadZombies();
    }

    void CleanupDeadZombies()
    {
        if (nextDamageTimes.Count == 0) return;
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

    Sprite CreateCircleSprite()
    {
        int texSize = 32;
        Texture2D tex = new Texture2D(texSize, texSize);
        tex.filterMode = FilterMode.Bilinear;
        float radius = texSize * 0.5f;
        float innerRadius = radius * 0.85f;
        Vector2 c = new Vector2(radius, radius);
        Color[] pixels = new Color[texSize * texSize];
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float alpha = 1f;
                if (d > radius) alpha = 0f;
                else if (d > innerRadius) alpha = 1f - (d - innerRadius) / (radius - innerRadius);
                pixels[y * texSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
    }
}
