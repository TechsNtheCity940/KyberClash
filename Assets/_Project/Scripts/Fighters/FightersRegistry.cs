namespace KyberClash.Fighters;

/// <summary>
/// FighterRegistry - Manages all registered fighters for matchmaking, balance, and gameplay
/// Loads Star Wars-themed fighters (Jedi/Sith/Grey Knight + Form/Color combinations)
/// </summary>
public class FightersRegistry : MonoBehaviour
{
    [Header("Registered Fighters")]
    public List<Fighter> _activeFighters; // Dynamic list of registered fighters
    
    [Header("Form System")]
    public FormType _currentForm = FormType.ShiiCho; // Default starting form
    
    [Header("Star Wars Theme Config")]
    public bool _useJediSithGreyClasses = true; // Enable Star Wars themed fighter classes
    public string _defaultBlueColor = "lightblue";
    public string _defaultRedColor = "darkred";

    private int _fighterCount = 0;
    private List<Fighter> _allFIGHTERS = new List<Fighter>();

    void Awake()
    {
        DontDestroyOnLoad(gameObject); // Persist across scenes
        
        Debug.Log("=== FightersRegistry AWAKE ===");
        
        // Load fighters based on type and color variants  
        if (_useJediSithGreyClasses)
            RegisterStarWarsFighters();
        else
            InitializeClassicFighters();

        OnGameModeReady(); // Expose for GameFlowManager integration
    }

    void OnDestroy()
    {
        Debug.Log("Fighter count: " + _activeFighters.Count);
    }

    /// <summary>
    /// Registers Star Wars themed fighters (27 base combinations × 3 form x color)
    /// Creates individual fighter instances for each FighterClass definition
    /// </summary>
    private void RegisterStarWarsFighters()
    {
        Debug.Log("Registering Star Wars Fighters...");

        // ---- JEDI GIANT (Blue/Green lightsabers, Guardian/DPS role) ----
        // JediGuardian can use Form III, V, I, VII via Vaapad variant
        RegisterJediFormI(JediGuardian.name, "LightBlue", "blue");
        RegisterJediFormI(JediGuardian.name, "darkblue", "blue");
        RegisterJediFormII(JediGuardian.name, "green", "green"); // Soresu form with Jedi theme
        RegisterJediFormIII(JediGuardian.name, "lightgreen", "green");
        
        // ---- SITH WARRIOR (Red lightsaber, DPS role) ----
        // SithWarrior prefers Juyo/Vaapad due to dark side compatibility
        RegisterSithFormI(SithWarrior.name, "red", "deepred");
        RegisterSithFormS(SithWarrior.name, "darkgrey", "red");
        
        // ---- GREY KNIGHT (White/Grey/Black lightsaber, Sentinel role) ----
        // Grey Knight can use ANY form - balanced approach
        RegisterGreyFormI(GreyKnight.name, "white", "silver");
        RegisterGreyFormV(GreyKnight.name, "grey", "slategray");
        RegisterGreyFormVII(GreyKnight.name, "black", "darkgray");

        // Total registered: 3 base fighters (Jedi/Sith/Grey) x 3 forms = 9 fighters

        Debug.Log("Total Star Wars Fighters registered: " + _allFIGHTERS.Count);
        for (int i = 0; i < _activeFighters.Count; i++)
            Debug.Log("Fighter " + i + ": " + _activeFighters[i].name);
    }

    /// <summary>
    /// Registers classic/fantasy fighters without Star Wars IP elements
    /// </summary>
    private void InitializeClassicFighters()
    {
        // For non-StarWars deployments (test scenarios)
        RegisterJediFormI("Jedi Master", "blue", "cyan");
        RegisterSithFormS("Sith Lord", "red", "darkred");
        RegisterGreyFormV("Balance Knight", "white", "silver");
    }

    private void RegisterJediFormI(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form I-Blue", role = ClassRoles.Guardian };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    private void RegisterJediFormII(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form II-" + color, role = ClassRoles.Guardian };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    private void RegisterJediFormIII(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form III-" + color, role = ClassRoles.Guardian };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    private void RegisterSithFormI(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form I-Red", role = ClassRoles.DPS };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    private void RegisterSithFormS(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form S-" + color, role = ClassRoles.DPS };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    private void RegisterGreyFormI(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form I-Silver", role = ClassRoles.Sentinel };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    private void RegisterGreyFormV(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form V-" + color, role = ClassRoles.Sentinel };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    private void RegisterGreyFormVII(string name, bool red, string color)
    {
        var fighter = new Fighter() { name = name + "-Form VII-Slate", role = ClassRoles.Sentinel };
        _allFIGHTERS.Add(fighter);
        OnFighterAdded(_fighterCount++);
    }

    public List<Fighter> GetActiveFighters()
    {
        return _activeFighters;
    }

    public List<Fighter> GetAllFighters()
    {
        return _allFIGHTERS;
    }

    // Exposed for GameFlowManager integration
    public void OnGameModeReady(Fighter newFighter)
    {
        // Called once when game mode is set up with fighters
        Debug.Log("GameMode setup complete - total fighters registered: " + _allFIGHTERS.Count);
    }

    public void OnFighterAdded(int id)
    {
        if (id < 0 || id >= _fighterCount)
            return;
        
        // Debug output to console for build system integration
        Debug.Log("Fighter " + id + " added to registry");
    }

    public FormType GetCurrentForm()
    {
        return _currentForm;
    }

    public void SetCurrentForm(FormType form)
    {
        _currentForm = form;
    }

    public List<Fighter> PickRandomFighters(int count)
    {
        if (count > _allFIGHTERS.Count)
            return new List<Fighter>(); // Return all fighters if requesting too many
        
        var picked = new List<Fighter>(_allFIGHTERS.GetRandomSubset(count));
        
        Debug.Log("Picked " + picked.Count + " fighters randomly");
        return picked;
    }

    public void FilterByClassRole(ClassRoles roleEnum)
    {
        _activeFighters.Clear();
        
        for (int i = 0; i < _allFIGHTERS.Count; i++)
        {
            var fighter = _allFIGHTERS[i];
            
            // Check if fighter's role matches requested enum
            switch (roleEnum)
            {
                case ClassRoles.Guardian: 
                    if (fighter.name.Contains("Jedi")) 
                        _activeFighters.Add(fighter); 
                    break;
                case ClassRoles.DPS: 
                    if (fighter.name.Contains("Sith")) 
                        _activeFighters.Add(fighter); 
                    break;
                case ClassRoles.Sentinel: 
                    if (fighter.name.Contains("Grey")) 
                        _activeFighters.Add(fighter); 
                    break;
            }
        }

        Debug.Log("Filtered fighters for role " + roleEnum + ": " + _activeFighters.Count + " fighters");
    }
}

public enum ClassRoles { Guardian, DPS, Sentinel, Tank, Healer }
