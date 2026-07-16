using UnityEngine;
using KyberKlash.Core;
using KyberKlash.Data;

namespace KyberKlash.Player.States
{
    /// <summary>
    /// Grab / Throw state (Smash-style). On entry, captures the nearest opponent in
    /// range; holds them briefly (velocity frozen, pulled toward the grabber), then
    /// throws them with an upward+outward knockback. Re-pressing grab throws early.
    /// </summary>
    public class GrabState : PlayerState
    {
        private PlayerController victim;
        private float holdTimer;
        private float holdDuration = 0.45f;
        private bool thrown;
        private int grabAnimationHash;

        private const float GrabRange = 2.2f;
        private const float ThrowHorizontal = 9f;
        private const float ThrowVertical = 11f;
        private const float ThrowDamage = 8f;

        protected override void OnEnterPlayerState()
        {
            grabAnimationHash = Animator.StringToHash("Grab");
            if (!animator.HasState(0, grabAnimationHash))
            {
                grabAnimationHash = Animator.StringToHash("Attack");
            }
            animator.CrossFade(grabAnimationHash, 0.05f);

            victim = null;
            thrown = false;
            holdTimer = holdDuration;

            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                victim = gameManager.GetNearestOpponent(player.transform.position, player, GrabRange);
            }

            if (victim == null)
            {
                RequestTransition<IdleState>();
                return;
            }

            player.IgniteSaber();
            player.PlaySound("Grab");
        }

        protected override void OnPhysicsPlayerUpdate(float fixedDeltaTime)
        {
            if (victim == null) return;

            // Keep the grabber roughly stationary during the hold.
            Vector3 holdVel = rb.linearVelocity;
            holdVel.x *= 0.6f;
            rb.linearVelocity = holdVel;

            // Pull the victim toward the grabber and freeze their fall.
            Vector3 toGrabber = player.transform.position - victim.transform.position;
            toGrabber.z = 0f;
            victim.Rigidbody.linearVelocity = toGrabber * 6f;
            victim.transform.position = Vector3.Lerp(victim.transform.position,
                player.transform.position + Vector3.right * (player.transform.localScale.x >= 0f ? 1f : -1f) * 1.1f,
                0.4f);
        }

        protected override void OnUpdatePlayerLogic(float deltaTime)
        {
            if (victim == null || victim.IsDead)
            {
                RequestTransition<IdleState>();
                return;
            }

            if (!thrown)
            {
                holdTimer -= deltaTime;
                if (input.GrabPressed || holdTimer <= 0f)
                {
                    ThrowVictim();
                }
            }
            else
            {
                // Brief recovery after the throw, then return to action.
                holdTimer -= deltaTime;
                if (holdTimer <= -0.15f)
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
        }

        private void ThrowVictim()
        {
            thrown = true;
            holdTimer = 0f;

            float facing = player.transform.localScale.x >= 0f ? 1f : -1f;
            Vector3 throwDir = new Vector3(facing, 0f, 0f).normalized;
            float angle = 35f * Mathf.Deg2Rad;
            Vector3 knockback = new Vector3(
                throwDir.x * Mathf.Cos(angle) * ThrowHorizontal,
                Mathf.Sin(angle) * ThrowVertical + ThrowVertical * 0.4f,
                0f);

            var damageInfo = new DamageInfo
            {
                attacker = player.gameObject,
                victim = victim.gameObject,
                damage = ThrowDamage,
                knockback = knockback,
                hitPoint = victim.transform.position,
                hitNormal = Vector3.up,
                attackData = null,
                isPerfectParry = false,
                isCounter = false
            };

            victim.TakeDamage(damageInfo);
            player.PlaySound("Throw");
            player.ScreenShake(0.08f, 0.3f);
        }
    }
}
