using UnityEngine;
using System.Collections.Generic;

namespace KyberKlash.VFX
{
    /// <summary>
    /// Lightsaber visual effect - LineRenderer based with gradient and trail
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(TrailRenderer))]
    public class LightsaberVFX : MonoBehaviour
    {
        [Header("Saber Settings")]
        [SerializeField] private Transform saberBase;
        [SerializeField] private Transform saberTip;
        [SerializeField] private float saberLength = 1.5f;
        [SerializeField] private int segments = 20;

        [Header("Colors")]
        [SerializeField] private Gradient defaultGradient;
        [SerializeField] private Color coreColor = Color.white;

        [Header("Effects")]
        [SerializeField] private GameObject ignitionVFX;
        [SerializeField] private GameObject idleVFX;
        [SerializeField] private AudioClip ignitionSound;
        [SerializeField] private AudioClip idleHumSound;
        [SerializeField] private AudioClip swingSound;
        [SerializeField] private AudioClip clashSound;

        // Components
        private LineRenderer lineRenderer;
        private TrailRenderer trailRenderer;
        private AudioSource audioSource;
        private Gradient currentGradient;
        private bool isIgnited = false;
        private float swingSpeedThreshold = 5f;
        private Vector3 lastTipPosition;

        public bool IsIgnited => isIgnited;
        public Gradient CurrentGradient => currentGradient;

        protected virtual void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            trailRenderer = GetComponent<TrailRenderer>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.maxDistance = 10f;
            }

            ResolveSaberReferences();
            SetupLineRenderer();
            SetupTrailRenderer();
        }

        private void SetupLineRenderer()
        {
            lineRenderer.positionCount = segments;
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 0.1f);
            lineRenderer.numCornerVertices = 4;
            lineRenderer.numCapVertices = 4;

            // Default gradient
            if (defaultGradient.colorKeys.Length == 0)
            {
                CreateDefaultGradient();
            }
            currentGradient = defaultGradient;
            lineRenderer.colorGradient = currentGradient;
        }

        private void CreateDefaultGradient()
        {
            defaultGradient = new Gradient();
            defaultGradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.blue, 0f),
                new GradientColorKey(Color.cyan, 0.5f),
                new GradientColorKey(Color.blue, 1f)
            };
            defaultGradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
        }

        private void SetupTrailRenderer()
        {
            trailRenderer.time = 0.1f;
            trailRenderer.startWidth = 0.15f;
            trailRenderer.endWidth = 0f;
            trailRenderer.minVertexDistance = 0.01f;
            trailRenderer.colorGradient = currentGradient;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.white
            };
            trailRenderer.emitting = false;
        }

        protected virtual void Start()
        {
            // Auto-ignite if not in editor
            if (!Application.isEditor || Application.isPlaying)
            {
                Ignite();
            }
        }

        protected virtual void LateUpdate()
        {
            if (!isIgnited) return;
            if (!HasSaberReferences()) return;

            UpdateSaberPositions();
            UpdateTrail();
            UpdateSwingSound();
        }

        private void UpdateSaberPositions()
        {
            if (saberBase == null || saberTip == null) return;

            Vector3[] positions = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                positions[i] = Vector3.Lerp(saberBase.position, saberTip.position, t);

                // Add slight curve for visual interest
                float curve = Mathf.Sin(t * Mathf.PI) * 0.02f;
                positions[i] += Vector3.Cross((saberTip.position - saberBase.position).normalized, Vector3.up) * curve;
            }
            lineRenderer.SetPositions(positions);
        }

        private void UpdateTrail()
        {
            if (saberBase == null || saberTip == null) return;

            if (trailRenderer.emitting)
            {
                trailRenderer.transform.position = saberTip.position;
                trailRenderer.transform.rotation = Quaternion.LookRotation(saberTip.position - saberBase.position);
            }
        }

        private void UpdateSwingSound()
        {
            if (saberTip == null) return;
            if (swingSound == null || audioSource == null) return;

            float speed = (saberTip.position - lastTipPosition).magnitude / Time.deltaTime;
            lastTipPosition = saberTip.position;

            if (speed > swingSpeedThreshold && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(swingSound, Mathf.Clamp01(speed / 20f));
            }
        }

        /// <summary>
        /// Ignite the lightsaber
        /// </summary>
        public void Ignite()
        {
            if (isIgnited) return;
            ResolveSaberReferences();

            isIgnited = true;
            lineRenderer.enabled = true;

            if (ignitionVFX != null && saberTip != null)
            {
                Object.Instantiate(ignitionVFX, saberTip.position, saberTip.rotation);
            }
            if (ignitionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(ignitionSound);
            }
            if (idleHumSound != null && audioSource != null)
            {
                audioSource.clip = idleHumSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            if (idleVFX != null && saberTip != null)
            {
                Object.Instantiate(idleVFX, saberTip.position, saberTip.rotation, saberTip);
            }

            // Animate ignition
            StartCoroutine(IgnitionAnimation());
        }

        private bool HasSaberReferences()
        {
            if (saberBase != null && saberTip != null)
            {
                return true;
            }

            ResolveSaberReferences();
            return saberBase != null && saberTip != null;
        }

        private void ResolveSaberReferences()
        {
            if (saberTip == null)
            {
                saberTip = transform.Find("SaberTip") ?? transform.parent?.Find("SaberTip");
            }

            if (saberBase == null)
            {
                Transform foundBase = transform.Find("SaberBase") ?? transform.parent?.Find("SaberBase");
                saberBase = foundBase != null ? foundBase : transform;
            }

            if (saberTip == null && saberBase != null)
            {
                GameObject tip = new GameObject("SaberTip");
                tip.transform.SetParent(saberBase);
                tip.transform.localPosition = Vector3.up * saberLength;
                saberTip = tip.transform;
            }
        }

        /// <summary>
        /// Extinguish the lightsaber
        /// </summary>
        public void Extinguish()
        {
            if (!isIgnited) return;

            isIgnited = false;

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            StartCoroutine(ExtinguishAnimation());
        }

        private System.Collections.IEnumerator IgnitionAnimation()
        {
            float duration = 0.3f;
            float timer = 0f;
            lineRenderer.widthMultiplier = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                lineRenderer.widthMultiplier = Mathf.Lerp(0f, 1f, timer / duration);
                yield return null;
            }

            lineRenderer.widthMultiplier = 1f;
            trailRenderer.emitting = true;
        }

        private System.Collections.IEnumerator ExtinguishAnimation()
        {
            trailRenderer.emitting = false;
            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                lineRenderer.widthMultiplier = Mathf.Lerp(1f, 0f, timer / duration);
                yield return null;
            }

            lineRenderer.enabled = false;
            lineRenderer.widthMultiplier = 1f;
        }

        /// <summary>
        /// Change saber color (for form changes)
        /// </summary>
        public void SetColor(Gradient gradient, Color core)
        {
            currentGradient = gradient;
            coreColor = core;
            lineRenderer.colorGradient = currentGradient;
            trailRenderer.colorGradient = currentGradient;
        }

        /// <summary>
        /// Trigger clash effect
        /// </summary>
        public void TriggerClash(Vector3 position)
        {
            if (clashSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clashSound);
            }

            // Brief color flash
            StartCoroutine(ClashFlash());
        }

        private System.Collections.IEnumerator ClashFlash()
        {
            Gradient original = currentGradient;
            Gradient flash = new Gradient();
            flash.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(coreColor, 0.5f),
                new GradientColorKey(Color.white, 1f)
            };
            flash.alphaKeys = original.alphaKeys;

            lineRenderer.colorGradient = flash;
            trailRenderer.colorGradient = flash;

            yield return new WaitForSeconds(0.05f);

            lineRenderer.colorGradient = currentGradient;
            trailRenderer.colorGradient = currentGradient;
        }

        /// <summary>
        /// Set emission state (for parry, special states)
        /// </summary>
        public void SetEmission(bool enabled, Color emissionColor = default)
        {
            if (enabled)
            {
                // Could add bloom/glow effect here
                if (emissionColor != default)
                {
                    // Modify gradient to include emission
                }
            }
        }

        protected virtual void OnValidate()
        {
            if (lineRenderer != null && currentGradient != null)
            {
                lineRenderer.colorGradient = currentGradient;
            }
        }

        /// <summary>
        /// Hit impact effect - spawns particles, decals, etc.
        /// </summary>
        public class HitImpactVFX : MonoBehaviour
        {
        [Header("Impact Settings")]
        [SerializeField] private GameObject hitParticles;
        [SerializeField] private GameObject hitDecal;
        [SerializeField] private float decalLifetime = 5f;
        [SerializeField] private float particleLifetime = 2f;

        [Header("Screen Shake")]
        [SerializeField] private float shakeIntensity = 0.3f;
        [SerializeField] private float shakeDuration = 0.1f;

        /// <summary>
        /// Play impact effect at position with normal
        /// </summary>
        public void PlayImpact(Vector3 position, Vector3 normal, GameObject particles = null, GameObject decal = null, float intensity = 0.3f)
        {
            // Particles
            GameObject particlePrefab = particles ?? VFXPool.Instance?.Get("HitParticles", position, Quaternion.LookRotation(normal));
            if (particlePrefab != null)
            {
                // Could use VFXPool here
            }

            // Decal
            if (decal != null)
            {
                GameObject decalObj = Object.Instantiate(decal, position + normal * 0.01f, Quaternion.LookRotation(normal));
                Object.Destroy(decalObj, decalLifetime);
            }

            // Screen shake
            if (KyberKlash.Player.CameraManager.Instance != null)
            {
                KyberKlash.Player.CameraManager.Instance.ScreenShake(intensity, shakeDuration);
            }
        }
    }
}
}
