using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Fotbool.Editor
{
    public class WebGLBuildScript : EditorWindow
    {
        [MenuItem("Fotbool/Build/WebGL")]
        public static void BuildWebGL()
        {
            ConfigurePlayerSettings();

            string outputPath = Path.Combine(Application.dataPath, "..", "Builds", "WebGL");
            Directory.CreateDirectory(outputPath);

            string[] scenes = GetBuildScenes();

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Fotbool] ✅ تم بناء WebGL بنجاح: {outputPath}");
                Debug.Log("[Fotbool] 🌐 ارفع مجلد WebGL إلى Netlify/Vercel/GitHub Pages");
                Debug.Log("[Fotbool] 📁 الملفات الرئيسية: index.html، Build folder، TemplateData folder");
            }
            else
            {
                Debug.LogError($"[Fotbool] ❌ فشل البناء: {report.summary.result}");
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
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }

            if (scenes.Count == 0)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(currentScene))
                    scenes.Add(currentScene);
            }

            return scenes.ToArray();
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Fotbool Studio";
            PlayerSettings.productName = "Fotbool";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // إعدادات WebGL
            PlayerSettings.WebGL.memorySize = 512; // ميغابايت
            PlayerSettings.WebGL.exceptionsMode = WebGLExceptionsMode.None;
            PlayerSettings.WebGL.template = "default";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;

            // دقة الشاشة للموبايل
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = false;
        }
    }
}
