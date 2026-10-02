namespace KyberClash.AI;

/// <summary>
/// LightsaberSystem - AI reference system for Star Wars lightsaber combat mechanics
/// Integrates Form I-VII logic into combat scenarios
/// Handles blade generation, color mapping, and physics integration
/// </summary>
public class LightsaberSystem : MonoBehaviour
{
    [Header("Blade Reference")]
    public GameObject _lightsaberObject; // Primary light saber mesh
    public string _bladeColorName = "blue"; // Color reference from fighter

    [Header("Combat Integration")]
    public bool _useAICombatReference = true; // AI-driven lightsaber simulation
    public List<CombatAbility> _combatAbilities = new List<CombatAbility>();

    [Header("Form Settings")]
    public FormType _currentForm = FormType.ShiiCho; 
    public CombatMechanics _formSettings = CombatMechanics.None;

    // Exposed for GameFlowManager integration - fighter references from FightersRegistry
    private string _fighterName = "Fighter";
    private bool _isCombatActive = false;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        Debug.Log("=== LightsaberSystem AWAKE ===");
        if (_useAICombatReference) 
            SetupAICombatReference();
        
        Debug.Log("Lightsaber reference initialized - form: " + _currentForm.toString());
    }

    void OnDestroy()
    {
        if (_isCombatActive) 
            StopCoroutine(_combatLoop);
        
        Debug.Log("=== LightsaberSystem DESTROYED ===");
    }

    private void SetupAICombatReference()
    {
        // Initialize AI combat reference with lightsaber mechanics
        this._useAICombatReference = true;
        this._combatAbilities.Clear();
        this._isCombatActive = false;
        
        Debug.Log("AI combat reference setup started");
    }

    public void SetLightsaberColor(string colorName)
    {
        switch (colorName)
        {
            case "cyan": 
                this._bladeColorName = "lightblue";
                this._useAICombatReference = true;
                this._combatAbilities.Add(new CombatAbility("Jedi Light", 3.0f, "Blue lightsaber energy burst"));
                break;
            case "lime": 
                this._bladeColorName = "green";
                break;
            case "deepred": 
                this._bladeColorName = "red";
                this._useAICombatReference = true; // Red requires AI combat toggle
                this._combatAbilities.Add(new CombatAbility("Sith Dark", 3.5f, "Red lightsaber with dark pressure"));
                break;
            case "silver": 
                this._bladeColorName = "grey";
                this._useAICombatReference = true; // Grey uses multiple AI modes
                this._combatAbilities.Add(new CombatAbility("Grey Dual", 2.5f, "White/Grey lightsaber fusion"));
                break;
            default: 
                Debug.LogWarning("Unknown color not supported in LightsaberSystem");
                break;
        }

        if (_lightsaberObject != null)
            _combatAbilities.ForEach(ab => ab.color = this._bladeColorName);
    }

    public void OnGameModeReady()
    {
        // Called when game mode is set up with fighters
        Debug.Log("OnGameModeReady: lightsaber combat initialized");
        
        if (_currentForm == FormType.JuyoVaapad) 
            Debug.LogWarning("Juyo/Vaapad form selected - may be exclusive to specific fighters");
    }

    public void OnFighterReferenceAdded(CombatMechanics referenceData)
    {
        // Called when fighter reference added via GameFlowManager integration
        _combatAbilities.Add(new CombatAbility(referenceData));
        
        Debug.Log("OnFighterReferenceAdded - fighter: " + (referenceData.name == null ? "Unnamed" : referenceData.name));
    }

    private void Update()
    {
        // HIGH PRIORITY #1: Fix VFX null reference guards before blade generation
        if (_useAICombatReference)
        {
            _isCombatActive = true;
            
            // Guard for lightsaber object and color name safety
            if (_lightsaberObject != null && !_bladeColorName.Contains("*"))
            {
                // Validate form SO exists before calling AI combat logic
                if (!string.IsNullOrEmpty(_currentForm.ToString()) && _combatAbilities.Count > 0)
                {
                    var validAbilities = _combatAbilities.Where(ab => ab != null && !ab.color.Contains("*"));
                    validAbilities.ForEach(ab => ab.color = this._bladeColorName);
                }
            }
        }
    }

    // Exposed for GameFlowManager callback via OnGameModeReady and OnFighterAdded hooks
    public static LightsaberSystem CreateBlade(CombatMechanics combatData)
    {
        var systemObj = new GameObject("LightsaberAI");
        
        return systemObj.AddComponent<LightsaberSystem>();
    }

    public void SetFormType(FormType formType, string color)
    {
        // Called when fighter form selected via GameFlowManager
        this._currentForm = formType;
        this._useAICombatReference = true;
        
        switch (formType)
        {
            case FormType.ShiiCho:
                this._combatAbilities.Add(new CombatAbility("Sweeping Strike", 2.5f, "Angular blade rotation"));
                break;
            default:
                Debug.LogWarning("Form " + formType.toString() + " requires additional AI logic");
                break;
        }

        // Called from GameFlowManager callback
        OnGameModeReady();
    }

    public FormType GetCurrentForm()
    {
        return _currentForm;
    }

    // Override for custom lightsaber reference configuration
    public void SetReference(CombatMechanics fightData)
    {
        this._useAICombatReference = true;
        this._combatAbilities.Add(fightData);
        
        Debug.Log("SetReference - fighter role: " + (fightData.role == null ? "Unnamed" : fightData.role));
    }
}
