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

    static SequenceManager instance;

    // Menu scene's MenuUIManager registers itself here, null while Menu scene is not loaded.
    // Static so it works no matter whose Awake runs first.
    // Menu states must only be entered after the Menu scene finished loading.
    static MenuUIManager menuUI;

    StateMachine stateMachine;
    State mainMenuState,
        levelSelectState,
        saveLoadState,
        gameState,
        exitState;

    // set by MenuEvents.LevelStartRequested, read when entering gameState
    LevelId selectedLevel;

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

        // TODO: start from checkpoint, for now Resume starts the level from the start too
        // if (fromCheckpoint)
    }

    private void SetStates()
    {
        mainMenuState = new State("Main Menu", EnterMainMenu, ExitMenus);
        levelSelectState = new State("Level Select", EnterLevelSelect, ExitMenus);
        saveLoadState = new State("Save Load", EnterSaveLoad, ExitMenus);
        gameState = new State("Game", EnterGame);
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
    }

    private void EnterLevelSelect()
    {
        menuUI.ShowLevelSelect();
        backAction.action.Enable();
    }

    private void EnterSaveLoad()
    {
        menuUI.ShowSaveLoad();
        backAction.action.Enable();
    }

    private void EnterGame()
    {
        // async: we are inside a button / input callback here, so don't load synchronously
        SceneManager.LoadSceneAsync(GetSceneName(selectedLevel));
    }

    // must match scene names in Build Settings
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
