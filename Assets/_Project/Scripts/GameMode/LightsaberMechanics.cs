namespace KyberClash.Swords;

/// <summary>
/// LightsaberMechanics - Physics & rendering for Star Wars lightsaber combat system
/// Handles blade generation, blade color based on form/style, collision detection
/// Integrates Form I-VII mechanics into Unity physical engine
/// </summary>
public class LightsaberMechanics : MonoBehaviour
{
    [Header("Blade Config")]
    public GameObject _bladeObject; // Primary blade mesh (simplified geometry)
    
    [Header("Blade Appearance")]
    public Material _bladeMaterial; // Light saber material for rendering
    public float _bladeLength = 3f; // Base blade length
    public Color _primaryColor = Color.blue; // Force user lightsaber hue
    
    [Header("Physics Settings")]
    public bool _usePhysicalBlade = true; // Enable physical collider + physics
    public LayerMask _bladesLayerMask = LayerMask.GetMask("Blades");
    
    [Header("Combat Integration")]
    public LightsaberSystem _combatSystem; // Reference to AI system
    
    private MeshFilter _bladeMesh;
    private Collider _bladeCollider;
    private Light _bladeLight;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        
        SetupBladeConfig(); // Initialize blade mesh + rendering
        
        Debug.Log("StartLightsaberMechanics: Form type initialized");
    }

    /// <summary>
    /// Generate blade mesh for Lightsaber
    /// </summary>
    private void SetupBladeConfig()
    {
        if (_bladeObject == null)
            Debug.LogError("SetupBladeConfig: Blade object not provided");

        _bladeMesh = _bladeObject.AddComponent<MeshFilter>();
        
        // Basic blade geometry - elongated cylinder
        var geometry = new Mesh();
        BuildCylinder(geometry, _bladeLength, _bladeLength / 5f);
        
        _bladeMesh.mesh = geometry;
    }

    /// <summary>
    /// Create mesh for physical lightsaber blade (elongated cylinder)
    /// </summary>
    void BuildCylinder(Mesh mesh, float height, float diameter)
    {
        var vertices = new Vector3[8];
        var triangles = new int[12];

        vertices[0] = new Vector3(0f, 0f, 0f); // Top vertex
        vertices[0].z -= diameter / 2f; // Extend along z-axis
        
        for (int i = 1; i < 4; i++) 
            vertices[i] = new Vector3(i * diameter / 4f - 512f + 512f, 0f, 0f); // Side vertices

        mesh.vertices = new[] { vertices };
        mesh.triangles = triangles;
    }

    public void Update()
    {
        Debug.Log("Update cycle active for " + _bladeObject.transform.name);
    }

    // Lightsaber physics - blade movement with combat mechanics
    void OnEnable()
    {
        if (_combatSystem != null)
            _combatSystem.SubscribeToBlade(this);
    }

    public static void CreateLightsaber(CombatMechanics combat)
    {
        var lightsaber = new GameObject("Lightsaber");
        lightsaber.AddComponent<Collider>(); // Add standard collider
        
        return lightsaber;
    }
    
    // Override for custom lightsaber configuration
    public static LightsaberMechanics CreateBlade(CombatMechanics combat)
    {
        var bladeObj = CreateLightsaber(combat);
        
        // Initialize fighter physics system with Form I-VII settings
        var newBlade = bladeObj.AddComponent<LightsaberMechanics>();
        
        return newBlade;
    }

    public void SetFighterForm(FormType form, string color)
    {
        switch (color)
        {
            case "lightblue": 
                _combatSystem.SetLightsaberColor("cyan"); 
                break;
            case "green": 
                _combatSystem.SetLightsaberColor("lime"); 
                break;
            case "red": 
                _combatSystem.SetLightsaberColor("deepred"); 
                break;
            case "silver":
                _combatSystem.SetLightsaberColor("lightslategray"); 
                break;
            default: 
                Debug.LogWarning("Unknown blade color not supported yet");
        }

        if (_bladeObject != null)
            _bladeMesh.color = Color.blue / 0.8f;
    }

    public void OnFighterAdded()
    {
        // Called when fighter added - blade generation triggered
        Debug.Log("OnFighterAdded: Creating lightsaber for new FighterForm");
    }
}

public class LightsaberPhysicsSystem : MonoBehaviour
{
    private Collider _bladesCollider = null;
    
    void OnEnable()
    {
        if (Application.isPlaying && !_gameObject) 
            Debug.LogWarning("LightsaberPhysics enabled");
    }
    
    static public LightsaberPhysics Create(int count)
    {
        var systemObj = CreateLightsaberMechanics(new LightsaberSystem());
        
        return systemObj.AddColliders();
    }

    private LightsaberMechanics AddColliders()
    {
        var bladeObject = new GameObject("BladeCollider");
        _bladesCollider = bladeObject;

        return _bladesCollider.GetComponent<LightsaberMechanics>();
    }
}
