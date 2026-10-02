using UnityEngine;
using UnityEngine.SceneManagement;
using KyberKlash.Core;
using KyberKlash.Data;
using KyberKlash.Player.States;
using KyberKlash.Stage;
using KyberKlash.VFX;
using KyberKlash.Combat;
using System.Linq;
using Unity.Cinemachine;

namespace KyberKlash.Player
{
    /// <summary>
    /// Main player controller - orchestrates movement, combat, meter, and state machine.
    /// Designed to be easily extended to NetworkBehaviour for multiplayer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(StateMachine))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerMeter))]
    [RequireComponent(typeof(CombatAnimationEvents))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Character Data")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private FormSO startingForm;

        [Header("Components")]
        [SerializeField] private Transform saberTip;
        [SerializeField] private Transform saberBase;
        [SerializeField] private LayerMask groundLayer = -1;
        [SerializeField] private LayerMask wallLayer = -1;
        [SerializeField] private LayerMask hurtboxLayer = -1;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private float wallCheckDistance = 0.5f;

        [Header("Coyote Time & Jump Buffer")]
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Movement Tuning")]
        [SerializeField] private float groundAcceleration = 20f;
        [SerializeField] private float airAcceleration = 10f;

        [Header("Dash")]
        [SerializeField] private float dashDistance = 6f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.5f;
        [SerializeField] private float airDashCooldown = 1f;

        [Header("Invulnerability")]
        [SerializeField] private float invulnerabilityFlashInterval = 0.1f;

        [Header("Smash Mechanics Tuning")]
        [Tooltip("Directional Influence: how much the victim's hold input rotates the knockback angle (0-1). Smash ~0.33.")]
        [SerializeField] private float diStrength = 0.33f;
        [Tooltip("Max number of recent moves tracked for stale-move negation.")]
        [SerializeField] private int staleMoveHistorySize = 9;
        [Tooltip("Knockback scale floor when a move is fully stale (Smash ~0.72).")]
        [SerializeField] private float staleMoveMinMultiplier = 0.72f;

        // Public properties for state access
        public CharacterData CharacterData => characterData;
        public FormSO CurrentForm => currentForm;
        public PlayerInputHandler InputHandler => inputHandler;
        public PlayerCombat Combat => combat;
        public PlayerMeter Meter => meter;
        public Rigidbody Rigidbody => rb;
        public Animator Animator => animator;
        public CapsuleCollider Collider => collider;
        public Transform SaberTip => saberTip;
        public Transform SaberBase => saberBase;
        public bool IsGrounded => isGrounded;
        public bool IsTouchingWall => isTouchingWall;
        public bool IsDead => isDead;
        public bool IsInHitStun => isInHitStun;
        public bool IsInKnockback => isInKnockback;
        public float LastHitStunDuration => lastHitStunDuration;
        public Vector3 LastKnockbackVelocity => lastKnockbackVelocity;

        // Components
        private PlayerInputHandler inputHandler;
        private PlayerCombat combat;
        private PlayerMeter meter;
        private FormMechanicHandler formMechanicHandler;
        private Rigidbody rb;
        private Animator animator;
        private CapsuleCollider collider;
        private StateMachine stateMachine;

        // State
        private FormSO currentForm;
        private bool isGrounded;
        private bool isTouchingWall;
        private bool isDead;
        private bool isInHitStun;
        private bool isInKnockback;
        private bool isInvulnerable;
        private bool recoverySpecialAvailable = true;
        private float lastHitStunDuration;
        private Vector3 lastKnockbackVelocity;
        private float dashCooldownTimer;
        private float airDashCooldownTimer;
        private int airJumpsUsed;
        private int airDashesUsed;
        private Vector3 spawnPoint;
        private Material[] originalMaterials;
        private Renderer[] renderers;
        private bool isInitialized;

        // One-way soft-platform support
        private Collider groundCollider;
        private float dropThroughTimer;

        // Smash mechanics state
        private readonly System.Collections.Generic.Queue<string> staleMoveHistory = new System.Collections.Generic.Queue<string>();
        private bool isOffScreen;

        // Events
        public event System.Action OnDeathEvent;
        public event System.Action OnRespawnEvent;
        public event System.Action<float, float> OnDamageTakenEvent; // damage, newPercent
        public event System.Action<float, float> OnMeterChangedEvent; // current, max

        // --- Match / stock state (single source of truth) ---
        private int stocksRemaining;
        public int StocksRemaining => stocksRemaining;
        public int PlayerIndex { get; private set; } = -1;
        public bool IsEliminated => stocksRemaining <= 0;

        public event System.Action<int> OnStockLost;   // remaining stocks after a loss
        public event System.Action OnEliminated;        // out of stocks (match loss)

        /// <summary>Initialize stock count + player slot for a match.</summary>
        public void InitializeMatchState(int stocks, int index)
        {
            stocksRemaining = Mathf.Max(0, stocks);
            PlayerIndex = index;
            isDead = false;
            isInvulnerable = false;
        }

        /// <summary>Lose one stock (called by the match manager on a confirmed KO).</summary>
        public void RegisterStockLoss()
        {
            if (stocksRemaining <= 0) return;
            stocksRemaining--;
            OnStockLost?.Invoke(stocksRemaining);
            if (stocksRemaining <= 0)
            {
                OnEliminated?.Invoke();
            }
        }

        protected virtual void Awake()
        {
            // Get components
            inputHandler = GetComponent<PlayerInputHandler>();
            combat = GetComponent<PlayerCombat>();
            meter = GetComponent<PlayerMeter>();
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            collider = GetComponent<CapsuleCollider>();
            stateMachine = GetComponent<StateMachine>();
            formMechanicHandler = GetComponent<FormMechanicHandler>();

            // Configure Rigidbody for platformer physics
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ConstrainToGameplayPlane();

            CacheRenderers();

            // Initialize form
            currentForm = startingForm ?? characterData?.defaultForm;

            // Spawn point
            spawnPoint = transform.position;
        }

        protected virtual void Start()
        {
            currentForm = startingForm != null ? startingForm : characterData?.defaultForm;
            if (!CanInitialize())
            {
                enabled = false;
                return;
            }

            // Initialize state machine with states
            InitializeStateMachine();

            // Start in idle state
            stateMachine.StartStateMachine<IdleState>();

            // Initialize meter
            meter.Initialize(characterData, currentForm);

            // Subscribe to meter events
            meter.OnMeterChanged += (current, max) => OnMeterChangedEvent?.Invoke(current, max);

            isInitialized = true;
        }

        protected virtual void Update()
        {
            if (!isInitialized) return;

            // Update cooldowns
            if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
            if (airDashCooldownTimer > 0f) airDashCooldownTimer -= Time.deltaTime;

            // Ground check
            UpdateGroundCheck();

            // Wall check
            UpdateWallCheck();

            // One-way soft-platform drop-through
            UpdateDropThrough();

            CheckBlastZoneRingOut();
        }

        protected virtual void FixedUpdate()
        {
            if (!isInitialized) return;

            // Apply form gravity multiplier
            if (!isGrounded)
            {
                float gravityScale = characterData.gravity * currentForm.gravityMultiplier * characterData.fallSpeedMultiplier;
                rb.AddForce(Vector3.up * Physics.gravity.y * (gravityScale - 1f), ForceMode.Acceleration);
            }

            ConstrainToGameplayPlane();
        }

        /// <summary>
        /// Register all player states with the state machine
        /// </summary>
        private void InitializeStateMachine()
        {
            // Movement states
            stateMachine.RegisterState<IdleState>();
            stateMachine.RegisterState<MoveState>();
            stateMachine.RegisterState<JumpState>();
            stateMachine.RegisterState<AirState>();
            stateMachine.RegisterState<DashState>();
            stateMachine.RegisterState<AirDashState>();

            // Combat states
            stateMachine.RegisterState<AttackState>();
            stateMachine.RegisterState<ParryState>();
            stateMachine.RegisterState<GrabState>();
            stateMachine.RegisterState<HitStunState>();
            stateMachine.RegisterState<KnockbackState>();
            stateMachine.RegisterState<DeadState>();
            stateMachine.RegisterState<RespawnState>();
            stateMachine.RegisterState<LedgeGrabState>();
        }

        private bool CanInitialize()
        {
            if (characterData == null)
            {
                Debug.LogError($"[PlayerController] {name} is missing CharacterData.", this);
                return false;
            }

            if (currentForm == null)
            {
                Debug.LogError($"[PlayerController] {name} has no starting/default FormSO.", this);
                return false;
            }

            if (inputHandler == null || combat == null || meter == null || rb == null || animator == null || collider == null || stateMachine == null)
            {
                Debug.LogError($"[PlayerController] {name} is missing one or more required player components.", this);
                return false;
            }

            return true;
        }

        public void InitializeCharacter(CharacterData data, FormSO form = null)
        {
            characterData = data;
            startingForm = form != null ? form : data?.defaultForm;
            currentForm = startingForm;

            if (data == null)
            {
                return;
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator != null && data.animatorController != null)
            {
                animator.runtimeAnimatorController = data.animatorController;
            }

            if (data.characterModelPrefab != null)
            {
                Transform existingVisual = transform.Find("GeneratedVisual");
                if (existingVisual != null)
                {
                    if (Application.isPlaying)
                        Destroy(existingVisual.gameObject);
                    else
                        DestroyImmediate(existingVisual.gameObject);
                }

                GameObject visual = Instantiate(data.characterModelPrefab, transform);
                visual.name = "GeneratedVisual";
                visual.transform.localPosition = new Vector3(0f, 1f, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * 1.8f;
                HidePlaceholderBody();
                CacheRenderers();
            }
        }

        private void HidePlaceholderBody()
        {
            Transform body = transform.Find("Body");
            if (body == null)
            {
                return;
            }

            Renderer[] bodyRenderers = body.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    bodyRenderers[i].enabled = false;
                }
            }
        }

        /// <summary>
        /// Ground detection using sphere cast
        /// </summary>
        private void UpdateGroundCheck()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            if (Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance + 0.1f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                isGrounded = true;
                groundCollider = hit.collider;
            }
            else
            {
                isGrounded = false;
                groundCollider = null;
            }
        }

        /// <summary>
        /// One-way soft-platform drop-through: when standing on a SoftPlatform and holding
        /// down, briefly ignore that collider so the fighter falls through (Smash behavior).
        /// </summary>
        private void UpdateDropThrough()
        {
            if (inputHandler != null && inputHandler.DropThroughPressed && isGrounded && groundCollider != null)
            {
                var soft = groundCollider.GetComponent<KyberKlash.Stage.SoftPlatform>();
                if (soft != null && soft.dropThrough)
                {
                    Physics.IgnoreCollision(collider, groundCollider, true);
                    dropThroughTimer = 0.35f;
                    return;
                }
            }

            if (dropThroughTimer > 0f)
            {
                dropThroughTimer -= Time.deltaTime;
                if (dropThroughTimer <= 0f && groundCollider != null)
                {
                    Physics.IgnoreCollision(collider, groundCollider, false);
                }
                return;
            }

            if (isGrounded && groundCollider != null && inputHandler != null)
            {
                var soft = groundCollider.GetComponent<KyberKlash.Stage.SoftPlatform>();
                if (soft != null && soft.dropThrough && inputHandler.MoveInput.y < -0.5f)
                {
                    Physics.IgnoreCollision(collider, groundCollider, true);
                    dropThroughTimer = 0.35f;
                }
            }
        }

        /// <summary>
        /// Wall detection
        /// </summary>
        private void UpdateWallCheck()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
            isTouchingWall = Physics.Raycast(origin, direction, wallCheckDistance, wallLayer, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Face a direction (left/right)
        /// </summary>
        public void FaceDirection(bool faceRight)
        {
            Vector3 scale = transform.localScale;
            scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        /// <summary>
        /// Check if ground dash is available
        /// </summary>
        public bool CanDash()
        {
            return dashCooldownTimer <= 0f && isGrounded;
        }

        /// <summary>
        /// Check if air dash is available
        /// </summary>
        public bool CanAirDash()
        {
            return currentForm.canAirDash && airDashCooldownTimer <= 0f && airDashesUsed < 1 && !isGrounded;
        }

        /// <summary>
        /// Use ground dash (start cooldown)
        /// </summary>
        public void UseDash()
        {
            dashCooldownTimer = dashCooldown;
        }

        /// <summary>
        /// Use air dash
        /// </summary>
        public void UseAirDash()
        {
            airDashCooldownTimer = airDashCooldown;
            airDashesUsed++;
        }

        /// <summary>
        /// Reset air actions (called on land)
        /// </summary>
        public void ResetAirActions()
        {
            airJumpsUsed = 0;
            airDashesUsed = 0;
            recoverySpecialAvailable = true;
        }

        /// <summary>
        /// Called when landing
        /// </summary>
        public void OnLanded()
        {
            ResetAirActions();
            SpawnVFX(characterData.landingVFXPrefab, transform.position + Vector3.down * 0.5f);
            PlayRandomSound(characterData.landSounds);
            ScreenShake(0.05f, 0.15f);
        }

        public bool CanAirJump()
        {
            return !isGrounded && airJumpsUsed < MaxAirJumps;
        }

        public void UseJump(bool isAirJump)
        {
            if (isAirJump)
            {
                airJumpsUsed++;
            }
        }

        public bool CanUseRecoverySpecial()
        {
            return recoverySpecialAvailable || isGrounded;
        }

        public void UseRecoverySpecial(Vector2 moveInput)
        {
            if (!isGrounded)
            {
                recoverySpecialAvailable = false;
            }

            Vector3 recoveryVelocity = GetRecoverySpecialVelocity(moveInput);
            rb.linearVelocity = recoveryVelocity;
            PlayAnimationStateIfExists("UpSpecial", 0.02f);
            IgniteSaber();
        }

        private Vector3 GetRecoverySpecialVelocity(Vector2 moveInput)
        {
            float facing = transform.localScale.x >= 0f ? 1f : -1f;
            float inputX = Mathf.Abs(moveInput.x) > 0.2f ? Mathf.Sign(moveInput.x) : facing;
            float inputY = moveInput.y > 0.35f ? 1.15f : 1f;

            float horizontal = 6f;
            float vertical = 12f;

            if (currentForm != null)
            {
                switch (currentForm.mechanicType)
                {
                    case FormSO.FormMechanicType.AcrobaticFlow:
                        horizontal = 8.5f;
                        vertical = 14.5f;
                        break;
                    case FormSO.FormMechanicType.PrecisionParry:
                        horizontal = 4.5f;
                        vertical = 12.5f;
                        break;
                    case FormSO.FormMechanicType.PerfectDeflection:
                        horizontal = 3.5f;
                        vertical = 13f;
                        break;
                    case FormSO.FormMechanicType.PowerCounter:
                        horizontal = 9.5f;
                        vertical = 10.5f;
                        break;
                    case FormSO.FormMechanicType.ForceUtility:
                        horizontal = 5.5f;
                        vertical = 15.5f;
                        break;
                    case FormSO.FormMechanicType.BerserkerTrance:
                        horizontal = 11f;
                        vertical = 11f;
                        break;
                    case FormSO.FormMechanicType.CounterStance:
                        horizontal = 6.5f;
                        vertical = 12.5f;
                        break;
                }
            }

            return new Vector3(inputX * horizontal, vertical * inputY, 0f);
        }

        /// <summary>
        /// Perform wall jump
        /// </summary>
        public void PerformWallJump()
        {
            if (!isTouchingWall) return;

            // Push away from wall
            Vector3 wallNormal = transform.localScale.x >= 0f ? Vector3.left : Vector3.right;
            float jumpForce = CalculateJumpForce();
            rb.linearVelocity = new Vector3(wallNormal.x * jumpForce * 0.5f, jumpForce, 0f);

            // Face away from wall
            FaceDirection(wallNormal.x > 0);

            airJumpsUsed = 0; // Wall jump doesn't count as air jump
            SpawnVFX(characterData.dashVFXPrefab, transform.position);
        }

        /// <summary>
        /// Calculate jump force based on character data and form
        /// </summary>
        private float CalculateJumpForce()
        {
            float jumpHeight = characterData.jumpHeight * currentForm.jumpHeightMultiplier;
            return Mathf.Sqrt(2f * jumpHeight * Physics.gravity.magnitude * characterData.gravity * currentForm.gravityMultiplier);
        }

        /// <summary>
        /// Take damage and apply knockback
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (isInvulnerable || isDead) return;

            // Form defense 1 - Power Counter (Shien/Djem So): while in the parry window and
            // the incoming hit meets the threshold, absorb it and counter-strike.
            if (formMechanicHandler != null && formMechanicHandler.TryPowerCounter(damageInfo))
            {
                return;
            }

            // Form defense 2 - Berserker Trance armor: small hits are shrugged off entirely.
            if (formMechanicHandler != null && formMechanicHandler.CheckTranceArmor(damageInfo.damage))
            {
                return;
            }

            // Apply damage percentage
            float damage = damageInfo.damage * currentForm.damageMultiplier;
            float projectedDamagePercent = Mathf.Min(meter.DamagePercent + damage, 999f);

            // Calculate knockback
            float knockback = damageInfo.attackData != null
                ? damageInfo.attackData.CalculateKnockback(projectedDamagePercent)
                : damageInfo.knockback.magnitude;
            knockback *= CalculateLaunchMultiplier(projectedDamagePercent, damageInfo.attackData);
            knockback *= characterData.GetKnockbackMultiplier();
            knockback *= currentForm.knockbackMultiplier;

            // Stale-move negation: repeating the same attack weakens its knockback,
            // preventing one-move spam (Smash RPS counterplay).
            if (damageInfo.attackData != null)
            {
                knockback *= GetStaleMoveMultiplier(damageInfo.attackData.attackId);
            }

            Vector3 knockbackDirection = damageInfo.knockback.sqrMagnitude > 0.001f ? damageInfo.knockback.normalized : Vector3.up;
            knockbackDirection.z = 0f;
            if (knockbackDirection.sqrMagnitude < 0.001f)
            {
                knockbackDirection = Vector3.up;
            }

            // Directional Influence (DI): the victim's current hold input rotates the
            // launch angle, letting skilled players survive otherwise-fatal combos.
            knockbackDirection = ApplyDirectionalInfluence(knockbackDirection, damageInfo.attackData);

            Vector3 knockbackVector = knockbackDirection * knockback;
            knockbackVector.z = 0f;

            // Store for state
            lastHitStunDuration = damageInfo.attackData != null ? damageInfo.attackData.hitStunFrames / 60f : 0.2f;
            lastKnockbackVelocity = knockbackVector;

            // Enter hitstun/knockback
            if (knockback > 10f)
            {
                isInKnockback = true;
                isInHitStun = false;
            }
            else
            {
                isInHitStun = true;
                isInKnockback = false;
            }

            meter.TakeDamage(damage);

            // Register the move for stale-move tracking (after it connects).
            if (damageInfo.attackData != null)
            {
                RegisterStaleMove(damageInfo.attackData.attackId);
            }

            // Apply knockback after damage percent updates so UI and launch feel tied together.
            rb.linearVelocity = knockbackVector;

            // Visual feedback
            ScreenShake(0.1f, 0.3f);
            Vibrate(0.5f, 0.5f, 0.1f);
            SpawnVFX(damageInfo.attackData != null ? damageInfo.attackData.hitVFXPrefab : null, damageInfo.hitPoint);
            HitPointFeedback.Spawn(damageInfo.hitPoint, damage, knockbackVector, knockback > 18f);
            PlayRandomSound(characterData.hitSounds);

            // Raise event
            OnDamageTakenEvent?.Invoke(damage, meter.DamagePercent);
        }

        private float CalculateLaunchMultiplier(float projectedDamagePercent, AttackSO attackData)
        {
            float multiplier = 1f + Mathf.Clamp(projectedDamagePercent, 0f, 300f) / 220f;
            if (attackData != null && IsLauncherAttack(attackData) && projectedDamagePercent > 50f)
            {
                multiplier += Mathf.Clamp((projectedDamagePercent - 50f) / 85f, 0f, 2.5f);
            }

            return multiplier;
        }

        private static bool IsLauncherAttack(AttackSO attackData)
        {
            if (attackData == null) return false;

            string trigger = attackData.animationTrigger ?? string.Empty;
            string id = attackData.attackId ?? string.Empty;
            return trigger.Contains("Heavy") ||
                   trigger.Contains("Special") ||
                   id.Contains("heavy") ||
                   id.Contains("special");
        }

        /// <summary>
        /// Apply Directional Influence: rotate the base knockback angle toward the victim's
        /// current movement input. DI never increases knockback magnitude, only shifts direction.
        /// </summary>
        private Vector3 ApplyDirectionalInfluence(Vector3 baseDirection, AttackSO attackData)
        {
            if (inputHandler == null || attackData == null) return baseDirection;

            Vector2 di = inputHandler.MoveInput;
            if (di.sqrMagnitude < 0.01f) return baseDirection;

            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x);
            float diAngle = Mathf.Atan2(di.y, di.x);

            // Rotate partially toward the DI direction, clamped so it can't fully invert.
            float delta = Mathf.DeltaAngle(baseAngle * Mathf.Rad2Deg, diAngle * Mathf.Rad2Deg);
            float maxShift = 90f * diStrength; // up to ~30 degrees at diStrength 0.33
            delta = Mathf.Clamp(delta, -maxShift, maxShift);

            float newAngle = (baseAngle * Mathf.Rad2Deg + delta) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(newAngle), Mathf.Sin(newAngle), 0f);
        }

        /// <summary>
        /// Returns a knockback/damage multiplier for stale-move negation. A move used
        /// repeatedly drops toward staleMoveMinMultiplier; fresh moves return ~1.0.
        /// </summary>
        private float GetStaleMoveMultiplier(string attackId)
        {
            if (string.IsNullOrEmpty(attackId)) return 1f;

            int occurrences = 0;
            foreach (var id in staleMoveHistory)
            {
                if (id == attackId) occurrences++;
            }

            if (occurrences == 0) return 1f;

            // Linear falloff from 1.0 (fresh) to staleMoveMinMultiplier (max repetitions).
            float t = Mathf.Clamp01((float)occurrences / staleMoveHistorySize);
            return Mathf.Lerp(1f, staleMoveMinMultiplier, t);
        }

        /// <summary>
        /// Record a used move into the stale-move history (bounded queue).
        /// </summary>
        private void RegisterStaleMove(string attackId)
        {
            if (string.IsNullOrEmpty(attackId)) return;

            staleMoveHistory.Enqueue(attackId);
            while (staleMoveHistory.Count > staleMoveHistorySize)
            {
                staleMoveHistory.Dequeue();
            }
        }

        public bool IsOffScreen => isOffScreen;

        /// <summary>
        /// The ledge currently grabbed (set by AirState when a ledge grab triggers).
        /// </summary>
        public Transform CurrentLedge { get; private set; }

        public void SetCurrentLedge(Transform ledge) => CurrentLedge = ledge;
        public void ClearCurrentLedge() => CurrentLedge = null;

        /// <summary>
        /// Die (ring out)
        /// </summary>
        public void Die()
        {
            if (isDead) return;

            isDead = true;
            GameAudioManager.Instance?.PlaySfx("ringout_ko");
            OnDeathEvent?.Invoke();
        }

        /// <summary>
        /// Called when entering DeadState - notifies game manager
        /// </summary>
        public void OnDeath()
        {
            // This is called by DeadState to notify the game manager
            // The actual death logic is in Die()
        }

        /// <summary>
        /// Respawn at spawn point
        /// </summary>
        public void Respawn()
        {
            transform.position = spawnPoint;
            rb.linearVelocity = Vector3.zero;
            ConstrainToGameplayPlane();
            isDead = false;
            ClearHitReaction();
            ResetAirActions();
            meter.ResetMeter();
            OnRespawnEvent?.Invoke();
        }

        public void ClearHitReaction()
        {
            isInHitStun = false;
            isInKnockback = false;
        }

        public void ClearHitStun()
        {
            isInHitStun = false;
        }

        public void ClearKnockback()
        {
            isInKnockback = false;
        }

        /// <summary>
        /// Set invulnerability state
        /// </summary>
        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
            // Could also change layer to ignore collisions
        }

        /// <summary>
        /// Start invulnerability flash effect
        /// </summary>
        public void StartInvulnerabilityFlash()
        {
            InvokeRepeating(nameof(FlashRenderer), 0f, invulnerabilityFlashInterval);
        }

        /// <summary>
        /// Stop invulnerability flash
        /// </summary>
        public void StopInvulnerabilityFlash()
        {
            CancelInvoke(nameof(FlashRenderer));
            // Restore original materials
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null)
                {
                    renderers[i].material = originalMaterials[i];
                }
            }
        }

        private void FlashRenderer()
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = !renderer.enabled;
                }
            }
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }

        /// <summary>
        /// Perform tech/ukemi on ground
        /// </summary>
        public void PerformTech()
        {
            // Quick recovery with invulnerability
            SetInvulnerable(true);
            StartInvulnerabilityFlash();
            Invoke(nameof(EndTechInvuln), 0.5f);
        }

        private void EndTechInvuln()
        {
            SetInvulnerable(false);
            StopInvulnerabilityFlash();
        }

        /// <summary>
        /// Screen shake via Cinemachine Impulse
        /// </summary>
        public void ScreenShake(float duration, float intensity)
        {
            var impulseSource = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(intensity);
            }
        }

        /// <summary>
        /// Vibrate gamepad
        /// </summary>
        public void Vibrate(float lowFreq, float highFreq, float duration)
        {
            inputHandler?.Vibrate(lowFreq, highFreq, duration);
        }

        /// <summary>
        /// Spawn VFX prefab
        /// </summary>
        public GameObject SpawnVFX(GameObject prefab, Vector3 position, Quaternion rotation = default)
        {
            if (prefab == null) return null;

            GameObject vfx = Instantiate(prefab, position, rotation == default ? Quaternion.identity : rotation);
            // Could add to object pool here
            return vfx;
        }

        /// <summary>
        /// Play random sound from array
        /// </summary>
        public void PlayRandomSound(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        /// <summary>
        /// Play named sound (for specific SFX)
        /// </summary>
        public void PlaySound(string soundName)
        {
            // Implementation would use an audio manager
            GameAudioManager.Instance?.PlaySfx(soundName);
        }

        /// <summary>
        /// Change form at runtime
        /// </summary>
        public void ChangeForm(FormSO newForm)
        {
            if (newForm == null || System.Array.IndexOf(characterData.availableForms, newForm) == -1)
            {
                Debug.LogWarning($"[PlayerController] Form {newForm?.name} not available for this character");
                return;
            }

            currentForm = newForm;
            meter.OnFormChanged(newForm);

            // Visual change
            if (saberTip != null)
            {
                var trail = saberTip.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    trail.colorGradient = newForm.saberGradient;
                }
            }
        }

        public void PlayAnimationStateIfExists(string stateName, float transitionDuration = 0.05f)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash))
            {
                animator.CrossFade(hash, transitionDuration);
            }
        }

        public void IgniteSaber()
        {
            var saber = GetComponentInChildren<KyberKlash.VFX.LightsaberVFX>();
            if (saber != null)
            {
                saber.Ignite();
            }
        }

        public void ExtinguishSaber()
        {
            var saber = GetComponentInChildren<KyberKlash.VFX.LightsaberVFX>();
            if (saber != null)
            {
                saber.Extinguish();
            }
        }

        /// <summary>
        /// Set spawn point (for stage respawning)
        /// </summary>
        public void SetSpawnPoint(Vector3 point)
        {
            point.z = 0f;
            spawnPoint = point;
        }

        private void ConstrainToGameplayPlane()
        {
            Vector3 position = transform.position;
            if (Mathf.Abs(position.z) > 0.0001f)
            {
                position.z = 0f;
                transform.position = position;
            }

            if (rb != null)
            {
                Vector3 velocity = rb.linearVelocity;
                if (Mathf.Abs(velocity.z) > 0.0001f)
                {
                    velocity.z = 0f;
                    rb.linearVelocity = velocity;
                }
            }
        }

        private void CheckBlastZoneRingOut()
        {
            if (isDead || StageManager.Instance == null)
            {
                return;
            }

            var stage = StageManager.Instance.CurrentStageData;
            Vector3 pos = transform.position;

            // Meteor/spike line: if falling fast past the lower sub-boundary, KO immediately
            // (enables spikes / risky fast-fall Dairs to finish before the victim can act).
            if (stage != null && stage.IsOnMeteorLine(pos, -rb.linearVelocity.y))
            {
                Die();
                return;
            }

            if (stage != null && stage.IsOutOfBounds(pos))
            {
                Die();
                return;
            }

            // Off-screen (magnifying-glass) state: inside blast zones but outside the camera
            // view. Apply steady chip damage and flag for the HUD radar.
            bool off = stage != null && stage.IsOffScreen(pos);
            if (off != isOffScreen)
            {
                isOffScreen = off;
            }
            if (off && meter != null)
            {
                meter.TakeDamage(stage.offScreenDamagePerSecond * Time.deltaTime);
            }
        }

        // Properties for external access
        public int AirJumpsUsed => airJumpsUsed;
        public int MaxAirJumps => characterData.maxJumps * (currentForm.maxAirJumps > 0 ? currentForm.maxAirJumps : 1);
        public float DamagePercent => meter.DamagePercent;
        public float MeterPercent => meter.MeterPercent;

        #region Animation Event Helpers

        /// <summary>
        /// Handle animation events from CombatAnimationEvents
        /// </summary>
        public virtual void OnAnimationEvent(CombatAnimationEvent evt)
        {
            // Override in derived states for specific handling
        }

        /// <summary>
        /// Set animator float parameter if it exists
        /// </summary>
        public void SetAnimatorFloatIfExists(string name, float value)
        {
            if (animator != null && animator.parameters.Any(p => p.name == name && p.type == AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(name, value);
            }
        }

        /// <summary>
        /// Set animator trigger if it exists
        /// </summary>
        public void SetAnimatorTriggerIfExists(string name)
        {
            if (animator != null && animator.parameters.Any(p => p.name == name && p.type == AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(name);
            }
        }

        #endregion
    }
}
