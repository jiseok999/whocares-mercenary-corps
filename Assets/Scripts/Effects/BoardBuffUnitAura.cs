using UnityEngine;

/// <summary>
/// 배치칸·인접 버프 유닛 발밑 상시 글로우(부드러운 원형, 링 없음). 여러 버프는 색만 혼합.
/// </summary>
public class BoardBuffUnitAura : MonoBehaviour
{
    const float BaseGlowAlpha = 0.38f;
    const float AlphaPerExtraSource = 0.06f;
    const float MaxGlowAlpha = 0.52f;
    const int MaxStackContribution = 3;
    const float WorldDiameterMul = 0.92f;
    const float CoreScaleRatio = 0.55f;
    const float ProviderPulseBoost = 1.06f;

    SpriteRenderer outerGlow;
    SpriteRenderer coreGlow;
    float worldDiameter = 1f;
    float pulseTimer;
    Color displayColor = Color.clear;
    int stackCount = 1;
    bool radiatesAdjacentBuff;

    public static void Sync(Character character, bool show, Color tint, int sourceCount, bool adjacentProvider)
    {
        if (character == null) return;

        if (!show || sourceCount <= 0)
        {
            Clear(character);
            return;
        }

        BoardBuffUnitAura aura = GetOrCreate(character);
        if (aura == null) return;

        float cell = 1f;
        BoardManager board = Object.FindFirstObjectByType<BoardManager>();
        if (board != null)
        {
            cell = Mathf.Max(0.01f, board.cellSize);
        }

        float parentScale = Mathf.Max(0.01f, Mathf.Abs(character.transform.lossyScale.x));
        float worldSize = cell * WorldDiameterMul;
        aura.ApplyVisual(tint, sourceCount, worldSize / parentScale, adjacentProvider);
    }

    public static void Clear(Character character)
    {
        if (character == null) return;
        Transform child = character.transform.Find("BoardBuffAura");
        if (child == null) return;
        BoardBuffUnitAura aura = child.GetComponent<BoardBuffUnitAura>();
        if (aura != null)
        {
            aura.SetRenderersEnabled(false);
        }
    }

    static BoardBuffUnitAura GetOrCreate(Character character)
    {
        Transform existing = character.transform.Find("BoardBuffAura");
        if (existing != null)
        {
            BoardBuffUnitAura found = existing.GetComponent<BoardBuffUnitAura>();
            if (found != null) return found;
        }

        GameObject go = new GameObject("BoardBuffAura");
        go.transform.SetParent(character.transform, false);
        go.transform.SetAsFirstSibling();
        go.transform.localPosition = new Vector3(0f, -0.1f, 0f);

        BoardBuffUnitAura aura = go.AddComponent<BoardBuffUnitAura>();
        aura.BuildVisuals(character);
        return aura;
    }

    void BuildVisuals(Character character)
    {
        int sortOrder = GetFloorSortOrder(character);

        outerGlow = CreateGlowLayer(transform, "OuterGlow", sortOrder);
        coreGlow = CreateGlowLayer(transform, "CoreGlow", sortOrder + 1);
        SetRenderersEnabled(false);
    }

    static SpriteRenderer CreateGlowLayer(Transform parent, string name, int sortOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = SkillEffectVisuals.GetPreviewDiscSprite();
        sr.sortingOrder = sortOrder;
        return sr;
    }

    void ApplyVisual(Color tint, int sourceCount, float localDiameter, bool adjacentProvider)
    {
        stackCount = Mathf.Clamp(sourceCount, 1, MaxStackContribution);
        radiatesAdjacentBuff = adjacentProvider;
        float alpha = Mathf.Min(MaxGlowAlpha, BaseGlowAlpha + AlphaPerExtraSource * (stackCount - 1));
        displayColor = new Color(tint.r, tint.g, tint.b, alpha);
        worldDiameter = Mathf.Max(0.4f, localDiameter);

        outerGlow.color = new Color(displayColor.r, displayColor.g, displayColor.b, displayColor.a * 0.72f);
        coreGlow.color = new Color(
            Mathf.Min(1f, displayColor.r * 1.08f + 0.06f),
            Mathf.Min(1f, displayColor.g * 1.08f + 0.06f),
            Mathf.Min(1f, displayColor.b * 1.08f + 0.06f),
            Mathf.Min(1f, displayColor.a * 1.05f));

        outerGlow.transform.localScale = Vector3.one * worldDiameter;
        coreGlow.transform.localScale = Vector3.one * (worldDiameter * CoreScaleRatio);
        SetRenderersEnabled(true);
    }

    void SetRenderersEnabled(bool enabled)
    {
        if (outerGlow != null) outerGlow.enabled = enabled;
        if (coreGlow != null) coreGlow.enabled = enabled;
    }

    static int GetFloorSortOrder(Character character)
    {
        if (character == null) return 0;

        int minBodyOrder = int.MaxValue;
        SpriteRenderer[] renderers = character.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || !sr.enabled) continue;
            Transform t = sr.transform;
            if (t == null || t.name.Contains("BoardBuffAura") || t.name.Contains("Marker") ||
                t.name.Contains("Pad") || t.name.Contains("Shadow"))
            {
                continue;
            }
            if (sr.sortingOrder < minBodyOrder)
            {
                minBodyOrder = sr.sortingOrder;
            }
        }

        return minBodyOrder == int.MaxValue ? 0 : minBodyOrder - 1;
    }

    void Update()
    {
        Character character = GetComponentInParent<Character>();
        if (character == null || outerGlow == null || coreGlow == null)
        {
            return;
        }

        if (character.isShopPreviewInstance || character.IsDragging ||
            !character.IsProperlyPlaced() || character.GetCurrentBoardCell() == null ||
            !character.GetCurrentBoardCell().isBoardCell)
        {
            SetRenderersEnabled(false);
            return;
        }

        SetRenderersEnabled(true);

        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        pulseTimer += Time.deltaTime * 2.4f;
        float pulseAmp = radiatesAdjacentBuff ? 0.07f * ProviderPulseBoost : 0.05f;
        float pulse = 1f + Mathf.Sin(pulseTimer) * pulseAmp;
        float corePulse = 1f + Mathf.Sin(pulseTimer * 1.35f + 0.6f) * (pulseAmp * 0.65f);

        outerGlow.transform.localScale = Vector3.one * (worldDiameter * pulse);
        coreGlow.transform.localScale = Vector3.one * (worldDiameter * CoreScaleRatio * corePulse);

        float alphaPulse = 0.9f + Mathf.Sin(pulseTimer * 1.15f) * 0.1f;
        Color outer = outerGlow.color;
        outer.a = displayColor.a * 0.72f * alphaPulse;
        outerGlow.color = outer;

        Color core = coreGlow.color;
        core.a = Mathf.Min(0.58f, displayColor.a * 1.05f * alphaPulse);
        coreGlow.color = core;
    }
}
