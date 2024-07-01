using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static CustomPostProcessing.UniversalRP.CustomPostProcessInjectionPoint;

namespace CustomPostProcessing.UniversalRP.Editor
{
    using static CustomPostProcessUI;

    [CustomEditor(typeof(CustomPostProcessRendererFeature))]
    public sealed class CustomPostProcessRenderFeatureEditor : UnityEditor.Editor
    {
        private SerializedCustomPostProcessSettings m_SerializedCustomPostProcessSettings { get; set; }

        private SerializedProperty m_ResourceData;

        // this list should be treated as having no injectionPoint
        // This is a container for the Types and TypesAsStrings for CustomPostProcessVolumeComponents in the current scene
        private CustomPostProcessVolumeComponentList currentSceneVolumeComponentList = new(0);

        internal ReorderableList ReadOnlyBeforeTransparents;
        internal ReorderableList ReadOnlyBeforePostProcess;
        internal ReorderableList ReadOnlyAfterPostProcess;
        
        private void OnEnable()
        {
            // serializedObject.FindProperty("CustomBeforeTransparentsPass");
            // serializedObject.FindProperty("CustomBeforePostProcessPass");
            // serializedObject.FindProperty("CustomAfterPostProcessPass");

            m_ResourceData = serializedObject.FindProperty(nameof(CustomPostProcessRendererFeature.postProcessData));

            m_SerializedCustomPostProcessSettings ??= new SerializedCustomPostProcessSettings(new SerializedObject(CustomPostProcessSettings.Instance));

            currentSceneVolumeComponentList.PopulateSceneDebugCustomPPComponentList();
            GetReadOnlyList(ref ReadOnlyBeforeTransparents, new GUIContent(Content.HeaderBT), BeforeTransparents);
            GetReadOnlyList(ref ReadOnlyBeforePostProcess, new GUIContent(Content.HeaderBPP), BeforePostProcess);
            GetReadOnlyList(ref ReadOnlyAfterPostProcess, new GUIContent(Content.HeaderAPP), AfterPostProcess);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var serialized = m_SerializedCustomPostProcessSettings;
            serialized.Update();

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope(Styles.dockHeader))
                {
                    GUILayout.Space(2);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField(Content.RendererFeatureHeaderLabel, EditorStyles.label, GUILayout.ExpandHeight(true));
                    }

                    OpenSettingsButton(Styles.appToolbarButtonLeft);
                    OpenFrameDebuggerButton(Styles.appToolbarButtonRight);

                    GUILayout.Space(2);
                }

                using (new EditorGUILayout.VerticalScope(Styles.DDBackground))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.ObjectField(m_ResourceData, typeof(ScriptableObject), Content.PostProcessData);
                    }
                    
                    ActivePostProcessDrawer.Draw(serialized, this);
                }
            }

            serialized.Apply();

            serializedObject.ApplyModifiedProperties();
        }

        #region Reorderable Lists

        /// <param name="reorderableList">The <see cref="ReorderableList"/> created during OnEnable.</param>
        /// <param name="label">The header of <paramref name="reorderableList"/>.</param>
        /// <param name="injectionPoint">
        /// An injection point for the full screen pass.
        /// This is similar to <see cref="UnityEngine.Rendering.Universal.RenderPassEvent"/> enum but limits to only supported events.
        /// </param>
        private void GetReadOnlyList(ref ReorderableList reorderableList, GUIContent label, CustomPostProcessInjectionPoint injectionPoint)
        {
            reorderableList ??= InitReadOnlyList(label, injectionPoint);
        }

        private ReorderableList InitReadOnlyList(GUIContent label, CustomPostProcessInjectionPoint injectionPoint)
        {
            var scenePostProcessTypesForInjectionPoint = currentSceneVolumeComponentList.FindListTypesInScene(injectionPoint);
            var currentPostProcessNames = scenePostProcessTypesForInjectionPoint.TypesToStringList();

            FieldInfo field = typeof(CustomPostProcessInjectionPoint).GetField(injectionPoint.ToString());
            InspectorNameAttribute inspectorNameAttribute = field.GetCustomAttribute<InspectorNameAttribute>();

            GUIContent header = inspectorNameAttribute != null ? new GUIContent(inspectorNameAttribute.displayName, label.tooltip) : label;

            return new ReorderableList
            (
                currentPostProcessNames,
                typeof(string),
                draggable: false,
                displayHeader: true,
                displayAddButton: false,
                displayRemoveButton: false
            )
            {
                drawElementCallback     = DrawElementCallback,
                drawHeaderCallback      = rect => EditorGUI.LabelField(rect, header, EditorStyles.label),
                elementHeightCallback   = _ => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                drawNoneElementCallback = rect => EditorGUI.LabelField(rect, Content.GUIEmpty)
            };

            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                rect.height = EditorGUIUtility.singleLineHeight;

                var sceneComponentType = scenePostProcessTypesForInjectionPoint[index];
                if (sceneComponentType == null) return;

                var guiContent = GetVolumeComponentGUI(sceneComponentType);

                EditorGUI.LabelField(rect, guiContent, EditorStyles.label);
            }
        }
        
        #endregion
    }
}