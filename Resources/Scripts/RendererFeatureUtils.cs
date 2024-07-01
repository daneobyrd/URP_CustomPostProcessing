using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.Universal
{
    public static class RendererFeatureUtils
    {
        public static ScriptableRendererData FindRelatedRendererData<T>(this T rendererFeature) where T : ScriptableRendererFeature
        {
            var rendererDataList = GetActivePipelineRendererDataList();

            foreach (var rendererData in rendererDataList)
            {
                if (rendererData.Equals(null) || rendererData.rendererFeatures.Count == 0)
                    continue;
                
                // same as rendererData.TryGetRendererFeature but do not output feature
                foreach (var target in rendererData.rendererFeatures)
                {
                    if (target.GetType() == typeof(T) || target == rendererFeature)
                    {
                        return rendererData;
                    }
                }
            }

            return null;
        }

        /// <inheritdoc cref="ScriptableRendererData.TryGetRendererFeature{T}"/>
        public static bool TryGetRendererFeature<T>(out T rendererFeature, UniversalRenderPipelineAsset urpAsset = null) where T : ScriptableRendererFeature
        {
            ScriptableRendererData rendererData;

            if (urpAsset == null)
            {
                rendererData = GetActivePipelineDefaultRendererData();
                return rendererData.TryGetRendererFeature(out rendererFeature);
            }

            if (TryGetRendererData(out rendererData, pipelineAsset: urpAsset))
            {
                return rendererData.TryGetRendererFeature(out rendererFeature);
            }

            rendererFeature = null;
            return false;
        }

        public static bool TryGetRendererFeature<T>(this Camera camera, out T rendererFeature) where T : ScriptableRendererFeature
        {
            return camera.GetCameraRendererData().TryGetRendererFeature(out rendererFeature);
        }
        
        public static bool TryGetRendererFeature<T>(ScriptableRendererData rendererData, out T rendererFeature) where T : ScriptableRendererFeature
        {
            return rendererData.TryGetRendererFeature(out rendererFeature);
        }
        
        // Fetch Renderer from:
        // 1. UniversalAdditionalCameraData().scriptableRenderer (from camera or Camera.main)
        // 2. UniversalRenderPipelineAsset.scriptableRenderer (from pipelineAsset or UniversalRenderPipeline.asset)
        
        /// <summary>
        /// Attempts to get the ScriptableRendererData requested from several sources.
        /// </summary>
        /// <param name="rendererData">From the requested Camera or the currently active RenderPipelineAsset.</param>
        /// <param name="camera">Camera component with UniversalAdditionCameraData on the same GameObject.</param>
        /// <param name="pipelineAsset">If null, fallback is UniversalRenderPipeline.asset.</param>
        /// <returns>False if, 1) UniversalRenderPipeline.asset is null,  </returns>
        /// <exception cref="ArgumentNullException"><br/>Thrown if <paramref name="camera"/> is null and not using Camera.main fallback.</exception>
        /// <exception cref="NullReferenceException"><br/>Thrown if Camera.main is null.</exception>
        public static bool TryGetRendererData(out ScriptableRendererData rendererData, Camera camera = null, UniversalRenderPipelineAsset pipelineAsset = null)
        {
            rendererData = null;
            
            if (pipelineAsset is null || !pipelineAsset)
            {
                pipelineAsset = UniversalRenderPipeline.asset;

                if (pipelineAsset is null || !pipelineAsset)
                {
                    Debug.LogError("Scriptable Renderer could not be found because no RenderPipelineAsset has been assigned in Edit/Graphics Settings.");
                    return false;
                }
            }
            
            if (camera)
            {
                rendererData = (camera.GetCameraRendererData(pipelineAsset, useMainCameraFallback: true));
            }
            
            
            if (!rendererData)
            {
                rendererData = pipelineAsset.GetPipelineDefaultRendererData();
            }

            return (rendererData);
        }

        #region Get RendererData from RenderPipelineAsset
        
        public static ScriptableRendererData       GetActivePipelineDefaultRendererData() { return UniversalRenderPipeline.asset.GetPipelineDefaultRendererData(); }
        public static List<ScriptableRendererData> GetActivePipelineRendererDataList()    { return UniversalRenderPipeline.asset.GetPipelineRendererDataList(); }
        
        public static ScriptableRendererData GetPipelineDefaultRendererData(this UniversalRenderPipelineAsset pipelineAsset)
        {
            ScriptableRendererData rendererData = null;

            // if this file is in the Universal.Runtime Assembly
            try // get internal properties
            {
                rendererData = pipelineAsset.scriptableRendererData;
            }
            catch (MemberAccessException)
            {
                var propertyInfo = pipelineAsset.GetType().GetProperty("scriptableRendererData", BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null)
                {
                    rendererData = propertyInfo.GetValue(pipelineAsset) as ScriptableRendererData;
                }
            }

            if (rendererData != null)
            {
                return rendererData;
            }
            
            // should be the same as pipelineAsset.scriptableRendererData
            return pipelineAsset.GetPipelineRendererDataList()[pipelineAsset.m_DefaultRendererIndex];
        }

        public static List<ScriptableRendererData> GetPipelineRendererDataList(this UniversalRenderPipelineAsset urpAsset, bool sanitizeList = false)
        {
            List<ScriptableRendererData> rendererDataList;

            // if this file is in the Universal.Runtime Assembly
            // or m_RendererDataList is no longer internal:
            // get property directly
            try
            {
                rendererDataList = urpAsset.m_RendererDataList.ToList();
                return rendererDataList;
            }
            catch (MemberAccessException)
            {
                // else get property via Reflection
                rendererDataList = null;
                
                var dataListPropertyInfo = urpAsset.GetType().GetProperty("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);

                if (dataListPropertyInfo?.GetValue(urpAsset) is ScriptableRendererData[] dataListArray)
                {
                    rendererDataList = dataListArray.ToList();
                }
            }

            if (sanitizeList)
            {
                SanitizeRendererDataList(rendererDataList);
            }
            
            return rendererDataList;
        }
        
        #endregion
        
        #region Find ScriptableRendererData if it contains provided ScriptableRenderer
        
        public static ScriptableRendererData FindMatchingRendererData(this UniversalRenderPipelineAsset urpAsset, ScriptableRenderer matchingRenderer)
        {
            if (urpAsset.scriptableRenderer == matchingRenderer)
            {
                return urpAsset.scriptableRendererData;
            }
            
            var rendererDataList = urpAsset.GetPipelineRendererDataList();

            for (var i = 0; i < rendererDataList.Count; ++i)
            {
                // GetRenderer() ensures Renderer list and RendererData list are the same length
                // THUS... index i will return the correct ScriptableRendererData.
                if (urpAsset.GetRenderer(i) == matchingRenderer)
                {
                    return rendererDataList[i];
                }
            }

            return null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="urpAsset"></param>
        /// <param name="rendererToMatch">ScriptableRendererData useful for checking </param>
        /// <param name="sanitizeList">When true, removes all null RendererData or RendererData without RendererFeatures from list.</param>
        /// <returns></returns>
        public static List<ScriptableRendererData> FindMatchingRendererDataList(this UniversalRenderPipelineAsset urpAsset, ScriptableRenderer rendererToMatch, bool sanitizeList = false)
        {
            var rendererDataList = urpAsset.GetPipelineRendererDataList();

            if (rendererToMatch != null)
            {
                for (var i = 0; i < rendererDataList.Count; ++i)
                {
                    // GetRenderer() ensures Renderer list and RendererData list are the same length
                    // THUS... index i will return the correct ScriptableRendererData.
                    if (urpAsset.GetRenderer(i) == rendererToMatch)
                    {
                        return rendererDataList;
                    }
                }
            }

            if (sanitizeList)
            {
                SanitizeRendererDataList(rendererDataList);
            }

            return rendererDataList;
        }
        
        #endregion
        
        #region Find ScriptableRendererData for Camera's active ScriptableRenderer

        public static ScriptableRendererData GetCameraRendererData(this Camera camera, UniversalRenderPipelineAsset urpAsset = null,  bool useMainCameraFallback = false)
        {
            if ((camera is null | !camera))
            {
                if (useMainCameraFallback)
                {
                    camera = Camera.main;

                    if (!camera)
                    {
                        throw new NullReferenceException($"Could not find a Camera component tagged 'Main Camera' in the active Scene: \"{SceneManager.GetActiveScene().name}\".");
                    }
                }
                else
                {
                    throw new ArgumentNullException(nameof(camera), "The Camera parameter is null.");
                }
            }
            
            var additionalCameraData = camera.GetUniversalAdditionalCameraData();

            urpAsset ??= UniversalRenderPipeline.asset;

            return urpAsset.FindMatchingRendererData(additionalCameraData.scriptableRenderer);
        }
        
        public static List<ScriptableRendererData> GetCameraRendererDataList(this Camera camera, UniversalRenderPipelineAsset urpAsset = null, bool useMainCameraFallback = false)
        {
            if ((camera is null | !camera))
            {
                if (useMainCameraFallback)
                {
                    camera = Camera.main;

                    if (!camera)
                    {
                        throw new NullReferenceException($"Could not find a Camera component tagged 'Main Camera' in the active Scene: \"{SceneManager.GetActiveScene().name}\".");
                    }
                }
                else
                {
                    throw new ArgumentNullException(nameof(camera), "The Camera parameter is null.");
                }
            }

            var additionalCameraData = camera.GetUniversalAdditionalCameraData();

            urpAsset ??= UniversalRenderPipeline.asset;

            return urpAsset.FindMatchingRendererDataList(additionalCameraData.scriptableRenderer);
        }

        #endregion
        
        // Removes all null RendererData and, by default, removes all RendererData without rendererFeatures
        private static void SanitizeRendererDataList(List<ScriptableRendererData> rendererDataList, bool requireRendererFeatures = true)
        {
            rendererDataList.RemoveAll(data => !data);
            if (requireRendererFeatures)
            {
                rendererDataList.RemoveAll(data => data.rendererFeatures.Count == 0);
            }
        }

        private static bool DuplicateFeatureCheckAcrossRenderers(List<ScriptableRendererData> rendererDataList, Type type)
        {
            var isSingleFeature = type.GetCustomAttribute(typeof(DisallowMultipleRendererFeature));
            int instancesFound = 0;

            foreach (var rendererData in rendererDataList)
            {
                var list = rendererData.rendererFeatures.FindAll(renderFeature => renderFeature.GetType() == type);
                instancesFound += list.Count;
            }

            return (isSingleFeature != null) && instancesFound > 1;
        }
    }
}