using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Fotbool.Editor
{
    /// <summary>
    -scr سكريبت بناء اللعبة للهواتف المحمولة (Android و iOS)
    -scr يمكن استدعاؤه من قائمة Unity: Fotbool → Build → Android / iOS
    /// </summary>
    public class MobileBuildScript : EditorWindow
    {
        [MenuItem("Fotbool/Build/Android APK")]
        public static void BuildAndroid()
        {
            BuildMobile(BuildTarget.Android, "Fotbool.apk", BuildOptions.None);
        }

        [MenuItem("Fotbool/Build/Android APK (Development)")]
        public static void BuildAndroidDevelopment()
        {
            BuildMobile(BuildTarget.Android, "Fotbool_Development.apk", BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        [MenuItem("Fotbool/Build/iOS")]
        public static void BuildiOS()
        {
            BuildMobile(BuildTarget.iOS, "Fotbool_iOS", BuildOptions.None);
        }

        private static void BuildMobile(BuildTarget target, string outputName, BuildOptions options)
        {
            // إعدادات اللاعب للهواتف المحمولة
            ConfigurePlayerSettings(target);

            // تحديد مسار الإخراج
            string outputPath = Path.Combine(Application.dataPath, "..", "Builds", outputName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // 장면ات للبناء (تأكد من وجود مشهد البداية)
            string[] scenes = GetBuildScenes();

            // تكوين خيارات البناء
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = options
            };

            // بدء عملية البناء
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Fotbool] ✅ تم بناء اللعبة بنجاح: {outputPath}");
                Debug.Log($"[Fotbool] 📦 حجم البناء: {new FileInfo(outputPath).Length / 1024 / 1024:N2} ميجابايت");
            }
            else
            {
                Debug.LogError($"[Fotbool] ❌ فشل بناء اللعبة: {report.summary.result}");
                foreach (var step in report.steps)
                {
                    if (step.messages != null)
                    {
                        foreach (var msg in step.messages)
                        {
                            Debug.LogError($"[Fotbool] {msg.type}: {msg.content}");
                        }
                    }
                }
            }
        }

        private static string[] GetBuildScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();

            // أضف جميع المشاهد المدرجة في Build Settings
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }

            // إذا لم تكن هناك مشاهد، أضف المشهد الحالي
            if (scenes.Count == 0)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(currentScene))
                    scenes.Add(currentScene);
            }

            return scenes.ToArray();
        }

        private static void ConfigurePlayerSettings(BuildTarget target)
        {
            PlayerSettings.companyName = "Fotbool Studio";
            PlayerSettings.productName = "Fotbool";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            if (target == BuildTarget.Android)
            {
                PlayerSettings.Android.bundleVersionCode = 1;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.fotbool.game");
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keyaliasName = "";
            }
            else if (target == BuildTarget.iOS)
            {
                PlayerSettings.iOS.buildNumber = "1";
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.fotbool.game");
                PlayerSettings.iOS.targetOSVersionString = "12.0";
            }

            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.runInBackground = false;
        }

        [MenuItem("Fotbool/Build/Open Build Folder")]
        public static void OpenBuildFolder()
        {
            string buildPath = Path.Combine(Application.dataPath, "..", "Builds");
            if (Directory.Exists(buildPath))
                EditorUtility.RevealInFinder(buildPath);
            else
                Debug.LogWarning("[Fotbool] مجلد البناء غير موجود بعد");
        }
    }
}