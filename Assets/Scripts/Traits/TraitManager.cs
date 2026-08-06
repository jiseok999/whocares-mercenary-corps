using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 조합(특성) 관리
/// </summary>
public class TraitManager : MonoBehaviour
{
    public static TraitManager Instance { get; private set; }

    const int MaxVisibleTraitRows = 7;
    const float TraitRowStep = 68f;
    const float TraitListTopPadding = 40f;
    const int DefaultPanelSortingOrder = 15;
    const int SelectionBoostPanelSortingOrder = 360;
    const int DefaultTooltipSortingOrder = 30;
    const int SelectionBoostTooltipSortingOrder = 370;
    const float TraitDragRowThresholdRatio = 0.42f;
    static readonly Color TraitInactiveIconColor = new Color(0.42f, 0.42f, 0.42f, 0.82f);
    static readonly Color TraitInactiveTextColor = new Color(0.62f, 0.58f, 0.54f, 0.88f);
    static readonly Color TraitInactiveTierColor = new Color(0.42f, 0.40f, 0.36f, 0.78f);
    static readonly Color TraitInactiveBackgroundColor = new Color(0.58f, 0.58f, 0.58f, 1f);
    
    private Dictionary<string, int> traitCounts = new Dictionary<string, int>();
    private readonly List<string> activeTraits = new List<string>(16);
    
    private Transform uiParent;
    private int traitListScrollIndex;
    private float traitListDragAccumulated;
    private Vector2 traitListLastPointerPos;
    private bool traitListHasLastPointerPos;
    class TraitRow
    {
        public GameObject root;
        public Image background;
        public Image icon;
        public Text countText;
        public Text tiersText;
        public Text nameText;
        public string traitName;
        public int count;
    }

    private List<TraitRow> traitRows = new List<TraitRow>();

    // 조합 호버 툴팁
    private GameObject traitTooltipRoot;
    private Text traitTooltipText;
    private Text traitTooltipTitle;
    private Image traitTooltipIcon;
    private RectTransform traitTooltipRect;
    private Canvas traitOverlayCanvas;
    private static Sprite cachedMixtureSprite;
    private int savedPanelSortingOrder = DefaultPanelSortingOrder;
    private int savedTooltipSortingOrder = DefaultTooltipSortingOrder;
    private bool selectionOverlayBoosted;
    class UnitInfoRow
    {
        public GameObject root;
        public Image background;
        public Text text;
    }

    private List<UnitInfoRow> unitInfoRows = new List<UnitInfoRow>();
    private Button toggleButton;
    private Text toggleButtonText;
    private bool showUnitInfo;
    private string lastTooltipTraitName;
    private int lastTooltipTraitCount;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (GetComponent<TraitPeriodicAttackRunner>() == null)
        {
            gameObject.AddComponent<TraitPeriodicAttackRunner>();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            GameLocalizationCoordinator.Unregister(RefreshLocalizedUi);
            Instance = null;
        }
    }

    void RefreshLocalizedUi()
    {
        Font font = UIFontProvider.Get();
        UpdateToggleText();
        for (int i = 0; i < traitRows.Count; i++)
        {
            TraitRow row = traitRows[i];
            if (row.nameText != null) UIFontProvider.ApplyFont(row.nameText, font);
            if (row.countText != null) UIFontProvider.ApplyFont(row.countText, font);
            if (row.tiersText != null) UIFontProvider.ApplyFont(row.tiersText, font);
        }
        for (int i = 0; i < unitInfoRows.Count; i++)
        {
            if (unitInfoRows[i].text != null) UIFontProvider.ApplyFont(unitInfoRows[i].text, font);
        }
        UpdateUI();
        if (traitTooltipRoot != null && traitTooltipRoot.activeSelf && !string.IsNullOrEmpty(lastTooltipTraitName))
        {
            if (traitTooltipTitle != null)
            {
                UIFontProvider.ApplyFont(traitTooltipTitle, font);
                traitTooltipTitle.text = BuildTraitTooltipTitle(lastTooltipTraitName, lastTooltipTraitCount);
            }
            if (traitTooltipText != null)
            {
                UIFontProvider.ApplyFont(traitTooltipText, font);
                traitTooltipText.text = BuildTraitTooltipText(lastTooltipTraitName, lastTooltipTraitCount);
            }
        }
    }

    public static void ReleaseInstance()
    {
        Instance = null;
    }
    
    void Update()
    {
        Recalculate();
        HandleTraitListScrollInput();
        UpdateUI();
        UpdateTraitTooltip();
    }
    
    public static void SetTraitUiSelectionBoost(bool enabled)
    {
        if (Instance != null)
        {
            Instance.SetSelectionOverlayBoost(enabled);
        }
    }

    public void SetSelectionOverlayBoost(bool enabled)
    {
        if (enabled == selectionOverlayBoosted) return;

        if (uiParent != null)
        {
            Canvas panelCanvas = uiParent.GetComponent<Canvas>();
            if (panelCanvas != null)
            {
                if (enabled)
                {
                    savedPanelSortingOrder = panelCanvas.sortingOrder;
                    panelCanvas.sortingOrder = SelectionBoostPanelSortingOrder;
                }
                else
                {
                    panelCanvas.sortingOrder = savedPanelSortingOrder;
                }
            }
        }

        if (traitTooltipRoot != null)
        {
            Canvas tooltipCanvas = traitTooltipRoot.GetComponent<Canvas>();
            if (tooltipCanvas != null)
            {
                if (enabled)
                {
                    savedTooltipSortingOrder = tooltipCanvas.sortingOrder;
                    tooltipCanvas.sortingOrder = SelectionBoostTooltipSortingOrder;
                }
                else
                {
                    tooltipCanvas.sortingOrder = savedTooltipSortingOrder;
                }
            }
        }

        selectionOverlayBoosted = enabled;
        if (!enabled)
        {
            HideTraitTooltip();
        }
    }

    public void RegisterUI(Transform parent, Button toggle, Text toggleText)
    {
        uiParent = parent;
        traitListScrollIndex = 0;
        toggleButton = toggle;
        toggleButtonText = toggleText;
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleView);
        }

        GameLocalizationCoordinator.Register(RefreshLocalizedUi);
        UpdateTraitUI();
    }

    void HandleTraitListScrollInput()
    {
        if (uiParent == null || showUnitInfo) return;

        Vector2 pointerPos = GetPointerScreenPosition();
        bool overPanel = IsScreenPointOverPanel(pointerPos);

        if (overPanel && IsPrimaryPointerPressed())
        {
            Vector2 pointerDelta = GetPointerDragDelta(pointerPos);
            traitListDragAccumulated += pointerDelta.y;
            ApplyTraitListDragAccumulated();
        }
        else
        {
            traitListDragAccumulated = 0f;
            traitListHasLastPointerPos = false;
        }

        if (!overPanel) return;

        float scrollDelta = Input.mouseScrollDelta.y;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            scrollDelta = Mouse.current.scroll.ReadValue().y;
        }
#endif
        if (Mathf.Abs(scrollDelta) < 0.01f) return;

        int direction = scrollDelta > 0f ? -1 : 1;
        traitListScrollIndex = Mathf.Clamp(traitListScrollIndex + direction, 0, GetMaxTraitListScrollIndex());
    }

    int GetMaxTraitListScrollIndex()
    {
        int count = 0;
        foreach (var kvp in traitCounts)
        {
            if (kvp.Value > 0) count++;
        }

        return Mathf.Max(0, count - MaxVisibleTraitRows);
    }

    void ApplyTraitListDragAccumulated()
    {
        float threshold = TraitRowStep * TraitDragRowThresholdRatio;
        int maxScrollIndex = GetMaxTraitListScrollIndex();

        while (traitListDragAccumulated >= threshold)
        {
            if (traitListScrollIndex >= maxScrollIndex) break;
            traitListScrollIndex++;
            traitListDragAccumulated -= threshold;
        }

        while (traitListDragAccumulated <= -threshold)
        {
            if (traitListScrollIndex <= 0) break;
            traitListScrollIndex--;
            traitListDragAccumulated += threshold;
        }
    }

    static bool IsPrimaryPointerPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }
#endif
        return Input.GetMouseButton(0);
    }

    Vector2 GetPointerDragDelta(Vector2 currentPointerPos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.delta.ReadValue();
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return Mouse.current.delta.ReadValue();
        }
#endif
        Vector2 delta = traitListHasLastPointerPos ? currentPointerPos - traitListLastPointerPos : Vector2.zero;
        traitListLastPointerPos = currentPointerPos;
        traitListHasLastPointerPos = true;
        return delta;
    }
    
    public int GetDamageBonusForUnit(int unitNumber) => 0;

    /// <summary>레거시 API — 조합 스탯 배율 제거, 항상 1.</summary>
    public float GetDamageMultiplierForUnit(int unitNumber) => 1f;

    /// <summary>레거시 API — 조합 스탯 배율 제거, 항상 1.</summary>
    public float GetAttackSpeedMultiplierForUnit(int unitNumber) => 1f;

    public int GetRoundEndGoldBonus() => 0;

    /// <summary>현재 발동 중(단계 1 이상)인 조합 목록.</summary>
    public IReadOnlyList<string> GetActiveTraits()
    {
        return activeTraits;
    }

    public int GetTraitCount(string traitName)
    {
        if (string.IsNullOrEmpty(traitName)) return 0;
        if (traitCounts.TryGetValue(traitName, out int count))
        {
            return count;
        }
        return 0;
    }

    /// <summary>조합명의 현재 발동 단계(0=미발동, 1=1단계, 2=2단계...).</summary>
    public int GetTraitActiveLevel(string traitName)
    {
        if (string.IsNullOrEmpty(traitName)) return 0;
        if (!traitCounts.TryGetValue(traitName, out int count)) return 0;
        if (!TraitPeriodicAttackDefs.TryGetDef(traitName, out TraitPeriodicAttackDefs.TraitDef def)) return 0;
        return TraitPeriodicAttackDefs.GetActiveLevel(def, count);
    }
    
    void Recalculate()
    {
        traitCounts.Clear();
        activeTraits.Clear();
        
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character unit in units)
        {
            if (unit == null) continue;
            if (!unit.IsProperlyPlaced()) continue;
            
            string[] traits = UnitTraitData.GetTraits(unit.unitNumber);
            int contribution = GameManager.Instance != null
                ? GameManager.Instance.GetBossTraitCountContribution(unit)
                : 1;
            foreach (string trait in traits)
            {
                if (string.IsNullOrEmpty(trait)) continue;
                if (!traitCounts.ContainsKey(trait)) traitCounts[trait] = 0;
                traitCounts[trait] += contribution;
            }
        }

        foreach (var kvp in traitCounts)
        {
            if (kvp.Value <= 0) continue;
            if (GetTraitActiveLevel(kvp.Key) <= 0) continue;
            activeTraits.Add(kvp.Key);
        }
    }

    public bool IsPenguinAuraActive() => false;

    public bool TryGetPenguinAuraMultiplier(out float multiplier)
    {
        multiplier = 1f;
        return false;
    }

    public string GetPenguinAuraEffectDescription() => string.Empty;

    public bool IsBoardCellInPenguinAura(int row, int col) => false;

    public int CountPlacedPenguinUnits()
    {
        int count = 0;
        Character[] units = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Character unit = units[i];
            if (unit == null || !unit.IsProperlyPlaced()) continue;
            if (HasTrait(unit.unitNumber, "펭귄")) count++;
        }
        return count;
    }

    public int CountPenguinAuraCells() => 0;

    /*
    static float GetSynergyEffectMultiplier(Character unit, string trait)
    {
        if (unit == null || GameManager.Instance == null) return 1f;
        if (!TryGetUnitCell(unit, out int row, out int col)) return 1f;
        return GameManager.Instance.IsUnitOnMatchingSynergyCell(unit.unitNumber, row, col, trait)
            ? GameManager.SynergyCellEffectMultiplier
            : 1f;
    }
    */

    static bool HasTrait(int unitNumber, string traitName)
    {
        string[] traits = UnitTraitData.GetTraits(unitNumber);
        if (traits == null) return false;
        for (int i = 0; i < traits.Length; i++)
        {
            if (traits[i] == traitName) return true;
        }
        return false;
    }

    static bool TryGetUnitCell(Character unit, out int row, out int col)
    {
        row = col = 0;
        if (unit == null) return false;
        BoardCell cell = unit.GetCurrentBoardCell();
        if (cell == null || !cell.isBoardCell) return false;
        string cellName = cell.gameObject.name;
        if (string.IsNullOrEmpty(cellName) || !cellName.StartsWith("BoardCell_")) return false;
        string[] parts = cellName.Split('_');
        if (parts.Length < 3) return false;
        return int.TryParse(parts[1], out row) && int.TryParse(parts[2], out col);
    }

    /// <summary>조합 패널에 표시할 현재 단계 효과 요약(미발동 시 빈 문자열).</summary>
    string GetTraitEffectSummary(string traitName, int count)
    {
        return TraitPeriodicAttackDefs.GetEffectSummary(traitName, count);
    }

    /// <summary>조합 단계 구간을 "2 4 6" 형태로 반환.</summary>
    string GetBreakpointText(string traitName)
    {
        return TraitPeriodicAttackDefs.GetBreakpointText(traitName);
    }

    /// <summary>툴팁 제목(조합명 + 보유 수)을 리치 텍스트로 구성합니다.</summary>
    string BuildTraitTooltipTitle(string traitName, int count)
    {
        return $"<size=20><b><color=#{UnitCombatStats.ColName}>{GameLocalization.GetTraitDisplayName(traitName)}</color></b></size>  " +
               $"<size=14><color=#{UnitCombatStats.ColSub}>{GameLocalization.TraitTooltipOwnedFormat(count)}</color></size>";
    }

    /// <summary>툴팁에 표시할 조합 상세 설명(본문)을 리치 텍스트로 구성합니다.</summary>
    string BuildTraitTooltipText(string traitName, int count)
    {
        return TraitPeriodicAttackDefs.BuildTooltipBody(traitName, count);
    }

    void EnsureTraitTooltip()
    {
        if (traitTooltipRoot != null) return;

        traitOverlayCanvas = FindRootOverlayCanvas();
        Transform parent = traitOverlayCanvas != null
            ? traitOverlayCanvas.transform
            : (uiParent != null ? uiParent : transform);

        traitTooltipRoot = new GameObject("TraitTooltip");
        traitTooltipRoot.transform.SetParent(parent, false);

        Canvas tooltipCanvas = traitTooltipRoot.AddComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 30;

        Image bg = traitTooltipRoot.AddComponent<Image>();
        TooltipTheme.ApplyStandardBackground(bg, true);

        traitTooltipRect = traitTooltipRoot.GetComponent<RectTransform>();
        traitTooltipRect.sizeDelta = new Vector2(260f, 150f);
        traitTooltipRect.pivot = new Vector2(0f, 1f);

        // 조합 아이콘
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(traitTooltipRoot.transform, false);
        traitTooltipIcon = iconObj.AddComponent<Image>();
        traitTooltipIcon.raycastTarget = false;
        traitTooltipIcon.preserveAspect = true;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(12f, -12f);
        iconRect.sizeDelta = new Vector2(44f, 44f);

        // 제목(조합명 + 보유 수)
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(traitTooltipRoot.transform, false);
        traitTooltipTitle = titleObj.AddComponent<Text>();
        traitTooltipTitle.font = UIFontProvider.Get();
        traitTooltipTitle.fontSize = 18;
        traitTooltipTitle.color = Color.white;
        traitTooltipTitle.supportRichText = true;
        traitTooltipTitle.alignment = TextAnchor.MiddleLeft;
        traitTooltipTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        traitTooltipTitle.verticalOverflow = VerticalWrapMode.Overflow;
        traitTooltipTitle.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(66f, -52f);
        titleRect.offsetMax = new Vector2(-12f, -10f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(traitTooltipRoot.transform, false);
        traitTooltipText = textObj.AddComponent<Text>();
        traitTooltipText.font = UIFontProvider.Get();
        traitTooltipText.fontSize = 16;
        traitTooltipText.color = Color.white;
        traitTooltipText.supportRichText = true;
        traitTooltipText.alignment = TextAnchor.UpperLeft;
        traitTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        traitTooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        traitTooltipText.raycastTarget = false;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 10f);
        textRect.offsetMax = new Vector2(-12f, -60f);

        traitTooltipRoot.SetActive(false);
    }

    public void HideActiveTooltip()
    {
        HideTraitTooltip();
    }

    public bool IsScreenPointOverPanel(Vector2 screenPos)
    {
        if (uiParent == null || !uiParent.gameObject.activeInHierarchy) return false;

        RectTransform rect = uiParent as RectTransform;
        return rect != null && TooltipPointerHelper.IsScreenPointInsideRect(rect, screenPos);
    }

    void UpdateTraitTooltip()
    {
        if (ShouldHideTraitTooltip())
        {
            HideTraitTooltip();
            return;
        }

        Vector2 screenPos = TooltipPointerHelper.GetScreenPosition();
        Canvas canvas = uiParent.GetComponentInParent<Canvas>();
        Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        TraitRow hovered = null;
        for (int i = 0; i < traitRows.Count; i++)
        {
            TraitRow row = traitRows[i];
            if (row == null || row.root == null || !row.root.activeSelf) continue;
            RectTransform rect = row.root.GetComponent<RectTransform>();
            if (rect == null) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, uiCam)) continue;
            if (!TooltipPointerHelper.IsPointerHittingTransform(row.root.transform, traitTooltipRoot != null ? traitTooltipRoot.transform : null))
            {
                continue;
            }

            hovered = row;
            break;
        }

        if (hovered == null || string.IsNullOrEmpty(hovered.traitName))
        {
            HideTraitTooltip();
            return;
        }

        EnsureTraitTooltip();
        if (selectionOverlayBoosted && traitTooltipRoot != null)
        {
            Canvas tooltipCanvas = traitTooltipRoot.GetComponent<Canvas>();
            if (tooltipCanvas != null && tooltipCanvas.sortingOrder < SelectionBoostTooltipSortingOrder)
            {
                tooltipCanvas.sortingOrder = SelectionBoostTooltipSortingOrder;
            }
        }

        lastTooltipTraitName = hovered.traitName;
        lastTooltipTraitCount = hovered.count;

        if (traitTooltipTitle != null)
        {
            traitTooltipTitle.text = BuildTraitTooltipTitle(hovered.traitName, hovered.count);
        }
        if (traitTooltipIcon != null)
        {
            traitTooltipIcon.sprite = GetTraitIconSprite(hovered.traitName);
        }
        traitTooltipText.text = BuildTraitTooltipText(hovered.traitName, hovered.count);

        // 내용에 맞춰 박스 높이 자동 조정 (헤더 60 + 본문 + 패딩)
        const float tooltipWidth = 280f;
        const float headerHeight = 60f;
        traitTooltipRect.sizeDelta = new Vector2(tooltipWidth, traitTooltipRect.sizeDelta.y);
        float contentHeight = headerHeight + traitTooltipText.preferredHeight + 14f;
        traitTooltipRect.sizeDelta = new Vector2(tooltipWidth, Mathf.Max(90f, contentHeight));

        PlaceTraitTooltip(screenPos);

        traitTooltipRoot.SetActive(true);
        traitTooltipRoot.transform.SetAsLastSibling();
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

    void PlaceTraitTooltip(Vector2 screenPos)
    {
        if (traitTooltipRect == null || traitOverlayCanvas == null) return;

        RectTransform canvasRect = traitOverlayCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Camera uiCam = traitOverlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : traitOverlayCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 localPoint))
        {
            return;
        }

        // 루트 캔버스 기준 — 매 프레임 SetParent 하지 않음 (위치 피드백 루프 방지)
        traitTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        traitTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        traitTooltipRect.pivot = new Vector2(0f, 1f);

        Vector2 size = traitTooltipRect.sizeDelta;
        Vector2 offset = new Vector2(18f, -8f);
        Vector2 pos = localPoint + offset;

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        const float margin = 8f;

        if (pos.x + size.x > halfW - margin)
        {
            pos.x = localPoint.x - size.x - offset.x;
        }
        if (pos.y - size.y < -halfH + margin)
        {
            pos.y = localPoint.y + Mathf.Abs(offset.y);
        }
        if (pos.y > halfH - margin)
        {
            pos.y = halfH - margin;
        }

        pos.x = Mathf.Clamp(pos.x, -halfW + margin, halfW - size.x - margin);
        pos.y = Mathf.Clamp(pos.y, -halfH + size.y + margin, halfH - margin);

        traitTooltipRect.anchoredPosition = pos;
    }

    bool ShouldHideTraitTooltip()
    {
        if (uiParent == null || !uiParent.gameObject.activeInHierarchy || showUnitInfo)
        {
            return true;
        }

        if (SkillManager.Instance != null && SkillManager.Instance.IsSkillArmed)
        {
            return true;
        }

        return false;
    }

    void HideTraitTooltip()
    {
        lastTooltipTraitName = null;
        lastTooltipTraitCount = 0;
        if (traitTooltipRoot != null && traitTooltipRoot.activeSelf)
        {
            traitTooltipRoot.SetActive(false);
        }
    }

    static Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }
    
    void UpdateUI()
    {
        if (uiParent == null) return;
        Image panelBackground = uiParent.GetComponent<Image>();
        if (panelBackground != null)
        {
            panelBackground.enabled = !showUnitInfo;
        }

        if (showUnitInfo)
        {
            UpdateUnitInfoUI();
        }
        else
        {
            UpdateTraitUI();
        }
        
        UpdateToggleText();
    }
    
    void UpdateTraitUI()
    {
        if (uiParent == null) return;

        List<(string traitName, int count, int level)> sortedTraits = BuildSortedTraitList();
        traitListScrollIndex = Mathf.Clamp(traitListScrollIndex, 0, GetMaxTraitListScrollIndex());

        while (traitRows.Count < sortedTraits.Count)
        {
            traitRows.Add(CreateTraitRow());
        }

        for (int index = 0; index < sortedTraits.Count; index++)
        {
            (string traitName, int count, int level) entry = sortedTraits[index];
            TraitRow row = traitRows[index];
            if (row.root.transform.parent != uiParent)
            {
                row.root.transform.SetParent(uiParent, false);
            }

            bool visible = index >= traitListScrollIndex && index < traitListScrollIndex + MaxVisibleTraitRows;
            if (!visible)
            {
                row.root.SetActive(false);
                continue;
            }

            int displayIndex = index - traitListScrollIndex;
            row.traitName = entry.traitName;
            row.count = entry.count;
            row.countText.text = entry.count.ToString();
            string breakpointText = GetBreakpointText(entry.traitName);
            row.tiersText.text = string.IsNullOrEmpty(breakpointText) ? "" : breakpointText;
            row.nameText.text = GameLocalization.GetTraitDisplayName(entry.traitName);
            row.icon.sprite = GetTraitIconSprite(entry.traitName);
            ApplyTraitRowVisualState(row, entry.level > 0);

            RectTransform rect = row.root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -(TraitListTopPadding + displayIndex * TraitRowStep));
            rect.sizeDelta = new Vector2(220f, 64f);
            rect.SetSiblingIndex(displayIndex);

            row.root.SetActive(true);
        }

        for (int i = sortedTraits.Count; i < traitRows.Count; i++)
        {
            traitRows[i].root.SetActive(false);
        }

        for (int i = 0; i < unitInfoRows.Count; i++)
        {
            unitInfoRows[i].root.SetActive(false);
        }
    }

    List<(string traitName, int count, int level)> BuildSortedTraitList()
    {
        var sortedTraits = new List<(string traitName, int count, int level)>();
        foreach (var kvp in traitCounts)
        {
            if (kvp.Value <= 0) continue;
            sortedTraits.Add((kvp.Key, kvp.Value, GetTraitActiveLevel(kvp.Key)));
        }

        sortedTraits.Sort((a, b) =>
        {
            int activeCompare = (b.level > 0).CompareTo(a.level > 0);
            if (activeCompare != 0) return activeCompare;
            int countCompare = b.count.CompareTo(a.count);
            if (countCompare != 0) return countCompare;
            return string.Compare(a.traitName, b.traitName, System.StringComparison.Ordinal);
        });

        return sortedTraits;
    }

    void ApplyTraitRowVisualState(TraitRow row, bool isActive)
    {
        if (row == null) return;

        if (row.background != null)
        {
            row.background.color = isActive ? Color.white : TraitInactiveBackgroundColor;
        }

        row.icon.color = isActive ? Color.white : TraitInactiveIconColor;
        row.countText.color = isActive ? Color.white : TraitInactiveTextColor;
        row.nameText.color = isActive ? Color.white : TraitInactiveTextColor;
        row.tiersText.color = isActive
            ? new Color(0.9f, 0.85f, 0.5f, 1f)
            : TraitInactiveTierColor;
    }
    
    void UpdateUnitInfoUI()
    {
        List<Character> units = new List<Character>();
        Character[] all = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character unit in all)
        {
            if (unit == null) continue;
            if (!unit.IsProperlyPlaced()) continue;
            units.Add(unit);
        }
        
        // 필요 행 수 확보
        while (unitInfoRows.Count < units.Count)
        {
            unitInfoRows.Add(CreateUnitInfoRow());
        }
        
        for (int i = 0; i < units.Count; i++)
        {
            Character unit = units[i];
            int evolution = unit.evolutionLevel;
            float atkSpeed = unit.attackCooldown;
            int atk = unit.GetDisplayAttackDamage();
            UnitInfoRow row = unitInfoRows[i];
            row.text.text = GameLocalization.TraitUnitInfoRowFormat(
                UnitTraitData.GetDisplayName(unit.unitNumber), atkSpeed, evolution, atk);
            
            RectTransform rect = row.root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -40 - i * 22);
            rect.sizeDelta = new Vector2(230, 24);
            
            row.root.SetActive(true);
        }
        
        // 나머지 숨김
        for (int i = units.Count; i < unitInfoRows.Count; i++)
        {
            unitInfoRows[i].root.SetActive(false);
        }
        
        // 조합 숨김
        for (int i = 0; i < traitRows.Count; i++)
        {
            traitRows[i].root.SetActive(false);
        }
    }
    
    void ToggleView()
    {
        showUnitInfo = !showUnitInfo;
        UpdateUI();
    }
    
    void UpdateToggleText()
    {
        if (toggleButtonText == null) return;
        toggleButtonText.text = showUnitInfo ? GameLocalization.TraitToggleTraits : GameLocalization.TraitToggleUnitInfo;
        UIFontProvider.ApplyFont(toggleButtonText);
    }

    UnitInfoRow CreateUnitInfoRow()
    {
        UnitInfoRow row = new UnitInfoRow();

        GameObject rowObj = new GameObject("UnitInfoRow");
        rowObj.transform.SetParent(uiParent, false);
        row.root = rowObj;

        Image bg = rowObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        row.background = bg;

        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(230, 24);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(rowObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = UIFontProvider.Get();
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        row.text = text;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 0);
        textRect.offsetMax = new Vector2(-8, 0);

        return row;
    }

    TraitRow CreateTraitRow()
    {
        TraitRow row = new TraitRow();

        GameObject rowObj = new GameObject("TraitRow");
        rowObj.transform.SetParent(uiParent, false);
        row.root = rowObj;

        Image bg = rowObj.AddComponent<Image>();
        bg.color = Color.white;
        bg.sprite = GetRowBackgroundSprite();
        bg.type = Image.Type.Simple;
        row.background = bg;

        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(0, 1);
        rowRect.pivot = new Vector2(0, 1);
        rowRect.sizeDelta = new Vector2(220, 64);

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(rowObj.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.color = Color.white;
        icon.sprite = GetDefaultAbilityIconSprite();
        icon.preserveAspect = true;
        row.icon = icon;

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 1);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 1);
        iconRect.sizeDelta = new Vector2(58, 58);
        iconRect.anchoredPosition = new Vector2(6, -6);

        GameObject countObj = new GameObject("Count");
        countObj.transform.SetParent(rowObj.transform, false);
        Text countText = countObj.AddComponent<Text>();
        countText.font = UIFontProvider.Get();
        countText.fontSize = 40;
        countText.color = Color.white;
        countText.alignment = TextAnchor.UpperLeft;
        row.countText = countText;

        RectTransform countRect = countObj.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0, 1);
        countRect.anchorMax = new Vector2(0, 1);
        countRect.pivot = new Vector2(0, 1);
        countRect.sizeDelta = new Vector2(60, 40);
        countRect.anchoredPosition = new Vector2(74, -14);

        GameObject tiersObj = new GameObject("Tiers");
        tiersObj.transform.SetParent(rowObj.transform, false);
        Text tiersText = tiersObj.AddComponent<Text>();
        tiersText.font = UIFontProvider.Get();
        tiersText.fontSize = 14;
        tiersText.color = new Color(0.9f, 0.85f, 0.5f, 1f);
        tiersText.alignment = TextAnchor.UpperCenter;
        row.tiersText = tiersText;

        RectTransform tiersRect = tiersObj.GetComponent<RectTransform>();
        tiersRect.anchorMin = new Vector2(0.5f, 1);
        tiersRect.anchorMax = new Vector2(0.5f, 1);
        tiersRect.pivot = new Vector2(0.5f, 1);
        tiersRect.sizeDelta = new Vector2(120, 18);
        tiersRect.anchoredPosition = new Vector2(32, -36);

        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(rowObj.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.font = UIFontProvider.Get();
        nameText.fontSize = 16;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.UpperCenter;
        row.nameText = nameText;

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.sizeDelta = new Vector2(-20, 20);
        nameRect.anchoredPosition = new Vector2(32, -6);

        return row;
    }

    static Sprite cachedPixelSprite;

    static Sprite GetPixelSprite()
    {
        if (cachedPixelSprite != null) return cachedPixelSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        cachedPixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
        return cachedPixelSprite;
    }

    static Sprite GetRowBackgroundSprite()
    {
        Sprite mixture = GetMixtureSprite();
        return mixture != null ? mixture : GetPixelSprite();
    }

    static Sprite GetMixtureSprite()
    {
        if (cachedMixtureSprite != null) return cachedMixtureSprite;

        cachedMixtureSprite = Resources.Load<Sprite>("ui_mixture");
        if (cachedMixtureSprite != null) return cachedMixtureSprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>("ui_mixture");
        if (sprites != null && sprites.Length > 0)
        {
            cachedMixtureSprite = sprites[0];
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == "ui_mixture_0")
                {
                    cachedMixtureSprite = sprites[i];
                    break;
                }
            }
        }

        return cachedMixtureSprite;
    }

    static Sprite GetDefaultAbilityIconSprite()
    {
        return TraitIconFactory.Get(string.Empty);
    }

    static Sprite GetTraitIconSprite(string traitName)
    {
        return TraitIconFactory.Get(traitName);
    }

    public static float GetTraitPanelVisibleHeight()
    {
        return MaxVisibleTraitRows * TraitRowStep + TraitListTopPadding;
    }

    /// <summary>조합 발동 토스트를 조합 목록(uiParent) 기준 로컬 좌표로 배치.</summary>
    public bool TryGetTraitProcToastLocalPosition(string traitName, out Transform listParent, out Vector2 anchoredPosition)
    {
        listParent = uiParent;
        anchoredPosition = default;
        if (string.IsNullOrEmpty(traitName) || uiParent == null) return false;

        for (int i = 0; i < traitRows.Count; i++)
        {
            TraitRow row = traitRows[i];
            if (row == null || row.root == null) continue;
            if (row.traitName != traitName) continue;
            if (!row.root.activeSelf) continue;

            RectTransform rowRect = row.root.transform as RectTransform;
            if (rowRect == null) return false;

            Vector2 rowPos = rowRect.anchoredPosition;
            float rowWidth = rowRect.rect.width;
            float rowHeight = rowRect.rect.height;
            anchoredPosition = new Vector2(rowPos.x + rowWidth + 8f, rowPos.y - rowHeight * 0.5f);
            return true;
        }

        List<(string traitName, int count, int level)> sorted = BuildSortedTraitList();
        int index = -1;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].traitName == traitName)
            {
                index = i;
                break;
            }
        }
        if (index < 0) return false;

        int displayIndex = index - traitListScrollIndex;
        if (displayIndex < 0 || displayIndex >= MaxVisibleTraitRows)
        {
            displayIndex = Mathf.Clamp(index, 0, MaxVisibleTraitRows - 1);
        }

        float rowTop = -(TraitListTopPadding + displayIndex * TraitRowStep);
        anchoredPosition = new Vector2(10f + 220f + 8f, rowTop - 32f);
        return true;
    }

    /// <summary>조합 발동 토스트를 붙일 UI 행의 오른쪽 중앙(스크린 좌표).</summary>
    public bool TryGetTraitProcToastAnchor(string traitName, out Vector2 screenPoint)
    {
        screenPoint = default;
        if (string.IsNullOrEmpty(traitName) || uiParent == null) return false;

        for (int i = 0; i < traitRows.Count; i++)
        {
            TraitRow row = traitRows[i];
            if (row == null || row.root == null) continue;
            if (row.traitName != traitName) continue;
            if (!row.root.activeSelf) continue;
            return TryGetRectRightCenter(row.root.transform as RectTransform, out screenPoint);
        }

        List<(string traitName, int count, int level)> sorted = BuildSortedTraitList();
        int index = -1;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].traitName == traitName)
            {
                index = i;
                break;
            }
        }
        if (index < 0) return false;

        RectTransform parentRt = uiParent as RectTransform;
        if (parentRt == null) return false;

        Vector3[] corners = new Vector3[4];
        parentRt.GetWorldCorners(corners);
        float rightX = corners[2].x;
        float topY = corners[1].y;

        int displayIndex = index - traitListScrollIndex;
        if (displayIndex < 0 || displayIndex >= MaxVisibleTraitRows)
        {
            displayIndex = Mathf.Clamp(index, 0, MaxVisibleTraitRows - 1);
        }

        float rowCenterY = topY - TraitListTopPadding - displayIndex * TraitRowStep - TraitRowStep * 0.5f;
        screenPoint = new Vector2(rightX, rowCenterY);
        return true;
    }

    public static Canvas GetOverlayCanvas()
    {
        return FindRootOverlayCanvas();
    }

    static bool TryGetRectRightCenter(RectTransform rect, out Vector2 screenPoint)
    {
        screenPoint = default;
        if (rect == null) return false;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        screenPoint = new Vector2(corners[2].x, (corners[2].y + corners[3].y) * 0.5f);
        return true;
    }
}


