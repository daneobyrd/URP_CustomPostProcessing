using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace CustomPostProcessing.UniversalRP.Editor
{
    /// <summary>
    /// Base class to inherit to create custom post process volume editors.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CustomPostProcessVolumeComponent), true)]
    public class CustomPostProcessVolumeComponentEditor : VolumeComponentEditor
    {
        internal static class Styles
        {
            public static readonly string customPostProcessNotInGlobalSettingsText = "This Custom Postprocess is not registered in the Global Settings.";
        }

        /// <summary>
        /// Unity calls this method each time it re-draws the Inspector.
        /// </summary>
        /// <remarks>
        /// You can safely override this method and not call <c>base.OnInspectorGUI()</c> unless you
        /// want Unity to display all the properties from the <see cref="VolumeComponent"/>
        /// automatically.
        /// </remarks>
        public override void OnInspectorGUI()
        {
            if (CustomPostProcessSettings.Instance.IsCustomPostProcessRegistered(target.GetType()))
            {
                return;
            }
            
            CustomPostProcessUI.GlobalSettingsHelpBox(Styles.customPostProcessNotInGlobalSettingsText, MessageType.Error,
                                                      CustomPostProcessUI.Content.customPostProcessOrderLabel.text);
        }
    }
}