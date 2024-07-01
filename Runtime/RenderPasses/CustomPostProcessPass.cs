using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomPostProcessing.UniversalRP
{
    using static CustomPostProcessCore;

    /// <summary>
    /// This ScriptableRenderPass renders all CustomPostProcessVolumeComponents for the specified injection point.
    /// The source RT is the camera's backbuffer and the destination RT is the camera's front-buffer.
    /// After each effect is rendered, the two RTs are swapped in preparation for future effects.
    /// </summary>
    [Serializable]
    public class CustomPostProcessPass : ScriptableRenderPass
    {
        // private int m_PassIndex;
        private ProfilingSampler m_ProfilingSampler;

        private RenderTextureDescriptor m_Descriptor;
        private RTHandle m_TempTarget1;
        private RTHandle m_TempTarget2;
        private RTHandle m_Destination;

        // User listed volume components from Project Settings->Graphics->URP Custom Post-Processing
        [SerializeField] [HideInInspector] private CustomPostProcessVolumeComponentList _componentListData;

        // Optional shader resources, accessible for CustomPPVolumeComponents
        private readonly ScriptableObject m_ResourceData;

        private readonly bool injectedBeforeTransparents;

        // Renderer is using swap buffer system
        private bool m_UseSwapBuffer;

        /// <summary>
        /// One or more requirements for pass. Based on chosen flags certain passes will be added to the pipeline.
        /// </summary>
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;

        public CustomPostProcessPass(CustomPostProcessVolumeComponentList volumeComponentList, ScriptableObject resourceData = null)
        {
            _componentListData = volumeComponentList;
            var injectionPoint = _componentListData.injectionPoint;

            renderPassEvent = (RenderPassEvent) injectionPoint;

            m_ProfilingSampler = new ProfilingSampler($"Custom {injectionPoint} Pass");

            m_ResourceData = resourceData;

            #region Taken From FullScreenPassRendererFeature
            
            injectedBeforeTransparents = injectionPoint <= CustomPostProcessInjectionPoint.BeforeTransparents;

            if (!injectedBeforeTransparents)
            {
                // Removing Color flag in order to avoid unnecessary CopyColor pass
                // Does not apply to before rendering transparents, due to how depth and color are being handled until
                // that injection point.
                // modifiedRequirements ^= ScriptableRenderPassInput.Color;
            }

            // ConfigureInput(modifiedRequirements);

            #endregion
        }

        public void Setup(in RenderTextureDescriptor baseDescriptor)
        {
            m_Descriptor                  = baseDescriptor;
            m_Descriptor.useMipMap        = false;
            m_Descriptor.autoGenerateMips = false;

            m_Descriptor.msaaSamples     = 1; // to disable MSAA
            m_Descriptor.depthBufferBits = (int) DepthBits.None;

            m_Destination   = k_CameraTarget;
            m_UseSwapBuffer = true;

            foreach (var type in _componentListData)
            {
                var customPP = GetCustomPostProcessComponent(type);
                
                customPP.SetupInternal(m_ResourceData);
            }
        }

        public bool IsActive()
        {
            foreach (var type in _componentListData)
            {
                var customPP = GetCustomPostProcessComponent(type);

                if (customPP != null && customPP.IsActive())
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { ResetTarget(); }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Regular render path (not on-tile) - we do everything in a single command buffer as it
            // makes it easier to manage temporary targets' lifetime
            var cmd = CommandBufferPool.Get();
            cmd.name = m_ProfilingSampler.name;

            ExecutePass(cmd, ref renderingData);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            foreach (var type in _componentListData)
            {
                var customPP = GetCustomPostProcessComponent(type);
                customPP.Cleanup();
            }

            m_TempTarget1?.Release();
            m_TempTarget2?.Release();
        }

        #region GetCompatibleDescriptor

        RenderTextureDescriptor GetCompatibleDescriptor() => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None) =>
            GetCompatibleDescriptor(m_Descriptor, width, height, format, depthBufferBits);

        private static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None)
        {
            desc.depthBufferBits = (int) depthBufferBits;
            desc.msaaSamples     = 1;
            desc.width           = width;
            desc.height          = height;
            desc.graphicsFormat  = format;
            return desc;
        }

        #endregion

        /// <summary>
        /// Draws all active <see cref="CustomPostProcessVolumeComponent"/> for this <see cref="CustomPostProcessPass"/>.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="renderingData">Current rendering state information.</param>
        /// <remarks>
        /// Each <see cref="CustomPostProcessVolumeComponent"/> writes to the camera front buffer.<br/>
        /// With multiple effects, Swap() is called to setting the current destination as the source for the next active volume component.<br/>
        /// After all effects have been drawn, the contents of the front buffer is blit to the back buffer.
        /// </remarks>
        private void ExecutePass(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // In some cases, accessing values by reference can improve performance by avoiding potentially high-overhead copy operations.
            // For example, the following statements shows how to define a ref local variable for a reference value.
            ref CameraData cameraData = ref renderingData.cameraData;
            ref ScriptableRenderer renderer = ref renderingData.cameraData.renderer;

            #region SwapBuffer

            RTHandle source = k_CameraTarget;
            RTHandle destination = null;

            if (m_UseSwapBuffer)
            {
                renderer.EnableSwapBufferMSAA(false);
                source      = renderer.cameraColorTargetHandle;
                destination = injectedBeforeTransparents ? renderer.GetCameraColorBackBuffer(cmd) : renderer.GetCameraColorFrontBuffer(cmd);
            }


            RTHandle GetSource() => source;

            RTHandle GetDestination()
            {
                // Temp Targets
                if (destination == null)
                {
                    RenderingUtils.ReAllocateIfNeeded(ref m_TempTarget1, GetCompatibleDescriptor(), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempTarget1");
                    destination = m_TempTarget1;
                }
                else if (destination == source && m_Descriptor.msaaSamples > 1)
                {
                    // Avoid using m_Source.id as new destination, it may come with a depth buffer that we don't want, may have MSAA that we don't want etc
                    RenderingUtils.ReAllocateIfNeeded(ref m_TempTarget2, GetCompatibleDescriptor(), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempTarget2");
                    destination = m_TempTarget2;
                }

                return destination;
            }

            // Adapted from Unity's PostProcessPass.cs
            void Swap(ref ScriptableRenderer r)
            {
                if (m_UseSwapBuffer)
                {
                    r.SwapColorBuffer(cmd);
                    source = r.cameraColorTargetHandle;

                    destination = r.GetCameraColorFrontBuffer(cmd);
                }
                else
                {
                    CoreUtils.Swap(ref source, ref destination);
                }
            }

            #endregion

            // Render custom effects
            foreach (var type in _componentListData)
            {
                var customPP = GetCustomPostProcessComponent(type);

                if (customPP == null || !customPP.IsActive())
                {
                    continue;
                }

#if UNITY_EDITOR
                // if visibleInSceneView and scene view mode has post process enabled
                if (cameraData.isSceneViewCamera)
                {
                    if (customPP.visibleInSceneView && !CoreUtils.ArePostProcessesEnabled(cameraData.camera))
                    {
                        continue;
                    }
                }
#endif

                // Back Buffer -> Front Buffer
                // or Back Buffer -> Temp Target 1
                // or Temp Target # -> Temp Target #
                customPP.Render(cmd, cameraData, source, destination);
                Swap(ref renderer);
            }

            #region Final Blit of Pass
            
            // Final Blit of Pass
            var colorLoadAction = RenderBufferLoadAction.DontCare;
            if (destination == k_CameraTarget && !cameraData.isDefaultViewport)
                colorLoadAction = RenderBufferLoadAction.Load;

            var m_BlitMaterial = Blitter.GetBlitMaterial(TextureDimension.Tex2D);

            if (m_UseSwapBuffer)
            {
                Blitter.BlitCameraTexture(cmd, source, GetDestination(), colorLoadAction, RenderBufferStoreAction.Store, m_BlitMaterial, 0);
                renderer.ConfigureCameraColorTarget(destination);
                Swap(ref renderer);
            }
            // as of URP 14.11
            // TODO: Implement swapbuffer in 2DRenderer so we can remove this
            // For now, when render post-processing in the middle of the camera stack (not resolving to screen)
            // we do an extra blit to ping pong results back to color texture. In future we should allow a Swap of the current active color texture
            // in the pipeline to avoid this extra blit.
            else
            {
                var firstSource = GetSource();
                Blitter.BlitCameraTexture(cmd, firstSource, GetDestination(), colorLoadAction, RenderBufferStoreAction.Store, m_BlitMaterial, 0);
                Blitter.BlitCameraTexture(cmd, GetDestination(), m_Destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_BlitMaterial, m_Destination.rt?.filterMode == FilterMode.Bilinear ? 1 : 0);
            }
            #endregion

        }
    }
}