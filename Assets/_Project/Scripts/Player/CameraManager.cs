using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

namespace KyberKlash.Player
{
    /// <summary>
    /// Camera manager using Cinemachine for dynamic fighter camera.
    /// Handles multiple players with target group.
    /// Compatible with Cinemachine 3.x (Unity 6)
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Header("Cinemachine")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private CinemachineTargetGroup targetGroup;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Camera Settings")]
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private float fixedCameraZ = -24f;
        [SerializeField] private float centerYOffset = 2.5f;
        [SerializeField] private float minOrthoSize = 5f;
        [SerializeField] private float maxOrthoSize = 15f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float horizontalPadding = 4f;
        [SerializeField] private float verticalPadding = 3f;
        [SerializeField] private bool disableVirtualCameraForFixedSideView = true;

        [Header("Screen Shake")]
        [SerializeField] private float defaultShakeDuration = 0.1f;
        [SerializeField] private float defaultShakeIntensity = 1f;

        private readonly List<PlayerController> players = new List<PlayerController>();

        public static CameraManager Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (sceneCamera == null)
            {
                sceneCamera = Camera.main;
            }

            if (disableVirtualCameraForFixedSideView && virtualCamera != null)
            {
                virtualCamera.enabled = false;
            }

            impulseSource = GetComponent<CinemachineImpulseSource>();
            ConfigureSideViewCamera();
        }

        protected virtual void Start()
        {
            // Find all players
            players.Clear();
            players.AddRange(FindObjectsByType<PlayerController>(FindObjectsSortMode.None));
            SetupTargetGroup();
            ConfigureSideViewCamera();
        }

        /// <summary>
        /// Register a player with the camera system
        /// </summary>
        public void RegisterPlayer(PlayerController player)
        {
            if (player == null) return;

            if (!players.Contains(player))
            {
                players.Add(player);
            }

            // Add to target group using Cinemachine 3.x API
            if (targetGroup != null)
            {
                targetGroup.AddMember(player.transform, 1f, 2f);
            }
        }

        /// <summary>
        /// Unregister a player
        /// </summary>
        public void UnregisterPlayer(PlayerController player)
        {
            if (player == null) return;

            players.Remove(player);

            if (targetGroup != null)
            {
                targetGroup.RemoveMember(player.transform);
            }
        }

        private void SetupTargetGroup()
        {
            if (targetGroup == null || players == null) return;

            foreach (var player in players)
            {
                if (player != null)
                {
                    targetGroup.AddMember(player.transform, 1f, 2f);
                }
            }
        }

        protected virtual void LateUpdate()
        {
            ConfigureSideViewCamera();
            FramePlayersSideView();
        }

        private void ConfigureSideViewCamera()
        {
            if (sceneCamera == null)
            {
                sceneCamera = Camera.main;
            }

            if (sceneCamera == null)
            {
                return;
            }

            sceneCamera.orthographic = true;
            sceneCamera.transform.rotation = Quaternion.identity;

            if (sceneCamera.GetComponent<AudioListener>() == null)
            {
                sceneCamera.gameObject.AddComponent<AudioListener>();
            }
        }

        private void FramePlayersSideView()
        {
            if (sceneCamera == null) return;
            if (players == null || players.Count == 0) return;

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            int activeCount = 0;

            foreach (var player in players)
            {
                if (player != null && !player.IsDead)
                {
                    Vector3 pos = player.transform.position;
                    min = Vector3.Min(min, pos);
                    max = Vector3.Max(max, pos);
                    activeCount++;
                }
            }

            if (activeCount == 0) return;

            Vector3 center = (min + max) * 0.5f;
            center.y += centerYOffset;
            center.z = fixedCameraZ;

            float width = (max.x - min.x) + horizontalPadding;
            float height = (max.y - min.y) + verticalPadding;
            float aspect = sceneCamera.aspect > 0f ? sceneCamera.aspect : (float)Screen.width / Mathf.Max(1, Screen.height);
            float requiredSize = Mathf.Max(height * 0.5f, width / (2f * aspect));
            requiredSize = Mathf.Clamp(requiredSize, minOrthoSize, maxOrthoSize);

            sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, center, zoomSpeed * Time.deltaTime);
            sceneCamera.orthographicSize = Mathf.Lerp(sceneCamera.orthographicSize, requiredSize, zoomSpeed * Time.deltaTime);

            // Keep the camera framing inside the stage's visible bounds so the camera
            // never reveals the off-screen blast zones (Smash convention: the blast lines
            // are deliberately off-screen; a radar shows off-screen fighters instead).
            ClampCameraToStageBounds();
        }

        /// <summary>
        /// Clamp the camera center so its view rectangle stays within the stage camera bounds
        /// (blast zones remain unseen, matching Smash's framing).
        /// </summary>
        private void ClampCameraToStageBounds()
        {
            var stage = KyberKlash.Stage.StageManager.Instance?.CurrentStageData;
            if (stage == null) return;

            float halfH = sceneCamera.orthographicSize;
            float halfW = halfH * sceneCamera.aspect;

            // If the view is larger than the bounds, just center on the stage.
            float boundsW = (stage.cameraRight - stage.cameraLeft) * 0.5f;
            float boundsH = (stage.cameraTop - stage.cameraBottom) * 0.5f;
            float centerX = (stage.cameraLeft + stage.cameraRight) * 0.5f;
            float centerY = (stage.cameraBottom + stage.cameraTop) * 0.5f;

            if (halfW * 2f >= (stage.cameraRight - stage.cameraLeft) ||
                halfH * 2f >= (stage.cameraTop - stage.cameraBottom))
            {
                Vector3 p = sceneCamera.transform.position;
                p.x = centerX;
                p.y = centerY;
                sceneCamera.transform.position = p;
                return;
            }

            Vector3 pos = sceneCamera.transform.position;
            pos.x = Mathf.Clamp(pos.x, centerX - (boundsW - halfW), centerX + (boundsW - halfW));
            pos.y = Mathf.Clamp(pos.y, centerY - (boundsH - halfH), centerY + (boundsH - halfH));
            sceneCamera.transform.position = pos;
        }

        /// <summary>
        /// For a given player, return the on-screen-relative direction toward them when they
        /// are off-screen (for the HUD radar arrow), or Vector2.zero if on-screen.
        /// </summary>
        public static Vector2 GetOffScreenDirection(PlayerController player)
        {
            if (player == null || Camera.main == null) return Vector2.zero;
            if (!player.IsOffScreen) return Vector2.zero;

            Vector3 view = Camera.main.WorldToViewportPoint(player.transform.position);
            // Direction from screen center to the clamped edge where the fighter is.
            float x = Mathf.Clamp(view.x, 0.04f, 0.96f);
            float y = Mathf.Clamp(view.y, 0.04f, 0.96f);
            Vector2 toEdge = new Vector2(x - 0.5f, y - 0.5f);
            if (toEdge.sqrMagnitude < 0.0001f) toEdge = new Vector2(view.x < 0.5f ? -1f : 1f, 0f);
            return toEdge.normalized;
        }

        /// <summary>
        /// Trigger screen shake
        /// </summary>
        public void ScreenShake(float intensity = -1f, float duration = -1f)
        {
            if (impulseSource != null)
            {
                float i = intensity > 0f ? intensity : defaultShakeIntensity;
                float d = duration > 0f ? duration : defaultShakeDuration;
                impulseSource.GenerateImpulse(i);
            }
        }

        /// <summary>
        /// Trigger screen shake with custom impulse
        /// </summary>
        public void ScreenShake(CinemachineImpulseSource source, float intensity)
        {
            source.GenerateImpulse(intensity);
        }

        /// <summary>
        /// Focus on specific player (for KO, respawn, etc.)
        /// </summary>
        public void FocusOnPlayer(PlayerController player, float duration = 2f)
        {
            if (virtualCamera == null || targetGroup == null || player == null) return;

            // Temporarily increase weight
            for (int i = 0; i < targetGroup.Targets.Count; i++)
            {
                if (targetGroup.Targets[i].Object == player.transform)
                {
                    targetGroup.Targets[i].Weight = 2f;
                    Invoke(nameof(ResetWeights), duration);
                    break;
                }
            }
        }

        private void ResetWeights()
        {
            if (targetGroup == null) return;

            for (int i = 0; i < targetGroup.Targets.Count; i++)
            {
                targetGroup.Targets[i].Weight = 1f;
            }
        }
    }
}
