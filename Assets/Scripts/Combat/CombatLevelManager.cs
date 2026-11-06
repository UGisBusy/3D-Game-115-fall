using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Combat level flow (template, see Docs/features.md "Level Manager" and "Combat level").
///
/// States (all event-driven through Links):
///   Start    --StartFinished-->   Play      init level, spawn Player at center
///   Play     --PauseRequested-->  Pause     ESC
///   Pause    --ResumeRequested--> Play      ESC or Resume button
///   Play     --Completed-->       Complete  Star collected
///   Pause    --ExitRequested-->   Exit      Pause Menu exit buttons
///   Complete --ExitRequested-->   Exit      Enter on Level Complete Screen
///
/// Waves (run in Play): one Wave at a time. When it is cleared, wait waveBreak sec, then start the next.
/// After the last Wave is cleared, the Star spawns.
/// Respawn: clear Enemies, spawn Player at center, restart the current Wave after respawnWaveDelay sec.
/// Collectible Spots clear themselves on PlayerEvents.Spawned.
///
/// Setup:
///  - turn off "Spawn On Start" on the Player; this manager spawns it.
///  - assign Pause / Confirm from the "Level" action map of PlayerControls.
///    Pause turns off Player input (move, attack, camera) until resumed.
/// </summary>
public class CombatLevelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Player player;

    [SerializeField]
    private Transform playerSpawnPoint;

    [SerializeField]
    private Transform[] enemySpawnAreas;

    [SerializeField]
    private Collectible starPrefab;

    [SerializeField]
    private Transform starSpawnPoint; // usually the center

    [Header("Input")]
    [SerializeField]
    private InputActionReference pauseAction; // Level/Pause (Escape)

    [SerializeField]
    private InputActionReference confirmAction; // Level/Confirm (Enter)

    [Header("Waves")]
    [SerializeField]
    private int totalWaves = 3; // TODO: read from Waves File

    [SerializeField]
    private float firstWaveDelay = 3f;

    [SerializeField]
    private float waveBreak = 5f; // seconds between a cleared Wave and the next one

    [SerializeField]
    private float respawnWaveDelay = 3f; // seconds after respawn before the Wave restarts

    [Header("Debug")]
    [SerializeField]
    private string currentStateName;

    [SerializeField]
    private int debugWave; // 1-based, for display

    [SerializeField]
    private int debugEnemies;

    private readonly LevelTransitions transitions = new();
    private StateMachine stateMachine;
    private TickState startState,
        playState,
        pauseState,
        completeState,
        exitState;

    // ---- Wave progress ----
    private readonly List<GameObject> enemies = new();
    private int currentWave; // 0-based; only goes up when a Wave is cleared
    private bool waveActive;
    private bool allWavesCleared;
    private float waveTimer;

    private bool saveOnExit;

    private void Awake()
    {
        BuildStateMachine();
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
        stateMachine.EnterState(startState);
    }

    private void Update()
    {
        stateMachine.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        stateMachine?.ExitState(); // runs exit actions: unsubscribes events, restores time scale and input
        Time.timeScale = 1f;
    }

    private void BuildStateMachine()
    {
        startState = new TickState("Start", OnStartEnter, tick: _ => transitions.RaiseStartFinished());
        playState = new TickState("Play", OnPlayEnter, OnPlayExit, PlayTick);
        pauseState = new TickState("Pause", OnPauseEnter, OnPauseExit, PauseTick);
        completeState = new TickState("Complete", OnCompleteEnter, tick: CompleteTick);
        exitState = new TickState("Exit", OnExitEnter);

        startState.AddLink(new Link(playState, transitions.StartFinishedWrapper));
        playState.AddLink(new Link(pauseState, transitions.PauseRequestedWrapper));
        playState.AddLink(new Link(completeState, transitions.CompletedWrapper));
        pauseState.AddLink(new Link(playState, transitions.ResumeRequestedWrapper));
        pauseState.AddLink(new Link(exitState, transitions.ExitRequestedWrapper));
        completeState.AddLink(new Link(exitState, transitions.ExitRequestedWrapper));

        stateMachine = new StateMachine(state => currentStateName = state.Name);
    }

    // ---------------- Public API (for UI buttons) ----------------

    // Pause Menu: Resume Button
    public void Resume() => transitions.RaiseResumeRequested();

    // Pause Menu / Level Complete Screen: exit buttons
    public void ExitLevel(bool save)
    {
        saveOnExit = save;
        transitions.RaiseExitRequested();
    }

    // ---------------- Start ----------------

    private void OnStartEnter()
    {
        // TODO: SequenceManager should init the Level Manager (spec: Start State).
        currentWave = 0;
        waveActive = false;
        allWavesCleared = false;
        waveTimer = firstWaveDelay;
        SpawnPlayer();
    }

    // ---------------- Play ----------------

    private void OnPlayEnter()
    {
        // Runs again when resuming from Pause, so only subscribe here; no resets.
        PlayerEvents.RespawnRequested += OnRespawnRequested;
        LevelEvents.Collected += OnCollected;
    }

    private void OnPlayExit()
    {
        PlayerEvents.RespawnRequested -= OnRespawnRequested;
        LevelEvents.Collected -= OnCollected;
    }

    private void PlayTick(float dt)
    {
        if (PausePressed())
        {
            transitions.RaisePauseRequested();
            return;
        }

        UpdateWaves(dt);
        debugWave = currentWave + 1;
        debugEnemies = enemies.Count;
    }

    private void OnRespawnRequested()
    {
        ClearEnemies();
        SpawnPlayer();

        // Same currentWave again; it only goes up when a Wave is cleared.
        if (!allWavesCleared)
        {
            waveActive = false;
            waveTimer = respawnWaveDelay;
        }
    }

    private void OnCollected(Collectible collectible)
    {
        if (collectible.Type == CollectibleType.Star)
            transitions.RaiseCompleted();
    }

    // ---------------- Pause ----------------

    private void OnPauseEnter()
    {
        Time.timeScale = 0f;
        SetPlayerInputEnabled(false);
        // TODO: show Pause Menu.
    }

    private void OnPauseExit()
    {
        Time.timeScale = 1f;
        SetPlayerInputEnabled(true);
        // TODO: hide Pause Menu.
    }

    private void PauseTick(float dt)
    {
        if (PausePressed())
            transitions.RaiseResumeRequested();
    }

    // ---------------- Complete ----------------

    private void OnCompleteEnter()
    {
        Debug.Log("Combat level complete");
        // TODO: show Level Complete Screen.
    }

    private void CompleteTick(float dt)
    {
        // Spec: press Enter to go back to Level Select.
        if (WasPressed(confirmAction))
            ExitLevel(save: false);
    }

    // ---------------- Exit ----------------

    private void OnExitEnter()
    {
        Time.timeScale = 1f;
        if (saveOnExit)
        {
            // TODO: write Save File.
        }
        // TODO: cleanup, tell SequenceManager to go to Level Select.
        Debug.Log($"Exit combat level (save: {saveOnExit})");
    }

    // ---------------- Waves ----------------

    private void UpdateWaves(float dt)
    {
        if (allWavesCleared)
            return;

        if (waveActive)
        {
            if (CountEnemies() > 0)
                return;

            // Wave cleared.
            waveActive = false;
            currentWave++;
            if (currentWave >= totalWaves)
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

    private void StartWave(int index)
    {
        waveActive = true;
        Debug.Log($"Wave {index + 1}/{totalWaves} start");

        // TODO: read Wave Info (enemy groups: spawn area, formation, type) from the Waves File,
        // spawn each enemy in enemySpawnAreas, and add it to the list:
        //   enemies.Add(Instantiate(enemyPrefab, position, rotation).gameObject);
        // With no enemies spawned, the Wave counts as cleared on the next frame.
    }

    // Alive enemies; destroyed (killed) ones are removed from the list.
    private int CountEnemies()
    {
        enemies.RemoveAll(e => e == null);
        return enemies.Count;
    }

    private void ClearEnemies()
    {
        foreach (GameObject e in enemies)
            if (e != null)
                Destroy(e);
        enemies.Clear();
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
}
