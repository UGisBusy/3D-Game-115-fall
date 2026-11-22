using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SequenceManager : MonoBehaviour
{
    [SerializeField]
    MenuUIManager uiManager;

    [SerializeField]
    InputActionReference backAction; // Menu/Back (Escape)

    [Header("Debug")]
    [SerializeField]
    string currentStateName = "None";

    static SequenceManager instance;
    StateMachine stateMachine;
    State mainMenuState,
        levelSelectState,
        saveLoadState,
        gameState,
        exitState;

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

        if (uiManager == null)
            throw new System.NullReferenceException("Sequence Manager has no UI Manager");

        if (backAction == null)
            throw new System.NullReferenceException("Sequence Manager has no Back Action");

        stateMachine = new StateMachine((newState) => currentStateName = newState.ToString());

        // backAction is only enabled while in a menu state
        backAction.action.performed += OnBackPerformed;
    }

    private void Start()
    {
        SetStates();

        uiManager.HideAll();
        stateMachine.EnterState(mainMenuState);
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        backAction.action.performed -= OnBackPerformed;
        backAction.action.Disable();
        instance = null;
    }

    private void OnBackPerformed(InputAction.CallbackContext _) => MenuEvents.RaiseBackRequested();

    private void SetStates()
    {
        mainMenuState = new State("Main Menu", EnterMainMenu, ExitMainMenu);
        levelSelectState = new State("Level Select", EnterLevelSelect, ExitLevelSelect);
        saveLoadState = new State("Save Load", EnterSaveLoad, ExitSaveLoad);
        gameState = new State("Game");
        exitState = new State("Exit", enter: ExitApp);

        mainMenuState.AddLink(new Link(levelSelectState, MenuEvents.PlayRequestedWrapper));
        mainMenuState.AddLink(new Link(saveLoadState, MenuEvents.LoadRequestedWrapper));
        mainMenuState.AddLink(new Link(exitState, MenuEvents.ExitRequestedWrapper));
        mainMenuState.AddLink(new Link(exitState, MenuEvents.BackRequestedWrapper));

        levelSelectState.AddLink(new Link(gameState, MenuEvents.LevelStartRequestedWrapper));
        levelSelectState.AddLink(new Link(mainMenuState, MenuEvents.BackRequestedWrapper));

        saveLoadState.AddLink(new Link(mainMenuState, MenuEvents.BackRequestedWrapper));
    }

    private void EnterMainMenu()
    {
        uiManager.ShowMainMenu();
        backAction.action.Enable();
    }

    private void ExitMainMenu()
    {
        uiManager.HideAll();
        backAction.action.Disable();
    }

    private void EnterLevelSelect()
    {
        uiManager.ShowLevelSelect();
        backAction.action.Enable();
    }

    private void ExitLevelSelect()
    {
        uiManager.HideAll();
        backAction.action.Disable();
    }

    private void EnterSaveLoad()
    {
        uiManager.ShowSaveLoad();
        backAction.action.Enable();
    }

    private void ExitSaveLoad()
    {
        uiManager.HideAll();
        backAction.action.Disable();
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
