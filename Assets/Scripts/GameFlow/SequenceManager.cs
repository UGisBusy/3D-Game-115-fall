using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SequenceManager : MonoBehaviour
{
    [SerializeField]
    InputActionReference backAction; // Menu/Back (Escape)

    [Header("Debug")]
    [SerializeField]
    string currentStateName = "None";

    [SerializeField]
    int resumeCheckpoint; // TODO: read from Save File; checkpoint used by the Start From Checkpoint buttons

    static SequenceManager instance;

    // Menu scene's MenuUIManager registers itself here, null while Menu scene is not loaded.
    // Static so it works no matter whose Awake runs first.
    // Menu states must only be entered after the Menu scene finished loading.
    static MenuUIManager menuUI;

    // Level scene's Level Manager registers itself here, null while no level is loaded.
    static ILevelManager levelManager;

    StateMachine stateMachine;
    State mainMenuState,
        levelSelectState,
        saveLoadState,
        gameState,
        exitState;

    // set by MenuEvents.LevelStartRequested, read when entering gameState
    LevelId selectedLevel;
    int selectedCheckpoint; // 0 = start from the beginning

    private void Awake()
    {
        // prevent having two SequenceManager instance
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (backAction == null)
            throw new System.NullReferenceException("Sequence Manager has no Back Action");

        stateMachine = new StateMachine((newState) => currentStateName = newState.ToString());

        // backAction is only enabled while in a menu state
        backAction.action.performed += OnBackPerformed;

        // raised right before LevelStartRequestedWrapper moves us into gameState
        MenuEvents.LevelStartRequested += OnLevelStartRequested;
    }

    private void Start()
    {
        SetStates();
        stateMachine.EnterState(mainMenuState);
    }

    // ---------- Menu UI registration ----------
    public static void RegisterMenuUI(MenuUIManager ui) => menuUI = ui;

    public static void UnregisterMenuUI(MenuUIManager ui)
    {
        if (menuUI == ui)
            menuUI = null;
    }

    // ---------- Level Manager registration ----------
    // false when a level scene is played directly in the editor; the Level Manager then starts itself
    public static bool IsRunning => instance != null;

    public static void RegisterLevelManager(ILevelManager manager) => levelManager = manager;

    public static void UnregisterLevelManager(ILevelManager manager)
    {
        if (levelManager == manager)
            levelManager = null;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        backAction.action.performed -= OnBackPerformed;
        backAction.action.Disable();
        MenuEvents.LevelStartRequested -= OnLevelStartRequested;
        instance = null;
    }

    private void OnBackPerformed(InputAction.CallbackContext _) => MenuEvents.RaiseBackRequested();

    private void OnLevelStartRequested(LevelId level, bool fromCheckpoint)
    {
        selectedLevel = level;
        selectedCheckpoint = fromCheckpoint ? resumeCheckpoint : 0;
    }

    private void SetStates()
    {
        mainMenuState = new State("Main Menu", EnterMainMenu, ExitMenus);
        levelSelectState = new State("Level Select", EnterLevelSelect, ExitMenus);
        saveLoadState = new State("Save Load", EnterSaveLoad, ExitMenus);
        gameState = new State("Game", EnterGame, ExitGame);
        exitState = new State("Exit", enter: ExitApp);

        mainMenuState.AddLink(new Link(levelSelectState, MenuEvents.PlayRequestedWrapper));
        mainMenuState.AddLink(new Link(saveLoadState, MenuEvents.LoadRequestedWrapper));
        mainMenuState.AddLink(new Link(exitState, MenuEvents.ExitRequestedWrapper));
        mainMenuState.AddLink(new Link(exitState, MenuEvents.BackRequestedWrapper));

        levelSelectState.AddLink(new Link(gameState, MenuEvents.LevelStartRequestedWrapper));
        levelSelectState.AddLink(new Link(mainMenuState, MenuEvents.BackRequestedWrapper));

        saveLoadState.AddLink(new Link(mainMenuState, MenuEvents.BackRequestedWrapper));
    }

    private void ExitMenus()
    {
        menuUI.HideAll();
        backAction.action.Disable();
    }

    private void EnterMainMenu()
    {
        menuUI.ShowMainMenu();
        backAction.action.Enable();
        CursorControl.SetLocked(false);
    }

    private void EnterLevelSelect()
    {
        menuUI.ShowLevelSelect();
        backAction.action.Enable();
        CursorControl.SetLocked(false);
    }

    private void EnterSaveLoad()
    {
        menuUI.ShowSaveLoad();
        backAction.action.Enable();
        CursorControl.SetLocked(false);
    }

    private void EnterGame()
    {
        LevelEvents.Exited += OnLevelExited;

        // async: we are inside a button / input callback here, so don't load synchronously
        SceneManager.LoadSceneAsync(GetSceneName(selectedLevel)).completed += _ => StartLevel();
    }

    // Level Manager registers in its Awake, which has run by the time loading completed.
    private void StartLevel()
    {
        if (levelManager == null)
        {
            Debug.LogWarning(
                $"Sequence Manager: {selectedLevel} level has no registered Level Manager"
            );
            return;
        }

        levelManager.StartLevel(selectedCheckpoint);
    }

    private void ExitGame()
    {
        LevelEvents.Exited -= OnLevelExited;
    }

    private void OnLevelExited()
    {
        // stay in gameState while loading, but don't react to a second Exited
        LevelEvents.Exited -= OnLevelExited;

        // Menu states need menuUI, which registers in the Menu scene's Awake,
        // so only enter Main Menu after the scene finished loading.
        SceneManager.LoadSceneAsync(MenuSceneName).completed += _ =>
            stateMachine.EnterState(mainMenuState);
    }

    // must match scene names in Build Settings
    private const string MenuSceneName = "Menu";

    private static string GetSceneName(LevelId level)
    {
        return level switch
        {
            LevelId.Obstacle => "ObstacleLevel",
            LevelId.Combat => "CombatLevel",
            _ => throw new System.ArgumentOutOfRangeException(nameof(level), level, null),
        };
    }

    private void ExitApp() => StartCoroutine(ExitAppNextFrame());

    private IEnumerator ExitAppNextFrame()
    {
        yield return null; // leave the input callback first
        Debug.Log("Exit App");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
