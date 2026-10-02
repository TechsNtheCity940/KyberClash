using System.Collections;
using UnityEngine;

namespace KyberKlash.VFX
{
    /// <summary>
    /// Simple hit impact VFX - spawns particles and handles lifetime
    /// </summary>
    public class HitImpactVFX : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private ParticleSystem[] particleSystems;
        [SerializeField] private Light impactLight;
        [SerializeField] private float lightDuration = 0.1f;
        
        [Header("Settings")]
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private bool autoReturnToPool = true;
        
        private float spawnTime;
        private VFXPool pool;
        private string poolKey;
        
        public void Initialize(VFXPool pool, string key)
        {
            this.pool = pool;
            this.poolKey = key;
            spawnTime = Time.time;
            
            // Play particles
            foreach (var ps in particleSystems)
            {
                if (ps != null) ps.Play();
            }
            
            // Flash light
            if (impactLight != null)
            {
                StartCoroutine(FlashLight());
            }
            
            // Auto-destroy/return
            if (autoReturnToPool && pool != null && !string.IsNullOrEmpty(poolKey))
            {
                StartCoroutine(AutoReturn());
            }
            else
            {
                Destroy(gameObject, lifetime);
            }
        }
        
        private IEnumerator FlashLight()
        {
            if (impactLight == null) yield break;
            
            impactLight.enabled = true;
            float startIntensity = impactLight.intensity;
            float elapsed = 0f;
            
            while (elapsed < lightDuration)
            {
                elapsed += Time.deltaTime;
                impactLight.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / lightDuration);
                yield return null;
            }
            
            impactLight.enabled = false;
        }
        
        private IEnumerator AutoReturn()
        {
            yield return new WaitForSeconds(lifetime);
            
            if (pool != null && !string.IsNullOrEmpty(poolKey))
            {
                pool.Return(poolKey, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void OnValidate()
        {
            if (particleSystems == null || particleSystems.Length == 0)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>();
            }
            
            if (impactLight == null)
            {
                impactLight = GetComponentInChildren<Light>();
            }
        }
    }
}