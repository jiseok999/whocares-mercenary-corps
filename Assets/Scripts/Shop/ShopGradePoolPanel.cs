using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 상단 — 등급별 풀 진행/고정 상태 UI (E).
/// </summary>
public class ShopGradePoolPanel : MonoBehaviour
{
    static readonly Color Cream = new Color(0.93f, 0.86f, 0.74f, 1f);
    static readonly Color Muted = new Color(0.62f, 0.56f, 0.48f, 1f);
    static readonly Color AccentGold = new Color(1f, 0.78f, 0.32f, 1f);
    static readonly Color LockedBadge = new Color(0.22f, 0.52f, 0.82f, 0.95f);
    static readonly Color RowBg = new Color(0.10f, 0.08f, 0.06f, 0.72f);

    RectTransform contentRoot;
    readonly List<GameObject> rowObjects = new List<GameObject>(5);

    public static ShopGradePoolPanel Ensure(Transform shopPanelParent)
    {
        if (shopPanelParent == null) return null;

        Transform existing = shopPanelParent.Find("GradePoolPanel");
        if (existing != null)
        {
            return existing.GetComponent<ShopGradePoolPanel>();
        }

        GameObject host = new GameObject("GradePoolPanel");
        host.transform.SetParent(shopPanelParent, false);
        RectTransform rect = host.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -6f);
        rect.sizeDelta = new Vector2(-24f, 0f);

        ShopGradePoolPanel panel = host.AddComponent<ShopGradePoolPanel>();
        panel.BuildContentRoot();
        return panel;
    }

    void BuildContentRoot()
    {
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(transform, false);
        contentRoot = contentObj.AddComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = Vector2.zero;
    }

    public void Refresh(ShopGradePoolLock poolLock)
    {
        if (contentRoot == null) BuildContentRoot();

        List<ShopGradePoolLock.GradePoolStatus> statuses = poolLock != null
            ? poolLock.GetActiveStatuses()
            : new List<ShopGradePoolLock.GradePoolStatus>();

        gameObject.SetActive(statuses.Count > 0);
        if (statuses.Count == 0) return;

        while (rowObjects.Count < statuses.Count)
        {
            rowObjects.Add(CreateRow(rowObjects.Count));
        }

        float y = 0f;
        const float rowHeight = 24f;
        const float rowGap = 4f;

        for (int i = 0; i < rowObjects.Count; i++)
        {
            bool show = i < statuses.Count;
            rowObjects[i].SetActive(show);
            if (!show) continue;

            ApplyRow(rowObjects[i], statuses[i]);
            RectTransform rowRect = rowObjects[i].GetComponent<RectTransform>();
            rowRect.anchoredPosition = new Vector2(0f, -y);
            y += rowHeight + rowGap;
        }

        RectTransform selfRect = transform as RectTransform;
        if (selfRect != null)
        {
            selfRect.sizeDelta = new Vector2(selfRect.sizeDelta.x, Mathf.Max(0f, y));
        }
    }

    GameObject CreateRow(int index)
    {
        GameObject rowObj = new GameObject($"GradePoolRow_{index}");
        rowObj.transform.SetParent(contentRoot, false);

        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, 24f);

        Image bg = rowObj.AddComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.sprite = WarmRoundedSprite.Get(RowBg, 8, new Color(1f, 0.82f, 0.38f, 0.18f), 1f);
        bg.raycastTarget = false;

        CreateLabel(rowObj.transform, "GradeLabel", 14, FontStyle.Bold, Cream,
            new Vector2(12f, 0f), new Vector2(52f, 22f), TextAnchor.MiddleLeft);
        CreateLabel(rowObj.transform, "StateLabel", 12, FontStyle.Bold, AccentGold,
            new Vector2(68f, 0f), new Vector2(72f, 22f), TextAnchor.MiddleLeft);
        CreateLabel(rowObj.transform, "UnitsLabel", 13, FontStyle.Normal, Cream,
            new Vector2(142f, 0f), new Vector2(820f, 22f), TextAnchor.MiddleLeft);

        return rowObj;
    }

    static void ApplyRow(GameObject rowObj, ShopGradePoolLock.GradePoolStatus status)
    {
        Text gradeLabel = rowObj.transform.Find("GradeLabel")?.GetComponent<Text>();
        Text stateLabel = rowObj.transform.Find("StateLabel")?.GetComponent<Text>();
        Text unitsLabel = rowObj.transform.Find("UnitsLabel")?.GetComponent<Text>();
        if (gradeLabel == null || stateLabel == null || unitsLabel == null) return;

        string gradeHex = UnitCombatStats.GetGradeColorHex(status.grade);
        gradeLabel.font = UIFontProvider.Get();
        gradeLabel.text = $"<color=#{gradeHex}>{GameLocalization.GradeNameFormat(status.grade)}</color>";

        string names = ShopGradePoolLock.FormatUnitNames(status.unitNumbers);
        stateLabel.font = UIFontProvider.Get();
        unitsLabel.font = UIFontProvider.Get();
        if (status.isLocked)
        {
            stateLabel.text = $"<color=#FFC94D>{GameLocalization.ShopGradePoolLockedLabel}</color>";
            unitsLabel.supportRichText = true;
            unitsLabel.text = $"<color=#E8DCC4>{names}</color>";
        }
        else
        {
            stateLabel.text = $"<color=#9D8A66>{GameLocalization.ShopGradePoolProgressFormat(status.distinctCount, status.threshold)}</color>";
            int remaining = Mathf.Max(0, status.threshold - status.distinctCount);
            string hint = remaining > 0
                ? $"  <color=#6E6552>{GameLocalization.ShopGradePoolRemainingHint(remaining)}</color>"
                : string.Empty;
            unitsLabel.supportRichText = true;
            unitsLabel.text = $"<color=#E8DCC4>{names}</color>{hint}";
        }
    }

    static Text CreateLabel(Transform parent, string name, int fontSize, FontStyle style, Color color,
        Vector2 anchoredPos, Vector2 size, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }
}
