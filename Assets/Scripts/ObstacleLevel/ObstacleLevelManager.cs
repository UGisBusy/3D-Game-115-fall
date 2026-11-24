using EffectSystem;
using LevelSystem;
using PlayerSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ObstacleLevel
{
    /// <summary>
    /// Obstacle level flow. States (event-driven through Links):
    ///   Start --StartFinished-->     Play      spawn the Player at the first Checkpoint
    ///   Play  --PauseRequested-->    Pause     ESC
    ///   Pause --ContinueRequested--> Play      ESC
    ///   Play  --Completed-->         Complete  Star collected
    /// Pause freezes time and the Player's input. Complete stops the Player's input.
    /// Owns the Crystal count: the Star stays hidden until every Crystal is collected.
    /// When it appears, the sky changes to starSky, so the Player knows from anywhere in the level.
    /// Respawning is owned by SpawnManager.
    /// Where the Player starts:
    ///  - Launched from the Menu: SequenceManager calls StartLevel(x) (ILevelManager): checkpoints[x], 0 = level start.
    ///  - Scene played directly in the editor: testStartCheckpoint (empty = checkpoints[0]).
    /// </summary>
    public class ObstacleLevelManager : MonoBehaviour, ILevelManager
    {
        [SerializeField]
        private Player player;

        [SerializeField]
        private SpawnManager spawnManager;

        [SerializeField]
        private InputActionReference pauseAction; // Level/Pause (Escape)

        [SerializeField]
        private Checkpoint[] checkpoints = new Checkpoint[0];

        [Tooltip("The Star in the scene. Hidden until every Crystal is collected.")]
        [SerializeField]
        private GameObject star;

        [Tooltip(
            "The sky after every Crystal is collected, so the Player sees the Star has appeared."
        )]
        [SerializeField]
        private Material starSky;

        [Tooltip("Plays on the Star when it appears.")]
        [SerializeField]
        private Effect starAppearedEffect;

        [Header("Debug")]
        [SerializeField]
        private string currentStateName;

        [SerializeField]
        private Checkpoint testStartCheckpoint;

        private Checkpoint spawnCheckpoint; // where this run starts

        private int crystalTotal;
        private int crystalCollected;

        private StateMachine stateMachine;
        private TickState startState,
            playState,
            pauseState,
            completeState;

        private void Awake()
        {
            if (checkpoints.Length == 0)
                throw new System.NullReferenceException(
                    "Obstacle Level Manager has no Checkpoints"
                );

            for (int i = 0; i < checkpoints.Length; i++)
                if (checkpoints[i] == null)
                    throw new System.NullReferenceException(
                        $"Obstacle Level Manager has no Checkpoint at element {i}"
                    );

            BuildStateMachine();
            SequenceManager.RegisterLevelManager(this);
        }

        private void OnEnable()
        {
            pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            pauseAction.action.Disable();
        }

        // From the Menu, SequenceManager calls StartLevel once the scene has loaded.
        // Played directly in the editor there is no SequenceManager, so start at testStartCheckpoint.
        // Start, not Awake: Player and Checkpoints need their Awake first.
        private void Start()
        {
            if (!SequenceManager.IsRunning)
                StartAt(testStartCheckpoint != null ? testStartCheckpoint : GetCheckpoint(0));
        }

        private void Update()
        {
            stateMachine.Tick(Time.deltaTime);
        }

        // Leaving the scene while paused would keep time frozen, so run the current state's exit.
        private void OnDestroy()
        {
            SequenceManager.UnregisterLevelManager(this);
            stateMachine.ExitState();
        }

        private void BuildStateMachine()
        {
            startState = new TickState(
                "Start",
                OnStartEnter,
                tick: _ => LevelTransitions.RaiseStartFinished()
            );
            playState = new TickState("Play", OnPlayEnter, OnPlayExit, PlayTick);
            pauseState = new TickState("Pause", OnPauseEnter, OnPauseExit, PauseTick);
            completeState = new TickState("Complete", OnCompleteEnter);

            startState.AddLink(new Link(playState, LevelTransitions.StartFinishedWrapper));
            playState.AddLink(new Link(pauseState, LevelTransitions.PauseRequestedWrapper));
            playState.AddLink(new Link(completeState, LevelTransitions.CompletedWrapper));
            pauseState.AddLink(new Link(playState, LevelTransitions.ContinueRequestedWrapper));

            stateMachine = new StateMachine(state => currentStateName = state.Name);
        }

        // ------------- StartLevel -------------

        // Called by SequenceManager once the scene has loaded (spec: Start State). checkpoint = checkpoints index.
        public void StartLevel(int checkpoint) => StartAt(GetCheckpoint(checkpoint));

        private void StartAt(Checkpoint checkpoint)
        {
            spawnCheckpoint = checkpoint;
            stateMachine.EnterState(startState);
        }

        // Awake made sure the list is filled, so only the number can be wrong (e.g. an old save).
        private Checkpoint GetCheckpoint(int number)
        {
            int last = checkpoints.Length - 1;
            if (number < 0 || number > last)
            {
                Debug.LogWarning(
                    $"{nameof(ObstacleLevelManager)}: no checkpoint {number} (0..{last}), clamped"
                );
                number = Mathf.Clamp(number, 0, last);
            }
            return checkpoints[number];
        }

        // ---------------- Start ----------------

        private void OnStartEnter()
        {
            spawnManager.SetActiveCheckpoint(spawnCheckpoint);
            spawnManager.SpawnPlayer();

            crystalTotal = CountCrystals();
            crystalCollected = 0;
            star.SetActive(false);
            ObstacleLevelEvents.RaiseCrystalCountChanged(crystalCollected, crystalTotal);
        }

        // ---------------- Play ----------------

        // Runs again when resuming from Pause, so it only subscribes.
        private void OnPlayEnter()
        {
            LevelEvents.Collected += OnCollected;
        }

        private void OnPlayExit()
        {
            LevelEvents.Collected -= OnCollected;
        }

        // Counts the Crystals placed in the level. Runs at the start, before any is collected.
        private static int CountCrystals()
        {
            int count = 0;

            foreach (
                Collectible collectible in FindObjectsByType<Collectible>(FindObjectsSortMode.None)
            )
            {
                if (collectible.Type == CollectibleType.Crystal)
                {
                    count++;
                }
            }

            return count;
        }

        private void CollectCrystal()
        {
            crystalCollected++;
            ObstacleLevelEvents.RaiseCrystalCountChanged(crystalCollected, crystalTotal);

            if (crystalCollected == crystalTotal) // all Crystals collected
            {
                RevealStar();
            }
        }

        // Spawns the Star and changes the sky, so the Player notices it from anywhere.
        private void RevealStar()
        {
            star.SetActive(true);
            RenderSettings.skybox = starSky;
            DynamicGI.UpdateEnvironment(); // the light the sky gives the level changes with it
            starAppearedEffect.Play(star.transform.position);
        }

        private void PlayTick(float dt)
        {
            if (pauseAction.action.WasPressedThisFrame())
            {
                LevelTransitions.RaisePauseRequested();
            }
        }

        private void OnCollected(Collectible collectible)
        {
            if (collectible.Type == CollectibleType.Star)
            {
                LevelTransitions.RaiseCompleted();
            }
            else if (collectible.Type == CollectibleType.Crystal)
            {
                CollectCrystal();
            }
            else if (collectible.Type == CollectibleType.Crystal)
            {
                CollectCrystal();
            }
        }

        // ---------------- Pause ----------------

        private void OnPauseEnter()
        {
            Time.timeScale = 0f;
            AudioListener.pause = true; // sounds that are playing wait too
            player.Input.SetEnabled(false);
        }

        private void OnPauseExit()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            player.Input.SetEnabled(true);
        }

        private void PauseTick(float dt) // resume the world
        {
            if (pauseAction.action.WasPressedThisFrame())
            {
                LevelTransitions.RaiseContinueRequested();
            }
        }

        // ---------------- Complete ----------------

        private void OnCompleteEnter()
        {
            player.Input.SetEnabled(false);
        }
    }
}
