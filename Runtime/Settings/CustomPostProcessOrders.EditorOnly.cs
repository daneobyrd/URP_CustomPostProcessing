#if UNITY_EDITOR

using System;

namespace CustomPostProcessing.UniversalRP
{
    public partial class CustomPostProcessOrders
    {
        internal void Clear()
        {
            m_BeforeTransparentCustomPostProcesses.Clear();
            beforeTransparentCustomPostProcesses = m_BeforeTransparentCustomPostProcesses;

            m_BeforePostProcessCustomPostProcesses.Clear();
            beforePostProcessCustomPostProcesses = m_BeforePostProcessCustomPostProcesses;

            m_AfterPostProcessCustomPostProcesses.Clear();
            afterPostProcessCustomPostProcesses = m_AfterPostProcessCustomPostProcesses;
        }

        internal void CopySettingsListTo(CustomPostProcessVolumeComponentList newList)
        {
            var injectionPoint = newList.injectionPoint;
            newList.Clear();

            var originList = injectionPoint switch
            {
                CustomPostProcessInjectionPoint.BeforeTransparents => m_BeforeTransparentCustomPostProcesses,
                CustomPostProcessInjectionPoint.BeforePostProcess  => m_BeforePostProcessCustomPostProcesses,
                CustomPostProcessInjectionPoint.AfterPostProcess   => m_AfterPostProcessCustomPostProcesses,
                _                                                  => null
            };

            if (originList == null)
            {
                throw new ArgumentNullException($"{newList}'s InjectionPoint is invalid.");
            }

            foreach (var type in originList)
            {
                newList.Add(type);
            }
        }
    }
}
#endif