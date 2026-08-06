using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환을 관리하는 클래스
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    
    public enum SceneType
    {
        TitleScene,     // 시작 씬
        LobbyScene,     // 대기 씬
        GameScene       // 인게임 씬
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            EnsureScenesInBuildSettings();
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 씬을 로드합니다
    /// </summary>
    public void LoadScene(SceneType sceneType)
    {
        string sceneName = GetSceneName(sceneType);
        int buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{sceneName}.unity");
        if (buildIndex >= 0)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

#if UNITY_EDITOR
        string scenePath = FindScenePath(sceneName);
        if (!string.IsNullOrEmpty(scenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scenePath,
                new LoadSceneParameters(LoadSceneMode.Single)
            );
            return;
        }
#endif

        Debug.LogError($"씬을 로드할 수 없습니다: {sceneName}. Build Profiles에 씬을 추가해 주세요.");
    }
    
    /// <summary>
    /// 씬 타입에 맞는 씬 이름을 반환합니다
    /// </summary>
    string GetSceneName(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.TitleScene:
                return "TitleScene";
            case SceneType.LobbyScene:
                return "LobbyScene";
            case SceneType.GameScene:
                return "GameScene";
            default:
                return "TitleScene";
        }
    }

#if UNITY_EDITOR
    void EnsureScenesInBuildSettings()
    {
        AddSceneToBuildSettings("TitleScene");
        AddSceneToBuildSettings("LobbyScene");
        AddSceneToBuildSettings("GameScene");
    }

    void AddSceneToBuildSettings(string sceneName)
    {
        string scenePath = FindScenePath(sceneName);
        if (string.IsNullOrEmpty(scenePath)) return;

        UnityEditor.EditorBuildSettingsScene[] scenes = UnityEditor.EditorBuildSettings.scenes;
        foreach (var scene in scenes)
        {
            if (scene.path == scenePath) return;
        }

        var newScenes = new UnityEditor.EditorBuildSettingsScene[scenes.Length + 1];
        for (int i = 0; i < scenes.Length; i++)
        {
            newScenes[i] = scenes[i];
        }
        newScenes[newScenes.Length - 1] = new UnityEditor.EditorBuildSettingsScene(scenePath, true);
        UnityEditor.EditorBuildSettings.scenes = newScenes;

        Debug.Log($"[SceneLoader] Build Settings에 씬 추가: {scenePath}");
    }

    string FindScenePath(string sceneName)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{sceneName} t:Scene");
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"[SceneLoader] 씬을 찾지 못했습니다: {sceneName}");
            return string.Empty;
        }
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
        return path;
    }
#endif
}

