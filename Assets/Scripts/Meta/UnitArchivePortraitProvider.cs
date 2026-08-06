using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 용병단 아카이브 — 실제 배치 프리팹(Units/unit_XXX)에서 목 위 초상 생성.
/// </summary>
public static class UnitArchivePortraitProvider
{
    const int PreviewLayer = 31;
    const int TextureSize = 128;
    const float DefaultCellSize = 0.75f;

    static readonly Dictionary<int, Sprite> CachedSprites = new Dictionary<int, Sprite>();

    static Transform previewRoot;
    static Transform previewInstanceRoot;
    static Camera previewCamera;

    public static Sprite GetPortraitSprite(int unitNumber)
    {
        if (unitNumber < 1 || unitNumber > 28) return null;
        if (CachedSprites.TryGetValue(unitNumber, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite sprite = RenderHeadPortraitFromPrefab(unitNumber);
        if (sprite != null)
        {
            CachedSprites[unitNumber] = sprite;
        }

        return sprite;
    }

    public static void ClearCache()
    {
        foreach (KeyValuePair<int, Sprite> pair in CachedSprites)
        {
            if (pair.Value == null) continue;
            if (pair.Value.texture != null)
            {
                Object.Destroy(pair.Value.texture);
            }
            Object.Destroy(pair.Value);
        }

        CachedSprites.Clear();
        ClearPreviewInstances();
    }

    static Sprite RenderHeadPortraitFromPrefab(int unitNumber)
    {
        GameObject prefab = LoadUnitPrefab(unitNumber);
        if (prefab == null) return null;

        EnsurePreviewInfrastructure();
        ClearPreviewInstances();

        GameObject instance = Object.Instantiate(prefab, previewInstanceRoot);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = GetPreviewLocalScale(unitNumber);
        SetLayerRecursively(instance, PreviewLayer);
        PlayIdleAnimation(instance);

        Bounds bounds;
        if (!TryCaptureHeadBounds(instance, out bounds))
        {
            Object.DestroyImmediate(instance);
            return null;
        }

        RenderTexture renderTexture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        previewCamera.orthographicSize = Mathf.Max(
            0.01f,
            ComputeOrthographicSize(bounds, TextureSize, TextureSize));
        previewCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        previewCamera.targetTexture = renderTexture;
        previewCamera.Render();
        previewCamera.targetTexture = null;

        Sprite sprite = CreateSpriteFromRenderTexture(renderTexture);
        renderTexture.Release();
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(instance);
        return sprite;
    }

    static Sprite CreateSpriteFromRenderTexture(RenderTexture renderTexture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    static GameObject LoadUnitPrefab(int unitNumber)
    {
        foreach (string path in GetUnitPrefabResourcePaths(unitNumber))
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null) return prefab;
        }

        return null;
    }

    static IEnumerable<string> GetUnitPrefabResourcePaths(int unitNumber)
    {
        string padded = unitNumber.ToString("000");
        yield return $"Units/unit_{padded}";
        yield return $"Units/unit_{unitNumber}";

        if (unitNumber == 2) yield return "Addons/RetroHeroes/2_Prefab/unit_002";
        if (unitNumber == 3) yield return "Addons/RetroHeroes/2_Prefab/unit_003";
    }

    static void EnsurePreviewInfrastructure()
    {
        if (previewCamera != null) return;

        GameObject rootObj = new GameObject("ArchivePortraitPreviewRoot");
        Object.DontDestroyOnLoad(rootObj);
        previewRoot = rootObj.transform;

        GameObject instanceRootObj = new GameObject("Instances");
        instanceRootObj.transform.SetParent(previewRoot, false);
        previewInstanceRoot = instanceRootObj.transform;

        GameObject cameraObj = new GameObject("ArchivePortraitPreviewCamera");
        cameraObj.transform.SetParent(previewRoot, false);
        previewCamera = cameraObj.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.orthographic = true;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 100f;
    }

    static void ClearPreviewInstances()
    {
        if (previewInstanceRoot == null) return;

        for (int i = previewInstanceRoot.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(previewInstanceRoot.GetChild(i).gameObject);
        }
    }

    static Vector3 GetPreviewLocalScale(int unitNumber)
    {
        float scale = DefaultCellSize;
        if (unitNumber != 2)
        {
            scale *= 1.5f;
        }

        return new Vector3(-scale, scale, scale);
    }

    static float ComputeOrthographicSize(Bounds bounds, int width, int height)
    {
        float padding = 1.08f;
        float aspect = (float)width / Mathf.Max(1, height);
        float verticalSize = bounds.extents.y * padding;
        float horizontalSize = bounds.extents.x / aspect * padding;
        return Mathf.Max(verticalSize, horizontalSize);
    }

    static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null) return;
        target.layer = layer;
        Transform root = target.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    static void PlayIdleAnimation(GameObject root)
    {
        if (root == null) return;

        SPUM_Prefabs[] spumPrefabs = root.GetComponentsInChildren<SPUM_Prefabs>(true);
        for (int i = 0; i < spumPrefabs.Length; i++)
        {
            SPUM_Prefabs spum = spumPrefabs[i];
            if (spum == null) continue;

            spum.OverrideControllerInit();
            spum.PopulateAnimationLists();
            if (spum.IDLE_List != null && spum.IDLE_List.Count > 0)
            {
                spum.PlayAnimation(PlayerState.IDLE, 0);
            }
            else if (spum._anim != null)
            {
                spum._anim.Play("Idle", 0, 0f);
                spum._anim.Update(0f);
            }
        }

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null) continue;
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }
    }

    static bool TryCaptureHeadBounds(GameObject instance, out Bounds bounds)
    {
        ApplyHeadOnlyVisibility(instance);
        if (TryGetEnabledSpriteBounds(instance, out bounds))
        {
            TryGetNeckUpHeadPreviewBounds(bounds, out bounds);
            return true;
        }

        EnableAllBodySprites(instance);
        if (!TryGetEnabledSpriteBounds(instance, out Bounds fullBounds))
        {
            bounds = default;
            return false;
        }

        TryGetNeckUpHeadPreviewBounds(fullBounds, out bounds);
        return true;
    }

    static void EnableAllBodySprites(GameObject instance)
    {
        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.sprite == null) continue;

            string spriteName = renderer.sprite.name.ToLowerInvariant();
            if (spriteName.Contains("shadow")) continue;
            renderer.enabled = true;
        }
    }

    static void ApplyHeadOnlyVisibility(GameObject instance)
    {
        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null) continue;
            renderer.enabled = ShouldShowHeadPreview(renderer);
        }
    }

    static bool TryGetEnabledSpriteBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || renderer.sprite == null) continue;

            string spriteName = renderer.sprite.name.ToLowerInvariant();
            if (spriteName.Contains("shadow")) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
    }

    static bool TryGetNeckUpHeadPreviewBounds(Bounds fullBounds, out Bounds bounds)
    {
        float headHeight = fullBounds.size.y * 0.38f;
        float centerY = fullBounds.max.y - headHeight * 0.5f;
        bounds = new Bounds(
            new Vector3(fullBounds.center.x, centerY, fullBounds.center.z),
            new Vector3(fullBounds.size.x * 0.82f, headHeight, fullBounds.size.z));
        return true;
    }

    static bool ShouldShowHeadPreview(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null) return false;

        string spriteName = renderer.sprite.name.ToLowerInvariant();
        if (spriteName.Contains("shadow")) return false;

        if (IsUnderTransformNamed(renderer.transform, "P_Head"))
        {
            return true;
        }

        Transform node = renderer.transform;
        while (node != null)
        {
            if (IsHeadSlotName(node.name))
            {
                return true;
            }

            node = node.parent;
        }

        node = renderer.transform;
        while (node != null)
        {
            if (IsNonHeadBranchName(node.name))
            {
                return false;
            }

            node = node.parent;
        }

        return false;
    }

    static bool IsUnderTransformNamed(Transform node, string targetName)
    {
        while (node != null)
        {
            if (node.name == targetName) return true;
            node = node.parent;
        }

        return false;
    }

    static bool IsNonHeadBranchName(string transformName)
    {
        if (string.IsNullOrEmpty(transformName) || transformName == "P_Head") return false;
        if (transformName.Contains("Weapon") || transformName.Contains("Shield")) return true;
        if (transformName.Contains("Foot") || transformName.Contains("Shoulder") || transformName.Contains("Back")) return true;
        if (transformName == "P_Body" || transformName.Contains("BodySet") ||
            transformName.Contains("ArmorBody") || transformName.Contains("ClothBody")) return true;
        if (transformName.Contains("Cloth") &&
            transformName.IndexOf("Face", System.StringComparison.OrdinalIgnoreCase) < 0) return true;
        if (transformName.Contains("Close") || transformName.Contains("CArm")) return true;
        if (transformName.Contains("Arm") &&
            transformName.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) < 0) return true;
        return false;
    }

    static bool IsHeadSlotName(string transformName)
    {
        if (string.IsNullOrEmpty(transformName)) return false;

        switch (transformName)
        {
            case "P_Hair":
            case "P_Helmet":
            case "P_Eye":
            case "P_LEye":
            case "P_REye":
            case "P_Mustache":
                return true;
        }

        return transformName.IndexOf("Face", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
