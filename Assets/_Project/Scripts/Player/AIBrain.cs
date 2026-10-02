using UnityEngine;
using KyberKlash.Core;
using KyberKlash.Stage;

namespace KyberKlash.Player
{
    /// <summary>
    /// Lightweight CPU opponent. Writes synthetic input into the attached PlayerInputHandler
    /// so it drives the exact same state machine / combat code a human would. Designed for
    /// single-player vs CPU matches. Difficulty scales reaction time and aggression.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class AIBrain : MonoBehaviour
    {
        [Header("Difficulty")]
        [Range(0f, 1f)] [SerializeField] private float aggression = 0.7f;     // 0 = passive, 1 = relentless
        [Range(0.02f, 0.4f)] [SerializeField] private float reactionTime = 0.12f;
        [Range(0f, 1f)] [SerializeField] private float defendChance = 0.6f;
        [Range(0.5f, 3f)] [SerializeField] private float decisionInterval = 1.1f;

        [Header("Tuning")]
        [SerializeField] private float attackRange = 2.6f;
        [SerializeField] private float approachRange = 9f;

        private PlayerController self;
        private PlayerInputHandler input;
        private PlayerController opponent;

        private float decisionTimer;
        private float reactionTimer;
        private float actionHoldTimer;
        private AIAction currentAction = AIAction.Approach;
        private PlayerInputHandler.AICommand cmd;
        private bool recovering;

        private enum AIAction { Approach, Attack, Defend, Retreat, Recover }

        private void Awake()
        {
            self = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputHandler>();
        }

        private void OnEnable()
        {
            input.EnableAIOverride(true);
        }

        private void OnDisable()
        {
            if (input != null) input.EnableAIOverride(false);
        }

        private void Update()
        {
            if (self == null || self.IsDead) return;

            // Locate opponent periodically (cheap).
            if (opponent == null || opponent.IsDead || opponent.IsEliminated)
            {
                opponent = GameManager.Instance != null
                    ? GameManager.Instance.GetNearestOpponent(self.transform.position, self)
                    : null;
            }

            // Offstage recovery is always top priority and runs every frame.
            recovering = IsOffstage();
            if (recovering)
            {
                BuildRecoverCommand();
                input.SetAICommand(cmd);
                return;
            }

            // Reaction + decision cadence so the CPU is beatable and feels fair.
            reactionTimer -= Time.deltaTime;
            decisionTimer -= Time.deltaTime;
            actionHoldTimer -= Time.deltaTime;

            if (decisionTimer <= 0f && reactionTimer <= 0f)
            {
                DecideAction();
                decisionTimer = decisionInterval * Random.Range(0.7f, 1.3f);
            }

            BuildCommand();
            input.SetAICommand(cmd);
        }

        private void DecideAction()
        {
            if (opponent == null) { currentAction = AIAction.Approach; return; }

            float dist = Vector3.Distance(opponent.transform.position, self.transform.position);
            float myPercent = self.DamagePercent;

            // Defend if the opponent is close and winding up an attack, and we're not busy.
            bool opponentThreatening = dist < attackRange * 1.4f && !self.IsInHitStun && !self.IsInKnockback;
            if (opponentThreatening && Random.value < defendChance * 0.5f)
            {
                currentAction = AIAction.Defend;
                actionHoldTimer = Random.Range(0.15f, 0.35f);
                return;
            }

            // High damage percent + close enemy => play it safe occasionally.
            if (myPercent > 90f && dist < attackRange * 1.2f && Random.value < 0.4f)
            {
                currentAction = AIAction.Retreat;
                actionHoldTimer = Random.Range(0.3f, 0.7f);
                return;
            }

            if (dist <= attackRange)
            {
                currentAction = AIAction.Attack;
                // Hold an attack for a short burst, then re-decide.
                actionHoldTimer = Random.Range(0.18f, 0.5f);
            }
            else if (dist <= approachRange)
            {
                currentAction = (Random.value < aggression) ? AIAction.Attack : AIAction.Approach;
                actionHoldTimer = Random.Range(0.2f, 0.6f);
            }
            else
            {
                currentAction = AIAction.Approach;
            }
        }

        private void BuildCommand()
        {
            cmd = default;
            if (opponent == null)
            {
                cmd.move = Vector2.zero;
                return;
            }

            Vector3 toOpp = opponent.transform.position - self.transform.position;
            float dir = Mathf.Sign(toOpp.x);
            if (dir == 0f) dir = 1f;
            float dist = toOpp.magnitude;
            float absY = Mathf.Abs(toOpp.y);

            switch (currentAction)
            {
                case AIAction.Approach:
                    cmd.move = new Vector2(dir, (absY > 1.5f && toOpp.y > 0f) ? 1f : 0f);
                    // Hop if opponent is above.
                    if (absY > 2.5f && toOpp.y > 0f && self.IsGrounded && Random.value < 0.05f)
                        cmd.jump = true;
                    break;

                case AIAction.Attack:
                    cmd.move = new Vector2(dir, 0f);
                    // Face the opponent by moving toward them; choose an attack type.
                    float roll = Random.value;
                    if (roll < 0.55f) cmd.lightAttack = true;
                    else if (roll < 0.85f) cmd.heavyAttack = true;
                    else if (self.MeterPercent > 0.4f) cmd.special = true;
                    // Air attacks when opponent is above.
                    if (absY > 1.6f && !self.IsGrounded) cmd.lightAttack = true;
                    // Small forward nudge to stay in range.
                    if (dist > attackRange * 0.9f) cmd.move = new Vector2(dir, cmd.move.y);
                    break;

                case AIAction.Defend:
                    cmd.parry = true;
                    cmd.move = new Vector2(dir * 0.2f, 0f);
                    break;

                case AIAction.Retreat:
                    cmd.move = new Vector2(-dir, 0f);
                    if (self.IsGrounded && Random.value < 0.1f) cmd.jump = true;
                    break;
            }
        }

        private void BuildRecoverCommand()
        {
            cmd = default;
            var stage = StageManager.Instance?.CurrentStageData;
            if (stage == null) return;

            float centerX = (stage.leftBlastZone + stage.rightBlastZone) * 0.5f;
            float dirToCenter = Mathf.Sign(centerX - self.transform.position.x);
            if (dirToCenter == 0f) dirToCenter = 1f;

            // Move back toward stage horizontally.
            cmd.move = new Vector2(dirToCenter, 0f);

            // If below the stage or launched outward, use up-special (recovery) and jump.
            if (self.transform.position.y < stage.bottomBlastZone + 6f || Mathf.Abs(self.transform.position.x) > (stage.rightBlastZone - 4f))
            {
                if (self.IsGrounded == false)
                {
                    cmd.recoverySpecial = true; // up-special to launch back to stage
                }
                else
                {
                    cmd.jump = true;
                }
            }
            // If above the top blast zone, drop down.
            if (self.transform.position.y > stage.topBlastZone - 3f)
            {
                cmd.move = new Vector2(dirToCenter, -1f);
            }
        }

        private bool IsOffstage()
        {
            var stage = StageManager.Instance?.CurrentStageData;
            if (stage == null) return false;
            float margin = 2.5f;
            return self.transform.position.x < stage.leftBlastZone + margin ||
                   self.transform.position.x > stage.rightBlastZone - margin ||
                   self.transform.position.y < stage.bottomBlastZone + margin;
        }

        /// <summary>Called by match setup to scale difficulty by slot/option.</summary>
        public void Configure(float aggression, float reactionTime, float defendChance)
        {
            this.aggression = Mathf.Clamp01(aggression);
            this.reactionTime = Mathf.Clamp(reactionTime, 0.02f, 0.4f);
            this.defendChance = Mathf.Clamp01(defendChance);
        }
    }
}
