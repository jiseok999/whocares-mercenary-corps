using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체 비행 경로에 스프라이트 잔상을 남깁니다.
/// </summary>
public class Unit1ProjectileAfterimage : MonoBehaviour
{
    public float spawnDistance = 0.14f;
    public float ghostLifetime = 0.2f;
    public float startAlpha = 0.22f;

    private SpriteRenderer sourceRenderer;
    private Transform visualTransform;
    private Vector3 lastSpawnPosition;
    private readonly List<GameObject> activeGhosts = new List<GameObject>();

    public static Unit1ProjectileAfterimage Attach(GameObject host)
    {
        if (host == null)
        {
            return null;
        }

        Unit1ProjectileAfterimage existing = host.GetComponent<Unit1ProjectileAfterimage>();
        if (existing != null)
        {
            return existing;
        }

        return host.AddComponent<Unit1ProjectileAfterimage>();
    }

    void Awake()
    {
        ResolveVisual();
    }

    void Start()
    {
        if (sourceRenderer == null)
        {
            ResolveVisual();
        }

        if (visualTransform != null)
        {
            lastSpawnPosition = visualTransform.position;
        }
    }

    void ResolveVisual()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        visualTransform = sourceRenderer != null ? sourceRenderer.transform : transform;
    }

    void Update()
    {
        if (sourceRenderer == null || sourceRenderer.sprite == null || visualTransform == null)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        PruneDestroyedGhosts();

        float step = Mathf.Max(0.05f, spawnDistance);
        while (Vector2.Distance(lastSpawnPosition, visualTransform.position) >= step)
        {
            Vector3 toward = (visualTransform.position - lastSpawnPosition).normalized * step;
            lastSpawnPosition += toward;
            SpawnGhost(lastSpawnPosition);
        }
    }

    void OnDestroy()
    {
        ClearAllGhosts();
    }

    void PruneDestroyedGhosts()
    {
        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            if (activeGhosts[i] == null)
            {
                activeGhosts.RemoveAt(i);
            }
        }
    }

    void ClearAllGhosts()
    {
        for (int i = 0; i < activeGhosts.Count; i++)
        {
            if (activeGhosts[i] != null)
            {
                Destroy(activeGhosts[i]);
            }
        }
        activeGhosts.Clear();
    }

    void SpawnGhost(Vector3 position)
    {
        GameObject ghostObj = new GameObject("Unit1Afterimage");
        ghostObj.transform.position = position;
        ghostObj.transform.rotation = visualTransform.rotation;
        ghostObj.transform.localScale = visualTransform.lossyScale;

        SpriteRenderer ghostRenderer = ghostObj.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = sourceRenderer.sprite;
        ghostRenderer.flipX = sourceRenderer.flipX;
        ghostRenderer.flipY = sourceRenderer.flipY;
        ghostRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;

        Color c = sourceRenderer.color;
        c.a = startAlpha;
        ghostRenderer.color = c;

        Unit1AfterimageFade fade = ghostObj.AddComponent<Unit1AfterimageFade>();
        fade.lifetime = ghostLifetime;
        fade.startAlpha = startAlpha;

        activeGhosts.Add(ghostObj);
    }
}

/// <summary>
/// 잔상 스프라이트를 서서히 페이드아웃합니다.
/// </summary>
public class Unit1AfterimageFade : MonoBehaviour
{
    public float lifetime = 0.2f;
    public float startAlpha = 0.22f;

    private SpriteRenderer spriteRenderer;
    private float elapsed;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float t = lifetime > 0f ? Mathf.Clamp01(elapsed / lifetime) : 1f;

        Color c = spriteRenderer.color;
        c.a = Mathf.Lerp(startAlpha, 0f, t);
        spriteRenderer.color = c;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
