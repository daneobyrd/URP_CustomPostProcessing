using UnityEditor;

namespace CustomPostProcessing.UniversalRP.Editor
{
    using static CustomPostProcessUI;
    [CustomEditor(typeof(CustomPostProcessSettings))]
    [CanEditMultipleObjects]
    public class CustomPostProcessSettingsEditor : SettingsEditor
    {
        private SerializedCustomPostProcessSettings m_SerializedCustomPostProcessSettings;

        private void OnEnable() { m_SerializedCustomPostProcessSettings = new SerializedCustomPostProcessSettings(serializedObject); }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() => CustomPostProcessSettings.Instance.GetSettingsProvider();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var serialized = m_SerializedCustomPostProcessSettings;

            serialized.Update();

            PostProcessSettingsDrawer.Draw(serialized, this);

            serialized.Apply();

            serializedObject.ApplyModifiedProperties();
        }
    }
}