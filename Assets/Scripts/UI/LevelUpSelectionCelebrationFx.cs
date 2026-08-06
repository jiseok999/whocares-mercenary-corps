using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업·유닛 선택 UI 등장 시 축하 연출 — 골든 플래시, 컨페티, 타이틀 팝.
/// </summary>
public class LevelUpSelectionCelebrationFx : MonoBehaviour
{
    public enum Intensity
    {
        Soft,
        Full
    }

    struct ConfettiPiece
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;
        public float spinDegPerSec;
        public float life;
        public float maxLife;
    }

    const float FlashDuration = 0.38f;
    const float ConfettiLifetime = 2.6f;

    static readonly Color FlashFillColor = new Color(1f, 0.88f, 0.42f, 0.42f);
    static readonly Color FlashRingColor = new Color(1f, 0.94f, 0.58f, 0.55f);
    static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.78f, 0.30f, 1f),
        new Color(1f, 0.92f, 0.55f, 1f),
        new Color(1f, 0.55f, 0.35f, 1f),
        new Color(0.95f, 0.45f, 0.85f, 1f),
        new Color(0.55f, 0.88f, 1f, 1f),
        new Color(0.65f, 0.95f, 0.55f, 1f)
    };

    readonly List<ConfettiPiece> confetti = new List<ConfettiPiece>(64);
    RectTransform confettiRoot;
    Coroutine confettiRoutine;
    GameObject burstRoot;

    public static LevelUpSelectionCelebrationFx Create(Transform overlayRoot)
    {
        GameObject host = new GameObject("LevelUpCelebrationFx");
        host.transform.SetParent(overlayRoot, false);
        RectTransform rect = host.AddComponent<RectTransform>();
        StretchFull(rect);
        rect.SetAsFirstSibling();
        return host.AddComponent<LevelUpSelectionCelebrationFx>();
    }

    public void PlayGoldenBurst(RectTransform anchor, Intensity intensity)
    {
        if (burstRoot != null)
        {
            Destroy(burstRoot);
        }

        burstRoot = new GameObject("GoldenBurst");
        burstRoot.transform.SetParent(anchor, false);
        burstRoot.transform.SetAsFirstSibling();

        RectTransform rootRect = burstRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, 40f);
        rootRect.sizeDelta = intensity == Intensity.Full ? new Vector2(920f, 420f) : new Vector2(760f, 320f);

        Image fill = CreateBurstImage(burstRoot.transform, "FlashFill", SkillEffectVisuals.GetPreviewDiscSprite(), FlashFillColor);
        Image ring = CreateBurstImage(burstRoot.transform, "FlashRing", SkillEffectVisuals.GetPreviewRingSprite(), FlashRingColor);
        StartCoroutine(AnimateBurst(fill, ring, intensity));
    }

    public void SpawnConfetti(RectTransform screenRoot, Intensity intensity)
    {
        if (confettiRoot == null)
        {
            GameObject rootObj = new GameObject("ConfettiRoot");
            rootObj.transform.SetParent(screenRoot, false);
            confettiRoot = rootObj.AddComponent<RectTransform>();
            StretchFull(confettiRoot);
            confettiRoot.SetAsFirstSibling();
        }

        ClearConfetti();
        int count = intensity == Intensity.Full ? 52 : 28;
        float width = confettiRoot.rect.width > 1f ? confettiRoot.rect.width : 1920f;
        float height = confettiRoot.rect.height > 1f ? confettiRoot.rect.height : 1080f;

        for (int i = 0; i < count; i++)
        {
            GameObject pieceObj = new GameObject($"Confetti_{i}");
            pieceObj.transform.SetParent(confettiRoot, false);
            Image img = pieceObj.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = WarmRoundedSprite.Get(ConfettiColors[i % ConfettiColors.Length], 3, Color.white, 0f);

            RectTransform rect = pieceObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float sizeW = Random.Range(7f, intensity == Intensity.Full ? 16f : 13f);
            float sizeH = Random.Range(10f, intensity == Intensity.Full ? 22f : 18f);
            rect.sizeDelta = new Vector2(sizeW, sizeH);
            rect.anchoredPosition = new Vector2(Random.Range(-width * 0.48f, width * 0.48f), Random.Range(20f, height * 0.35f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            ConfettiPiece piece = new ConfettiPiece
            {
                rect = rect,
                image = img,
                velocity = new Vector2(Random.Range(-90f, 90f), Random.Range(-320f, -180f)),
                spinDegPerSec = Random.Range(-240f, 240f),
                life = 0f,
                maxLife = ConfettiLifetime + Random.Range(-0.4f, 0.5f)
            };
            confetti.Add(piece);
        }

        if (confettiRoutine != null)
        {
            StopCoroutine(confettiRoutine);
        }
        confettiRoutine = StartCoroutine(UpdateConfetti());
    }

    public IEnumerator PlayTitlePop(Text titleText, Intensity intensity)
    {
        if (titleText == null) yield break;

        Transform titleTransform = titleText.transform;
        Vector3 baseScale = Vector3.one;
        titleTransform.localScale = baseScale * (intensity == Intensity.Full ? 0.55f : 0.72f);

        float dur = intensity == Intensity.Full ? 0.42f : 0.32f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float scale = intensity == Intensity.Full
                ? Mathf.Lerp(0.55f, 1f, EaseOutBack(t))
                : Mathf.Lerp(0.72f, 1f, EaseOutCubic(t));
            titleTransform.localScale = baseScale * scale;
            yield return null;
        }

        titleTransform.localScale = baseScale;
    }

    public IEnumerator PlayCheerBanner(Transform parent, Intensity intensity)
    {
        bool full = intensity == Intensity.Full;
        string message = full ? "축하합니다!" : "새로운 유닛!";
        float bannerY = full ? 300f : 270f;
        int fontSize = full ? 34 : 26;
        Vector2 bannerSize = full ? new Vector2(420f, 72f) : new Vector2(320f, 56f);

        GameObject bannerObj = new GameObject("CheerBanner");
        bannerObj.transform.SetParent(parent, false);
        RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.anchoredPosition = new Vector2(0f, bannerY);
        bannerRect.sizeDelta = bannerSize;

        Image bannerBg = bannerObj.AddComponent<Image>();
        bannerBg.raycastTarget = false;
        bannerBg.sprite = WarmRoundedSprite.Get(
            new Color(1f, 0.78f, 0.30f, full ? 0.22f : 0.16f),
            18,
            new Color(1f, 0.88f, 0.45f, full ? 0.55f : 0.38f),
            2f);

        CreateBannerText(bannerObj.transform, message, fontSize, FontStyle.Bold,
            new Color(1f, 0.95f, 0.62f, 1f));

        CanvasGroup cg = bannerObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        bannerObj.transform.localScale = Vector3.one * (full ? 0.6f : 0.75f);

        float inDur = full ? 0.28f : 0.24f;
        float hold = full ? 0.55f : 0.42f;
        float outDur = 0.22f;
        float elapsed = 0f;

        while (elapsed < inDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / inDur);
            cg.alpha = t;
            bannerObj.transform.localScale = Vector3.one * Mathf.Lerp(full ? 0.6f : 0.75f, full ? 1.08f : 1.02f, EaseOutBack(t));
            yield return null;
        }

        cg.alpha = 1f;
        bannerObj.transform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(hold);

        elapsed = 0f;
        while (elapsed < outDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / outDur);
            cg.alpha = 1f - t;
            bannerObj.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, t);
            yield return null;
        }

        Destroy(bannerObj);
    }

    void OnDestroy()
    {
        ClearConfetti();
    }

    IEnumerator AnimateBurst(Image fill, Image ring, Intensity intensity)
    {
        float mul = intensity == Intensity.Full ? 1f : 0.75f;
        float elapsed = 0f;
        while (elapsed < FlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FlashDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);

            if (fill != null)
            {
                Color c = FlashFillColor;
                c.a = FlashFillColor.a * pulse * mul;
                fill.color = c;
                fill.rectTransform.localScale = Vector3.one * (0.75f + pulse * 0.35f);
            }

            if (ring != null)
            {
                Color c = FlashRingColor;
                c.a = FlashRingColor.a * pulse * mul;
                ring.color = c;
                ring.rectTransform.localScale = Vector3.one * (0.95f + pulse * 0.28f);
            }

            yield return null;
        }

        if (burstRoot != null)
        {
            Destroy(burstRoot);
            burstRoot = null;
        }
    }

    IEnumerator UpdateConfetti()
    {
        while (confetti.Count > 0)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = confetti.Count - 1; i >= 0; i--)
            {
                ConfettiPiece piece = confetti[i];
                if (piece.rect == null)
                {
                    confetti.RemoveAt(i);
                    continue;
                }

                piece.life += dt;
                if (piece.life >= piece.maxLife)
                {
                    Destroy(piece.rect.gameObject);
                    confetti.RemoveAt(i);
                    continue;
                }

                piece.velocity.y -= 420f * dt;
                piece.rect.anchoredPosition += piece.velocity * dt;
                piece.rect.Rotate(0f, 0f, piece.spinDegPerSec * dt);

                float fade = 1f - Mathf.Clamp01((piece.life - piece.maxLife * 0.55f) / (piece.maxLife * 0.45f));
                if (piece.image != null)
                {
                    Color c = piece.image.color;
                    c.a = fade;
                    piece.image.color = c;
                }

                confetti[i] = piece;
            }

            yield return null;
        }

        confettiRoutine = null;
    }

    void ClearConfetti()
    {
        for (int i = 0; i < confetti.Count; i++)
        {
            if (confetti[i].rect != null)
            {
                Destroy(confetti[i].rect.gameObject);
            }
        }
        confetti.Clear();
    }

    static Image CreateBurstImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        StretchFull(obj.GetComponent<RectTransform>());
        return img;
    }

    static Text CreateBannerText(Transform parent, string content, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        StretchFull(text.rectTransform);
        return text;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
