using System;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CustomPostProcessing.UniversalRP
{
    public static class CustomPostProcessCore
    {
        internal const string PackagePath = "Packages/db.urp.custom-post-processing/";
        internal const string PackageRoot = "Packages/URP_CustomPostProcessing/";
        internal const string DefaultSettingsAssetPath = PackagePath + "Runtime/Data/";

        public static VolumeStack GetVolumeStack(this CameraData cameraData) => cameraData.camera.GetUniversalAdditionalCameraData().volumeStack;

        /// <summary>
        /// Get <see cref="CustomPostProcessVolumeComponent"/> from the <see cref="VolumeStack"/> of <paramref name="derivedType"/>.
        /// </summary>
        /// <param name="stack">Current or custom <see cref="VolumeStack"/> containing Volume blending update for <see cref="VolumeComponent"/>.</param>
        /// <param name="derivedType">Generic <see cref="System.Type"/> derived from <see cref="CustomPostProcessVolumeComponent"/>.</param>
        public static CustomPostProcessVolumeComponent GetCustomPostProcessComponent(this VolumeStack stack, Type derivedType)
        {
            #if UNITY_EDITOR
            Assert.IsTrue(typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(derivedType));
            #endif
            
            return stack.GetComponent(derivedType) as CustomPostProcessVolumeComponent;
        }

        /// <summary>
        /// Get <see cref="CustomPostProcessVolumeComponent"/> using the <paramref name="derivedType"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="VolumeStack"/> used is <see cref="VolumeManager"/>.instance.stack
        /// </remarks>
        /// <param name="derivedType">Type derived from <see cref="CustomPostProcessVolumeComponent"/>.</param>
        public static CustomPostProcessVolumeComponent GetCustomPostProcessComponent(Type derivedType) { return VolumeManager.instance.stack.GetCustomPostProcessComponent(derivedType); }

        /// <summary>
        /// Get <see cref="CustomPostProcessVolumeComponent"/> from the <see cref="UniversalAdditionalCameraData"/>.volumeStack using the <paramref name="derivedType"/>.
        /// </summary>
        /// <param name="cameraData"><see cref="CameraData"/> struct for the currently active camera.</param>
        /// <param name="derivedType">Generic <see cref="System.Type"/> derived from <see cref="CustomPostProcessVolumeComponent"/>.</param>
        /// <remarks>
        /// <see cref="VolumeStack"/> used is <c>CameraData.camera.GetUniversalAdditionalCameraData().volumeStack;</c>
        /// </remarks>
        public static CustomPostProcessVolumeComponent GetCustomPostProcessComponent(this CameraData cameraData, Type derivedType)
        {
            var stack = cameraData.GetVolumeStack();
            return stack != null
                ? GetCustomPostProcessComponent(stack, derivedType) // from camera's stack
                : GetCustomPostProcessComponent(derivedType);       // from VolumeManager.instance.stack
        }
        
        // Checks packages first and if it doesn't exist there checks assets.
        public static T LoadAssetFromPackage<T>(string packageFilePath) where T : UnityEngine.Object
        {
            // try to load as a package path
            var asset = AssetDatabase.LoadAssetAtPath<T>(packageFilePath);
            if (asset != null)
                return asset;
 
            // try to convert path to a package path from a presumed package display path
            var splits = packageFilePath.Split('/');
            if (splits.Length >= 2)
            {
                var possiblePackageDisplayName = splits[1];
                var possiblePackageName = PackageDisplayNameToPackageName(possiblePackageDisplayName);
                if (!string.IsNullOrEmpty(possiblePackageName))
                {
                    splits[1] = possiblePackageName;
                    var possiblePackageFilePath = string.Join('/', splits);
 
                    var possibleAsset = AssetDatabase.LoadAssetAtPath<T>(possiblePackageFilePath);
                    if (possibleAsset != null)
                        return possibleAsset;
                }
            }
 
            // try to load as project asset path
            var possibleAssetFilePath = $"Assets/{packageFilePath}";
            return AssetDatabase.LoadAssetAtPath<T>(possibleAssetFilePath);
        }

        private static string PackageDisplayNameToPackageName(string packageDisplayName)
        {
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
 
            return packages
                   .Where(package => package.displayName == packageDisplayName)
                   .Select(package => package.name)
                   .FirstOrDefault();
        }
    }


    public static class CustomPostProcessMenuItems
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/Custom Post Processing/Blank Post-Process Data")]
        public static void CreateExamplePostProcessDataClass()
        {
            string templatePath = $"{CustomPostProcessCore.PackagePath}Editor/Templates/01-C# Script-ScriptableObject_NewPostProcessData.txt";
            
            const string destinationPath = "Assets/Scripts/Rendering/Data/";
            string scriptName = "NewPostProcessData.cs";

            var fullPath = destinationPath + scriptName;
            
            CoreUtils.EnsureFolderTreeInAssetFilePath(destinationPath);

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, fullPath);

            AssetDatabase.Refresh();
        }

        public static void CreatePostProcessVolumeComponent()
        {
            // string templatePath = $"{CustomPostProcessCore.PackagePath}/Editor/01-C# Script-ScriptableObject_NewPostProcessData.txt";
        }
#endif
    }
}