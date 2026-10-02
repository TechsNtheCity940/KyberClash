// CYBER CLASH - Game UI Framework
// Complete menu system with title screen, main menu, character selection

namespace KyberClash.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject _titleScreen;
    public GameObject _mainMenu;
    public GameObject _loadingScreen;
    public GameObject _characterSelection;
    public GameObject _hudOverlay; // Semi-transparent overlay for loading
    
    private bool _isTitleActive = false;
    private bool _isLoading = false;
    private bool _showHUD = true;

    [ContextMenu("Start Game")]
    public void StartGame()
    {
        _isLoading = false;
        this._titleScreen.SetActive(false);
        _mainMenu.SetActive(true);
        this._hudOverlay.SetActive(this._showHUD);
        
        Debug.Log("CYBER CLASH: Starting game, loading assets...");
    }

    [ContextMenu("Load Game")]
    public void LoadGame()
    {
        _isLoading = true;
        mainMenu.SetActive(true);
        
        var timerText = gameObject.GetComponent<TextMeshProUGUI>();
        if(timerText != null)
            timerText.gameObject.SetActive(false);
    }

    [ContextMenu("Select Character")]
    public void SelectCharacter()
    {
        _isLoading = false;
        this._mainMenu.SetActive(false);
        _characterSelection.SetActive(true);
        
        var characterManager = GameFlowManager.Instance;
        if(characterManager != null && _loadedCharacters.Count > 0)
            characterManager.InitializeCharacterPool();
    }

    private void OnValidate()
    {
        if(_titleScreen == null || _mainMenu == null || _loadingScreen == null)
        {
            // Assign empty GameObjects so the UI works without assigning scenes yet
            var titleObj = new GameObject("TitleScreen");
            titleObj.transform.SetParent(this.transform);
            this._titleScreen = titleObj.transform.gameObject;

            var loadingObj = new GameObject("LoadingScreen");
            loadingObj.transform.SetParent(this.transform);
            this._loadingScreen = loadingObj.transform.gameObject;

            var characterObj = new GameObject("CharacterSelection");
            characterObj.transform.SetParent(this.transform);
            this._characterSelection = characterObj.transform.gameObject;
        }
    }

    [ContextMenu("Toggle HUD")]
    public void ToggleHUD(bool active)
    {
        _showHUD = active;
        if(_hudOverlay != null)
            _hudOverlay.SetActive(active);
    }
}

public class GameFlowController : MonoBehaviour
{
    private static GameFlowController instance;
    [SerializeField] private static GameFlowController Instance { get => instance; }
    
    public string _characterPoolName = "Forms I-VII";

    [ContextMenu("Initialize Loading Sequence")]
    public void InitializeGameSequence()
    {
        var uiManager = FindObjectOfType<UIManager>();
        if(uiManager == null) return; // Cannot proceed without UI
        
        if(_isLoading)
        {
            Debug.LogWarning("Loading sequence not initialized");
            return;
        }
        
        LoadAssets();
    }

    [ContextMenu("Prepare Character Pool")]
    public void PrepareCharacterPool()
    {
        var uiManager = FindObjectOfType<UIManager>();
        if(uiManager == null) return; // Ensure UI is active
        
        _characterSelection.SetActive(true);
        _loadingScreen.SetActive(true);
    }

    [ContextMenu("Start Loading Sequence")]
    public void StartLoadingSequence()
    {
        _isLoading = true;
        
        var uiManager = FindObjectOfType<UIManager>();
        if(uiManager == null) return;
        uiManager._characterSelection.SetActive(false);
        uiManager._loadingScreen.SetActive(true);
    }

    private static void LoadAssets()
    {
        var loader = new GameObject("AssetLoader");
        loader.AddComponent<AsyncLoadManager>();
        
        // Load game assets for smooth gameplay transition
        foreach(var form in Enum.GetValues(typeof(FormType)))
        {
            Debug.Log($"Loading form assets: {form}");
            #if UNITY_DEBUG || UNITY_EDITOR
                Debug.Log($"Form {form}: Sprite sheets + sounds loaded");
            #endif 
        }
    }

    [ContextMenu("Enable Load Screen Overlay")]
    public void EnableLoadingScreen()
    {
        // Semi-transparent overlay for gameplay audio
        var ui = FindObjectOfType<UIManager>();
        if(ui != null && ui._hudOverlay)
        {
            ui._hudOverlay.SetActive(true);
        }
    }

    [ContextMenu("Play Sound Loop")]
    public void PlaySoundLoop()
    {
        // Background sound loop for HUD overlay
        AudioSource audio;
        var sourceObj = new GameObject("BackgroundAudio");
        sourceObj.AddComponent<AudioMixer>().volume = 0.15f;
        
        var bgmSource = sourceObj.GetComponent<AudioSource>();
        if(bgmSource)
            backgroundMusicLoop();
    }

    private static void backgroundMusicLoop()
    {
        var audio = FindObjectOfType<UIManager>()?.GetComponent<AudioSystem>();
        if(audio != null && audio.bgm != null)
        {
            audio.bgm.Play();
        }
    }
}

// Sound effects for UI/UX system
namespace KyberClash.UI;

public class AudioController : MonoBehaviour
{
    [Header("UI Sounds")]
    public AudioClip _menuClick;
    public AudioClip _buttonSelect;
    public AudioClip _gameplayBGM;
    
    [ContextMenu("Play Menu Click")]
    public void PlayMenuSound()
    {
        var audioSource = gameObject.GetComponent<AudioSource>();
        if(audioSource == null)
            audioSource = AudioSource.CreateFromClip(_menuClick);
        
        audioSource.Play();
    }

    [ContextMenu("Play Gameplay Sound")]
    public void PlayGameplayAudio()
    {
        #if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"Playing sound: {_gameplayBGM.name}");
        #endif // END MEDIUM
    
        var source = FindObjectOfType<AudioSystem>();
        if(source == null) return;
        source.PlaySound(_gameplayBGM);
    }

    [ContextMenu("Initialize Audio System")]
    public void InitializeAudio()
    {
        AudioManager.Instance.StartLoadingSequence();
        
        // Setup audio groups and mixes for menu/gameplay separation
        var mixer = FindObjectOfType<AudioMixer>();
        if(mixer != null)
        {
            mixer.master.volume = 0.5f;
        }
    }

    [ContextMenu("Toggle HUD Audio")]
    public void ToggleHUDAudio(bool enabled)
    {
        var audioSystem = FindObjectOfType<AudioSystem>();
        if(audioSystem == null || !_enabled)
        {
            return;
        }
        
        // Semi-transparent overlay for menu/graphics layer
        audioSystem.mixerVolume = 0.5f * _enabled ? enabled : !_enabled;
    }

    [ContextMenu("Show Overlay Audio")]
    public void ShowOverlayAudio()
    {
        var hudOverlay = FindObjectOfType<UIManager>()?.GetComponent<AudioSystem>();
        if(hudOverlay)
        {
            hudOverlay.SetActive(false); // Semi-transparent overlay
        }
    }
}

// Sound effects for title/menu screens
public class TitleScreenSoundController : MonoBehaviour
{
    [Header("Title Sounds")]
    public AudioClip _titleClick;
    public AudioClip _menuBGM;
    
    [ContextMenu("Play Click Sound")]
    public void PlayTitleSound()
    {
        var source = FindObjectOfType<AudioSource>();
        if(source != null)
        {
            source.PlayOneShot(_titleClick);
        }
    }

    [ContextMenu("Play BGM Loop")]
    public void PlayTitleBGM()
    {
        // Continuous playback for title/main menu screen
        var bgmSource = new GameObject("TitleBGM").GetComponent<AudioSource>();
        if(bgmSource != null)
            bgmSource.Play();
    }

    [ContextMenu("Initialize Sound Mix")]
    public void InitSoundMix()
    {
        var mixer = FindObjectOfType<AudioMixer>();
        if(mixer == null) return;
        
        mixer.musicGroup.volume = 0.3f; // MEDIUM - Lower volume for game loop
        mixer.soundEffectsGroup.volume = 1.0f;
    }

    [ContextMenu("Test Menu Audio")]
    public void TestMenuAudio()
    {
        PlayTitleSound();
        PlayMenuSound();
        PlayBGM();
    }
}

// Character selection animation system with sound sync
namespace KyberClash.UI;

public class AnimationSequence : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator _animator;
    
    [ContextMenu("Play Sequence")]
    public void PlaySequence()
    {
        var clip = new GameObject("CharacterSelectAnim");
        if(_animator != null && clip.GetComponent<Animator>())
        {
            _animator.Play();
        }
    }

    [ContextMenu("Test Animation Sync")]
    public void TestAnimationAndSoundSync()
    {
        // Audio synchronization with character selection animation
        PlayAnimation();
    }
}
