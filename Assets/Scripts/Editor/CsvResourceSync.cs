using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 프로젝트 루트의 밸런스 CSV(zombies.csv, rounds.csv)를
/// Assets/Resources 와 Assets/StreamingAssets 로 자동 복사하는 에디터 유틸리티.
///
/// 왜 필요한가:
/// - PC 빌드는 StreamingAssets를 실제 파일 경로로 읽을 수 있어서 지금까지 문제가 없었다.
/// - 하지만 안드로이드 빌드에서는 StreamingAssets가 APK 내부에 압축되어 들어가고
///   Application.streamingAssetsPath가 "jar:file:///....apk!/assets" 형태의 URL이 되기 때문에
///   System.IO(File.Exists / File.ReadAllText)로는 절대 읽을 수 없다.
///   그 결과 zombies.csv를 못 읽어 좀비 데이터가 0개가 되고, 적이 한 마리도 스폰되지 않았다.
///   (소환 마법진 연출은 스폰 시도 전에 먼저 재생되므로 "연출만 나오고 적은 안 나오는" 증상이 됨)
/// - Resources 폴더는 모든 플랫폼에서 동일하게 동작하므로, 여기에 사본을 두고
///   런타임에서 파일 경로 → Resources 순으로 폴백하도록 했다.
///
/// 이 스크립트는 빌드할 때마다 자동으로 사본을 최신 상태로 맞춰주므로
/// 밸런스 수정은 지금처럼 프로젝트 루트의 CSV만 고치면 된다.
/// </summary>
public class CsvResourceSync : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    static readonly string[] CsvFileNames = { "zombies.csv", "rounds.csv" };

    /// <summary>모든 빌드(PC/안드로이드 포함) 직전에 자동 실행된다.</summary>
    public void OnPreprocessBuild(BuildReport report)
    {
        SyncAll(logWhenUnchanged: false);
    }

    [MenuItem("Build/CSV 데이터를 Resources로 동기화")]
    public static void SyncMenu()
    {
        SyncAll(logWhenUnchanged: true);
        EditorUtility.DisplayDialog(
            "CSV 동기화 완료",
            "프로젝트 루트의 zombies.csv / rounds.csv 를\nAssets/Resources 와 Assets/StreamingAssets 로 복사했습니다.",
            "확인");
    }

    public static void SyncAll(bool logWhenUnchanged)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogWarning("[CsvResourceSync] 프로젝트 루트 경로를 확인할 수 없습니다.");
            return;
        }

        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        string streamingDir = Path.Combine(Application.dataPath, "StreamingAssets");

        Directory.CreateDirectory(resourcesDir);
        Directory.CreateDirectory(streamingDir);

        bool changed = false;

        for (int i = 0; i < CsvFileNames.Length; i++)
        {
            string fileName = CsvFileNames[i];
            string source = Path.Combine(projectRoot, fileName);

            if (!File.Exists(source))
            {
                Debug.LogWarning($"[CsvResourceSync] 원본 CSV를 찾을 수 없습니다: {source}");
                continue;
            }

            changed |= CopyIfDifferent(source, Path.Combine(resourcesDir, fileName));
            changed |= CopyIfDifferent(source, Path.Combine(streamingDir, fileName));
        }

        if (changed)
        {
            AssetDatabase.Refresh();
            Debug.Log("[CsvResourceSync] CSV 데이터를 Resources / StreamingAssets 로 동기화했습니다.");
        }
        else if (logWhenUnchanged)
        {
            Debug.Log("[CsvResourceSync] CSV 사본이 이미 최신 상태입니다.");
        }
    }

    static bool CopyIfDifferent(string source, string destination)
    {
        try
        {
            if (File.Exists(destination) &&
                File.ReadAllText(source) == File.ReadAllText(destination))
            {
                return false;
            }

            File.Copy(source, destination, true);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CsvResourceSync] 복사 실패 {source} → {destination}: {e.Message}");
            return false;
        }
    }
}
