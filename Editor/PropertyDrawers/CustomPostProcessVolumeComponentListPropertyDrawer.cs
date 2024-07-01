using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace CustomPostProcessing.UniversalRP.Editor
{
    using static CustomPostProcessSerializedPropertyUtils;

    [CustomPropertyDrawer(typeof(CustomPostProcessVolumeComponentList))]
    internal class CustomPostProcessVolumeComponentListPropertyDrawer : PropertyDrawer
    {
        List<Type> FetchAvailableCustomPostProcessVolumesTypes(CustomPostProcessInjectionPoint injectionPoint, SerializedProperty list)
        {
            var listTypes = new List<Type>();

            using (HashSetPool<string>.Get(out var tmp))
            {
                for (int i = 0; i < list.arraySize; i++)
                {
                    // tmp.Add(list.GetArrayElementAtIndex(i).stringValue);
                    TryAddCustomPostProcessTypeName(ref tmp, list.GetArrayElementAtIndex(i));
                }

                foreach (var type in TypeCache.GetTypesDerivedFrom<CustomPostProcessVolumeComponent>())
                {
                    if (type.IsAbstract || tmp.Contains(type.AssemblyQualifiedName))
                        continue;

                    if (type.GetCustomAttribute<HideInInspector>() != null)
                        continue;

                    var comp = ScriptableObject.CreateInstance(type) as CustomPostProcessVolumeComponent;

                    if (comp != null && comp.injectionPoint == injectionPoint)
                        listTypes.Add(type);

                    CoreUtils.Destroy(comp);
                }
            }

            return listTypes;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property) { return new Label(property.propertyPath); }

        private static Dictionary<string, ReorderableList> s_ReorderableList = new();

        ReorderableList GetList(SerializedProperty property, GUIContent label)
        {
            if (s_ReorderableList.TryGetValue(property.propertyPath, out var reorderableList))
            {
                if (reorderableList != null && reorderableList.serializedProperty != null && !reorderableList.serializedProperty.Equals(null))
                {
                    return reorderableList;
                }
            }

            var currentPostProcessTypes = property.FindPropertyRelative("m_CustomPostProcessTypesAsString");

            var injectionPoint = property.FindPropertyRelative("m_InjectionPoint")
                                         .GetEnumValue<CustomPostProcessInjectionPoint>();

            FieldInfo field = typeof(CustomPostProcessInjectionPoint).GetField(injectionPoint.ToString());
            InspectorNameAttribute inspectorNameAttribute = field.GetCustomAttribute<InspectorNameAttribute>();

            GUIContent header = inspectorNameAttribute != null ? new GUIContent(inspectorNameAttribute.displayName, label.tooltip) : label;


            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                var stringType = currentPostProcessTypes.GetArrayElementAtIndex(index).stringValue;
                var elemType = Type.GetType(stringType);

                // var fullName = stringType[..stringType.IndexOf(",", System.StringComparison.Ordinal)];
                var fullName = elemType?.FullName;

                EditorGUI.LabelField
                (
                    rect,
                    EditorGUIUtility.TrTextContent
                    (
                        text: elemType == null ? $"Invalid type {stringType}" : elemType.Name + $" ({elemType.Namespace})",
                        tooltip: fullName
                    ),
                    EditorStyles.label
                );
            }

            reorderableList = new ReorderableList
            (
                property.serializedObject,
                currentPostProcessTypes,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true
            )
            {
                drawElementCallback = DrawElementCallback,
                drawHeaderCallback  = rect => EditorGUI.LabelField(rect, header, EditorStyles.boldLabel),
                onAddCallback = _ =>
                {
                    var menu = new GenericMenu();

                    var listTypes = FetchAvailableCustomPostProcessVolumesTypes(injectionPoint, currentPostProcessTypes);
                    foreach (var type in listTypes)
                    {
                        menu.AddItem(new GUIContent(type.Name + $" ({type.Namespace})", tooltip: type.AssemblyQualifiedName), false, TryAddVolumeType(type));
                    }

                    GenericMenu.MenuFunction TryAddVolumeType(Type type)
                    {
                        return () =>
                        {
                            int lastPos = currentPostProcessTypes.arraySize;
                            currentPostProcessTypes.InsertArrayElementAtIndex(lastPos);
                            var newProperty = currentPostProcessTypes.GetArrayElementAtIndex(lastPos);
                            newProperty.stringValue = type.AssemblyQualifiedName;
                            property.serializedObject.ApplyModifiedProperties();
                        };
                    }

                    if (menu.GetItemCount() == 0)
                        menu.AddDisabledItem(new GUIContent("No Custom Post Process Available"));

                    menu.ShowAsContext();
                },
                onRemoveCallback = list =>
                {
                    currentPostProcessTypes.DeleteArrayElementAtIndex(list.index);
                    property.serializedObject.ApplyModifiedProperties();
                },
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                onReorderCallback = (list) =>
                {
                    property.serializedObject.ApplyModifiedProperties();
                }
            };


            s_ReorderableList[property.propertyPath] = reorderableList;

            return reorderableList;
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();
            try
            {
                GetList(property, label)?.DoList(position);
            }
            catch (NullReferenceException)
            {
                s_ReorderableList[property.propertyPath] = null;
            }
            catch (ArgumentNullException)
            {
                s_ReorderableList[property.propertyPath] = null;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            try
            {
                return GetList(property, label)?.GetHeight() ?? 0; // List height + Spacing before and after
            }
            catch (NullReferenceException)
            {
                s_ReorderableList[property.propertyPath] = null;
                return 0;
            }
            catch (ArgumentNullException)
            {
                s_ReorderableList[property.propertyPath] = null;
                return 0;
            }
        }
    }
}