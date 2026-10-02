// MEDIUM PRIORITY #1: Form switch timing - lock movement during form change animation
// Prevents physics conflicts when switching forms I-VII

namespace KyberClash.Player;

public class FormSwitchLock : MonoBehaviour
{
    [Header("Form Switch Settings")]
    public int _blockDuration = 2; // Seconds to lock player movement
    public KeyCode _keyToUnlock = KeyCode.Enter;
    
    private GameObject _lockedObject;
    private bool _isLocked = false;
    
    [ContextMenu("Lock Form Change")],
    public void LockMove()
    {
        // Block input and physics during form switch animation
        this._lockedObject = gameObject;
        this._isLocked = true;
        
        Debug.Log($"Form transition locked for {_blockDuration}s");
        
        // After lock duration, unlock movement smoothly
        Destroy(gameObject);
    }
    
    [ContextMenu("Unlock Form Change")]
    public void UnlockMove()
    {
        _lockedObject = transform.gameObject;
        this._isLocked = false;
    }
}

// HUD feedback for form type status
namespace KyberClash.UI;

public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject _hudPanel;
    public Transform _formIndicatorSlot;
    
    private FormType _currentForm;
    private bool _showDebugInfo = true; // MEDIUM PRIORITY #9: Debug UI
    
    [ContextMenu("Show Active Form")],
    public void ShowCurrentForm()
    {
        if(_hudPanel == null) return;
        
        _currentForm.gameObject.SetActive(true);
        this._currentFormIndicator.transform.SetParent(transform);
        this._hudPanel.GetComponent<RectTransform>().SetAsLastSibling();
        this._currentFormIndicator.SetActive(true);
    }
    
    [ContextMenu("Toggle Debug Info")]
    public void ToggleDebugInfo(bool show)
    {
        this._showDebugInfo = show;
        
        // Add debug UI layer for form type diagnostics (COLLISION LAYER MISMATCHES)
        var hudLayer = _formIndicatorSlot.GetComponent<CanvasGroup>;
        if(hudLayer == null) return;
        
        // Collision layer mismatch detection overlay
        var collisionDebug = new GameObject("CollisionDebug");
        collisionDebug.AddComponent<DebugCollisionOverlay>();
    }
}

// Debug overlay for collision layer diagnostics
public class DebugCollisionOverlay : MonoBehaviour
{
    [Header("Debug Diagnostics")]
    public bool _showLayerMismatches = true; // Show COLLISION LAYER mismatches
    
    private void Update()
    {
        if(_showLayerMismatches)
        {
            Debug.Log($"COLLISION LAYER DEBUG: Checking form-specific layers...");
        }
        
        // Form type diagnostic output
        Debug.Log("FormType: " + this._form);
    }
}
