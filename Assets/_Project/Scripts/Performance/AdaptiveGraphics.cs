using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Fotbool.Performance
{
    /// <summary>Clone the URP asset so adaptive changes never modify the authored asset.</summary>
    public sealed class AdaptiveGraphics : MonoBehaviour
    {
        public int TargetFps = 60;
        private UniversalRenderPipelineAsset original, runtime;
        private float elapsed, sum;
        private int frames;
        private void OnEnable()
        {
            original = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (original == null) original = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (original == null) { enabled = false; return; }
            runtime = Instantiate(original);
            QualitySettings.renderPipeline = runtime;
            bool low = SystemInfo.systemMemorySize < 4000 || SystemInfo.graphicsMemorySize < 1500;
            runtime.renderScale = low ? 0.75f : 1f;
            runtime.shadowDistance = low ? 25f : 55f;
            QualitySettings.lodBias = low ? 0.7f : 1.2f;
            Application.targetFrameRate = TargetFps;
        }
        private void Update()
        {
            if (Time.timeScale == 0 || runtime == null) return;
            elapsed += Time.unscaledDeltaTime; sum += Time.unscaledDeltaTime; frames++;
            if (elapsed < 5f) return;
            float mean = sum / Mathf.Max(1, frames);
            if (mean > 1.15f / Mathf.Max(30, TargetFps)) runtime.renderScale = Mathf.Max(0.65f, runtime.renderScale - 0.05f);
            else if (mean < 1.04f / Mathf.Max(30, TargetFps)) runtime.renderScale = Mathf.Min(1f, runtime.renderScale + 0.025f);
            elapsed = sum = 0; frames = 0;
        }
        private void OnDisable()
        {
            if (runtime == null) return;
            QualitySettings.renderPipeline = original;
            Destroy(runtime);
        }
    }
}
