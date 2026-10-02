using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using KyberKlash.Data;

namespace KyberKlash.Editor
{
    /// <summary>
    /// Editor tool to add Animation Events to all form animation clips
    /// Uses AttackSO frame data for precise frame-accurate event placement
    /// </summary>
    public class AnimationEventSetup : EditorWindow
    {
        private static AttackSO[] attackCache;

        [MenuItem("KyberClash/Animation/Add Combat Events to All Clips")]
        public static void AddCombatEventsToAllClips()
        {
            Debug.Log("=== Adding Combat Animation Events ===");

            // Cache all attacks for quick lookup
            attackCache = AssetDatabase.FindAssets("t:AttackSO")
                .Select(guid => AssetDatabase.LoadAssetAtPath<AttackSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(attack => attack != null)
                .ToArray();

            string animPath = "Assets/_Project/Animations/Characters";
            var formFolders = System.IO.Directory.GetDirectories(animPath);

            int totalClips = 0;
            int updatedClips = 0;

            foreach (var folder in formFolders)
            {
                var clips = System.IO.Directory.GetFiles(folder, "*.anim", System.IO.SearchOption.AllDirectories);

                foreach (var clipPath in clips)
                {
                    totalClips++;
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath.Replace('\\', '/'));
                    if (clip != null && AddCombatEventsToClip(clip))
                    {
                        updatedClips++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"=== Animation Events Complete: {updatedClips}/{totalClips} clips updated ===");
        }

        [MenuItem("KyberClash/Animation/Add Events to Single Clip (Select Clip)")]
        public static void AddEventsToSelectedClip()
        {
            if (attackCache == null || attackCache.Length == 0)
            {
                attackCache = AssetDatabase.FindAssets("t:AttackSO")
                    .Select(guid => AssetDatabase.LoadAssetAtPath<AttackSO>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(attack => attack != null)
                    .ToArray();
            }

            var clip = Selection.activeObject as AnimationClip;
            if (clip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select an AnimationClip", "OK");
                return;
            }

            if (AddCombatEventsToClip(clip))
            {
                EditorUtility.DisplayDialog("Success", $"Added events to {clip.name}", "OK");
                AssetDatabase.SaveAssets();
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "No events needed or already present", "OK");
            }
        }

        /// <summary>
        /// Find matching AttackSO based on clip naming convention
        /// Naming pattern: {formName}_{attackType} (e.g., "light_slash", "UpAir", "NeutralSpecial")
        /// </summary>
        static AttackSO FindAttackSOForClip(string clipName)
        {
            if (attackCache == null || attackCache.Length == 0) return null;

            string lowerName = clipName.ToLower();

            // Direct match on attackId
            foreach (var attack in attackCache)
            {
                if (!string.IsNullOrEmpty(attack.attackId) && 
                    lowerName.Contains(attack.attackId.ToLower().Replace("_", "")))
                {
                    return attack;
                }
            }

            // Match on animationTrigger
            foreach (var attack in attackCache)
            {
                if (!string.IsNullOrEmpty(attack.animationTrigger) &&
                    lowerName.Contains(attack.animationTrigger.ToLower()))
                {
                    return attack;
                }
            }

            return null;
        }

        static bool AddCombatEventsToClip(AnimationClip clip)
        {
            if (clip == null) return false;

            var existingEvents = AnimationUtility.GetAnimationEvents(clip);
            bool alreadyHasAllEvents = existingEvents != null && existingEvents.Length > 0 &&
                existingEvents.Any(e => e.functionName == "OnAttackStartup") &&
                existingEvents.Any(e => e.functionName == "OnAttackActive") &&
                existingEvents.Any(e => e.functionName == "OnAttackEnd") &&
                existingEvents.Any(e => e.functionName == "OnHitboxEnable") &&
                existingEvents.Any(e => e.functionName == "OnHitboxDisable");

            if (alreadyHasAllEvents)
            {
                return false;
            }

            var events = new List<AnimationEvent>();

            // Keep existing events that we don't manage
            if (existingEvents != null)
            {
                foreach (var evt in existingEvents)
                {
                    if (!IsManagedEvent(evt.functionName))
                    {
                        events.Add(evt);
                    }
                }
            }

            // Add combat events based on clip name
            string clipName = clip.name.ToLower();

            bool isAttackSpecific = clipName.Contains("attack") || clipName.Contains("air") || clipName.Contains("special");
            bool isParry = clipName.Contains("parry");
            bool isLand = clipName.Contains("land") || clipName.Contains("idle") || clipName.Contains("hitstun") || clipName.Contains("knockback") || clipName.Contains("launch") || clipName.Contains("death");
            bool isDash = clipName.Contains("dash");
            bool isJump = clipName.Contains("jump");

            // Core combat events for attack clips - use AttackSO frame data for precision
            if (isAttackSpecific)
            {
                // Try to find matching AttackSO for precise frame data
                AttackSO attackSO = FindAttackSOForClip(clipName);

                float clipDuration = clip.length;

                // Calculate frame-accurate times (60fps)
                float activeFrame = 0f;
                float disableFrame = 0f;
                float endFrame = 0f;

                if (attackSO != null)
                {
                    // Use AttackSO frame data - convert frames to time at 60fps
                    activeFrame = attackSO.startupFrames / 60f;
                    disableFrame = (attackSO.startupFrames + attackSO.activeFrames) / 60f;
                    endFrame = (attackSO.startupFrames + attackSO.activeFrames + attackSO.recoveryFrames * 0.5f) / 60f;

                    // Clamp to clip length (some animations are shorter than frame data suggests)
                    activeFrame = Mathf.Min(activeFrame, clipDuration * 0.99f);
                    disableFrame = Mathf.Min(disableFrame, clipDuration * 0.99f);
                    endFrame = Mathf.Min(endFrame, clipDuration * 0.99f);
                }
                else
                {
                    // Fallback to estimation if no matching AttackSO found
                    activeFrame = clip.length * 0.25f;
                    disableFrame = activeFrame + clip.length * 0.3f;
                    endFrame = clip.length * 0.8f;
                }

                // Startup - frame 0 (first frame)
                AddEvent(events, 0f, "OnAttackStartup");

                // Active - at startup frames end
                AddEvent(events, activeFrame, "OnAttackActive");

                // Hitbox enable at active start
                AddEvent(events, activeFrame, "OnHitboxEnable");

                // Hitbox disable at active end
                AddEvent(events, disableFrame, "OnHitboxDisable");

                // End - at recovery start
                AddEvent(events, endFrame, "OnAttackEnd");

                // Swing SFX at active start
                AddEvent(events, activeFrame, "OnSwingSFX");

                // Mechanic activate at active start for specials
                if (clipName.Contains("special") || clipName.Contains("neutral") || clipName.Contains("side") || clipName.Contains("up") || clipName.Contains("down"))
                {
                    AddEvent(events, activeFrame, "OnMechanicActivate");
                }
            }

            // Landing SFX/VFX
            if (isLand && !clipName.Contains("jump") && !clipName.Contains("air"))
            {
                // Land SFX at end of landing/transition
                float landFrame = clip.length * 0.5f;
                AddEvent(events, landFrame, "OnLandSFX");
                AddEvent(events, landFrame, "OnLandVFX");
            }

            // Dash SFX/VFX
            if (isDash)
            {
                AddEvent(events, 0f, "OnDashSFX");
                AddEvent(events, 0f, "OnDashVFX");
            }

            // Jump SFX
            if (isJump)
            {
                AddEvent(events, 0f, "OnJumpSFX");
            }

            // Parry events
            if (isParry)
            {
                AddEvent(events, 0f, "OnParryStart");
                float endFrame = clip.length * 0.8f;
                AddEvent(events, endFrame, "OnParryEnd");
            }

            // Set events
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);

            return true;
        }

        static bool IsManagedEvent(string functionName)
        {
            var managedEvents = new HashSet<string>
            {
                "OnAttackStartup", "OnAttackActive", "OnAttackEnd",
                "OnHitboxEnable", "OnHitboxDisable", "OnHitboxReactivate",
                "OnSwingSFX", "OnHitSFX", "OnLandSFX", "OnDashSFX", "OnJumpSFX",
                "OnLandVFX", "OnDashVFX",
                "OnParryStart", "OnParryEnd",
                "OnMechanicActivate"
            };
            return managedEvents.Contains(functionName);
        }

        static void AddEvent(List<AnimationEvent> events, float time, string functionName, string stringParam = "", int intParam = 0, float floatParam = 0f, Object objectReference = null)
        {
            // Don't add duplicate events at same time with same function
            if (events.Any(e => Mathf.Abs(e.time - time) < 0.001f && e.functionName == functionName))
                return;

            var evt = new AnimationEvent
            {
                time = time,
                functionName = functionName,
                stringParameter = stringParam,
                intParameter = intParam,
                floatParameter = floatParam,
                objectReferenceParameter = objectReference
            };
            events.Add(evt);
        }

        [MenuItem("KyberClash/Animation/Validate All Clip Events")]
        public static void ValidateAllClipEvents()
        {
            string animPath = "Assets/_Project/Animations/Characters";
            var formFolders = System.IO.Directory.GetDirectories(animPath);

            int total = 0, withEvents = 0, missing = 0;

            foreach (var folder in formFolders)
            {
                var clips = System.IO.Directory.GetFiles(folder, "*.anim", System.IO.SearchOption.AllDirectories);

                foreach (var clipPath in clips)
                {
                    total++;
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath.Replace('\\', '/'));
                    if (clip != null)
                    {
                        var events = AnimationUtility.GetAnimationEvents(clip);
                        if (events != null && events.Length > 0)
                        {
                            withEvents++;
                            bool hasCore = events.Any(e => e.functionName == "OnAttackStartup" || e.functionName == "OnAttackActive" || e.functionName == "OnAttackEnd");
                            if (!hasCore && clip.name.ToLower().Contains("attack"))
                            {
                                missing++;
                                Debug.LogWarning($"Missing combat events: {clipPath}", clip);
                            }
                        }
                        else if (clip.name.ToLower().Contains("attack"))
                        {
                            missing++;
                            Debug.LogWarning($"No events at all: {clipPath}", clip);
                        }
                    }
                }
            }

            Debug.Log("=== Validation Complete ===");
            Debug.Log($"Total clips: {total}");
            Debug.Log($"With events: {withEvents}");
            Debug.Log($"Attack clips missing events: {missing}");
        }

        [MenuItem("KyberClash/Animation/Clear All Custom Events (Keep Built-in)")]
        public static void ClearAllCustomEvents()
        {
            string animPath = "Assets/_Project/Animations/Characters";
            var formFolders = System.IO.Directory.GetDirectories(animPath);

            int cleared = 0;

            foreach (var folder in formFolders)
            {
                var clips = System.IO.Directory.GetFiles(folder, "*.anim", System.IO.SearchOption.AllDirectories);

                foreach (var clipPath in clips)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath.Replace('\\', '/'));
                    if (clip != null)
                    {
                        var events = AnimationUtility.GetAnimationEvents(clip);
                        if (events != null && events.Length > 0)
                        {
                            var keepEvents = new List<AnimationEvent>();
                            foreach (var evt in events)
                            {
                                if (!IsManagedEvent(evt.functionName))
                                {
                                    keepEvents.Add(evt);
                                }
                            }
                            AnimationUtility.SetAnimationEvents(clip, keepEvents.ToArray());
                            EditorUtility.SetDirty(clip);
                            cleared++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"=== Cleared custom events from {cleared} clips ===");
        }
    }
}