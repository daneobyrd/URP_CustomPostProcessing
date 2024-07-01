using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomPostProcessing.UniversalRP
{
    using static CustomPostProcessInjectionPoint;

    /// <summary>
    /// Based on HDRP's CustomPostProcessVolumeComponent.
    /// </summary>
    public abstract class CustomPostProcessVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        private bool m_IsInitialized;
        
        // Keep track of all the instances alive of the custom post process component so we can release them when needed
        private static HashSet<CustomPostProcessVolumeComponent> instances = new();

        protected CustomPostProcessVolumeComponent()
        {
            string className = GetType().ToString();
            int dotIndex = className.LastIndexOf(".", System.StringComparison.Ordinal) + 1;
            displayName = className[dotIndex..];
        }

        public Material material;

        // const string kShaderPath;

        /// <summary>
        /// True if you want your custom post process to be visible in the scene view. False otherwise.
        /// </summary>
        public bool visibleInSceneView => true;

        public abstract bool IsActive();

        /// <note>As of URP 14.0, Unity does not provide a way to determine TileCompatibility for any user-authored <see cref="ScriptableRenderPass"/>.</note>
        public virtual bool IsTileCompatible() { return false; }

        public virtual CustomPostProcessInjectionPoint injectionPoint => AfterPostProcess;

        #region Setup

        /// <summary>
        /// This function must call <see cref="Initialize(String)"/> or <see cref="Initialize(Shader)"/>..
        /// </summary>
        /// <param name="resourceData">Optional ScriptableObject containing shader resources or texture resources.</param>
        protected abstract void Setup(ScriptableObject resourceData);
        internal void SetupInternal(ScriptableObject resourceData = null)
        {
            if (!m_IsInitialized)
            {
                Setup(resourceData);

                m_IsInitialized = true;
                instances.Add(this);
            }
        }

        #endregion

        #region Material Initialization

        /// <inheritdoc cref="CoreUtils.CreateEngineMaterial(Shader)"/>
        protected void Initialize(Shader shader)
        {
            material ??= CoreUtils.CreateEngineMaterial(shader);
        }

        /// <inheritdoc cref="CoreUtils.CreateEngineMaterial(String)"/>
        protected void Initialize(string kShaderPath)
        {
            material ??= CoreUtils.CreateEngineMaterial(kShaderPath);
        }

        #endregion

        /// <example>
        /// <code>
        /// material.SetFloat(ShaderIDs.Opacity, opacity.value);
        /// cmd.SetPostProcessInputTexture(source);
        /// material.DrawFullScreen(cmd, srcRT, destination);
        /// </code>
        /// </example>
        public abstract void Render(CommandBuffer cmd, CameraData cameraData, RTHandle source, RTHandle destination);

        #region Cleanup

        // Cleanup any resources created for this effect.
        public virtual void Cleanup()
        {
            CoreUtils.Destroy(material);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CleanupInternal();
        }

        internal void CleanupInternal()
        {
            if (m_IsInitialized)
                Cleanup();

            m_IsInitialized = false;
            instances.Remove(this);
        }

        internal static void CleanupAllCustomPostProcesses()
        {
            foreach (var instance in instances.ToList()) // Copy to remove elements safely
                instance.CleanupInternal();
        }

        #endregion
    }
}