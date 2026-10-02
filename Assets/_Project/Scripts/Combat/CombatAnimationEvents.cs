using UnityEngine;
using KyberKlash.Player;
using KyberKlash.Combat;
using KyberKlash.Data;

namespace KyberKlash.Combat
{
    /// <summary>
    /// Animation Event handler for combat - receives events from animation clips
    /// </summary>
    public class CombatAnimationEvents : MonoBehaviour
    {
        private PlayerCombat combat;
        private PlayerController player;
        private FormMechanicHandler mechanicHandler;
        
        private void Awake()
        {
            combat = GetComponentInParent<PlayerCombat>();
            player = GetComponentInParent<PlayerController>();
            mechanicHandler = player?.GetComponent<FormMechanicHandler>();
        }
        
        /// <summary>
        /// Called when attack startup frames begin (frame 1 of startup)
        /// </summary>
        public void OnAttackStartup()
        {
            combat?.OnAnimationEvent(CombatAnimationEvent.Startup);
            player?.OnAnimationEvent(CombatAnimationEvent.Startup);
        }
        
        /// <summary>
        /// Called when hitbox becomes active (first active frame)
        /// </summary>
        public void OnAttackActive()
        {
            combat?.OnAnimationEvent(CombatAnimationEvent.Active);
            player?.OnAnimationEvent(CombatAnimationEvent.Active);
        }
        
        /// <summary>
        /// Called when attack ends (recovery begins)
        /// </summary>
        public void OnAttackEnd()
        {
            combat?.OnAnimationEvent(CombatAnimationEvent.Recovery);
            player?.OnAnimationEvent(CombatAnimationEvent.Recovery);
        }
        
        /// <summary>
        /// Called to enable hitbox - more precise than frame-based
        /// </summary>
        public void OnHitboxEnable()
        {
            if (combat != null && combat.CurrentAttack != null)
            {
                combat.ActivateHitbox(combat.CurrentAttack);
            }
        }
        
        /// <summary>
        /// Called to disable hitbox - more precise than frame-based
        /// </summary>
        public void OnHitboxDisable()
        {
            combat?.DeactivateHitbox();
        }
        
        /// <summary>
        /// Called for multi-hit attacks to reactivate hitbox
        /// </summary>
        public void OnHitboxReactivate()
        {
            combat?.ReactivateHitbox();
        }
        
        /// <summary>
        /// Called to play swing SFX
        /// </summary>
        public void OnSwingSFX()
        {
            if (combat?.CurrentAttack != null)
            {
                player?.PlaySound(combat.CurrentAttack.swingSFX?.name ?? "Swing");
            }
        }
        
        /// <summary>
        /// Called to play hit SFX when connecting - stores hitSFX for OnHitConnected trigger
        /// </summary>
        public void OnHitSFX()
        {
            // Store that we want to play hit SFX on hit connect
            // This is called by animation events but hit SFX plays when hit connects
        }
        
        /// <summary>
        /// Called to play landing SFX
        /// </summary>
        public void OnLandSFX()
        {
            player?.PlaySound("Land");
        }
        
        /// <summary>
        /// Called to play dash SFX
        /// </summary>
        public void OnDashSFX()
        {
            player?.PlaySound("Dash");
        }
        
        /// <summary>
        /// Called to play jump SFX
        /// </summary>
        public void OnJumpSFX()
        {
            player?.PlaySound("Jump");
        }
        
        /// <summary>
        /// Called to spawn dust VFX on land
        /// </summary>
        public void OnLandVFX()
        {
            player?.SpawnVFX(player?.CharacterData?.dustVFXPrefab, transform.position + Vector3.down * 0.5f);
        }
        
        /// <summary>
        /// Called to spawn dash VFX
        /// </summary>
        public void OnDashVFX()
        {
            player?.SpawnVFX(player?.CharacterData?.dashVFXPrefab, transform.position);
        }
        
        /// <summary>
        /// Called when parry window starts
        /// </summary>
        public void OnParryStart()
        {
            combat?.StartParry();
        }
        
        /// <summary>
        /// Called when parry window ends
        /// </summary>
        public void OnParryEnd()
        {
            combat?.EndParry();
        }
        
        /// <summary>
        /// Called for form-specific mechanic activation
        /// </summary>
        public void OnMechanicActivate()
        {
            mechanicHandler?.OnMechanicActivate();
        }
    }
}