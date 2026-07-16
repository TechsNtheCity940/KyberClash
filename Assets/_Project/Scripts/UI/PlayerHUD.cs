using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KyberKlash.Player;

namespace KyberKlash.UI
{
    /// <summary>
    /// HUD element for a single player showing damage % and meter
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI damagePercentText;
        [SerializeField] private Slider meterSlider;
        [SerializeField] private Image meterFillImage;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image characterPortrait;
        [SerializeField] private GameObject stockIconsContainer;
        [SerializeField] private GameObject stockIconPrefab;

        [Header("Animation")]
        [SerializeField] private float damagePopDuration = 0.3f;
        [SerializeField] private float meterChangeSpeed = 5f;

        private PlayerController player;
        private PlayerMeter meter;
        private float displayedDamagePercent;
        private float displayedMeterPercent;
        private int stocks = 3;
        private RectTransform offScreenArrow;

        public void Initialize(PlayerController playerController)
        {
            player = playerController;
            meter = playerController.Meter;
            EnsureRuntimeReferences();

            // Subscribe to events
            meter.OnDamagePercentChanged += OnDamagePercentChanged;
            meter.OnMeterChanged += OnMeterChanged;
            player.OnDeathEvent += OnPlayerDeath;
            player.OnRespawnEvent += OnPlayerRespawn;

            // Initial values
            displayedDamagePercent = meter.DamagePercent;
            displayedMeterPercent = meter.MeterPercent;
            UpdateDamageDisplay();
            UpdateMeterDisplay();

            // Set character name
            if (playerNameText != null && player.CharacterData != null)
            {
                playerNameText.text = player.CharacterData.displayName;
            }

            // Set portrait
            if (characterPortrait != null && player.CharacterData != null)
            {
                characterPortrait.sprite = player.CharacterData.characterPortrait;
            }

            // Setup stock icons
            SetupStockIcons();
        }

        private void SetupStockIcons()
        {
            EnsureRuntimeReferences();
            if (stockIconsContainer == null || stockIconPrefab == null) return;

            // Clear existing
            foreach (Transform child in stockIconsContainer.transform)
            {
                Destroy(child.gameObject);
            }

            // Create stock icons
            for (int i = 0; i < stocks; i++)
            {
                GameObject icon = Instantiate(stockIconPrefab, stockIconsContainer.transform);
                icon.name = $"Stock_{i}";
                icon.SetActive(true);
            }
        }

        private void EnsureRuntimeReferences()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
            }

            Image background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
                background.color = new Color(0.03f, 0.04f, 0.08f, 0.82f);
            }

            if (playerNameText == null)
            {
                playerNameText = CreateText("PlayerName", new Vector2(0f, 38f), 18, Color.white);
            }

            if (damagePercentText == null)
            {
                damagePercentText = CreateText("DamagePercent", new Vector2(0f, 2f), 34, Color.white);
            }

            if (meterSlider == null)
            {
                GameObject sliderObject = new GameObject("MeterSlider");
                sliderObject.transform.SetParent(transform, false);
                meterSlider = sliderObject.AddComponent<Slider>();
                RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0.08f, 0.08f);
                sliderRect.anchorMax = new Vector2(0.92f, 0.22f);
                sliderRect.offsetMin = Vector2.zero;
                sliderRect.offsetMax = Vector2.zero;

                GameObject fillObject = new GameObject("Fill");
                fillObject.transform.SetParent(sliderObject.transform, false);
                meterFillImage = fillObject.AddComponent<Image>();
                meterFillImage.color = Color.cyan;
                RectTransform fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                meterSlider.fillRect = fillRect;
                meterSlider.minValue = 0f;
                meterSlider.maxValue = 1f;
                meterSlider.interactable = false;
            }

            if (stockIconsContainer == null)
            {
                GameObject stocksObject = new GameObject("Stocks");
                stocksObject.transform.SetParent(transform, false);
                stockIconsContainer = stocksObject;
                RectTransform stocksRect = stocksObject.AddComponent<RectTransform>();
                stocksRect.anchorMin = new Vector2(0.08f, 0.72f);
                stocksRect.anchorMax = new Vector2(0.92f, 0.92f);
                stocksRect.offsetMin = Vector2.zero;
                stocksRect.offsetMax = Vector2.zero;
                var layout = stocksObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 4f;
            }

            if (stockIconPrefab == null)
            {
                stockIconPrefab = new GameObject("StockIconPrefab");
                stockIconPrefab.SetActive(false);
                stockIconPrefab.transform.SetParent(transform, false);
                Image iconImage = stockIconPrefab.AddComponent<Image>();
                iconImage.color = new Color(0.65f, 0.9f, 1f, 1f);
                RectTransform iconRect = stockIconPrefab.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(18f, 18f);
            }
        }

        private TextMeshProUGUI CreateText(string name, Vector2 anchoredPosition, int fontSize, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(180f, fontSize * 1.6f);
            rect.anchoredPosition = anchoredPosition;
            return text;
        }

        protected virtual void Update()
        {
            // Smooth damage display
            if (Mathf.Abs(displayedDamagePercent - meter.DamagePercent) > 0.1f)
            {
                displayedDamagePercent = Mathf.Lerp(displayedDamagePercent, meter.DamagePercent, meterChangeSpeed * Time.deltaTime);
                UpdateDamageDisplay();
            }

            // Smooth meter display
            if (Mathf.Abs(displayedMeterPercent - meter.MeterPercent) > 0.01f)
            {
                displayedMeterPercent = Mathf.Lerp(displayedMeterPercent, meter.MeterPercent, meterChangeSpeed * Time.deltaTime);
                UpdateMeterDisplay();
            }

            // Off-screen radar arrow (Smash-style indicator when a fighter is past the
            // camera edge but still inside the blast zones).
            UpdateOffScreenArrow();
        }

        /// <summary>
        /// Show/position a directional arrow when the player is off-screen, pointing toward
        /// them. Hidden when on-screen.
        /// </summary>
        private void UpdateOffScreenArrow()
        {
            if (player == null) return;

            Vector2 dir = CameraManager.GetOffScreenDirection(player);
            bool off = dir.sqrMagnitude > 0.0001f;

            if (offScreenArrow == null)
            {
                offScreenArrow = CreateOffScreenArrow();
            }

            if (offScreenArrow == null) return;

            offScreenArrow.gameObject.SetActive(off);
            if (off)
            {
                // Place near the screen edge in the HUD's local space (approximate radar).
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                offScreenArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
                offScreenArrow.anchoredPosition = dir * 60f;
            }
        }

        private RectTransform CreateOffScreenArrow()
        {
            GameObject arrow = new GameObject("OffScreenArrow");
            arrow.transform.SetParent(transform, false);
            RectTransform rt = arrow.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(28f, 28f);
            var img = arrow.AddComponent<Image>();
            img.color = player != null && player.CharacterData != null ? player.CharacterData.uiColor : Color.white;
            // Triangle-ish arrow via simple shape; runtime handles missing sprite gracefully.
            arrow.SetActive(false);
            return rt;
        }

        private void OnDamagePercentChanged(float newPercent)
        {
            // Pop animation
            StartCoroutine(DamagePopAnimation());
        }

        private void OnMeterChanged(float current, float max)
        {
            // Meter changes are smoothed in Update
        }

        private void OnPlayerDeath()
        {
            stocks = Mathf.Max(0, stocks - 1);
            UpdateStockIcons();
        }

        private void OnPlayerRespawn()
        {
            // Reset display
            displayedDamagePercent = 0f;
            displayedMeterPercent = 0f;
            UpdateDamageDisplay();
            UpdateMeterDisplay();
        }

        private void UpdateDamageDisplay()
        {
            if (damagePercentText != null)
            {
                damagePercentText.text = $"{Mathf.FloorToInt(displayedDamagePercent)}%";
                
                // Color based on damage
                if (displayedDamagePercent > 150f)
                    damagePercentText.color = Color.red;
                else if (displayedDamagePercent > 100f)
                    damagePercentText.color = Color.yellow;
                else if (displayedDamagePercent > 50f)
                    damagePercentText.color = new Color(1f, 0.45f, 0.15f, 1f);
                else
                    damagePercentText.color = Color.white;
            }
        }

        private void UpdateMeterDisplay()
        {
            if (meterSlider != null)
            {
                meterSlider.value = displayedMeterPercent;
            }

            if (meterFillImage != null)
            {
                meterFillImage.fillAmount = displayedMeterPercent;
                
                // Color based on meter
                if (displayedMeterPercent >= 1f)
                    meterFillImage.color = Color.yellow;
                else if (displayedMeterPercent >= 0.5f)
                    meterFillImage.color = Color.cyan;
                else
                    meterFillImage.color = Color.blue;
            }
        }

        private void UpdateStockIcons()
        {
            if (stockIconsContainer == null) return;

            for (int i = 0; i < stockIconsContainer.transform.childCount; i++)
            {
                var child = stockIconsContainer.transform.GetChild(i);
                child.gameObject.SetActive(i < stocks);
            }
        }

        private System.Collections.IEnumerator DamagePopAnimation()
        {
            if (damagePercentText == null) yield break;

            Vector3 originalScale = damagePercentText.transform.localScale;
            Vector3 popScale = originalScale * 1.3f;
            float timer = 0f;

            // Scale up
            while (timer < damagePopDuration * 0.5f)
            {
                timer += Time.deltaTime;
                damagePercentText.transform.localScale = Vector3.Lerp(originalScale, popScale, timer / (damagePopDuration * 0.5f));
                yield return null;
            }

            // Scale down
            timer = 0f;
            while (timer < damagePopDuration * 0.5f)
            {
                timer += Time.deltaTime;
                damagePercentText.transform.localScale = Vector3.Lerp(popScale, originalScale, timer / (damagePopDuration * 0.5f));
                yield return null;
            }

            damagePercentText.transform.localScale = originalScale;
        }

        protected virtual void OnDestroy()
        {
            if (meter != null)
            {
                meter.OnDamagePercentChanged -= OnDamagePercentChanged;
                meter.OnMeterChanged -= OnMeterChanged;
            }
            if (player != null)
            {
                player.OnDeathEvent -= OnPlayerDeath;
                player.OnRespawnEvent -= OnPlayerRespawn;
            }
        }
    }

    /// <summary>
    /// Manages all player HUDs for multiplayer
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("HUD Prefabs")]
        [SerializeField] private GameObject playerHUDPrefab;
        [SerializeField] private Transform hudContainer;

        [Header("Layout")]
        [SerializeField] private HUDLayout[] layouts; // Per player count

        public static HUDManager Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        protected virtual void Start()
        {
            // Find all players and create HUDs
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            CreateHUDs(players);
        }

        private void CreateHUDs(PlayerController[] players)
        {
            EnsureRuntimeContainer();
            if (playerHUDPrefab == null)
            {
                playerHUDPrefab = CreateFallbackHudPrefab();
            }

            if (playerHUDPrefab == null || hudContainer == null) return;

            // Clear existing
            foreach (Transform child in hudContainer)
            {
                Destroy(child.gameObject);
            }

            // Get layout for player count
            int playerCount = players.Length;
            HUDLayout layout = GetLayoutForPlayerCount(playerCount);

            // Create HUD for each player
            for (int i = 0; i < players.Length; i++)
            {
                GameObject hudObj = Instantiate(playerHUDPrefab, hudContainer);
                hudObj.name = $"PlayerHUD_{i}";
                hudObj.SetActive(true);
                
                var hud = hudObj.GetComponent<PlayerHUD>();
                if (hud != null)
                {
                    hud.Initialize(players[i]);
                }

                // Position according to layout
                if (i < layout.positions.Length)
                {
                    RectTransform rt = hudObj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = layout.positions[i];
                        rt.anchorMax = layout.positions[i];
                        rt.anchoredPosition = Vector2.zero;
                    }
                }
            }
        }

        private HUDLayout GetLayoutForPlayerCount(int count)
        {
            if (layouts != null)
            {
                foreach (var layout in layouts)
                {
                    if (layout.playerCount == count)
                    {
                        return layout;
                    }
                }
            }

            // Default fallback
            Vector2[] positions =
            {
                new Vector2(0.16f, 0.08f),
                new Vector2(0.84f, 0.08f),
                new Vector2(0.16f, 0.92f),
                new Vector2(0.84f, 0.92f)
            };

            return new HUDLayout { playerCount = count, positions = positions };
        }

        /// <summary>
        /// Register a late-joining player
        /// </summary>
        public void RegisterPlayer(PlayerController player)
        {
            // Recreate all HUDs with updated count
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            CreateHUDs(players);
        }

        public void RegisterPlayers(PlayerController[] players)
        {
            CreateHUDs(players);
        }

        private GameObject CreateFallbackHudPrefab()
        {
            GameObject prefab = new GameObject("RuntimePlayerHUD");
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 108f);
            prefab.AddComponent<PlayerHUD>();
            prefab.SetActive(false);
            return prefab;
        }

        private void EnsureRuntimeContainer()
        {
            if (hudContainer != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("HUDCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 400;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            GameObject containerObject = new GameObject("HUDContainer");
            containerObject.transform.SetParent(canvas.transform, false);
            hudContainer = containerObject.AddComponent<RectTransform>();
            RectTransform rect = (RectTransform)hudContainer;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    [System.Serializable]
    public class HUDLayout
    {
        public int playerCount;
        public Vector2[] positions;
    }
}
