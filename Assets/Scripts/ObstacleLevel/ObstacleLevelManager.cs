using LevelSystem;
using PlayerSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ObstacleLevel
{
    /// <summary>
    /// Obstacle level flow. States (event-driven through Links):
    ///   Start --StartFinished-->   Play      spawn the Player at the first Checkpoint
    ///   Play  --PauseRequested-->  Pause     ESC
    ///   Pause --ResumeRequested--> Play      ESC
    ///   Play  --Completed-->       Complete  Star collected
    /// Pause freezes time and the Player's input. Complete stops the Player's input.
    /// Owns the Crystal count: the Star stays hidden until every Crystal is collected.
    /// Respawning is owned by SpawnManager.
    /// </summary>
    public class ObstacleLevelManager : MonoBehaviour
    {
        [SerializeField]
        private Player player;

        [SerializeField]
        private SpawnManager spawnManager;

        [SerializeField]
        private InputActionReference pauseAction; // Level/Pause (Escape)

        [SerializeField]
        private Checkpoint startCheckpoint; // TODO: use editor to set it for now, later seq manager should set it

        [Header("Crystals")]
        [Tooltip("Parent of every Crystal in the level.")]
        [SerializeField]
        private Transform crystals;

        [Tooltip("The Star in the scene. Hidden until every Crystal is collected.")]
        [SerializeField]
        private GameObject star;

        [Header("Debug")]
        [SerializeField]
        private string currentStateName;

        private int crystalTotal;
        private int crystalCollected;

        private readonly LevelTransitions transitions = new();
        private StateMachine stateMachine;
        private TickState startState,
            playState,
            pauseState,
            completeState;

        private void Awake()
        {
            BuildStateMachine();
        }

        private void OnEnable()
        {
            pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            pauseAction.action.Disable();
        }

        // Start instead of Awake: the Player and Checkpoints must finish their Awake first.
        private void Start()
        {
            stateMachine.EnterState(startState);
        }

        private void Update()
        {
            stateMachine.Tick(Time.deltaTime);
        }

        // Leaving the scene while paused would keep time frozen, so run the current state's exit.
        private void OnDestroy()
        {
            stateMachine.ExitState();
        }

        private void BuildStateMachine()
        {
            startState = new TickState(
                "Start",
                OnStartEnter,
                tick: _ => transitions.RaiseStartFinished()
            );
            playState = new TickState("Play", OnPlayEnter, OnPlayExit, PlayTick);
            pauseState = new TickState("Pause", OnPauseEnter, OnPauseExit, PauseTick);
            completeState = new TickState("Complete", OnCompleteEnter);

            startState.AddLink(new Link(playState, transitions.StartFinishedWrapper));
            playState.AddLink(new Link(pauseState, transitions.PauseRequestedWrapper));
            playState.AddLink(new Link(completeState, transitions.CompletedWrapper));
            pauseState.AddLink(new Link(playState, transitions.ResumeRequestedWrapper));

            stateMachine = new StateMachine(state => currentStateName = state.Name);
        }

        // ------------- StartLevel -------------
        public void StartLevel(Checkpoint startCheckpoint)
        {
            // TODO: for later seq manager to call
            this.startCheckpoint = startCheckpoint;
            stateMachine.EnterState(startState);
        }

        // ---------------- Start ----------------

        private void OnStartEnter()
        {
            spawnManager.SetActiveCheckpoint(startCheckpoint);
            spawnManager.SpawnPlayer();

            crystalTotal = crystals.childCount;
            crystalCollected = 0;
            star.SetActive(false);
            ObstacleLevelEvents.RaiseCrystalCountChanged(crystalCollected, crystalTotal);
        }

        // ---------------- Play ----------------

        // Runs again when resuming from Pause, so it only subscribes.
        private void OnPlayEnter()
        {
            LevelEvents.Collected += OnCollected;
            ObstacleLevelEvents.CrystalCollected += OnCrystalCollected;
        }

        private void OnPlayExit()
        {
            LevelEvents.Collected -= OnCollected;
            ObstacleLevelEvents.CrystalCollected -= OnCrystalCollected;
        }

        private void OnCrystalCollected()
        {
            crystalCollected++;
            ObstacleLevelEvents.RaiseCrystalCountChanged(crystalCollected, crystalTotal);

            if (crystalCollected == crystalTotal) // all Crystals collected
            {
                star.SetActive(true); // spawn the star
            }
        }

        private void PlayTick(float dt)
        {
            if (pauseAction.action.WasPressedThisFrame())
            {
                transitions.RaisePauseRequested();
            }
        }

        private void OnCollected(Collectible collectible)
        {
            if (collectible.Type == CollectibleType.Star)
            {
                transitions.RaiseCompleted();
            }
        }

        // ---------------- Pause ----------------

        private void OnPauseEnter()
        {
            Time.timeScale = 0f;
            player.Input.SetEnabled(false);
        }

        private void OnPauseExit()
        {
            Time.timeScale = 1f;
            player.Input.SetEnabled(true);
        }

        private void PauseTick(float dt) // resume the world
        {
            if (pauseAction.action.WasPressedThisFrame())
            {
                transitions.RaiseResumeRequested();
            }
        }

        // ---------------- Complete ----------------

        private void OnCompleteEnter()
        {
            player.Input.SetEnabled(false);
        }
    }
}
