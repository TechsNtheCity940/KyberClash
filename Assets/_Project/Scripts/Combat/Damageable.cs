using UnityEngine;
using KyberKlash.Data;

namespace KyberKlash.Combat
{
    /// <summary>
    /// Interface for anything that can take damage
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this entity
        /// </summary>
        void TakeDamage(DamageInfo damageInfo);
        
        /// <summary>
        /// Current health/damage percent
        /// </summary>
        float DamagePercent { get; }
        
        /// <summary>
        /// Is this entity dead/knocked out
        /// </summary>
        bool IsDead { get; }
        
        /// <summary>
        /// GameObject reference for positioning
        /// </summary>
        GameObject GameObject { get; }
    }

    /// <summary>
    /// Component that makes a GameObject damageable
    /// Can be used on players, enemies, destructible objects, etc.
    /// </summary>
    public class Damageable : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool destroyOnDeath = false;
        [SerializeField] private float deathDelay = 1f;

        [Header("Damage Settings")]
        [SerializeField] private bool invulnerable = false;
        [SerializeField] private float invulnerabilityTime = 0.5f;

        private bool isDead = false;
        private float invulnTimer = 0f;

        public float DamagePercent => (1f - currentHealth / maxHealth) * 100f;
        public bool IsDead => isDead;
        public GameObject GameObject => gameObject;

        public event System.Action<DamageInfo> OnDamageTaken;
        public event System.Action OnDeath;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        protected virtual void Update()
        {
            if (invulnTimer > 0f)
            {
                invulnTimer -= Time.deltaTime;
            }
        }

        public virtual void TakeDamage(DamageInfo damageInfo)
        {
            if (invulnerable || invulnTimer > 0f || isDead) return;

            // Apply damage
            currentHealth -= damageInfo.damage;
            currentHealth = Mathf.Max(0f, currentHealth);

            // Set invulnerability
            invulnTimer = invulnerabilityTime;

            // Apply knockback if has Rigidbody
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = damageInfo.knockback;
            }

            // Raise events
            OnDamageTaken?.Invoke(damageInfo);

            if (currentHealth <= 0f && !isDead)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            isDead = true;
            OnDeath?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject, deathDelay);
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        public void SetInvulnerable(bool value, float duration = 0f)
        {
            invulnerable = value;
            if (duration > 0f)
            {
                invulnTimer = duration;
            }
        }
    }

    /// <summary>
    /// Hazard zone - damages entities that enter/stay in it
    /// </summary>
    public class HazardZone : MonoBehaviour
    {
        [Header("Hazard Settings")]
        [SerializeField] private float damagePerSecond = 10f;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private Vector3 knockbackDirection = Vector3.up;
        [SerializeField] private bool damageOnEnter = true;
        [SerializeField] private bool damageOnStay = true;
        [SerializeField] private LayerMask affectedLayers = -1;

        [Header("Visuals")]
        [SerializeField] private GameObject warningVFX;
        [SerializeField] private GameObject activeVFX;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip damageSound;

        [Header("Timing")]
        [SerializeField] private float activationDelay = 1f;
        [SerializeField] private float activeDuration = 3f;
        [SerializeField] private float cooldown = 5f;
        [SerializeField] private bool randomActivation = false;
        [SerializeField] private float randomChance = 0.3f;

        private bool isActive = false;
        private bool isWarning = false;
        private float timer = 0f;
        private Collider zoneCollider;
        private GameObject currentVFX;

        protected virtual void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            if (zoneCollider == null)
            {
                zoneCollider = gameObject.AddComponent<BoxCollider>();
            }
            zoneCollider.isTrigger = true;
        }

        protected virtual void Start()
        {
            if (randomActivation)
            {
                timer = Random.Range(0f, cooldown);
            }
            else
            {
                timer = activationDelay;
            }
        }

        protected virtual void Update()
        {
            if (!isActive && !isWarning)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    ActivateWarning();
                }
            }
            else if (isWarning)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    ActivateHazard();
                }
            }
            else if (isActive)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    DeactivateHazard();
                }
            }
        }

        private void ActivateWarning()
        {
            isWarning = true;
            isActive = false;
            timer = activationDelay;

            if (warningVFX != null)
            {
                currentVFX = Instantiate(warningVFX, transform.position, transform.rotation, transform);
            }
            if (warningSound != null)
            {
                AudioSource.PlayClipAtPoint(warningSound, transform.position);
            }
        }

        private void ActivateHazard()
        {
            isWarning = false;
            isActive = true;
            timer = activeDuration;

            if (currentVFX != null)
            {
                Destroy(currentVFX);
            }
            if (activeVFX != null)
            {
                currentVFX = Instantiate(activeVFX, transform.position, transform.rotation, transform);
            }
        }

        private void DeactivateHazard()
        {
            isActive = false;
            isWarning = false;
            timer = randomActivation ? Random.Range(cooldown * 0.5f, cooldown * 1.5f) : cooldown;

            if (currentVFX != null)
            {
                Destroy(currentVFX);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isActive || !damageOnStay) return;
            if (!IsLayerAffected(other.gameObject.layer)) return;

            DamageEntity(other);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || !damageOnEnter) return;
            if (!IsLayerAffected(other.gameObject.layer)) return;

            DamageEntity(other);
        }

        private bool IsLayerAffected(int layer)
        {
            return (affectedLayers.value & (1 << layer)) != 0;
        }

        private void DamageEntity(Collider other)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable == null) return;

            DamageInfo damageInfo = new DamageInfo
            {
                attacker = gameObject,
                victim = damageable.GameObject,
                damage = damagePerSecond * Time.deltaTime,
                knockback = knockbackDirection.normalized * knockbackForce,
                hitPoint = other.ClosestPoint(transform.position),
                hitNormal = knockbackDirection.normalized,
                attackData = null,
                isPerfectParry = false,
                isCounter = false
            };

            damageable.TakeDamage(damageInfo);

            if (damageSound != null)
            {
                AudioSource.PlayClipAtPoint(damageSound, transform.position, 0.5f);
            }
        }

        /// <summary>
        /// Force activate hazard (for scripted events)
        /// </summary>
        public void ForceActivate(float duration = -1f)
        {
            if (duration > 0f) activeDuration = duration;
            ActivateWarning();
        }

        /// <summary>
        /// Force deactivate hazard
        /// </summary>
        public void ForceDeactivate()
        {
            DeactivateHazard();
            timer = cooldown;
        }
    }
}