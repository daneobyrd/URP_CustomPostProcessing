using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CustomPostProcessing.UniversalRP
{
    [Settings(SettingsUsage.RuntimeProject, displayPath: "Graphics/URP Custom Post-Processing")]
    public partial class CustomPostProcessSettings : Settings<CustomPostProcessSettings>
    {
        [SerializeField] private CustomPostProcessOrders m_CustomPostProcessOrders = new();

        internal CustomPostProcessOrders customPostProcessOrders => m_CustomPostProcessOrders;

        /// <summary>
        /// Get the post process order list for the desired InjectionPoint.
        /// </summary>
        /// <returns>A list of Type.AssemblyQualifiedName strings that the user has added in Edit/ProjectSettings.</returns>
        public CustomPostProcessVolumeComponentList GetPostProcessList(CustomPostProcessInjectionPoint point)
        {
            return point switch
            {
                CustomPostProcessInjectionPoint.BeforeTransparents => customPostProcessOrders.beforeTransparentCustomPostProcesses,
                CustomPostProcessInjectionPoint.BeforePostProcess  => customPostProcessOrders.beforePostProcessCustomPostProcesses,
                CustomPostProcessInjectionPoint.AfterPostProcess   => customPostProcessOrders.afterPostProcessCustomPostProcesses,
                _                                                  => null
            };
        }

        public bool IsCustomPostProcessRegistered(Type customPostProcessType) { return customPostProcessOrders.IsCustomPostProcessRegistered(customPostProcessType); }

        public bool IsCustomPostProcessRegistered(CustomPostProcessVolumeComponent customPostProcessVolumeComponent)
        {
            return customPostProcessOrders.IsCustomPostProcessRegistered(customPostProcessVolumeComponent);
        }
    }
}