using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// 대기 씬(Lobby Scene) 컨트롤러
/// </summary>
public class LobbySceneController : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;
    public Button specialSkillButton;
    
    private SkillSelectionManager skillSelectionManager;
    
    void Start()
    {
        // UI 입력이 먹도록 이벤트 시스템 보장
        EnsureEventSystem();
        UIFontProvider.ApplyToAllText();

        // UI가 없으면 자동으로 생성
        if (startButton == null || specialSkillButton == null)
        {
            CreateUI();
        }

        // 씬에 배치된 버튼이 있어도 클릭 이벤트는 항상 연결
        BindButtonHandlers();
        
        // GameManager가 없으면 생성 (실버 코인/강화 데이터용)
        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }
        
        // SceneLoader가 없으면 생성
        if (SceneLoader.Instance == null)
        {
            GameObject sceneLoaderObj = new GameObject("SceneLoader");
            sceneLoaderObj.AddComponent<SceneLoader>();
        }
    }
    
    /// <summary>
    /// UI를 자동으로 생성합니다
    /// </summary>
    void CreateUI()
    {
        EnsureEventSystem();
        
        // Canvas가 없으면 생성
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            MobileUIScaling.Configure(canvasObj.AddComponent<CanvasScaler>());
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        CreateLobbyBackground(canvas.transform);
        CreateLobbySummonerImage(canvas.transform);
        
        // 시작 버튼 생성
        if (startButton == null)
        {
            startButton = CreateButton(canvas.transform, "StartButton", "시작", new Vector2(0, 100));
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        // 특수 스킬 버튼 생성
        if (specialSkillButton == null)
        {
            specialSkillButton = CreateButton(canvas.transform, "SpecialSkillButton", "광역 마법", new Vector2(-580, -160));
            specialSkillButton.onClick.AddListener(OnSpecialSkillButtonClicked);
        }
    }

    void CreateLobbyBackground(Transform parent)
    {
        Sprite bgSprite = Resources.Load<Sprite>("lobby");
        if (bgSprite == null)
        {
            Debug.LogWarning("lobby.png 스프라이트를 찾지 못했습니다. Assets/Resources/lobby.png 확인 필요");
            return;
        }

        GameObject bgObj = new GameObject("LobbyBackground");
        bgObj.transform.SetParent(parent, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = bgSprite;
        bgImage.color = Color.white;

        RectTransform rect = bgObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        bgObj.transform.SetAsFirstSibling();
    }

    void CreateLobbySummonerImage(Transform parent)
    {
        Sprite summonerSprite = Resources.Load<Sprite>("summoner");
        if (summonerSprite == null)
        {
            Debug.LogWarning("summoner.png 스프라이트를 찾지 못했습니다. Assets/Resources/summoner.png 확인 필요");
            return;
        }

        GameObject summonerObj = new GameObject("SummonerImage");
        summonerObj.transform.SetParent(parent, false);
        Image summonerImage = summonerObj.AddComponent<Image>();
        summonerImage.sprite = summonerSprite;
        summonerImage.color = Color.white;

        RectTransform rect = summonerObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        float scale = 0.25f;
        rect.sizeDelta = new Vector2(summonerSprite.rect.width, summonerSprite.rect.height) * scale;
    }


    void BindButtonHandlers()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (specialSkillButton != null)
        {
            specialSkillButton.onClick.RemoveListener(OnSpecialSkillButtonClicked);
            specialSkillButton.onClick.AddListener(OnSpecialSkillButtonClicked);
        }
    }

    void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null &&
            eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
    
    /// <summary>
    /// 버튼을 생성합니다
    /// </summary>
    Button CreateButton(Transform parent, string name, string text, Vector2 position)
    {
        // 버튼 오브젝트 생성
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        // RectTransform 설정
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 60);
        rectTransform.anchoredPosition = position;
        
        // Image 컴포넌트 추가 (버튼 배경)
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.9f, 1f); // 파란색 배경
        
        // Button 컴포넌트 추가
        Button button = buttonObj.AddComponent<Button>();
        
        // 텍스트 오브젝트 생성
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = UIFontProvider.Get();
        textComponent.fontSize = 30;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        SimpleUIPackTheme.ApplyLobbyBlackHorizontalButton(button);
        return button;
    }
    
    /// <summary>
    /// 시작 버튼을 눌렀을 때 - 인게임 씬으로 이동
    /// </summary>
    public void OnStartButtonClicked()
    {
        Debug.Log("시작 버튼이 클릭되었습니다!");
        
        if (SceneLoader.Instance != null)
        {
            Debug.Log("SceneLoader를 통해 씬 로드 시도: GameScene");
            SceneLoader.Instance.LoadScene(SceneLoader.SceneType.GameScene);
        }
        else
        {
            Debug.Log("SceneLoader가 없어 SceneManager로 직접 씬 로드 시도: GameScene");
            SceneManager.LoadScene("GameScene");
        }
    }
    
    public void OnSpecialSkillButtonClicked()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Canvas를 찾을 수 없습니다!");
            return;
        }
        
        if (skillSelectionManager == null)
        {
            CreateSkillSelectionUI(canvas);
        }
        
        skillSelectionManager.Open();
    }

    void CreateSkillSelectionUI(Canvas canvas)
    {
        GameObject panelObj = new GameObject("SkillSelectionPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        SimpleUIPackTheme.ApplyPopupBackground(panelImage);
        Sprite popupSprite = Resources.Load<Sprite>("popup_test");
        if (popupSprite != null)
        {
            panelImage.sprite = popupSprite;
            panelImage.color = Color.white;
            panelImage.type = Image.Type.Simple;
        }
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        if (popupSprite != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(popupSprite.rect.width, popupSprite.rect.height);
            panelRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }
        
        skillSelectionManager = panelObj.AddComponent<SkillSelectionManager>();
        skillSelectionManager.BuildUI();
    }
}

