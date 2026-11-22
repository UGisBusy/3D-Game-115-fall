using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper for Sequence Managers.
/// Control the all UI elements in Menu Scene (Main Menu, Save Load Menu, and Level Select Menu).
/// </summary>
public class MenuUIManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField]
    private GameObject MainMenuPanel;

    [SerializeField]
    private Button PlayButton;

    [SerializeField]
    private Button LoadButton;

    [SerializeField]
    private Button ExitButton;

    [Header("Level Select Menu")]
    [SerializeField]
    private GameObject LevelSelectPanel;

    [SerializeField]
    private Button ObstacleStartButton;

    [SerializeField]
    private Button ObstacleResumeButton;

    [SerializeField]
    private Button CombatStartButton;

    [SerializeField]
    private Button CombatResumeButton;

    [Header("Save Load Menu")]
    [SerializeField]
    private GameObject SaveLoadPanel;

    // TODO: Save Load UI

    private void Awake()
    {
        // Main Menu UI elements checks
        if (MainMenuPanel == null)
            throw new System.NullReferenceException("Menu UI Manager has no Main Menu Panel");

        if (PlayButton == null)
            throw new System.NullReferenceException("Menu UI Manager has no Play Button");

        if (LoadButton == null)
            throw new System.NullReferenceException("Menu UI Manager has no Load Button");

        if (ExitButton == null)
            throw new System.NullReferenceException("Menu UI Manager has no Exit Button");

        // Level Select Menu UI elements checks
        if (LevelSelectPanel == null)
            throw new System.NullReferenceException("Menu UI Manager has no Level Select Panel");

        if (ObstacleStartButton == null)
            throw new System.NullReferenceException("Menu UI Manager has no Obstacle Start Button");

        if (ObstacleResumeButton == null)
            throw new System.NullReferenceException(
                "Menu UI Manager has no Obstacle Resume Button"
            );

        if (CombatStartButton == null)
            throw new System.NullReferenceException("Menu UI Manager has no Combat Start Button");

        if (CombatResumeButton == null)
            throw new System.NullReferenceException("Menu UI Manager has no Combat Resume Button");

        // Save Load Menu UI elements checks
        if (SaveLoadPanel == null)
            throw new System.NullReferenceException("Menu UI Manager has no Save Load Panel");
    }

    public void ShowMainMenu() => ShowOnly(MainMenuPanel);

    public void ShowLevelSelect() => ShowOnly(LevelSelectPanel);

    public void ShowSaveLoad() => ShowOnly(SaveLoadPanel);

    public void HideAll() => ShowOnly(null);

    private void ShowOnly(GameObject panel)
    {
        SetActive(MainMenuPanel, MainMenuPanel == panel);
        SetActive(LevelSelectPanel, LevelSelectPanel == panel);
        SetActive(SaveLoadPanel, SaveLoadPanel == panel);
    }

    private static void SetActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
