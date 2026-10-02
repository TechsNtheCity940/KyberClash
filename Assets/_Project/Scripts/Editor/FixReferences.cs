using UnityEngine;
using UnityEditor;
using System.IO;
using KyberKlash.Data;

namespace KyberKlash.Editor
{
    public class FixReferences : EditorWindow
    {
        [MenuItem("KyberClash/Fix All References")]
        public static void FixAllReferences()
        {
            Debug.Log("=== Starting KyberClash Reference Fixes ===");
            
            // 1. Create required layers
            CreateRequiredLayers();
            
            // 2. Fix Player prefab references
            FixPlayerPrefab();
            
            // 3. Fix CharacterData references
            FixCharacterData();
            
            // 4. Fix FormSO references
            FixFormSO();
            
            // 5. Fix Scene setup
            FixSceneSetup();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("=== All fixes complete ===");
        }
        
        static void CreateRequiredLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            
            string[] requiredLayers = { "Ground", "Wall", "Hurtbox", "Hitbox", "Clash", "BlastZone" };
            int startLayer = 8;
            
            for (int i = 0; i < requiredLayers.Length; i++)
            {
                var layerProp = layers.GetArrayElementAtIndex(startLayer + i);
                if (layerProp.stringValue != requiredLayers[i])
                {
                    layerProp.stringValue = requiredLayers[i];
                    Debug.Log($"Set layer {startLayer + i} = {requiredLayers[i]}");
                }
            }
            tagManager.ApplyModifiedProperties();
        }
        
        static void FixPlayerPrefab()
        {
            string prefabPath = "Assets/_Project/Prefabs/Player.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Player prefab not found at {prefabPath}");
                return;
            }
            
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            
            // PlayerController
            var controller = prefabRoot.GetComponent<KyberKlash.Player.PlayerController>();
            if (controller != null)
            {
                var serialized = new SerializedObject(controller);
                
                serialized.FindProperty("characterData").objectReferenceValue = 
                    AssetDatabase.LoadAssetAtPath<KyberKlash.Data.CharacterData>("Assets/_Project/Data/Characters/Character_JollyKnight.asset");
                serialized.FindProperty("startingForm").objectReferenceValue = 
                    AssetDatabase.LoadAssetAtPath<KyberKlash.Data.FormSO>("Assets/_Project/Data/Forms/Form_ShiiCho.asset");
                serialized.FindProperty("groundLayer").intValue = LayerMask.NameToLayer("Ground");
                serialized.FindProperty("wallLayer").intValue = LayerMask.NameToLayer("Wall");
                serialized.FindProperty("hurtboxLayer").intValue = LayerMask.NameToLayer("Hurtbox");
                
                // Find saberTip and saberBase
                var saberTip = prefabRoot.transform.Find("Saber/SaberTip");
                var saberBase = prefabRoot.transform.Find("Saber/SaberBase");
                if (saberTip != null) serialized.FindProperty("saberTip").objectReferenceValue = saberTip;
                if (saberBase != null) serialized.FindProperty("saberBase").objectReferenceValue = saberBase;
                
                serialized.ApplyModifiedProperties();
            }
            
            // PlayerCombat
            var combat = prefabRoot.GetComponent<KyberKlash.Player.PlayerCombat>();
            if (combat != null)
            {
                var serialized = new SerializedObject(combat);
                serialized.FindProperty("hurtboxLayer").intValue = LayerMask.NameToLayer("Hurtbox");
                serialized.FindProperty("clashLayer").intValue = LayerMask.NameToLayer("Clash");
                
                var saberTip = prefabRoot.transform.Find("Saber/SaberTip");
                var saberBase = prefabRoot.transform.Find("Saber/SaberBase");
                if (saberTip != null) serialized.FindProperty("saberTip").objectReferenceValue = saberTip;
                if (saberBase != null) serialized.FindProperty("saberBase").objectReferenceValue = saberBase;
                
                serialized.ApplyModifiedProperties();
            }
            
            // FormMechanicHandler
            var mechanicHandler = prefabRoot.GetComponent<KyberKlash.Player.FormMechanicHandler>();
            // No specific property setup needed for now
            
            // CombatAnimationEvents - add if missing
            var animEvents = prefabRoot.GetComponent<KyberKlash.Combat.CombatAnimationEvents>();
            if (animEvents == null)
            {
                animEvents = prefabRoot.AddComponent<KyberKlash.Combat.CombatAnimationEvents>();
                Debug.Log("Added CombatAnimationEvents component to Player prefab");
            }
            
            // LightsaberVFX
            var saberObj = prefabRoot.transform.Find("Saber");
            if (saberObj != null)
            {
                var vfx = saberObj.GetComponent<KyberKlash.VFX.LightsaberVFX>();
                if (vfx != null)
                {
                    var serialized = new SerializedObject(vfx);
                    var saberTip = saberObj.Find("SaberTip");
                    var saberBase = saberObj.Find("SaberBase");
                    if (saberTip != null) serialized.FindProperty("saberTip").objectReferenceValue = saberTip.transform;
                    if (saberBase != null) serialized.FindProperty("saberBase").objectReferenceValue = saberBase.transform;
                    serialized.FindProperty("saberLength").floatValue = 1.5f;
                    serialized.FindProperty("segments").intValue = 20;
                    serialized.ApplyModifiedProperties();
                }
            }
            
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("Player prefab references fixed");
        }
        
        static void FixCharacterData()
        {
            var character = AssetDatabase.LoadAssetAtPath<KyberKlash.Data.CharacterData>("Assets/_Project/Data/Characters/Character_JollyKnight.asset");
            if (character == null) return;
            
            var serialized = new SerializedObject(character);
            
            serialized.FindProperty("defaultLightAttack").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_Light.asset");
            serialized.FindProperty("defaultHeavyAttack").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_Heavy.asset");
            serialized.FindProperty("defaultUpAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_UpAir.asset");
            serialized.FindProperty("defaultDownAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_DownAir.asset");
            serialized.FindProperty("defaultForwardAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_ForwardAir.asset");
            serialized.FindProperty("defaultBackAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_BackAir.asset");
            serialized.FindProperty("defaultNeutralSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_NeutralSpecial.asset");
            serialized.FindProperty("defaultSideSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_SideSpecial.asset");
            serialized.FindProperty("defaultUpSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_UpSpecial.asset");
            serialized.FindProperty("defaultDownSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_DownSpecial.asset");
            
            // Set animator controller from one of the forms
            var formController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/Characters/acrobaticflow_form_ataru/acrobaticflow_form_ataru.controller");
            if (formController != null)
            {
                serialized.FindProperty("animatorController").objectReferenceValue = formController;
            }
            
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(character);
            Debug.Log("CharacterData references fixed");
        }
        
        static void FixFormSO()
        {
            var form = AssetDatabase.LoadAssetAtPath<KyberKlash.Data.FormSO>("Assets/_Project/Data/Forms/Form_ShiiCho.asset");
            if (form == null) return;
            
            var serialized = new SerializedObject(form);
            
            serialized.FindProperty("lightAttack").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_Light.asset");
            serialized.FindProperty("heavyAttack").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_Heavy.asset");
            serialized.FindProperty("upAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_UpAir.asset");
            serialized.FindProperty("downAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_DownAir.asset");
            serialized.FindProperty("forwardAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_ForwardAir.asset");
            serialized.FindProperty("backAerial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_BackAir.asset");
            serialized.FindProperty("neutralSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_NeutralSpecial.asset");
            serialized.FindProperty("sideSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_SideSpecial.asset");
            serialized.FindProperty("upSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_UpSpecial.asset");
            serialized.FindProperty("downSpecial").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<KyberKlash.Data.AttackSO>("Assets/_Project/Data/Attacks/Attack_DownSpecial.asset");
            
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(form);
            Debug.Log("FormSO references fixed");
        }
        
        static void FixSceneSetup()
        {
            // Fix GameManager in currently loaded scene
            var gameManager = Object.FindFirstObjectByType<KyberKlash.Core.GameManager>();
            if (gameManager != null)
            {
                var serialized = new UnityEditor.SerializedObject(gameManager);
                
                // Assign available characters if empty
                var charsProp = serialized.FindProperty("availableCharacters");
                if (charsProp != null && (charsProp.arraySize == 0 || charsProp.GetArrayElementAtIndex(0).objectReferenceValue == null))
                {
                    var characters = new CharacterData[]
                    {
                        AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/Character_JollyKnight.asset"),
                        AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/Character_SithfullyYours.asset")
                    };
                    charsProp.arraySize = characters.Length;
                    for (int i = 0; i < characters.Length; i++)
                    {
                        charsProp.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
                    }
                }
                
                serialized.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(gameManager);
                Debug.Log("Scene GameManager references fixed");
            }
            
            // Fix player instances in scene - add CombatAnimationEvents if missing
            var players = Object.FindObjectsByType<KyberKlash.Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.GetComponent<KyberKlash.Combat.CombatAnimationEvents>() == null)
                {
                    player.gameObject.AddComponent<KyberKlash.Combat.CombatAnimationEvents>();
                    UnityEditor.EditorUtility.SetDirty(player.gameObject);
                    Debug.Log($"Added CombatAnimationEvents to {player.gameObject.name}");
                }
            }
        }
    }
}