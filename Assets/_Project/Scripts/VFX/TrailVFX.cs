using UnityEngine;

namespace KyberKlash.VFX
{
    /// <summary>
    /// Trail effect for saber swings and special moves
    /// </summary>
    public class TrailVFX : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private Gradient trailGradient;
        [SerializeField] private float trailTime = 0.3f;
        [SerializeField] private float startWidth = 0.15f;
        [SerializeField] private float endWidth = 0f;
        
        [Header("Emission")]
        [SerializeField] private bool emitOnStart = true;
        
        private bool isEmitting = false;
        
        void Awake()
        {
            if (trailRenderer == null)
                trailRenderer = GetComponent<TrailRenderer>();
            
            SetupTrail();
        }
        
        void OnEnable()
        {
            if (emitOnStart)
                StartEmission();
        }
        
        void SetupTrail()
        {
            if (trailRenderer == null) return;
            
            trailRenderer.time = trailTime;
            trailRenderer.startWidth = startWidth;
            trailRenderer.endWidth = endWidth;
            trailRenderer.minVertexDistance = 0.01f;
            
            if (trailGradient != null)
            {
                trailRenderer.colorGradient = trailGradient;
            }
            
            // Ensure proper material
            if (trailRenderer.material == null || trailRenderer.material.shader.name != "Sprites/Default")
            {
                trailRenderer.material = new Material(Shader.Find("Sprites/Default"))
                {
                    color = Color.white
                };
            }
            
            trailRenderer.emitting = false;
        }
        
        public void StartEmission()
        {
            if (trailRenderer != null)
            {
                trailRenderer.emitting = true;
                trailRenderer.Clear();
                isEmitting = true;
            }
        }
        
        public void StopEmission()
        {
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                isEmitting = false;
            }
        }
        
        public void SetColor(Gradient gradient)
        {
            trailGradient = gradient;
            if (trailRenderer != null)
            {
                trailRenderer.colorGradient = gradient;
            }
        }
        
        public void SetWidth(float start, float end)
        {
            startWidth = start;
            endWidth = end;
            if (trailRenderer != null)
            {
                trailRenderer.startWidth = start;
                trailRenderer.endWidth = end;
            }
        }
        
        public void SetTime(float time)
        {
            trailTime = time;
            if (trailRenderer != null)
            {
                trailRenderer.time = time;
            }
        }
        
        void OnValidate()
        {
            if (trailRenderer == null)
                trailRenderer = GetComponent<TrailRenderer>();
            
            SetupTrail();
        }
    }
}