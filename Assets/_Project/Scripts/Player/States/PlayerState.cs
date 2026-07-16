using UnityEngine;
using KyberKlash.Data;

namespace KyberKlash.Player.States
{
    /// <summary>
    /// Base class for all player states.
    /// Provides common references and helper methods.
    /// </summary>
    public abstract class PlayerState : KyberKlash.Core.State<PlayerController>
    {
        protected PlayerController player;
        protected PlayerInputHandler input;
        protected PlayerCombat combat;
        protected PlayerMeter meter;
        protected Rigidbody rb;
        protected Animator animator;
        protected CharacterData characterData;
        protected FormSO currentForm;

        // Cached physics values
        protected bool isGrounded;
        protected bool wasGrounded;
        protected float coyoteTimer;
        protected float jumpBufferTimer;
        protected int jumpsUsed;

        public override void Enter(KyberKlash.Core.StateMachine machine)
        {
            base.Enter(machine);
            player = owner;
            input = player.InputHandler;
            combat = player.Combat;
            meter = player.Meter;
            rb = player.Rigidbody;
            animator = player.Animator;
            characterData = player.CharacterData;
            currentForm = player.CurrentForm;

            CacheGroundState();
            OnEnterPlayerState();
        }

        protected virtual void OnEnterPlayerState() { }

        public override void Exit()
        {
            OnExitPlayerState();
            base.Exit();
        }

        protected virtual void OnExitPlayerState() { }

        public override void UpdateLogic(float deltaTime)
        {
            base.UpdateLogic(deltaTime);
            UpdateGroundState();
            UpdateTimers(deltaTime);
            OnUpdatePlayerLogic(deltaTime);
        }

        public override void PhysicsUpdate(float fixedDeltaTime)
        {
            base.PhysicsUpdate(fixedDeltaTime);
            OnPhysicsPlayerUpdate(fixedDeltaTime);
        }

        public override void CheckTransitions()
        {
            base.CheckTransitions();
            CheckCommonTransitions();
            OnCheckPlayerTransitions();
        }

        protected virtual void OnUpdatePlayerLogic(float deltaTime) { }
        protected virtual void OnPhysicsPlayerUpdate(float fixedDeltaTime) { }
        protected virtual void OnCheckPlayerTransitions() { }

        /// <summary>
        /// Cache grounded state for coyote time
        /// </summary>
        protected void CacheGroundState()
        {
            wasGrounded = isGrounded;
            isGrounded = player.IsGrounded;
        }

        /// <summary>
        /// Update ground detection and coyote time
        /// </summary>
        protected void UpdateGroundState()
        {
            wasGrounded = isGrounded;
            isGrounded = player.IsGrounded;

            if (isGrounded)
            {
                coyoteTimer = characterData.coyoteTime;
                jumpsUsed = 0;
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Update input buffers
        /// </summary>
        protected void UpdateTimers(float deltaTime)
        {
            if (jumpBufferTimer > 0f)
                jumpBufferTimer -= deltaTime;
        }

        /// <summary>
        /// Request jump with buffering
        /// </summary>
        protected void RequestJump()
        {
            jumpBufferTimer = characterData.jumpBufferTime;
        }

        /// <summary>
        /// Check if jump is buffered and can execute
        /// </summary>
        protected bool CanExecuteBufferedJump()
        {
            return jumpBufferTimer > 0f && (isGrounded || coyoteTimer > 0f || jumpsUsed < characterData.maxJumps);
        }

        /// <summary>
        /// Consume jump buffer
        /// </summary>
        protected void ConsumeJumpBuffer()
        {
            jumpBufferTimer = 0f;
        }

        /// <summary>
        /// Apply movement with form modifiers
        /// </summary>
        protected void ApplyMovement(Vector3 inputDirection, float speedMultiplier = 1f)
        {
            if (inputDirection.sqrMagnitude < 0.01f) return;

            float moveSpeed = characterData.moveSpeed * currentForm.moveSpeedMultiplier * speedMultiplier;
            Vector3 targetVelocity = inputDirection * moveSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            targetVelocity.z = 0f;

            // Smooth acceleration
            float acceleration = isGrounded ? characterData.groundAcceleration : characterData.airAcceleration;
            Vector3 velocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Face movement direction
        /// </summary>
        protected void FaceMovementDirection(Vector3 inputDirection)
        {
            if (inputDirection.sqrMagnitude > 0.01f)
            {
                player.FaceDirection(inputDirection.x > 0);
            }
        }

        protected Vector3 GetSideMoveDirection(Vector2 moveInput)
        {
            if (Mathf.Abs(moveInput.x) < 0.01f)
            {
                return Vector3.zero;
            }

            return new Vector3(Mathf.Sign(moveInput.x), 0f, 0f);
        }

        protected Vector3 GetFacingDirection()
        {
            return player.transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
        }

        /// <summary>
        /// Check for common transitions (hit, death, etc.)
        /// </summary>
        protected void CheckCommonTransitions()
        {
            if (player.IsDead)
            {
                RequestTransition<DeadState>();
                return;
            }

            if (player.IsInHitStun)
            {
                RequestTransition<HitStunState>();
                return;
            }

            if (player.IsInKnockback)
            {
                RequestTransition<KnockbackState>();
                return;
            }
        }

        /// <summary>
        /// Get the attack data for a specific attack slot
        /// </summary>
        protected AttackSO GetAttackData(FormSO.AttackSlot slot)
        {
            return currentForm?.GetAttack(slot, characterData.GetDefaultAttack(slot));
        }

        /// <summary>
        /// Check if player can attack (not in recovery, has meter if needed)
        /// </summary>
        protected bool CanAttack(AttackSO attackData)
        {
            if (attackData == null) return false;
            if (meter.CurrentValue < attackData.minMeterRequired) return false;
            return true;
        }

        /// <summary>
        /// Start an attack
        /// </summary>
        protected void StartAttack(AttackSO attackData)
        {
            if (CanAttack(attackData))
            {
                combat.StartAttack(attackData);
                RequestTransition<AttackState>();
            }
        }

        /// <summary>
        /// Try to parry
        /// </summary>
        protected void TryParry()
        {
            if (combat.CanParry())
            {
                combat.StartParry();
                RequestTransition<ParryState>();
            }
        }

        /// <summary>
        /// Start a ground/air attack chosen by current stick direction (Smash-style tilts/aerials).
        /// strong=true selects the heavy variant; dir biases left/right when supplied.
        /// </summary>
        protected void StartDirectionalAttack(bool strong, Vector2 dir)
        {
            Vector2 move = input.MoveInput;
            if (dir.sqrMagnitude > 0.01f) move = dir;

            FormSO.AttackSlot slot;
            if (strong)
            {
                slot = FormSO.AttackSlot.Heavy;
            }
            else if (move.y > 0.5f)
            {
                slot = FormSO.AttackSlot.UpAerial;
            }
            else if (move.x > 0.5f)
            {
                slot = FormSO.AttackSlot.ForwardAerial;
            }
            else if (move.x < -0.5f)
            {
                slot = FormSO.AttackSlot.BackAerial;
            }
            else
            {
                slot = FormSO.AttackSlot.Light;
            }

            StartAttack(GetAttackData(slot));
        }
    }
}
