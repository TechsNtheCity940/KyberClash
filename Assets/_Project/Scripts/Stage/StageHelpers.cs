using UnityEngine;

namespace KyberKlash.Stage
{
    /// <summary>
    /// Marks a collider as a soft (pass-through) platform. The player can jump up through it
    /// from below and drop through it with down input, but lands on it from above.
    /// Implemented by having the PlayerController ignore this collider while rising / dropping.
    /// </summary>
    public class SoftPlatform : MonoBehaviour
    {
        [Tooltip("Allows the player to drop through by holding down + jump/dash.")]
        public bool dropThrough = true;

        /// <summary>
        /// Returns true if the player should collide with (land on) this platform given their
        /// vertical velocity and whether they are actively dropping through.
        /// </summary>
        public bool ShouldSupport(float verticalVelocity, bool dropping)
        {
            if (dropping) return false;
            // Only support when moving downward (or near-stationary) so you pass up through it.
            return verticalVelocity <= 0.01f;
        }
    }

    /// <summary>
    /// Empty marker placed at a platform/main-stage edge so fighters can grab, hang, and
    /// recover (Smash-style ledge mechanics).
    /// </summary>
    public class LedgeAnchor : MonoBehaviour { }
}
