using System;
using UnityEditor;

namespace CustomPostProcessing.UniversalRP.Editor
{
    public class SerializedCustomPostProcessSettings
    {
        public SerializedObject   serializedObject                  { get; }
        public SerializedProperty serializedCustomPostProcessOrders { get; }

        public SerializedCustomPostProcessSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            if (serializedObject.targetObject is CustomPostProcessSettings)
            {
                serializedCustomPostProcessOrders = this.serializedObject.FindProperty("m_CustomPostProcessOrders");
            }
            else
            {
                throw new Exception($"Target object has an invalid object, objects must be of type {typeof(CustomPostProcessSettings)}");
            }
        }

        public void Update() { serializedObject.Update(); }
        public void Apply()  { serializedObject.ApplyModifiedProperties(); }
    }
}