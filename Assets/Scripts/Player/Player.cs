using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Facade of the Player. Owns HP, invincibility, spawn/reset, the state machine, and wires the pieces together.
    /// Other systems talk to the Player only through this class (TakeHit, Heal, Kill, Spawn); gameplay events go through the static PlayerEvents.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMotor), typeof(PlayerInputReader))]
    [RequireComponent(typeof(DashAbility))]
    public class Player : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField]
        private int maxHp = 3;

        [SerializeField]
        private float hurtInvincibleTime = 1f;

        [SerializeField]
        private float spawnInvincibleTime = 2f;

        [Header("Debug")]
        // "Spawn at the current position on Start. Turn off when a LevelManager spawns the Player."
        [SerializeField]
        private bool spawnOnStart = true;

        [SerializeField]
        private string currentStateName;

        // --- Components ---
        public PlayerMotor Motor { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public DashAbility Dash { get; private set; }
        public BowAbility Bow { get; private set; } // optional, only in combat level
        public PlayerTransitions Transitions { get; } = new();

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

        private void OnEnable() => LevelEvents.Collected += OnCollected;

        private void OnDisable() => LevelEvents.Collected -= OnCollected;

        private void OnCollected(Collectible collectible)
        {
            if (collectible.Type == CollectibleType.Heart)
                Heal(collectible.Amount);
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

            stateMachine.Tick(dt);
            if (Bow != null)
            {
                PlayerState state = CurrentState;
                Bow.Tick(dt, state != null && state.AllowsAttack);
            }
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

            Normal.AddLink(new Link(Dashing, Transitions.DashRequestedWrapper));
            Normal.AddLink(new Link(Knockback, Transitions.KnockedBackWrapper));
            Normal.AddLink(new Link(Dead, Transitions.DiedWrapper));

            Dashing.AddLink(new Link(Normal, Transitions.DashFinishedWrapper));
            Dashing.AddLink(new Link(Dead, Transitions.DiedWrapper));

            Knockback.AddLink(new Link(Knockback, Transitions.KnockedBackWrapper));
            Knockback.AddLink(new Link(Normal, Transitions.KnockbackFinishedWrapper));
            Knockback.AddLink(new Link(Dead, Transitions.DiedWrapper));

            stateMachine = new StateMachine(state => currentStateName = state.Name);
        }

        // ---------------- Public API ----------------

        // Called by LevelManager (or on Start for testing). Resets everything and places the Player.
        public void Spawn(Vector3 position, Quaternion rotation)
        {
            stateMachine.EnterState(Normal);

            Hp = maxHp;
            PlayerEvents.RaiseHpChanged(Hp, maxHp);

            Motor.ResetMotor();
            Motor.Teleport(position, rotation);
            Dash.ResetCooldown();
            if (Bow != null)
                Bow.ResetBow();

            invincibleTimer = spawnInvincibleTime;
            PlayerEvents.RaiseSpawned();
        }

        // Damage + optional knockback
        public void TakeHit(HitInfo hit)
        {
            if (IsDead || (!hit.IsForced && IsInvincible))
                return;

            Hp = Mathf.Max(0, Hp - hit.Damage);
            PlayerEvents.RaiseHpChanged(Hp, maxHp);
            PlayerEvents.RaiseHurt(hit);

            if (Hp == 0)
            {
                Kill();
                return;
            }

            invincibleTimer = hurtInvincibleTime;

            if (hit.KnockbackVelocity != Vector3.zero)
            {
                Knockback.Setup(hit.KnockbackVelocity, hit.KnockbackDuration);
                Transitions.RaiseKnockedBack();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead)
                return;
            Hp = Mathf.Min(maxHp, Hp + amount);
            PlayerEvents.RaiseHpChanged(Hp, maxHp);
        }

        // Instant death
        public void Kill()
        {
            if (IsDead)
                return;
            Hp = 0;
            PlayerEvents.RaiseHpChanged(Hp, maxHp);
            Transitions.RaiseDied();
            PlayerEvents.RaiseDied();
        }
    }
}
