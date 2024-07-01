using UnityEditor;
using UnityEngine.UIElements;

namespace CustomPostProcessing.UniversalRP.Editor
{
    [CustomPropertyDrawer(typeof(CustomPostProcessOrders))]
    public class CustomPostProcessOrdersPropertyDrawer : RelativePropertiesDrawer
    {
        protected override string[] relativePropertiesNames => new[]
        {
            "m_BeforeTransparentCustomPostProcesses",
            "m_BeforePostProcessCustomPostProcesses",
            "m_AfterPostProcessCustomPostProcesses"
        };
    }
}