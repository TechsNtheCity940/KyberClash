using UnityEngine;
using UnityEditor;
using KyberKlash.Data;

namespace KyberKlash.Editor
{
    /// <summary>
    /// Editor script to assign VFX/SFX prefabs to AttackSOs
    /// </summary>
    public class AssignVFXToAttackSOsEditor : EditorWindow
    {
        [MenuItem("KyberClash/VFX/Assign VFX to AttackSOs")]
        public static void AssignVFXToAttackSOs()
        {
            Debug.Log("Assigning VFX prefabs to AttackSOs...");
            
            // Load VFX prefabs
            var hitLight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/HitImpact_Light.prefab");
            var hitMedium = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/HitImpact_Medium.prefab");
            var hitHeavy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/HitImpact_Heavy.prefab");
            var hitClash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/HitImpact_Clash.prefab");
            var hitParry = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/HitImpact_Parry.prefab");
            
            var trailLight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/Trail_Light.prefab");
            var trailHeavy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/Trail_Heavy.prefab");
            var trailSpecial = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/Trail_Special.prefab");
            var trailSaber = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/Trail_Saber.prefab");
            
            // Verify prefabs exist
            if (hitLight == null || hitMedium == null || hitHeavy == null || hitClash == null || hitParry == null)
            {
                Debug.LogError("Some hit VFX prefabs not found!");
                return;
            }
            
            if (trailLight == null || trailHeavy == null || trailSpecial == null || trailSaber == null)
            {
                Debug.LogError("Some trail VFX prefabs not found!");
                return;
            }
            
            // Get all AttackSOs
            var attackGuids = AssetDatabase.FindAssets("t:AttackSO", new[] { "Assets/_Project/Data/Attacks" });
            
            foreach (var guid in attackGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<AttackSO>(path);
                
                if (attack == null) continue;
                
                var serialized = new SerializedObject(attack);
                
                // Assign hit VFX based on attack type/damage
                if (attack.attackId.Contains("Light") || attack.baseDamage <= 10)
                {
                    serialized.FindProperty("hitVFXPrefab").objectReferenceValue = hitLight;
                }
                else if (attack.attackId.Contains("Heavy") || attack.baseDamage > 15)
                {
                    serialized.FindProperty("hitVFXPrefab").objectReferenceValue = hitHeavy;
                }
                else
                {
                    serialized.FindProperty("hitVFXPrefab").objectReferenceValue = hitMedium;
                }
                
                // Assign clash VFX
                serialized.FindProperty("clashVFXPrefab").objectReferenceValue = hitClash;
                
                // Assign trail based on attack type
                if (attack.attackId.Contains("Special"))
                {
                    serialized.FindProperty("trailEffectPrefab").objectReferenceValue = trailSpecial;
                }
                else if (attack.attackId.Contains("Heavy"))
                {
                    serialized.FindProperty("trailEffectPrefab").objectReferenceValue = trailHeavy;
                }
                else if (attack.attackId.Contains("Light") || attack.attackId.Contains("Air"))
                {
                    serialized.FindProperty("trailEffectPrefab").objectReferenceValue = trailLight;
                }
                else
                {
                    serialized.FindProperty("trailEffectPrefab").objectReferenceValue = trailSaber;
                }
                
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(attack);
                Debug.Log($"Assigned VFX to {attack.attackId}");
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log("VFX assignment complete!");
        }
    }
}