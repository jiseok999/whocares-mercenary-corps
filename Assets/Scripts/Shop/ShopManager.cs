using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 정보를 담는 클래스
/// </summary>
[System.Serializable]
public class CharacterData
{
    public string name;
    public string description;
    public Color color;
    
    public CharacterData(string name, string description, Color color)
    {
        this.name = name;
        this.description = description;
        this.color = color;
    }
}

/// <summary>
/// 상점을 관리하는 매니저
/// </summary>
public class ShopManager : MonoBehaviour
{
    public enum ShopRefreshSource
    {
        /// <summary>UI 초기화 등 — 대사 없음, roster만 갱신</summary>
        Silent,
        /// <summary>첫 시작·전투 종료 자동 새로고침 — 용병 대사 가능</summary>
        Auto,
        /// <summary>플레이어 수동 새로고침 — 대사 없음</summary>
        Manual,
    }

    public static ShopManager Instance { get; private set; }
    
    [Header("Shop Settings")]
    public int shopSlots = 4; // 상점에 표시할 캐릭터 수
    
    [Header("UI References")]
    public GameObject shopPanel; // 상점 패널
    public Transform shopSlotsParent; // 상점 슬롯들의 부모
    public Button refreshButton; // 재선택 버튼
    
    private List<GameObject> currentShopItems = new List<GameObject>(); // 현재 상점에 표시된 아이템들
    
    // 28개 유닛 데이터 리스트
    private List<UnitData> allUnits = new List<UnitData>();
    
    private BoardManager boardManager;
    private Sprite cachedChoicePopupSprite;
    private Sprite cachedGradeBackSprite;
    private readonly Dictionary<int, Sprite> cachedUnitSprites = new Dictionary<int, Sprite>();
    private Camera previewCamera;
    private Transform previewRoot;
    private readonly List<GameObject> previewInstances = new List<GameObject>();
    private readonly List<RenderTexture> previewTextures = new List<RenderTexture>();
    private const int PreviewLayer = 30;
    private const int MaxStarLevel = UnitCombatStats.MaxEvolutionLevel;

    private GameObject shopTooltipRoot;
    private Text shopTooltipText;
    private RectTransform shopTooltipRect;
    private Canvas shopOverlayCanvas;

    const int ShopCanvasSortingOrder = 50;
    const int ShopTooltipSortingOrder = 60;

    ShopRefreshEffect refreshEffect;
    Coroutine refreshShopRoutine;
    Coroutine mercenaryDialogueRoutine;

    readonly List<int> currentShopUnitNumbers = new List<int>(4);
    readonly HashSet<int> spokenUnitNumbersThisRun = new HashSet<int>();
    readonly ShopGradePoolLock gradePoolLock = new ShopGradePoolLock();
    ShopGradePoolPanel gradePoolPanel;
    bool hasTriggeredFirstAutoDialogue;

    /// <summary>자동 새로고침 용병 대사 연출이 끝났을 때 (대사 없음 포함).</summary>
    public event System.Action MercenaryDialogueFinished;

    const float MercenaryDialogueVisibleDuration = 3f;
    const float MercenaryDialogueStagger = 0.18f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            gradePoolLock.Reset();
            InitializeUnits();
            refreshEffect = GetComponent<ShopRefreshEffect>();
            if (refreshEffect == null)
            {
                refreshEffect = gameObject.AddComponent<ShopRefreshEffect>();
            }
            GameLocalizationCoordinator.Register(RefreshLocalizedShopUi);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            GameLocalizationCoordinator.Unregister(RefreshLocalizedShopUi);
            Instance = null;
        }
    }

    void RefreshLocalizedShopUi()
    {
        RefreshGradePoolPanel();
        RefreshShopSlotLocalizedLabels();
        RefreshShopTraitLocalizedLabels();
    }

    void RefreshShopTraitLocalizedLabels()
    {
        Font font = UIFontProvider.Get();
        for (int i = 0; i < currentShopItems.Count; i++)
        {
            GameObject item = currentShopItems[i];
            if (item == null) continue;
            Transform traitsRoot = item.transform.Find("Traits");
            if (traitsRoot == null) continue;
            for (int r = 0; r < traitsRoot.childCount; r++)
            {
                Transform row = traitsRoot.GetChild(r);
                Transform nameTransform = row.Find("Name");
                if (nameTransform == null) continue;
                Text nameText = nameTransform.GetComponent<Text>();
                if (nameText == null) continue;
                nameText.font = font;
                Transform iconTransform = row.Find("Icon");
                if (iconTransform != null && row.name.StartsWith("TraitRow_"))
                {
                    int rowIndex = 0;
                    int.TryParse(row.name.Substring("TraitRow_".Length), out rowIndex);
                    if (i < currentShopUnitNumbers.Count)
                    {
                        string[] traits = UnitTraitData.GetTraits(currentShopUnitNumbers[i]);
                        if (traits != null && rowIndex >= 0 && rowIndex < traits.Length)
                        {
                            nameText.text = GameLocalization.GetTraitDisplayName(traits[rowIndex]);
                        }
                    }
                }
            }
        }
    }

    void RefreshShopSlotLocalizedLabels()
    {
        Font font = UIFontProvider.Get();
        for (int i = 0; i < currentShopItems.Count; i++)
        {
            GameObject item = currentShopItems[i];
            if (item == null) continue;
            Transform actionTransform = item.transform.Find("ActionLabel");
            if (actionTransform == null) continue;
            Text actionText = actionTransform.GetComponent<Text>();
            if (actionText == null) continue;
            actionText.font = font;
            if (!string.IsNullOrEmpty(actionText.text))
            {
                actionText.text = GameLocalization.ShopEvolve;
            }
        }
    }
    
    void Start()
    {
        boardManager = FindFirstObjectByType<BoardManager>();
        if (shopPanel == null)
        {
            GameSceneController controller = FindFirstObjectByType<GameSceneController>();
            controller?.EnsureShopUiCreated();
        }
        EnsureShopVisible();
    }

    void Update()
    {
        if (shopTooltipRoot == null || !shopTooltipRoot.activeSelf) return;

        if (SkillManager.Instance != null && SkillManager.Instance.IsSkillArmed)
        {
            HideShopTooltip();
            return;
        }

        if (!IsPointerOverShopSlot())
        {
            HideShopTooltip();
            return;
        }

        PlaceShopTooltip(GetPointerScreenPosition());
    }

    public void HideActiveTooltip()
    {
        HideShopTooltip();
    }

    bool IsPointerOverShopSlot()
    {
        if (shopPanel == null || !shopPanel.activeSelf) return false;

        Vector2 screenPos = GetPointerScreenPosition();
        Canvas canvas = shopPanel.GetComponentInParent<Canvas>();
        Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        for (int i = 0; i < currentShopItems.Count; i++)
        {
            GameObject item = currentShopItems[i];
            if (item == null || !item.activeInHierarchy) continue;

            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, uiCam))
            {
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// 28개의 유닛 데이터를 초기화합니다
    /// </summary>
    void InitializeUnits()
    {
        allUnits.Clear();
        
        // 1~6: 5등급
        for (int i = 1; i <= 6; i++)
        {
            if (i == 4)
            {
                continue;
            }

            allUnits.Add(new UnitData(i, 5, string.Empty));
        }
        
        // 7~12: 4등급
        for (int i = 7; i <= 12; i++)
        {
            allUnits.Add(new UnitData(i, 4, string.Empty));
        }
        
        // 13~18: 3등급
        for (int i = 13; i <= 18; i++)
        {
            allUnits.Add(new UnitData(i, 3, string.Empty));
        }
        
        // 19~23: 2등급
        for (int i = 19; i <= 23; i++)
        {
            allUnits.Add(new UnitData(i, 2, string.Empty));
        }
        
        // 24~28: 1등급
        for (int i = 24; i <= 28; i++)
        {
            // 26 수상할 정도로 완벽한 펭귄 — 상점 미노출 (부활: 아래 if·continue 제거 후 Add 주석 해제)
            if (i == 26)
            {
                // allUnits.Add(new UnitData(26, 1, string.Empty));
                continue;
            }

            allUnits.Add(new UnitData(i, 1, string.Empty));
        }
        
        Debug.Log($"총 {allUnits.Count}개의 유닛 데이터를 초기화했습니다.");
    }
    
    /// <summary>
    /// 상점 패널을 표시합니다 (상시 노출, 게임 일시정지 없음)
    /// </summary>
    public void EnsureShopVisible()
    {
        if (shopPanel == null) return;

        Transform shopRoot = shopPanel.transform.parent;
        Canvas rootCanvas = FindRootOverlayCanvas();

        if (rootCanvas != null)
        {
            if (shopRoot != null && shopRoot.parent != rootCanvas.transform)
            {
                shopRoot.SetParent(rootCanvas.transform, false);
            }
            else if (shopRoot == null && shopPanel.transform.parent != rootCanvas.transform)
            {
                shopPanel.transform.SetParent(rootCanvas.transform, false);
            }
        }

        if (shopRoot != null)
        {
            shopRoot.gameObject.SetActive(true);
        }

        if (!shopPanel.activeSelf)
        {
            shopPanel.SetActive(true);
        }

        CanvasGroup group = shopPanel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        Canvas shopLayerCanvas = shopRoot != null ? shopRoot.GetComponent<Canvas>() : shopPanel.GetComponent<Canvas>();
        if (shopLayerCanvas != null)
        {
            shopLayerCanvas.overrideSorting = true;
            shopLayerCanvas.sortingOrder = ShopCanvasSortingOrder;
        }

        GameSceneController sceneController = FindFirstObjectByType<GameSceneController>();
        if (sceneController != null)
        {
            sceneController.PlacePlayerLevelHudBehindShop(rootCanvas);
            sceneController.PlaceLevelNameBackgroundBehindUiDown();
        }

        if (shopRoot != null)
        {
            shopRoot.SetAsLastSibling();
        }
        else
        {
            shopPanel.transform.SetAsLastSibling();
        }

        RefreshGradePoolPanel();
    }
    
    /// <summary>
    /// 상점을 엽니다 (패널 표시 + 새로고침, 게임은 계속 진행)
    /// </summary>
    public void OpenShop()
    {
        EnsureShopVisible();
        RefreshShop(ShopRefreshSource.Auto);
    }

    /// <summary>
    /// 라운드 클리어 후 쉬는 시간 — 상점 패널을 자동으로 표시하고 상품을 갱신합니다.
    /// </summary>
    public void OpenBreakShop()
    {
        EnsureShopVisible();
        RefreshShop(ShopRefreshSource.Auto);
    }
    
    /// <summary>
    /// 상점 상품을 새로고침합니다 (골드 2개 필요, 패널은 닫지 않음)
    /// </summary>
    public bool TryOpenShop()
    {
        if (GameManager.Instance == null) return false;

        if (GameManager.Instance.IsCombatActive())
        {
            ShowShopNotice(GameLocalization.ShopRefreshDuringCombat);
            return false;
        }
        
        if (GameManager.Instance.SpendGold(2))
        {
            EnsureShopVisible();
            RefreshShop(ShopRefreshSource.Manual);
            return true;
        }
        
        Debug.Log("골드가 부족합니다! (2골드 필요)");
        return false;
    }
    
    /// <summary>
    /// 상점을 닫습니다
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            
            // 게임 시간 재개
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetShopOpen(false);
            }
        }
    }
    
    /// <summary>
    /// 상점이 열려있는지 확인합니다
    /// </summary>
    public bool IsShopOpen()
    {
        return shopPanel != null && shopPanel.activeSelf;
    }
    
    /// <summary>
    /// 상점을 새로고침합니다 (레벨 기반 확률로 랜덤 유닛 표시)
    /// </summary>
    public void RefreshShop(ShopRefreshSource source = ShopRefreshSource.Auto)
    {
        if (mercenaryDialogueRoutine != null)
        {
            StopCoroutine(mercenaryDialogueRoutine);
            mercenaryDialogueRoutine = null;
        }

        List<int> rosterBeforeRefresh = new List<int>(currentShopUnitNumbers);

        if (refreshShopRoutine != null)
        {
            StopCoroutine(refreshShopRoutine);
            refreshShopRoutine = null;
        }

        if (refreshEffect != null && refreshEffect.isActiveAndEnabled && shopSlotsParent != null)
        {
            refreshShopRoutine = StartCoroutine(RefreshShopWithEffect(source, rosterBeforeRefresh));
            return;
        }

        RefreshShopImmediate();
        mercenaryDialogueRoutine = StartCoroutine(PlayMercenaryDialoguesAfterDelay(source, rosterBeforeRefresh, 0f));
    }

    IEnumerator RefreshShopWithEffect(ShopRefreshSource source, List<int> rosterBeforeRefresh)
    {
        if (allUnits.Count == 0)
        {
            InitializeUnits();
        }

        yield return AnimateShopSlotsOut();

        RefreshShopImmediate();
        refreshEffect.PlayReveal(shopSlotsParent, currentShopItems);

        float revealWait = ShopRefreshEffect.GetRevealDuration(currentShopItems.Count);
        mercenaryDialogueRoutine = StartCoroutine(PlayMercenaryDialoguesAfterDelay(source, rosterBeforeRefresh, revealWait));
        refreshShopRoutine = null;
    }

    void RefreshShopImmediate()
    {
        if (allUnits.Count == 0)
        {
            InitializeUnits();
        }

        ClearShop();

        int playerLevel = 1;
        if (GameManager.Instance != null)
        {
            playerLevel = GameManager.Instance.playerLevel;
        }

        for (int i = 0; i < shopSlots; i++)
        {
            UnitData selectedUnit = SelectUnitByLevel(playerLevel);
            if (selectedUnit != null)
            {
                CreateShopItem(selectedUnit, i);
            }
        }

        RefreshGradePoolPanel();
    }

    IEnumerator AnimateShopSlotsOut()
    {
        if (currentShopItems.Count == 0)
        {
            yield break;
        }

        const float duration = 0.12f;
        float elapsed = 0f;
        int count = currentShopItems.Count;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(1f, 0.75f, t);
            float alpha = Mathf.Lerp(1f, 0.35f, t);

            for (int i = 0; i < count; i++)
            {
                GameObject item = currentShopItems[i];
                if (item == null) continue;

                item.transform.localScale = Vector3.one * scale;
                ApplyShopItemAlpha(item, alpha);
            }

            yield return null;
        }

        ClearShop();
    }

    static void ApplyShopItemAlpha(GameObject shopItem, float alpha)
    {
        if (shopItem == null) return;

        Graphic[] graphics = shopItem.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null) continue;
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
    
    /// <summary>
    /// 레벨 기반 확률로 유닛을 선택합니다
    /// </summary>
    UnitData SelectUnitByLevel(int level)
    {
        // 레벨에 따라 등급 선택
        int selectedGrade = LevelGradeProbability.SelectGradeByLevel(level);
        
        // 해당 등급의 유닛들만 필터링 (최대 진화 달성 유닛 타입 제외)
        List<UnitData> gradeUnits = FilterShopCandidates(selectedGrade);
        
        // 해당 등급의 유닛이 없으면 1등급 유닛 사용
        if (gradeUnits.Count == 0)
        {
            gradeUnits = FilterShopCandidates(1);
        }
        
        // 해당 등급 내에서 랜덤 선택 (동일 확률)
        if (gradeUnits.Count > 0)
        {
            int randomIndex = Random.Range(0, gradeUnits.Count);
            return gradeUnits[randomIndex];
        }
        
        // 기본값 (첫 번째 유닛)
        return allUnits.Count > 0 ? allUnits[0] : null;
    }
    
    /// <summary>
    /// 상점 아이템을 생성합니다 (큰 패널 형태)
    /// </summary>
    void CreateShopItem(UnitData unitData, int slotIndex)
    {
        if (shopSlotsParent == null) return;
        
        GameObject shopItemObj = new GameObject($"ShopItem_{slotIndex}");
        shopItemObj.transform.SetParent(shopSlotsParent, false);
        
        // RectTransform 설정
        RectTransform rectTransform = shopItemObj.AddComponent<RectTransform>();
        Sprite gradeBackSprite = GetGradeBackSprite();
        Vector2 slotSize = gradeBackSprite != null
            ? new Vector2(gradeBackSprite.rect.width, gradeBackSprite.rect.height)
            : new Vector2(160f, 160f);
        rectTransform.sizeDelta = slotSize;

        float spacing = 0f;
        float totalWidth = (slotSize.x * shopSlots) + (spacing * (shopSlots - 1));
        float startX = -totalWidth * 0.5f + (slotSize.x * 0.5f);
        float halfSlotRightOffset = slotSize.x * 0.5f;
        rectTransform.anchoredPosition = new Vector2(startX + slotIndex * (slotSize.x + spacing) + halfSlotRightOffset, 0); // 가로로 배치(반 슬롯 우측 이동)
        
        // 배경 이미지 (임시 리소스)
        Image backgroundImage = shopItemObj.AddComponent<Image>();
        backgroundImage.sprite = gradeBackSprite != null ? gradeBackSprite : GetFallbackUnitSprite();
        Color gradeFrameTint = GetGradeFrameTint(unitData.grade);
        if (gradeBackSprite != null)
        {
            backgroundImage.color = gradeFrameTint;
            backgroundImage.SetNativeSize();
        }
        else
        {
            backgroundImage.color = new Color(0f, 0f, 0f, 0.8f);
        }
        
        Character ownedForSlot = FindOwnedCharacter(unitData.unitNumber);
        int displayEvolutionLevel = ownedForSlot != null ? ownedForSlot.evolutionLevel : 1;
        bool isOwned = ownedForSlot != null;

        // 버튼 컴포넌트 추가 (클릭 감지용)
        Button button = shopItemObj.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = backgroundImage;

        if (!TryAddShopUnitVisual(shopItemObj.transform, unitData.unitNumber, slotSize, gradeFrameTint))
        {
            Debug.LogWarning($"상점 유닛 미리보기 실패: unit_{unitData.unitNumber:000}");
        }

        // 상단 마크 이미지 (유닛 이미지 위)
        Sprite gradeMarkSprite = Resources.Load<Sprite>("1_grade_back_mark");
        GameObject markObj = null;
        if (gradeMarkSprite != null)
        {
            markObj = new GameObject("GradeMark");
            markObj.transform.SetParent(shopItemObj.transform, false);
            Image markImage = markObj.AddComponent<Image>();
            markImage.sprite = gradeMarkSprite;
            markImage.color = gradeFrameTint;
            markImage.SetNativeSize();

            RectTransform markRect = markObj.GetComponent<RectTransform>();
            markRect.anchorMin = new Vector2(0.5f, 0.5f);
            markRect.anchorMax = new Vector2(0.5f, 0.5f);
            markRect.pivot = new Vector2(0.5f, 0.5f);
            markRect.anchoredPosition = new Vector2(0f, -67f);
        }
        
        GameObject traitsRoot = CreateShopTraitDisplay(shopItemObj.transform, unitData.unitNumber);

        // 유닛 이름 텍스트 (슬롯 하단)
        GameObject nameObj = new GameObject("UnitName");
        nameObj.transform.SetParent(shopItemObj.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = UnitTraitData.GetDisplayName(unitData.unitNumber);
        nameText.font = UIFontProvider.Get();
        nameText.fontSize = 22;
        nameText.color = UnitTraitData.GetDisplayNameColor(unitData.unitNumber);
        nameText.alignment = TextAnchor.LowerCenter;

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0f);
        nameRect.anchorMax = new Vector2(0.5f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, 4f);
        nameRect.sizeDelta = new Vector2(170f, 24f);

        GameObject actionObj = new GameObject("ActionLabel");
        actionObj.transform.SetParent(shopItemObj.transform, false);
        Text actionText = actionObj.AddComponent<Text>();
        int unitPrice = GetUnitPriceByGrade(unitData.grade);
        actionText.text = isOwned ? GameLocalization.ShopEvolve : "";
        actionText.font = UIFontProvider.Get();
        actionText.fontSize = 20;
        actionText.color = new Color(1f, 0.92f, 0.35f, 1f);
        actionText.alignment = TextAnchor.MiddleCenter;
        actionText.raycastTarget = false;

        RectTransform actionRect = actionObj.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.5f, 0f);
        actionRect.anchorMax = new Vector2(0.5f, 0f);
        actionRect.pivot = new Vector2(0.5f, 0f);
        actionRect.anchoredPosition = new Vector2(0f, 16f);
        actionRect.sizeDelta = new Vector2(160f, 22f);

        // 슬롯 하단에 가격(골드 아이콘 + 수치) 표시
        GameObject costRowObj = new GameObject("CostRow");
        costRowObj.transform.SetParent(shopItemObj.transform, false);
        RectTransform costRowRect = costRowObj.AddComponent<RectTransform>();
        costRowRect.anchorMin = new Vector2(0.5f, 0f);
        costRowRect.anchorMax = new Vector2(0.5f, 0f);
        costRowRect.pivot = new Vector2(0.5f, 0f);
        costRowRect.anchoredPosition = new Vector2(0f, 60f); // 현재 위치에서 +20
        costRowRect.sizeDelta = new Vector2(120f, 20f);

        GameObject costBgObj = new GameObject("CostBackground");
        costBgObj.transform.SetParent(costRowObj.transform, false);
        Image costBgImage = costBgObj.AddComponent<Image>();
        Sprite costBgSprite = Resources.Load<Sprite>("button_red");
        if (costBgSprite != null)
        {
            costBgImage.sprite = costBgSprite;
            costBgImage.color = Color.white;
        }
        else
        {
            costBgImage.sprite = GetFallbackUnitSprite();
            costBgImage.color = new Color(0.35f, 0.12f, 0.12f, 0.95f);
        }
        costBgImage.raycastTarget = false;

        RectTransform costBgRect = costBgObj.GetComponent<RectTransform>();
        costBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        costBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        costBgRect.pivot = new Vector2(0.5f, 0.5f);
        costBgRect.anchoredPosition = Vector2.zero;
        if (costBgSprite != null)
        {
            float bgWidth = Mathf.Clamp(costBgSprite.rect.width * 0.42f, 72f, 110f);
            float bgHeight = Mathf.Clamp(costBgSprite.rect.height * 0.42f, 18f, 28f);
            costBgRect.sizeDelta = new Vector2(bgWidth, bgHeight);
        }
        else
        {
            costBgRect.sizeDelta = new Vector2(86f, 22f);
        }
        costBgObj.transform.SetAsFirstSibling();

        GameObject costIconObj = new GameObject("CostIcon");
        costIconObj.transform.SetParent(costRowObj.transform, false);
        Image costIconImage = costIconObj.AddComponent<Image>();
        Sprite costIconSprite = Resources.Load<Sprite>("gold");
        if (costIconSprite != null)
        {
            costIconImage.sprite = costIconSprite;
            costIconImage.color = Color.white;
            costIconImage.SetNativeSize();
        }
        else
        {
            costIconImage.sprite = GetFallbackUnitSprite();
            costIconImage.color = new Color(1f, 0.9f, 0.2f, 1f);
        }
        costIconImage.raycastTarget = false;

        RectTransform costIconRect = costIconObj.GetComponent<RectTransform>();
        costIconRect.anchorMin = new Vector2(0f, 0.5f);
        costIconRect.anchorMax = new Vector2(0f, 0.5f);
        costIconRect.pivot = new Vector2(0f, 0.5f);
        costIconRect.anchoredPosition = new Vector2(28f, 0f);
        if (costIconSprite != null)
        {
            costIconRect.sizeDelta = costIconRect.sizeDelta / 6f;
        }
        float costTextLeft = costIconRect.anchoredPosition.x + costIconRect.sizeDelta.x + 4f;

        GameObject costTextObj = new GameObject("CostText");
        costTextObj.transform.SetParent(costRowObj.transform, false);
        Text costText = costTextObj.AddComponent<Text>();
        costText.text = unitPrice.ToString();
        costText.font = UIFontProvider.Get();
        costText.fontSize = 18;
        costText.color = Color.white;
        costText.alignment = TextAnchor.MiddleLeft;
        costText.raycastTarget = false;

        RectTransform costTextRect = costTextObj.GetComponent<RectTransform>();
        costTextRect.anchorMin = new Vector2(0f, 0f);
        costTextRect.anchorMax = new Vector2(1f, 1f);
        costTextRect.pivot = new Vector2(0f, 0.5f);
        costTextRect.anchoredPosition = Vector2.zero;
        costTextRect.offsetMin = new Vector2(costTextLeft, 0f);
        costTextRect.offsetMax = Vector2.zero;
        costTextRect.sizeDelta = Vector2.zero;

        if (markObj != null)
        {
            markObj.transform.SetAsLastSibling();
        }

        if (traitsRoot != null)
        {
            traitsRoot.transform.SetAsLastSibling();
        }

        // 비용/이름/행동 라벨은 마크 존재 여부와 관계없이 항상 전면 배치
        nameObj.transform.SetAsLastSibling();
        costRowObj.transform.SetAsLastSibling();
        actionObj.transform.SetAsLastSibling();
        
        // 버튼 클릭 이벤트 연결
        button.onClick.AddListener(() => OnShopSlotClicked(unitData, shopItemObj));

        SetupShopSlotHover(shopItemObj, unitData, displayEvolutionLevel);

        ShopSlotUnitRef slotRef = shopItemObj.AddComponent<ShopSlotUnitRef>();
        slotRef.unitNumber = unitData.unitNumber;
        
        currentShopItems.Add(shopItemObj);
        currentShopUnitNumbers.Add(unitData.unitNumber);
    }

    void SetupShopSlotHover(GameObject shopItemObj, UnitData unitData, int evolutionLevel)
    {
        EventTrigger trigger = shopItemObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = shopItemObj.AddComponent<EventTrigger>();
        }

        RectTransform slotRect = shopItemObj.GetComponent<RectTransform>();
        Graphic unitGraphic = FindShopUnitGraphic(shopItemObj);

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (unitGraphic != null)
            {
                UiSilhouetteOutline.SetTarget(shopItemObj.transform, unitGraphic);
            }
            ShowShopTooltip(unitData, evolutionLevel, slotRect);
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            UiSilhouetteOutline.Clear(shopItemObj.transform);
            HideShopTooltip();
        });
        trigger.triggers.Add(exit);

        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ =>
        {
            if (GameManager.Instance != null && GameManager.Instance.IsCombatActive())
            {
                ShowShopNotice(GameLocalization.ShopBuyDuringCombat);
            }
        });
        trigger.triggers.Add(down);
    }

    static Graphic FindShopUnitGraphic(GameObject shopItemObj)
    {
        if (shopItemObj == null)
        {
            return null;
        }

        RawImage rawImage = shopItemObj.GetComponentInChildren<RawImage>(true);
        if (rawImage != null)
        {
            return rawImage;
        }

        Image[] images = shopItemObj.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].gameObject.name == "UnitImage")
            {
                return images[i];
            }
        }

        return null;
    }

    void OnShopSlotClicked(UnitData unitData, GameObject shopItem)
    {
        if (unitData == null) return;

        if (GameManager.Instance != null && GameManager.Instance.IsCombatActive())
        {
            ShowShopNotice(GameLocalization.ShopBuyDuringCombat);
            return;
        }

        BuyUnit(unitData, shopItem);
    }

    void EnsureShopTooltip()
    {
        if (shopTooltipRoot != null) return;

        shopOverlayCanvas = FindRootOverlayCanvas();
        Transform parent = shopOverlayCanvas != null
            ? shopOverlayCanvas.transform
            : (shopPanel != null ? shopPanel.transform.parent : transform);

        shopTooltipRoot = new GameObject("ShopTooltip");
        shopTooltipRoot.transform.SetParent(parent, false);

        Canvas tooltipCanvas = shopTooltipRoot.AddComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = ShopTooltipSortingOrder;
        shopTooltipRoot.AddComponent<GraphicRaycaster>();

        Image bg = shopTooltipRoot.AddComponent<Image>();
        TooltipTheme.ApplyStandardBackground(bg, true);

        shopTooltipRect = shopTooltipRoot.GetComponent<RectTransform>();
        shopTooltipRect.sizeDelta = new Vector2(320f, 120f);
        shopTooltipRect.pivot = new Vector2(0f, 1f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(shopTooltipRoot.transform, false);
        shopTooltipText = textObj.AddComponent<Text>();
        shopTooltipText.font = UIFontProvider.Get();
        shopTooltipText.fontSize = 18;
        shopTooltipText.color = Color.white;
        shopTooltipText.supportRichText = true;
        shopTooltipText.alignment = TextAnchor.UpperLeft;
        shopTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        shopTooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        shopTooltipText.raycastTarget = false;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 10f);
        textRect.offsetMax = new Vector2(-12f, -10f);

        shopTooltipRoot.SetActive(false);
    }

    void ApplyShopTooltipSorting()
    {
        if (shopTooltipRoot == null) return;

        Canvas tooltipCanvas = shopTooltipRoot.GetComponent<Canvas>();
        if (tooltipCanvas == null) return;

        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = ShopTooltipSortingOrder;
    }

    void ShowShopTooltip(UnitData unitData, int evolutionLevel, RectTransform anchor)
    {
        if (unitData == null || anchor == null) return;

        EnsureShopTooltip();
        ApplyShopTooltipSorting();
        shopTooltipText.text = UnitCombatStats.BuildShopTooltipText(unitData, evolutionLevel);
        LayoutShopTooltip();
        PlaceShopTooltip(GetPointerScreenPosition());

        shopTooltipRoot.SetActive(true);
        shopTooltipRoot.transform.SetAsLastSibling();
    }

    static Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if (touch != null)
        {
            return touch.primaryTouch.position.ReadValue();
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            return mouse.position.ReadValue();
        }
#endif
        return Input.mousePosition;
    }

    static Canvas FindRootOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas namedUi = null;
        Canvas best = null;
        float bestArea = -1f;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            if (!c.isRootCanvas) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;

            if (c.gameObject.name == "UI")
            {
                namedUi = c;
            }

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

        if (namedUi != null)
        {
            return namedUi;
        }

        return best != null ? best : FindFirstObjectByType<Canvas>();
    }

    void PlaceShopTooltip(Vector2 screenPos)
    {
        if (shopTooltipRect == null || shopOverlayCanvas == null) return;

        RectTransform canvasRect = shopOverlayCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Camera uiCam = shopOverlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : shopOverlayCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 localPoint))
        {
            return;
        }

        shopTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        shopTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        shopTooltipRect.pivot = new Vector2(0f, 1f);

        Vector2 size = shopTooltipRect.sizeDelta;
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

        shopTooltipRect.anchoredPosition = pos;
    }

    void LayoutShopTooltip()
    {
        if (shopTooltipRect == null || shopTooltipText == null) return;

        const float tooltipWidth = 320f;
        const float padX = 12f;
        const float padY = 10f;
        const float minHeight = 80f;

        // 텍스트 래핑 폭을 먼저 확정해야 preferredHeight가 정확히 계산됩니다.
        shopTooltipRect.sizeDelta = new Vector2(tooltipWidth, minHeight);
        RectTransform textRect = shopTooltipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(padX, padY);
        textRect.offsetMax = new Vector2(-padX, -padY);

        float contentHeight = shopTooltipText.preferredHeight;
        shopTooltipRect.sizeDelta = new Vector2(tooltipWidth, Mathf.Max(minHeight, contentHeight + padY * 2f));
    }

    void HideShopTooltip()
    {
        if (shopTooltipRoot != null)
        {
            shopTooltipRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 구매된 상점 슬롯만 UI에서 제거합니다 (전체 RefreshShop 없음)
    /// </summary>
    void RemoveShopSlot(GameObject shopItem)
    {
        if (shopItem == null) return;

        currentShopItems.Remove(shopItem);

        RawImage previewImage = shopItem.GetComponentInChildren<RawImage>(true);
        if (previewImage != null && previewImage.texture is RenderTexture renderTexture)
        {
            int textureIndex = previewTextures.IndexOf(renderTexture);
            if (textureIndex >= 0)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                previewTextures.RemoveAt(textureIndex);

                if (textureIndex < previewInstances.Count)
                {
                    if (previewInstances[textureIndex] != null)
                    {
                        Destroy(previewInstances[textureIndex]);
                    }
                    previewInstances.RemoveAt(textureIndex);
                }
            }
        }

        UiSilhouetteOutline.Clear(shopItem.transform);
        Destroy(shopItem);
    }
    
    /// <summary>
    /// 상점 아이템을 모두 제거합니다
    /// </summary>
    void ClearShop()
    {
        HideShopTooltip();

        foreach (GameObject item in currentShopItems)
        {
            if (item != null)
            {
                UiSilhouetteOutline.Clear(item.transform);
                Destroy(item);
            }
        }
        currentShopItems.Clear();
        currentShopUnitNumbers.Clear();

        for (int i = 0; i < previewInstances.Count; i++)
        {
            if (previewInstances[i] != null)
            {
                Destroy(previewInstances[i]);
            }
        }
        previewInstances.Clear();

        for (int i = 0; i < previewTextures.Count; i++)
        {
            if (previewTextures[i] != null)
            {
                previewTextures[i].Release();
                Destroy(previewTextures[i]);
            }
        }
        previewTextures.Clear();
    }

    IEnumerator PlayMercenaryDialoguesAfterDelay(ShopRefreshSource source, List<int> rosterBeforeRefresh, float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

        if (source != ShopRefreshSource.Auto)
        {
            NotifyMercenaryDialogueFinished();
            mercenaryDialogueRoutine = null;
            yield break;
        }

        List<ShopDialogueCandidate> eligible = BuildEligibleMercenaryDialogueCandidates(rosterBeforeRefresh);
        if (eligible.Count == 0)
        {
            NotifyMercenaryDialogueFinished();
            mercenaryDialogueRoutine = null;
            yield break;
        }

        List<ShopDialogueCandidate> speakers = PickMercenarySpeakers(eligible);
        speakers.Sort(CompareMercenarySpeakersByGrade);
        for (int i = 0; i < speakers.Count; i++)
        {
            ShopDialogueCandidate speaker = speakers[i];
            if (i > 0)
            {
                yield return new WaitForSecondsRealtime(MercenaryDialogueStagger);
            }

            if (speaker.slot == null) continue;
            if (!ShopMercenaryDialogue.TryGetLine(speaker.unitNumber, out string line)) continue;

            spokenUnitNumbersThisRun.Add(speaker.unitNumber);
            ShopMercenaryDialogueBubbleUi.Create(speaker.slot.transform, line);
        }

        yield return new WaitForSecondsRealtime(MercenaryDialogueVisibleDuration);
        NotifyMercenaryDialogueFinished();
        mercenaryDialogueRoutine = null;
    }

    void NotifyMercenaryDialogueFinished()
    {
        MercenaryDialogueFinished?.Invoke();
    }

    struct ShopDialogueCandidate
    {
        public GameObject slot;
        public int unitNumber;
    }

    List<ShopDialogueCandidate> BuildEligibleMercenaryDialogueCandidates(List<int> rosterBeforeRefresh)
    {
        var eligible = new List<ShopDialogueCandidate>(shopSlots);
        for (int i = 0; i < currentShopItems.Count; i++)
        {
            GameObject slot = currentShopItems[i];
            if (slot == null) continue;

            ShopSlotUnitRef slotRef = slot.GetComponent<ShopSlotUnitRef>();
            if (slotRef == null || slotRef.unitNumber <= 0) continue;
            if (rosterBeforeRefresh != null && rosterBeforeRefresh.Contains(slotRef.unitNumber)) continue;
            if (spokenUnitNumbersThisRun.Contains(slotRef.unitNumber)) continue;

            eligible.Add(new ShopDialogueCandidate
            {
                slot = slot,
                unitNumber = slotRef.unitNumber,
            });
        }

        return eligible;
    }

    List<ShopDialogueCandidate> PickMercenarySpeakers(List<ShopDialogueCandidate> eligible)
    {
        var pool = new List<ShopDialogueCandidate>(eligible);
        int speakCount = Mathf.Min(pool.Count, Random.Range(1, 3));
        var speakers = new List<ShopDialogueCandidate>(speakCount);

        if (!hasTriggeredFirstAutoDialogue)
        {
            hasTriggeredFirstAutoDialogue = true;
            int preferredUnit = ShopMercenaryDialogue.FirstAutoPreferredUnit;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].unitNumber != preferredUnit) continue;
                speakers.Add(pool[i]);
                pool.RemoveAt(i);
                speakCount--;
                break;
            }
        }

        for (int i = 0; i < speakCount; i++)
        {
            if (pool.Count == 0) break;
            int pickIndex = PickWeightedMercenaryIndex(pool);
            speakers.Add(pool[pickIndex]);
            pool.RemoveAt(pickIndex);
        }

        return speakers;
    }

    static int PickWeightedMercenaryIndex(List<ShopDialogueCandidate> pool)
    {
        if (pool == null || pool.Count == 0) return 0;
        if (pool.Count == 1) return 0;

        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            totalWeight += ShopMercenaryDialogue.GetDialoguePickWeight(pool[i].unitNumber);
        }

        if (totalWeight <= 0)
        {
            return Random.Range(0, pool.Count);
        }

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= ShopMercenaryDialogue.GetDialoguePickWeight(pool[i].unitNumber);
            if (roll < 0)
            {
                return i;
            }
        }

        return pool.Count - 1;
    }

    static int CompareMercenarySpeakersByGrade(ShopDialogueCandidate a, ShopDialogueCandidate b)
    {
        int gradeCompare = UnitCombatStats.GetGradeForUnit(a.unitNumber)
            .CompareTo(UnitCombatStats.GetGradeForUnit(b.unitNumber));
        if (gradeCompare != 0) return gradeCompare;
        return a.unitNumber.CompareTo(b.unitNumber);
    }
    
    /// <summary>
    /// 등급에 따른 색상을 반환합니다
    /// </summary>
    Color GetGradeColor(int grade)
    {
        switch (grade)
        {
            case 5: return new Color(0.7f, 0.7f, 0.7f); // 회색 (5등급)
            case 4: return new Color(0.2f, 0.8f, 0.2f); // 초록색 (4등급)
            case 3: return new Color(0.2f, 0.5f, 1f);   // 파란색 (3등급)
            case 2: return new Color(0.8f, 0.2f, 0.8f); // 보라색 (2등급)
            case 1: return new Color(1f, 0.8f, 0f);     // 금색 (1등급)
            default: return Color.white;
        }
    }

    Color GetGradeFrameTint(int grade)
    {
        switch (grade)
        {
            case 4: return new Color(0.2f, 0.8f, 0.2f); // 초록색
            case 3: return new Color(0.2f, 0.5f, 1f);   // 파란색
            case 2: return new Color(0.8f, 0.2f, 0.8f); // 보라색
            case 1: return new Color(1f, 0.9f, 0.2f);   // 노란색
            case 5:
            default: return Color.white;                // 기본색
        }
    }

    Sprite GetChoicePopupSprite()
    {
        if (cachedChoicePopupSprite != null) return cachedChoicePopupSprite;
        cachedChoicePopupSprite = Resources.Load<Sprite>("choice_popup");
        if (cachedChoicePopupSprite == null)
        {
            Debug.LogWarning("choice_popup.png 스프라이트를 찾지 못했습니다. Assets/Resources/choice_popup.png 확인 필요");
        }
        return cachedChoicePopupSprite;
    }

    Sprite GetGradeBackSprite()
    {
        if (cachedGradeBackSprite != null) return cachedGradeBackSprite;
        cachedGradeBackSprite = Resources.Load<Sprite>("1_grade_back");
        if (cachedGradeBackSprite == null)
        {
            cachedGradeBackSprite = Resources.Load<Sprite>("1_grade_back_0");
        }
        if (cachedGradeBackSprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("1_grade_back");
            if (sprites != null && sprites.Length > 0)
            {
                cachedGradeBackSprite = sprites[0];
            }
        }
        if (cachedGradeBackSprite == null)
        {
            Debug.LogWarning("1_grade_back 스프라이트를 찾지 못했습니다. Assets/Resources/1_grade_back 확인 필요");
        }
        return cachedGradeBackSprite;
    }

    const float ShopTraitRowHeight = 24f;
    const float ShopTraitRowGap = 3f;
    const int ShopTraitFontSize = 20;
    const float ShopTraitIconSize = 22f;

    GameObject CreateShopTraitDisplay(Transform parent, int unitNumber)
    {
        string[] traits = UnitTraitData.GetTraits(unitNumber);

        GameObject traitsRoot = new GameObject("Traits");
        traitsRoot.transform.SetParent(parent, false);
        RectTransform traitsRect = traitsRoot.AddComponent<RectTransform>();
        traitsRect.anchorMin = new Vector2(0.06f, 0.18f);
        traitsRect.anchorMax = new Vector2(0.40f, 0.82f);
        traitsRect.pivot = new Vector2(1f, 0.5f);
        traitsRect.anchoredPosition = new Vector2(-2f, 10f);
        traitsRect.sizeDelta = Vector2.zero;

        if (traits == null || traits.Length == 0)
        {
            CreateShopTraitRow(traitsRoot.transform, GameLocalization.ShopNoTrait, 0, 1, useIcon: false);
            return traitsRoot;
        }

        for (int i = 0; i < traits.Length; i++)
        {
            CreateShopTraitRow(traitsRoot.transform, traits[i], i, traits.Length, useIcon: true);
        }

        return traitsRoot;
    }

    void CreateShopTraitRow(Transform parent, string traitName, int rowIndex, int rowCount, bool useIcon)
    {
        GameObject rowObj = new GameObject($"TraitRow_{rowIndex}");
        rowObj.transform.SetParent(parent, false);
        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(1f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(1f, 0.5f);

        int safeRowCount = Mathf.Max(1, rowCount);
        float rowStep = ShopTraitRowHeight + ShopTraitRowGap;
        float topRowCenterY = (safeRowCount - 1) * rowStep * 0.5f;
        rowRect.anchoredPosition = new Vector2(0f, topRowCenterY - rowIndex * rowStep);
        rowRect.sizeDelta = new Vector2(108f, ShopTraitRowHeight);

        Image rowBg = rowObj.AddComponent<Image>();
        Sprite rowBgSprite = Resources.Load<Sprite>("button_red");
        if (rowBgSprite != null)
        {
            rowBg.sprite = rowBgSprite;
            rowBg.type = Image.Type.Sliced;
            rowBg.color = new Color(0.12f, 0.14f, 0.2f, 0.55f);
        }
        else
        {
            rowBg.color = new Color(0f, 0f, 0f, 0.35f);
        }
        rowBg.raycastTarget = false;

        float textRightPad = useIcon ? ShopTraitIconSize + 6f : 4f;

        GameObject textObj = new GameObject("Name");
        textObj.transform.SetParent(rowObj.transform, false);
        Text nameText = textObj.AddComponent<Text>();
        nameText.text = useIcon ? GameLocalization.GetTraitDisplayName(traitName) : traitName;
        nameText.font = UIFontProvider.Get();
        nameText.fontSize = ShopTraitFontSize;
        nameText.color = GetShopTraitTextColor(traitName);
        nameText.alignment = TextAnchor.MiddleRight;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.raycastTarget = false;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 0f);
        textRect.offsetMax = new Vector2(-textRightPad, 0f);

        if (!useIcon)
        {
            return;
        }

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(rowObj.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = TraitIconFactory.Get(traitName);
        icon.preserveAspect = true;
        icon.color = Color.white;
        icon.raycastTarget = false;

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-4f, 0f);
        iconRect.sizeDelta = new Vector2(ShopTraitIconSize, ShopTraitIconSize);
    }

    public static Color GetShopTraitTextColor(string traitName)
    {
        switch (traitName)
        {
            case "자연": return new Color(0.55f, 0.95f, 0.45f);
            case "불": return new Color(1f, 0.55f, 0.35f);
            case "얼음": return new Color(0.6f, 0.9f, 1f);
            case "번개": return new Color(1f, 0.95f, 0.45f);
            case "바람": return new Color(0.65f, 1f, 0.85f);
            case "빛": return new Color(1f, 0.98f, 0.75f);
            case "어둠": return new Color(0.75f, 0.6f, 0.95f);
            case "도깨비": return new Color(0.45f, 0.95f, 0.6f);
            case "마법사": return new Color(0.6f, 0.75f, 1f);
            case "기사단": return new Color(0.9f, 0.9f, 0.95f);
            case "재앙": return new Color(0.95f, 0.45f, 0.5f);
            case "악마": return new Color(0.9f, 0.45f, 0.8f);
            case "파수꾼": return new Color(0.5f, 0.85f, 0.85f);
            case "도적단": return new Color(0.9f, 0.75f, 0.45f);
            case "펭귄": return new Color(0.7f, 0.95f, 1f);
            default: return new Color(0.88f, 0.9f, 0.95f);
        }
    }

    bool TryAddShopUnitVisual(Transform shopItemParent, int unitNumber, Vector2 slotSize, Color gradeGlowTint, bool centered = false)
    {
        bool hasSpumPrefab = GetUnitPrefab(unitNumber) != null;

        RenderTexture previewTexture = CreateUnitPreviewTexture(unitNumber, slotSize);
        if (previewTexture != null)
        {
            return AddShopUnitPreviewImage(shopItemParent, previewTexture, gradeGlowTint, centered);
        }

        if (hasSpumPrefab)
        {
            Debug.LogWarning($"[Shop] SPUM 미리보기 실패 — unit_{unitNumber:000}, 정적 아이콘으로 대체 시도");
        }

        Sprite portraitSprite = GetShopPortraitSprite(unitNumber);
        if (portraitSprite != null)
        {
            return AddShopUnitSpriteImage(shopItemParent, portraitSprite, gradeGlowTint, centered);
        }

        Sprite unitSprite = GetUnitSpriteFromPrefab(unitNumber);
        if (unitSprite != null)
        {
            return AddShopUnitSpriteImage(shopItemParent, unitSprite, gradeGlowTint, centered);
        }

        return false;
    }

    static Sprite shopUnitGlowSprite;

    static Sprite GetShopUnitGlowSprite()
    {
        if (shopUnitGlowSprite != null) return shopUnitGlowSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.42f));
            }
        }

        texture.Apply();
        shopUnitGlowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return shopUnitGlowSprite;
    }

    static void CreateShopUnitGlow(Transform parent, Color glowTint, float alpha = 0.5f, float size = 132f)
    {
        GameObject glowObj = new GameObject("UnitGlow");
        glowObj.transform.SetParent(parent, false);
        glowObj.transform.SetAsFirstSibling();

        Image glowImage = glowObj.AddComponent<Image>();
        glowImage.sprite = GetShopUnitGlowSprite();
        glowImage.color = new Color(glowTint.r, glowTint.g, glowTint.b, alpha);
        glowImage.raycastTarget = false;

        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = new Vector2(size, size);
    }

    static bool AddShopUnitPreviewImage(Transform parent, RenderTexture texture, Color gradeGlowTint, bool centered = false)
    {
        if (parent == null || texture == null) return false;

        RectTransform container = CreateShopUnitVisualContainer(parent, gradeGlowTint, centered);
        GameObject unitImageObj = new GameObject("UnitPreview");
        unitImageObj.transform.SetParent(container, false);
        RawImage unitImage = unitImageObj.AddComponent<RawImage>();
        unitImage.texture = texture;
        unitImage.color = Color.white;
        unitImage.raycastTarget = false;

        float aspect = (float)texture.width / Mathf.Max(1, texture.height);
        ApplyShopUnitImageFitRect(unitImageObj.GetComponent<RectTransform>(), aspect);
        return true;
    }

    static bool AddShopUnitSpriteImage(Transform parent, Sprite sprite, Color gradeGlowTint, bool centered = false)
    {
        if (parent == null || sprite == null) return false;

        RectTransform container = CreateShopUnitVisualContainer(parent, gradeGlowTint, centered);
        GameObject unitImageObj = new GameObject("UnitImage");
        unitImageObj.transform.SetParent(container, false);
        Image unitImage = unitImageObj.AddComponent<Image>();
        unitImage.sprite = sprite;
        unitImage.color = Color.white;
        unitImage.preserveAspect = true;

        float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        ApplyShopUnitImageFitRect(unitImageObj.GetComponent<RectTransform>(), aspect);
        return true;
    }

    static RectTransform CreateShopUnitVisualContainer(Transform parent, Color gradeGlowTint, bool centered = false)
    {
        GameObject containerObj = new GameObject("UnitVisualArea");
        containerObj.transform.SetParent(parent, false);
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();

        if (centered)
        {
            // 레벨업/시작 선택 카드: 초상화 프레임 전체에 중앙 정렬
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.offsetMin = new Vector2(4f, 4f);
            containerRect.offsetMax = new Vector2(-4f, -4f);
            CreateShopUnitGlow(containerObj.transform, gradeGlowTint, 0.30f, 148f);
        }
        else
        {
            containerRect.anchorMin = new Vector2(0.42f, 0.2f);
            containerRect.anchorMax = new Vector2(0.94f, 0.85f);
            containerRect.pivot = new Vector2(1f, 0.5f);
            containerRect.anchoredPosition = new Vector2(-4f, 0f);
            containerRect.sizeDelta = Vector2.zero;
            CreateShopUnitGlow(containerObj.transform, gradeGlowTint);
        }

        return containerRect;
    }

    static void ApplyShopUnitImageFitRect(RectTransform unitRect, float aspectRatio)
    {
        if (unitRect == null) return;

        unitRect.anchorMin = Vector2.zero;
        unitRect.anchorMax = Vector2.one;
        unitRect.pivot = new Vector2(0.5f, 0.5f);
        unitRect.anchoredPosition = Vector2.zero;
        unitRect.sizeDelta = Vector2.zero;
        unitRect.localScale = Vector3.one;

        AspectRatioFitter fitter = unitRect.GetComponent<AspectRatioFitter>();
        if (fitter == null)
        {
            fitter = unitRect.gameObject.AddComponent<AspectRatioFitter>();
        }

        fitter.aspectRatio = Mathf.Max(0.01f, aspectRatio);
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
    }

    static float GetInGameCellSizeStatic()
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board != null)
        {
            return board.cellSize;
        }

        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        return controller != null ? controller.cellSize : 0.75f;
    }

    float GetInGameCellSize()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }
        if (boardManager != null)
        {
            return boardManager.cellSize;
        }

        return GetInGameCellSizeStatic();
    }

    static float ComputeShopPreviewOrthographicSize(Bounds bounds, int width, int height)
    {
        float padding = 1.05f;
        float aspect = (float)width / Mathf.Max(1, height);
        float verticalSize = bounds.extents.y * padding;
        float horizontalSize = bounds.extents.x / aspect * padding;
        return Mathf.Max(verticalSize, horizontalSize);
    }

    Vector3 GetInGameShopPreviewLocalScale(int unitNumber, float cellSize)
    {
        float scale = cellSize;
        if (unitNumber != 2)
        {
            scale *= 1.5f;
        }

        return new Vector3(-scale, scale, scale);
    }

    Sprite GetUnitSprite(int unitNumber)
    {
        if (cachedUnitSprites.TryGetValue(unitNumber, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite sprite = GetShopPortraitSprite(unitNumber);
        if (sprite == null)
        {
            sprite = GetUnitSpriteFromPrefab(unitNumber);
        }

        cachedUnitSprites[unitNumber] = sprite;
        return sprite;
    }

    Sprite GetShopPortraitSprite(int unitNumber)
    {
        foreach (string path in GetShopPortraitResourcePaths(unitNumber))
        {
            Sprite sprite = TryLoadFirstSprite(path);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    static IEnumerable<string> GetShopPortraitResourcePaths(int unitNumber)
    {
        string padded = unitNumber.ToString("000");
        yield return $"unit_{padded}";
        yield return $"unit_{unitNumber}";

        if (unitNumber == 26)
        {
            yield return "unit_026";
        }

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

    Sprite GetUnitSpriteFromPrefab(int unitNumber)
    {
        GameObject prefab = GetUnitPrefab(unitNumber);
        if (prefab == null)
        {
            return null;
        }

        SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        Sprite bestSprite = null;
        float bestArea = 0f;
        int bestSortingOrder = int.MinValue;

        bool IsExcluded(SpriteRenderer renderer)
        {
            string goName = renderer.gameObject.name.ToLowerInvariant();
            if (goName == "shadow" || goName == "back") return true;

            string spriteName = renderer.sprite != null ? renderer.sprite.name.ToLowerInvariant() : string.Empty;
            if (spriteName.Contains("shadow")) return true;

            return false;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null) continue;
            if (IsExcluded(renderer)) continue;

            float area = renderer.sprite.rect.width * renderer.sprite.rect.height;
            int sorting = renderer.sortingOrder;
            bool isBetter = area > bestArea || (Mathf.Approximately(area, bestArea) && sorting > bestSortingOrder);
            if (isBetter)
            {
                bestArea = area;
                bestSortingOrder = sorting;
                bestSprite = renderer.sprite;
            }
        }

        if (bestSprite != null)
        {
            return bestSprite;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null) continue;

            float area = renderer.sprite.rect.width * renderer.sprite.rect.height;
            int sorting = renderer.sortingOrder;
            bool isBetter = area > bestArea || (Mathf.Approximately(area, bestArea) && sorting > bestSortingOrder);
            if (isBetter)
            {
                bestArea = area;
                bestSortingOrder = sorting;
                bestSprite = renderer.sprite;
            }
        }

        return bestSprite;
    }

    RenderTexture CreateUnitPreviewTexture(int unitNumber, Vector2 slotSize)
    {
        GameObject prefab = GetUnitPrefab(unitNumber);
        if (prefab == null)
        {
            return null;
        }

        EnsurePreviewCamera();

        int texSize = Mathf.Max(64, Mathf.RoundToInt(Mathf.Max(slotSize.x, slotSize.y)));
        int width = texSize;
        int height = texSize;
        RenderTexture renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        previewTextures.Add(renderTexture);

        float cellSize = GetInGameCellSize();
        GameObject instance = Instantiate(prefab, previewRoot);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = GetInGameShopPreviewLocalScale(unitNumber, cellSize);
        SetLayerRecursively(instance, PreviewLayer);
        PlayShopPreviewIdleAnimations(instance);
        previewInstances.Add(instance);

        if (!TryGetShopPreviewBounds(instance, out Bounds bounds))
        {
            instance.SetActive(false);
            previewTextures.Remove(renderTexture);
            previewInstances.Remove(instance);
            Destroy(instance);
            renderTexture.Release();
            Destroy(renderTexture);
            return null;
        }

        DeactivateAllShopPreviewInstances();
        instance.SetActive(true);

        previewCamera.orthographicSize = Mathf.Max(0.01f, ComputeShopPreviewOrthographicSize(bounds, width, height));
        previewCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        previewCamera.targetTexture = renderTexture;
        previewCamera.Render();
        previewCamera.targetTexture = null;

        instance.SetActive(false);

        return renderTexture;
    }

    static bool TryGetShopPreviewBounds(GameObject instance, out Bounds bounds)
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

    void DeactivateAllShopPreviewInstances()
    {
        for (int i = 0; i < previewInstances.Count; i++)
        {
            if (previewInstances[i] != null)
            {
                previewInstances[i].SetActive(false);
            }
        }
    }

    static void PlayShopPreviewIdleAnimations(GameObject root)
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

    /// <summary>상점 미리보기 — 머리·얼굴 파츠만 남기고 무기·몸·팔다리는 숨깁니다.</summary>
    static void ApplyShopHeadOnlyVisibility(GameObject instance)
    {
        if (instance == null) return;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null) continue;
            renderer.enabled = ShouldShowInShopHeadPreview(renderer);
        }
    }

    static bool TryGetShopHeadPreviewBounds(GameObject instance, out Bounds bounds)
    {
        return TryGetEnabledSpriteBounds(instance, out bounds);
    }

    static bool TryGetEnabledSpriteBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        if (instance == null) return false;

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

    static void RestoreShopPreviewRenderers(GameObject instance)
    {
        if (instance == null) return;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true;
            }
        }
    }

    static void ApplyShopWeaponOnlyHide(GameObject instance)
    {
        if (instance == null) return;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null) continue;
            renderer.enabled = !IsUnderShopWeaponOrShieldBranch(renderer.transform);
        }
    }

    static bool IsUnderShopWeaponOrShieldBranch(Transform node)
    {
        while (node != null)
        {
            if (node.name.Contains("Weapon") || node.name.Contains("Shield"))
            {
                return true;
            }

            node = node.parent;
        }

        return false;
    }

    static bool TryGetNeckUpHeadPreviewBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        if (!TryGetEnabledSpriteBounds(instance, out Bounds fullBounds))
        {
            return false;
        }

        float headHeight = fullBounds.size.y * 0.38f;
        float centerY = fullBounds.max.y - headHeight * 0.5f;
        bounds = new Bounds(
            new Vector3(fullBounds.center.x, centerY, fullBounds.center.z),
            new Vector3(fullBounds.size.x * 0.82f, headHeight, fullBounds.size.z));
        return true;
    }

    static bool ShouldShowInShopHeadPreview(SpriteRenderer renderer)
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
            if (IsShopHeadSlotName(node.name))
            {
                return true;
            }

            node = node.parent;
        }

        node = renderer.transform;
        while (node != null)
        {
            if (IsShopNonHeadBranchName(node.name))
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
            if (node.name == targetName)
            {
                return true;
            }

            node = node.parent;
        }

        return false;
    }

    static bool IsShopNonHeadBranchName(string transformName)
    {
        if (string.IsNullOrEmpty(transformName) || transformName == "P_Head")
        {
            return false;
        }

        if (transformName.Contains("Weapon") || transformName.Contains("Shield"))
        {
            return true;
        }

        if (transformName.Contains("Foot") || transformName.Contains("Shoulder") || transformName.Contains("Back"))
        {
            return true;
        }

        if (transformName == "P_Body" || transformName.Contains("BodySet") ||
            transformName.Contains("ArmorBody") || transformName.Contains("ClothBody"))
        {
            return true;
        }

        if (transformName.Contains("Cloth") &&
            transformName.IndexOf("Face", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return true;
        }

        if (transformName.Contains("Close") || transformName.Contains("CArm"))
        {
            return true;
        }

        if (transformName.Contains("Arm") &&
            transformName.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return true;
        }

        return false;
    }

    static bool IsShopHeadSlotName(string transformName)
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

    GameObject GetUnitPrefab(int unitNumber)
    {
        foreach (string path in GetUnitPrefabResourcePaths(unitNumber))
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    static IEnumerable<string> GetUnitPrefabResourcePaths(int unitNumber)
    {
        string padded = unitNumber.ToString("000");
        yield return $"Units/unit_{padded}";
        yield return $"Units/unit_{unitNumber}";

        switch (unitNumber)
        {
            case 2:
                yield return "Addons/RetroHeroes/2_Prefab/unit_002";
                break;
            case 3:
                yield return "Addons/RetroHeroes/2_Prefab/unit_003";
                break;
        }
    }

    void EnsurePreviewCamera()
    {
        if (previewCamera != null) return;

        GameObject cameraObj = new GameObject("ShopPreviewCamera");
        cameraObj.transform.SetParent(transform, false);
        previewCamera = cameraObj.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.orthographic = true;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 100f;

        GameObject rootObj = new GameObject("ShopPreviewRoot");
        rootObj.transform.SetParent(transform, false);
        previewRoot = rootObj.transform;
    }

    void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null) return;
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    Sprite GetFallbackUnitSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 1f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
    
    void SyncGradePoolFromOwnedUnits()
    {
        gradePoolLock.Reset();
        Character[] all = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Character character = all[i];
            if (character == null || character.isShopPreviewInstance || character.unitNumber <= 0) continue;

            int grade = UnitCombatStats.GetGradeForUnit(character.unitNumber);
            gradePoolLock.RegisterPurchase(character.unitNumber, grade);
        }
    }

    List<UnitData> FilterShopCandidates(int grade)
    {
        SyncGradePoolFromOwnedUnits();

        List<UnitData> candidates = new List<UnitData>();
        foreach (UnitData unit in allUnits)
        {
            if (unit.grade == grade && !IsUnitMaxStar(unit.unitNumber))
            {
                candidates.Add(unit);
            }
        }

        if (gradePoolLock.IsLocked(grade))
        {
            var filtered = new List<UnitData>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                UnitData unit = candidates[i];
                if (gradePoolLock.ContainsType(grade, unit.unitNumber))
                {
                    filtered.Add(unit);
                }
            }
            candidates = filtered;
        }

        return candidates;
    }

    void RegisterShopPurchase(UnitData unitData, bool isEvolutionPurchase)
    {
        if (unitData == null) return;

        int grade = UnitCombatStats.GetGradeForUnit(unitData.unitNumber);
        int threshold = ShopGradePoolLock.GetLockThreshold(grade);
        int distinctAfter = CountDistinctOwnedUnitsInGrade(grade);
        int distinctBefore = isEvolutionPurchase ? distinctAfter : Mathf.Max(0, distinctAfter - 1);

        SyncGradePoolFromOwnedUnits();

        if (distinctBefore < threshold && distinctAfter >= threshold)
        {
            string names = ShopGradePoolLock.FormatUnitNames(gradePoolLock.GetDistinctUnitNumbers(grade));
            ShowShopNotice(GameLocalization.ShopGradePoolLocked(grade, names));
        }

        RefreshGradePoolPanel();
    }

    int CountDistinctOwnedUnitsInGrade(int grade)
    {
        var seen = new HashSet<int>();
        Character[] all = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Character character = all[i];
            if (character == null || character.isShopPreviewInstance || character.unitNumber <= 0) continue;
            if (UnitCombatStats.GetGradeForUnit(character.unitNumber) != grade) continue;
            seen.Add(character.unitNumber);
        }
        return seen.Count;
    }

    public List<ShopGradePoolLock.GradePoolStatus> GetGradePoolStatuses()
    {
        SyncGradePoolFromOwnedUnits();
        return gradePoolLock.GetActiveStatuses();
    }

    public Sprite GetUnitPortraitSprite(int unitNumber)
    {
        return GetUnitSprite(unitNumber);
    }

    void RefreshGradePoolPanel()
    {
        SyncGradePoolFromOwnedUnits();
        if (shopPanel == null) return;

        gradePoolPanel = ShopGradePoolPanel.Ensure(shopPanel.transform);
        if (gradePoolPanel != null)
        {
            gradePoolPanel.Refresh(gradePoolLock);
        }

        if (shopSlotsParent == null) return;
        RectTransform slotsRect = shopSlotsParent as RectTransform;
        if (slotsRect == null) return;

        int activeRows = gradePoolLock.GetActiveStatuses().Count;
        float slotsY = activeRows > 0 ? -12f - activeRows * 28f : -4f;
        slotsRect.anchoredPosition = new Vector2(slotsRect.anchoredPosition.x, slotsY);
    }

    bool IsUnitMaxStar(int unitNumber)
    {
        Character owned = FindOwnedCharacter(unitNumber);
        return owned != null && owned.evolutionLevel >= MaxStarLevel;
    }

    Character FindOwnedCharacter(int unitNumber)
    {
        Character[] all = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character best = null;
        foreach (Character character in all)
        {
            if (character == null || character.isShopPreviewInstance || character.unitNumber != unitNumber) continue;
            if (best == null || character.evolutionLevel > best.evolutionLevel)
            {
                best = character;
            }
        }
        return best;
    }

    void ShowShopNotice(string message)
    {
        GameSceneController controller = FindFirstObjectByType<GameSceneController>();
        if (controller != null)
        {
            controller.ShowPlacementNotice(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// 유닛을 구매합니다 (클릭 시 호출)
    /// </summary>
    void BuyUnit(UnitData unitData, GameObject shopItem)
    {
        if (unitData == null) return;

        int unitPrice = GetUnitPriceByGrade(unitData.grade);

        Character owned = FindOwnedCharacter(unitData.unitNumber);
        if (owned != null)
        {
            if (owned.evolutionLevel >= MaxStarLevel)
            {
                ShowShopNotice(GameLocalization.ShopUnitSoldOut);
                return;
            }

            if (GameManager.Instance == null || !GameManager.Instance.SpendGold(unitPrice))
            {
                ShowShopNotice(GameLocalization.ShopNotEnoughGold(unitPrice));
                return;
            }

            owned.IncreaseEvolutionLevel();
            RegisterShopPurchase(unitData, isEvolutionPurchase: true);
            Debug.Log($"유닛 {unitData.GetUnitName()} 성급 상승: {owned.evolutionLevel}성");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyFirstRoundUnitPurchased();
            }
            RemoveShopSlot(shopItem);
            return;
        }

        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        if (boardManager != null)
        {
            if (GameManager.Instance == null || !GameManager.Instance.SpendGold(unitPrice))
            {
                ShowShopNotice(GameLocalization.ShopNotEnoughGold(unitPrice));
                return;
            }

            Color unitColor = GetGradeColor(unitData.grade);
            Character summoned = boardManager.AddUnitToBench(unitData, unitColor);
            if (summoned != null)
            {
                RegisterShopPurchase(unitData, isEvolutionPurchase: false);
                UnitAcquireEffect.PlaySummon(summoned);
            }
            Debug.Log($"유닛 {unitData.GetUnitName()} ({unitData.GetGradeName()}) 구매 (1성)");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyFirstRoundUnitPurchased();
            }
            RemoveShopSlot(shopItem);
        }
    }

    int GetUnitPriceByGrade(int grade)
    {
        switch (grade)
        {
            case 5: return 1;
            case 4: return 2;
            case 3: return 3;
            case 2: return 4;
            case 1: return 5;
            default: return 1;
        }
    }

    public struct LevelUpUnitChoice
    {
        public UnitData unitData;
        public bool isNewUnit;
        public int currentEvolutionLevel;
        public int resultEvolutionLevel;
    }

    /// <summary>
    /// 레벨업 보상용 유닛 3택1 후보를 현재 레벨 등급 확률로 굴립니다.
    /// </summary>
    public List<LevelUpUnitChoice> RollLevelUpChoices(int playerLevel, int count = 3)
    {
        if (allUnits.Count == 0)
        {
            InitializeUnits();
        }

        var choices = new List<LevelUpUnitChoice>(count);
        var usedNumbers = new HashSet<int>();
        int attempts = 0;
        int maxAttempts = count * 20;

        while (choices.Count < count && attempts < maxAttempts)
        {
            attempts++;
            UnitData picked = SelectUnitByLevel(playerLevel);
            if (picked == null) break;
            if (usedNumbers.Contains(picked.unitNumber)) continue;

            usedNumbers.Add(picked.unitNumber);
            Character owned = FindOwnedCharacter(picked.unitNumber);
            bool isNew = owned == null;
            int currentEvolution = isNew ? 0 : owned.evolutionLevel;
            int resultEvolution = isNew ? 1 : owned.evolutionLevel + 1;

            choices.Add(new LevelUpUnitChoice
            {
                unitData = picked,
                isNewUnit = isNew,
                currentEvolutionLevel = currentEvolution,
                resultEvolutionLevel = resultEvolution
            });
        }

        return choices;
    }

    /// <summary>
    /// 레벨업 보상으로 유닛을 보드에 자동 배치하거나 진화시킵니다 (골드 소비 없음).
    /// </summary>
    public bool GrantLevelUpUnitReward(UnitData unitData)
    {
        if (unitData == null) return false;

        Character owned = FindOwnedCharacter(unitData.unitNumber);
        if (owned != null)
        {
            if (owned.evolutionLevel >= MaxStarLevel)
            {
                ShowShopNotice(GameLocalization.ShopCannotEvolveMore);
                return false;
            }

            owned.IncreaseEvolutionLevel();
            Debug.Log($"레벨업 보상: {unitData.GetUnitName()} {owned.evolutionLevel}성");
            return true;
        }

        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        if (boardManager == null) return false;

        Color unitColor = GetGradeColor(unitData.grade);
        Character summoned = boardManager.AddUnitToBoardAuto(unitData, unitColor);
        if (summoned != null)
        {
            UnitAcquireEffect.PlaySummon(summoned);
            Debug.Log($"레벨업 보상: {unitData.GetUnitName()} ({unitData.GetGradeName()}) 보드 배치");
            return true;
        }

        // 보드가 가득 차면 대기칸(벤치)으로 배치
        summoned = boardManager.AddUnitToBench(unitData, unitColor);
        if (summoned != null)
        {
            UnitAcquireEffect.PlaySummon(summoned);
            ShowShopNotice(GameLocalization.ShopBenchFallback);
            Debug.Log($"레벨업 보상: {unitData.GetUnitName()} ({unitData.GetGradeName()}) 대기칸 배치");
            return true;
        }

        ShowShopNotice(GameLocalization.ShopNoSpaceForUnit);
        return false;
    }

    public Color GetGradeFrameColorForUi(int grade) => GetGradeFrameTint(grade);

    public void PopulateLevelUpCardVisual(Transform cardRoot, LevelUpUnitChoice choice)
    {
        if (cardRoot == null || choice.unitData == null) return;

        Color gradeTint = GetGradeFrameTint(choice.unitData.grade);
        Vector2 visualSize = new Vector2(170f, 170f);
        // 특성은 카드 쪽에서 칩 형태로 표시하므로 초상화만 중앙 배치
        TryAddShopUnitVisual(cardRoot, choice.unitData.unitNumber, visualSize, gradeTint, centered: true);
    }
}
