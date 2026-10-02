using UnityEngine;
using KyberKlash.Data;
using KyberKlash.Combat;
using KyberKlash.Core;

namespace KyberKlash.Player
{
    /// <summary>
    /// Handles form-specific mechanic logic for each FormSO.FormMechanicType.
    /// Attached to PlayerController, receives OnMechanicActivate events from animations.
    /// </summary>
    public class FormMechanicHandler : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        // References
        private PlayerController player;
        private PlayerCombat combat;
        private PlayerMeter meter;
        
        // State
        private bool isTranceActive = false;
        private float tranceTimer = 0f;
        private int aerialChainCount = 0;
        private bool riposteReady = false;
        private GameObject pendingRiposteTarget;
        private float riposteDamageMultiplier = 1.5f;
        private float riposteKnockbackMultiplier = 1f;
        
        // Events
        public event System.Action<string> OnMechanicTriggered;
        
        private void Awake()
        {
            player = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            meter = GetComponent<PlayerMeter>();
        }
        
        private void Update()
        {
            if (!isTranceActive) return;
            
            // Berserker Trance: Drain meter while active
            if (player.CurrentForm.mechanicType == FormSO.FormMechanicType.BerserkerTrance)
            {
                float cost = player.CurrentForm.mechanicData.tranceMeterCostPerSecond * Time.deltaTime;
                meter.SpendMeter(cost);
                
                // Trance ends when meter depleted
                if (meter.CurrentValue <= 0f)
                {
                    EndTrance();
                }
            }
        }
        
        /// <summary>
        /// Called from animation events when OnMechanicActivate triggers
        /// </summary>
        public void OnMechanicActivate()
        {
            if (player == null || player.CurrentForm == null) return;
            
            switch (player.CurrentForm.mechanicType)
            {
                case FormSO.FormMechanicType.CounterStance:
                    HandleCounterStance();
                    break;
                    
                case FormSO.FormMechanicType.PrecisionParry:
                    // Handled in ParryState - just log for now
                    if (debugMode) Debug.Log("PrecisionParry mechanic activated");
                    break;
                    
                case FormSO.FormMechanicType.PerfectDeflection:
                    HandlePerfectDeflection();
                    break;
                    
                case FormSO.FormMechanicType.AcrobaticFlow:
                    HandleAcrobaticFlow();
                    break;
                    
                case FormSO.FormMechanicType.PowerCounter:
                    // Active countering is handled in PlayerController.TakeDamage via
                    // TryPowerCounter() when an attack lands during the parry window.
                    if (debugMode) Debug.Log("PowerCounter mechanic armed (resolves on incoming hit)");
                    break;
                    
                case FormSO.FormMechanicType.ForceUtility:
                    HandleForceUtility();
                    break;
                    
                case FormSO.FormMechanicType.BerserkerTrance:
                    HandleBerserkerTrance();
                    break;
                    
                case FormSO.FormMechanicType.Custom:
                    // Reserved for custom mechanics
                    break;
            }
        }
        
        #region Counter Stance (Shii-Cho)
        
        private void HandleCounterStance()
        {
            if (player.CurrentForm.mechanicData == null) return;
            
            // Start parry window on counter activation
            float counterWindow = player.CurrentForm.mechanicData.counterWindowFrames / 60f;
            combat.StartParry();
            
            // Grant meter on successful counter (handled by OnParrySuccess)
            OnMechanicTriggered?.Invoke("CounterStance");
            
            if (debugMode) Debug.Log($"CounterStance activated - parry window: {counterWindow}s");
        }
        
        #endregion
        
        #region Perfect Deflection (Soresu)
        
        private void HandlePerfectDeflection()
        {
            // Perfect deflection is handled in parry flow - just award meter
            OnMechanicTriggered?.Invoke("PerfectDeflection");
            
            if (debugMode) Debug.Log("PerfectDeflection mechanic activated");
        }
        
        #endregion
        
        #region Acrobatic Flow (Ataru)
        
        private void HandleAcrobaticFlow()
        {
            // Reset aerial chain counter on special use
            aerialChainCount = 0;
            
            // Enhanced aerial mobility
            player.ResetAirActions();
            
            OnMechanicTriggered?.Invoke("AcrobaticFlow");
            
            if (debugMode) Debug.Log("AcrobaticFlow mechanic activated - aerial chains reset");
        }
        
        /// <summary>
        /// Track aerial attack chains for Ataru form
        /// </summary>
        public void RegisterAerialAttack()
        {
            if (player.CurrentForm.mechanicType != FormSO.FormMechanicType.AcrobaticFlow) return;
            
            aerialChainCount++;
            
            // Extend combo window
            float extension = player.CurrentForm.mechanicData.comboWindowExtension;
            // Would extend hitstun window by extension amount
            
            if (debugMode) Debug.Log($"Ataru aerial chain: {aerialChainCount}");
        }
        
        /// <summary>
        /// Get aerial chain multiplier for damage/meter
        /// </summary>
        public float GetAerialChainMultiplier()
        {
            if (player.CurrentForm.mechanicType != FormSO.FormMechanicType.AcrobaticFlow) 
                return 1f;
                
            int maxChains = player.CurrentForm.mechanicData.maxAerialChains;
            float bonusPerChain = 0.1f; // 10% per chain
            
            return 1f + (aerialChainCount * bonusPerChain);
        }
        
        #endregion
        
        #region Power Counter (Shien/Djem So)
        
        /// <summary>
        /// Attempt to counter an incoming hit: when the Shien/Djem So form is currently in
        /// its parry window and the incoming damage meets the threshold, the hit is absorbed
        /// (no damage/knockback applied) and a powered counter-attack is launched. Returns
        /// true when the hit was countered so PlayerController can skip normal damage.
        /// </summary>
        public bool TryPowerCounter(DamageInfo damageInfo)
        {
            if (player.CurrentForm.mechanicType != FormSO.FormMechanicType.PowerCounter) return false;
            if (player.CurrentForm.mechanicData == null) return false;
            if (player.IsDead) return false;
            if (!player.Combat.IsParrying) return false;
            if (damageInfo.damage < player.CurrentForm.mechanicData.counterAbsorbThreshold) return false;

            float dmgBonus = player.CurrentForm.mechanicData.counterDamageBonus;
            AttackSO counter = player.CurrentForm.mechanicData.counterAttack;

            meter.GainMeter(player.CurrentForm.meterGainOnPerfectParry);

            if (damageInfo.attacker != null)
            {
                float dir = Mathf.Sign(damageInfo.attacker.transform.position.x - player.transform.position.x);
                if (dir != 0f) player.FaceDirection(dir > 0f);
            }

            if (counter != null)
            {
                combat.StartAttack(counter);
                player.IgniteSaber();
            }

            player.PlaySound("CounterStance");
            player.ScreenShake(0.12f, 0.5f);
            OnMechanicTriggered?.Invoke("PowerCounter");
            return true;
        }
        
        /// <summary>
        /// Check if incoming attack can be countered based on damage threshold
        /// </summary>
        public bool CanCounterAttack(DamageInfo damageInfo)
        {
            if (player.CurrentForm.mechanicType != FormSO.FormMechanicType.PowerCounter) return false;
            if (player.CurrentForm.mechanicData == null) return false;
            
            // Check if damage meets threshold for counter
            if (damageInfo.damage >= player.CurrentForm.mechanicData.counterAbsorbThreshold)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Apply counter damage bonus
        /// </summary>
        public float GetCounterDamageBonus()
        {
            if (player.CurrentForm.mechanicType != FormSO.FormMechanicType.PowerCounter) return 1f;
            return player.CurrentForm.mechanicData.counterDamageBonus;
        }
        
        #endregion
        
        #region Force Utility (Niman)
        
        private void HandleForceUtility()
        {
            if (meter.CurrentValue < 20f)
            {
                OnMechanicTriggered?.Invoke("ForceUtilityBlocked");
                return;
            }

            meter.SpendMeter(20f);

            // Force Push: shove the nearest opponent away.
            var gm = GameManager.Instance;
            PlayerController opponent = gm != null ? gm.GetNearestOpponent(player.transform.position, player, 14f) : null;
            if (opponent != null)
            {
                float dir = Mathf.Sign(opponent.transform.position.x - player.transform.position.x);
                if (dir == 0f) dir = player.transform.localScale.x >= 0f ? 1f : -1f;
                float force = player.CurrentForm.mechanicData != null
                    ? player.CurrentForm.mechanicData.forcePushForce
                    : 20f;

                var dmg = new DamageInfo
                {
                    attacker = player.gameObject,
                    victim = opponent.gameObject,
                    damage = 6f,
                    knockback = new Vector3(dir * force, force * 0.4f, 0f),
                    hitPoint = opponent.transform.position,
                    hitNormal = Vector3.right * dir,
                    attackData = null,
                    isPerfectParry = false,
                    isCounter = false
                };
                opponent.TakeDamage(dmg);
                player.PlaySound("hit");
                player.ScreenShake(0.1f, 0.3f);
            }

            OnMechanicTriggered?.Invoke("ForceUtility");
            if (debugMode) Debug.Log("ForceUtility (push) executed");
        }
        
        #endregion
        
        #region Berserker Trance (Juyo/Vaapad)
        
        private void HandleBerserkerTrance()
        {
            if (player.CurrentForm.mechanicData == null) return;
            
            // Toggle trance on/off
            if (isTranceActive)
            {
                EndTrance();
            }
            else
            {
                // Check if we have enough meter
                if (meter.CurrentValue >= 30f)
                {
                    StartTrance();
                }
            }
        }
        
        private void StartTrance()
        {
            isTranceActive = true;
            tranceTimer = 0f;
            
            // Apply trance modifiers
            // - Increased damage
            // - Increased speed
            // - Armor threshold
            
            OnMechanicTriggered?.Invoke("TranceStart");
            
            if (debugMode) Debug.Log("BerserkerTrance STARTED");
        }
        
        private void EndTrance()
        {
            isTranceActive = false;
            tranceTimer = 0f;
            
            OnMechanicTriggered?.Invoke("TranceEnd");
            
            if (debugMode) Debug.Log("BerserkerTrance ENDED");
        }
        
        public bool IsInTrance() => isTranceActive;
        
        public float GetTranceDamageBonus()
        {
            if (!isTranceActive) return 1f;
            return player.CurrentForm.mechanicData.tranceDamageBonus;
        }
        
        public bool CheckTranceArmor(float damage)
        {
            if (!isTranceActive || player.CurrentForm.mechanicData == null) return false;
            return damage <= player.CurrentForm.mechanicData.tranceArmorThreshold;
        }
        
        #endregion
        
        #region Riposte System (Makashi)
        
        /// <summary>
        /// Called when parry succeeds to set up riposte
        /// </summary>
        public void EnableRiposte(GameObject target, float damageMultiplier, float knockbackMultiplier = 1f)
        {
            pendingRiposteTarget = target;
            riposteReady = true;
            riposteDamageMultiplier = damageMultiplier > 0f ? damageMultiplier : 1.5f;
            riposteKnockbackMultiplier = knockbackMultiplier > 0f ? knockbackMultiplier : 1f;

            OnMechanicTriggered?.Invoke("RiposteReady");

            if (debugMode) Debug.Log($"Riposte ready against {target?.name}");

            // Auto-riposte: Makashi performs its counter attack automatically if the player
            // does not input their own counter within the window.
            if (player.CurrentForm.mechanicType == FormSO.FormMechanicType.PrecisionParry &&
                player.CurrentForm.mechanicData != null &&
                player.CurrentForm.mechanicData.counterAttack != null)
            {
                StartCoroutine(RiposteRoutine(target));
            }
        }

        private System.Collections.IEnumerator RiposteRoutine(GameObject target)
        {
            yield return new WaitForSeconds(0.12f);
            if (player == null || player.IsDead) yield break;
            if (!riposteReady) yield break;

            AttackSO riposteAttack = player.CurrentForm.mechanicData.counterAttack;
            if (riposteAttack != null && target != null)
            {
                float dir = Mathf.Sign(target.transform.position.x - player.transform.position.x);
                if (!float.IsNaN(dir) && dir != 0f) player.FaceDirection(dir > 0f);
                combat.StartAttack(riposteAttack);
                player.IgniteSaber();
                riposteReady = false;
                pendingRiposteTarget = null;
                OnMechanicTriggered?.Invoke("RiposteExecuted");
            }
        }

        /// <summary>Damage multiplier for a hit landed during the riposte window (consumed on use).</summary>
        public float ConsumeRiposteMultiplier()
        {
            if (!riposteReady) return 1f;
            float m = riposteDamageMultiplier;
            riposteReady = false;
            pendingRiposteTarget = null;
            return m;
        }

        public bool IsRiposteReady => riposteReady;

        public bool TryRiposte(AttackSO attack)
        {
            if (!riposteReady || pendingRiposteTarget == null || attack == null)
                return false;

            AttackSO riposteAttack = player.CurrentForm.mechanicData.counterAttack;
            if (riposteAttack != null)
            {
                combat.StartAttack(riposteAttack);
                riposteReady = false;
                pendingRiposteTarget = null;

                OnMechanicTriggered?.Invoke("RiposteExecuted");
                return true;
            }

            return false;
        }
        
        #endregion
    }
}