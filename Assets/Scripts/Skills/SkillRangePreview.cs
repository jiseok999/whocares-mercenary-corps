using UnityEngine;

/// <summary>
/// 특수 스킬 준비(armed) 시 마우스 위치에 범위 미리보기를 표시합니다.
/// </summary>
public class SkillRangePreview : MonoBehaviour
{
    SpriteRenderer fillRenderer;
    SpriteRenderer ringRenderer;
    int activeSkillId = -1;
    float pulseTime;
    float baseFillDiameter;
    float baseRingDiameter;

    static readonly Color IceColor = new Color(0.55f, 0.88f, 1f, 0.28f);
    static readonly Color FireColor = new Color(1f, 0.50f, 0.15f, 0.30f);
    static readonly Color DarkColor = new Color(0.50f, 0.22f, 0.75f, 0.32f);
    static readonly Color LightningColor = new Color(1f, 0.85f, 0.30f, 0.30f);

    public void EnsureVisuals()
    {
        if (ringRenderer != null) return;

        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(transform, false);
        ringRenderer = ringObj.AddComponent<SpriteRenderer>();
        ringRenderer.sortingOrder = 48;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(transform, false);
        fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        fillRenderer.sortingOrder = 47;
    }

    public void Show(int skillId)
    {
        EnsureVisuals();
        activeSkillId = skillId;
        pulseTime = 0f;

        Color fill = GetFillColor(skillId);
        Color ring = GetRingColor(skillId);
        float radius = TraitSpecialSkillRegistry.GetSkillRadius(skillId);

        fillRenderer.sprite = SkillEffectVisuals.GetPreviewDiscSprite();
        fillRenderer.color = fill;
        ringRenderer.sprite = SkillEffectVisuals.GetPreviewRingSprite();
        ringRenderer.color = ring;

        float diameter = radius * 2f;
        baseFillDiameter = diameter;
        baseRingDiameter = diameter + 0.25f;
        fillRenderer.transform.localScale = Vector3.one * baseFillDiameter;
        ringRenderer.transform.localScale = Vector3.one * baseRingDiameter;

        fillRenderer.enabled = true;
        ringRenderer.enabled = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        activeSkillId = -1;
        if (fillRenderer != null) fillRenderer.enabled = false;
        if (ringRenderer != null) ringRenderer.enabled = false;
        gameObject.SetActive(false);
    }

    public void SetWorldPosition(Vector3 worldPos)
    {
        worldPos.z = 0f;
        transform.position = worldPos;
    }

    void Update()
    {
        if (activeSkillId < 0 || fillRenderer == null || ringRenderer == null) return;

        pulseTime += Time.deltaTime;
        float pulse = 0.92f + Mathf.Sin(pulseTime * 6f) * 0.06f;
        fillRenderer.transform.localScale = Vector3.one * (baseFillDiameter * pulse);
        ringRenderer.transform.localScale = Vector3.one * (baseRingDiameter * pulse);

        Color fill = fillRenderer.color;
        fill.a = GetFillColor(activeSkillId).a * (0.85f + Mathf.Sin(pulseTime * 8f) * 0.12f);
        fillRenderer.color = fill;
    }

    static Color GetFillColor(int skillId)
    {
        switch (skillId)
        {
            case 1: return IceColor;
            case 2: return FireColor;
            case 3: return DarkColor;
            case 4: return LightningColor;
            default: return new Color(1f, 1f, 1f, 0.2f);
        }
    }

    static Color GetRingColor(int skillId)
    {
        Color fill = GetFillColor(skillId);
        return new Color(Mathf.Min(fill.r + 0.15f, 1f), Mathf.Min(fill.g + 0.15f, 1f), fill.b, 0.85f);
    }
}
