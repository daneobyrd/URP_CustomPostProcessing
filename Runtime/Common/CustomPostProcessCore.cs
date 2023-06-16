using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace URP_CustomPostProcessing
{
    public static class CustomPostProcessCore
    {
        public static readonly string packagePath = "Packages/db.urp.custom-post-processing";
    }

    public static class CustomPostProcessingMenuItems
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/Custom Post Processing/Empty URP Post-process Data")]
        public static void CreateExamplePostProcessDataClass()
        {
            string templatePath = $"{CustomPostProcessCore.packagePath}/Editor/01-C# Script-ScriptableObject_NewPostProcessData.txt";

            const string destinationPath = "Assets/Scripts/Rendering/Data";
            string scriptName = "EmptyPostProcessData.cs";
            var fullPath = destinationPath + "/" + scriptName;

            // Replace fullPath with just the scriptName and delete the Creating folders section
            // if you simply want the file to be created in your currently active folder.

            #region Create Folders if Needed

            // There HAS to be a better way to create a script in a path, and create that directory if it does not exist.
            string segment0 = "Assets";
            string segment1 = "Scripts";
            string segment2 = "Rendering";
            string segment3 = "Data";

            string[] segments = {segment0, segment1, segment2, segment3};

            var baseDir = $"{segment0}";
            var subDir1 = $"{segment0}/{segment1}";
            var subDir2 = $"{segment0}/{segment1}/{segment2}";
            var subDir3 = $"{segment0}/{segment1}/{segment2}/{segment3}";

            string[] pathHierarchy = {baseDir, subDir1, subDir2, subDir3};

            // If path doesn't exist, create the necessary subFolders
            for (var i = 1; i < pathHierarchy.Length; i++)
            {
                var parentFolder = pathHierarchy[i - 1];
                var newSubFolder = pathHierarchy[i];

                var existingSubFolders = AssetDatabase.GetSubFolders(parentFolder);
                if (existingSubFolders.Contains(newSubFolder)) continue;

                AssetDatabase.CreateFolder(parentFolder, segments[i]);
                AssetDatabase.Refresh();
            }

            #endregion

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, fullPath);

            AssetDatabase.Refresh();
        }
#endif
    }

    public static class RenderTargetHandleExtension
    {
        public static void Release(this RenderTargetHandle renderTargetHandle, CommandBuffer cmd) { cmd.ReleaseTemporaryRT(renderTargetHandle.id); }
    }

    public static class ScriptableRendererInternal
    {
        // Adapted from ScriptableRendererData
        /// <summary>
        /// Returns true if contains renderer feature with specified type.
        /// </summary>
        /// <typeparam name="T">Renderer Feature type.</typeparam>
        /// <returns></returns>
        private static bool TryGetRendererFeature<T>(in ScriptableRendererData rendererData, out T rendererFeature) where T : ScriptableRendererFeature
        {
            foreach (var target in rendererData.rendererFeatures)
            {
                if (target.GetType() == typeof(T))
                {
                    rendererFeature = target as T;
                    return true;
                }
            }

            rendererFeature = null;
            return false;
        }

        public static bool TryGetRendererFeature<T>(out T rendererFeature) where T : ScriptableRendererFeature
        {
            GetReflectedScriptableRendererData(out var rendererData);
            return TryGetRendererFeature<T>(rendererData, out rendererFeature);
        }

        private static void GetReflectedScriptableRendererData(out ScriptableRendererData rendererData)
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

        public static void SwapColorBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            const string methodName = "SwapColorBuffer";
            GetUniversalRendererMethodInternal(renderer, methodName, out MethodInfo swapColorBufferMethod);
            swapColorBufferMethod.Invoke(renderer, new object[] {cmd});
        }

        private static bool TryGetColorBufferSystem(ScriptableRenderer renderer, out FieldInfo colorBufferSystemField)
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

        private static RenderTargetIdentifier GetCameraColorBufferInternal(ScriptableRenderer renderer, CommandBuffer cmd, in string methodName)
        {
            RenderTargetIdentifier fallbackCameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

            bool fetchColorBufferSystem = TryGetColorBufferSystem(renderer, out FieldInfo colorBufferSystemField);
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
            MethodInfo methodInfo = colorBufferSystemType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            object bufferObject = methodInfo?.Invoke(colorBufferSystemObject, new object[] {cmd});

            if (methodInfo == null || bufferObject == null)
            {
#if UNITY_EDITOR
#if !UNITY_2022_1_OR_NEWER // 2021
                {
                    // GetBackBuffer method was added in 2022
                    if (methodName != "GetBackBuffer")
                    {
                        Debug.LogError
                        (
                            $"Unable to access m_ColorBufferSystem.{methodName}() via System.Reflection." +
                            "Check the provided method name and/or whether the method exists."
                        );
                    }
                }
#endif
#endif
                return fallbackCameraTarget;
            }

            // TODO: Replace usage of RenderTargetHandle with RTHandle for ver. 2022+
            RenderTargetHandle colorBufferHandle = (RenderTargetHandle) bufferObject;
            return colorBufferHandle.id;
        }

        public static RenderTargetIdentifier GetCameraColorBackBuffer(this ScriptableRenderer renderer, CommandBuffer cmd) { return GetCameraColorBufferInternal(renderer, cmd, "GetBackBuffer"); }

        public static RenderTargetIdentifier GetCameraColorFrontBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            GetUniversalRendererMethodInternal(renderer, "GetCameraColorFrontBuffer", out var rendererMethod);
            return (RenderTargetIdentifier) rendererMethod.Invoke(renderer, new object[] {cmd});
            // return GetCameraColorBufferInternal(renderer, cmd, "GetFrontBuffer");
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
}