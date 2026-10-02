using UnityEngine;
using KyberKlash.Data;
using KyberKlash.Combat;

namespace KyberKlash.Player.States
{
    /// <summary>
    /// Attack state - handles all attack types with frame data
    /// </summary>
    public class AttackState : PlayerState
    {
        private AttackSO currentAttack;
        private int currentFrame;
        private int totalFrames;
        private bool hitboxActive;
        private int hitsDealt;
        private GameObject trailEffect;
        private int attackAnimationHash;
        private int startupFrames;
        private int activeFrames;
        private int recoveryFrames;

        protected override void OnEnterPlayerState()
        {
            // Get the attack that triggered this state (passed via combat component)
            currentAttack = combat.CurrentAttack;
            if (currentAttack == null)
            {
                RequestTransition<IdleState>();
                return;
            }

            // Calculate frame data
            this.startupFrames = Mathf.RoundToInt(currentAttack.startupFrames / currentForm.attackSpeedMultiplier);
            this.activeFrames = Mathf.RoundToInt(currentAttack.activeFrames / currentForm.attackSpeedMultiplier);
            this.recoveryFrames = Mathf.RoundToInt(currentAttack.recoveryFrames / currentForm.attackSpeedMultiplier);
            totalFrames = this.startupFrames + this.activeFrames + this.recoveryFrames;
            currentFrame = 0;
            hitboxActive = false;
            hitsDealt = 0;

            // Play animation
            attackAnimationHash = Animator.StringToHash(currentAttack.animationTrigger);
            animator.CrossFade(attackAnimationHash, 0.02f);
            animator.speed = currentForm.attackSpeedMultiplier;
            player.IgniteSaber();

            // Apply meter cost
            if (currentAttack.meterCost > 0f)
            {
                meter.SpendMeter(currentAttack.meterCost);
            }

            // Spawn trail effect
            if (currentAttack.trailEffectPrefab != null)
            {
                trailEffect = Object.Instantiate(currentAttack.trailEffectPrefab, player.SaberTip.position, player.SaberTip.rotation, player.SaberTip);
            }

            // Face attack direction if no movement input
            if (input.MoveInput.sqrMagnitude < 0.01f)
            {
                // Already facing correct direction from movement state
            }
        }

        protected override void OnPhysicsPlayerUpdate(float fixedDeltaTime)
        {
            currentFrame++;

            // Activate hitbox
            if (currentFrame == startupFrames)
            {
                hitboxActive = true;
                combat.ActivateHitbox(currentAttack);
            }

            // Deactivate hitbox
            if (currentFrame == startupFrames + activeFrames)
            {
                hitboxActive = false;
                combat.DeactivateHitbox();
            }

            // Handle multi-hit
            if (hitboxActive && currentAttack.maxHits > 1 && hitsDealt < currentAttack.maxHits)
            {
                int multiHitInterval = Mathf.RoundToInt(currentAttack.multiHitIntervalFrames / currentForm.attackSpeedMultiplier);
                if ((currentFrame - startupFrames) % multiHitInterval == 0)
                {
                    // Re-activate for multi-hit
                    combat.ReactivateHitbox();
                }
            }

            // Apply root motion or movement during attack
            if (currentFrame < startupFrames + activeFrames)
            {
                // Allow slight movement during startup/active
                Vector2 moveInput = input.MoveInput;
                if (Mathf.Abs(moveInput.x) > 0.01f && !isGrounded)
                {
                    Vector3 moveDir = GetSideMoveDirection(moveInput);
                    float airSpeed = characterData.airSpeed * currentForm.airSpeedMultiplier * 0.3f; // Reduced during attack
                    Vector3 targetVel = moveDir * airSpeed;
                    targetVel.y = rb.linearVelocity.y;
                    targetVel.z = 0f;
                    Vector3 velocity = Vector3.Lerp(rb.linearVelocity, targetVel, characterData.airAcceleration * fixedDeltaTime);
                    velocity.z = 0f;
                    rb.linearVelocity = velocity;
                }
            }
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            // Check for hit pause (handled by combat system via time scale)

            // Check for parry during recovery (special mechanic)
            if (currentForm.mechanicType == FormSO.FormMechanicType.PrecisionParry &&
                currentFrame > startupFrames + activeFrames && input.ParryPressed)
            {
                if (combat.CanParry())
                {
                    combat.StartParry();
                    RequestTransition<ParryState>();
                    return;
                }
            }

            // Check for attack cancel into special (if allowed)
            if (currentFrame > startupFrames + activeFrames && input.SpecialPressed)
            {
                AttackSO special = currentForm.GetAttack(FormSO.AttackSlot.NeutralSpecial, characterData.GetDefaultAttack(FormSO.AttackSlot.NeutralSpecial));
                if (CanAttack(special))
                {
                    StartAttack(special);
                    return;
                }
            }

            // Attack complete
            if (currentFrame >= totalFrames)
            {
                OnAttackComplete();
            }
        }

        private void OnAttackComplete()
        {
            // Grant meter for whiff
            if (hitsDealt == 0 && currentAttack.meterGainOnWhiff > 0f)
            {
                meter.GainMeter(currentAttack.meterGainOnWhiff * currentForm.meterGainMultiplier);
            }

            // Transition based on state
            if (isGrounded)
            {
                if (Mathf.Abs(input.MoveInput.x) > 0.01f)
                    RequestTransition<MoveState>();
                else
                    RequestTransition<IdleState>();
            }
            else
            {
                RequestTransition<AirState>();
            }
        }

        protected override void OnExitPlayerState()
        {
            hitboxActive = false;
            combat.DeactivateHitbox();
            animator.speed = 1f;

            if (trailEffect != null)
            {
                Object.Destroy(trailEffect);
            }
        }

        /// <summary>
        /// Called by combat system when hit connects
        /// </summary>
        public void OnHitConnected(DamageInfo damageInfo)
        {
            hitsDealt++;
            
            // Track aerial attacks for AcrobaticFlow
            if (!isGrounded)
            {
                var mechanicHandler = player.GetComponent<FormMechanicHandler>();
                mechanicHandler?.RegisterAerialAttack();
            }

            // Grant meter
            float meterGain = currentAttack.meterGainOnHit * currentForm.meterGainMultiplier;
            if (damageInfo.isPerfectParry)
            {
                meterGain = currentAttack.perfectParryMeterReward * currentForm.meterGainMultiplier;
            }
            meter.GainMeter(meterGain);

            // Play hit SFX
            if (currentAttack.hitSFX != null)
            {
                player.PlaySound(currentAttack.hitSFX.name);
            }
            else
            {
                player.PlaySound("Hit"); // Fallback
            }

            // Hit pause using HitPauseSystem
            if (currentAttack.hitPauseFrames > 0)
            {
                HitPauseSystem.Instance?.RequestHitPause(currentAttack.hitPauseFrames);
            }

            // Screen shake
            player.ScreenShake(currentAttack.hitPauseFrames * 0.02f, 0.3f);
        }

        // Remove the old HitPauseRoutine
    }

    /// <summary>
    /// Parry/Block state - timed defensive action
    /// </summary>
    public class ParryState : PlayerState
    {
        private float parryTimer;
        private float parryWindow;
        private bool perfectParryWindow;
        private bool hasParried;
        private int parryAnimationHash;
        private GameObject parryVFX;

        protected override void OnEnterPlayerState()
        {
            parryAnimationHash = Animator.StringToHash("Parry");
            animator.CrossFade(parryAnimationHash, 0.01f);
            player.IgniteSaber();

            // Parry window from current form
            parryWindow = currentForm.mechanicData != null ?
                currentForm.mechanicData.counterWindowFrames / 60f : 0.3f;

            perfectParryWindow = currentForm.mechanicData != null &&
                currentForm.mechanicData.precisionWindowFrames > 0;

            parryTimer = parryWindow;
            hasParried = false;

            // Visual feedback
            parryVFX = player.SpawnVFX(player.CharacterData.dashVFXPrefab, player.transform.position);
            if (parryVFX != null)
            {
                var ps = parryVFX.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = currentForm.saberColor;
                }
            }
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            parryTimer -= deltaTime;

            // Check for successful parry (handled by combat when hit)
            if (hasParried)
            {
                // Stay in parry state briefly for visual
                if (parryTimer <= parryWindow * 0.5f)
                {
                    OnParryComplete();
                }
                return;
            }

            // Window expired
            if (parryTimer <= 0f)
            {
                OnParryComplete();
            }

            // Can cancel parry into movement (after minimum time)
            if (parryTimer < parryWindow * 0.7f)
            {
                if (Mathf.Abs(input.MoveInput.x) > 0.01f)
                {
                    RequestTransition<MoveState>();
                    return;
                }
                if (input.JumpPressed)
                {
                    RequestTransition<JumpState>();
                    return;
                }
            }
        }

        private void OnParryComplete()
        {
            if (isGrounded)
            {
                if (Mathf.Abs(input.MoveInput.x) > 0.01f)
                    RequestTransition<MoveState>();
                else
                    RequestTransition<IdleState>();
            }
            else
            {
                RequestTransition<AirState>();
            }
        }

        protected override void OnExitPlayerState()
        {
            if (parryVFX != null)
            {
                Object.Destroy(parryVFX);
            }
        }

        /// <summary>
        /// Called by combat when parry succeeds
        /// </summary>
        public void OnParrySuccess(bool perfect, DamageInfo damageInfo)
        {
            hasParried = true;

            if (perfect)
            {
                // Perfect parry rewards
                float meterGain = currentForm.meterGainOnPerfectParry;
                if (currentForm.mechanicData != null && currentForm.mechanicType == FormSO.FormMechanicType.PrecisionParry)
                {
                    meterGain *= 1.5f; // Makashi bonus
                }
                meter.GainMeter(meterGain);

                // Riposte opportunity
                if (currentForm.mechanicType == FormSO.FormMechanicType.PrecisionParry)
                {
                    combat.EnableRiposte(damageInfo.attacker, currentForm.mechanicData.riposteDamageMultiplier);
                }

                // Visual/audio feedback
                player.ScreenShake(0.1f, 0.5f);
                player.PlaySound("PerfectParry");
            }
            else
            {
                // Normal parry
                player.ScreenShake(0.05f, 0.2f);
                player.PlaySound("Parry");
            }
        }
    }

    /// <summary>
    /// Hit stun state - brief immobilization after being hit
    /// </summary>
    public class HitStunState : PlayerState
    {
        private float hitStunTimer;
        private int hitStunAnimationHash;

        protected override void OnEnterPlayerState()
        {
            hitStunAnimationHash = Animator.StringToHash("HitStun");
            animator.CrossFade(hitStunAnimationHash, 0.02f);

            // Get hit stun duration from last attack
            hitStunTimer = player.LastHitStunDuration;
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            hitStunTimer -= deltaTime;

            if (hitStunTimer <= 0f)
            {
                if (isGrounded)
                {
                    RequestTransition<IdleState>();
                }
                else
                {
                    RequestTransition<AirState>();
                }
            }
        }

        protected override void OnExitPlayerState()
        {
            player.ClearHitStun();
        }
    }

    /// <summary>
    /// Knockback state - launched by strong attacks
    /// </summary>
    public class KnockbackState : PlayerState
    {
        private float knockbackTimer;
        private Vector3 knockbackVelocity;
        private bool hitGround;
        private int knockbackAnimationHash;

        protected override void OnEnterPlayerState()
        {
            knockbackAnimationHash = Animator.StringToHash("Launch");
            if (!animator.HasState(0, knockbackAnimationHash))
            {
                knockbackAnimationHash = Animator.StringToHash("Knockback");
            }
            animator.CrossFade(knockbackAnimationHash, 0.02f);

            knockbackVelocity = player.LastKnockbackVelocity;
            knockbackTimer = 1f; // Max time before transitioning to air state
            hitGround = false;

            rb.linearVelocity = knockbackVelocity;
        }

        protected override void OnPhysicsPlayerUpdate(float fixedDeltaTime)
        {
            // Apply gravity during knockback
            rb.linearVelocity += Vector3.up * Physics.gravity.y * characterData.gravity * fixedDeltaTime;

            // Check for ground impact
            if (isGrounded && !hitGround)
            {
                hitGround = true;
                OnGroundImpact();
            }
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            knockbackTimer -= deltaTime;

            // Can tech (recover) after minimum time
            if (knockbackTimer < 0.5f && input.ParryPressed && isGrounded)
            {
                // Tech/Ukemi
                player.PerformTech();
                RequestTransition<IdleState>();
                return;
            }

            // Transition to air state if airborne
            if (!isGrounded && hitGround)
            {
                RequestTransition<AirState>();
                return;
            }

            // Auto-transition after timer
            if (knockbackTimer <= 0f)
            {
                if (isGrounded)
                {
                    RequestTransition<IdleState>();
                }
                else
                {
                    RequestTransition<AirState>();
                }
            }
        }

        private void OnGroundImpact()
        {
            // Bounce or slide based on velocity
            float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);

            if (horizontalSpeed > 10f)
            {
                // Slide
                rb.linearVelocity *= 0.5f;
                player.SpawnVFX(player.CharacterData.dustVFXPrefab, player.transform.position + Vector3.down * 0.5f);
            }
            else
            {
                // Stop
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }

        protected override void OnExitPlayerState()
        {
            player.ClearKnockback();
        }
    }

    /// <summary>
    /// Dead state - ring out or KO
    /// </summary>
    public class DeadState : PlayerState
    {
        private float respawnTimer;
        private int deathAnimationHash;

        protected override void OnEnterPlayerState()
        {
            deathAnimationHash = Animator.StringToHash("Death");
            animator.CrossFade(deathAnimationHash, 0.1f);
            player.ExtinguishSaber();

            // Disable physics
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            player.Collider.enabled = false;

            // Death VFX and SFX
            player.SpawnVFX(player.CharacterData.deathVFXPrefab, player.transform.position);
            player.PlayRandomSound(player.CharacterData.deathSounds);

            respawnTimer = 3f; // Time before respawn

            // Notify game manager
            player.OnDeath();
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            respawnTimer -= deltaTime;

            if (respawnTimer <= 0f)
            {
                RequestTransition<RespawnState>();
            }
        }
    }

    /// <summary>
    /// Respawn state - brief invulnerability after respawning
    /// </summary>
    public class RespawnState : PlayerState
    {
        private float invulnTimer;
        private float invulnDuration = 2f;
        private int respawnAnimationHash;

        protected override void OnEnterPlayerState()
        {
            respawnAnimationHash = Animator.StringToHash("Respawn");
            animator.CrossFade(respawnAnimationHash, 0.1f);

            // Reset position to spawn point
            player.Respawn();

            // Re-enable physics
            rb.isKinematic = false;
            player.Collider.enabled = true;

            // Invulnerability
            player.SetInvulnerable(true);
            invulnTimer = invulnDuration;

            // Visual indicator
            player.StartInvulnerabilityFlash();
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            invulnTimer -= deltaTime;

            if (invulnTimer <= 0f)
            {
                player.SetInvulnerable(false);
                player.StopInvulnerabilityFlash();

                if (Mathf.Abs(input.MoveInput.x) > 0.01f)
                    RequestTransition<MoveState>();
                else
                    RequestTransition<IdleState>();
            }
        }
    }
}
