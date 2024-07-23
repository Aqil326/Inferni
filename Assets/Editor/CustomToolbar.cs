using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Diagnostics;
using UnityToolbarExtender;
using System.Collections.Generic;
using System.Linq;

static class ToolbarStyles
{
    public static readonly GUIStyle commandButtonStyle;

    static ToolbarStyles()
    {
        commandButtonStyle = new GUIStyle("Command")
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageAbove,
            fontStyle = FontStyle.Bold
        };
    }
}

[InitializeOnLoad]
public class CustomToolbar
{
    private static string DEMO_DEFINE = "IS_DEMO";
    private static string NO_STEAMWORKS_DEFINE = "NOSTEAMWORKS";

    static CustomToolbar()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {
        GUILayout.FlexibleSpace();

        bool isDemo = false;
#if IS_DEMO
        isDemo = true;
#endif

        bool noStem = false;
#if NOSTEAMWORKS
        noStem = true;
#endif

        if (GUILayout.Button(new GUIContent("Is Demo : " + (isDemo? "ON" : "OFF"), "Toggles if the build is Demo or not")))
        {
            ToggleIsDemo();
        }

        if (GUILayout.Button(new GUIContent("Steam Support : " + (noStem? "OFF" : "ON"), "Toggles if the build support Steam or not")))
        {
            ToggleNoStem();
        }

    }

    private static void ToggleIsDemo()
    {
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        List<string> allDefines = definesString.Split(';').ToList();

        if(allDefines.Contains(DEMO_DEFINE))
        {
            allDefines.Remove(DEMO_DEFINE);
        }
        else
        {
            allDefines.Add(DEMO_DEFINE);
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup,
            string.Join(";", allDefines.ToArray()));
    }

    private static void ToggleNoStem()
    {
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        List<string> allDefines = definesString.Split(';').ToList();

        if(allDefines.Contains(NO_STEAMWORKS_DEFINE))
        {
            allDefines.Remove(NO_STEAMWORKS_DEFINE);
        }
        else
        {
            allDefines.Add(NO_STEAMWORKS_DEFINE);
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup,
            string.Join(";", allDefines.ToArray()));
    }
}
