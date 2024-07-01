namespace CustomPostProcessing.UniversalRP
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    // Accessing various internal methods and members from ScriptableRenderer, UniversalRenderer, and more.
    public static class ScriptableRendererInternal
    {
        /// <inheritdoc cref="ScriptableRendererData.TryGetRendererFeature{T}"/>
        /// <seealso cref="ScriptableRendererData.TryGetRendererFeature{T}"/>
        public static bool TryGetRendererFeature<T>(out T rendererFeature) where T : ScriptableRendererFeature
        {
            GetScriptableRendererData(out var rendererData);

            if (rendererData != null)
            {
                foreach (var target in rendererData.rendererFeatures)
                {
                    if (target.GetType() == typeof(T))
                    {
                        rendererFeature = target as T;
                        return true;
                    }
                }
            }

            rendererFeature = null;
            return false;
        }

        /// <exception cref="NullReferenceException"> Could not find <paramref name="rendererData"/> of desired Type.</exception>
        private static void GetScriptableRendererData(out ScriptableRendererData rendererData)
        {
            if (UniversalRenderPipeline.asset is null)
            {
                rendererData = null;
            }

            // Get first entry in m_RendererDataList array from UniversalRendererData
            rendererData = ((ScriptableRendererData[])
                typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance)
                                                    ?.GetValue(UniversalRenderPipeline.asset))?[0];

            if (rendererData is null)
                throw new NullReferenceException(nameof(rendererData));
        }

        private static void GetUniversalRendererMethodInternal(ScriptableRenderer renderer, string methodName, out MethodInfo rendererMethod)
        {
            UniversalRenderer universalRenderer = renderer as UniversalRenderer;
            rendererMethod = universalRenderer?.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererMethod == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to get {methodName} via System.Reflection");
#endif
            }
        }

        #region SwapBuffer

        public static void EnableSwapBufferMSAA(this ScriptableRenderer renderer, bool enable)
        {
            const string methodName = "EnableSwapBufferMSAA";
            GetUniversalRendererMethodInternal(renderer, methodName, out MethodInfo enableSwapBufferMSAAMethod);
            enableSwapBufferMSAAMethod.Invoke(renderer, new object[] {enable});
        }

        /// <seealso cref="UniversalRenderer.SwapColorBuffer"/>
        public static void SwapColorBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            const string methodName = "SwapColorBuffer";
            GetUniversalRendererMethodInternal(renderer, methodName, out MethodInfo swapColorBufferMethod);
            swapColorBufferMethod.Invoke(renderer, new object[] {cmd});
        }
        
        /// <seealso cref="UnityEngine.Rendering.Universal.UniversalRenderer.m_ColorBufferSystem"/>
        /// <seealso cref="UnityEngine.Rendering.Universal.Internal.RenderTargetBufferSystem"/>
        private static bool TryGetUniversalColorBufferSystem(ScriptableRenderer renderer, out FieldInfo colorBufferSystemField)
        {
            UniversalRenderer universalRenderer = renderer as UniversalRenderer;

            const string fieldName = "m_ColorBufferSystem";
            colorBufferSystemField = universalRenderer?.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (colorBufferSystemField != null) return true;
#if UNITY_EDITOR
            Debug.LogError($"Unable to get {fieldName} via System.Reflection.");
#endif
            return false;
        }

        /// <summary>
        /// Call GetFrontBuffer or GetBackBuffer from the Universal Renderer's m_ColorBufferSystem.
        /// </summary>
        /// <param name="renderer">Universal Renderer.</param>
        /// <param name="cmd">Command Buffer.</param>
        /// <param name="methodName">Desired Method from RenderTargetBufferSystem.</param>
        /// <returns>The RTHandle for the Camera's current front buffer or back buffer.</returns>
        private static RTHandle GetRTHandleFromColorBufferSystem(ScriptableRenderer renderer, CommandBuffer cmd, in string methodName)
        {
            RTHandle fallbackCameraTarget = ScriptableRenderPass.k_CameraTarget;

            bool fetchColorBufferSystem = TryGetUniversalColorBufferSystem(renderer, out FieldInfo colorBufferSystemField);
            if (fetchColorBufferSystem == false)
            {
                return fallbackCameraTarget;
            }

            object colorBufferSystemObject = colorBufferSystemField.GetValue(renderer);
            if (colorBufferSystemObject == null)
            {
                return fallbackCameraTarget;
            }

            Type colorBufferSystemType = colorBufferSystemObject.GetType();
            MethodInfo methodInfo = colorBufferSystemType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, new[] {typeof(RTHandle)}, null);
            object bufferObject = methodInfo?.Invoke(colorBufferSystemObject, new object[] {cmd});

            if (methodInfo == null || bufferObject == null)
            {
                return fallbackCameraTarget;
            }

            RTHandle colorBufferRTHandle = (RTHandle) bufferObject;
            return colorBufferRTHandle;
        }

        /// <seealso cref="UnityEngine.Rendering.Universal.UniversalRenderer.GetCameraColorFrontBuffer"/>
        public static RTHandle GetCameraColorFrontBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            GetUniversalRendererMethodInternal(renderer, "GetCameraColorFrontBuffer", out var rendererMethod);
            if (rendererMethod == null)
            {
                // Call UniversalRenderer.m_ColorBufferSystem.GetFrontBuffer()
                return GetRTHandleFromColorBufferSystem(renderer, cmd, "GetFrontBuffer");
            }
            
            return (RTHandle) rendererMethod.Invoke(renderer, new object[] {cmd});
        }
        
        /// <seealso cref="UnityEngine.Rendering.Universal.UniversalRenderer.GetCameraColorBackBuffer"/>
        public static RTHandle GetCameraColorBackBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            GetUniversalRendererMethodInternal(renderer, "GetCameraColorBackBuffer", out var rendererMethod);
            if (rendererMethod == null)
            {
                // Call UniversalRenderer.m_ColorBufferSystem.GetBackBuffer()
                return GetRTHandleFromColorBufferSystem(renderer, cmd, "GetBackBuffer");
            }

            return (RTHandle) rendererMethod.Invoke(renderer, new object[] {cmd});
        }

        public static void ConfigureCameraColorTarget(this ScriptableRenderer renderer, RTHandle colorTarget)
        {
            var methodInfo = renderer.GetType().GetMethod("ConfigureCameraColorTarget", (BindingFlags.NonPublic | BindingFlags.Instance), null, new[] {typeof(RTHandle)}, null);
            methodInfo?.Invoke(renderer, new object[] {colorTarget});
        }

        #endregion
    }

    public static class CameraDataInternal
    {
        public static bool requireSrgbConversion(this CameraData cameraData)
        {
            PropertyInfo getRequireSrgbConversionBool = cameraData.GetType().GetProperty("requireSrgbConversion", BindingFlags.NonPublic | BindingFlags.Instance)
                                                        ?? throw new ArgumentNullException(nameof(cameraData));
            return (bool) getRequireSrgbConversionBool.GetValue(cameraData);
        }
    }

    public static class RenderingUtilsInternal
    {
        public static RenderTargetIdentifier GetCameraTargetIdentifier(ref RenderingData renderingData)
        {
            // Note: We need to get the cameraData.targetTexture as this will get the targetTexture of the camera stack.
            // Overlay cameras need to output to the target described in the base camera while doing camera stack.
            ref CameraData cameraData = ref renderingData.cameraData;

            RenderTargetIdentifier cameraTarget = (cameraData.targetTexture != null) ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                if (cameraData.xr.singlePassEnabled)
                {
                    cameraTarget = cameraData.xr.renderTarget;
                }
                else
                {
                    int depthSlice = cameraData.xr.GetTextureArraySlice();
                    cameraTarget = new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, depthSlice);
                }
            }
#endif

            return cameraTarget;
        }
        
        public static void FinalBlit(CommandBuffer cmd, ref CameraData cameraData, RTHandle source, RTHandle destination)
        {
            // TODO: Final blit pass should always blit to backbuffer. The first time we do we don't need to Load contents to tile.
            // We need to keep in the pipeline of first render pass to each render target to properly set load/store actions.
            // meanwhile we set to load so split screen case works.
            RenderBufferLoadAction loadAction = RenderBufferLoadAction.DontCare;
            if (!cameraData.isSceneViewCamera && !cameraData.isDefaultViewport)
                loadAction = RenderBufferLoadAction.Load;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                loadAction = RenderBufferLoadAction.Load;
#endif
            RenderBufferStoreAction storeAction = RenderBufferStoreAction.Store;
            Material blitMaterial = Blitter.GetBlitMaterial(destination.rt.dimension);
            int passIndex = source.rt.filterMode == FilterMode.Bilinear ? 1 : 0;

            // Reflection invocation
            Type[] parameterTypes = {typeof(CommandBuffer), typeof(CameraData), typeof(RTHandle), typeof(RenderBufferLoadAction), typeof(RenderBufferStoreAction), typeof(Material), typeof(int)};
            object[] parameters = {cmd, cameraData, source, destination, loadAction, storeAction, blitMaterial, passIndex};

            var method = typeof(RenderingUtils).GetMethod("FinalBlit", BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
            method?.Invoke(typeof(RenderingUtils), parameters);
        }
    }

    public static class CustomPostProcessRenderingUtils
    {
        private static readonly int PostBufferID = Shader.PropertyToID("_InputTexture");
        private static readonly int scaleBiasID = Shader.PropertyToID("_ScaleBias");

        public static void SetPostProcessInputTexture(this Material mat, RTHandle source) { mat.SetTexture(PostBufferID, source.rt); }

        public static void SetPostProcessInputTexture(this CommandBuffer cmd, RTHandle source) { cmd.SetGlobalTexture(PostBufferID, source.nameID); }

        public static void DrawFullScreen(this Material material, CommandBuffer cmd, RTHandle source, RTHandle destination, int pass = 0)
        {
            Blitter.BlitCameraTexture(cmd, source, destination, material, pass);
        }
    }
}