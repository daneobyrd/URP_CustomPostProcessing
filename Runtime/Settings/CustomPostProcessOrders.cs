using System;
using UnityEngine;

namespace CustomPostProcessing.UniversalRP
{
    [Serializable]
    public partial class CustomPostProcessOrders
    {
        #region SerializeFields

        [SerializeField] [InspectorName("Before Transparents")]
        internal CustomPostProcessVolumeComponentList m_BeforeTransparentCustomPostProcesses = new(CustomPostProcessInjectionPoint.BeforeTransparents);

        [SerializeField] [InspectorName("Before Post Process")]
        internal CustomPostProcessVolumeComponentList m_BeforePostProcessCustomPostProcesses = new(CustomPostProcessInjectionPoint.BeforePostProcess);

        [SerializeField] [InspectorName("After Post Process")]
        internal CustomPostProcessVolumeComponentList m_AfterPostProcessCustomPostProcesses = new(CustomPostProcessInjectionPoint.AfterPostProcess);

        #endregion

        #region Data Accessors

        public CustomPostProcessVolumeComponentList beforeTransparentCustomPostProcesses { get => m_BeforeTransparentCustomPostProcesses; set => m_BeforeTransparentCustomPostProcesses = value; }

        public CustomPostProcessVolumeComponentList beforePostProcessCustomPostProcesses { get => m_BeforePostProcessCustomPostProcesses; set => m_BeforePostProcessCustomPostProcesses = value; }

        public CustomPostProcessVolumeComponentList afterPostProcessCustomPostProcesses { get => m_AfterPostProcessCustomPostProcesses; set => m_AfterPostProcessCustomPostProcesses = value; }

        #endregion


        public bool IsCustomPostProcessRegistered(System.Type customPostProcessType)
        {
            string type = customPostProcessType.AssemblyQualifiedName;
            return beforeTransparentCustomPostProcesses.Contains(type)
                   || beforePostProcessCustomPostProcesses.Contains(type)
                   || afterPostProcessCustomPostProcesses.Contains(type);
        }

        public bool IsCustomPostProcessRegistered<T>(T customPostProcessVolumeComponent) where T : CustomPostProcessVolumeComponent
        {
            return IsCustomPostProcessRegistered(customPostProcessVolumeComponent.GetType());
        }
    }
}