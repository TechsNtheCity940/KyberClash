using UnityEngine;

namespace KyberKlash.Data
{
    /// <summary>
    /// Core character definition - all characters share this base data structure.
    /// Individual characters are created as ScriptableObject assets from this.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Kyber Clash/Character/Character Data")]
    public class CharacterData : BaseDataSO
    {
        [Header("Character Identity")]
        public string characterId;
        public string displayName;
        [TextArea(2, 4)] public string characterDescription;
        public Sprite characterPortrait;
        public Sprite characterIcon;
        public RuntimeAnimatorController animatorController;

        [Header("Base Stats")]
        [Tooltip("Base movement speed (units/second)")]
        public float baseMoveSpeed = 8f;

        [Tooltip("Base air movement speed")]
        public float baseAirSpeed = 6f;

        [Tooltip("Base jump height (units)")]
        public float baseJumpHeight = 5f;

        [Tooltip("Base gravity scale")]
        public float baseGravity = 1f;

        [Tooltip("Base dash distance")]
        public float baseDashDistance = 6f;

        [Tooltip("Base dash duration in seconds")]
        public float baseDashDuration = 0.2f;

        [Tooltip("Max air jumps")]
        public int baseMaxAirJumps = 1;

        [Tooltip("Can air dash by default")]
        public bool baseCanAirDash = true;

        [Tooltip("Air dash cooldown")]
        public float baseAirDashCooldown = 1f;

        [Tooltip("Can wall jump")]
        public bool baseCanWallJump = false;

        [Tooltip("Coyote time window in seconds")]
        public float coyoteTime = 0.1f;

        [Tooltip("Jump buffer time in seconds")]
        public float jumpBufferTime = 0.1f;

        [Tooltip("Weight class affects knockback taken")]
        public WeightClass weightClass = WeightClass.Medium;

        [Tooltip("Fall speed multiplier")]
        public float fallSpeedMultiplier = 1f;

        [Tooltip("Fast fall speed multiplier")]
        public float fastFallMultiplier = 2f;

        [Header("Default Form")]
        public FormSO defaultForm;

        [Header("Available Forms")]
        [Tooltip("All forms this character can use (unlockable)")]
        public FormSO[] availableForms;

        [Header("Default Attacks (Used when Form doesn't override)")]
        public AttackSO defaultLightAttack;
        public AttackSO defaultHeavyAttack;
        public AttackSO defaultUpAerial;
        public AttackSO defaultDownAerial;
        public AttackSO defaultForwardAerial;
        public AttackSO defaultBackAerial;
        public AttackSO defaultNeutralSpecial;
        public AttackSO defaultSideSpecial;
        public AttackSO defaultUpSpecial;
        public AttackSO defaultDownSpecial;

        [Header("Visuals")]
        public GameObject characterModelPrefab;
        public Material[] modelMaterials;
        public Transform saberEmitterTransform; // Relative to model root
        [Tooltip("HUD/radar accent color for this character.")]
        public Color uiColor = Color.cyan;

        [Header("Audio")]
        public AudioClip[] jumpSounds;
        public AudioClip[] landSounds;
        public AudioClip[] hitSounds;
        public AudioClip[] deathSounds;
        public AudioClip[] tauntSounds;

        [Header("VFX")]
        public GameObject dustVFXPrefab;
        public GameObject landingVFXPrefab;
        public GameObject dashVFXPrefab;
        public GameObject deathVFXPrefab;

        /// <summary>
        /// Weight class determines knockback resistance
        /// </summary>
        public enum WeightClass
        {
            Feather = 0,   // Very light, high knockback taken
            Light = 1,
            Medium = 2,    // Standard
            Heavy = 3,
            SuperHeavy = 4 // Very heavy, low knockback taken
        }

        // Properties for runtime access (with form multipliers applied)
        public float moveSpeed => baseMoveSpeed;
        public float airSpeed => baseAirSpeed;
        public float jumpHeight => baseJumpHeight;
        public float gravity => baseGravity;
        public float dashDistance => baseDashDistance;
        public float dashDuration => baseDashDuration;
        public int maxJumps => baseMaxAirJumps;
        public float groundAcceleration => 20f;
        public float airAcceleration => 10f;

        /// <summary>
        /// Get knockback multiplier based on weight class
        /// </summary>
        public float GetKnockbackMultiplier()
        {
            return weightClass switch
            {
                WeightClass.Feather => 1.4f,
                WeightClass.Light => 1.2f,
                WeightClass.Medium => 1f,
                WeightClass.Heavy => 0.8f,
                WeightClass.SuperHeavy => 0.6f,
                _ => 1f
            };
        }

        /// <summary>
        /// Get default attack for slot (used when Form doesn't override)
        /// </summary>
        public AttackSO GetDefaultAttack(FormSO.AttackSlot slot)
        {
            return slot switch
            {
                FormSO.AttackSlot.Light => defaultLightAttack,
                FormSO.AttackSlot.Heavy => defaultHeavyAttack,
                FormSO.AttackSlot.UpAerial => defaultUpAerial,
                FormSO.AttackSlot.DownAerial => defaultDownAerial,
                FormSO.AttackSlot.ForwardAerial => defaultForwardAerial,
                FormSO.AttackSlot.BackAerial => defaultBackAerial,
                FormSO.AttackSlot.NeutralSpecial => defaultNeutralSpecial,
                FormSO.AttackSlot.SideSpecial => defaultSideSpecial,
                FormSO.AttackSlot.UpSpecial => defaultUpSpecial,
                FormSO.AttackSlot.DownSpecial => defaultDownSpecial,
                _ => null
            };
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(characterId))
                characterId = name;

            if (string.IsNullOrEmpty(displayName))
                displayName = name;

            baseMoveSpeed = Mathf.Max(0.1f, baseMoveSpeed);
            baseAirSpeed = Mathf.Max(0.1f, baseAirSpeed);
            baseJumpHeight = Mathf.Max(0.1f, baseJumpHeight);
            baseGravity = Mathf.Max(0.1f, baseGravity);
            baseDashDistance = Mathf.Max(0f, baseDashDistance);
            baseDashDuration = Mathf.Max(0.01f, baseDashDuration);
            baseMaxAirJumps = Mathf.Max(0, baseMaxAirJumps);
            baseAirDashCooldown = Mathf.Max(0f, baseAirDashCooldown);
            fallSpeedMultiplier = Mathf.Max(0.1f, fallSpeedMultiplier);
            fastFallMultiplier = Mathf.Max(0.1f, fastFallMultiplier);

            if (availableForms == null)
                availableForms = new FormSO[0];

            if (defaultForm != null && !System.Array.Exists(availableForms, f => f == defaultForm))
            {
                // Auto-add default form to available forms
                System.Array.Resize(ref availableForms, availableForms.Length + 1);
                availableForms[availableForms.Length - 1] = defaultForm;
            }
        }
    }
}