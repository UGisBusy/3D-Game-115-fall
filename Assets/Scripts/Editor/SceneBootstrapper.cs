using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class SceneBootstrapper
{
    private const string keyLoadMenuScene = "3DG/Load Menu Scene";
    private const string keyPreviousScene = "PreviousScene";
    private const string MenuScene = "Assets/Scenes/Menu.unity";

    private static bool isLoadMenuScene
    {
        get => EditorPrefs.GetBool(keyLoadMenuScene);
        set => EditorPrefs.SetBool(keyLoadMenuScene, value);
    }

    private static string PreviousScene
    {
        get => EditorPrefs.GetString(keyPreviousScene);
        set => EditorPrefs.SetString(keyPreviousScene, value);
    }

    static SceneBootstrapper()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
    {
        if (!isLoadMenuScene)
            return;

        switch (playModeStateChange)
        {
            case PlayModeStateChange.ExitingEditMode:
                PreviousScene = EditorSceneManager.GetActiveScene().path;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(MenuScene);
                break;

            case PlayModeStateChange.EnteredEditMode:
                if (!string.IsNullOrEmpty(PreviousScene))
                    EditorSceneManager.OpenScene(PreviousScene);
                break;
        }
    }

    // menu items
    [MenuItem(keyLoadMenuScene)]
    private static void ToggleLoadBootScene()
    {
        isLoadMenuScene = !isLoadMenuScene;
    }

    [MenuItem(keyLoadMenuScene, true)]
    private static bool ValidateToggleLoadBootScene()
    {
        Menu.SetChecked(keyLoadMenuScene, isLoadMenuScene);
        return true;
    }
}
