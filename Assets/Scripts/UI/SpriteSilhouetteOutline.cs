using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스프라이트 실루엣을 따라가는 하얀 호버 테두리(배경 확대 방식).
/// </summary>
public static class SpriteSilhouetteOutline
{
    class OutlinePair
    {
        public SpriteRenderer source;
        public SpriteRenderer outline;
    }

    class OutlineState
    {
        public Transform target;
        public Transform root;
        public Color outlineColor = Color.white;
        public readonly List<OutlinePair> pairs = new List<OutlinePair>(12);
    }

    const float ScaleMultiplier = 1.08f;
    const int SortOrderOffset = -1;
    const string OutlineSuffix = "_SilhouetteOutline";

    static Material outlineMaterial;

    static readonly Dictionary<int, OutlineState> StatesByHost = new Dictionary<int, OutlineState>();

    public static void SetTarget(Transform host, Transform target, Func<SpriteRenderer, bool> includeSource)
    {
        SetTarget(host, target, includeSource, Color.white);
    }

    public static void SetTarget(Transform host, Transform target, Func<SpriteRenderer, bool> includeSource, Color outlineColor)
    {
        if (host == null)
        {
            return;
        }

        OutlineState state = GetOrCreateState(host);
        state.outlineColor = outlineColor;
        if (target == null)
        {
            Clear(host);
            return;
        }

        if (state.target == target && state.pairs.Count > 0)
        {
            Sync(host);
            if (state.root != null)
            {
                state.root.gameObject.SetActive(true);
            }
            return;
        }

        Rebuild(state, target, includeSource);
        Sync(host);
        if (state.root != null)
        {
            state.root.gameObject.SetActive(true);
        }
    }

    public static void Sync(Transform host)
    {
        if (host == null || !StatesByHost.TryGetValue(host.GetInstanceID(), out OutlineState state))
        {
            return;
        }

        if (state.target == null || state.root == null)
        {
            return;
        }

        for (int i = state.pairs.Count - 1; i >= 0; i--)
        {
            OutlinePair pair = state.pairs[i];
            if (pair.source == null || pair.outline == null)
            {
                state.pairs.RemoveAt(i);
                continue;
            }

            SpriteRenderer source = pair.source;
            SpriteRenderer outline = pair.outline;
            bool visible = source.enabled &&
                           source.gameObject.activeInHierarchy &&
                           source.sprite != null &&
                           source.color.a > 0.04f;

            outline.enabled = visible;
            if (!visible)
            {
                continue;
            }

            outline.sprite = source.sprite;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder + SortOrderOffset;
            outline.sharedMaterial = GetOutlineMaterial();
            outline.color = state.outlineColor;
            outline.maskInteraction = source.maskInteraction;
            outline.drawMode = source.drawMode;
            outline.size = source.size;
            outline.tileMode = source.tileMode;

            CopyLocalTransform(source.transform, outline.transform, ScaleMultiplier);
            PlaceOutlineBehindSource(source.transform, outline.transform);
        }
    }

    public static void Clear(Transform host)
    {
        if (host == null)
        {
            return;
        }

        if (!StatesByHost.TryGetValue(host.GetInstanceID(), out OutlineState state))
        {
            return;
        }

        DestroyPairs(state);
        state.target = null;
        if (state.root != null)
        {
            state.root.gameObject.SetActive(false);
            state.root.SetParent(host, false);
        }
    }

    public static bool IsOutlineRenderer(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        Transform t = renderer.transform;
        while (t != null)
        {
            if (t.name.EndsWith(OutlineSuffix, StringComparison.Ordinal))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }

    static OutlineState GetOrCreateState(Transform host)
    {
        int key = host.GetInstanceID();
        if (StatesByHost.TryGetValue(key, out OutlineState existing))
        {
            return existing;
        }

        GameObject rootObj = new GameObject("SilhouetteOutlineRoot");
        rootObj.transform.SetParent(host, false);
        rootObj.transform.localPosition = Vector3.zero;
        rootObj.transform.localRotation = Quaternion.identity;
        rootObj.transform.localScale = Vector3.one;
        rootObj.SetActive(false);

        OutlineState created = new OutlineState { root = rootObj.transform };
        StatesByHost[key] = created;
        return created;
    }

    static void Rebuild(OutlineState state, Transform target, Func<SpriteRenderer, bool> includeSource)
    {
        DestroyPairs(state);
        state.target = target;
        state.root.SetParent(target, false);
        state.root.localPosition = Vector3.zero;
        state.root.localRotation = Quaternion.identity;
        state.root.localScale = Vector3.one;

        SpriteRenderer[] sources = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            SpriteRenderer source = sources[i];
            if (source == null || !includeSource(source))
            {
                continue;
            }

            GameObject outlineObj = new GameObject(source.name + OutlineSuffix);
            outlineObj.transform.SetParent(source.transform.parent, false);

            SpriteRenderer outline = outlineObj.AddComponent<SpriteRenderer>();
            outline.sprite = source.sprite;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder + SortOrderOffset;
            outline.sharedMaterial = GetOutlineMaterial();
            outline.color = state.outlineColor;
            outline.maskInteraction = source.maskInteraction;
            outline.drawMode = source.drawMode;
            outline.size = source.size;
            outline.tileMode = source.tileMode;

            CopyLocalTransform(source.transform, outline.transform, ScaleMultiplier);
            PlaceOutlineBehindSource(source.transform, outline.transform);

            state.pairs.Add(new OutlinePair { source = source, outline = outline });
        }
    }

    static Material GetOutlineMaterial()
    {
        return GetSharedOutlineMaterial();
    }

    public static Material GetSharedOutlineMaterial()
    {
        if (outlineMaterial != null)
        {
            return outlineMaterial;
        }

        Shader shader = Shader.Find("Custom/SpriteSolidSilhouette");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        outlineMaterial = new Material(shader);
        outlineMaterial.color = Color.white;
        outlineMaterial.name = "SpriteSolidSilhouetteOutline";
        return outlineMaterial;
    }

    static void DestroyPairs(OutlineState state)
    {
        for (int i = 0; i < state.pairs.Count; i++)
        {
            OutlinePair pair = state.pairs[i];
            if (pair.outline != null)
            {
                UnityEngine.Object.Destroy(pair.outline.gameObject);
            }
        }

        state.pairs.Clear();
    }

    static void CopyLocalTransform(Transform source, Transform outline, float scaleMultiplier)
    {
        outline.localPosition = source.localPosition;
        outline.localRotation = source.localRotation;

        Vector3 sourceScale = source.localScale;
        outline.localScale = new Vector3(
            SignOrOne(sourceScale.x) * Mathf.Abs(sourceScale.x) * scaleMultiplier,
            SignOrOne(sourceScale.y) * Mathf.Abs(sourceScale.y) * scaleMultiplier,
            sourceScale.z);
    }

    static float SignOrOne(float value)
    {
        if (Mathf.Approximately(value, 0f))
        {
            return 1f;
        }

        return Mathf.Sign(value);
    }

    static void PlaceOutlineBehindSource(Transform source, Transform outline)
    {
        if (source == null || outline == null || source.parent != outline.parent)
        {
            return;
        }

        int sourceIndex = source.GetSiblingIndex();
        outline.SetSiblingIndex(sourceIndex);
        source.SetSiblingIndex(sourceIndex + 1);
    }
}
