using UnityEngine;
using KyberKlash.Data;

namespace KyberKlash.Player
{
    /// <summary>
    /// Manages the Resonance/Force meter - building from combat actions, spending on enhanced moves.
    /// NetworkVariable-ready for multiplayer.
    /// </summary>
    public class PlayerMeter : MonoBehaviour
    {
        [Header("Meter Settings")]
        [SerializeField] private float maxMeter = 100f;
        [SerializeField] private float passiveRegenRate = 2f; // per second
        [SerializeField] private float passiveRegenDelay = 3f; // seconds after last action

        // Current state
        private float currentMeter;
        private float lastActionTime;
        private bool isRegenerating;

        // References
        private PlayerController player;
        private CharacterData characterData;
        private FormSO currentForm;

        /// <summary>
        /// Current meter value (0 to maxMeter)
        /// </summary>
        public float CurrentValue => currentMeter;

        /// <summary>
        /// Maximum meter value
        /// </summary>
        public float MaxValue => maxMeter;

        /// <summary>
        /// Meter as percentage (0-1)
        /// </summary>
        public float MeterPercent => currentMeter / maxMeter;

        /// <summary>
        /// Damage percent (0-999%)
        /// </summary>
        public float DamagePercent { get; private set; }

        /// <summary>
        /// Can spend meter for enhanced attacks/specials
        /// </summary>
        public bool CanSpend(float amount) => currentMeter >= amount;

        /// <summary>
        /// Event raised when meter changes
        /// </summary>
        public event System.Action<float, float> OnMeterChanged; // current, max

        /// <summary>
        /// Event raised when damage percent changes
        /// </summary>
        public event System.Action<float> OnDamagePercentChanged; // new percent

        protected virtual void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        /// <summary>
        /// Initialize with character data
        /// </summary>
        public void Initialize(CharacterData data, FormSO form)
        {
            characterData = data;
            currentForm = form;
            currentMeter = 0f;
            DamagePercent = 0f;
            lastActionTime = 0f;
            isRegenerating = false;

            OnMeterChanged?.Invoke(currentMeter, maxMeter);
            OnDamagePercentChanged?.Invoke(DamagePercent);
        }

        /// <summary>
        /// Called when form changes
        /// </summary>
        public void OnFormChanged(FormSO newForm)
        {
            currentForm = newForm;
        }

        protected virtual void Update()
        {
            HandlePassiveRegen();
        }

        /// <summary>
        /// Handle passive meter regeneration
        /// </summary>
        private void HandlePassiveRegen()
        {
            if (currentMeter >= maxMeter) return;

            float timeSinceAction = Time.time - lastActionTime;
            if (timeSinceAction >= passiveRegenDelay)
            {
                if (!isRegenerating)
                {
                    isRegenerating = true;
                }

                float regenRate = passiveRegenRate * currentForm?.meterGainMultiplier ?? 1f;
                GainMeter(regenRate * Time.deltaTime);
            }
            else
            {
                isRegenerating = false;
            }
        }

        /// <summary>
        /// Gain meter (from hits, parries, etc.)
        /// </summary>
        public void GainMeter(float amount)
        {
            if (amount <= 0f) return;

            float previous = currentMeter;
            currentMeter = Mathf.Min(currentMeter + amount, maxMeter);
            lastActionTime = Time.time;

            if (Mathf.Abs(currentMeter - previous) > 0.01f)
            {
                OnMeterChanged?.Invoke(currentMeter, maxMeter);
            }
        }

        /// <summary>
        /// Spend meter (for specials, enhanced attacks)
        /// </summary>
        public bool SpendMeter(float amount)
        {
            if (currentMeter < amount) return false;

            currentMeter -= amount;
            lastActionTime = Time.time;
            isRegenerating = false;

            OnMeterChanged?.Invoke(currentMeter, maxMeter);
            return true;
        }

        /// <summary>
        /// Take damage - increases damage percent
        /// </summary>
        public void TakeDamage(float damage)
        {
            DamagePercent += damage;
            DamagePercent = Mathf.Min(DamagePercent, 999f); // Cap at 999%
            
            // Gain meter from taking damage
            GainMeter(damage * 0.5f * (currentForm?.meterGainMultiplier ?? 1f));

            OnDamagePercentChanged?.Invoke(DamagePercent);
        }

        /// <summary>
        /// Reset meter on respawn
        /// </summary>
        public void ResetMeter()
        {
            currentMeter = 0f;
            DamagePercent = 0f;
            lastActionTime = Time.time;
            isRegenerating = false;

            OnMeterChanged?.Invoke(currentMeter, maxMeter);
            OnDamagePercentChanged?.Invoke(DamagePercent);
        }

        /// <summary>
        /// Add damage percent directly (for testing/debugging)
        /// </summary>
        public void AddDamagePercent(float percent)
        {
            DamagePercent = Mathf.Min(DamagePercent + percent, 999f);
            OnDamagePercentChanged?.Invoke(DamagePercent);
        }

        /// <summary>
        /// Set meter directly (for testing/debugging)
        /// </summary>
        public void SetMeter(float value)
        {
            currentMeter = Mathf.Clamp(value, 0f, maxMeter);
            OnMeterChanged?.Invoke(currentMeter, maxMeter);
        }

        /// <summary>
        /// Set damage percent directly
        /// </summary>
        public void SetDamagePercent(float percent)
        {
            DamagePercent = Mathf.Clamp(percent, 0f, 999f);
            OnDamagePercentChanged?.Invoke(DamagePercent);
        }
    }
}