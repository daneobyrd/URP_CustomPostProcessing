using UnityEditor;
using UnityEditor.Rendering;

namespace CustomPostProcessing.UniversalRP.Editor
{
    using CED = CoreEditorDrawer<SerializedCustomPostProcessSettings>;

    internal static partial class CustomPostProcessUI
    {
        public static readonly CED.IDrawer PostProcessSettingsDrawer = CED.Group
        (
            CED.Group((serialized, owner) => { DrawSectionHeader(EditorGUIUtility.TrTextContent("Custom Post-Process Orders")); }),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawCustomPostProcess)
        );

        static void DrawCustomPostProcess(SerializedCustomPostProcessSettings serialized, UnityEditor.Editor owner)
        {
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.serializedCustomPostProcessOrders);
            serialized.Apply();
        }

        public static readonly CED.IDrawer ActivePostProcessDrawer = CED.Group
        (
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawReadOnlyCustomPostProcess)
        );

        private static void DrawReadOnlyCustomPostProcess(SerializedCustomPostProcessSettings serialized, UnityEditor.Editor owner)
        {
            serialized.serializedObject.Update();

            if (owner is not CustomPostProcessRenderFeatureEditor renderFeatureEditor)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorDefaultMargins))
            {
                renderFeatureEditor.ReadOnlyBeforeTransparents?.DoLayoutList();
                renderFeatureEditor.ReadOnlyBeforePostProcess?.DoLayoutList();
                renderFeatureEditor.ReadOnlyAfterPostProcess?.DoLayoutList();
            }

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}