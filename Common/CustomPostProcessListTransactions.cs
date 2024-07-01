using System;
using System.Collections.Generic;

namespace CustomPostProcessing.UniversalRP
{
    public static class CustomPostProcessListTransactions
    {
        public static List<Type> ObjectsToTypeList<T>(this IEnumerable<T> objects, bool allowDuplicateTypes = true) where T : class
        {
            var typeList = new List<Type>();

            foreach (var obj in objects)
            {
                var type = obj.GetType();

                if (!allowDuplicateTypes && typeList.Contains(type))
                {
                    continue;
                }

                typeList.Add(type);
            }

            return typeList;
        }

        public static List<Type> StringsToTypeList(this List<string> nameList, bool allowDuplicateTypes = true)
        {
            var typeList = new List<Type>();

            foreach (var name in nameList)
            {
                var type = Type.GetType(name);

                if (!allowDuplicateTypes && typeList.Contains(type))
                    continue;

                typeList.Add(type);
            }

            return typeList;
        }

        public static List<string> TypesToStringList(this List<Type> typeList, bool removeAbstractTypes = true)
        {
            // Sanitize the lists
            typeList.RemoveAll(type => type is null || (removeAbstractTypes && type.IsAbstract));

            var stringList = new List<string>();

            foreach (var type in typeList)
            {
                stringList.Add(type.AssemblyQualifiedName);
            }

            return stringList;
        }

        public static List<string> ObjectTypesToStringList<T>(this IEnumerable<T> objects, bool allowDuplicateTypes = true) where T : class
        {
            var stringList = new List<string>();

            foreach (var obj in objects)
            {
                var type = obj.GetType();

                if (!allowDuplicateTypes && stringList.Contains(type.AssemblyQualifiedName))
                {
                    continue;
                }

                stringList.Add(type.AssemblyQualifiedName);
            }

            return stringList;
        }

        /*
        public static List<string> GetCustomVolumeComponentsNameList(this List<CustomPostProcessVolumeComponent> ppVolumeComponents, bool outputDisplayNames = false)
        {
            return outputDisplayNames
                ? ppVolumeComponents.Select(c => c.displayName).ToList()
                : ppVolumeComponents.ObjectsToTypeList().TypesToStringList();
        }
        */
    }
}