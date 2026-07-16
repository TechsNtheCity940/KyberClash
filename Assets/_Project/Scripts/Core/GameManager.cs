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

        // State
        private PlayerController[] players;
        private float matchTimer;
        private bool matchActive = false;
        private int playersRemaining;

        public static GameManager Instance { get; private set; }
        public CharacterData[] AvailableCharacters => availableCharacters;
        public StageData[] AvailableStages => availableStages;
        public StageData CurrentConfiguredStage => testStage;

        public event System.Action<PlayerController> OnPlayerEliminated;
        public event System.Action<PlayerController> OnPlayerWon;
        public event System.Action OnMatchStart;
        public event System.Action OnMatchEnd;

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
                    EndMatch();
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

            // For testing, create 2 players
            int playerCount = Mathf.Min(2, availableCharacters.Length);
            players = new PlayerController[playerCount];
            playersRemaining = playerCount;

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
                    controller.SetSpawnPoint(spawnPos);

                    // Setup input for local multiplayer
                    var inputHandler = controller.GetComponent<PlayerInputHandler>();
                    if (inputHandler != null)
                    {
                        inputHandler.SetPlayerIndex(i);
                    }

                    controller.OnDeathEvent += () => OnPlayerDeath(controller);
                    controller.OnRespawnEvent += () => OnPlayerRespawn(controller);

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
            OnPlayerEliminated?.Invoke(player);
        }

        private void OnPlayerRespawn(PlayerController player)
        {
            // Player respawned with invulnerability
        }

        private void EndMatch()
        {
            matchActive = false;
            OnMatchEnd?.Invoke();

            // Show results, return to menu, etc.
            Debug.Log("[GameManager] Match ended!");
        }

        /// <summary>
        /// Restart current match
        /// </summary>
        public void RestartMatch()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMenu()
        {
            // No dedicated menu scene exists yet; reload the prototype arena instead of
            // referencing a "MainMenu" scene that would throw at runtime.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PrototypeArena")
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
