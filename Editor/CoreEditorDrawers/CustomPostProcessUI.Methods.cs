using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace CustomPostProcessing.UniversalRP.Editor
{
    internal static partial class CustomPostProcessUI
    {
        /// <summary>Draw a header section like in Global Settings</summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="documentationURL">Documentation URL</param>
        /// <param name="contextAction">The context action</param>
        /// <param name="hasMoreOptions">Delegate saying if we have MoreOptions</param>
        /// <param name="toggleMoreOptions">Callback called when the MoreOptions is toggled</param>
        public static void DrawSectionHeader(GUIContent title, string documentationURL = null, Action<Vector2> contextAction = null,
                                             Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
        {
            var backgroundRect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(1f, 20f));
            float xMin = backgroundRect.xMin;

            float iconSize = 16f;

            var contextMenuRect = new Rect(backgroundRect.xMax - (iconSize + 5), backgroundRect.y + iconSize + 8f, iconSize, iconSize);
            // DrawBackground(backgroundRect);
            // DrawBackground(contextMenuRect);

            var labelRect = backgroundRect;

            using (new EditorGUILayout.HorizontalScope())
            {
                var style = CoreEditorStyles.sectionHeaderStyle;
                style.fixedHeight = style.CalcHeight(title, backgroundRect.width);
                style.fontSize    = 14;
                EditorGUI.LabelField(backgroundRect, title, style);

                // Context menu
                var contextMenuIcon = CoreEditorStyles.contextMenuIcon.image;
                if (contextAction != null)
                {
                    if (GUI.Button(contextMenuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                        contextAction(new Vector2(contextMenuRect.x, contextMenuRect.yMax));
                }

                ShowHelpButton(contextMenuRect, documentationURL, title);
            }

            // Handle events
            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (contextMenuRect.Contains(e.mousePosition))
                {
                    // Right click: Context menu
                    contextAction?.Invoke(new Vector2(contextMenuRect.x, contextMenuRect.yMax));
                    e.Use();
                }
            }
        }

        private static void ShowHelpButton(Rect contextMenuRect, string documentationURL, GUIContent title)
        {
            if (string.IsNullOrEmpty(documentationURL))
                return;

            var documentationRect = contextMenuRect;
            documentationRect.x -= 16 + 2;

            var documentationIcon = new GUIContent(CoreEditorStyles.iconHelp, $"Open Reference for {title.text}.");

            if (GUI.Button(documentationRect, documentationIcon, CoreEditorStyles.iconHelpStyle))
                Help.BrowseURL(documentationURL);
        }

        public static void OpenSettingsButton(GUIStyle guiStyle = null)
        {
            guiStyle ??= EditorStyles.toolbarButton;

            if (GUILayout.Button((GUIContent) Content.SettingsButtonLabel, guiStyle, Content.settingsButtonOptions))
            {
                SettingsService.OpenProjectSettings(CustomPostProcessSettings.Attribute.DisplayPath);
            }
        }

        private static void OpenWindowAndToggleEnabled()
        {
            const string className = "UnityEditor.FrameDebuggerWindow, UnityEditor.CoreModule";

            var frameDebuggerWindowType = Type.GetType(className);
            if (frameDebuggerWindowType == null)
            {
                // Debug.LogError("FrameDebuggerWindow type not found.");
                return;
            }

            var openWindowAndToggleEnabledMethod = frameDebuggerWindowType.GetMethod(nameof(OpenWindowAndToggleEnabled), BindingFlags.Static | BindingFlags.Public);

            if (openWindowAndToggleEnabledMethod == null)
            {
                // Debug.LogError("OpenWindowAndToggleEnabled method not found.");
                return;
            }

            openWindowAndToggleEnabledMethod.Invoke(null, null);
        }

        public static void OpenFrameDebuggerButton(GUIStyle guiStyle = null)
        {
            guiStyle ??= EditorStyles.toolbarButton;

            if (GUILayout.Button((GUIContent) Content.FrameDebuggerButton, style: guiStyle, Content.frameDebuggerButtionOptions))
            {
                OpenWindowAndToggleEnabled();
            }
        }
        
        internal static void GlobalSettingsHelpBox(string message, MessageType type, string propertyPath)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                SettingsService.OpenProjectSettings(CustomPostProcessSettings.Attribute.DisplayPath);
                CoreEditorUtils.Highlight("Project Settings", propertyPath);
                GUIUtility.ExitGUI();
            });
        }
    }
}