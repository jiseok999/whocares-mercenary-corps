using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 새로고침 시 슬롯 영역에 짧은 플래시·등장 연출을 재생합니다.
/// </summary>
public class ShopRefreshEffect : MonoBehaviour
{
    const float FlashDuration = 0.32f;
    const float SlotPopDuration = 0.28f;
    const float SlotStagger = 0.07f;
    const float PostRevealDelay = 0.35f;

    public static float GetRevealDuration(int slotCount)
    {
        int count = Mathf.Max(0, slotCount);
        return FlashDuration + SlotStagger * Mathf.Max(0, count - 1) + SlotPopDuration + PostRevealDelay;
    }

    static readonly Color FlashColor = new Color(1f, 0.88f, 0.42f, 0.45f);
    static readonly Color RingColor = new Color(1f, 0.92f, 0.55f, 0.55f);

    Coroutine activeRoutine;

    public void PlayReveal(Transform slotsParent, IList<GameObject> slotItems)
    {
        if (slotsParent == null || slotItems == null || slotItems.Count == 0)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(RevealRoutine(slotsParent, slotItems));
    }

    IEnumerator RevealRoutine(Transform slotsParent, IList<GameObject> slotItems)
    {
        RectTransform slotsRect = slotsParent as RectTransform;
        if (slotsRect == null)
        {
            yield break;
        }

        for (int i = 0; i < slotItems.Count; i++)
        {
            GameObject item = slotItems[i];
            if (item == null) continue;
            item.transform.localScale = Vector3.zero;
        }

        GameObject flashRoot = CreateFlashOverlay(slotsRect);
        Image flashFill = flashRoot.transform.Find("FlashFill")?.GetComponent<Image>();
        Image flashRing = flashRoot.transform.Find("FlashRing")?.GetComponent<Image>();

        float elapsed = 0f;
        float totalDuration = FlashDuration + SlotStagger * Mathf.Max(0, slotItems.Count - 1) + SlotPopDuration;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float flashT = Mathf.Clamp01(elapsed / FlashDuration);
            float flashPulse = Mathf.Sin(flashT * Mathf.PI);
            if (flashFill != null)
            {
                Color fill = FlashColor;
                fill.a = FlashColor.a * flashPulse;
                flashFill.color = fill;
                flashFill.rectTransform.localScale = Vector3.one * (0.85f + flashPulse * 0.2f);
            }

            if (flashRing != null)
            {
                Color ring = RingColor;
                ring.a = RingColor.a * flashPulse * 0.85f;
                flashRing.color = ring;
                flashRing.rectTransform.localScale = Vector3.one * (1.05f + flashPulse * 0.15f);
            }

            for (int i = 0; i < slotItems.Count; i++)
            {
                GameObject item = slotItems[i];
                if (item == null) continue;

                float slotStart = SlotStagger * i;
                float slotT = Mathf.Clamp01((elapsed - slotStart) / SlotPopDuration);
                float eased = EaseOutBack(slotT);
                item.transform.localScale = Vector3.one * eased;
            }

            yield return null;
        }

        for (int i = 0; i < slotItems.Count; i++)
        {
            if (slotItems[i] != null)
            {
                slotItems[i].transform.localScale = Vector3.one;
            }
        }

        if (flashRoot != null)
        {
            Destroy(flashRoot);
        }

        activeRoutine = null;
    }

    static GameObject CreateFlashOverlay(RectTransform slotsRect)
    {
        GameObject root = new GameObject("ShopRefreshFlash");
        root.transform.SetParent(slotsRect, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = new Vector2(-24f, -20f);
        rootRect.offsetMax = new Vector2(24f, 20f);
        root.transform.SetAsLastSibling();

        GameObject fillObj = new GameObject("FlashFill");
        fillObj.transform.SetParent(root.transform, false);
        Image fill = fillObj.AddComponent<Image>();
        fill.sprite = SkillEffectVisuals.GetPreviewDiscSprite();
        fill.color = FlashColor;
        fill.raycastTarget = false;
        StretchCenter(fillObj.GetComponent<RectTransform>());

        GameObject ringObj = new GameObject("FlashRing");
        ringObj.transform.SetParent(root.transform, false);
        Image ring = ringObj.AddComponent<Image>();
        ring.sprite = SkillEffectVisuals.GetPreviewRingSprite();
        ring.color = RingColor;
        ring.raycastTarget = false;
        StretchCenter(ringObj.GetComponent<RectTransform>());

        return root;
    }

    static void StretchCenter(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(920f, 200f);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
