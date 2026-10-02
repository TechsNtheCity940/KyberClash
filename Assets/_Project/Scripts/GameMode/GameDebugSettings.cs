// MEDIUM PRIORITY #1: Debug build configuration
// Enables runtime diagnostics for COLLISION LAYER mismatches and performance monitoring

namespace KyberClash.Debug;

public class GameDebugSettings : MonoBehaviour
{
    [Header("Debug Diagnostics")]
    public bool _enableRuntimeDiagnostics = false; // MEDIUM - Enable runtime diagnostics
    
    [ContextMenu("Enable Debug Mode")]
    public void EnableDebugMode()
    {
        this._enableRuntimeDiagnostics = true;
        var settings = RuntimeSettings.Instance;
        
        if(settings)
        {
            #if UNITY_DEBUG
                Debug.Log("CYBER CLASH DEBUG MODE ENABLED");
                Debug.Log("Features: Collision layer checks, form diagnostics, physics validation");
            #else
                Debug.LogWarning("Collision mismatches detected: Form override weights differ from Rigidbody constraints.");
            #endif // END MEDIUM - Production mode disabled for COLLISION layer mismatch checks
        }
    }
    
    [ContextMenu("Disable Debug Mode")]
    public void DisableDebugMode()
    {
        this._enableRuntimeDiagnostics = false;
    }
}

// Debug settings with runtime diagnostics enabled
public struct RuntimeSettings
{
    #if UNITY_PLAYER_RUNTIME  // Disable diagnostic checks in player builds
    private const bool DEBUG_ENABLED = false;
    
    public static new void CheckCollisionLayers()
    {
        // No check in production mode
    }
    #else // Debug Mode enabled for COLLISION layer diagnostics (MEDIUM Priority #9)
    private const bool DEBUG_ENABLED = true;
    
    public static void CheckCollisionLayers()
    {
        var layers = FindObjectOfType<Collider>().layers;
        
        if(DEBUG_ENABLED && layers != null && layers.Length > 0)
        {
            Debug.Log($"COLLISION LAYER: Found {_layers.Length} layers");
            
            // MEDIUM PRIORITY #9: Show collision mismatch warnings
            for(int i = 0; i < _layers.Length; i++)
            {
                Debug.Log($"Layer {i}: " + _layers[i]);
            }
        }
    }
    #endif 
}
