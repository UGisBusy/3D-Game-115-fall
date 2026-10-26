using UnityEngine;
using UnityEngine.InputSystem;

public class SequenceManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField]
    string currentStateName = "None";

    [Header("Menus")]
    [SerializeField]
    MainMenu mainMenu;

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

        stateMachine = new StateMachine((newState) => currentStateName = newState.ToString());
    }

    private void Start()
    {
        SetStates();
        SetLinks();

        stateMachine.EnterState(mainMenuState);
    }

    private void SetStates()
    {
        mainMenuState = new State("Main Menu", enter: mainMenu.Show, exit: mainMenu.Hide);
        levelSelectState = new State("Level Select");
        saveLoadState = new State("Save Load");
        gameState = new State("Game");
        exitState = new State("Exit", enter: ExitApp);
    }

    private void SetLinks()
    {
        mainMenuState.AddLink(
            new Link(
                levelSelectState,
                new EventWrapper
                {
                    Subscribe = h => MainMenuEvents.StartClicked += h,
                    Unsubscribe = h => MainMenuEvents.StartClicked -= h,
                }
            )
        );

        mainMenuState.AddLink(
            new Link(
                saveLoadState,
                new EventWrapper
                {
                    Subscribe = h => MainMenuEvents.LoadClicked += h,
                    Unsubscribe = h => MainMenuEvents.LoadClicked -= h,
                }
            )
        );

        mainMenuState.AddLink(
            new Link(
                exitState,
                new EventWrapper
                {
                    Subscribe = h => MainMenuEvents.QuitClicked += h,
                    Unsubscribe = h => MainMenuEvents.QuitClicked -= h,
                }
            )
        );

        // TEMP, TODO: remove these
        InputAction advanceStateAction = new InputAction(binding: "<Keyboard>/p");

        // TEMP, TODO: remove these
        levelSelectState.AddLink(new InputLink(saveLoadState, advanceStateAction));
        saveLoadState.AddLink(new InputLink(gameState, advanceStateAction));
        gameState.AddLink(new InputLink(exitState, advanceStateAction));
    }

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
