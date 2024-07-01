using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CustomPostProcessing.UniversalRP.Editor
{
    public static class CustomPostProcessSerializedPropertyUtils
    {
        #region Private Methods for Adding stringValue from Type from SerializedProperty

        internal static void TryAddCustomPostProcessTypeName(ref List<string> list, SerializedProperty element)
        {
            if (TryAddStringValue(ref list, element))
            {
                // TryAdd string from CustomPostProcessVolumeComponentList.m_CustomPostProcessTypesAsString
                return;
            }

            if (TryAddObjectValueTypeAsString<CustomPostProcessVolumeComponent>(ref list, element))
            {
                // TryAdd CustomPostProcessVolumeComponent.GetType().AssemblyQualifiedName
                // from CustomPostProcessPass._customPostProcessVolumeComponents
                return;
            }
        }

        internal static void TryAddCustomPostProcessTypeName(ref HashSet<string> hashSet, SerializedProperty element)
        {
            if (TryAddStringValue(ref hashSet, element))
            {
                // TryAdd m_CustomPostProcessTypesAsString entry
                return;
            }

            if (TryAddObjectValueTypeAsString<CustomPostProcessVolumeComponent>(ref hashSet, element))
            {
                // CustomPostProcessVolumeComponent.GetType().AssemblyQualifiedName
                // from CustomPostProcessPass._customPostProcessVolumeComponents
                return;
            }
        }

        private static bool TryAddStringValue(ref List<string> list, SerializedProperty element)
        {
            var listCount = list.Count;

            if (element.propertyType == SerializedPropertyType.String)
            {
                if (!list.Contains(element.stringValue))
                {
                    list.Add(element.stringValue);
                }
            }

            return listCount < list.Count;
        }

        private static bool TryAddStringValue(ref HashSet<string> hashSet, SerializedProperty element)
        {
            var listCount = hashSet.Count;
            if (element.propertyType == SerializedPropertyType.String)
            {
                hashSet.Add(element.stringValue);
            }

            return listCount < hashSet.Count;
        }

        private static bool TryAddObjectValueTypeAsString<T>(ref List<string> list, SerializedProperty element)
        {
            var listCount = list.Count;
            // CustomPostProcessVolumeComponent in CustomPostProcessPass._customPostProcessVolumeComponents
            if (element.propertyType == SerializedPropertyType.ObjectReference
                && element.objectReferenceValue is T component)
            {
                list.Add(component.GetType().AssemblyQualifiedName);
            }

            return listCount < list.Count;
        }

        private static bool TryAddObjectValueTypeAsString<T>(ref HashSet<string> hashSet, SerializedProperty element)
        {
            var listCount = hashSet.Count;
            // CustomPostProcessVolumeComponent in CustomPostProcessPass._customPostProcessVolumeComponents
            if (element.propertyType == SerializedPropertyType.ObjectReference
                && element.objectReferenceValue is T component)
            {
                hashSet.Add(component.GetType().AssemblyQualifiedName);
            }

            return listCount < hashSet.Count;
        }

        #endregion

        #region Private Methods for Adding Type from stringValue from SerializedProperty

        internal static void TryAddCustomPostProcessType(ref List<Type> list, SerializedProperty element)
        {
            if (TryAddTypeFromStringValue(ref list, element))
            {
                // TryAdd string from CustomPostProcessVolumeComponentList.m_CustomPostProcessTypesAsString
                return;
            }

            if (TryAddObjectValueType<CustomPostProcessVolumeComponent>(ref list, element))
            {
                // TryAdd CustomPostProcessVolumeComponent.GetType().AssemblyQualifiedName
                // from CustomPostProcessPass._customPostProcessVolumeComponents
                return;
            }
        }

        internal static void TryAddCustomPostProcessType(ref HashSet<Type> hashSet, SerializedProperty element)
        {
            if (TryAddTypeFromStringValue(ref hashSet, element))
            {
                // TryAdd m_CustomPostProcessTypesAsString entry
                return;
            }

            if (TryAddObjectValueType<CustomPostProcessVolumeComponent>(ref hashSet, element))
            {
                // CustomPostProcessVolumeComponent.GetType().AssemblyQualifiedName
                // from CustomPostProcessPass._customPostProcessVolumeComponents
                return;
            }
        }

        private static bool TryAddTypeFromStringValue(ref List<Type> list, SerializedProperty element)
        {
            var listCount = list.Count;

            var type = Type.GetType(element.stringValue);
            if (!list.Contains(type))
            {
                list.Add(type);
            }

            return listCount < list.Count;
        }

        private static bool TryAddTypeFromStringValue(ref HashSet<Type> hashSet, SerializedProperty element)
        {
            var listCount = hashSet.Count;

            var type = Type.GetType(element.stringValue);
            hashSet.Add(type);

            return listCount < hashSet.Count;
        }

        private static bool TryAddObjectValueType<T>(ref List<Type> list, SerializedProperty element)
        {
            var listCount = list.Count;
            // CustomPostProcessVolumeComponent in CustomPostProcessPass._customPostProcessVolumeComponents
            if (element.propertyType == SerializedPropertyType.ObjectReference
                && element.objectReferenceValue is T component)
            {
                list.Add(component.GetType());
            }

            return listCount < list.Count;
        }

        private static bool TryAddObjectValueType<T>(ref HashSet<Type> hashSet, SerializedProperty element)
        {
            var listCount = hashSet.Count;
            // CustomPostProcessVolumeComponent in CustomPostProcessPass._customPostProcessVolumeComponents
            if (element.propertyType == SerializedPropertyType.ObjectReference
                && element.objectReferenceValue is T component)
            {
                hashSet.Add(component.GetType());
            }

            return listCount < hashSet.Count;
        }

        #endregion

        /// <param name="list">Expected to be m_CustomPostProcessTypesAsString or CustomPostProcessPass._customPostProcessVolumeComponents.</param>
        /// <returns></returns>
        public static List<string> ToStringList(this SerializedProperty list)
        {
            if (!list.isArray) return new List<string>();

            var newList = new List<string>();

            // Store serialized list assembly qualified type names in tmp
            for (int i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);

                TryAddCustomPostProcessTypeName(ref newList, element);
            }

            return newList;
        }

        public static List<Type> ToTypeList(this SerializedProperty list)
        {
            if (!list.isArray) return new List<Type>();

            var newList = new List<Type>();

            // Store serialized list assembly qualified type names in tmp
            for (int i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);

                TryAddCustomPostProcessType(ref newList, element);
            }

            return newList;
        }

        internal static List<Type> FindListTypesInScene(this CustomPostProcessVolumeComponentList customVolumeComponentList,
                                                      CustomPostProcessInjectionPoint injectionPoint = 0)
        {
            var listTypes = new List<Type>();

            var checkInjectionPoint = injectionPoint != 0;
            
            if (!CustomPostProcessVolumeQuery.TryGetVolumeProfilesInScene(out var volumeProfiles))
            {
                return listTypes;
            }

            foreach (var profile in volumeProfiles)
            {
                var profileComponents = new List<CustomPostProcessVolumeComponent>();

                if (!profile.TryGetAllSubclassOf(typeof(CustomPostProcessVolumeComponent), profileComponents))
                    continue;

                foreach (var component in profileComponents)
                {
                    if (checkInjectionPoint)
                    {
                        if (component.injectionPoint != injectionPoint)
                            continue;
                    }

                    var type = component.GetType();

                    if (listTypes.Contains(type))
                        continue;

                    if (!customVolumeComponentList.Contains(type))
                        continue;


                    listTypes.Add(type);
                }
            }

            return listTypes;
        }

        internal static List<Type> FindListTypesInScene(SerializedProperty list, CustomPostProcessInjectionPoint injectionPoint = 0, Camera camera = null)
        {
            var listTypes = new List<Type>();

            var checkInjectionPoint = injectionPoint != 0;

            camera ??= Camera.main;

            if (!CustomPostProcessVolumeQuery.TryGetVolumeProfilesInScene(out var volumeProfiles))
            {
                return listTypes;
            }

            using (UnityEngine.Rendering.HashSetPool<string>.Get(out var tmp))
            {
                if (!list.isArray) return listTypes;

                // Store serialized list assembly qualified type names in tmp
                for (int i = 0; i < list.arraySize; i++)
                {
                    TryAddCustomPostProcessTypeName(ref tmp, list.GetArrayElementAtIndex(i));
                }

                foreach (var profile in volumeProfiles)
                {
                    // get all CustomPostProcessVolumeComponent in each VolumeProfile

                    var profileComponents = new List<CustomPostProcessVolumeComponent>();
                    if (!profile.TryGetAllSubclassOf(typeof(CustomPostProcessVolumeComponent), profileComponents))
                        continue;

                    foreach (var component in profileComponents)
                    {
                        if (checkInjectionPoint)
                        {
                            if (component.injectionPoint != injectionPoint)
                                continue;
                        }

                        var type = component.GetType();

                        if (listTypes.Contains(type))
                            continue;

                        if (!tmp.Contains(type.AssemblyQualifiedName))
                            continue;


                        listTypes.Add(type);
                    }
                }
            }

            return listTypes;
        }
    }
}