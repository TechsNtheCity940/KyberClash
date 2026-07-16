using UnityEngine;

namespace KyberKlash.Data
{
    /// <summary>
    /// Defines a single attack's properties in a completely data-driven way.
    /// All attack configuration lives here - no hardcoded values in combat logic.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttack", menuName = "Kyber Clash/Combat/Attack Data")]
    public class AttackSO : BaseDataSO
    {
        [Header("Attack Identity")]
        [Tooltip("Unique identifier for this attack type")]
        public string attackId;

        [Tooltip("Human-readable name for debugging/UI")]
        public string displayName;

        [Header("Frame Data (at 60fps)")]
        [Tooltip("Frames before hitbox becomes active")]
        public int startupFrames = 5;

        [Tooltip("Frames hitbox stays active")]
        public int activeFrames = 3;

        [Tooltip("Frames before player can act again")]
        public int recoveryFrames = 10;

        [Header("Damage & Knockback")]
        [Tooltip("Base damage percentage dealt")]
        public float baseDamage = 10f;

        [Tooltip("Base knockback velocity magnitude")]
        public float baseKnockback = 15f;

        [Tooltip("Knockback angle in degrees (0 = horizontal, 90 = straight up)")]
        [Range(0f, 360f)] public float knockbackAngle = 45f;

        [Tooltip("Knockback growth per 1% target damage")]
        public float knockbackGrowth = 0.05f;

        [Tooltip("Minimum knockback regardless of damage")]
        public float minKnockback = 5f;

        [Tooltip("Maximum knockback cap")]
        public float maxKnockback = 50f;

        [Header("Hitbox")]
        [Tooltip("Shape of the hitbox")]
        public HitboxShape hitboxShape = HitboxShape.Box;

        [Tooltip("Size of hitbox (radius for sphere, half-extents for box)")]
        public Vector3 hitboxSize = new Vector3(1f, 1f, 2f);

        [Tooltip("Offset from attack origin (saber tip, hand, etc.)")]
        public Vector3 hitboxOffset = new Vector3(0f, 0f, 1f);

        [Tooltip("Rotation offset for hitbox")]
        public Vector3 hitboxRotation = Vector3.zero;

        [Header("Meter Interaction")]
        [Tooltip("Meter gained on hit (positive) or spent (negative)")]
        public float meterGainOnHit = 5f;

        [Tooltip("Meter gained on whiff")]
        public float meterGainOnWhiff = 1f;

        [Tooltip("Meter cost to perform (for specials)")]
        public float meterCost = 0f;

        [Tooltip("Requires minimum meter to execute")]
        public float minMeterRequired = 0f;

        [Header("Hit Effects")]
        [Tooltip("Hit pause frames (game freeze on impact)")]
        public int hitPauseFrames = 4;

        [Tooltip("Hit stun frames (victim cannot act)")]
        public int hitStunFrames = 20;

        [Tooltip("Block stun frames (if parried/blocked)")]
        public int blockStunFrames = 10;

        [Tooltip("Pushback distance on block")]
        public float blockPushback = 2f;

        [Header("Visuals & Audio")]
        [Tooltip("Animation trigger/parameter name")]
        public string animationTrigger = "Attack";

        [Tooltip("VFX prefab to spawn on hit")]
        public GameObject hitVFXPrefab;

        [Tooltip("SFX to play on hit")]
        public AudioClip hitSFX;

        [Tooltip("VFX prefab for clash (when attacks collide)")]
        public GameObject clashVFXPrefab;

        [Tooltip("Trail effect during active frames")]
        public GameObject trailEffectPrefab;

        [Header("Parry Interaction")]
        [Tooltip("Can this attack be parried?")]
        public bool canBeParried = true;

        [Tooltip("Perfect parry window in frames")]
        public int perfectParryWindowFrames = 8;

        [Tooltip("Reward meter on perfect parry")]
        public float perfectParryMeterReward = 15f;

        [Header("Advanced")]
        [Tooltip("Does this attack have armor/super armor frames?")]
        public bool hasArmor = false;

        [Tooltip("Armor frames (subset of active frames)")]
        public int armorFrames = 0;

        [Tooltip("Maximum hits per attack (for multi-hit moves)")]
        public int maxHits = 1;

        [Tooltip("Time between multi-hits in frames")]
        public int multiHitIntervalFrames = 5;

        /// <summary>
        /// Total frames for this attack
        /// </summary>
        public int TotalFrames => startupFrames + activeFrames + recoveryFrames;

        /// <summary>
        /// Convert startup frames to seconds
        /// </summary>
        public float StartupTime => startupFrames / 60f;

        /// <summary>
        /// Convert active frames to seconds
        /// </summary>
        public float ActiveTime => activeFrames / 60f;

        /// <summary>
        /// Convert recovery frames to seconds
        /// </summary>
        public float RecoveryTime => recoveryFrames / 60f;

        /// <summary>
        /// Calculate knockback for a target at given damage percentage
        /// </summary>
        public float CalculateKnockback(float targetDamagePercent)
        {
            float kb = baseKnockback + (targetDamagePercent * knockbackGrowth);
            return Mathf.Clamp(kb, minKnockback, maxKnockback);
        }

        /// <summary>
        /// Calculate knockback vector based on attacker facing direction
        /// </summary>
        public Vector3 CalculateKnockbackVector(float targetDamagePercent, bool attackerFacingRight, bool isGrounded)
        {
            float magnitude = CalculateKnockback(targetDamagePercent);
            float angleRad = knockbackAngle * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * magnitude * (attackerFacingRight ? 1f : -1f);
            float y = Mathf.Sin(angleRad) * magnitude;

            // Reduce vertical knockback if target is grounded (Smash-style)
            if (isGrounded && knockbackAngle > 10f)
            {
                y *= 0.7f;
            }

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Validate attack data in editor
        /// </summary>
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(attackId))
                attackId = name;

            if (string.IsNullOrEmpty(displayName))
                displayName = name;

            startupFrames = Mathf.Max(0, startupFrames);
            activeFrames = Mathf.Max(1, activeFrames);
            recoveryFrames = Mathf.Max(0, recoveryFrames);

            baseDamage = Mathf.Max(0f, baseDamage);
            baseKnockback = Mathf.Max(0f, baseKnockback);
        }

        /// <summary>
        /// Hitbox shape types for collision detection
        /// </summary>
        public enum HitboxShape
        {
            Sphere,
            Box,
            Capsule,
            /// <summary>
            /// Arc shape for lightsaber sweeps - defined by arc angle and radius
            /// </summary>
            SaberArc
        }
    }
}