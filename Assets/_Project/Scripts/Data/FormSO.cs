using UnityEngine;

namespace KyberKlash.Data
{
    /// <summary>
    /// Defines a fighting style/Form that modifies a character's moveset and movement properties.
    /// Forms are swappable at runtime (character select, or mid-match with meter cost).
    /// </summary>
    [CreateAssetMenu(fileName = "NewForm", menuName = "Kyber Clash/Character/Form Data")]
    public class FormSO : BaseDataSO
    {
        [Header("Form Identity")]
        public string formId;
        public string displayName;
        [TextArea(2, 4)] public string loreDescription;

        [Header("Visual Identity")]
        public Color saberColor = Color.blue;
        public Color saberCoreColor = Color.white;
        public Gradient saberGradient;
        public Material saberMaterial;
        public GameObject formVFXPrefab;

        [Header("Movement Modifiers")]
        [Tooltip("Multiplier for ground move speed")]
        public float moveSpeedMultiplier = 1f;

        [Tooltip("Multiplier for air move speed")]
        public float airSpeedMultiplier = 1f;

        [Tooltip("Multiplier for jump height")]
        public float jumpHeightMultiplier = 1f;

        [Tooltip("Multiplier for gravity scale")]
        public float gravityMultiplier = 1f;

        [Tooltip("Multiplier for dash distance/speed")]
        public float dashMultiplier = 1f;

        [Tooltip("Max air jumps (0 = no double jump)")]
        public int maxAirJumps = 1;

        [Tooltip("Can air dash?")]
        public bool canAirDash = true;

        [Tooltip("Air dash cooldown in seconds")]
        public float airDashCooldown = 1f;

        [Tooltip("Can wall jump?")]
        public bool canWallJump = false;

        [Header("Combat Modifiers")]
        [Tooltip("Global damage multiplier for all attacks")]
        public float damageMultiplier = 1f;

        [Tooltip("Global knockback multiplier")]
        public float knockbackMultiplier = 1f;

        [Tooltip("Attack speed multiplier (affects frame data)")]
        public float attackSpeedMultiplier = 1f;

        [Tooltip("Meter gain multiplier")]
        public float meterGainMultiplier = 1f;

        [Tooltip("Meter cost multiplier for specials")]
        public float meterCostMultiplier = 1f;

        [Header("Unique Form Attacks")]
        [Tooltip("Light attack override (null = use character default)")]
        public AttackSO lightAttack;

        [Tooltip("Heavy attack override")]
        public AttackSO heavyAttack;

        [Tooltip("Up aerial override")]
        public AttackSO upAerial;

        [Tooltip("Down aerial override")]
        public AttackSO downAerial;

        [Tooltip("Forward aerial override")]
        public AttackSO forwardAerial;

        [Tooltip("Back aerial override")]
        public AttackSO backAerial;

        [Tooltip("Neutral special override")]
        public AttackSO neutralSpecial;

        [Tooltip("Side special override")]
        public AttackSO sideSpecial;

        [Tooltip("Up special override")]
        public AttackSO upSpecial;

        [Tooltip("Down special override")]
        public AttackSO downSpecial;

        [Header("Unique Mechanics")]
        [Tooltip("Special mechanic type for this form")]
        public FormMechanicType mechanicType = FormMechanicType.None;

        [Tooltip("Data for the unique mechanic (varies by type)")]
        public FormMechanicData mechanicData;

        [Header("Meter")]
        [Tooltip("Passive meter gain per second")]
        public float passiveMeterGain = 0f;

        [Tooltip("Meter gained on hit")]
        public float meterGainOnHit = 5f;

        [Tooltip("Meter gained on perfect parry")]
        public float meterGainOnPerfectParry = 20f;

        /// <summary>
        /// Get attack for a specific input, falling back to character defaults
        /// </summary>
        public AttackSO GetAttack(AttackSlot slot, AttackSO characterDefault)
        {
            AttackSO formAttack = GetAttackForSlot(slot);
            return formAttack != null ? formAttack : characterDefault;
        }

        private AttackSO GetAttackForSlot(AttackSlot slot)
        {
            return slot switch
            {
                AttackSlot.Light => lightAttack,
                AttackSlot.Heavy => heavyAttack,
                AttackSlot.UpAerial => upAerial,
                AttackSlot.DownAerial => downAerial,
                AttackSlot.ForwardAerial => forwardAerial,
                AttackSlot.BackAerial => backAerial,
                AttackSlot.NeutralSpecial => neutralSpecial,
                AttackSlot.SideSpecial => sideSpecial,
                AttackSlot.UpSpecial => upSpecial,
                AttackSlot.DownSpecial => downSpecial,
                _ => null
            };
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(formId))
                formId = name;

            if (string.IsNullOrEmpty(displayName))
                displayName = name;

            moveSpeedMultiplier = Mathf.Max(0.1f, moveSpeedMultiplier);
            airSpeedMultiplier = Mathf.Max(0.1f, airSpeedMultiplier);
            jumpHeightMultiplier = Mathf.Max(0.1f, jumpHeightMultiplier);
            gravityMultiplier = Mathf.Max(0.1f, gravityMultiplier);
            dashMultiplier = Mathf.Max(0.1f, dashMultiplier);
            maxAirJumps = Mathf.Max(0, maxAirJumps);
            airDashCooldown = Mathf.Max(0f, airDashCooldown);

            damageMultiplier = Mathf.Max(0f, damageMultiplier);
            knockbackMultiplier = Mathf.Max(0f, knockbackMultiplier);
            attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
            meterGainMultiplier = Mathf.Max(0f, meterGainMultiplier);
            meterCostMultiplier = Mathf.Max(0f, meterCostMultiplier);
        }

        /// <summary>
        /// Attack slot types for mapping inputs to attacks
        /// </summary>
        public enum AttackSlot
        {
            Light,
            Heavy,
            UpAerial,
            DownAerial,
            ForwardAerial,
            BackAerial,
            NeutralSpecial,
            SideSpecial,
            UpSpecial,
            DownSpecial
        }

        /// <summary>
        /// Unique mechanic types per Form
        /// </summary>
        public enum FormMechanicType
        {
            None,
            /// <summary>
            /// Form Shii-Cho: Balanced, generates meter on block
            /// </summary>
            CounterStance,
            /// <summary>
            /// Form Makashi: Dueling, precise parries, ripostes
            /// </summary>
            PrecisionParry,
            /// <summary>
            /// Form Soresu: Defensive, perfect parries reflect projectiles
            /// </summary>
            PerfectDeflection,
            /// <summary>
            /// Form Ataru: Acrobatic, air mobility, combo extender
            /// </summary>
            AcrobaticFlow,
            /// <summary>
            /// Form Shien/Djem So: Power, counter-attacks, heavy hits
            /// </summary>
            PowerCounter,
            /// <summary>
            /// Form Niman: Balanced, Force powers, utility
            /// </summary>
            ForceUtility,
            /// <summary>
            /// Form Juyo/Vaapad: Aggressive, high risk/reward, meter drain for power
            /// </summary>
            BerserkerTrance,
            /// <summary>
            /// Custom mechanic defined in FormMechanicData
            /// </summary>
            Custom
        }

        /// <summary>
        /// Serializable data for form-specific mechanics
        /// </summary>
        [System.Serializable]
        public class FormMechanicData
        {
            [Header("Counter Stance (Shii-Cho)")]
            public float counterWindowFrames = 8f;
            public float counterMeterGain = 10f;
            public AttackSO counterAttack;

            [Header("Precision Parry (Makashi)")]
            public float precisionWindowFrames = 4f;
            public float riposteDamageMultiplier = 1.5f;
            public float riposteKnockbackMultiplier = 1.3f;

            [Header("Perfect Deflection (Soresu)")]
            public bool canDeflectProjectiles = true;
            public float deflectionAngle = 45f;
            public AttackSO reflectedProjectileAttack;

            [Header("Acrobatic Flow (Ataru)")]
            public float comboWindowExtension = 0.2f;
            public int maxAerialChains = 3;
            public float aerialMomentumPreservation = 0.8f;

            [Header("Power Counter (Shien/Djem So)")]
            public float counterAbsorbThreshold = 15f;
            public float counterDamageBonus = 1.5f;
            public float counterKnockbackBonus = 1.5f;

            [Header("Force Utility (Niman)")]
            public float forcePushCooldown = 5f;
            public float forcePushForce = 20f;
            public float forcePullRange = 10f;
            public float forcePullForce = 15f;

            [Header("Berserker Trance (Juyo/Vaapad)")]
            public float tranceMeterCostPerSecond = 10f;
            public float tranceDamageBonus = 2f;
            public float tranceSpeedBonus = 1.3f;
            public float tranceArmorThreshold = 20f;
        }
    }
}