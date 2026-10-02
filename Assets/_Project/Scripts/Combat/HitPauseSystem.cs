using UnityEngine;

namespace KyberKlash.Combat
{
    /// <summary>
    /// Frame-perfect hit pause system at 60fps
    /// Pauses game time for hit confirmation feedback
    /// </summary>
    public class HitPauseSystem : MonoBehaviour
    {
        public static HitPauseSystem Instance { get; private set; }

        private bool isPaused;
        private float pauseTimer;
        private float pauseDuration;
        private float originalTimeScale;
        private int pauseFrames;
        private int pausedFrames;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Request hit pause for specified frames at 60fps
        /// </summary>
        /// <param name="frames">Number of frames to pause (at 60fps)</param>
        public void RequestHitPause(int frames)
        {
            if (frames <= 0) return;

            // Don't stack hit pauses - take the longest
            if (isPaused && frames <= pauseFrames)
                return;

            pauseFrames = frames;
            pauseDuration = frames / 60f; // 60fps = 1/60 second per frame

            if (!isPaused)
            {
                StartPause();
            }
            else
            {
                // Extend existing pause
                pauseTimer = 0f;
                pauseDuration = frames / 60f;
                pauseFrames = frames;
                pausedFrames = 0;
            }
        }

        private void StartPause()
        {
            isPaused = true;
            pauseTimer = 0f;
            pausedFrames = 0;
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (!isPaused) return;

            // Use unscaled delta time to count frames
            float unscaledDelta = Time.unscaledDeltaTime;
            pauseTimer += unscaledDelta;
            pausedFrames = Mathf.RoundToInt(pauseTimer * 60f);

            if (pauseTimer >= pauseDuration)
            {
                EndPause();
            }
        }

        private void EndPause()
        {
            isPaused = false;
            Time.timeScale = originalTimeScale;
            pauseTimer = 0f;
            pauseFrames = 0;
            pausedFrames = 0;
        }

        /// <summary>
        /// Request hit pause with attack-specific duration
        /// </summary>
        public void RequestHitPause(float hitPauseFrames)
        {
            RequestHitPause(Mathf.RoundToInt(hitPauseFrames));
        }

        /// <summary>
        /// Check if currently in hit pause
        /// </summary>
        public bool IsPaused => isPaused;

        /// <summary>
        /// Get remaining pause frames
        /// </summary>
        public int RemainingFrames => isPaused ? Mathf.Max(0, pauseFrames - pausedFrames) : 0;
    }
}