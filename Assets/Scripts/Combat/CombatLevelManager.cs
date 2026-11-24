using PlayerSystem;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Combat level flow (template, see Docs/features.md "Level Manager" and "Combat level").
///
/// States (all event-driven through Links):
///   Start    --StartFinished-->     Play      init level, spawn Player at center
///   Play     --PauseRequested-->    Pause     ESC
///   Play     --Failed-->            Fail      Player died
///   Play     --Completed-->         Complete  Star collected
///   Pause    --ContinueRequested--> Play      ESC or Continue Button
///   Pause    --ExitRequested-->     Exit      Back To Menu Button
///   Fail     --RetryRequested-->    Play      Space or Retry Button
///   Fail     --ExitRequested-->     Exit      Back To Menu Button
///   Complete --ExitRequested-->     Exit      Space or Back To Menu Button
/// Pause / Fail Save Button: LevelEvents.SaveRequested (no state change).
///
/// Waves (run in Play): read from the WaveSet (Waves File); EnemySpawner places the enemies.
/// One Wave at a time. When it is cleared (nothing alive, no group waiting), wait waveBreak sec, then start the next.
/// After the last Wave is cleared, the Star spawns.
/// Retry: clear Enemies, spawn Player at center, restart the failed Wave after firstWaveDelay sec.
/// Collectible Spots clear themselves on PlayerEvents.Spawned (level start and retry).
/// </summary>
public class CombatLevelManager : MonoBehaviour, ILevelManager
{
    [Header("References")]
    [SerializeField]
    private Player player;

    [SerializeField]
    private Transform playerSpawnPoint;

    [SerializeField]
    private EnemySpawner enemySpawner;

    [SerializeField]
    private Collectible starPrefab;

    [SerializeField]
    private Transform starSpawnPoint; // usually the center

    [SerializeField]
    private LevelUIManager levelUI; // Pause / Fail / Complete screens

    [Header("Input")]
    [SerializeField]
    private InputActionReference pauseAction; // Level/Pause (Escape)

    [SerializeField]
    private InputActionReference confirmAction; // Level/Confirm (Space)

    [Header("Waves")]
    [SerializeField]
    private WaveSet waveSet; // the Waves File

    [SerializeField]
    private float firstWaveDelay = 3f; // seconds after level start / retry before the Wave starts

    [SerializeField]
    private float waveBreak = 5f; // seconds between a cleared Wave and the next one

    [Header("Debug")]
    [SerializeField]
    private string currentStateName;

    [SerializeField]
    private int debugWave; // 1-based, for display

    [SerializeField]
    private int debugEnemies;

    [SerializeField]
    private int testCheckpoint; // start Wave (0-based), only used when this scene is played directly

    private StateMachine stateMachine;
    private TickState startState,
        playState,
        pauseState,
        failState,
        completeState,
        exitState;

    // ---- Wave progress ----
    private int startWave; // 0-based; set by StartLevel from the checkpoint number
    private int currentWave; // 0-based; only goes up when a Wave is cleared
    private bool waveActive;
    private bool allWavesCleared;
    private float waveTimer;

    private void Awake()
    {
        if (levelUI == null)
            throw new System.NullReferenceException("Combat Level Manager has no Level UI Manager");

        BuildStateMachine();
        SequenceManager.RegisterLevelManager(this);
    }

    private void OnEnable()
    {
        SetActionEnabled(pauseAction, true);
        SetActionEnabled(confirmAction, true);
    }

    private void OnDisable()
    {
        SetActionEnabled(pauseAction, false);
        SetActionEnabled(confirmAction, false);
    }

    private void Start()
    {
        // Normally SequenceManager calls StartLevel. Without it (scene played directly
        // in the editor) start on our own so the level can still be tested alone.
        if (!SequenceManager.IsRunning)
            StartLevel(testCheckpoint);
    }

    // Called by SequenceManager once the scene finished loading (spec: Start State).
    // checkpoint = Wave to start at (0-based), 0 = from the beginning.
    public void StartLevel(int checkpoint)
    {
        startWave = ClampWave(checkpoint);
        stateMachine.EnterState(startState);
    }

    private int ClampWave(int wave)
    {
        int lastWave = Mathf.Max(TotalWaves - 1, 0);
        if (wave >= 0 && wave <= lastWave)
            return wave;

        Debug.LogWarning(
            $"{nameof(CombatLevelManager)}: checkpoint {wave} is not a Wave (0..{lastWave}), clamped"
        );
        return Mathf.Clamp(wave, 0, lastWave);
    }

    private void Update()
    {
        stateMachine.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        SequenceManager.UnregisterLevelManager(this);
        stateMachine?.ExitState(); // runs exit actions: unsubscribes events, restores time scale and input
        Time.timeScale = 1f;
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

    // ---------------- Start ----------------

    private void OnStartEnter()
    {
        currentWave = startWave;
        waveActive = false;
        allWavesCleared = false;
        waveTimer = firstWaveDelay;
        SpawnPlayer();
    }

    // ---------------- Play ----------------

    private void OnPlayEnter()
    {
        // Runs again when continuing from Pause / retrying from Fail, so only subscribe here; no resets.
        PlayerEvents.Died += OnPlayerDied;
        LevelEvents.Collected += OnCollected;
    }

    private void OnPlayExit()
    {
        PlayerEvents.Died -= OnPlayerDied;
        LevelEvents.Collected -= OnCollected;
    }

    private void PlayTick(float dt)
    {
        if (PausePressed())
        {
            LevelTransitions.RaisePauseRequested();
            return;
        }

        UpdateWaves(dt);
        debugWave = currentWave + 1;
        debugEnemies = enemySpawner != null ? enemySpawner.AliveCount : 0;
    }

    // The Fail Screen freezes the level at once; a death effect with Play While Paused keeps playing over it.
    private void OnPlayerDied() => LevelTransitions.RaiseFailed();

    private void OnCollected(Collectible collectible)
    {
        if (collectible.Type == CollectibleType.Star)
            LevelTransitions.RaiseCompleted();
    }

    // ---------------- Pause ----------------

    private void OnPauseEnter()
    {
        Time.timeScale = 0f;
        SetPlayerInputEnabled(false);
        LevelEvents.SaveRequested += OnSaveRequested; // saving is allowed in Pause and Fail only
        levelUI.ShowPause();
    }

    private void OnPauseExit()
    {
        LevelEvents.SaveRequested -= OnSaveRequested;
        Time.timeScale = 1f;
        SetPlayerInputEnabled(true);
        HideScreens();
    }

    private void PauseTick(float dt)
    {
        if (PausePressed())
            LevelTransitions.RaiseContinueRequested();
    }

    // ---------------- Fail ----------------

    private void OnFailEnter()
    {
        Time.timeScale = 0f;
        SetPlayerInputEnabled(false);

        // Only listen while in Fail, so a stray Retry can't respawn mid-fight.
        // The Fail -> Play Link subscribed first, so this runs after Fail exit restored time and input.
        LevelTransitions.RetryRequested += OnRetryRequested;
        LevelEvents.SaveRequested += OnSaveRequested;
        levelUI.ShowFail();
        Debug.Log("Combat level failed");
    }

    private void OnFailExit()
    {
        LevelTransitions.RetryRequested -= OnRetryRequested;
        LevelEvents.SaveRequested -= OnSaveRequested;
        Time.timeScale = 1f;
        SetPlayerInputEnabled(true);
        HideScreens();
    }

    private void FailTick(float dt)
    {
        // Fail Screen: Space retries.
        if (WasPressed(confirmAction))
            LevelTransitions.RaiseRetryRequested();
    }

    private void OnRetryRequested() => RestartCurrentWave();

    // ---------------- Complete ----------------

    private void OnCompleteEnter()
    {
        // Freeze the world (enemies, collectible spots, fireballs) behind the Level Complete Screen.
        Time.timeScale = 0f;
        SetPlayerInputEnabled(false);
        levelUI.ShowComplete();
        Debug.Log("Combat level complete");
    }

    private void OnCompleteExit()
    {
        Time.timeScale = 1f;
        SetPlayerInputEnabled(true);
        HideScreens();
    }

    private void CompleteTick(float dt)
    {
        // Spec: press Enter to go back to Main Menu.
        if (WasPressed(confirmAction))
            LevelTransitions.RaiseExitRequested();
    }

    // ---------------- Save ----------------

    // Save Button on Pause / Fail Screen. Checkpoint = current Wave (the failed one when in Fail).
    private void OnSaveRequested()
    {
        // TODO: overwrite the Save File with checkpoint currentWave for the Combat level.
        Debug.Log($"Save combat level at checkpoint {currentWave}");
    }

    // ---------------- Exit ----------------

    private void OnExitEnter()
    {
        Time.timeScale = 1f;
        Debug.Log("Exit combat level");

        // SequenceManager unloads this scene and goes to Main Menu.
        LevelEvents.RaiseExited();
    }

    // ---------------- Waves ----------------

    private void UpdateWaves(float dt)
    {
        if (allWavesCleared)
            return;

        if (waveActive)
        {
            if (enemySpawner != null && !enemySpawner.IsWaveDone)
                return;

            // Wave cleared.
            waveActive = false;
            currentWave++;
            if (currentWave >= TotalWaves)
            {
                allWavesCleared = true;
                SpawnStar();
                return;
            }
            waveTimer = waveBreak;
            return;
        }

        waveTimer -= dt;
        if (waveTimer <= 0f)
            StartWave(currentWave);
    }

    private int TotalWaves => waveSet != null ? waveSet.Waves.Count : 0;

    private void StartWave(int index)
    {
        if (index >= TotalWaves)
        {
            // No Waves File (or it is empty): nothing to fight.
            Debug.LogWarning(
                $"{nameof(CombatLevelManager)}: no Wave {index + 1} in the Waves File"
            );
            allWavesCleared = true;
            SpawnStar();
            return;
        }

        waveActive = true;
        Debug.Log($"Wave {index + 1}/{TotalWaves} start");
        if (enemySpawner != null)
            enemySpawner.SpawnWave(waveSet.Waves[index]);
    }

    private void SpawnStar()
    {
        Debug.Log("All waves cleared");
        if (starPrefab == null)
            return;

        Transform point = starSpawnPoint != null ? starSpawnPoint : playerSpawnPoint;
        Vector3 position = point != null ? point.position : transform.position;
        Instantiate(starPrefab, position, Quaternion.identity);
    }

    // ---------------- Helpers ----------------

    private void RestartCurrentWave()
    {
        if (enemySpawner != null)
            enemySpawner.ClearAll();

        // Player.Spawn raises PlayerEvents.Spawned: every CollectibleSpot removes its item and
        // restarts its timer, and every fireball (Arrow) in flight is removed.
        SpawnPlayer();

        // Same currentWave again (the failed one); it only goes up when a Wave is cleared.
        if (!allWavesCleared)
        {
            waveActive = false;
            waveTimer = firstWaveDelay;
        }
    }

    private void SpawnPlayer()
    {
        if (player == null || playerSpawnPoint == null)
        {
            Debug.LogWarning($"{nameof(CombatLevelManager)}: Player or Player Spawn Point not set");
            return;
        }
        player.Spawn(playerSpawnPoint.position, playerSpawnPoint.rotation);
    }

    private bool PausePressed() => WasPressed(pauseAction);

    private static bool WasPressed(InputActionReference reference) =>
        reference != null && reference.action.WasPressedThisFrame();

    private static void SetActionEnabled(InputActionReference reference, bool enabled)
    {
        if (reference == null)
            return;
        if (enabled)
            reference.action.Enable();
        else
            reference.action.Disable();
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        if (player != null)
            player.Input.SetEnabled(enabled);
    }

    // State exits also run from OnDestroy, where the UI may already be destroyed (scene unload).
    private void HideScreens()
    {
        if (levelUI != null)
            levelUI.HideAll();
    }
}
