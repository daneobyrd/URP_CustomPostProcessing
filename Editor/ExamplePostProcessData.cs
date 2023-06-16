#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace URP_CustomPostProcessing
{
    // This script was generated from a template.
    
    // This example class is based on URP's PostProcessData class.
    // Edit this file to reload the resources you need.
    // Then create a new scriptable object using the context menu action
    // listed in the MenuItem attribute of CreateExamplePostProcessData(). 
    
    // This example has been made private and is not Serialized to avoid showing up in the editor.
    // [Serializable]
    private class ExamplePostProcessData : ScriptableObject
    {
        // Change this to the root folder of all the resources you need
        private static string basePath = UniversalRenderPipelineAsset.packagePath;
        
        #region Editor
        
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateExamplePostProcessDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<ExamplePostProcessData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, basePath);
                Selection.activeObject = instance;
            }
        }
        
        [MenuItem("Assets/Create/Rendering/Custom Post Processing/New ExamplePostProcessData Asset", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        static void CreateExamplePostProcessData() { ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateExamplePostProcessDataAsset>(), "ExamplePostProcessData.asset", null, null); }
        
#endif
        
        #endregion
        
        #region Resources
        
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            // Add your shader references here
            
            // Example:
            // [Reload("Shaders/PostProcessing/StopNaN.shader")]
            // public Shader stopNanPS;
            
        }
        
        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            // Add your texture references here
            
            // Examples:
            //-------------------------------------------------//
            /*       Array of ordinal texture references       */
            //-------------------------------------------------//
            
            // [Reload("Textures/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            // public Texture2D[] blueNoise16LTex;
            
            //-------------------------------------------------//
            /*       Array of various texture references       */
            //-------------------------------------------------//
            
            /*
            [Reload(new[]
                {
                    "Textures/FilmGrain/Thin01.png",
                    "Textures/FilmGrain/Thin02.png",
                    "Textures/FilmGrain/Medium01.png",
                    "Textures/FilmGrain/Medium02.png",
                    "Textures/FilmGrain/Medium03.png",
                    "Textures/FilmGrain/Medium04.png",
                    "Textures/FilmGrain/Medium05.png",
                    "Textures/FilmGrain/Medium06.png",
                    "Textures/FilmGrain/Large01.png",
                    "Textures/FilmGrain/Large02.png"
                })
            ]
            public Texture2D[] filmGrainTex;
            */
            
            //-------------------------------------------------//
            /*            Single texture reference             */
            //-------------------------------------------------//
            
            // [Reload("Textures/SMAA/AreaTex.tga")]
            // public Texture2D smaaAreaTex;
        }
        
        public ShaderResources shaderResources;
        public TextureResources textureResources;
        
        #endregion
    }
}