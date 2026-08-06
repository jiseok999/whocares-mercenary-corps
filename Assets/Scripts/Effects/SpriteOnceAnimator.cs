using UnityEngine;

public class SpriteOnceAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames = System.Array.Empty<Sprite>();
    private int maxFrame = 0;
    private float frameTime = 0.06f;
    private float timer;
    private int frameIndex;

    public void Initialize(SpriteRenderer renderer, Sprite[] sprites, int maxFrameIndex, float frameInterval)
    {
        spriteRenderer = renderer;
        frames = sprites ?? System.Array.Empty<Sprite>();
        maxFrame = Mathf.Clamp(maxFrameIndex, 0, frames.Length - 1);
        frameTime = Mathf.Max(0.01f, frameInterval);
        frameIndex = 0;
        timer = 0f;

        if (spriteRenderer != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    void Update()
    {
        if (spriteRenderer == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        if (timer < frameTime)
        {
            return;
        }

        timer -= frameTime;
        frameIndex++;
        if (frameIndex > maxFrame)
        {
            Destroy(gameObject);
            return;
        }

        spriteRenderer.sprite = frames[frameIndex];
    }
}
