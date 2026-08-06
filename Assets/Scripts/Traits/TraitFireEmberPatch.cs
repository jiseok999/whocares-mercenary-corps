using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화염 낙하 2단계 — 착지 지점 잔불 지대 (짧은 화상 DoT).
/// </summary>
public class TraitFireEmberPatch : MonoBehaviour
{
    static readonly List<Zombie> Buffer = new List<Zombie>(32);
    static Sprite emberSprite;

    float endTime;
    float nextBurnTime;
    float radius;
    int burnPerTick;
    float visualScale = 1f;
    SpriteRenderer sr;

    public static void Spawn(Vector3 at, float duration, float rad, int burnDmg, float visualMul = 1f)
    {
        GameObject go = new GameObject("TraitFireEmber");
        go.transform.position = at;
        TraitFireEmberPatch patch = go.AddComponent<TraitFireEmberPatch>();
        patch.Init(duration, rad, burnDmg, visualMul);
    }

    void Init(float duration, float rad, int burnDmg, float visualMul)
    {
        radius = rad;
        burnPerTick = burnDmg;
        visualScale = Mathf.Max(0.1f, visualMul);
        endTime = Time.time + duration;
        nextBurnTime = Time.time + 0.25f;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetEmberSprite();
        sr.color = new Color(1f, 0.42f, 0.08f, 0.58f);
        sr.sortingOrder = 2;
        transform.localScale = Vector3.one * radius * 2f * visualScale;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        float remain = endTime - Time.time;
        float pulse = 0.88f + Mathf.Sin(Time.time * 16f) * 0.07f;
        transform.localScale = Vector3.one * radius * 2f * visualScale * pulse;
        if (sr != null)
        {
            float flicker = 0.32f + Mathf.Sin(Time.time * 11f) * 0.08f;
            sr.color = new Color(1f, flicker, 0.04f, Mathf.Clamp01(remain) * 0.58f);
        }

        if (Time.time < nextBurnTime)
        {
            return;
        }

        nextBurnTime = Time.time + 0.35f;
        Zombie.CopyLivingZombiesTo(Buffer);
        for (int i = 0; i < Buffer.Count; i++)
        {
            Zombie z = Buffer[i];
            if (z == null) continue;
            if (Vector2.Distance(transform.position, z.transform.position) <= radius)
            {
                z.ApplyBurn(burnPerTick, 1.2f);
            }
        }
    }

    static Sprite GetEmberSprite()
    {
        if (emberSprite != null) return emberSprite;

        const int size = 48;
        Texture2D tex = new Texture2D(size, size);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                float a = d <= 1f ? Mathf.SmoothStep(1f, 0f, d) * 0.92f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        emberSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return emberSprite;
    }
}
