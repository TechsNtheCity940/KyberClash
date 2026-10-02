using UnityEngine;

namespace KyberKlash.VFX
{
    /// <summary>
    /// Screen shake effect - integrates with CameraManager/Cinemachine for hit feedback
    /// </summary>
    public class ScreenShakeVFX : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float defaultShakeDuration = 0.2f;
        [SerializeField] private float defaultShakeIntensity = 0.3f;
        [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        private Vector3 originalPosition;
        private float shakeTimer = 0f;
        private float currentDuration = 0f;
        private float currentIntensity = 0f;
        private bool isShaking = false;
        
        public static ScreenShakeVFX Instance { get; private set; }
        public static event System.Action<float, float> OnShakeRequested;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            originalPosition = transform.localPosition;
        }
        
        /// <summary>
        /// Trigger screen shake with specific intensity and duration
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            if (isShaking && duration <= currentRemainingTime())
            {
                return; // Don't interrupt with shorter shake
            }
            
            originalPosition = transform.localPosition;
            shakeTimer = 0f;
            currentDuration = duration;
            currentIntensity = intensity > 0 ? intensity : defaultShakeIntensity;
            isShaking = true;
        }
        
        private float currentRemainingTime()
        {
            return isShaking ? (currentDuration - shakeTimer) : 0f;
        }
        
        void Update()
        {
            if (!isShaking) return;
            
            shakeTimer += Time.deltaTime;
            
            if (shakeTimer >= currentDuration)
            {
                isShaking = false;
                transform.localPosition = originalPosition;
                return;
            }
            
            float progress = shakeTimer / currentDuration;
            float intensity = shakeCurve.Evaluate(progress) * currentIntensity;
            
            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * intensity,
                Random.Range(-1f, 1f) * intensity,
                0f
            );
            
            transform.localPosition = originalPosition + shakeOffset;
        }
        
        /// <summary>
        /// Static method for external shake requests
        /// </summary>
        public static void TriggerShake(float intensity, float duration)
        {
            OnShakeRequested?.Invoke(intensity, duration);
            
            if (Instance != null)
            {
                Instance.Shake(intensity, duration);
            }
        }
    }
}