using UnityEngine;
using KyberKlash.Data;
using KyberKlash.Stage;

namespace KyberKlash.Player.States
{
    /// <summary>
    /// Grounded idle state - waiting for input
    /// </summary>
    public class IdleState : PlayerState
    {
        private float idleTimer;
        private int idleAnimationHash;

        protected override void OnEnterPlayerState()
        {
            idleTimer = 0f;
            idleAnimationHash = Animator.StringToHash("Idle");
            animator.CrossFade(idleAnimationHash, 0.1f);

            // Reset air actions
            player.ResetAirActions();
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            idleTimer += deltaTime;

            // Check for movement input
            Vector2 moveInput = input.MoveInput;
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                RequestTransition<MoveState>();
                return;
            }

            // Check for jump
            if (input.JumpPressed || CanExecuteBufferedJump())
            {
                ConsumeJumpBuffer();
                RequestTransition<JumpState>();
                return;
            }

            // Check for attacks
            if (input.LightAttackPressed)
            {
                StartDirectionalAttack(false, Vector2.zero);
                return;
            }

            if (input.StrongAttackPressed)
            {
                StartDirectionalAttack(true, input.StrongAttackDir);
                return;
            }

            if (input.SpecialPressed)
            {
                StartAttack(currentForm.GetAttack(FormSO.AttackSlot.NeutralSpecial, characterData.GetDefaultAttack(FormSO.AttackSlot.NeutralSpecial)));
                return;
            }

            // Check for parry / block (hold Down)
            if (input.ParryPressed || input.ParryHeld)
            {
                TryParry();
                return;
            }

            // Check for grab / throw
            if (input.GrabPressed)
            {
                RequestTransition<GrabState>();
                return;
            }

            // Check for dash
            if (input.DashPressed && player.CanDash())
            {
                RequestTransition<DashState>();
                return;
            }

            // Idle variation animations
            if (idleTimer > 5f && Random.value < 0.01f)
            {
                player.SetAnimatorTriggerIfExists("IdleVariation");
                idleTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Grounded movement state
    /// </summary>
    public class MoveState : PlayerState
    {
        private int moveAnimationHash;

        protected override void OnEnterPlayerState()
        {
            moveAnimationHash = Animator.StringToHash("Move");
            animator.CrossFade(moveAnimationHash, 0.1f);
            player.SetAnimatorFloatIfExists("MoveSpeed", 1f);
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            Vector2 moveInput = input.MoveInput;

            // Stop moving -> return to idle
            if (Mathf.Abs(moveInput.x) < 0.01f)
            {
                RequestTransition<IdleState>();
                return;
            }

            // Apply movement
            Vector3 moveDirection = GetSideMoveDirection(moveInput);
            ApplyMovement(moveDirection);
            FaceMovementDirection(moveDirection);

            // Update animation speed based on velocity
            float speedPercent = Mathf.Abs(rb.linearVelocity.x) / (characterData.moveSpeed * currentForm.moveSpeedMultiplier);
            player.SetAnimatorFloatIfExists("MoveSpeed", speedPercent);

            // Check for jump
            if (input.JumpPressed || CanExecuteBufferedJump())
            {
                ConsumeJumpBuffer();
                RequestTransition<JumpState>();
                return;
            }

            // Check for attacks
            if (input.LightAttackPressed)
            {
                StartDirectionalAttack(false, Vector2.zero);
                return;
            }

            if (input.StrongAttackPressed)
            {
                StartDirectionalAttack(true, input.StrongAttackDir);
                return;
            }

            if (input.SpecialPressed)
            {
                StartAttack(currentForm.GetAttack(FormSO.AttackSlot.NeutralSpecial, characterData.GetDefaultAttack(FormSO.AttackSlot.NeutralSpecial)));
                return;
            }

            // Check for parry / block (hold Down)
            if (input.ParryPressed || input.ParryHeld)
            {
                TryParry();
                return;
            }

            // Check for grab / throw
            if (input.GrabPressed)
            {
                RequestTransition<GrabState>();
                return;
            }

            // Check for dash
            if (input.DashPressed && player.CanDash())
            {
                RequestTransition<DashState>();
                return;
            }
        }

        protected override void OnExitPlayerState()
        {
            player.SetAnimatorFloatIfExists("MoveSpeed", 0f);
        }
    }

    /// <summary>
    /// Jump state - initial jump impulse (supports Smash short hop: tap = lower jump, hold = full)
    /// </summary>
    public class JumpState : PlayerState
    {
        private int jumpAnimationHash;
        private bool hasJumped;
        private bool isShortHop;
        private float shortHopCutVelocity;

        protected override void OnEnterPlayerState()
        {
            bool isAirJump = !player.IsGrounded;
            jumpAnimationHash = Animator.StringToHash(isAirJump ? "DoubleJump" : "Jump");
            if (!animator.HasState(0, jumpAnimationHash))
            {
                jumpAnimationHash = Animator.StringToHash("Jump");
            }

            animator.CrossFade(jumpAnimationHash, 0.05f);
            hasJumped = false;
            isShortHop = !input.JumpHeld; // Released during jump-squat => short hop
            player.UseJump(isAirJump);

            // Short-hop apex target (a fraction of full jump height)
            float fullJumpForce = CalculateJumpForce();
            float shortHopHeight = (characterData.jumpHeight * currentForm.jumpHeightMultiplier) * 0.55f;
            shortHopCutVelocity = Mathf.Sqrt(2f * shortHopHeight * Physics.gravity.magnitude * characterData.gravity * currentForm.gravityMultiplier);

            // Play jump sound
            player.PlayRandomSound(player.CharacterData.jumpSounds);

            // Spawn jump VFX
            player.SpawnVFX(player.CharacterData.dustVFXPrefab, player.transform.position + Vector3.down * 0.5f);
        }

        protected override void OnPhysicsPlayerUpdate(float fixedDeltaTime)
        {
            if (!hasJumped)
            {
                // Apply jump impulse
                float jumpForce = CalculateJumpForce();
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
                hasJumped = true;
            }

            // Short-hop cut: if the player released jump while still rising, clamp ascent
            // to the short-hop apex (classic Smash variable jump height).
            if (isShortHop && rb.linearVelocity.y > shortHopCutVelocity)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, shortHopCutVelocity, 0f);
            }

            // Air control
            Vector2 moveInput = input.MoveInput;
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                Vector3 moveDirection = GetSideMoveDirection(moveInput);
                float airSpeed = characterData.airSpeed * currentForm.airSpeedMultiplier;
                Vector3 targetVelocity = moveDirection * airSpeed;
                targetVelocity.y = rb.linearVelocity.y;
                targetVelocity.z = 0f;

                Vector3 velocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, characterData.airAcceleration * fixedDeltaTime);
                velocity.z = 0f;
                rb.linearVelocity = velocity;
                FaceMovementDirection(moveDirection);
            }
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            // Transition to air state once leaving ground
            if (hasJumped && !isGrounded)
            {
                RequestTransition<AirState>();
                return;
            }

            // Check for air attacks
            if (input.LightAttackPressed)
            {
                StartAttack(currentForm.GetAttack(FormSO.AttackSlot.UpAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.UpAerial)));
                return;
            }

            if (input.HeavyAttackPressed)
            {
                StartAttack(currentForm.GetAttack(FormSO.AttackSlot.DownAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.DownAerial)));
                return;
            }

            // Check for air dash
            if (input.DashPressed && player.CanAirDash())
            {
                RequestTransition<AirDashState>();
                return;
            }

            // Check for parry
            if (input.ParryPressed)
            {
                TryParry();
                return;
            }

            // Fast fall
            if (input.MoveInput.y < -0.5f)
            {
                rb.linearVelocity += Vector3.down * characterData.gravity * characterData.fallSpeedMultiplier * Time.fixedDeltaTime;
            }
        }

        private float CalculateJumpForce()
        {
            float jumpHeight = characterData.jumpHeight * currentForm.jumpHeightMultiplier;
            return Mathf.Sqrt(2f * jumpHeight * Physics.gravity.magnitude * characterData.gravity * currentForm.gravityMultiplier);
        }
    }

    /// <summary>
    /// General air state - handles aerial movement and actions
    /// </summary>
    public class AirState : PlayerState
    {
        private int fallAnimationHash;
        private bool wasGroundedLastFrame;

        protected override void OnEnterPlayerState()
        {
            fallAnimationHash = Animator.StringToHash("Fall");
            animator.CrossFade(fallAnimationHash, 0.1f);
            wasGroundedLastFrame = false;
        }

        protected override void OnPhysicsPlayerUpdate(float fixedDeltaTime)
        {
            // Air movement
            Vector2 moveInput = input.MoveInput;
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                Vector3 moveDirection = GetSideMoveDirection(moveInput);
                float airSpeed = characterData.airSpeed * currentForm.airSpeedMultiplier;
                Vector3 targetVelocity = moveDirection * airSpeed;
                targetVelocity.y = rb.linearVelocity.y;
                targetVelocity.z = 0f;

                Vector3 velocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, characterData.airAcceleration * fixedDeltaTime);
                velocity.z = 0f;
                rb.linearVelocity = velocity;
                FaceMovementDirection(moveDirection);
            }

            // Apply gravity
            float gravityScale = characterData.gravity * currentForm.gravityMultiplier * characterData.fallSpeedMultiplier;
            if (rb.linearVelocity.y < 0 && input.MoveInput.y < -0.5f)
            {
                gravityScale *= characterData.fastFallMultiplier;
            }
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (gravityScale - 1f) * fixedDeltaTime;
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            // Ledge grab: if airborne and drifting into a ledge, snap and hang.
            if (!isGrounded && StageManager.Instance != null &&
                StageManager.Instance.TryGetNearestLedge(player.transform.position, out var ledge, 2.2f))
            {
                // Only grab when moving toward the ledge horizontally
                float toLedge = Mathf.Sign(ledge.position.x - player.transform.position.x);
                if (Mathf.Abs(input.MoveInput.x) < 0.6f || Mathf.Sign(input.MoveInput.x) == toLedge || rb.linearVelocity.x * toLedge > -1f)
                {
                    player.SetCurrentLedge(ledge);
                    RequestTransition<LedgeGrabState>();
                    return;
                }
            }

            // Landed -> transition to idle/move
            if (isGrounded && !wasGroundedLastFrame)
            {
                player.OnLanded();

                if (Mathf.Abs(input.MoveInput.x) > 0.01f)
                {
                    RequestTransition<MoveState>();
                }
                else
                {
                    RequestTransition<IdleState>();
                }
                return;
            }
            wasGroundedLastFrame = isGrounded;

            // Check for double jump
            if ((input.JumpPressed || CanExecuteBufferedJump()) && player.CanAirJump())
            {
                ConsumeJumpBuffer();
                RequestTransition<JumpState>();
                return;
            }

            // Recovery special (double-tap Up) - launch back toward the stage
            if (input.RecoverySpecialPressed)
            {
                AttackSO attack = currentForm.GetAttack(FormSO.AttackSlot.UpSpecial, characterData.GetDefaultAttack(FormSO.AttackSlot.UpSpecial));
                if (player.CanUseRecoverySpecial() && CanAttack(attack))
                {
                    player.UseRecoverySpecial(input.MoveInput);
                    StartAttack(attack);
                    return;
                }
            }

            // Air attacks
            if (input.LightAttackPressed)
            {
                // Determine aerial direction
                AttackSO attack = DetermineAerialAttack();
                StartAttack(attack);
                return;
            }

            if (input.StrongAttackPressed)
            {
                AttackSO attack = currentForm.GetAttack(FormSO.AttackSlot.DownAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.DownAerial));
                StartAttack(attack);
                return;
            }

            if (input.SpecialPressed)
            {
                AttackSO attack = currentForm.GetAttack(FormSO.AttackSlot.UpSpecial, characterData.GetDefaultAttack(FormSO.AttackSlot.UpSpecial));
                if (player.CanUseRecoverySpecial() && CanAttack(attack))
                {
                    player.UseRecoverySpecial(input.MoveInput);
                    StartAttack(attack);
                    return;
                }
            }

            // Grab / throw (air)
            if (input.GrabPressed)
            {
                RequestTransition<GrabState>();
                return;
            }

            // Air dash
            if (input.DashPressed && player.CanAirDash())
            {
                RequestTransition<AirDashState>();
                return;
            }

            // Parry
            if (input.ParryPressed)
            {
                TryParry();
                return;
            }

            // Wall jump check
            if (currentForm.canWallJump && player.IsTouchingWall && input.JumpPressed)
            {
                player.PerformWallJump();
                RequestTransition<JumpState>();
                return;
            }
        }

        private AttackSO DetermineAerialAttack()
        {
            Vector2 moveInput = input.MoveInput;

            // Neutral air (no directional input)
            if (moveInput.sqrMagnitude < 0.1f)
            {
                return currentForm.GetAttack(FormSO.AttackSlot.UpAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.UpAerial));
            }

            // Directional aerials
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;

            if (angle > 45f && angle < 135f) // Up
            {
                return currentForm.GetAttack(FormSO.AttackSlot.UpAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.UpAerial));
            }
            else if (angle < -45f && angle > -135f) // Down
            {
                return currentForm.GetAttack(FormSO.AttackSlot.DownAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.DownAerial));
            }
            else if (angle >= -45f && angle <= 45f) // Forward
            {
                return currentForm.GetAttack(FormSO.AttackSlot.ForwardAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.ForwardAerial));
            }
            else // Back
            {
                return currentForm.GetAttack(FormSO.AttackSlot.BackAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.BackAerial));
            }
        }
    }

    /// <summary>
    /// Ground dash state
    /// </summary>
    public class DashState : PlayerState
    {
        private float dashTimer;
        private float dashDuration;
        private Vector3 dashDirection;
        private int dashAnimationHash;

        protected override void OnEnterPlayerState()
        {
            dashAnimationHash = Animator.StringToHash("Dash");
            animator.CrossFade(dashAnimationHash, 0.05f);

            dashDirection = GetSideMoveDirection(input.MoveInput);
            if (dashDirection.sqrMagnitude < 0.01f)
            {
                dashDirection = GetFacingDirection();
            }

            dashDuration = characterData.dashDuration * currentForm.dashMultiplier;
            dashTimer = dashDuration;

            player.UseDash();
            player.SpawnVFX(player.CharacterData.dashVFXPrefab, player.transform.position);

            // Initial dash velocity
            float dashSpeed = characterData.dashDistance / dashDuration * currentForm.dashMultiplier;
            rb.linearVelocity = dashDirection * dashSpeed;
        }

        protected override void OnPhysicsPlayerUpdate(float fixedDeltaTime)
        {
            dashTimer -= fixedDeltaTime;

            // Maintain dash velocity
            float dashSpeed = characterData.dashDistance / dashDuration * currentForm.dashMultiplier;
            Vector3 vel = rb.linearVelocity;
            vel.x = dashDirection.x * dashSpeed;
            vel.z = 0f;
            rb.linearVelocity = vel;
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            // Dash-dance: reversing the stick mid-dash cancels into a fresh dash the other
            // way (Smash spacing/mindgame tool). Only after a small grace period.
            if (dashTimer > dashDuration * 0.15f)
            {
                Vector3 desired = GetSideMoveDirection(input.MoveInput);
                if (desired.sqrMagnitude > 0.01f && Mathf.Sign(desired.x) != Mathf.Sign(dashDirection.x))
                {
                    dashDirection = desired;
                    player.UseDash();
                    player.FaceDirection(desired.x > 0);
                    return;
                }
            }

            if (dashTimer <= 0f)
            {
                if (Mathf.Abs(input.MoveInput.x) > 0.01f)
                {
                    RequestTransition<MoveState>();
                }
                else
                {
                    RequestTransition<IdleState>();
                }
                return;
            }

            // Can cancel dash into attack after minimum time
            if (dashTimer < dashDuration * 0.5f && input.LightAttackPressed)
            {
                StartAttack(currentForm.GetAttack(FormSO.AttackSlot.Light, characterData.GetDefaultAttack(FormSO.AttackSlot.Light)));
                return;
            }
        }
    }

    /// <summary>
    /// Air dash state
    /// </summary>
    public class AirDashState : PlayerState
    {
        private float dashTimer;
        private float dashDuration;
        private Vector3 dashDirection;
        private int airDashAnimationHash;

        protected override void OnEnterPlayerState()
        {
            airDashAnimationHash = Animator.StringToHash("AirDash");
            animator.CrossFade(airDashAnimationHash, 0.05f);

            dashDirection = GetSideMoveDirection(input.MoveInput);
            if (dashDirection.sqrMagnitude < 0.01f)
            {
                dashDirection = GetFacingDirection();
            }

            dashDuration = characterData.dashDuration * currentForm.dashMultiplier;
            dashTimer = dashDuration;

            player.UseAirDash();
            player.SpawnVFX(player.CharacterData.dashVFXPrefab, player.transform.position);

            // Initial air dash velocity
            float dashSpeed = characterData.dashDistance / dashDuration * currentForm.dashMultiplier;
            rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, 0f, 0f);
        }

        protected override void OnPhysicsPlayerUpdate(float fixedDeltaTime)
        {
            dashTimer -= fixedDeltaTime;

            // Maintain horizontal dash velocity, allow vertical gravity
            float dashSpeed = characterData.dashDistance / dashDuration * currentForm.dashMultiplier;
            Vector3 vel = rb.linearVelocity;
            vel.x = dashDirection.x * dashSpeed;
            vel.z = 0f;
            rb.linearVelocity = vel;
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            if (dashTimer <= 0f || isGrounded)
            {
                RequestTransition<AirState>();
                return;
            }

            // Can attack out of air dash
            if (dashTimer < dashDuration * 0.3f && input.LightAttackPressed)
            {
                StartAttack(currentForm.GetAttack(FormSO.AttackSlot.ForwardAerial, characterData.GetDefaultAttack(FormSO.AttackSlot.ForwardAerial)));
                return;
            }
        }
    }

    /// <summary>
    /// Ledge-grab state (Smash-style): when airborne and drifting into a ledge, the fighter
    /// snaps to hang from it, invulnerable, and can climb up, jump off, or drop. Prevents
    /// cheap ring-outs and creates the signature ledge-guarding/edgeguarding layer.
    /// </summary>
    public class LedgeGrabState : PlayerState
    {
        private Transform ledge;
        private float hangTimer;
        private int ledgeAnimationHash;
        private bool hasGrabbed;

        protected override void OnEnterPlayerState()
        {
            ledgeAnimationHash = Animator.StringToHash("LedgeGrab");
            if (!animator.HasState(0, ledgeAnimationHash))
            {
                ledgeAnimationHash = Animator.StringToHash("Idle");
            }
            animator.CrossFade(ledgeAnimationHash, 0.05f);

            // Snap to the ledge and stop motion
            ledge = player.CurrentLedge;
            if (ledge != null)
            {
                Vector3 snap = ledge.position + Vector3.up * 0.4f;
                player.transform.position = new Vector3(snap.x, snap.y, 0f);
            }
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            player.SetInvulnerable(true);
            hangTimer = 0f;
            hasGrabbed = true;

            player.SpawnVFX(player.CharacterData.dustVFXPrefab, player.transform.position);
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            if (!hasGrabbed) return;
            hangTimer += deltaTime;

            // Drop from ledge (down input)
            if (input.MoveInput.y < -0.5f)
            {
                ReleaseLedge(false);
                return;
            }

            // Jump off ledge (jump with no horizontal commitment = neutral getup)
            if (input.JumpPressed)
            {
                ReleaseLedge(true);
                return;
            }

            // Climb up (toward stage / up input) after a brief hang
            if (hangTimer > 0.2f && (input.MoveInput.y > 0.5f || Mathf.Abs(input.MoveInput.x) > 0.2f))
            {
                ClimbUp();
                return;
            }
        }

        private void ClimbUp()
        {
            rb.isKinematic = false;
            player.SetInvulnerable(false);
            // Pop up onto the platform
            Vector3 up = ledge != null ? ledge.position + Vector3.up * 1.2f : player.transform.position + Vector3.up * 1.2f;
            player.transform.position = new Vector3(up.x, up.y, 0f);
            rb.linearVelocity = new Vector3(0f, 4f, 0f);
            player.ClearHitReaction();
            RequestTransition<IdleState>();
        }

        private void ReleaseLedge(bool jumpOff)
        {
            rb.isKinematic = false;
            player.SetInvulnerable(false);
            if (jumpOff)
            {
                Vector3 dir = GetFacingDirection();
                rb.linearVelocity = new Vector3(dir.x * 6f, 9f, 0f);
            }
            else
            {
                rb.linearVelocity = new Vector3(0f, -2f, 0f); // drop down
            }
            player.ClearHitReaction();
            RequestTransition<AirState>();
        }

        protected override void OnExitPlayerState()
        {
            rb.isKinematic = false;
            player.SetInvulnerable(false);
            player.ClearCurrentLedge();
        }
    }
}
