using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Automated Android APK build for "Who cares? Mercenary Corps! Synergy Defense".
///
/// Usage (inside the Unity Editor):
///   Menu bar → Build → Android APK
///
/// Usage (command line, no Editor UI needed — replace the Unity path with your
/// actual Unity Hub install path if it differs):
///   "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe" ^
///     -batchmode -quit ^
///     -projectPath "C:\Users\jiseo\My_Game" ^
///     -buildTarget Android ^
///     -executeMethod BuildScript.BuildAndroid ^
///     -logFile "C:\Users\jiseo\My_Game\build.log"
///
/// Requirements:
///   - Android Build Support module installed via Unity Hub (Installs tab →
///     gear icon next to 6000.2.6f2 → Add modules → Android Build Support,
///     with OpenJDK and Android SDK & NDK Tools checked).
///   - No keystore setup is required for a local test build — Unity
///     auto-generates/uses a debug keystore when none is configured in
///     Player Settings → Publishing Settings.
///
/// Output:
///   Builds/Android/WhocaresMercenaryCorps_<version>.apk inside the project folder.
/// </summary>
public static class BuildScript
{
    private const string OutputDir = "Builds/Android";

    [MenuItem("Build/Android APK")]
    public static void BuildAndroid()
    {
        // Make sure we're actually targeting Android before building.
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[BuildScript] Switching active build target to Android...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        // Use whatever scenes are enabled in File > Build Settings
        // (currently: TitleScene, GameScene — LobbyScene is present but disabled).
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No enabled scenes in Build Settings. Aborting build.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Build an .apk directly (not an .aab bundle) so it can be installed/tested right away.
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory(OutputDir);
        string version = string.IsNullOrEmpty(PlayerSettings.bundleVersion) ? "0.0.0" : PlayerSettings.bundleVersion;
        string fileName = $"WhocaresMercenaryCorps_{version}.apk";
        string locationPathName = Path.Combine(OutputDir, fileName);

        Debug.Log($"[BuildScript] Building APK → {locationPathName}");
        Debug.Log($"[BuildScript] Scenes: {string.Join(", ", scenes)}");

        var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        });

        var summary = buildReport.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] SUCCESS — {locationPathName} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime})");
        }
        else
        {
            Debug.LogError($"[BuildScript] FAILED — result={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
