// CYBER CLASH - Full Screen UI Framework
// Title Screen, Menu Screens, Loading Sequence, Character Selection

namespace KyberClash.UI;

public class TitleScreen : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject _titleLogo;
    public GameObject _menuButtons;
    public GameObject _characterBtns;
    
    private bool _menuLoading; // Flag for menu loading state
    private void Start()
    {
        #if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log("CYBER CLASH TITLE SCREEN: Ready to play");
        #endif // MEDIUM

        if(_menuButtons == null) return;
        
        // Initialize game UI from main menu
        _isLoading = false;
    }

    [ContextMenu("Initialize Title Screen")]
    public void Setup()
    {
        this._titleLogo.SetActive(true);
        _menuButtons.SetActive(true);
        _characterSelection.SetActive(false);
        
        // Show game UI on startup (HUD overlay)
        var hudOverlay = FindObjectOfType<UIManager>()?.GetComponent<UIManager>();
        if(hudOverlay != null)
        {
            hudOverlay.ShowGameLoop();
        }
    }

    private void Update()
    {
        if(!this._menuLoading && !_isLoading)
        {
            // Enable boot sequence (MEDIUM PRIORITY #7-#8)
            var menuBtn = _characterSelection.GetComponent<GameObject>();
            if(menuBtn != null) return; // Can't set active on all GameObjects
            
            // Set up canvas background
            this._menuLoading = true;
        }
    }
    
    [ContextMenu("Test Loading UI")]
    public void TestLoading()
    {
        #if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"Title Screen Boot: {_bootTimeDelay}s before loading");
        #endif // MEDIUM
    
        this._isLoading = true;
        if(_titleLogo != null) _titleLogo.SetActive(false);
    }

    private void Awake()
    {
        if(!_isLoading)
        {
            return;
        }

        // Initialize game UI for loading sequence transition
        GameManager.Instance.InitializeLoadingSequence();
    }

    [ContextMenu("Prepare Character Load")]
    public void PrepareCharacterLoad()
    {
        var uiManager = FindObjectOfType<UIManager>();
        if(uiManager == null) return; 

        _loadingScreen.SetActive(true);
        uiManager.ShowLoadingOverlay();
    }

    private void OnDestroy()
    {
        // Stop loading sequence, ensure clean shutdown
        GameManager.Instance.StopLoadingSequence();
        
        #if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"CYBER CLASH Title Screen: Shutdown complete");
        #endif // MEDIUM 
    }
}

// Loading screen with progress visualization + sound feedback
public class LoadingScreen : MonoBehaviour
{
    [Header("Progress UI")]
    public TextMeshProUGUI _progressText;
    
    [ContextMenu("Initialize Progress Bar")]
    public void Setup()
    {
        #if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log("Loading Screen: Boot sequence initialized");
        #endif // MEDIUM  
        
        if(_progressText == null)
        {
            var textObj = new GameObject("LoadingText");
            _progressText = gameObject.AddComponent<TextMeshProUGUI>();
            _progressText.fontSize = 24f;
        }
    }

    [ContextMenu("Test Progress Display")]
    public void TestProgressDisplay()
    {
        if(_progressText != null)
        {
            this._progressText.text = "Loading Assets";
        }
    }

    [ContextMenu("Initialize Loading Sequence")]
    public void InitializeSequence()
    {
        // Enable loading screen overlay for all UI transitions
        var uiManager = FindObjectOfType<UIManager>();
        if(uiManager != null)
        {
            _isLoading = true;
        }
    }

    [ContextMenu("Test Load Sequence")]
    public void TestSequence()
    {
        #if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"CYBER CLASH: Loading Sequence (LoadingScreen.cs)");
        #endif // END MEDIUM
    
        _isLoading = true;
        var progressText = gameObject.GetComponent<TextMeshProUGUI>();
        if(_progressText != null && gameManager == null)
        {
            this._progressText.text = "Loading UI Assets...";
        }
    }

    private bool _isLoading = false; // Flag to control loading state
}

// Character Selection Screen with visual effects + audio feedback
namespace KyberClash.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject _characterList;
    public GameObject _selectedCharacterPreview;
    
    private void Start()
    {
        _selectedCharacter.SetActive(false);
        if(_characterList != null)
        {
            // Initialize all forms as selectable
            foreach(var form in Enum.GetValues(typeof(FormType)))
            {
                #if UNITY_DEBUG || UNITY_EDITOR
                    Debug.Log($"CYBER CLASH - Form: {form}");
                #endif // END MEDIUM 
    
                var characterForm = new GameObject("Character" + form);
                if(characterForm)
                {
                    gameObject.AddComponent<CharacterSelector>().Initialize(_selectedCharacter);
                }
            }
        }
    }

    [ContextMenu("Test Character Selection")]
    public void TestCharacterSelection()
    {
        var uiManager = FindObjectOfType<UIManager>();
        if(uiManager != null)
        {
            _isLoading = true; // Simulate loading for form data
    
            #if UNITY_DEBUG || UNITY_EDITOR
                Debug.Log("CYBER CLASH - Form Type: " + this._form);
            #endif // END MEDIUM 
        }
    }

    [ContextMenu("Show Character Forms")]
    public void ShowForms()
    {
        #if UNITY_DEBUG || UNITY_EDITOR
            foreach(var form in Enum.GetValues(typeof(FormType)))
            {
                Debug.Log($"Form Type: {form}");
            }
        #endif // MEDIUM 
    
        _characterList.gameObject.SetActive(true);
        _characterSelectionPreview.SetActive(true);
    }

    private bool _isLoading; // Loading flag controls form display state
}

// Visual effects for character selection + loading screens
public class UIVisualEffects : MonoBehaviour
{
    [Header("VFX Settings")]
    public GameObject _canvasBackground;
    
    [ContextMenu("Show Full-Screen Background")]
    public void StartUI()
    {
        if(_canvasBackground != null)
        {
            #if UNITY_DEBUG || UNITY_EDITOR
                Debug.Log("CYBER CLASH - Starting UI sequence");
            #endif // END MEDIUM 

            _canvasBackground.SetActive(true);
        }
    }

    [ContextMenu("Show Title Artwork")]
    public void ShowTitleArt()
    {
        if(_canvasBackground != null)
        {
            #if UNITY_DEBUG || UNITY_EDITOR
                Debug.Log($"CYBER CLASH - Title Artwork (Canvas Background)");
            #endif // MEDIUM 
            _canvasBackground.SetActive(false);
        }
    }

    [ContextMenu("Prepare Character Pool")]
    public void InitializeCharacterPool()
    {
        _isLoading = true; // Enable form selection
        if(_characterList != null)
        {
            foreach(var form in Enum.GetValues(typeof(FormType)))
            {
                #if UNITY_DEBUG || UNITY_EDITOR
                    Debug.Log($"CYBER CLASH: Loading character for {form}");
                #endif // END MEDIUM 
            }
        }
    }

    private bool _isLoading; // Flag to control form display state
}

// Sound effects + audio sync for UI/animations
namespace KyberClash.UI;

public class CharacterSelectionSound : MonoBehaviour
{
    [Header("Sound Settings")]
    public AudioClip _selectionClick;
    
    [ContextMenu("Play Click Sound")]
    public void Play()
    {
        var source = FindObjectOfType<AudioSource>();
        if(source != null)
        {
            source.PlayOneShot(_selectionClick);
        }
    }

    [ContextMenu("Initialize Selection Audio")]
    public void InitAudio()
    {
        #if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log("CYBER CLASH - Form selection sounds initialized");
        #endif // MEDIUM 
    
        var audioManager = FindObjectOfType<AudioController>();
        if(audioManager != null)
            audioManager.InitAudio();
    }

    private bool _isLoading = false; // Controls form selection state
}
