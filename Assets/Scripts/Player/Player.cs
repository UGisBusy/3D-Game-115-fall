using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Facade of the Player. Owns HP, invincibility, spawn/reset, the state machine, and wires the pieces together.
    /// Other systems talk to the Player only through this class (TakeHit, Heal, Kill, Spawn, Events).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMotor), typeof(PlayerInputReader))]
    [RequireComponent(typeof(DashAbility))]
    public class Player : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField]
        private int maxHp = 3;

        [SerializeField]
        private float hurtInvincibleTime = 1f;

        [SerializeField]
        private float spawnInvincibleTime = 2f;

        [Header("Debug")]
        [Tooltip(
            "Spawn at the current position on Start. Turn off when a LevelManager spawns the Player."
        )]
        [SerializeField]
        private bool spawnOnStart = true;

        // --- Components ---
        public PlayerMotor Motor { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public DashAbility Dash { get; private set; }
        public BowAbility Bow { get; private set; } // optional, only in combat level
        public PlayerEvents Events { get; } = new();

        // --- States ---
        public NormalState Normal { get; private set; }
        public DashState Dashing { get; private set; }
        public KnockbackState Knockback { get; private set; }
        public DeadState Dead { get; private set; }
        public PlayerState CurrentState => stateMachine.CurrentState as PlayerState;

        private StateMachine stateMachine;

        // --- Health state ---
        public int Hp { get; private set; }
        public int MaxHp => maxHp;
        public bool IsDead => CurrentState == Dead;
        public bool IsInvincible => invincibleTimer > 0f || CurrentState == Dashing;

        private float invincibleTimer;

        private void Awake()
        {
            Motor = GetComponent<PlayerMotor>();
            Input = GetComponent<PlayerInputReader>();
            Dash = GetComponent<DashAbility>();
            Bow = GetComponent<BowAbility>();
            BuildStateMachine();
        }

        private void Start()
        {
            if (spawnOnStart)
                Spawn(transform.position, transform.rotation);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (invincibleTimer > 0f)
                invincibleTimer -= dt;

            PlayerState state = CurrentState;
            state?.Tick(dt);
            if (Bow != null)
                Bow.Tick(dt, state != null && state.AllowsAttack);
        }

        /// <summary>
        /// Transitions (all event-driven through Links):
        ///   Normal    --DashRequested-->     Dashing
        ///   Normal    --KnockedBack-->       Knockback
        ///   Dashing   --DashFinished-->      Normal
        ///   Knockback --KnockedBack-->       Knockback (re-hit restarts it)
        ///   Knockback --KnockbackFinished--> Normal
        ///   any alive --Died-->              Dead
        ///   Dead      leaves only through Spawn()
        /// </summary>
        private void BuildStateMachine()
        {
            Normal = new NormalState(this);
            Dashing = new DashState(this);
            Knockback = new KnockbackState(this);
            Dead = new DeadState(this);

            Normal.AddLink(new Link(Dashing, Events.DashRequestedWrapper));
            Normal.AddLink(new Link(Knockback, Events.KnockedBackWrapper));
            Normal.AddLink(new Link(Dead, Events.DiedWrapper));

            Dashing.AddLink(new Link(Normal, Events.DashFinishedWrapper));
            Dashing.AddLink(new Link(Dead, Events.DiedWrapper));

            Knockback.AddLink(new Link(Knockback, Events.KnockedBackWrapper));
            Knockback.AddLink(new Link(Normal, Events.KnockbackFinishedWrapper));
            Knockback.AddLink(new Link(Dead, Events.DiedWrapper));

            stateMachine = new StateMachine();
        }

        // ---------------- Public API ----------------

        /// <summary>Called by LevelManager (or on Start for testing). Resets everything and places the Player.</summary>
        public void Spawn(Vector3 position, Quaternion rotation)
        {
            stateMachine.EnterState(Normal); // exits the old state first (e.g. ends a dash)

            Hp = maxHp;
            Events.RaiseHpChanged(Hp, maxHp);

            Motor.ResetMotor();
            Motor.Teleport(position, rotation);
            Dash.ResetCooldown();
            if (Bow != null)
                Bow.ResetBow();

            invincibleTimer = spawnInvincibleTime;
            Events.RaiseSpawned();
        }

        /// <summary>Damage + optional knockback. Ignored while invincible or dead.</summary>
        public void TakeHit(HitInfo hit)
        {
            if (IsDead || IsInvincible)
                return;

            Hp = Mathf.Max(0, Hp - hit.Damage);
            Events.RaiseHpChanged(Hp, maxHp);
            Events.RaiseHurt(hit);

            if (Hp == 0)
            {
                Kill();
                return;
            }

            invincibleTimer = hurtInvincibleTime;

            if (hit.KnockbackVelocity != Vector3.zero)
            {
                Knockback.Setup(hit.KnockbackVelocity, hit.KnockbackDuration);
                Events.RaiseKnockedBack();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead)
                return;
            Hp = Mathf.Min(maxHp, Hp + amount);
            Events.RaiseHpChanged(Hp, maxHp);
        }

        /// <summary>Instant death (fall off platform, HP reached 0).</summary>
        public void Kill()
        {
            if (IsDead)
                return;
            Hp = 0;
            Events.RaiseHpChanged(Hp, maxHp);
            Events.RaiseDied(); // Link moves the state machine to Dead
        }
    }
}
