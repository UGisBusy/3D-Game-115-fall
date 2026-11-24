using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper for Level Managers.
/// Control the all screen UI in a level scene (Pause Screen, Fail Screen, and Complete Screen).
/// Buttons only raise LevelTransitions / LevelEvents, so this needs no ref to the Level Manager,
/// and the same prefab works in every level scene. Keyboard (ESC / Space) is handled by the Level Manager.
/// </summary>
public class LevelUIManager : MonoBehaviour
{
    [Header("Pause Screen")]
    [SerializeField]
    private GameObject PausePanel;

    [SerializeField]
    private Button PauseContinueButton;

    [SerializeField]
    private Button PauseSaveButton;

    [SerializeField]
    private Button PauseMenuButton;

    [Header("Fail Screen")]
    [SerializeField]
    private GameObject FailPanel;

    [SerializeField]
    private Button FailRetryButton;

    [SerializeField]
    private Button FailSaveButton;

    [SerializeField]
    private Button FailMenuButton;

    [Header("Complete Screen")]
    [SerializeField]
    private GameObject CompletePanel;

    [SerializeField]
    private Button CompleteMenuButton;

    private void Awake()
    {
        // Pause Screen UI elements checks
        if (PausePanel == null)
            throw new System.NullReferenceException("Level UI Manager has no Pause Panel");

        if (PauseContinueButton == null)
            throw new System.NullReferenceException("Level UI Manager has no Pause Continue Button");

        if (PauseSaveButton == null)
            throw new System.NullReferenceException("Level UI Manager has no Pause Save Button");

        if (PauseMenuButton == null)
            throw new System.NullReferenceException("Level UI Manager has no Pause Menu Button");

        // Fail Screen UI elements checks
        if (FailPanel == null)
            throw new System.NullReferenceException("Level UI Manager has no Fail Panel");

        if (FailRetryButton == null)
            throw new System.NullReferenceException("Level UI Manager has no Fail Retry Button");

        if (FailSaveButton == null)
            throw new System.NullReferenceException("Level UI Manager has no Fail Save Button");

        if (FailMenuButton == null)
            throw new System.NullReferenceException("Level UI Manager has no Fail Menu Button");

        // Complete Screen UI elements checks
        if (CompletePanel == null)
            throw new System.NullReferenceException("Level UI Manager has no Complete Panel");

        if (CompleteMenuButton == null)
            throw new System.NullReferenceException("Level UI Manager has no Complete Menu Button");

        BindButtons();

        // nothing is shown until the Level Manager asks
        HideAll();
    }

    // Buttons live and die with this scene, so listeners are never removed.
    private void BindButtons()
    {
        // Pause Screen
        PauseContinueButton.onClick.AddListener(LevelTransitions.RaiseContinueRequested);
        PauseSaveButton.onClick.AddListener(LevelEvents.RaiseSaveRequested);
        PauseMenuButton.onClick.AddListener(LevelTransitions.RaiseExitRequested);

        // Fail Screen
        FailRetryButton.onClick.AddListener(LevelTransitions.RaiseRetryRequested);
        FailSaveButton.onClick.AddListener(LevelEvents.RaiseSaveRequested);
        FailMenuButton.onClick.AddListener(LevelTransitions.RaiseExitRequested);

        // Complete Screen
        CompleteMenuButton.onClick.AddListener(LevelTransitions.RaiseExitRequested);
    }

    public void ShowPause() => ShowOnly(PausePanel);

    public void ShowFail() => ShowOnly(FailPanel);

    public void ShowComplete() => ShowOnly(CompletePanel);

    public void HideAll() => ShowOnly(null);

    private void ShowOnly(GameObject panel)
    {
        PausePanel.SetActive(PausePanel == panel);
        FailPanel.SetActive(FailPanel == panel);
        CompletePanel.SetActive(CompletePanel == panel);
    }
}
