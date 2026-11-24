using EffectSystem;
using LevelSystem;
using PlayerSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ObstacleLevel
{
    /// <summary>
    /// Obstacle level flow. States (event-driven through Links):
    ///   Start    --StartFinished-->     Play      spawn the Player at the start Checkpoint
    ///   Play     --PauseRequested-->    Pause     ESC
    ///   Play     --Failed-->            Fail      Player died
    ///   Play     --Completed-->         Complete  Star collected
    ///   Pause    --ContinueRequested--> Play      ESC or Continue Button
    ///   Pause    --ExitRequested-->     Exit      Back To Menu Button
    ///   Fail     --RetryRequested-->    Play      Space or Retry Button: respawn at the active Checkpoint
    ///   Fail     --ExitRequested-->     Exit      Back To Menu Button
    ///   Complete --ExitRequested-->     Exit      Space or Back To Menu Button
    /// Pause / Fail Save Button: LevelEvents.SaveRequested (no state change).
    /// Pause, Fail and Complete show their screen (LevelUIManager), freeze time and sound, and turn the
    /// Player's input off, which also shows the cursor (PlayerInputReader -> CursorControl).
    /// Exit raises LevelEvents.Exited: SequenceManager goes back to the Main Menu.
    /// Owns the Crystal count: the Star stays hidden until every Crystal is collected.
    /// When it appears, the sky changes to starSky, so the Player knows from anywhere in the level.
    /// Respawning is owned by SpawnManager.
    /// Where the Player starts:
    ///  - Launched from the Menu: SequenceManager calls StartLevel(x) (ILevelManager): checkpoints[x], 0 = level start.
    ///  - Scene played directly in the editor: testCheckpoint (empty = checkpoints[0]).
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
        private InputActionReference confirmAction; // Level/Confirm (Space)

        [SerializeField]
        private LevelUIManager levelUI; // Pause / Fail / Complete screens

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
        private Checkpoint testCheckpoint;

        private Checkpoint spawnCheckpoint; // where this run starts
        private int activeCheckpoint; // checkpoints index of the active Checkpoint, -1 = not in the list

        private int crystalTotal;
        private int crystalCollected;

        private StateMachine stateMachine;
        private TickState startState,
            playState,
            pauseState,
            failState,
            completeState,
            exitState;

        private void Awake()
        {
            if (levelUI == null)
                throw new System.NullReferenceException("Obstacle Level Manager has no Level UI Manager");

            if (confirmAction == null)
                throw new System.NullReferenceException("Obstacle Level Manager has no Confirm Action");

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
            confirmAction.action.Enable();
            LevelEvents.CheckpointActivated += OnCheckpointActivated;
        }

        private void OnDisable()
        {
            pauseAction.action.Disable();
            confirmAction.action.Disable();
            LevelEvents.CheckpointActivated -= OnCheckpointActivated;
        }

        private void OnCheckpointActivated(Checkpoint checkpoint) =>
            activeCheckpoint = System.Array.IndexOf(checkpoints, checkpoint);

        // From the Menu, SequenceManager calls StartLevel once the scene has loaded.
        // Played directly in the editor there is no SequenceManager, so start at testCheckpoint.
        // Start, not Awake: Player and Checkpoints need their Awake first.
        private void Start()
        {
            if (!SequenceManager.IsRunning)
                StartAt(testCheckpoint != null ? testCheckpoint : GetCheckpoint(0));
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
            failState = new TickState("Fail", OnFailEnter, OnFailExit, FailTick);
            completeState = new TickState("Complete", OnCompleteEnter, OnCompleteExit, CompleteTick);
            exitState = new TickState("Exit", OnExitEnter);

            startState.AddLink(new Link(playState, LevelTransitions.StartFinishedWrapper));

            playState.AddLink(new Link(pauseState, LevelTransitions.PauseRequestedWrapper));
            playState.AddLink(new Link(failState, LevelTransitions.FailedWrapper));
            playState.AddLink(new Link(completeState, LevelTransitions.CompletedWrapper));

            pauseState.AddLink(new Link(playState, LevelTransitions.ContinueRequestedWrapper));
            pauseState.AddLink(new Link(exitState, LevelTransitions.ExitRequestedWrapper));

            failState.AddLink(new Link(playState, LevelTransitions.RetryRequestedWrapper));
            failState.AddLink(new Link(exitState, LevelTransitions.ExitRequestedWrapper));

            completeState.AddLink(new Link(exitState, LevelTransitions.ExitRequestedWrapper));

            stateMachine = new StateMachine(state => currentStateName = state.Name);
        }

        // ------------- StartLevel -------------

        // Called by SequenceManager once the scene has loaded (spec: Start State). checkpoint = checkpoints index.
        public void StartLevel(int checkpoint) => StartAt(GetCheckpoint(checkpoint));

        private void StartAt(Checkpoint checkpoint)
        {
            spawnCheckpoint = checkpoint;
            activeCheckpoint = System.Array.IndexOf(checkpoints, checkpoint);
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
            HideScreens();
            spawnManager.SetActiveCheckpoint(spawnCheckpoint);
            spawnManager.SpawnPlayer();

            crystalTotal = CountCrystals();
            crystalCollected = 0;
            star.SetActive(false);
            ObstacleLevelEvents.RaiseCrystalCountChanged(crystalCollected, crystalTotal);
        }

        // ---------------- Play ----------------

        // Runs again when continuing from Pause / retrying from Fail, so it only subscribes.
        private void OnPlayEnter()
        {
            LevelEvents.Collected += OnCollected;
            PlayerEvents.Died += OnPlayerDied;
        }

        private void OnPlayExit()
        {
            LevelEvents.Collected -= OnCollected;
            PlayerEvents.Died -= OnPlayerDied;
        }

        // The Fail Screen freezes the level at once; the death effect keeps playing over it
        // (its Effect has Play While Paused on, in LevelEffects).
        private void OnPlayerDied() => LevelTransitions.RaiseFailed();

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
            Freeze(true);
            LevelEvents.SaveRequested += OnSaveRequested; // saving is allowed in Pause and Fail only
            levelUI.ShowPause();
        }

        private void OnPauseExit()
        {
            LevelEvents.SaveRequested -= OnSaveRequested;
            Freeze(false);
            HideScreens();
        }

        private void PauseTick(float dt) // resume the world
        {
            if (pauseAction.action.WasPressedThisFrame())
            {
                LevelTransitions.RaiseContinueRequested();
            }
        }

        // ---------------- Fail ----------------

        private void OnFailEnter()
        {
            Freeze(true);

            // Only listen while in Fail, so a stray Retry can't respawn mid-run.
            // The Fail -> Play Link subscribed first, so this runs after Fail exit restored time and input.
            LevelTransitions.RetryRequested += OnRetryRequested;
            LevelEvents.SaveRequested += OnSaveRequested;
            levelUI.ShowFail();
        }

        private void OnFailExit()
        {
            LevelTransitions.RetryRequested -= OnRetryRequested;
            LevelEvents.SaveRequested -= OnSaveRequested;
            Freeze(false);
            HideScreens();
        }

        private void FailTick(float dt)
        {
            // Fail Screen: Space retries.
            if (confirmAction.action.WasPressedThisFrame())
                LevelTransitions.RaiseRetryRequested();
        }

        // Respawn at the active Checkpoint (the last one the Player touched).
        private void OnRetryRequested() => spawnManager.SpawnPlayer();

        // ---------------- Complete ----------------

        private void OnCompleteEnter()
        {
            Freeze(true);
            levelUI.ShowComplete();
        }

        private void OnCompleteExit()
        {
            Freeze(false);
            HideScreens();
        }

        private void CompleteTick(float dt)
        {
            // Complete Screen: Space goes back to the Main Menu.
            if (confirmAction.action.WasPressedThisFrame())
                LevelTransitions.RaiseExitRequested();
        }

        // ---------------- Save ----------------

        // Save Button on Pause / Fail Screen. Checkpoint = the active Checkpoint's number in checkpoints.
        private void OnSaveRequested()
        {
            // TODO: overwrite the Save File with checkpoint activeCheckpoint for the Obstacle level.
            Debug.Log($"Save obstacle level at checkpoint {activeCheckpoint}");
        }

        // ---------------- Exit ----------------

        private void OnExitEnter()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Debug.Log("Exit obstacle level");

            // SequenceManager unloads this scene and goes to the Main Menu.
            LevelEvents.RaiseExited();
        }

        // ---------------- Helpers ----------------

        // Pause, Fail and Complete stop the world behind their screen. Player input off also shows the
        // cursor, input on locks it again (PlayerInputReader -> CursorControl).
        private void Freeze(bool frozen)
        {
            Time.timeScale = frozen ? 0f : 1f;
            AudioListener.pause = frozen; // sounds that are playing wait too
            player.Input.SetEnabled(!frozen);
        }

        // State exits also run from OnDestroy, where the UI may already be destroyed (scene unload).
        private void HideScreens()
        {
            if (levelUI != null)
                levelUI.HideAll();
        }
    }
}
