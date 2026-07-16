using UnityEngine;

namespace KyberKlash.Data
{
    /// <summary>
    /// Stage layout archetypes (Smash taxonomy):
    /// Battlefield = main platform + soft platforms; FinalDestination = flat single platform;
    /// Omega = a stage reskinned to a flat FD-like layout (no platforms/hazards).
    /// </summary>
    public enum StageLayoutType
    {
        Battlefield,    // Main platform + 2-3 soft (pass-through) platforms
        FinalDestination, // Flat single platform, no soft platforms
        Omega,          // Flat FD-style variant of a themed stage (no platforms/hazards)
        Custom          // Fully data-driven (use platforms array directly)
    }

    /// <summary>
    /// Stage definition - data-driven stage configuration
    /// </summary>
    [CreateAssetMenu(fileName = "NewStage", menuName = "Kyber Clash/Stage/Stage Data")]
    public class StageData : BaseDataSO
    {
        [Header("Stage Identity")]
        public string stageId;
        public string displayName;
        [TextArea(2, 4)] public string stageDescription;
        public Sprite stageThumbnail;
        public Sprite stageIcon;
        public string stageName => displayName; // Alias for compatibility

        [Header("Scene & Visuals")]
        public string sceneName; // Scene asset name
        public GameObject stagePrefab; // Full stage prefab

        [Header("Layout Archetype")]
        [Tooltip("Smash-style stage taxonomy. Battlefield=platforms, FinalDestination=flat, Omega=flat themed, Custom=use platforms[].")]
        public StageLayoutType layoutType = StageLayoutType.Battlefield;

        [Header("Blast Zones (Ring-out boundaries)")]
        [Tooltip("Left blast zone X position")]
        public float leftBlastZone = -20f;

        [Tooltip("Right blast zone X position")]
        public float rightBlastZone = 20f;

        [Tooltip("Top blast zone Y position")]
        public float topBlastZone = 15f;

        [Tooltip("Bottom blast zone Y position")]
        public float bottomBlastZone = -15f;

        [Tooltip("Visual blast zone indicators")]
        public GameObject blastZoneVFXPrefab;

        [Header("Meteor / Spike Blast Line (Ultimate)")]
        [Tooltip("A lower sub-boundary above the real bottom blast zone. If a fighter is moving DOWN faster than meteorBlastSpeed, they KO there (enables spikes / risky fast-fall Dairs). Set >= bottomBlastZone to disable.")]
        public float meteorBlastZone = -13f;
        [Tooltip("Downward speed (units/s) required to die on the meteor line.")]
        public float meteorBlastSpeed = 18f;

        [Header("Camera / Off-Screen (Magnifying-Glass) Bounds")]
        [Tooltip("Camera view rectangle (usually tighter than blast zones). Going past this but inside blast zones = off-screen.")]
        public float cameraLeft = -14f;
        public float cameraRight = 14f;
        public float cameraTop = 10f;
        public float cameraBottom = -10f;
        [Tooltip("Damage-per-second applied while a fighter is off-camera (magnifying-glass nudges them back).")]
        public float offScreenDamagePerSecond = 12f;

        [Header("Platforms (soft, pass-through)")]
        [Tooltip("Soft platforms for Battlefield/Custom layouts. Ignored for FinalDestination/Omega.")]
        public StagePlatform[] platforms;

        [Header("Ledges")]
        [Tooltip("Ledge anchor points on the main platform (for grab/hang/recovery). Auto-derived if empty.")]
        public Transform[] ledgeAnchors;

        [Header("Hazards")]
        public StageHazard[] hazards;

        [Header("Stage Properties")]
        public bool hasHazards = true;
        public bool isTournamentLegal = true; // For competitive play
        public int maxPlayers = 4;

        [Header("Music")]
        public AudioClip stageMusic;
        public float musicVolume = 0.8f;

        /// <summary>
        /// Check if a position is outside blast zones (ring out)
        /// </summary>
        public bool IsOutOfBounds(Vector3 position)
        {
            return position.x < leftBlastZone ||
                   position.x > rightBlastZone ||
                   position.y > topBlastZone ||
                   position.y < bottomBlastZone;
        }

        /// <summary>
        /// Check the meteor/spike sub-line: only triggers when falling fast.
        /// Returns true if the fighter should KO on the meteor line.
        /// </summary>
        public bool IsOnMeteorLine(Vector3 position, float downwardSpeed)
        {
            if (meteorBlastZone <= bottomBlastZone) return false; // disabled
            return position.y < meteorBlastZone && downwardSpeed > meteorBlastSpeed;
        }

        /// <summary>
        /// Check if a position is outside the visible camera rectangle but still
        /// inside the blast zones (Smash "magnifying glass" off-screen state).
        /// </summary>
        public bool IsOffScreen(Vector3 position)
        {
            if (IsOutOfBounds(position)) return false; // already a ring-out
            return position.x < cameraLeft || position.x > cameraRight ||
                   position.y > cameraTop || position.y < cameraBottom;
        }

        /// <summary>
        /// Get the nearest blast zone direction from a position
        /// </summary>
        public Vector3 GetBlastZoneDirection(Vector3 position)
        {
            float distLeft = position.x - leftBlastZone;
            float distRight = rightBlastZone - position.x;
            float distTop = topBlastZone - position.y;
            float distBottom = position.y - bottomBlastZone;

            float minDist = Mathf.Min(distLeft, distRight, distTop, distBottom);

            if (minDist == distLeft) return Vector3.left;
            if (minDist == distRight) return Vector3.right;
            if (minDist == distTop) return Vector3.up;
            return Vector3.down;
        }

        /// <summary>
        /// Get the direction a fighter should be nudged when off-screen (toward stage center).
        /// </summary>
        public Vector3 GetOffScreenRecenterDirection(Vector3 position)
        {
            float centerX = (leftBlastZone + rightBlastZone) * 0.5f;
            float centerY = (bottomBlastZone + topBlastZone) * 0.5f;
            return new Vector3(centerX - position.x, centerY - position.y, 0f).normalized;
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(stageId))
                stageId = name;

            if (string.IsNullOrEmpty(displayName))
                displayName = name;

            leftBlastZone = Mathf.Min(leftBlastZone, rightBlastZone - 1f);
            rightBlastZone = Mathf.Max(rightBlastZone, leftBlastZone + 1f);
            bottomBlastZone = Mathf.Min(bottomBlastZone, topBlastZone - 1f);
            topBlastZone = Mathf.Max(topBlastZone, bottomBlastZone + 1f);

            cameraLeft = Mathf.Clamp(cameraLeft, leftBlastZone, rightBlastZone);
            cameraRight = Mathf.Clamp(cameraRight, cameraLeft + 0.5f, rightBlastZone);
            cameraBottom = Mathf.Clamp(cameraBottom, bottomBlastZone, topBlastZone);
            cameraTop = Mathf.Clamp(cameraTop, cameraBottom + 0.5f, topBlastZone);

            if (meteorBlastZone < bottomBlastZone) meteorBlastZone = bottomBlastZone;

            if (hazards == null)
                hazards = new StageHazard[0];

            if (platforms == null)
                platforms = new StagePlatform[0];
        }
    }

    /// <summary>
    /// Soft platform definition (pass-through; jump up through, drop through with down).
    /// </summary>
    [System.Serializable]
    public class StagePlatform
    {
        public string platformName;
        public Vector3 position;     // Local position relative to stage root
        public Vector3 size = new Vector3(5f, 0.5f, 2f);
        public bool dropThrough = true; // Can be dropped through with down input
        public GameObject platformPrefab; // Optional override visual
    }

    /// <summary>
    /// Stage hazard definition
    /// </summary>
    [System.Serializable]
    public class StageHazard
    {
        public string hazardName;
        public HazardType hazardType;
        public Transform hazardTransform; // Position in stage
        public Vector3 hazardSize;
        public float activationDelay = 0f;
        public float activeDuration = 5f;
        public float cooldown = 10f;
        public bool randomActivation = false;
        public float randomActivationChance = 0.3f;
        public GameObject warningVFXPrefab;
        public GameObject activeVFXPrefab;
        public AudioClip warningSFX;
        public AudioClip activeSFX;
        public float damage = 15f;
        public float knockback = 10f;
        public Vector3 knockbackDirection = Vector3.up;

        public enum HazardType
        {
            DamageZone,      // Continuous damage area
            ProjectileLauncher, // Fires projectiles
            PlatformCollapse,  // Platform falls
            StageTransition,   // Stage transforms
            EnvironmentalEffect // Wind, gravity change, etc.
        }
    }
}