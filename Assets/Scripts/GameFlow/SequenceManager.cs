using UnityEngine;
using UnityEngine.InputSystem;

public class SequenceManager : MonoBehaviour
{
    [SerializeField]
    MenuUIManager uiManager;

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

        stateMachine = new StateMachine((newState) => currentStateName = newState.ToString());
    }

    private void Start()
    {
        SetStates();
        SetLinks();

        uiManager.HideAll();
        stateMachine.EnterState(mainMenuState);
    }

    private void SetStates()
    {
        mainMenuState = new State("Main Menu");
        levelSelectState = new State("Level Select");
        saveLoadState = new State("Save Load");
        gameState = new State("Game");
        exitState = new State("Exit", enter: ExitApp);
    }

    private void SetLinks()
    {
        // TEMP, TODO: remove these
        InputAction advanceStateAction = new InputAction(binding: "<Keyboard>/p");

        // TEMP, TODO: remove these
        mainMenuState.AddLink(new InputLink(levelSelectState, advanceStateAction));
        levelSelectState.AddLink(new InputLink(saveLoadState, advanceStateAction));
        saveLoadState.AddLink(new InputLink(gameState, advanceStateAction));
        gameState.AddLink(new InputLink(exitState, advanceStateAction));
    }

    private void EnterMainMenu() { }

    private void ExitMainMenu() { }

    private void EnterLevelSelect() { }

    private void ExitLevelSelect() { }

    private void EnterSaveLoad() { }

    private void ExitSaveLoad() { }

    private void ExitApp()
    {
        Debug.Log("Exit App");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
