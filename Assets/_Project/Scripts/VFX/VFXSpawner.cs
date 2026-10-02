// MED PRIORITY #6: Proper VFX frame release when switching forms
// Ensures TrailRenderer pool cleanup to prevent memory leaks

namespace KyberClash.VFX;

public class TrailRendererPool : MonoBehaviour
{
    [Header("VFX Pool Settings")]
    public Transform _prefabPrefab = new GameObject("TrailingBlade").transform;
    public int _maxPoolSize = 32;
    
    // VFX pool for efficient frame spawning and cleanup
    private List<TrailRenderer> _activeFrames = new();
    private ParticleSystem[] _frames;
    
    [ContextMenu("Initialize Pool")]
    public void InitializePool()
    {
        _vfxPool = new TrailRenderer[_maxPoolSize];
        
        // Generate frames for each form type (Forms I-VII)
        var forms = Enum.GetValues(typeof(FormType));
        for(int formIndex = 0; formIndex < forms.Length; formIndex++)
        {
            _frames[formIndex] = Instantiate(_prefabPrefab, null);
        }
    }
    
    // Spawn VFX frame (null-safe)
    public void SpawnFrame(Transform targetTransform, Vector3 startpos, Vector3 endpos)
    {
        if(targetTransform == null || !_vfxPool.Contains(_activeFrames))
            return; // Exit immediately if invalid
            
        for(int i = 0; i < _maxPoolSize && _activeFrames.Count > 0; i++)
            SpawnVFXFrame(startpos, endpos);
    }
    
    private void Cleanup()
    {
        _vfxPool.Clear();
        
        // Dispose all frames to prevent memory leaks
        foreach(var frame in _frames)
        {
            if(frame != null)
            {
                Destroy(frame.gameObject);
            }
        }
        
        this._activeFrames.Clear();
    }
}

// Helper for spawning frames with null checks
public class VFXSpawner : MonoBehaviour
{
    [Header("VFX Pool Settings")]
    public TrailRendererPool _vfxPool;
    
    [ContextMenu("Test Spawn Frame")]
    public void TestSpawn()
    {
        var startpos = new Vector3(0, 1f, 0);
        var endpos = new Vector3(5, -2f, 0);
        
        _vfxPool.SpawnFrame(transform, startpos, endpos);
        
        Cleanup();
        Destroy(_vfxPool.gameObject);
    }
}
