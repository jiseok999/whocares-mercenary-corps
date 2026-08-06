using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 벤치·보드 유닛 호버 시 표시하는 필드 툴팁 (상점 툴팁과 동일한 검은 배경 스타일)
/// </summary>
public class UnitFieldTooltip : MonoBehaviour
{
    static UnitFieldTooltip instance;

    GameObject tooltipRoot;
    RectTransform tooltipRect;
    Canvas overlayCanvas;
    Text headerText;
    Text patternText;
    RectTransform headerRect;
    RectTransform patternRect;
    RectTransform divRect1;
    RectTransform divRect2;
    Text traitsTitleText;
    RectTransform traitsTitleRect;
    readonly Text[] statTexts = new Text[3];
    readonly RectTransform[] statRowRects = new RectTransform[3];
    Text permanentText;
    RectTransform permanentRect;
    readonly Text[] traitTexts = new Text[2];
    readonly Image[] traitIcons = new Image[2];
    readonly RectTransform[] traitRowRects = new RectTransform[2];

    const int MaxTraitRows = 2;

    const float TooltipWidth = 300f;
    const float PadX = 16f;
    const float PadTop = 13f;
    const float PadBottom = 14f;
    const float SectionGap = 8f;
    const float RowHeight = 26f;
    const float RowGap = 5f;
    const float DividerHeight = 2f;
    static readonly StatIconFactory.IconType[] StatIconTypes =
    {
        StatIconFactory.IconType.Damage,
        StatIconFactory.IconType.AttackSpeed,
        StatIconFactory.IconType.Range
    };

    GameObject outlineHost;
    Character outlineTarget;
    string[] layoutTraits = System.Array.Empty<string>();

    public static void EnsureExists()
    {
        if (instance != null) return;

        GameObject host = new GameObject("UnitFieldTooltip");
        instance = host.AddComponent<UnitFieldTooltip>();
        UnitAttackRangePreview.EnsureExists();
    }

    public static void Hide()
    {
        if (instance != null)
        {
            instance.HideAll();
        }
        else
        {
            UnitAttackRangePreview.Hide();
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void LateUpdate()
    {
        if (ShouldSuppressFieldTooltip())
        {
            HideAll();
            return;
        }

        if (!TryGetPointerWorldPosition(out Vector3 pointerWorld))
        {
            HideAll();
            return;
        }

        Character target = FindHoverTarget(pointerWorld);
        if (target == null)
        {
            HideAll();
            return;
        }

        ShowInternal(target);
        ShowOutline(target);
        UnitAttackRangePreview.ShowFor(target);
    }

    void HideAll()
    {
        HideInternal();
        HideOutline();
        UnitAttackRangePreview.Hide();
    }

    static bool IsAnyCharacterDragging()
    {
        return Character.IsDragInProgress;
    }

    static bool ShouldSuppressFieldTooltip()
    {
        if (IsAnyCharacterDragging()) return true;
        if (SkillManager.Instance != null && SkillManager.Instance.IsSkillArmed) return true;

        // 보스 보상 등 모달 상점 UI 중에만 숨김. 대기시간·첫 배치 중에는 유닛 정보 표시 허용.
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.IsShopOpen())
        {
            return true;
        }

        return IsPointerOverBlockingGameplayPanels();
    }

    /// <summary>
    /// 상점·조합 등 게임 필드를 가리는 UI 위에 있을 때만 툴팁을 숨깁니다.
    /// (상점 패널이 상시 노출이므로 IsShopOpen/IsPointerOverUi 전역 차단은 사용하지 않음)
    /// </summary>
    static bool IsPointerOverBlockingGameplayPanels()
    {
        Vector2 screenPos = TooltipPointerHelper.GetScreenPosition();

        if (ShopManager.Instance != null && ShopManager.Instance.shopPanel != null)
        {
            RectTransform shopRect = ShopManager.Instance.shopPanel.GetComponent<RectTransform>();
            if (shopRect != null && shopRect.gameObject.activeInHierarchy &&
                TooltipPointerHelper.IsScreenPointInsideRect(shopRect, screenPos))
            {
                return true;
            }
        }

        if (TraitManager.Instance != null && TraitManager.Instance.IsScreenPointOverPanel(screenPos))
        {
            return true;
        }

        return false;
    }

    static Character FindHoverTarget(Vector3 pointerWorld)
    {
        Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character best = null;
        float bestDistance = float.MaxValue;

        foreach (Character character in characters)
        {
            if (character == null || !character.IsOnBenchOrBoard()) continue;
            if (!character.ContainsWorldPoint(pointerWorld)) continue;

            float distance = Vector2.Distance(character.transform.position, pointerWorld);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = character;
            }
        }

        return best;
    }

    static bool TryGetPointerWorldPosition(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector2 screenPos = Vector2.zero;
        bool hasPointer = false;

#if ENABLE_INPUT_SYSTEM
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if (touch != null)
        {
            screenPos = touch.primaryTouch.position.ReadValue();
            hasPointer = true;
        }

        if (!hasPointer)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                hasPointer = true;
            }
        }
#endif

        if (!hasPointer)
        {
            screenPos = Input.mousePosition;
            hasPointer = true;
        }

        Vector3 pos = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        worldPos = cam.ScreenToWorldPoint(pos);
        worldPos.z = 0f;
        return true;
    }

    void EnsureTooltipUi()
    {
        if (tooltipRoot != null) return;

        overlayCanvas = FindRootOverlayCanvas();
        Transform parent = overlayCanvas != null ? overlayCanvas.transform : transform;

        tooltipRoot = new GameObject("FieldUnitTooltip");
        tooltipRoot.transform.SetParent(parent, false);

        Canvas tooltipCanvas = tooltipRoot.AddComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 30;
        tooltipRoot.AddComponent<GraphicRaycaster>();

        Image bg = tooltipRoot.AddComponent<Image>();
        TooltipTheme.ApplyStandardBackground(bg, true);

        tooltipRect = tooltipRoot.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(TooltipWidth, 200f); // 높이는 LayoutTooltip에서 자동 계산
        tooltipRect.pivot = new Vector2(0f, 0f);

        // 헤더 (유닛명 + 등급/진화)
        headerText = CreateTooltipText("Header", 20, FontStyle.Normal);
        headerRect = headerText.GetComponent<RectTransform>();
        SetupTopAnchored(headerRect, tooltipRoot.transform);

        divRect1 = CreateTooltipDivider(tooltipRoot.transform);

        // 스탯 행 (아이콘 + 리치 텍스트)
        for (int i = 0; i < statTexts.Length; i++)
        {
            GameObject row = new GameObject("StatRow_" + i, typeof(RectTransform));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            SetupTopAnchored(rowRect, tooltipRoot.transform);
            statRowRects[i] = rowRect;

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.sprite = StatIconFactory.Get(StatIconTypes[i]);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            iconRect.sizeDelta = new Vector2(22f, 22f);

            Text valueText = CreateTooltipText("Value", 18, FontStyle.Normal);
            valueText.transform.SetParent(row.transform, false);
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = new Vector2(30f, 0f);
            valueRect.offsetMax = new Vector2(0f, 0f);
            statTexts[i] = valueText;
        }

        permanentText = CreateTooltipText("Permanent", 17, FontStyle.Normal);
        permanentRect = permanentText.GetComponent<RectTransform>();
        SetupTopAnchored(permanentRect, tooltipRoot.transform);

        divRect2 = CreateTooltipDivider(tooltipRoot.transform);

        traitsTitleText = CreateTooltipText("TraitsTitle", 16, FontStyle.Normal);
        traitsTitleRect = traitsTitleText.GetComponent<RectTransform>();
        SetupTopAnchored(traitsTitleRect, tooltipRoot.transform);

        for (int i = 0; i < MaxTraitRows; i++)
        {
            GameObject row = new GameObject("TraitRow_" + i, typeof(RectTransform));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            SetupTopAnchored(rowRect, tooltipRoot.transform);
            traitRowRects[i] = rowRect;

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            traitIcons[i] = icon;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(24f, 24f);

            Text valueText = CreateTooltipText("Name", 18, FontStyle.Normal);
            valueText.transform.SetParent(row.transform, false);
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = new Vector2(32f, 0f);
            valueRect.offsetMax = Vector2.zero;
            traitTexts[i] = valueText;
        }

        // 패턴 설명
        patternText = CreateTooltipText("Pattern", 16, FontStyle.Normal);
        patternRect = patternText.GetComponent<RectTransform>();
        SetupTopAnchored(patternRect, tooltipRoot.transform);

        tooltipRoot.SetActive(false);
    }

    /// <summary>자식을 좌상단(0,1) 기준으로 앵커링합니다. 너비/높이는 LayoutTooltip에서 지정합니다.</summary>
    void SetupTopAnchored(RectTransform rt, Transform parent)
    {
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
    }

    /// <summary>내용 길이에 맞춰 각 섹션을 위에서부터 배치하고 전체 높이를 계산합니다.</summary>
    void LayoutTooltip()
    {
        float contentW = TooltipWidth - PadX * 2f;
        float y = PadTop;

        // 헤더
        headerRect.sizeDelta = new Vector2(contentW, headerRect.sizeDelta.y);
        float headerH = Mathf.Max(headerText.preferredHeight, 24f);
        PlaceSection(headerRect, contentW, headerH, y);
        y += headerH + SectionGap;

        // 구분선 1
        PlaceSection(divRect1, contentW, DividerHeight, y);
        y += DividerHeight + SectionGap;

        // 스탯 행
        for (int i = 0; i < statRowRects.Length; i++)
        {
            PlaceSection(statRowRects[i], contentW, RowHeight, y);
            y += RowHeight;
            if (i < statRowRects.Length - 1) y += RowGap;
        }
        y += SectionGap;

        if (permanentRect != null && permanentRect.gameObject.activeSelf)
        {
            permanentRect.sizeDelta = new Vector2(contentW, permanentRect.sizeDelta.y);
            float permanentH = Mathf.Max(permanentText.preferredHeight, 22f);
            PlaceSection(permanentRect, contentW, permanentH, y);
            y += permanentH + SectionGap;
        }
        else if (permanentRect != null)
        {
            permanentRect.gameObject.SetActive(false);
        }

        // 구분선 2
        PlaceSection(divRect2, contentW, DividerHeight, y);
        y += DividerHeight + SectionGap;

        string[] traits = layoutTraits;
        bool hasTraits = traits != null && traits.Length > 0;
        if (hasTraits)
        {
            traitsTitleRect.gameObject.SetActive(true);
            traitsTitleRect.sizeDelta = new Vector2(contentW, traitsTitleRect.sizeDelta.y);
            float traitsTitleH = Mathf.Max(traitsTitleText.preferredHeight, 18f);
            PlaceSection(traitsTitleRect, contentW, traitsTitleH, y);
            y += traitsTitleH + 4f;

            for (int i = 0; i < traitRowRects.Length; i++)
            {
                bool showRow = i < traits.Length && !string.IsNullOrEmpty(traits[i]);
                traitRowRects[i].gameObject.SetActive(showRow);
                if (!showRow) continue;

                PlaceSection(traitRowRects[i], contentW, RowHeight, y);
                y += RowHeight;
                if (i + 1 < traits.Length && !string.IsNullOrEmpty(traits[i + 1]))
                {
                    y += RowGap;
                }
            }

            y += SectionGap;
        }
        else
        {
            traitsTitleRect.gameObject.SetActive(false);
            for (int i = 0; i < traitRowRects.Length; i++)
            {
                traitRowRects[i].gameObject.SetActive(false);
            }
        }

        // 패턴
        patternRect.sizeDelta = new Vector2(contentW, patternRect.sizeDelta.y);
        float patternH = Mathf.Max(patternText.preferredHeight, 18f);
        PlaceSection(patternRect, contentW, patternH, y);
        y += patternH + PadBottom;

        tooltipRect.sizeDelta = new Vector2(TooltipWidth, y);
    }

    void PlaceSection(RectTransform rt, float width, float height, float topY)
    {
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(PadX, -topY);
    }

    Text CreateTooltipText(string name, int fontSize, FontStyle style)
    {
        GameObject obj = new GameObject(name);
        Text text = obj.AddComponent<Text>();
        text.font = UIFontProvider.Get();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.supportRichText = true;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    RectTransform CreateTooltipDivider(Transform parent)
    {
        GameObject obj = new GameObject("Divider");
        Image img = obj.AddComponent<Image>();
        img.color = new Color(1f, 0.85f, 0.45f, 0.22f);
        img.raycastTarget = false;
        RectTransform rt = obj.GetComponent<RectTransform>();
        SetupTopAnchored(rt, parent);
        return rt;
    }

    void ShowInternal(Character character)
    {
        if (character == null || character.unitNumber < 1) return;

        EnsureTooltipUi();
        int unit = character.unitNumber;
        int evo = character.evolutionLevel;
        headerText.text = UnitCombatStats.BuildFieldHeaderRich(unit, evo);
        statTexts[0].text = UnitCombatStats.BuildDamageRowRich(unit, evo);
        statTexts[1].text = UnitCombatStats.BuildAttackSpeedRowRich(unit, evo);
        statTexts[2].text = UnitCombatStats.BuildRangeRowRich(unit);

        string permanent = UnitCombatStats.BuildPermanentUpgradeLineRich(unit);
        if (permanentRect != null && permanentText != null)
        {
            bool showPermanent = !string.IsNullOrEmpty(permanent);
            permanentRect.gameObject.SetActive(showPermanent);
            permanentText.text = permanent;
        }

        layoutTraits = UnitTraitData.GetTraits(unit);
        if (layoutTraits == null)
        {
            layoutTraits = System.Array.Empty<string>();
        }

        traitsTitleText.text = UnitCombatStats.BuildTraitsSectionTitleRich();
        for (int i = 0; i < traitRowRects.Length; i++)
        {
            if (i < layoutTraits.Length && !string.IsNullOrEmpty(layoutTraits[i]))
            {
                string traitName = layoutTraits[i];
                traitIcons[i].sprite = TraitIconFactory.Get(traitName);
                traitIcons[i].color = Color.white;
                traitTexts[i].text = UnitCombatStats.BuildTraitNameRich(traitName);
            }
            else
            {
                traitTexts[i].text = string.Empty;
            }
        }

        patternText.text = UnitCombatStats.BuildPatternRich(unit);

        LayoutTooltip();

        if (overlayCanvas == null) return;

        Vector3 anchorWorld = GetCharacterTopRightWorld(character);
        anchorWorld.z = character.transform.position.z;

        if (!TryPlaceTooltipOnCanvas(tooltipRect, overlayCanvas, anchorWorld, new Vector2(8f, 8f)))
        {
            return;
        }

        tooltipRoot.SetActive(true);
        tooltipRoot.transform.SetAsLastSibling();
    }

    /// <summary>화면 전체를 담당하는 루트 오버레이 캔버스를 찾습니다.</summary>
    static Canvas FindRootOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        float bestArea = -1f;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            if (!c.isRootCanvas) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;

            if (best != null && best.overrideSorting != c.overrideSorting)
            {
                if (!c.overrideSorting && best.overrideSorting)
                {
                    best = c;
                    RectTransform r = c.transform as RectTransform;
                    bestArea = r != null ? Mathf.Abs(r.rect.width * r.rect.height) : 0f;
                }
                continue;
            }

            RectTransform rect = c.transform as RectTransform;
            float area = rect != null ? Mathf.Abs(rect.rect.width * rect.rect.height) : 0f;
            if (best == null || area > bestArea)
            {
                best = c;
                bestArea = area;
            }
        }

        return best != null ? best : FindFirstObjectByType<Canvas>();
    }

    static bool TryPlaceTooltipOnCanvas(RectTransform tooltipRect, Canvas rootCanvas, Vector3 worldAnchor, Vector2 offset)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || tooltipRect == null || rootCanvas == null) return false;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return false;

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldAnchor);
        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCam, out Vector2 localPoint))
        {
            return false;
        }

        // 루트 캔버스 기준 좌표 — 매 프레임 SetParent 하지 않음 (위치 피드백 루프 방지)
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 0f);

        Vector2 size = tooltipRect.sizeDelta;
        Vector2 pos = localPoint + offset;

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        const float margin = 8f;

        // 우측/상단을 벗어나면 반대쪽으로 배치
        if (pos.x + size.x > halfW - margin)
        {
            pos.x = localPoint.x - size.x - offset.x;
        }
        if (pos.y + size.y > halfH - margin)
        {
            pos.y = localPoint.y - size.y - offset.y;
        }

        float minX = -halfW + margin;
        float maxX = halfW - size.x - margin;
        float minY = -halfH + margin;
        float maxY = halfH - size.y - margin;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        tooltipRect.anchoredPosition = pos;
        return true;
    }

    static Vector3 GetCharacterTopRightWorld(Character character)
    {
        if (character == null) return Vector3.zero;

        if (TryGetCharacterSpriteBounds(character, out Bounds combined))
        {
            return new Vector3(combined.max.x, combined.max.y, character.transform.position.z);
        }

        float halfWidth = Mathf.Abs(character.transform.localScale.x) * 0.5f;
        float halfHeight = Mathf.Abs(character.transform.localScale.y) * 0.6f;
        return character.transform.position + new Vector3(halfWidth, halfHeight, 0f);
    }

    void HideInternal()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    void ShowOutline(Character character)
    {
        if (character == null)
        {
            HideOutline();
            return;
        }

        if (outlineHost == null)
        {
            outlineHost = new GameObject("FieldUnitOutlineHost");
            outlineHost.transform.SetParent(transform, false);
        }

        outlineTarget = character;
        SpriteSilhouetteOutline.SetTarget(
            outlineHost.transform,
            character.transform,
            renderer => ShouldIncludeHoverBoundsRenderer(renderer, character));
    }

    void HideOutline()
    {
        outlineTarget = null;
        if (outlineHost != null)
        {
            SpriteSilhouetteOutline.Clear(outlineHost.transform);
        }
    }

    static bool TryGetCharacterSpriteBounds(Character character, out Bounds bounds)
    {
        bounds = default;
        if (character == null) return false;

        SpriteRenderer[] renderers = character.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;
        float medianMaxExtent = 0f;
        int extentCount = 0;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (!ShouldIncludeHoverBoundsRenderer(renderer, character)) continue;

            Bounds rendererBounds = GetRendererTightWorldBounds(renderer);
            if (rendererBounds.size.sqrMagnitude < 0.0001f) continue;

            float maxExtent = Mathf.Max(rendererBounds.extents.x, rendererBounds.extents.y);
            medianMaxExtent += maxExtent;
            extentCount++;

            if (!hasBounds)
            {
                bounds = rendererBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rendererBounds);
            }
        }

        if (!hasBounds)
        {
            float halfWidth = Mathf.Abs(character.transform.localScale.x) * 0.5f;
            float halfHeight = Mathf.Abs(character.transform.localScale.y) * 0.6f;
            Vector3 center = character.transform.position;
            bounds = new Bounds(center, new Vector3(halfWidth * 2f, halfHeight * 2f, 0.01f));
            return true;
        }

        if (extentCount > 2)
        {
            Bounds filtered = default;
            bool hasFiltered = false;
            medianMaxExtent /= extentCount;
            float outlierThreshold = Mathf.Max(medianMaxExtent * 2.5f, 0.25f);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (!ShouldIncludeHoverBoundsRenderer(renderer, character)) continue;

                Bounds rendererBounds = GetRendererTightWorldBounds(renderer);
                if (rendererBounds.size.sqrMagnitude < 0.0001f) continue;

                float maxExtent = Mathf.Max(rendererBounds.extents.x, rendererBounds.extents.y);
                if (maxExtent > outlierThreshold) continue;

                if (!hasFiltered)
                {
                    filtered = rendererBounds;
                    hasFiltered = true;
                }
                else
                {
                    filtered.Encapsulate(rendererBounds);
                }
            }

            if (hasFiltered)
            {
                bounds = filtered;
            }
        }

        return hasBounds;
    }

    static bool ShouldIncludeHoverBoundsRenderer(SpriteRenderer renderer, Character character)
    {
        if (renderer == null || !renderer.enabled || renderer.sprite == null) return false;
        if (character == null) return false;
        if (IsHoverOutlineRenderer(renderer)) return false;
        if (SpriteSilhouetteOutline.IsOutlineRenderer(renderer)) return false;
        if (renderer.color.a < 0.05f) return false;
        if (renderer.sortingOrder < -20) return false;
        if (renderer.transform == character.transform) return false;

        Transform walk = renderer.transform;
        while (walk != null && walk != character.transform)
        {
            string nodeName = walk.name;
            if (nodeName == "EvolutionStars" ||
                nodeName == "Shadow" ||
                nodeName == "FieldUnitHoverOutline" ||
                nodeName == "SilhouetteOutlineRoot" ||
                nodeName.StartsWith("Star_") ||
                nodeName.StartsWith("Unit22") ||
                nodeName.EndsWith("Attack") ||
                nodeName.EndsWith("Overlay") ||
                nodeName.EndsWith("Marker") ||
                nodeName.EndsWith("Pad"))
            {
                return false;
            }

            walk = walk.parent;
        }

        return true;
    }

    static Bounds GetRendererTightWorldBounds(SpriteRenderer renderer)
    {
        Bounds localBounds = renderer.sprite.bounds;
        Vector3 worldCenter = renderer.transform.TransformPoint(localBounds.center);
        Vector3 worldSize = renderer.transform.TransformVector(localBounds.size);
        worldSize.x = Mathf.Abs(worldSize.x);
        worldSize.y = Mathf.Abs(worldSize.y);
        worldSize.z = 0.01f;
        return new Bounds(worldCenter, worldSize);
    }

    static bool IsHoverOutlineRenderer(SpriteRenderer renderer)
    {
        return SpriteSilhouetteOutline.IsOutlineRenderer(renderer);
    }
}
