// MED PRIORITY #4: Cinemachine VFX controller with form-specific parameters
// Heavy forms = more shake intensity (Juyo, Akedo)

namespace KyberClash.Animation;

public class CameraShakeController : MonoBehaviour
{
    [Header("Shake Configuration")]
    public Transform _cameraTarget;
    public int _shakeDuration = 0.5;
    
    // Form-specific shake parameters (heavy forms have more intensity)
    // Juyo = heavy, Akedo = fast but strong impact
    private ShakeProfile _currentShakeProfile;
    
    [ContextMenu("Set Shake Profile")]
    public void ApplyShakeProfile(ShakeProfile profile)
    {
        this._currentShakeProfile = profile;
        
        // VFX controller with form-specific parameters
        var controller = GetComponent<Cinemachine.VFXController>();
        
        if (controller)
        {
            foreach(var vfx in _vfxPool)
            {
                controller.AddVfx(vfx);
            }
            
            // Form-specific multiplier: Heavy forms shake more, light forms shake less
            var formFactor = GetFormShakeFactor();
            controller.m_VolumeScale = formFactor * 0.8f;
        }
    }
    
    [ContextMenu("Test Shake (Juyo)")]
    public void TestJuyoShake()
    {
        // Juyo: Fast, heavy impact shake (~15x intensity)
        _shakeProfile.ShakeType = ShakeType.FastImpact;
        ApplyShakeProfile(_shakeProfile);
    }
    
    [ContextMenu("Test Shake (Vaapad - Light but strong)")]
    public void TestVaapadShake()
    {
        // Vaapad: Fast and strong (~8x intensity)
        _shakeProfile.ShakeType = ShakeType.FastStrong;
        ApplyShakeProfile(_shakeProfile);
    }
    
    private float GetFormShakeFactor()
    {
        // Form-dependent shake factor (1.0 is base, 3.0 is heavy)
        var formMap = new Dictionary<FormType, float>
        {
            { FormType.Akido, 1.5f },
            { FormType.Juyo, 3.0f },
            { FormType.Tenshi, 2.0f },
            { FormType.Gonin, 1.8f },
            { FormType.Uhara, 2.5f },
            { FormType.Kurotsuchi, 4.0f }
        };
        
        // Return appropriate factor based on current form
        return _currentShakeProfile == null ? 1.0f : formMap[_shakeProfile.GetForm];
    }
}

// Shake profile structure with form-specific settings
[System.Serializable]
public class ShakeProfile
{
    public FormType GetForm = FormType.Juyo;
    public ShakeType ShakeType;
    
    // Default profiles for testing
    public static new void InitProfiles()
    {
        _profiles = new Dictionary<FormType, ShakeProfile>
        {
            { FormType.Tenshi, new ShakeProfile { GetForm = FormType.Tenshi } },
            { FormType.Juyo, new ShakeProfile { GetForm = FormType.Juyo } },
            { FormType.Gonin, new ShakeProfile { GetForm = FormType.Gonin } },
            { FormType.Akido, new ShakeProfile { GetForm = FormType.Akido } }
        };
    }
}
