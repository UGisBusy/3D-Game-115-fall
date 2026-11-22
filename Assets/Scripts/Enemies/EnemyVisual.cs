using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the enemy model's animations from what the enemy is doing. Lives on the enemy root, next to Enemy.
/// Enemy scripts only report events (hit, attack, windup...); this picks the Animator state.
///
/// Priority, highest first:
///   1. Held state  (SetHeld): windup (Charge windup, Heavy slam windup), action (Charge charging, Jump in the air),
///                  or stun (Shield enemy after its shield breaks)
///   2. One-shot    (PlayHit / PlayAttack / PlaySpecial): plays its clip once, then goes back
///   3. Locomotion  move (or run, after SetRunning(true)) / idle, picked by the Rigidbody's horizontal speed
///
/// State names are Animator state names. Tools > 3DG > Setup Enemy Visuals builds the controllers
/// (states named after their clips) and fills these in. Empty = not used, falls through to the next priority.
/// </summary>
[DisallowMultipleComponent]
public class EnemyVisual : MonoBehaviour
{
    public enum Held
    {
        None,
        Windup,
        Action,
        Stun,
    }

    [SerializeField]
    private Animator animator; // the model's Animator, found in children if empty

    [Header("States")]
    [SerializeField]
    private string idleState;

    [SerializeField]
    private string moveState;

    [SerializeField]
    private string attackState;

    [SerializeField]
    private string hitState;

    [SerializeField]
    private string windupState;

    [SerializeField]
    private string actionState;

    [SerializeField]
    private string specialState; // one-shot for a special attack (Heavy slam)

    [SerializeField]
    private string stunState; // held while stunned

    [SerializeField]
    private string runState; // replaces moveState after SetRunning(true)

    [Header("Tuning")]
    [SerializeField]
    private float moveThreshold = 0.3f; // horizontal speed that counts as moving

    [SerializeField]
    private float moveAnimSpeed; // speed the move clip is made for; > 0 scales playback with the real speed

    [SerializeField]
    private float runAnimSpeed; // same for the run clip

    [SerializeField]
    private float crossFade = 0.12f; // blend time between states, seconds

    private readonly Dictionary<string, float> clipLengths = new();
    private Rigidbody rb;
    private string current;
    private string oneShot;
    private float oneShotTimer;
    private Held held;
    private float heldSpeed = 1f; // playback speed of the held state (stretched to a duration)
    private bool running;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                clipLengths[clip.name] = clip.length;
    }

    // ---------------- Events from enemy scripts ----------------

    // Holds a state until set back to None. It starts from the beginning.
    // duration > 0: the clip is stretched (or sped up) to play exactly once over that many seconds.
    public void SetHeld(Held state, float duration = 0f)
    {
        held = state;
        heldSpeed = 1f;

        string name = state switch
        {
            Held.Windup => windupState,
            Held.Action => actionState,
            Held.Stun => stunState,
            _ => null,
        };
        if (string.IsNullOrEmpty(name) || animator == null)
            return;

        if (duration > 0f && clipLengths.TryGetValue(name, out float length))
            heldSpeed = length / duration;
        Play(name, restart: true);
    }

    // Use the run clip instead of the move clip when moving (Shield enemy after losing its shield).
    public void SetRunning(bool value) => running = value;

    public void PlayHit() => PlayOneShot(hitState);

    public void PlayAttack() => PlayOneShot(attackState);

    // Special attack (Heavy slam). Falls back to the normal attack if no special state is set.
    public void PlaySpecial() =>
        PlayOneShot(string.IsNullOrEmpty(specialState) ? attackState : specialState);

    private void PlayOneShot(string state)
    {
        if (string.IsNullOrEmpty(state))
            return;

        oneShot = state;
        oneShotTimer = clipLengths.TryGetValue(state, out float length) ? length : 0.5f;
        if (held == Held.None)
            Play(state, restart: true); // restart even if the same one-shot is already playing
    }

    // ---------------- Picking the state ----------------

    private void Update()
    {
        if (animator == null)
            return;

        if (oneShotTimer > 0f)
            oneShotTimer -= Time.deltaTime;

        float playSpeed = 1f;
        string want;

        if (held == Held.Windup && !string.IsNullOrEmpty(windupState))
        {
            want = windupState;
            playSpeed = heldSpeed;
        }
        else if (held == Held.Action && !string.IsNullOrEmpty(actionState))
        {
            want = actionState;
            playSpeed = heldSpeed;
        }
        else if (held == Held.Stun && !string.IsNullOrEmpty(stunState))
        {
            want = stunState;
            playSpeed = heldSpeed;
        }
        else if (oneShotTimer > 0f)
            want = oneShot;
        else
        {
            bool useRun = running && !string.IsNullOrEmpty(runState);
            string move = useRun ? runState : moveState;
            float animSpeed = useRun ? runAnimSpeed : moveAnimSpeed;

            float speed = FlatSpeed();
            bool moving = speed > moveThreshold && !string.IsNullOrEmpty(move);
            want = moving ? move : idleState;
            if (moving && animSpeed > 0f)
                playSpeed = Mathf.Clamp(speed / animSpeed, 0.5f, 2f);
        }

        animator.speed = playSpeed;
        if (want != current)
            Play(want, restart: false);
    }

    private void Play(string state, bool restart)
    {
        if (string.IsNullOrEmpty(state))
            return;
        if (restart)
            animator.CrossFadeInFixedTime(state, crossFade, 0, 0f);
        else
            animator.CrossFadeInFixedTime(state, crossFade);
        current = state;
    }

    private float FlatSpeed()
    {
        if (rb == null)
            return 0f;
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        return v.magnitude;
    }
}
