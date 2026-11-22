using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 3DG > Load Menu Scene: when checked, Play always starts from the Menu scene, whatever scene is open.
/// Uses Unity's playModeStartScene, so the open scene is never closed or reopened in the editor:
/// stopping returns to it exactly as it was, and the Inspector never points at destroyed objects.
/// </summary>
[InitializeOnLoad]
public static class SceneBootstrapper
{
    private const string keyLoadMenuScene = "3DG/Load Menu Scene";
    private const string MenuScene = "Assets/Scenes/Menu.unity";

    private static bool isLoadMenuScene
    {
        get => EditorPrefs.GetBool(keyLoadMenuScene);
        set => EditorPrefs.SetBool(keyLoadMenuScene, value);
    }

    static SceneBootstrapper()
    {
        // The asset database may not be ready while scripts load; set it on the next editor tick.
        EditorApplication.delayCall += ApplyStartScene;
    }

    private static void ApplyStartScene()
    {
        EditorSceneManager.playModeStartScene = isLoadMenuScene
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene)
            : null;
    }

    // menu items
    [MenuItem(keyLoadMenuScene)]
    private static void ToggleLoadBootScene()
    {
        isLoadMenuScene = !isLoadMenuScene;
        ApplyStartScene();
    }

    [MenuItem(keyLoadMenuScene, true)]
    private static bool ValidateToggleLoadBootScene()
    {
        Menu.SetChecked(keyLoadMenuScene, isLoadMenuScene);
        return true;
    }
}
