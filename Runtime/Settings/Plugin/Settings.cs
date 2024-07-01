// Copyright 2021 by Hextant Studios. https://HextantStudios.com
// This work is licensed under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/

// This script has been modified from it's original version.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// Base class for project/users settings. Use the [Settings] attribute to
// specify its usage, display path, and filename.
// * Derived classes *must* be placed in a file with the same name as the class.
// * Settings are stored in Assets/Settings/ folder.
// * The user settings folder Assets/Settings/Editor/User/ *must* be
//   excluded from source control.
// * User settings will be placed in a subdirectory named the same as
//   the current project folder so that shallow cloning (symbolic links to
//   the Assets/ folder) can be used when testing multiplayer games.
// See: https://HextantStudios.com/unity-custom-settings/
public abstract class Settings<T> : ScriptableObject where T : Settings<T>
{
    // The singleton instance. (Not thread safe but fine for ScriptableObjects.)
    public static T Instance => _instance != null ? _instance : Initialize();

    private static T _instance;

    // The derived type's [Settings] attribute.
    public static SettingsAttribute Attribute => _attribute ??= typeof(T).GetCustomAttribute<SettingsAttribute>(true);

    private static SettingsAttribute _attribute;

    // Loads or creates the settings instance and stores it in _instance.
    private static T Initialize()
    {
        // If the instance is already valid, return it. Needed if called from a 
        // derived class that wishes to ensure the settings are initialized.
        if (_instance != null) return _instance;

        // Verify there was a [Settings] attribute.
        if (Attribute == null)
        {
            throw new InvalidOperationException
            (
                "[Settings] attribute missing for type: " + typeof(T).Name
            );
        }

        // Attempt to load the settings asset.
        var fileName = Attribute.FileName ?? typeof(T).Name;
        var filePath = GetSettingsPath() + $"{fileName}.asset";

        _instance = LoadAsset(fileName, filePath);

#if UNITY_EDITOR
        if (_instance == null)
        {
            _instance = TryLocateAsset();
        }

        var oldPath = AssetDatabase.GetAssetPath(_instance);

        _instance = TryMove(_instance, oldPath, filePath);
        if (_instance != null) { return _instance; }

        _instance = TryClone(_instance, oldPath, filePath);
        if (_instance != null) { return _instance; }

        // Create the settings instance if it was not loaded or found.
        if (_instance == null)
        {
            CreateNewAsset(filePath);
        }
#endif

        return _instance;
    }

    static T LoadAsset(string filename, string path)
    {
        if (Attribute.Usage == SettingsUsage.RuntimeProject)
            return Resources.Load<T>(filename);
        else
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }
    }

#if UNITY_EDITOR
    static T TryLocateAsset()
    {
        bool AssetPathExists(T i) => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(i)) == false;

        var instancesFound = Resources.FindObjectsOfTypeAll<T>().Where(AssetPathExists).ToList();
        if (instancesFound.Count > 0)
        {
            return instancesFound[0];
        }

        return null;
    }

    /// <summary>
    /// Move settings if its path changed (type renamed or attribute changed)
    /// while the editor was running. This must be done manually if the
    /// change was made outside the editor.
    /// </summary>
    /// <returns>
    /// Null if:<br/>
    /// • Asset needs to be moved, but cannot be moved.<br/>
    /// • AssetDatabase.MoveAsset() fails.<br/>
    /// sourceAsset if:<br/>
    /// • Asset does not need to be moved.<br/>
    /// • AssetDatabase.MoveAsset() succeeds.<br/>
    /// </returns>
    static T TryMove(T sourceAsset, string oldPath, string newPath)
    {
        // If Asset does not need to be moved, return sourceAsset.
        var moveAssetNotNeeded = oldPath.StartsWith("Assets/Settings/", StringComparison.CurrentCultureIgnoreCase);
        if (moveAssetNotNeeded)
        {
            return sourceAsset;
        }

        if (!IsAssetMovePossible())
        {
            return null;
        }

        // If Asset can be moved,
        //      and move is successful, return sourceAsset.
        //      and moved fails, return null.
        var moveResult = AssetDatabase.MoveAsset(oldPath, newPath);
        var moveSuccess = string.IsNullOrEmpty(moveResult);
        if (moveSuccess)
        {
            Debug.Log
            (
                $"Moved {sourceAsset}" + Environment.NewLine +
                $"From: {oldPath}" + Environment.NewLine +
                $"To: {newPath}."
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sourceAsset;
        }

        Debug.LogWarning
        (
            $"Failed to move previous settings asset " +
            $"'{oldPath}' to '{newPath}'. " +
            $"A new settings asset will be created.", _instance
        );

        return null;

        bool IsAssetMovePossible()
        {
            if (IsAssetInPackage(oldPath, out var info))
            {
                if (IsAssetInReadOnlyPackage(info))
                {
                    return false;
                }

                return true;
            }

            if (!oldPath.StartsWith("Assets/", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            var validateMoveAsset = AssetDatabase.ValidateMoveAsset(oldPath, newPath);
            var movePossible = string.IsNullOrEmpty(validateMoveAsset);
            if (!movePossible)
            {
                Debug.LogError($"Couldn't move {oldPath} because {validateMoveAsset}");
                return false;
            }

            return true;
        }

        // From CoreEditorUtils.cs
        bool IsAssetInPackage(string path, out PackageInfo info)
        {
            Assert.IsNotNull(path);
            info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
            return info != null;
        }

        bool IsAssetInReadOnlyPackage(in PackageInfo info) { return info != null && (info.source != PackageSource.Local && info.source != PackageSource.Embedded); }
    }

    // Method assumes it will only be called when it has been determined that sourceAsset should be cloned.
    static T TryClone(T sourceAsset, string oldPath, string newPath)
    {
        // TryClone
        try
        {
            var clone = Instantiate(sourceAsset);
            clone.Clear();

            AssetDatabase.CreateAsset(clone, newPath);
            clone.CopyDataFrom(sourceAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return clone;
        }
        catch (Exception exception)
        {
            // Expected: UnityException or ArgumentNullException
            Debug.LogWarning
            (
                "Failed to copy previous settings asset " +
                $"'{oldPath}' to '{newPath}'. " +
                "A new settings asset will be created." +
                $"{exception}", _instance
            );
            return null;
        }
    }


    /// <summary>
    /// Virtual function to handle resetting settings before copying data.
    /// </summary>
    protected virtual void Clear() { }

    /// <summary>
    /// Virtual function to allow user's to manually handle copying settings.
    /// </summary>
    /// <param name="origin">Settings instance found by <see cref="TryLocateAsset"/></param>
    /// <returns>Copy of origin settings to be used in <see cref="CopyDataFrom"/> for AssetDatabase.CreateAsset()</returns>
    /// <remarks>All overriding methods must call Instantiate</remarks>
    protected virtual void CopyDataFrom(T origin) { }


#endif

    static void CreateNewAsset(string path)
    {
        Debug.LogWarning
        (
            "Failed to find an existing settings asset " +
            "A new settings asset will be created.", _instance
        );

        _instance = CreateInstance<T>();

#if UNITY_EDITOR
        // Verify the derived class is in a file with the same name.
        var script = MonoScript.FromScriptableObject(_instance);
        if (script == null || script.name != typeof(T).Name)
        {
            DestroyImmediate(_instance);
            _instance = null;
            throw new System.InvalidOperationException
            (
                "Settings-derived class and filename must match: " +
                typeof(T).Name
            );
        }

        // Create a new settings instance if it was not found.
        // Create the directory as Unity does not do this itself.
        Directory.CreateDirectory
        (
            Path.Combine
            (
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(path)! // path is never null
            )
        );

        // Create the asset only in the editor.
        AssetDatabase.CreateAsset(_instance, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }

    // Returns the full asset path to the settings file.
    static string GetSettingsPath()
    {
        var path = "Assets/Settings/";

        switch (Attribute.Usage)
        {
            case SettingsUsage.RuntimeProject:
                path += "Resources/";
                break;
#if UNITY_EDITOR
            case SettingsUsage.EditorProject:
                path += "Editor/";
                break;
            case SettingsUsage.EditorUser:
                path += "Editor/User/" + GetProjectFolderName() + '/';
                break;
#endif
            default: throw new System.InvalidOperationException();
        }

        return path;
    }


    // Called to validate settings changes.
    protected virtual void OnValidate() { }

#if UNITY_EDITOR
    // Sets the specified setting to the desired value and marks the settings
    // so that it will be saved.
    protected void Set<S>(ref S setting, S value)
    {
        if (EqualityComparer<S>.Default.Equals(setting, value)) return;
        setting = value;
        OnValidate();
        SetDirty();
    }

    // Marks the settings dirty so that it will be saved.
    protected new void SetDirty() => EditorUtility.SetDirty(this);

    // The directory name of the current project folder.
    static string GetProjectFolderName() { return new DirectoryInfo(Path.GetFullPath(Path.Combine(Application.dataPath, ".."))).Name; }
#endif

    // Base class for settings contained by a Settings<T> instance.
    [Serializable]
    public abstract class SubSettings
    {
        // Called when a setting is modified.
        protected virtual void OnValidate() { }

#if UNITY_EDITOR
        // Sets the specified setting to the desired value and marks the settings
        // instance so that it will be saved.
        protected void Set<S>(ref S setting, S value)
        {
            if (EqualityComparer<S>.Default.Equals(setting, value)) return;
            setting = value;
            OnValidate();
            Instance.SetDirty();
        }
#endif
    }
}