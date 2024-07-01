#if UNITY_EDITOR

namespace CustomPostProcessing.UniversalRP
{
    public partial class CustomPostProcessSettings
    {
        // Overridden Virtual Methods used when Copying Data to a new Asset
        protected override void Clear() { m_CustomPostProcessOrders.Clear(); }

        protected override void CopyDataFrom(CustomPostProcessSettings origin)
        {
            var originSettings = origin.customPostProcessOrders;
            originSettings.CopySettingsListTo(m_CustomPostProcessOrders.m_BeforeTransparentCustomPostProcesses);
            originSettings.CopySettingsListTo(m_CustomPostProcessOrders.m_BeforePostProcessCustomPostProcesses);
            originSettings.CopySettingsListTo(m_CustomPostProcessOrders.m_AfterPostProcessCustomPostProcesses);
        }
    }
}
#endif