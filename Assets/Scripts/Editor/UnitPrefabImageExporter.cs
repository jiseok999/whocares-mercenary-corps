#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 인게임 상점 미리보기와 동일한 방식으로 유닛 프리팹 전신 Idle 이미지를 PNG로 일괄 저장합니다.
/// </summary>
public static class UnitPrefabImageExporter
{
    const int PreviewLayer = 31;
    const int TextureSize = 256;
    const float DefaultCellSize = 0.75f;
    const int FirstUnitNumber = 1;
    const int LastUnitNumber = 28;

    [MenuItem("Tools/Export Unit Prefab Images")]
    static void ExportAll()
    {
        string outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ExportedUnitImages"));
        Directory.CreateDirectory(outputDir);

        int exported = 0;
        int skipped = 0;
        var messages = new List<string>();

        GameObject rootObj = new GameObject("UnitPrefabExportRoot");
        rootObj.hideFlags = HideFlags.HideAndDontSave;
        Transform instanceRoot = rootObj.transform;

        GameObject cameraObj = new GameObject("UnitPrefabExportCamera");
        cameraObj.hideFlags = HideFlags.HideAndDontSave;
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographic = true;
        camera.cullingMask = 1 << PreviewLayer;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        try
        {
            for (int unitNumber = FirstUnitNumber; unitNumber <= LastUnitNumber; unitNumber++)
            {
                string fileName = $"unit_{unitNumber:000}.png";
                string filePath = Path.Combine(outputDir, fileName);

                if (TryExportPrefabRender(unitNumber, filePath, instanceRoot, camera))
                {
                    exported++;
                    messages.Add($"{fileName} (prefab)");
                    continue;
                }

                if (TryExportStaticSprite(unitNumber, filePath))
                {
                    exported++;
                    messages.Add($"{fileName} (static sprite)");
                    continue;
                }

                skipped++;
                messages.Add($"unit_{unitNumber:000} — 추출 실패");
                Debug.LogWarning($"[UnitPrefabImageExporter] unit_{unitNumber:000} 이미지를 찾지 못했습니다.");
            }
        }
        finally
        {
            Object.DestroyImmediate(cameraObj);
            Object.DestroyImmediate(rootObj);
        }

        EditorUtility.DisplayDialog(
            "유닛 이미지 추출 완료",
            $"저장 위치:\n{outputDir}\n\n성공: {exported}개\n실패: {skipped}개",
            "확인");

        Debug.Log($"[UnitPrefabImageExporter] {exported}개 저장, {skipped}개 실패\n{string.Join("\n", messages)}");
    }

    static bool TryExportPrefabRender(int unitNumber, string filePath, Transform instanceRoot, Camera camera)
    {
        GameObject prefab = LoadUnitPrefab(unitNumber);
        if (prefab == null) return false;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, instanceRoot);
        if (instance == null) return false;

        try
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = GetPreviewLocalScale(unitNumber, DefaultCellSize);
            SetLayerRecursively(instance, PreviewLayer);
            PlayIdleAnimation(instance);

            if (!TryGetSpriteBounds(instance, out Bounds bounds))
            {
                return false;
            }

            RenderTexture renderTexture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            try
            {
                camera.orthographicSize = Mathf.Max(
                    0.01f,
                    ComputeOrthographicSize(bounds, TextureSize, TextureSize));
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                camera.targetTexture = renderTexture;
                camera.Render();
                camera.targetTexture = null;

                return SaveRenderTextureToPng(renderTexture, filePath);
            }
            finally
            {
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    static bool TryExportStaticSprite(int unitNumber, string filePath)
    {
        foreach (string path in GetStaticSpriteResourcePaths(unitNumber))
        {
            Sprite sprite = TryLoadFirstSprite(path);
            if (sprite == null) continue;

            Texture2D readable = CreateReadableSpriteTexture(sprite);
            if (readable == null) continue;

            try
            {
                byte[] png = readable.EncodeToPNG();
                if (png == null || png.Length == 0) return false;

                File.WriteAllBytes(filePath, png);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        return false;
    }

    static bool SaveRenderTextureToPng(RenderTexture renderTexture, string filePath)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;

        try
        {
            byte[] png = texture.EncodeToPNG();
            if (png == null || png.Length == 0) return false;

            File.WriteAllBytes(filePath, png);
            return true;
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    static Texture2D CreateReadableSpriteTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;

        Rect rect = sprite.textureRect;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        if (width <= 0 || height <= 0) return null;

        Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);

        try
        {
            Color[] pixels = sprite.texture.GetPixels(
                Mathf.RoundToInt(rect.x),
                Mathf.RoundToInt(rect.y),
                width,
                height);
            output.SetPixels(pixels);
            output.Apply();
            return output;
        }
        catch
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                sprite.texture.width,
                sprite.texture.height,
                0,
                RenderTextureFormat.ARGB32);

            Graphics.Blit(sprite.texture, renderTexture);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            Object.DestroyImmediate(output);
            return copy;
        }
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

    static IEnumerable<string> GetStaticSpriteResourcePaths(int unitNumber)
    {
        string padded = unitNumber.ToString("000");
        yield return $"unit_{padded}";
        yield return $"unit_{unitNumber}";

        if (unitNumber == 26) yield return "unit_026";

        yield return $"Unit_{padded}_idle";
        yield return $"unit_{padded}_idle";
    }

    static Sprite TryLoadFirstSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null) return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites == null || sprites.Length == 0) return null;

        Sprite best = null;
        float bestArea = 0f;
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite candidate = sprites[i];
            if (candidate == null) continue;

            float area = candidate.rect.width * candidate.rect.height;
            if (area > bestArea)
            {
                bestArea = area;
                best = candidate;
            }
        }

        return best;
    }

    static Vector3 GetPreviewLocalScale(int unitNumber, float cellSize)
    {
        float scale = cellSize;
        if (unitNumber != 2)
        {
            scale *= 1.5f;
        }

        return new Vector3(-scale, scale, scale);
    }

    static float ComputeOrthographicSize(Bounds bounds, int width, int height)
    {
        const float padding = 1.05f;
        float aspect = (float)width / Mathf.Max(1, height);
        float verticalSize = bounds.extents.y * padding;
        float horizontalSize = bounds.extents.x / aspect * padding;
        return Mathf.Max(verticalSize, horizontalSize);
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

    static bool TryGetSpriteBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        if (instance == null) return false;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.sprite == null) continue;

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
}
#endif
