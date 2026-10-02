using System.Collections;
using System.Collections.Generic;
using KyberKlash.Data;
using KyberKlash.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KyberKlash.Core
{
    [DisallowMultipleComponent]
    public class GameFlowManager : MonoBehaviour
    {
        private const string UiResourceRoot = "KyberKlash/UI/";

        private Canvas canvas;
        private RectTransform root;
        private GameManager gameManager;
        private CharacterData[] characters;
        private CharacterData[] selectedCharacters = new CharacterData[2];
        private readonly Dictionary<string, Sprite> uiSpriteCache = new Dictionary<string, Sprite>();
        private int activeSelectionSlot;
        private bool matchStarted;

        public static GameFlowManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            GameObject flowObject = new GameObject("GameFlowManager");
            DontDestroyOnLoad(flowObject);
            flowObject.AddComponent<GameFlowManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.SetAutoStart(false);
                SubscribeMatchEvents();
            }

            if (!matchStarted)
            {
                ShowStartScreen();
            }
        }

        private void Update()
        {
            // Avoid double-handling when a modal (pause/results) already owns the screen.
            if (gameManager == null || !gameManager.IsMatchActive) return;
            if (!matchStarted) return; // results/title screen is showing

            bool escapeDown = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
            if (escapeDown)
            {
                if (gameManager.IsPaused)
                {
                    HidePauseOverlay();
                }
                else
                {
                    gameManager.SetPaused(true);
                    ShowPauseOverlay();
                }
            }
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;

            EnsureEventSystem();

            GameObject canvasObject = new GameObject("GameFlowCanvas");
            DontDestroyOnLoad(canvasObject);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            root = canvasObject.GetComponent<RectTransform>();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                DontDestroyOnLoad(eventSystemObject);
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                InputSystemUIInputModule inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }
            else
            {
                InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null && inputModule.actionsAsset == null)
                {
                    inputModule.AssignDefaultActions();
                }
            }
        }

        private void Clear()
        {
            EnsureCanvas();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private void ShowStartScreen()
        {
            matchStarted = false;
            Clear();
            GameAudioManager.Instance?.PlayMusic("music_title");

            CreateBackground("start_screen_background", new Color(0.015f, 0.02f, 0.04f, 0.98f));
            CreateLogoTitle(new Vector2(0f, 125f));
            CreateLabel("KYBER CLASH", new Vector2(0f, 48f), 28, new Color(0.72f, 0.92f, 1f, 1f));
            CreateButton("START", new Vector2(0f, -48f), () =>
            {
                ShowCharacterSelect();
            });
            CreateButton("QUIT", new Vector2(0f, -130f), () =>
            {
                Application.Quit();
            });
        }

        /// <summary>Public entry used by GameManager.ReturnToMenu() to re-show the title.</summary>
        public void ShowStartScreenPublic()
        {
            matchStarted = false;
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            ShowStartScreen();
        }

        private void ShowPauseOverlay()
        {
            // Dim the gameplay without destroying the HUD.
            CreateBackground("pause_overlay", new Color(0.01f, 0.012f, 0.03f, 0.62f));
            CreateTitle("PAUSED", new Vector2(0f, 150f), 64);
            CreateButton("RESUME", new Vector2(0f, 40f), () => HidePauseOverlay());
            CreateButton("RESTART MATCH", new Vector2(0f, -40f), () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.RestartMatch();
            });
            CreateButton("QUIT TO TITLE", new Vector2(0f, -120f), () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
                else ShowStartScreenPublic();
            });
        }

        private void HidePauseOverlay()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            Clear();
            matchStarted = true;
        }

        /// <summary>
        /// Victory / Defeat / Results screen. Called by GameManager.OnMatchResolved.
        /// </summary>
        private void ShowMatchResults(PlayerController winner)
        {
            matchStarted = false;
            Clear();

            bool isDraw = winner == null;
            string headline = isDraw ? "DRAW" : "VICTORY";
            Color headlineColor = isDraw
                ? new Color(0.95f, 0.95f, 0.95f, 1f)
                : new Color(0.72f, 0.92f, 1f, 1f);

            CreateBackground("results_background", new Color(0.01f, 0.012f, 0.03f, 0.97f));
            CreateTitle(headline, new Vector2(0f, 200f), 78);
            // Recolor the headline via a fresh label overlay positioned on top.
            CreateLabel(headline, new Vector2(0f, 200f), 78, headlineColor);

            if (!isDraw && winner != null && winner.CharacterData != null)
            {
                CreateLabel(winner.CharacterData.displayName + " WINS", new Vector2(0f, 140f), 32, new Color(1f, 0.92f, 0.98f, 1f));
                if (winner.CharacterData.characterPortrait != null)
                {
                    Image portrait = CreateChildImage(root, "WinnerPortrait", winner.CharacterData.characterPortrait);
                    RectTransform pr = portrait.rectTransform;
                    pr.anchorMin = new Vector2(0.5f, 0.5f);
                    pr.anchorMax = new Vector2(0.5f, 0.5f);
                    pr.sizeDelta = new Vector2(220f, 220f);
                    pr.anchoredPosition = new Vector2(0f, 40f);
                    portrait.preserveAspect = true;
                    portrait.raycastTarget = false;
                }
            }
            else
            {
                CreateLabel("NO CONTEST", new Vector2(0f, 140f), 30, Color.white);
            }

            // Per-fighter summary (stocks remaining / final damage %).
            var players = GameManager.Instance != null ? GameManager.Instance.GetPlayers() : new PlayerController[0];
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null || p.CharacterData == null) continue;
                string line = $"{p.CharacterData.displayName}  -  {p.DamagePercent:0}% dmg  -  {Mathf.Max(0, p.StocksRemaining)} stock(s)";
                CreateLabel(line, new Vector2(0f, 10f - i * 34f), 22, p == winner ? new Color(0.8f, 1f, 0.9f, 1f) : new Color(0.8f, 0.85f, 0.92f, 1f));
            }

            // Reason line
            string reason = GameManager.Instance != null ? ReasonText(GameManager.Instance.LastEndReason) : string.Empty;
            if (!string.IsNullOrEmpty(reason))
            {
                CreateLabel(reason, new Vector2(0f, -110f), 22, new Color(0.75f, 0.9f, 1f, 1f));
            }

            CreateButton("REMATCH", new Vector2(0f, -180f), () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.RestartMatch();
            }, 320f);
            CreateButton("TITLE", new Vector2(0f, -250f), ShowStartScreenPublic, 320f);
        }

        private static string ReasonText(GameManager.MatchEndReason reason)
        {
            switch (reason)
            {
                case GameManager.MatchEndReason.LastFighterStanding: return "Last fighter standing";
                case GameManager.MatchEndReason.TimeLimit: return "Time limit - lowest damage wins";
                case GameManager.MatchEndReason.Draw: return "Draw";
                default: return string.Empty;
            }
        }

        /// <summary>Subscribe the results screen to match resolution. Call once.</summary>
        private void SubscribeMatchEvents()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnMatchResolved -= ShowMatchResults;
            gm.OnMatchResolved += ShowMatchResults;
        }

        private void ShowCharacterSelect()
        {
            Clear();
            GameAudioManager.Instance?.PlayMusic("music_character_select");

            CreateBackground("character_select_background", new Color(0.018f, 0.022f, 0.045f, 0.98f));
            CreateTitle("SELECT YOUR FIGHTERS", new Vector2(0f, 205f), 48);

            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
            characters = gameManager != null ? gameManager.AvailableCharacters : null;
            if (characters == null || characters.Length == 0)
            {
                CreateLabel("No CharacterData assigned yet.", Vector2.zero, 28, Color.yellow);
                CreateButton("BACK", new Vector2(0f, -170f), ShowStartScreen);
                return;
            }

            if (selectedCharacters[0] == null)
            {
                selectedCharacters[0] = characters[0];
            }

            if (selectedCharacters[1] == null)
            {
                selectedCharacters[1] = characters[Mathf.Min(1, characters.Length - 1)];
            }

            CreateSelectionSlotButton(0, new Vector2(-180f, 150f));
            CreateSelectionSlotButton(1, new Vector2(180f, 150f));

            // Smash-style "NAME vs NAME" banner.
            string banner = (selectedCharacters[0] != null ? selectedCharacters[0].displayName : "?")
                + "    VS    "
                + (selectedCharacters[1] != null ? selectedCharacters[1].displayName : "?");
            CreateLabel(banner, new Vector2(0f, 92f), 30, new Color(1f, 0.92f, 0.98f, 1f));

            CreateCharacterGrid();

            CreateButton("RANDOM ARENA — BATTLE", new Vector2(0f, -245f), () =>
            {
                StartCoroutine(LoadRandomMatchRoutine());
            }, 360f);
            CreateButton("BACK", new Vector2(-460f, -245f), ShowStartScreen, 180f);
        }

        private void CreateSelectionSlotButton(int slot, Vector2 position)
        {
            CharacterData sel = selectedCharacters[slot];
            string label = "P" + (slot + 1) + "\n" + (sel != null ? sel.displayName : "—");

            Button button = CreateButton(label, position, () =>
            {
                activeSelectionSlot = slot;
                ShowCharacterSelect();
            }, 280f, 80f);

            // Show the selected fighter's portrait inside the slot (Smash-style).
            if (sel != null && sel.characterPortrait != null)
            {
                Image portrait = CreateChildImage(button.transform, "SlotPortrait", sel.characterPortrait);
                SetAnchors(portrait.rectTransform, new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.92f), Vector2.zero, Vector2.zero);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = activeSelectionSlot == slot ? new Color(1f, 0.88f, 0.95f, 1f) : Color.white;
            colors.highlightedColor = Color.white;
            button.colors = colors;
        }

        private void CreateCharacterGrid()
        {
            RectTransform grid = CreatePanel("CharacterGrid", new Vector2(0f, -20f), new Vector2(1020f, 314f), new Color(0f, 0f, 0f, 0.25f));
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(132f, 132f);
            layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 7;
            layout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < characters.Length; i++)
            {
                CharacterData character = characters[i];
                bool selected = IsSelected(character);
                Button card = CreateButtonObject(character.displayName, grid, selected ? "character_card_selected" : "character_card_frame", "character_card_selected", "menu_button_pressed", false);
                card.onClick.AddListener(() =>
                {
                    selectedCharacters[activeSelectionSlot] = character;
                    activeSelectionSlot = (activeSelectionSlot + 1) % selectedCharacters.Length;
                    ShowCharacterSelect();
                });

                Image image = card.GetComponent<Image>();
                image.color = selected ? new Color(1f, 0.92f, 0.98f, 1f) : Color.white;

                if (character.characterPortrait != null)
                {
                    Image portrait = CreateChildImage(card.transform, "Portrait", character.characterPortrait);
                    SetAnchors(portrait.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
                    portrait.preserveAspect = true;
                    portrait.raycastTarget = false;
                }

                TextMeshProUGUI label = CreateText(card.transform, character.displayName, 15, Color.white, TextAlignmentOptions.Center);
                SetAnchors(label.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.2f), Vector2.zero, Vector2.zero);
            }
        }

        private IEnumerator LoadRandomMatchRoutine()
        {
            Clear();
            CreateBackground("loading_screen_background", new Color(0.01f, 0.012f, 0.03f, 1f));

            StageData selectedStage = gameManager != null ? gameManager.PickRandomStage() : null;
            CreateTitle("ENTERING ARENA", new Vector2(0f, 64f), 58);
            CreateLabel(selectedStage != null ? selectedStage.displayName : "Prototype Arena", new Vector2(0f, -12f), 30, Color.white);
            CreateLabel("Random arena selected", new Vector2(0f, -58f), 20, new Color(0.75f, 0.9f, 1f, 1f));
            Image spinner = CreateLoadingSpinner(new Vector2(0f, 162f));
            RectTransform fill = CreateLoadingBar(new Vector2(0f, -116f));

            float loadTimer = 0f;
            const float minimumLoadSeconds = 1.15f;
            while (loadTimer < minimumLoadSeconds)
            {
                loadTimer += Time.unscaledDeltaTime;
                UpdateLoadingAnimation(spinner, fill, loadTimer / minimumLoadSeconds);
                yield return null;
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
            }

            if (gameManager != null)
            {
                gameManager.ConfigureMatch(selectedCharacters, selectedStage);
                gameManager.LoadConfiguredStage();
                matchStarted = true;
                Clear();
                GameAudioManager.Instance?.PlayMusic("music_gameplay_battle", 0.62f);
                gameManager.StartMatch();
            }
        }

        private bool IsSelected(CharacterData character)
        {
            return selectedCharacters[0] == character || selectedCharacters[1] == character;
        }

        private void CreateBackground(string resourceKey, Color fallbackColor)
        {
            Image background = CreateChildImage(root, "Background", Resources.Load<Sprite>(UiResourceRoot + resourceKey));
            SetAnchors(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background.color = background.sprite != null ? Color.white : fallbackColor;
            background.preserveAspect = false;
            background.raycastTarget = false;
        }

        private void CreateTitle(string text, Vector2 position, int size)
        {
            CreateLabel(text, position, size, new Color(0.72f, 0.92f, 1f, 1f));
        }

        private void CreateLogoTitle(Vector2 position)
        {
            Sprite logo = LoadUiSprite("kyber_klash_logo");
            if (logo == null)
            {
                CreateTitle("KYBER KLASH", position, 88);
                return;
            }

            Image logoImage = CreateChildImage(root, "KyberKlashLogo", logo);
            RectTransform rect = logoImage.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 220f);
            rect.anchoredPosition = position;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
        }

        private TextMeshProUGUI CreateLabel(string text, Vector2 position, int size, Color color)
        {
            TextMeshProUGUI label = CreateText(root, text, size, color, TextAlignmentOptions.Center);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, size * 2.4f);
            rect.anchoredPosition = position;
            return label;
        }

        private Button CreateButton(string text, Vector2 position, UnityEngine.Events.UnityAction action, float width = 240f, float height = 64f)
        {
            Button button = CreateButtonObject(text, root);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = position;
            button.onClick.AddListener(action);
            return button;
        }

        private Button CreateButtonObject(string text, Transform parent, string normalSpriteKey = "menu_button_normal", string hoverSpriteKey = "menu_button_hover", string pressedSpriteKey = "menu_button_pressed", bool createLabel = true)
        {
            GameObject buttonObject = new GameObject(text + "Button");
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = LoadUiSprite(normalSpriteKey) ?? LoadUiSprite("menu_button_frame");
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : new Color(0.07f, 0.09f, 0.14f, 0.92f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = ImageHasStateSprites(hoverSpriteKey, pressedSpriteKey) ? Selectable.Transition.SpriteSwap : Selectable.Transition.ColorTint;

            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = LoadUiSprite(hoverSpriteKey);
            spriteState.selectedSprite = LoadUiSprite(hoverSpriteKey);
            spriteState.pressedSprite = LoadUiSprite(pressedSpriteKey);
            button.spriteState = spriteState;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.selectedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(1f, 0.86f, 0.94f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
            button.colors = colors;
            button.onClick.AddListener(() => GameAudioManager.Instance?.PlaySfx("ui_confirm"));
            AddHoverSound(buttonObject);

            if (createLabel)
            {
                TextMeshProUGUI label = CreateText(buttonObject.transform, text, 18, Color.white, TextAlignmentOptions.Center);
                label.fontStyle = FontStyles.Bold;
                label.outlineWidth = 0.18f;
                label.outlineColor = new Color32(0, 12, 24, 220);
                SetAnchors(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 4f), new Vector2(-18f, -4f));
            }

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                EventSystem.current.SetSelectedGameObject(buttonObject);
            }

            return button;
        }

        private RectTransform CreatePanel(string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(root, false);
            Image image = panelObject.AddComponent<Image>();
            image.sprite = LoadUiSprite("menu_panel_frame");
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : color;
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private Image CreateLoadingSpinner(Vector2 position)
        {
            Image spinner = CreateChildImage(root, "LoadingSpinner", LoadUiSprite("loading_spinner_0"));
            RectTransform rect = spinner.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(128f, 128f);
            rect.anchoredPosition = position;
            spinner.preserveAspect = true;
            spinner.raycastTarget = false;
            return spinner;
        }

        private RectTransform CreateLoadingBar(Vector2 position)
        {
            Image frame = CreateChildImage(root, "LoadingBarFrame", LoadUiSprite("loading_bar_frame"));
            RectTransform frameRect = frame.rectTransform;
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = new Vector2(460f, 42f);
            frameRect.anchoredPosition = position;
            frame.type = frame.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            frame.color = frame.sprite != null ? Color.white : new Color(0.04f, 0.08f, 0.14f, 0.9f);
            frame.raycastTarget = false;

            Image fill = CreateChildImage(frame.transform, "LoadingBarFill", LoadUiSprite("loading_bar_fill"));
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = new Vector2(8f, 8f);
            fillRect.offsetMax = new Vector2(-8f, -8f);
            fill.type = fill.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            fill.color = fill.sprite != null ? Color.white : new Color(0.2f, 0.75f, 1f, 0.95f);
            fill.raycastTarget = false;
            return fillRect;
        }

        private void UpdateLoadingAnimation(Image spinner, RectTransform fill, float normalizedProgress)
        {
            float progress = Mathf.Clamp01(normalizedProgress);
            if (spinner != null)
            {
                int frame = Mathf.FloorToInt(Time.unscaledTime * 14f) % 8;
                spinner.sprite = LoadUiSprite("loading_spinner_" + frame);
                spinner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 140f);
            }

            if (fill != null)
            {
                fill.anchorMax = new Vector2(progress, 1f);
                fill.offsetMax = new Vector2(-8f, -8f);
            }
        }

        private static Image CreateChildImage(Transform parent, string name, Sprite sprite)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, int size, Color color, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        private Sprite LoadUiSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (uiSpriteCache.TryGetValue(key, out Sprite cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>(UiResourceRoot + key);
            uiSpriteCache[key] = sprite;
            return sprite;
        }

        private bool ImageHasStateSprites(string hoverSpriteKey, string pressedSpriteKey)
        {
            return LoadUiSprite(hoverSpriteKey) != null && LoadUiSprite(pressedSpriteKey) != null;
        }

        private static void AddHoverSound(GameObject buttonObject)
        {
            EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => GameAudioManager.Instance?.PlaySfx("ui_move", 0.45f));
            trigger.triggers.Add(entry);
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
