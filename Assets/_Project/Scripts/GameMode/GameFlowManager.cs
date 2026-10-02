namespace KyberClash.GameMode;

/// <summary>
/// GameFlowManager - Controls matchmaking and fighter selection for playable matches
/// Integrates FightersRegistry with StageData arena management
/// Handles multi-player combat initialization per FighterForm
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    [Header("Fighter Configuration")]
    public FightersRegistry _fighterRegistry; // Reference to our fighters
            
    [Header("Match Mode Settings")]
    public bool _useStarWarsFighters = true; // Use Jedi/Sith/Grey classes
    public int _maxPlayers = 4; // Maximum active players per match

    [Header("Round System")]
    public bool _hasStadiumArena = true; // StageData battlefield/starscape arena
    
    private GameManager _gameManagerRef;
    private float _roundDuration;

    /// <summary>
    /// Unity CallBack - Awake() executed once when scene loads
    /// </summary>
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        if (_fighterRegistry == null)
            Debug.LogError("GameFlowManager: fighters not initialized!");

        // Auto-verify fighter registry has fighters loaded
        var fighters = _fighterRegistry.GetAllFighters();
        Debug.Log("Game mode started - active fighters: " + fighters.Count);
    }

    /// <summary>
    /// Called when game mode is set up with fighters
    /// </summary>
    void Start()
    {
        OnGameModeReady(_fighterRegistry); // Register fighter load callback
        _roundDuration = 180f; // Default match time: min 3 seconds, max 219 f = ~4 minutes
    }

    void OnDestroy()
    {
        Debug.Log("GameFlowManager destroyed - round ended");
    }

    /// <summary>
    /// Core Match Flow Method - Picks random fighters and assigns to arena
    /// Exposed from FightersRegistry for GameMode integration
    /// </summary>
    public List<Fighter> PickRandomFighters(int count)
    {
        if (_fighterRegistry == null)
            Debug.LogError("GameFlowManager: fighter registry uninitialized!");

        _roundDuration = 180f; // Default match time
        
        var fightersPicked = _fighterRegistry.PickRandomFighters(count);
        
        Debug.Log("Match mode picked " + fightersPicked.Count + " fighters for arena");
        foreach (var f in fightersPicked) 
            Debug.Log(f.name + " assigned to round");

        // StageData integration - use default combat zone with star-wars theme
        _roundDuration = 180f;
        
        return fightersPicked;
    }

    /// <summary>
    /// Filter fighters by role (Guardian/DPS/Sentinel/Tank/Healer)
    /// </summary>
    public List<Fighter> FilterByClassRole(ClassRoles roleEnum)
    {
        var filtered = _fighterRegistry.FilterByClassRole(roleEnum);
        
        Debug.Log("Filtered " + filtered.Count + " fighters for " + roleEnum);
        return filtered;
    }

    /// <summary>
    /// Setup Round Data - Uses StageData for match zone configuration
    /// </summary>
    public void SetupRoundData(FormType form, float maxTime)
    {
        // StageData integration via fightersPicked and maxTime
        _fighterRegistry.SetCurrentForm(form);
        
        if (maxTime < 0f) maxTime = 180f;

        Debug.Log("Setup Round Data - Form " + form.toString() + ", timeout: " + maxTime);
    }

    /// <summary>
    /// Core Match Flow Method - Picks random fighters and assigns to arena
    /// </summary>
    public void StartMatchGame()
    {
        // Use FightersRegistry for fighter selection during GameMode setup
        _roundDuration = 180f;
        
        var fighters = PickRandomFighters(_maxPlayers);
        
        Debug.Log("=== GAME MATCH STARTED ===");
        foreach (var f in fighters) 
            Debug.Log(f.name + " selected");
    }

    // Exposed by FightersRegistry callback via OnGameModeReady()
    public void OnGameModeReady(Fighter newFighter)
    {
        Debug.Log("OnGameModeReady - game setup complete");
    }

    public void SetupArenaStadium(StageData stageType)
    {
        // StageData integration - arena management for battle/starscape scenarios
        _roundDuration = 180f;
        
        switch (stageType)
        {
            case StageData.Arena: 
                Debug.Log("Arena mode active");
                break;
            default: 
                Debug.Log("Battlefield scenario loaded"); 
                break;
        }
    }
}

public enum ClassRoles { Guardian, DPS, Sentinel, Tank, Healer }
