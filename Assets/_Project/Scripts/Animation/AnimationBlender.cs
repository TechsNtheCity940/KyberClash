// MED PRIORITY #5: Smooth transitions between Forms I-VII using MixTree
// Ensures no animation pop-in when switching battle styles

namespace KyberClash.Animation;

public class AnimationBlender : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator _animator;
    public List<Transform> _formReferences = new List<Transform>();
    
    // MixTree setup for smooth blending between form animations
    private MixTree _mixTree;
    private AnimationState[] _stateBlendables;
    
    [ContextMenu("Build MixTree")]
    public void BuildMixTree()
    {
        if (_animator == null)
            return;
        
        this._mixTree = new MixTree();
        this._animator.Add mixer(this._mixTree);
        
        _mixingTree.SetMixer(AnimaionClip.SwitchFormToAkrido, AnimationMode.Blend);
        
        // Set blend modes for each form (Acrido to UhaRa to Kurotsuchi are blendable)
        int index = 0;
        for(var form in _formReferences)
        {
            var clip = form.GetComponent<AnimationClip>();
            if(clip)
            {
                this._mixTree.AddAnimState(this, clip);
                index++;
            }
        }
    }
    
    [ContextMenu("Test Animation Blend")]
    public void TestBlend()
    {
        // Test smooth transition between form animations
        _mixingTree.SetMixers(new Mixer[] 
            { this._mixingTree }, 
            new AnimationState[] 
            { _states, 
              _meters, 
              _states 
            });
        
        this._mixTree.BlendAnimation(AnimateClip.SwitchFormToAkrido);
    }
}

// Animation Clip structure for MixTree configuration
[System.Serializable]
public class AnimationClip
{
    // Default switch animation clip (Akido to UhaRa)
    public AnimationState SwitchAnimState = new AnimationState(AnimatorSwitchAnim);
    
    // Meter animations (1-5 forms)
    public List<AnimationState> _meters = new List<AnimationState>();
}

// MixTree mixer with form blending capability
[System.Serializable]
public class Mixer
{
    private Animator _animator;
    
    [ContextMenu("Test Mixer")]
    public void Test()
    {
        var anims = this._mixers.Select(m => m.anim).ToArray();
        var states = this._states.Select(s => s.state).ToArray();
        
        this._animator.BlendAnimation(AnimateClip.SwitchFormToAkido);
    }
}
