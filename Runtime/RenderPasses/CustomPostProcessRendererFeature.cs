using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomPostProcessing.UniversalRP
{
    /// <summary>
    /// PostProcessVolumeRendererFeature is a renderer feature used to change screen appearance such as post processing effect.
    /// This implementation lets it's user create an effect with minimal code involvement.
    /// </summary>
    [Serializable, DisallowMultipleRendererFeature("Custom Post-Process")]
    [CoreRPHelpURL
    (
        pageName: "integration-with-post-processing",
        pageHash: "#post-proc-how-to",
        packageName: "com.unity.render-pipelines.universal"
    )]
    public sealed class CustomPostProcessRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] public ScriptableObject postProcessData;

        [SerializeField] public CustomPostProcessPass CustomBeforeTransparentsPass;
        [SerializeField] public CustomPostProcessPass CustomBeforePostProcessPass;
        [SerializeField] public CustomPostProcessPass CustomAfterPostProcessPass;
        
        public GraphicsFormat GetHDRFormat()
        {
            // Texture format pre-lookup
            const FormatUsage usage = FormatUsage.Linear | FormatUsage.Render;
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, usage)) // HDR fallback
            {
                return GraphicsFormat.B10G11R11_UFloatPack32;
                // m_UseRGBM          = false;
            }

            return QualitySettings.activeColorSpace == ColorSpace.Linear
                ? GraphicsFormat.R8G8B8A8_SRGB
                : GraphicsFormat.R8G8B8A8_UNorm;
            // m_UseRGBM = true;
        }

        private void OnEnable()
        {
            Create();
        }

        public override void Create()
        {
#if UNITY_EDITOR
            if (CustomPostProcessSettings.Instance is null)
            {
                return;
            }
            // CustomPostProcessSettings.instance.OnDataChange = Create;
#endif
            var customPostProcessOrder = CustomPostProcessSettings.Instance.customPostProcessOrders;

            CustomBeforeTransparentsPass = new CustomPostProcessPass(customPostProcessOrder.beforeTransparentCustomPostProcesses, postProcessData);
            CustomBeforePostProcessPass  = new CustomPostProcessPass(customPostProcessOrder.beforePostProcessCustomPostProcesses, postProcessData);
            CustomAfterPostProcessPass   = new CustomPostProcessPass(customPostProcessOrder.afterPostProcessCustomPostProcesses, postProcessData);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            
            if (UniversalRenderer.IsOffscreenDepthTexture(in cameraData) || cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
                return;

            if (!cameraData.postProcessEnabled)
            {
#if UNITY_EDITOR
                if (Selection.activeObject == cameraData.camera)
                {
                    Debug.Log("Check whether the selected camera has post-processing enabled.");
                }
#endif
                return;
            }

            if (!isActive)
            {
                return;
            }

            if (cameraData.isPreviewCamera)
            {
                return;
            }

            if (CustomBeforeTransparentsPass.IsActive())
            {
                renderer.EnqueuePass(CustomBeforeTransparentsPass);
            }

            if (CustomBeforePostProcessPass.IsActive())
            {
                renderer.EnqueuePass(CustomBeforePostProcessPass);
            }

            if (CustomAfterPostProcessPass.IsActive())
            {
                renderer.EnqueuePass(CustomAfterPostProcessPass);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            cameraData.cameraTargetDescriptor.graphicsFormat = GetHDRFormat();
            
            CustomBeforeTransparentsPass.Setup(cameraData.cameraTargetDescriptor);
            CustomBeforePostProcessPass.Setup(cameraData.cameraTargetDescriptor);
            CustomAfterPostProcessPass.Setup(cameraData.cameraTargetDescriptor);
        }

        protected override void Dispose(bool disposing)
        {
            CustomBeforeTransparentsPass?.Dispose();
            CustomBeforePostProcessPass?.Dispose();
            CustomAfterPostProcessPass?.Dispose();
            CustomPostProcessVolumeComponent.CleanupAllCustomPostProcesses();
        }
    }
}