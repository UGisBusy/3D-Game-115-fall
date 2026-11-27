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

    [SerializeField]
    private Button LevelSelectBackButton;

    [Header("Save Load Menu")]
    [SerializeField]
    private GameObject SaveLoadPanel;

    [SerializeField]
    private Button SaveLoadBackButton;

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

        if (LevelSelectBackButton == null)
            throw new System.NullReferenceException(
                "Menu UI Manager has no Level Select Back Button"
            );

        // Save Load Menu UI elements checks
        if (SaveLoadPanel == null)
            throw new System.NullReferenceException("Menu UI Manager has no Save Load Panel");

        if (SaveLoadBackButton == null)
            throw new System.NullReferenceException("Menu UI Manager has no Save Load Back Button");

        BindButtons();

        // SequenceManager outlives this scene, so it can't hold a serialized ref to us
        SequenceManager.RegisterMenuUI(this);
    }

    private void OnDestroy()
    {
        SequenceManager.UnregisterMenuUI(this);
    }

    // Buttons live and die with this scene, so listeners are never removed.
    private void BindButtons()
    {
        // Main Menu
        PlayButton.onClick.AddListener(MenuEvents.RaisePlayRequested);
        LoadButton.onClick.AddListener(MenuEvents.RaiseLoadRequested);
        ExitButton.onClick.AddListener(MenuEvents.RaiseExitRequested);

        // Level Select Menu
        ObstacleStartButton.onClick.AddListener(OnObstacleStartClicked);
        ObstacleResumeButton.onClick.AddListener(OnObstacleResumeClicked);
        CombatStartButton.onClick.AddListener(OnCombatStartClicked);
        CombatResumeButton.onClick.AddListener(OnCombatResumeClicked);
        LevelSelectBackButton.onClick.AddListener(MenuEvents.RaiseBackRequested);

        // Save Load Menu
        SaveLoadBackButton.onClick.AddListener(MenuEvents.RaiseBackRequested);
    }

    private void OnObstacleStartClicked() =>
        MenuEvents.RaiseLevelStartRequested(LevelId.Obstacle, fromCheckpoint: false);

    private void OnObstacleResumeClicked() =>
        MenuEvents.RaiseLevelStartRequested(LevelId.Obstacle, fromCheckpoint: true);

    private void OnCombatStartClicked() =>
        MenuEvents.RaiseLevelStartRequested(LevelId.Combat, fromCheckpoint: false);

    private void OnCombatResumeClicked() =>
        MenuEvents.RaiseLevelStartRequested(LevelId.Combat, fromCheckpoint: true);

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
