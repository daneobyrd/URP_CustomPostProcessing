using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomPostProcessing.UniversalRP
{
    [Serializable]
    public class CustomPostProcessVolumeComponentList : ISerializationCallbackReceiver
    {
        [SerializeField] private CustomPostProcessInjectionPoint m_InjectionPoint;
        public CustomPostProcessInjectionPoint injectionPoint => m_InjectionPoint;

        [SerializeField] private List<string> m_CustomPostProcessTypesAsString;
        private List<Type> m_CustomPostProcessTypes;

        public CustomPostProcessVolumeComponentList(CustomPostProcessInjectionPoint injectionPoint)
        {
            m_CustomPostProcessTypes         = new List<Type>();
            m_CustomPostProcessTypesAsString = new List<string>();
            m_InjectionPoint                 = injectionPoint;
        }

        public IEnumerator<Type> GetEnumerator()
        {
            if (m_CustomPostProcessTypes == null)
            {
                SyncCustomPostProcessTypes();
            }

            return m_CustomPostProcessTypes.GetEnumerator();
        }

        public int Count
        {
            get
            {
                if (m_CustomPostProcessTypes == null)
                {
                    SyncCustomPostProcessTypes();
                }
                return m_CustomPostProcessTypes.Count;
            }
        }

        public Type this[int index]
        {
            get
            {
                if (m_CustomPostProcessTypes == null)
                {
                    SyncCustomPostProcessTypes();
                }
                return m_CustomPostProcessTypes?[index];
            }
            set
            {
                if (m_CustomPostProcessTypes == null)
                {
                    SyncCustomPostProcessTypes();
                }
                m_CustomPostProcessTypes[index] = value;
            }
        }

        private void SyncCustomPostProcessTypes()
        {
            if (m_CustomPostProcessTypes == null)
                m_CustomPostProcessTypes = new List<Type>();
            else
                m_CustomPostProcessTypes.Clear();

            foreach (var typeString in m_CustomPostProcessTypesAsString)
            {
                var customPostProcessComponentType = Type.GetType(typeString);
                if (customPostProcessComponentType == null)
                    throw new ArgumentNullException($"{nameof(typeString)} is not a type");

                var type = Type.GetType(typeString);
                if (typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(type))
                    m_CustomPostProcessTypes.Add(type);
            }
        }

        public void OnAfterDeserialize() { SyncCustomPostProcessTypes(); }

        public void OnBeforeSerialize() { }

        internal void Clear()
        {
            m_CustomPostProcessTypesAsString.Clear();
            SyncCustomPostProcessTypes();
        }
        
        #region Contains
        
        public bool Contains(string typeString)                              => m_CustomPostProcessTypesAsString.Contains(typeString);
        public bool Contains<T>() where T : CustomPostProcessVolumeComponent => m_CustomPostProcessTypesAsString.Contains(typeof(T).AssemblyQualifiedName);
        public bool Contains(Type type)                                      => m_CustomPostProcessTypesAsString.Contains(type.AssemblyQualifiedName);

        #endregion
        
        // Indexing and Find
        public int IndexOf(string typeString)                              => m_CustomPostProcessTypesAsString.IndexOf(typeString);
        public int IndexOf<T>() where T : CustomPostProcessVolumeComponent => m_CustomPostProcessTypesAsString.IndexOf(typeof(T).AssemblyQualifiedName);
        public int IndexOf(Type type)                                      => m_CustomPostProcessTypesAsString.IndexOf(type.AssemblyQualifiedName);

        public int FindIndex(string typeString)                              => m_CustomPostProcessTypesAsString.FindIndex(s => s == typeString);
        public int FindIndex<T>() where T : CustomPostProcessVolumeComponent => m_CustomPostProcessTypesAsString.FindIndex(s => s == typeof(T).AssemblyQualifiedName);
        public int FindIndex(Type type)                                      => m_CustomPostProcessTypesAsString.FindIndex(s => s == type.AssemblyQualifiedName);
        
        public string Find(string typeString)                              => m_CustomPostProcessTypesAsString.Find(s => s == typeString);
        public Type   Find(Type type)                                      => Type.GetType(m_CustomPostProcessTypesAsString.Find(s => s == type.AssemblyQualifiedName));
        public Type   Find<T>() where T : CustomPostProcessVolumeComponent => Type.GetType(m_CustomPostProcessTypesAsString.Find(s => s == typeof(T).AssemblyQualifiedName));
        //

        #region Add
        
        public bool Add(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                throw new ArgumentNullException(nameof(typeString));

            if (!Contains(typeString))
            {
                var type = Type.GetType(typeString);
                if (typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(type))
                {
                    m_CustomPostProcessTypesAsString.Add(typeString);
                    SyncCustomPostProcessTypes();
                    return true;
                }
            }

            return false;
        }

        public bool Add(Type type) => Add(type.AssemblyQualifiedName);

        public bool Add<T>() where T : CustomPostProcessVolumeComponent => Add(typeof(T).AssemblyQualifiedName);

        public bool AddRange(List<string> typesString)
        {
            if (typesString == null)
                throw new ArgumentNullException(nameof(typesString));

            bool changed = false;
            foreach (var typeString in typesString)
            {
                if (!Contains(typeString))
                {
                    var type = Type.GetType(typeString);
                    if (typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(type))
                    {
                        m_CustomPostProcessTypesAsString.Add(typeString);
                        changed = true;
                    }
                }
            }

            if (changed)
                SyncCustomPostProcessTypes();

            return changed;
        }
        
        public bool AddRange(IEnumerable<CustomPostProcessVolumeComponent> volumeComponents)
        {
            if (volumeComponents == null)
                throw new ArgumentNullException(nameof(volumeComponents));

            return AddRange(volumeComponents.ObjectTypesToStringList());
        }
        
        #endregion

        #region Remove
        
        public bool Remove(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                throw new ArgumentNullException(nameof(typeString));

            if (!Contains(typeString))
                return false;

            if (m_CustomPostProcessTypesAsString.Remove(typeString))
            {
                SyncCustomPostProcessTypes();
                return true;
            }

            return false;
        }

        public bool Remove<T>() where T : CustomPostProcessVolumeComponent => Remove(typeof(T).AssemblyQualifiedName);

        public int RemoveAll(Predicate<string> match)
        {
            var removeAll = m_CustomPostProcessTypesAsString.RemoveAll(match);
            if (removeAll > 0) SyncCustomPostProcessTypes();

            return removeAll;
        }

        public bool RemoveRange(List<string> typesString)
        {
            if (typesString == null)
                throw new ArgumentNullException(nameof(typesString));

            bool changed = false;
            foreach (var typeString in typesString)
            {
                if (Contains(typeString))
                {
                    var type = Type.GetType(typeString);
                    if (typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(type))
                    {
                        m_CustomPostProcessTypesAsString.Remove(typeString);
                        changed = true;
                    }
                }
            }

            if (changed)
                SyncCustomPostProcessTypes();

            return changed;
        }

        public void RemoveAt(int index)
        {
            if ((uint) index >= (uint) Count)
                throw new ArgumentOutOfRangeException();

            bool changed = false;
            if (index < Count)
            {
                m_CustomPostProcessTypesAsString.RemoveAt(index);
                changed = true;
            }

            if (changed)
                SyncCustomPostProcessTypes();
        }

        #endregion
    }
}