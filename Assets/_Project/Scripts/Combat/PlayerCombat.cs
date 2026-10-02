using UnityEngine;
using KyberKlash.Data;
using KyberKlash.Player.States;
using KyberKlash.Combat;
using System.Collections.Generic;

namespace KyberKlash.Player
{
    /// <summary>
    /// Handles all combat logic: attacks, hitboxes, parries, clash detection.
    /// Data-driven via AttackSO - no hardcoded attack logic.
    /// </summary>
    public class PlayerCombat : MonoBehaviour, IDamageable
    {
        [Header("Hitbox Settings")]
        [SerializeField] private LayerMask hurtboxLayer = -1;
        [SerializeField] private LayerMask clashLayer = -1;

        [Header("References")]
        [SerializeField] private Transform saberTip;
        [SerializeField] private Transform saberBase;

        // Current attack state
        private AttackSO currentAttack;
        private bool hitboxActive;
        private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();
        private int currentFrame;
        private int startupFrames;
        private int activeFrames;

        // Parry state
        private bool isParrying;
        private bool perfectParryActive;
        private float parryWindowTimer;
        private System.Action<GameObject, float, float> riposteCallback;

        // Form mechanic bonuses
        private FormMechanicHandler formMechanicHandler;

        // Clash detection
        private bool clashOccurred;

        public AttackSO CurrentAttack => currentAttack;
        public bool IsHitboxActive => hitboxActive;
        public bool IsParrying => isParrying;

        public event System.Action<DamageInfo> OnHitDealt;
        public event System.Action<DamageInfo, bool> OnParrySuccess;
        public event System.Action<GameObject, AttackSO> OnClash;

        public float DamagePercent => player.Meter.DamagePercent;
        public bool IsDead => player.IsDead;
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Implementation of IDamageable.TakeDamage - forwards to PlayerController
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            player.TakeDamage(damageInfo);
        }

        private PlayerController player;

        protected virtual void Awake()
        {
            player = GetComponent<PlayerController>();
            formMechanicHandler = GetComponent<FormMechanicHandler>();
        }

        public void StartAttack(AttackSO attackData)
        {
            if (attackData == null) return;

            currentAttack = attackData;
            hitboxActive = false;
            hitTargets.Clear();
            clashOccurred = false;
            currentFrame = 0;

            startupFrames = Mathf.RoundToInt(attackData.startupFrames / player.CurrentForm.attackSpeedMultiplier);
            activeFrames = Mathf.RoundToInt(attackData.activeFrames / player.CurrentForm.attackSpeedMultiplier);
        }

        /// <summary>
        /// Called from animation events for combat phase changes
        /// </summary>
        public void OnAnimationEvent(CombatAnimationEvent evt)
        {
            switch (evt)
            {
                case CombatAnimationEvent.Startup:
                    // Startup phase - hitbox not yet active
                    hitboxActive = false;
                    break;
                case CombatAnimationEvent.Active:
                    // Active frames - enable hitbox
                    if (currentAttack != null)
                    {
                        ActivateHitbox(currentAttack);
                    }
                    break;
                case CombatAnimationEvent.Recovery:
                    // Recovery - disable hitbox
                    DeactivateHitbox();
                    break;
                case CombatAnimationEvent.HitboxEnable:
                    if (currentAttack != null)
                    {
                        ActivateHitbox(currentAttack);
                    }
                    break;
                case CombatAnimationEvent.HitboxDisable:
                    DeactivateHitbox();
                    break;
                case CombatAnimationEvent.HitboxReactivate:
                    ReactivateHitbox();
                    break;
                case CombatAnimationEvent.SwingSFX:
                    player?.PlaySound(currentAttack?.swingSFX.name);
                    break;
                case CombatAnimationEvent.HitSFX:
                    player?.PlaySound(currentAttack?.hitSFX.name);
                    break;
                case CombatAnimationEvent.LandSFX:
                    player?.PlaySound("Land");
                    break;
            }
        }

        public void ActivateHitbox(AttackSO attackData)
        {
            hitboxActive = true;
            PerformHitboxCheck(attackData);
        }

        public void DeactivateHitbox()
        {
            hitboxActive = false;
        }

        public void ReactivateHitbox()
        {
            if (currentAttack != null && hitTargets.Count < currentAttack.maxHits)
            {
                PerformHitboxCheck(currentAttack);
            }
        }

        private void PerformHitboxCheck(AttackSO attackData)
        {
            Vector3 origin = GetHitboxOrigin(attackData);
            Vector3 halfExtents = attackData.hitboxSize * 0.5f;
            Quaternion rotation = Quaternion.Euler(attackData.hitboxRotation) * transform.rotation;

            Collider[] hits = new Collider[10];
            int hitCount = 0;

            switch (attackData.hitboxShape)
            {
                case AttackSO.HitboxShape.Sphere:
                    hitCount = Physics.OverlapSphereNonAlloc(origin, attackData.hitboxSize.x, hits, hurtboxLayer, QueryTriggerInteraction.Collide);
                    break;

                case AttackSO.HitboxShape.Box:
                    hitCount = Physics.OverlapBoxNonAlloc(origin, halfExtents, hits, rotation, hurtboxLayer, QueryTriggerInteraction.Collide);
                    break;

                case AttackSO.HitboxShape.Capsule:
                    Vector3 top = origin + Vector3.up * attackData.hitboxSize.y;
                    Vector3 bottom = origin - Vector3.up * attackData.hitboxSize.y;
                    hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, attackData.hitboxSize.x, hits, hurtboxLayer, QueryTriggerInteraction.Collide);
                    break;

                case AttackSO.HitboxShape.SaberArc:
                    hitCount = PerformSaberArcCheck(attackData, hits);
                    break;
            }

            for (int i = 0; i < hitCount; i++)
            {
                ProcessHit(hits[i], attackData, origin);
            }

            CheckForClash(attackData, origin);
        }

        private Vector3 GetHitboxOrigin(AttackSO attackData)
        {
            if (saberTip != null)
            {
                return saberTip.TransformPoint(attackData.hitboxOffset);
            }
            return transform.TransformPoint(attackData.hitboxOffset);
        }

        private int PerformSaberArcCheck(AttackSO attackData, Collider[] hits)
        {
            if (saberBase == null || saberTip == null) return 0;

            Vector3 basePos = saberBase.position;
            Vector3 tipPos = saberTip.position;
            float radius = attackData.hitboxSize.x;

            int segments = 5;
            var uniqueHits = new HashSet<Collider>();

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = Vector3.Lerp(basePos, tipPos, t);

                int count = Physics.OverlapSphereNonAlloc(point, radius, hits, hurtboxLayer, QueryTriggerInteraction.Collide);
                for (int j = 0; j < count; j++)
                {
                    uniqueHits.Add(hits[j]);
                }
            }

            int index = 0;
            foreach (var hit in uniqueHits)
            {
                if (index < hits.Length)
                {
                    hits[index++] = hit;
                }
            }

            return index;
        }

        private void ProcessHit(Collider hitCollider, AttackSO attackData, Vector3 hitOrigin)
        {
            if (hitTargets.Contains(hitCollider.gameObject) && attackData.maxHits <= 1)
                return;

            var damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable == null) return;

            var targetPlayer = hitCollider.GetComponent<PlayerController>();
            bool perfectParry = false;

            if (targetPlayer != null && targetPlayer.Combat.IsParrying)
            {
                perfectParry = targetPlayer.Combat.CheckPerfectParry();
                targetPlayer.Combat.OnParrySuccessInternal(perfectParry, CreateDamageInfo(targetPlayer.gameObject, attackData, hitOrigin));
                SpawnClashEffect(hitOrigin);
                return;
            }

            Vector3 knockbackDir = CalculateKnockbackDirection(attackData, targetPlayer);
            Vector3 knockback = knockbackDir * attackData.CalculateKnockback(targetPlayer?.Meter.DamagePercent ?? 0f);

            DamageInfo damageInfo = CreateDamageInfo(targetPlayer?.gameObject ?? hitCollider.gameObject, attackData, hitOrigin);
            damageInfo.knockback = knockback;
            damageInfo.isPerfectParry = perfectParry;

            damageable.TakeDamage(damageInfo);
            hitTargets.Add(hitCollider.gameObject);

            // Ataru: track aerial chains so subsequent aerial hits scale up.
            if (formMechanicHandler != null && !player.IsGrounded)
            {
                formMechanicHandler.RegisterAerialAttack();
            }

            OnHitDealt?.Invoke(damageInfo);

            var attackState = player?.GetComponent<AttackState>();
            if (attackState != null)
            {
                attackState.OnHitConnected(damageInfo);
            }
        }

        private Vector3 CalculateKnockbackDirection(AttackSO attackData, PlayerController target)
        {
            Vector3 baseDirection;

            if (target != null)
            {
                baseDirection = (target.transform.position - transform.position).normalized;
                baseDirection.y = 0f;
                baseDirection.z = 0f;

                if (baseDirection.sqrMagnitude < 0.01f)
                {
                    baseDirection = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
                }
            }
            else
            {
                baseDirection = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
            }

            float angleRad = attackData.knockbackAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            return new Vector3(baseDirection.x * cos, sin, 0f).normalized;
        }

        private DamageInfo CreateDamageInfo(GameObject victim, AttackSO attackData, Vector3 hitPoint)
        {
            float damage = attackData.baseDamage * player.CurrentForm.damageMultiplier;

            // Form offensive bonuses ----------------------------------------------------
            if (formMechanicHandler != null)
            {
                // Berserker Trance: flat damage multiplier while in trance.
                damage *= formMechanicHandler.GetTranceDamageBonus();

                // Riposte window: powered counter swing (Makashi) deals bonus damage.
                damage *= formMechanicHandler.ConsumeRiposteMultiplier();

                // Ataru: consecutive aerial hits scale damage.
                if (!player.IsGrounded)
                    damage *= formMechanicHandler.GetAerialChainMultiplier();
            }

            return new DamageInfo
            {
                attacker = gameObject,
                victim = victim,
                damage = damage,
                knockback = Vector3.zero,
                hitPoint = hitPoint,
                hitNormal = Vector3.up,
                attackData = attackData,
                isPerfectParry = false,
                isCounter = false
            };
        }

        private void CheckForClash(AttackSO attackData, Vector3 origin)
        {
            if (clashOccurred) return;

            Collider[] clashes = Physics.OverlapSphere(origin, attackData.hitboxSize.x * 2f, clashLayer, QueryTriggerInteraction.Collide);

            foreach (var clash in clashes)
            {
                var otherCombat = clash.GetComponent<PlayerCombat>();
                if (otherCombat != null && otherCombat != this && otherCombat.IsHitboxActive)
                {
                    clashOccurred = true;
                    OnClash?.Invoke(clash.gameObject, otherCombat.CurrentAttack);
                    SpawnClashEffect(origin);
                    break;
                }
            }
        }

        private void SpawnClashEffect(Vector3 position)
        {
            if (currentAttack != null && currentAttack.clashVFXPrefab != null)
            {
                Object.Instantiate(currentAttack.clashVFXPrefab, position, Quaternion.identity);
            }
        }

        public void StartParry()
        {
            isParrying = true;
            perfectParryActive = true;
            parryWindowTimer = player.CurrentForm.mechanicData?.counterWindowFrames / 60f ?? 0.3f;

            float perfectWindow = player.CurrentForm.mechanicData?.precisionWindowFrames / 60f ?? 0.1f;
            Invoke(nameof(EndPerfectParryWindow), perfectWindow);
        }

        private void EndPerfectParryWindow()
        {
            perfectParryActive = false;
        }

        public void EndParry()
        {
            isParrying = false;
            perfectParryActive = false;
            CancelInvoke(nameof(EndPerfectParryWindow));
        }

        public bool CheckPerfectParry()
        {
            return perfectParryActive;
        }

        public void OnParrySuccessInternal(bool perfect, DamageInfo damageInfo)
        {
            isParrying = false;
            OnParrySuccess?.Invoke(damageInfo, perfect);
        }

        public void EnableRiposte(GameObject target, float damageMultiplier, float knockbackMultiplier = 1f)
        {
            riposteCallback = (t, dmg, kb) =>
            {
                if (t == target)
                {
                    var riposteAttack = player.CurrentForm.mechanicData.counterAttack;
                    if (riposteAttack != null)
                    {
                        // Modify attack data temporarily
                    }
                }
            };
        }

        public bool CanParry()
        {
            return !isParrying && player.CurrentForm != null;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (currentAttack != null && hitboxActive)
            {
                Gizmos.color = Color.red;
                Vector3 origin = GetHitboxOrigin(currentAttack);

                switch (currentAttack.hitboxShape)
                {
                    case AttackSO.HitboxShape.Sphere:
                        Gizmos.DrawWireSphere(origin, currentAttack.hitboxSize.x);
                        break;
                    case AttackSO.HitboxShape.Box:
                        Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation * Quaternion.Euler(currentAttack.hitboxRotation), Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, currentAttack.hitboxSize);
                        break;
                }
            }

            if (isParrying)
            {
                Gizmos.color = perfectParryActive ? Color.yellow : Color.cyan;
                Gizmos.DrawWireSphere(transform.position + Vector3.up, 1.5f);
            }
        }
    }
}
