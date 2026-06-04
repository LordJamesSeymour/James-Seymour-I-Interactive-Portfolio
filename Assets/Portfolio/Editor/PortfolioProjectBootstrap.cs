using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Editor
{
    public static class PortfolioProjectBootstrap
    {
        private const string TemplateSetting = "PROJECT:PortfolioFullscreen";

        private static readonly string[] PortfolioFolders =
        {
            "Assets/Portfolio/Scripts/Core",
            "Assets/Portfolio/Scripts/Player",
            "Assets/Portfolio/Scripts/Camera",
            "Assets/Portfolio/Scripts/Interaction",
            "Assets/Portfolio/Scripts/UI",
            "Assets/Portfolio/Scripts/Data",
            "Assets/Portfolio/Prefabs",
            "Assets/Portfolio/Prefabs/Buildings",
            "Assets/Portfolio/ScriptableObjects/Projects",
            "Assets/Portfolio/Scenes",
            "Assets/Portfolio/Art",
            "Assets/Portfolio/Materials",
            "Assets/Portfolio/Editor",
            "Assets/WebGLTemplates/PortfolioFullscreen"
        };

        [MenuItem("Portfolio/Setup/Apply All Project Defaults")]
        public static void ApplyAllProjectDefaults()
        {
            EnsurePortfolioFolders();
            ApplyUnitySerializationSettings();
            ConfigureWebTemplate();
            TryDisableSplashScreen();
            AssetDatabase.SaveAssets();
            Debug.Log("Portfolio project defaults applied.");
        }

        public static void ApplyAllForBatchmode()
        {
            ApplyAllProjectDefaults();

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Portfolio/Setup/Create Missing Portfolio Folders")]
        public static void EnsurePortfolioFolders()
        {
            foreach (string folder in PortfolioFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                Directory.CreateDirectory(folder);
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Portfolio/Setup/Apply Unity Serialization Settings")]
        public static void ApplyUnitySerializationSettings()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;

            PropertyInfo versionControlProperty = typeof(EditorSettings).GetProperty(
                "externalVersionControl",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (versionControlProperty != null && versionControlProperty.CanWrite)
            {
                versionControlProperty.SetValue(null, "Visible Meta Files");
            }

            Debug.Log("Unity serialization is set to Force Text. Version control mode is expected to remain Visible Meta Files.");
        }

        [MenuItem("Portfolio/Setup/Use Portfolio Fullscreen Web Template")]
        public static bool ConfigureWebTemplate()
        {
            Type webGlSettingsType = typeof(PlayerSettings).GetNestedType("WebGL", BindingFlags.Public | BindingFlags.NonPublic);
            if (webGlSettingsType == null)
            {
                Debug.LogWarning("Could not find PlayerSettings.WebGL; select the PortfolioFullscreen template manually in Project Settings.");
                return false;
            }

            PropertyInfo templateProperty = webGlSettingsType.GetProperty(
                "template",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (templateProperty == null || !templateProperty.CanWrite)
            {
                Debug.LogWarning("Could not set PlayerSettings.WebGL.template automatically; select PortfolioFullscreen manually.");
                return false;
            }

            templateProperty.SetValue(null, TemplateSetting);
            Debug.Log($"WebGL template set to {TemplateSetting}.");
            return true;
        }

        [MenuItem("Portfolio/Setup/Try Disable Unity Splash Screen")]
        public static bool TryDisableSplashScreen()
        {
            Type splashScreenType = typeof(PlayerSettings).GetNestedType("SplashScreen", BindingFlags.Public | BindingFlags.NonPublic);
            if (splashScreenType == null)
            {
                Debug.LogWarning("Could not find PlayerSettings.SplashScreen; disable the splash screen manually if your license allows it.");
                return false;
            }

            bool changed = false;
            changed |= TrySetStaticBoolProperty(splashScreenType, "show", false);
            changed |= TrySetStaticBoolProperty(splashScreenType, "showUnityLogo", false);

            if (!changed)
            {
                Debug.LogWarning("Unity splash screen settings could not be changed automatically. This may be restricted by the current Unity version or license.");
            }
            else
            {
                Debug.Log("Requested Unity splash screen disable through PlayerSettings.SplashScreen.");
            }

            return changed;
        }

        private static bool TrySetStaticBoolProperty(Type type, string propertyName, bool value)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
            {
                return false;
            }

            try
            {
                property.SetValue(null, value);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not set {type.FullName}.{propertyName}: {exception.Message}");
                return false;
            }
        }
    }
}
