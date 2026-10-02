using UnityEngine;
using UnityEngine.SceneManagement;
using KyberKlash.Player;
using KyberKlash.Stage;
using KyberKlash.Data;
using KyberKlash.UI;

namespace KyberKlash.Core
{
    /// <summary>
    /// Game manager - handles match state, player spawning, win conditions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Match Settings")]
        [SerializeField] private int stocksPerPlayer = 3;
        [SerializeField] private float matchTimeMinutes = 3f;
        [SerializeField] private bool timeLimitEnabled = true;
        [SerializeField] private bool autoStartMatch = true;
        [SerializeField] private bool randomizeStage = true;

        [Header("References")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CharacterData[] availableCharacters;
        [SerializeField] private StageData[] availableStages;
        [SerializeField] private StageData testStage;

        [Header("CPU Opponents")]
        [Tooltip("Index 0 = Player 1, 1 = Player 2. When true that slot is driven by AIBrain. Defaults to P2 = CPU so a solo human still gets a real match.")]
        [SerializeField] private bool[] cpuPlayers = new bool[2] { false, true };
        [SerializeField] [Range(0f, 1f)] private float cpuAggression = 0.7f;
        [SerializeField] [Range(0.02f, 0.4f)] private float cpuReactionTime = 0.12f;
        [SerializeField] [Range(0f, 1f)] private float cpuDefendChance = 0.6f;

        // State
        private PlayerController[] players;
        private float matchTimer;
        private bool matchActive = false;
        private int playersRemaining;
        private bool isPaused = false;
        private PlayerController matchWinner;
        private int eliminationOrder; // increments each elimination for placement

        public static GameManager Instance { get; private set; }
        public CharacterData[] AvailableCharacters => availableCharacters;
        public StageData[] AvailableStages => availableStages;
        public StageData CurrentConfiguredStage => testStage;
        public bool IsMatchActive => matchActive;
        public bool IsPaused => isPaused;
        public PlayerController MatchWinner => matchWinner;

        public event System.Action<PlayerController> OnPlayerEliminated;
        public event System.Action<PlayerController> OnPlayerWon;
        public event System.Action OnMatchStart;
        public event System.Action OnMatchEnd;
        /// <summary>Raised once a winner is decided (stocks or time limit).</summary>
        public event System.Action<PlayerController> OnMatchResolved;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void Start()
        {
            if (!autoStartMatch)
            {
                return;
            }

            PickRandomStageIfEnabled();

            // Ensure exactly one stage is loaded. The scene's StageManager may have already
            // loaded its own currentStageData in its own Start(); only (re)load if nothing
            // is loaded yet, to avoid a double-load race that can leave players spawning
            // before platforms exist.
            if (StageManager.Instance != null && StageManager.Instance.CurrentStageData == null && testStage != null)
            {
                StageManager.Instance.LoadStage(testStage);
            }

            // Start match after stage loads
            Invoke(nameof(StartMatch), 1f);
        }

        protected virtual void Update()
        {
            if (!matchActive) return;

            if (timeLimitEnabled)
            {
                matchTimer -= Time.deltaTime;
                if (matchTimer <= 0f)
                {
                    // Time up: the survivor with the LOWEST damage percent wins
                    // (Smash convention). A tie on equal damage is a draw.
                    PlayerController best = null;
                    float bestPercent = float.MaxValue;
                    bool tie = false;
                    if (players != null)
                    {
                        foreach (var p in players)
                        {
                            if (p == null || p.IsEliminated) continue;
                            float pct = p.DamagePercent;
                            if (pct < bestPercent - 0.01f)
                            {
                                bestPercent = pct;
                                best = p;
                                tie = false;
                            }
                            else if (Mathf.Abs(pct - bestPercent) <= 0.01f && p != best)
                            {
                                tie = true;
                            }
                        }
                    }
                    ResolveMatch(tie ? null : best, tie ? MatchEndReason.Draw : MatchEndReason.TimeLimit);
                }
            }
        }

        public void StartMatch()
        {
            if (matchActive)
            {
                return;
            }

            matchActive = true;
            matchTimeMinutes = timeLimitEnabled ? matchTimeMinutes : 0f;
            matchTimer = matchTimeMinutes * 60f;

            SpawnPlayers();
            OnMatchStart?.Invoke();
        }

        private void SpawnPlayers()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[GameManager] Cannot spawn players because playerPrefab is not assigned.", this);
                return;
            }

            if (availableCharacters == null || availableCharacters.Length == 0)
            {
                Debug.LogError("[GameManager] Cannot spawn players because no CharacterData assets are assigned.", this);
                return;
            }

            // Find spawn points
            Transform[] spawnPoints = StageManager.Instance?.GetSpawnPoints() ?? new Transform[0];

            int playerCount = Mathf.Min(2, availableCharacters.Length);
            players = new PlayerController[playerCount];
            playersRemaining = playerCount;
            eliminationOrder = 0;
            matchWinner = null;

            // Safe spawn height: just above the main platform so fighters drop in instead
            // of spawning inside/under it or already past the blast zone.
            float safeY = (StageManager.Instance != null && StageManager.Instance.CurrentStageData != null)
                ? StageManager.Instance.CurrentStageData.bottomBlastZone + 4f
                : 3f;

            for (int i = 0; i < playerCount; i++)
            {
                Vector3 spawnPos = (spawnPoints.Length > i && spawnPoints[i] != null)
                    ? spawnPoints[i].position
                    : new Vector3(i * 3f - 1.5f, 2f, 0f);
                // Override Y to a guaranteed-safe drop height (keep X/Z from the stage).
                spawnPos.y = safeY;

                GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                playerObj.name = $"Player_{i + 1}";

                var controller = playerObj.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.InitializeCharacter(availableCharacters[i]);
                    controller.InitializeMatchState(stocksPerPlayer, i);
                    controller.SetSpawnPoint(spawnPos);

                    // Setup input for local multiplayer
                    var inputHandler = controller.GetComponent<PlayerInputHandler>();
                    if (inputHandler != null)
                    {
                        inputHandler.SetPlayerIndex(i);
                    }

                    // CPU opponent: enable the AI brain for this slot when configured.
                    bool isCpu = cpuPlayers != null && cpuPlayers.Length > i && cpuPlayers[i];
                    var ai = controller.GetComponent<AIBrain>();
                    if (isCpu)
                    {
                        if (ai == null) ai = controller.gameObject.AddComponent<AIBrain>();
                        ai.Configure(cpuAggression, cpuReactionTime, cpuDefendChance);
                        ai.enabled = true;
                    }
                    else if (ai != null)
                    {
                        ai.enabled = false;
                    }

                    controller.OnDeathEvent += () => OnPlayerDeath(controller);
                    controller.OnRespawnEvent += () => OnPlayerRespawn(controller);
                    // Stock-based elimination: a confirmed KO consumes a stock; when the
                    // last stock is gone the fighter is eliminated and the match resolves.
                    controller.OnStockLost += (remaining) => OnPlayerStockLost(controller, remaining);
                    controller.OnEliminated += () => OnPlayerEliminatedHandler(controller);

                    players[i] = controller;
                }
            }

            // Register with camera
            if (CameraManager.Instance != null)
            {
                foreach (var player in players)
                {
                    CameraManager.Instance.RegisterPlayer(player);
                }
            }

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.RegisterPlayers(players);
            }
        }

        public void SetAutoStart(bool enabled)
        {
            autoStartMatch = enabled;
            if (!enabled)
            {
                CancelInvoke(nameof(StartMatch));
            }
        }

        public StageData PickRandomStage()
        {
            if (availableStages != null && availableStages.Length > 0)
            {
                return availableStages[Random.Range(0, availableStages.Length)];
            }

            return testStage;
        }

        public void ConfigureMatch(CharacterData[] selectedCharacters, StageData selectedStage)
        {
            if (selectedCharacters != null && selectedCharacters.Length > 0)
            {
                availableCharacters = selectedCharacters;
            }

            if (selectedStage != null)
            {
                testStage = selectedStage;
            }
        }

        public void LoadConfiguredStage()
        {
            if (testStage != null && StageManager.Instance != null)
            {
                StageManager.Instance.LoadStage(testStage);
            }
        }

        private void PickRandomStageIfEnabled()
        {
            if (randomizeStage)
            {
                StageData stage = PickRandomStage();
                if (stage != null)
                {
                    testStage = stage;
                }
            }
        }

        private void OnPlayerDeath(PlayerController player)
        {
            // A ring-out/KO costs the player one stock. The match manager (not the HUD) owns
            // the stock count, so the HUD simply reflects whatever the controller reports.
            player.RegisterStockLoss();
        }

        private void OnPlayerRespawn(PlayerController player)
        {
            // Player respawned with invulnerability; nothing else to do here.
        }

        private void OnPlayerStockLost(PlayerController player, int remaining)
        {
            Debug.Log($"[GameManager] {player.name} lost a stock. {remaining} remaining.", this);
            if (remaining > 0)
            {
                OnPlayerEliminated?.Invoke(player);
            }
        }

        private void OnPlayerEliminatedHandler(PlayerController player)
        {
            Debug.Log($"[GameManager] {player.name} has been eliminated!", this);
            OnPlayerEliminated?.Invoke(player);

            playersRemaining = Mathf.Max(0, playersRemaining - 1);

            // Last fighter standing wins immediately (does not apply to time-limit draws).
            if (matchActive && playersRemaining <= 1)
            {
                PlayerController survivor = GetLastLivingPlayer();
                ResolveMatch(survivor, MatchEndReason.LastFighterStanding);
            }
        }

        private PlayerController GetLastLivingPlayer()
        {
            if (players == null) return null;
            PlayerController alive = null;
            foreach (var p in players)
            {
                if (p != null && !p.IsEliminated)
                {
                    if (alive == null) alive = p;
                    else return null; // more than one still alive
                }
            }
            return alive;
        }

        /// <summary>Why the match ended - drives the results screen copy.</summary>
        public enum MatchEndReason { LastFighterStanding, TimeLimit, Draw }

        public MatchEndReason LastEndReason { get; private set; }

        private void EndMatch(MatchEndReason reason = MatchEndReason.LastFighterStanding)
        {
            if (!matchActive) return;
            matchActive = false;
            LastEndReason = reason;
            OnMatchEnd?.Invoke();
            Debug.Log($"[GameManager] Match ended ({reason}).", this);
        }

        private void ResolveMatch(PlayerController winner, MatchEndReason reason)
        {
            if (!matchActive && matchWinner != null) return;

            EndMatch(reason);
            matchWinner = winner;
            if (winner != null)
            {
                OnPlayerWon?.Invoke(winner);
            }
            OnMatchResolved?.Invoke(winner);
            GameAudioManager.Instance?.PlaySfx(winner != null ? "match_win" : "match_draw");
        }

        /// <summary>Toggle pause. Returns the new paused state.</summary>
        public bool TogglePause()
        {
            if (!matchActive) return isPaused;
            SetPaused(!isPaused);
            return isPaused;
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused) return;
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            // Pause/resume the match music so the silence reads as "paused".
            if (paused) GameAudioManager.Instance?.PlaySfx("ui_confirm");
        }

        /// <summary>
        /// Restart the current match immediately (rematch / pause-menu restart).
        /// Reuses the already-configured characters + stage so no scene reload is needed.
        /// </summary>
        public void RestartMatch()
        {
            Time.timeScale = 1f;
            isPaused = false;

            // Clear any existing player instances so stock state resets cleanly.
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (p != null) Destroy(p.gameObject);
                }
            }
            players = null;
            matchWinner = null;
            playersRemaining = 0;
            eliminationOrder = 0;

            StartMatch();
        }

        /// <summary>
        /// Return to the title screen flow (managed by GameFlowManager).
        /// </summary>
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ShowStartScreenPublic();
            }
            else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PrototypeArena")
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("PrototypeArena");
            }
            else
            {
                RestartMatch();
            }
        }

        /// <summary>
        /// All active players (for grab/throw opponent lookup, etc.)
        /// </summary>
        public PlayerController[] GetPlayers()
        {
            return players ?? new PlayerController[0];
        }

        /// <summary>
        /// Nearest living opponent to a position, excluding the given player.
        /// Returns null if none in range or only one player exists.
        /// </summary>
        public PlayerController GetNearestOpponent(Vector3 position, PlayerController except, float maxRange = Mathf.Infinity)
        {
            if (players == null) return null;

            PlayerController best = null;
            float bestDist = maxRange;
            foreach (var p in players)
            {
                if (p == null || p == except || p.IsDead) continue;
                float dist = Vector3.Distance(p.transform.position, position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = p;
                }
            }
            return best;
        }
    }
}
