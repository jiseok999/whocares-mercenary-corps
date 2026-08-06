using System;
using UnityEngine;

/// <summary>
/// 간단한 스프라이트 프레임 애니메이션
/// </summary>
public class UnitSpriteAnimator : MonoBehaviour
{
    [SerializeField] private string resourceName;
    [SerializeField] private float framesPerSecond = 6f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float timer;
    private int frameIndex;
    private bool isAnimating = true;
    private bool anchorToBottom = false;
    private Vector3 anchorLocalPosition;
    private bool hasAnchorPosition = false;
    private float baselineMinY;
    private int maxFrameCount = -1;

    public void Initialize(SpriteRenderer renderer, string spriteResourceName, float fps, int useFirstFrameCount = -1)
    {
        spriteRenderer = renderer;
        resourceName = spriteResourceName;
        framesPerSecond = Mathf.Max(1f, fps);
        maxFrameCount = useFirstFrameCount;
        LoadFrames();
    }

    public void EnableBottomAnchor(bool enable)
    {
        anchorToBottom = enable;
        if (spriteRenderer == null) return;

        if (anchorToBottom)
        {
            anchorLocalPosition = spriteRenderer.transform.localPosition;
            hasAnchorPosition = true;
            if (frames != null && frames.Length > 0)
            {
                if (frameIndex >= frames.Length)
                {
                    frameIndex = 0;
                }
                baselineMinY = GetSpriteMinY(frames[0]);
                ApplyBottomAnchor(frames[frameIndex]);
            }
            else
            {
                baselineMinY = GetSpriteMinY(spriteRenderer.sprite);
                ApplyBottomAnchor(spriteRenderer.sprite);
            }
        }
        else
        {
            if (hasAnchorPosition)
            {
                spriteRenderer.transform.localPosition = anchorLocalPosition;
            }
        }
    }

    public void SetAnchorPosition(Vector3 localPosition)
    {
        anchorLocalPosition = localPosition;
        hasAnchorPosition = true;

        if (anchorToBottom && spriteRenderer != null)
        {
            ApplyBottomAnchorInternal(spriteRenderer.sprite);
        }
    }

    public void ApplyBottomAnchor(Sprite sprite)
    {
        ApplyBottomAnchorInternal(sprite);
    }

    public void SetAnimating(bool enable)
    {
        isAnimating = enable;
    }

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (frames == null || frames.Length == 0)
        {
            LoadFrames();
        }
    }

    void Update()
    {
        if (!isAnimating || frames == null || frames.Length == 0 || spriteRenderer == null)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        timer += Time.deltaTime;
        float frameTime = 1f / framesPerSecond;
        if (timer >= frameTime)
        {
            timer -= frameTime;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
            ApplyBottomAnchorInternal(spriteRenderer.sprite);
        }
    }

    void LoadFrames()
    {
        if (string.IsNullOrEmpty(resourceName)) return;

        Sprite[] loaded = Resources.LoadAll<Sprite>(resourceName);
        if (loaded == null || loaded.Length == 0)
        {
            frames = Array.Empty<Sprite>();
            return;
        }

        Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
        if (maxFrameCount > 0 && loaded.Length > maxFrameCount)
        {
            var sliced = new Sprite[maxFrameCount];
            Array.Copy(loaded, 0, sliced, 0, maxFrameCount);
            frames = sliced;
        }
        else
        {
            frames = loaded;
        }

        if (spriteRenderer != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
            frameIndex = 0;
            if (anchorToBottom)
            {
                anchorLocalPosition = spriteRenderer.transform.localPosition;
                hasAnchorPosition = true;
                baselineMinY = GetSpriteMinY(frames[0]);
                ApplyBottomAnchorInternal(frames[0]);
            }
        }
    }

    float GetSpriteMinY(Sprite sprite)
    {
        if (sprite == null) return 0f;
        return sprite.bounds.min.y;
    }

    void ApplyBottomAnchorInternal(Sprite sprite)
    {
        if (!anchorToBottom || spriteRenderer == null || sprite == null) return;

        float currentMinY = GetSpriteMinY(sprite);
        float delta = baselineMinY - currentMinY;
        Vector3 pos = hasAnchorPosition ? anchorLocalPosition : spriteRenderer.transform.localPosition;
        pos.y += delta;
        spriteRenderer.transform.localPosition = pos;
    }
}

