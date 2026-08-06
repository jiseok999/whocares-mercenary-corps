using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image / RawImage 실루엣 하얀 호버 테두리.
/// </summary>
public static class UiSilhouetteOutline
{
    class OutlineState
    {
        public Graphic source;
        public Graphic outline;
    }

    const float ScaleMultiplier = 1.08f;
    const string OutlineSuffix = "_SilhouetteOutline";

    static readonly Dictionary<int, OutlineState> StatesByHost = new Dictionary<int, OutlineState>();
    static Material uiOutlineMaterial;

    public static void SetTarget(Transform host, Graphic sourceGraphic)
    {
        if (host == null)
        {
            return;
        }

        if (sourceGraphic == null)
        {
            Clear(host);
            return;
        }

        OutlineState state = GetOrCreateState(host);
        if (state.source == sourceGraphic && state.outline != null)
        {
            SyncState(state);
            state.outline.gameObject.SetActive(true);
            return;
        }

        DestroyOutline(state);
        state.source = sourceGraphic;
        state.outline = CreateOutlineGraphic(sourceGraphic);
        if (state.outline != null)
        {
            PlaceOutlineBehindSource(state.source.rectTransform, state.outline.rectTransform);
            state.outline.gameObject.SetActive(true);
        }
    }

    public static void Clear(Transform host)
    {
        if (host == null || !StatesByHost.TryGetValue(host.GetInstanceID(), out OutlineState state))
        {
            return;
        }

        DestroyOutline(state);
        state.source = null;
    }

    public static bool IsOutlineGraphic(Graphic graphic)
    {
        return graphic != null &&
               graphic.name.EndsWith(OutlineSuffix, StringComparison.Ordinal);
    }

    static OutlineState GetOrCreateState(Transform host)
    {
        int key = host.GetInstanceID();
        if (StatesByHost.TryGetValue(key, out OutlineState existing))
        {
            return existing;
        }

        OutlineState created = new OutlineState();
        StatesByHost[key] = created;
        return created;
    }

    static Graphic CreateOutlineGraphic(Graphic source)
    {
        Material material = GetUiOutlineMaterial();
        Transform parent = source.transform.parent;
        if (parent == null)
        {
            return null;
        }

        if (source is Image sourceImage)
        {
            GameObject obj = new GameObject(source.name + OutlineSuffix, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            CopyRectTransform(source.rectTransform, rect);
            ApplyOutlineScale(rect);

            Image outline = obj.AddComponent<Image>();
            outline.sprite = sourceImage.sprite;
            outline.type = sourceImage.type;
            outline.preserveAspect = sourceImage.preserveAspect;
            outline.fillCenter = sourceImage.fillCenter;
            outline.fillMethod = sourceImage.fillMethod;
            outline.fillOrigin = sourceImage.fillOrigin;
            outline.fillAmount = sourceImage.fillAmount;
            outline.fillClockwise = sourceImage.fillClockwise;
            outline.material = material;
            outline.color = Color.white;
            outline.raycastTarget = false;
            outline.maskable = sourceImage.maskable;
            return outline;
        }

        if (source is RawImage sourceRaw)
        {
            GameObject obj = new GameObject(source.name + OutlineSuffix, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            CopyRectTransform(source.rectTransform, rect);
            ApplyOutlineScale(rect);

            RawImage outline = obj.AddComponent<RawImage>();
            outline.texture = sourceRaw.texture;
            outline.uvRect = sourceRaw.uvRect;
            outline.material = material;
            outline.color = Color.white;
            outline.raycastTarget = false;
            outline.maskable = sourceRaw.maskable;
            return outline;
        }

        return null;
    }

    static void SyncState(OutlineState state)
    {
        if (state.source == null || state.outline == null)
        {
            return;
        }

        Material material = GetUiOutlineMaterial();
        CopyRectTransform(state.source.rectTransform, state.outline.rectTransform);
        ApplyOutlineScale(state.outline.rectTransform);
        state.outline.color = Color.white;
        state.outline.material = material;

        if (state.source is Image sourceImage && state.outline is Image outlineImage)
        {
            outlineImage.sprite = sourceImage.sprite;
            outlineImage.type = sourceImage.type;
            outlineImage.preserveAspect = sourceImage.preserveAspect;
        }
        else if (state.source is RawImage sourceRaw && state.outline is RawImage outlineRaw)
        {
            outlineRaw.texture = sourceRaw.texture;
            outlineRaw.uvRect = sourceRaw.uvRect;
        }

        PlaceOutlineBehindSource(state.source.rectTransform, state.outline.rectTransform);
    }

    static Material GetUiOutlineMaterial()
    {
        if (uiOutlineMaterial != null)
        {
            return uiOutlineMaterial;
        }

        Shader shader = Shader.Find("Custom/UISolidSilhouette");
        if (shader == null)
        {
            shader = Shader.Find("Custom/SpriteSolidSilhouette");
        }
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        uiOutlineMaterial = new Material(shader);
        uiOutlineMaterial.color = Color.white;
        uiOutlineMaterial.name = "UISolidSilhouetteOutline";
        return uiOutlineMaterial;
    }

    static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    static void ApplyOutlineScale(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        Vector3 scale = rect.localScale;
        rect.localScale = new Vector3(scale.x * ScaleMultiplier, scale.y * ScaleMultiplier, scale.z);
    }

    static void PlaceOutlineBehindSource(RectTransform source, RectTransform outline)
    {
        if (source == null || outline == null || source.parent != outline.parent)
        {
            return;
        }

        int sourceIndex = source.GetSiblingIndex();
        outline.SetSiblingIndex(sourceIndex);
        source.SetSiblingIndex(sourceIndex + 1);
    }

    static void DestroyOutline(OutlineState state)
    {
        if (state.outline != null)
        {
            UnityEngine.Object.Destroy(state.outline.gameObject);
            state.outline = null;
        }
    }
}
