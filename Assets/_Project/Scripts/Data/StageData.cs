namespace KyberClash.Data;

/// <summary>
/// StageData - Managing battle/arena/stadium scenarios for Super Smash Brothers-style gameplay
/// Handles multiple player spawns, arena configurations (PlayStation Arena/Focus Arena)
/// </summary>
public class StageData : MonoBehaviour
{
    [Header("Default Config")]
    public int _maxSpawns = 4; // Default match size
    public bool _useStadiumArena = true; // Stadium for large scale arena battles
    
    [Header("Stage Types")]
    public bool _stagePlayStationActive = true; // PlayStation mode
    public bool _focusArenaModeEnabled = false; // Focus Arena configuration

    [Header("Combat Zones")]
    public GameObject _arenaZoneObject; // Primary battle zone
    public LayerMask _bladesLayerMask = LayerMask.GetMask("Blades");

    /// <summary>
    /// Initializes StageData when scene loads - sets maxSpawns and arena mode
    /// </summary>
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        var fightersPicked = Fighters.PickRandomFighters(_maxSpawns);

        Debug.Log("StageData initialized with " + fightersPicked.Count + " fighters");

        _stagePlayStationActive = true; // Default: PlayStation arena mode
        _focusArenaModeEnabled = false; // Focus Arena optional toggle only
    }

    void OnDestroy()
    {
        if (_arenaZoneObject != null) 
            Destroy(_arenaZoneObject);
    }

    /// <summary>
    /// Expose for GameFlowManager integration (used by GameFlowManager.cs)
    /// </summary>
    public void OnStageDataSetup(Fighter newFighter, int spawnCount)
    {
        _maxSpawns = spawnCount;
        // Debug.Log("OnStageDataSetup: spawning " + spawnCount + " fighters")
        return;
    }

    /// <summary>
    /// Called when fighter added to arena - manages spawn positions & collision zones
    /// </summary>
    public void OnFighterAdded(int count)
    {
        // Debug.Log("OnFighterAdded: fighting with " + count + " fighters")
        
        if (_arenaZoneObject != null)
            _arenaZoneObject.SetActive(true);
    }
}
