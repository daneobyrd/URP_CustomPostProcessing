using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using CustomPostProcessing.UniversalRP;

namespace CustomPostProcessing.UniversalRP.Editor
{
    internal static partial class CustomPostProcessUI
    {
        /// <see href="https://github.com/DefaultLP/UnityEditorStyles/tree/main"/>
        internal static class Styles
        {
            // public static GUIStyle RLHeader     => new("RL Header");
            // public static GUIStyle RLBackground => new("RL Background");

            // public static GUIStyle shurikenEffectBG => new("ShurikenEffectBg"); // {padding = new RectOffset(1, 8, 1, 1)};

            // public static GUIStyle tabWindowBackground => new("TabWindowBackground");
            // public static GUIStyle tabOnlyOne          => new("Tab onlyOne");
            public static GUIStyle dockHeader => new("dockHeader");
            // public static GUIStyle dockAreaStandalone  => new("dockAreaStandalone");


            // public static GUIStyle textFieldDropdownBG => new("TextFieldDropDownText") {fixedHeight = 0};
            public static GUIStyle DDBackground => new("DD Background");

            // public static GUIStyle miniButton      => EditorStyles.miniButton;
            // public static GUIStyle miniButtonLeft  => EditorStyles.miniButtonLeft;
            // public static GUIStyle miniButtonRight => EditorStyles.miniButtonRight;

            // public static GUIStyle toolbarButtonLeft  => new("toolbarButtonLeft");
            // public static GUIStyle toolbarButtonRight => new("toolbarButtonRight");

            public static GUIStyle appToolbar             => new("AppToolbar");
            public static GUIStyle appToolbarButtonLeft   => new("AppToolbarButtonLeft");
            public static GUIStyle appToolbarButtonMiddle => new("AppToolbarButtonMid");
            public static GUIStyle appToolbarButtonRight  => new("AppToolbarButtonRight");

            // public static GUIStyle profilerBadge           => new("ProfilerBadge") {fixedHeight = 0};
            // public static GUIStyle profilerGraphBackground => new("ProfilerGraphBackground");
        }

        internal static class EditorConstants
        {
            public static float defaultLineSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            // public static float reorderableListHandleIndentWidth = 12;
            // public static GUIContent enabled = new GUIContent("Enabled", "Enable or Disable the custom pass");
        }

        internal static class Content
        {
            public static readonly string HeaderBT = "Before Transparents";
            public static readonly string HeaderBPP = "Before Post Process";
            public static readonly string HeaderAPP = "After Post Process";
            
            public static readonly GUIContent customPostProcessOrderLabel = EditorGUIUtility.TrTextContent("Custom Post Process Orders");

            public static Tuple<string, Texture2D> EffectIsActive  => new("Enabled & active", CoreEditorStyles.iconComplete);
            public static Tuple<string, Texture2D> EffectNotActive => new("Enabled & not active", CoreEditorStyles.iconWarn);
            public static Tuple<string, Texture2D> EffectDisabled  => new("Disabled", CoreEditorStyles.iconPending);

            public static Tuple<string, Texture2D> EffectNeedsAttention =>
                new
                (
                    "VolumeComponent is missing from Custom Post-Process Orders."
                    + Environment.NewLine
                    + "Fix in Custom Post-Process Settings.", CoreEditorStyles.iconFail
                );

            public static readonly GUIContent GUIEmpty = EditorGUIUtility.TrTextContent("List is empty.", "Edit Custom Post-Process Orders and add volume components to the scene.");

            public static GUIContent RendererFeatureHeaderLabel => EditorGUIUtility.TrTextContent("Custom Post Process Volume Components", "Found in current scene");
            public static GUIContent PostProcessData => EditorGUIUtility.TrTextContent("Data", "The asset containing references to shaders that the Renderer Feature uses for custom post-processing");
            public static GUIContent SettingsButtonLabel => EditorGUIUtility.TrTextContent("Settings", "Edit Custom Post-Process Orders");

            public static readonly GUILayoutOption[] settingsButtonOptions = {GUILayout.Width(60), GUILayout.Height(20)};

            public static GUIContent FrameDebuggerButton => EditorGUIUtility.TrIconContent(EditorGUIUtility.FindTexture("Debug_Frame_d@2x"), tooltip: "Open the Frame Debugger");
            public static readonly GUILayoutOption[] frameDebuggerButtionOptions = {GUILayout.Width(27), GUILayout.Height(20)};
        }

        public static GUIContent GetVolumeComponentGUI(Type componentType)
        {
            var component = CustomPostProcessCore.GetCustomPostProcessComponent(componentType);

            var name = component.displayName;

            bool valid = CustomPostProcessSettings.Instance.IsCustomPostProcessRegistered(componentType);
            bool enabled = component.active;
            bool active = component.IsActive();

            var statusTuple = (valid, enabled, active) switch
            {
                (true, true, true)  => Content.EffectIsActive,
                (true, true, false) => Content.EffectNotActive,
                (true, false, _)    => Content.EffectDisabled,
                (false, _, _)       => Content.EffectNeedsAttention,
            };

            return EditorGUIUtility.TrTextContentWithIcon(text: name, tooltip: statusTuple.Item1, icon: statusTuple.Item2);
        }
    }
}