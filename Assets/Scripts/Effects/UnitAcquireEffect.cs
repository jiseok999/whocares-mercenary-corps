using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아군 유닛 소환·진화 연출 — 바닥 글로우·빛기둥·페이드인(유닛 localScale은 변경하지 않음).
/// </summary>
public class UnitAcquireEffect : MonoBehaviour
{
    enum EffectKind
    {
        Summon,
        Evolution
    }

    const float GroundGlowDuration = 0.2f;
    const float RevealDuration = 0.34f;
    const float FinishFlashDuration = 0.16f;
    const int StartupWaitFrames = 2;
    const int SparkCount = 5;

    static readonly Color SummonGlow = new Color(0.55f, 0.88f, 1f, 0.38f);
    static readonly Color SummonBeam = new Color(0.82f, 0.94f, 1f, 0.5f);
    static readonly Color SummonSpark = new Color(1f, 0.96f, 0.72f, 0.75f);

    static readonly Color EvolutionGlow = new Color(1f, 0.72f, 0.32f, 0.42f);
    static readonly Color EvolutionBeam = new Color(1f, 0.86f, 0.48f, 0.55f);
    static readonly Color EvolutionSpark = new Color(1f, 0.92f, 0.55f, 0.8f);

    Character targetCharacter;
    SpriteRenderer[] spriteRenderers;
    Color[] originalColors;
    bool visualsCaptured;

    SpriteRenderer groundGlow;
    SpriteRenderer beam;
    readonly SparkFx[] sparks = new SparkFx[SparkCount];

    struct SparkFx
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float angleRad;
    }

    public static void PlaySummon(Character character)
    {
        Play(character, EffectKind.Summon);
    }

    public static void PlayEvolution(Character character)
    {
        Play(character, EffectKind.Evolution);
    }

    static void Play(Character character, EffectKind kind)
    {
        if (character == null) return;

        GameObject host = new GameObject(kind == EffectKind.Summon ? "UnitSummonFx" : "UnitEvolutionFx");
        host.transform.SetParent(character.transform, false);
        host.transform.localPosition = Vector3.zero;

        UnitAcquireEffect effect = host.AddComponent<UnitAcquireEffect>();
        effect.StartCoroutine(effect.RunAfterCharacterReady(character, kind));
    }

    IEnumerator RunAfterCharacterReady(Character character, EffectKind kind)
    {
        for (int i = 0; i < StartupWaitFrames; i++)
        {
            yield return null;
            if (character == null)
            {
                yield break;
            }
        }

        if (character == null)
        {
            yield break;
        }

        bool isSummon = kind == EffectKind.Summon;
        if (!visualsCaptured)
        {
            CaptureVisuals(character);
        }

        if (isSummon)
        {
            ApplySpriteAlpha(0f);
        }

        yield return Run(character, kind);
    }

    IEnumerator Run(Character character, EffectKind kind)
    {
        if (character == null)
        {
            yield break;
        }

        targetCharacter = character;
        float baseDiameter = GetEffectDiameter(character);
        int sortOrder = GetEffectSortOrder(character);

        bool isSummon = kind == EffectKind.Summon;
        Color glowColor = isSummon ? SummonGlow : EvolutionGlow;
        Color beamColor = isSummon ? SummonBeam : EvolutionBeam;
        Color sparkColor = isSummon ? SummonSpark : EvolutionSpark;

        Transform fxRoot = transform;
        groundGlow = CreateFxSprite(fxRoot, "GroundGlow", glowColor, sortOrder, -0.03f, SkillEffectVisuals.GetPreviewDiscSprite());
        beam = CreateFxSprite(fxRoot, "LightBeam", beamColor, sortOrder + 2, 0f, SkillEffectVisuals.GetPreviewDiscSprite());
        beam.transform.localScale = new Vector3(baseDiameter * 0.22f, baseDiameter * 1.05f, 1f);
        CreateSparks(fxRoot, sparkColor, sortOrder + 1, baseDiameter);

        float glowDur = isSummon ? GroundGlowDuration : GroundGlowDuration * 0.85f;
        float revealDur = isSummon ? RevealDuration : RevealDuration * 0.85f;
        float finishDur = FinishFlashDuration;

        float elapsed = 0f;
        float total = glowDur + revealDur + finishDur;

        while (elapsed < total)
        {
            if (character == null)
            {
                RestoreCharacterVisuals();
                yield break;
            }

            float dt = GetEffectDeltaTime();
            elapsed += dt;

            float glowEnd = glowDur;
            float revealEnd = glowDur + revealDur;

            float glowT = glowEnd > 0.0001f ? Mathf.Clamp01(elapsed / glowEnd) : 1f;
            float revealT = revealDur > 0.0001f ? Mathf.Clamp01((elapsed - glowEnd) / revealDur) : 1f;
            float finishT = finishDur > 0.0001f ? Mathf.Clamp01((elapsed - revealEnd) / finishDur) : 1f;

            AnimateGroundGlow(groundGlow, baseDiameter, glowT, glowColor);
            AnimateBeam(beam, baseDiameter, elapsed < revealEnd ? revealT : 0f, finishT, beamColor);
            AnimateSparks(baseDiameter, glowT, revealT, sparkColor);

            if (elapsed >= glowEnd * 0.25f)
            {
                if (isSummon)
                {
                    AnimateSummonReveal(revealT);
                }
                else
                {
                    AnimateEvolutionReveal(revealT);
                }
            }

            if (elapsed >= revealEnd)
            {
                ApplyFinishTint(finishT, isSummon);
            }

            yield return null;
        }

        RestoreCharacterVisuals();
        Destroy(gameObject);
    }

    void CreateSparks(Transform parent, Color color, int sortOrder, float baseDiameter)
    {
        for (int i = 0; i < SparkCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / SparkCount + 0.35f;
            SpriteRenderer sr = CreateFxSprite(parent, $"Spark_{i}", color, sortOrder, 0f, SkillEffectVisuals.GetPreviewDiscSprite());
            sr.transform.localScale = Vector3.one * (baseDiameter * 0.08f);
            sparks[i] = new SparkFx
            {
                transform = sr.transform,
                renderer = sr,
                angleRad = angle
            };
        }
    }

    static void AnimateGroundGlow(SpriteRenderer glow, float baseDiameter, float t, Color baseColor)
    {
        if (glow == null) return;

        float expand = EaseOutCubic(Mathf.Clamp01(t * 1.1f));
        float fade = 1f - EaseInQuad(Mathf.Clamp01((t - 0.35f) / 0.65f));
        float pulse = Mathf.Sin(t * Mathf.PI);

        float scale = baseDiameter * (0.35f + expand * 0.45f);
        glow.transform.localScale = Vector3.one * scale;
        glow.transform.localRotation = Quaternion.identity;

        Color c = baseColor;
        c.a = baseColor.a * fade * pulse;
        glow.color = c;
    }

    void AnimateSparks(float baseDiameter, float glowT, float revealT, Color sparkBase)
    {
        float travel = Mathf.Max(glowT, revealT * 0.85f);
        float fade = 1f - EaseInQuad(travel);

        for (int i = 0; i < sparks.Length; i++)
        {
            SparkFx spark = sparks[i];
            if (spark.renderer == null || spark.transform == null) continue;

            float radius = baseDiameter * (0.12f + travel * 0.38f);
            float height = baseDiameter * (0.05f + travel * 0.55f);
            Vector3 offset = new Vector3(Mathf.Cos(spark.angleRad) * radius, height, 0f);
            spark.transform.localPosition = offset;

            float size = baseDiameter * (0.06f + (1f - travel) * 0.05f);
            spark.transform.localScale = Vector3.one * size;

            Color c = sparkBase;
            c.a = sparkBase.a * fade * Mathf.Sin(Mathf.Clamp01(travel) * Mathf.PI);
            spark.renderer.color = c;
        }
    }

    static float GetEffectDiameter(Character character)
    {
        BoardManager board = Object.FindFirstObjectByType<BoardManager>();
        if (board != null)
        {
            return Mathf.Max(0.75f, board.cellSize * 1.05f);
        }

        if (character != null)
        {
            Vector3 lossy = character.transform.lossyScale;
            return Mathf.Max(0.75f, Mathf.Abs(lossy.x) * 1.05f);
        }

        return 1f;
    }

    void CaptureVisuals(Character character)
    {
        SpriteRenderer[] allRenderers = character.GetComponentsInChildren<SpriteRenderer>(true);
        var unitRenderers = new List<SpriteRenderer>(allRenderers.Length);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            SpriteRenderer sr = allRenderers[i];
            if (sr == null || IsEffectRenderer(sr)) continue;
            unitRenderers.Add(sr);
        }

        spriteRenderers = unitRenderers.ToArray();
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }

        visualsCaptured = true;
    }

    bool IsEffectRenderer(SpriteRenderer renderer)
    {
        if (renderer == null || transform == null) return false;
        return renderer.transform == transform || renderer.transform.IsChildOf(transform);
    }

    void RestoreCharacterVisuals()
    {
        if (spriteRenderers == null || originalColors == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;
            sr.color = originalColors[i];
        }
    }

    void AnimateSummonReveal(float t)
    {
        float eased = EaseOutBack(t);
        float alpha = Mathf.Clamp01(eased);
        ApplySpriteAlpha(alpha);
    }

    void AnimateEvolutionReveal(float t)
    {
        ApplySpriteAlpha(1f);
        float punch = Mathf.Sin(t * Mathf.PI);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color baseColor = originalColors[i];
            Color c = Color.Lerp(baseColor, new Color(1f, 0.92f, 0.55f, baseColor.a), punch * 0.4f);
            sr.color = c;
        }
    }

    void ApplyFinishTint(float t, bool isSummon)
    {
        float flash = Mathf.Sin(t * Mathf.PI);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color baseColor = originalColors[i];
            Color highlight = isSummon ? Color.white : new Color(1f, 0.92f, 0.55f, baseColor.a);
            Color c = Color.Lerp(baseColor, highlight, flash * 0.35f);
            c.a = baseColor.a;
            sr.color = c;
        }
    }

    void ApplySpriteAlpha(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color c = originalColors[i];
            c.a = c.a * alpha;
            sr.color = c;
        }
    }

    static void AnimateBeam(SpriteRenderer beamRenderer, float baseDiameter, float revealT, float finishT, Color beamBase)
    {
        if (beamRenderer == null) return;

        float revealPulse = revealT > 0.0001f ? Mathf.Sin(revealT * Mathf.PI) : 0f;
        float finishPulse = finishT > 0.0001f ? Mathf.Sin(finishT * Mathf.PI) * 0.45f : 0f;
        float pulse = Mathf.Max(revealPulse, finishPulse);

        beamRenderer.transform.localScale = new Vector3(
            baseDiameter * (0.18f + pulse * 0.1f),
            baseDiameter * (0.95f + pulse * 0.2f),
            1f);
        Color c = beamBase;
        c.a = beamBase.a * pulse;
        beamRenderer.color = c;
    }

    static SpriteRenderer CreateFxSprite(Transform parent, string name, Color color, int sortOrder, float localY, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(0f, localY, 0f);

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortOrder;
        return renderer;
    }

    static int GetEffectSortOrder(Character character)
    {
        int order = 4;
        SpriteRenderer[] renderers = character.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;
            if (renderer.sortingOrder > order)
            {
                order = renderer.sortingOrder;
            }
        }

        return order + 2;
    }

    static float GetEffectDeltaTime()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return Time.unscaledDeltaTime;
        }

        return Time.deltaTime;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }

    static float EaseInQuad(float t)
    {
        return t * t;
    }
}
