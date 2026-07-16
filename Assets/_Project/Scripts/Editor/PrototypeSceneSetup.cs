using UnityEngine;
using UnityEngine.SceneManagement;
using KyberKlash.Combat;
using KyberKlash.Core;
using KyberKlash.Data;
using KyberKlash.Player;
using KyberKlash.Stage;
using KyberKlash.UI;
using KyberKlash.VFX;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.UI;

namespace KyberKlash.Core
{
    /// <summary>
    /// Editor script to set up the prototype scene programmatically
    /// Run via: Kyber Clash > Setup Prototype Scene
    /// </summary>
    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditorInternal;
    
    public class PrototypeSceneSetup : EditorWindow
    {
        private const string DataPath = "Assets/_Project/Data";
        private const string PrefabPath = "Assets/_Project/Prefabs";
        private const string ScenePath = "Assets/_Project/Scenes";

        [MenuItem("Kyber Clash/Setup Prototype Scene")]
        public static void SetupPrototypeScene()
        {
            // Create scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PrototypeArena";
            
            // Save scene
            EnsureFolder(ScenePath);
            string scenePath = ScenePath + "/PrototypeArena.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Create reusable player prefab first so managers can reference it
            var playerPrefab = CreatePlayerPrefab();
            
            // Create stage
            CreateStage();
            
            // Create camera
            CreateCamera();
            
            // Create game manager
            CreateGameManager(playerPrefab);
            
            // Create VFX pool
            CreateVFXPool();
            
            // Create HUD
            CreateHUD();
            
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[PrototypeSceneSetup] Prototype arena scene created!");
        }
        
        private static void CreateStage()
        {
            var stageObj = new GameObject("Stage");
            SetTagIfExists(stageObj, "Stage");
            
            // Ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(stageObj.transform);
            ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50 units
            SetLayerIfExists(ground, "Ground");
            
            // Platforms
            CreatePlatform(stageObj.transform, "Platform_Left", new Vector3(-8f, 3f, 0f), new Vector3(6f, 0.5f, 4f));
            CreatePlatform(stageObj.transform, "Platform_Right", new Vector3(8f, 3f, 0f), new Vector3(6f, 0.5f, 4f));
            CreatePlatform(stageObj.transform, "Platform_Top", new Vector3(0f, 6f, 0f), new Vector3(4f, 0.5f, 4f));
            
            // Walls (for wall jump)
            CreateWall(stageObj.transform, "Wall_Left", new Vector3(-15f, 5f, 0f), new Vector3(1f, 10f, 20f));
            CreateWall(stageObj.transform, "Wall_Right", new Vector3(15f, 5f, 0f), new Vector3(1f, 10f, 20f));
            
            // Spawn points
            CreateSpawnPoint(stageObj.transform, "Spawn_1", new Vector3(-5f, 2f, 0f), 0);
            CreateSpawnPoint(stageObj.transform, "Spawn_2", new Vector3(5f, 2f, 0f), 1);
            CreateSpawnPoint(stageObj.transform, "Spawn_3", new Vector3(-5f, 2f, 5f), 2);
            CreateSpawnPoint(stageObj.transform, "Spawn_4", new Vector3(5f, 2f, 5f), 3);
            
            // Hazard zone example
            CreateHazardZone(stageObj.transform, "Hazard_Lava", new Vector3(0f, 0.1f, 0f), new Vector3(20f, 0.5f, 20f));
            
            // Add StageManager
            var stageManager = stageObj.AddComponent<StageManager>();
            var stageData = AssetDatabase.LoadAssetAtPath<StageData>(DataPath + "/Stages/Stage_PrototypeArena.asset");
            SetObjectReference(stageManager, "currentStageData", stageData);
            
            // Save as prefab
            EnsureFolder(PrefabPath);
            string prefabPath = PrefabPath + "/Stage_PrototypeArena.prefab";
            var stagePrefab = PrefabUtility.SaveAsPrefabAsset(stageObj, prefabPath);
            SetObjectReference(stageData, "stagePrefab", stagePrefab);
            EditorUtility.SetDirty(stageData);
        }
        
        private static void CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent);
            platform.transform.localPosition = position;
            platform.transform.localScale = scale;
            SetLayerIfExists(platform, "Ground");
            
            // Add visual material
            var renderer = platform.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.3f, 0.35f);
            renderer.material = mat;
        }
        
        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            SetLayerIfExists(wall, "Wall");
            
            var renderer = wall.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.2f, 0.25f);
            renderer.material = mat;
        }
        
        private static void CreateSpawnPoint(Transform parent, string name, Vector3 position, int index)
        {
            var spawn = new GameObject(name);
            spawn.transform.SetParent(parent);
            spawn.transform.localPosition = position;
            var spawnPoint = spawn.AddComponent<SpawnPoint>();
            spawnPoint.playerIndex = index;
        }
        
        private static void CreateHazardZone(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var hazard = new GameObject(name);
            hazard.transform.SetParent(parent);
            hazard.transform.localPosition = position;
            hazard.transform.localScale = scale;
            SetLayerIfExists(hazard, "Hazard");
            
            var zone = hazard.AddComponent<HazardZone>();
            var collider = hazard.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one;
        }
        
        private static void CreateCamera()
        {
            var camObj = new GameObject("MainCamera");
            SetTagIfExists(camObj, "MainCamera");
            
            var camera = camObj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            
            var vcam = camObj.AddComponent<CinemachineVirtualCamera>();
            vcam.Priority = 10;
            
            var targetGroup = camObj.AddComponent<CinemachineTargetGroup>();
            
            var impulseSource = camObj.AddComponent<CinemachineImpulseSource>();
            
            var camManager = camObj.AddComponent<CameraManager>();
            SetObjectReference(camManager, "virtualCamera", vcam);
            SetObjectReference(camManager, "targetGroup", targetGroup);
            SetObjectReference(camManager, "impulseSource", impulseSource);
            
            EnsureFolder(PrefabPath);
            string prefabPath = PrefabPath + "/MainCamera.prefab";
            PrefabUtility.SaveAsPrefabAsset(camObj, prefabPath);
        }
        
        private static void CreateGameManager(GameObject playerPrefab)
        {
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            var characters = new CharacterData[]
            {
                AssetDatabase.LoadAssetAtPath<CharacterData>(DataPath + "/Characters/Character_JollyKnight.asset"),
                AssetDatabase.LoadAssetAtPath<CharacterData>(DataPath + "/Characters/Character_SithfullyYours.asset")
            };
            var stageData = AssetDatabase.LoadAssetAtPath<StageData>(DataPath + "/Stages/Stage_PrototypeArena.asset");

            SetObjectReference(gm, "playerPrefab", playerPrefab);
            SetObjectArray(gm, "availableCharacters", characters);
            SetObjectReference(gm, "testStage", stageData);
            
            EnsureFolder(PrefabPath);
            string prefabPath = PrefabPath + "/GameManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(gmObj, prefabPath);
        }
        
        private static GameObject CreatePlayerPrefab()
        {
            var playerObj = new GameObject("Player");
            SetTagIfExists(playerObj, "Player");
            SetLayerIfExists(playerObj, "Player");

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(playerObj.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            DestroyImmediate(body.GetComponent<Collider>());
            
            // Capsule collider for character
            var collider = playerObj.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.4f;
            collider.center = new Vector3(0f, 1f, 0f);
            
            // Rigidbody
            var rb = playerObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Animator
            var animator = playerObj.AddComponent<Animator>();
            
            // State Machine
            var stateMachine = playerObj.AddComponent<StateMachine>();
            
            // Input Handler
            var inputHandler = playerObj.AddComponent<PlayerInputHandler>();
            
            // Combat
            var combat = playerObj.AddComponent<PlayerCombat>();
            
            // Meter
            var meter = playerObj.AddComponent<PlayerMeter>();
            
            // Controller
            var controller = playerObj.AddComponent<PlayerController>();
            var defaultCharacter = AssetDatabase.LoadAssetAtPath<CharacterData>(DataPath + "/Characters/Character_JollyKnight.asset");
            SetObjectReference(controller, "characterData", defaultCharacter);
            SetObjectReference(controller, "startingForm", defaultCharacter != null ? defaultCharacter.defaultForm : null);
            
            // Lightsaber VFX
            var saberObj = new GameObject("Saber");
            saberObj.transform.SetParent(playerObj.transform);
            saberObj.transform.localPosition = new Vector3(0.5f, 1.5f, 0.5f);
            var saberVFX = saberObj.AddComponent<LightsaberVFX>();
            
            // Saber tip/base for hitbox
            var tipObj = new GameObject("SaberTip");
            tipObj.transform.SetParent(saberObj.transform);
            tipObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            
            var baseObj = new GameObject("SaberBase");
            baseObj.transform.SetParent(saberObj.transform);
            baseObj.transform.localPosition = Vector3.zero;
            
            SetObjectReference(controller, "saberTip", tipObj.transform);
            SetObjectReference(controller, "saberBase", baseObj.transform);
            SetObjectReference(combat, "saberTip", tipObj.transform);
            SetObjectReference(combat, "saberBase", baseObj.transform);
            
            EnsureFolder(PrefabPath);
            string prefabPath = PrefabPath + "/Player.prefab";
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerObj, prefabPath);
            DestroyImmediate(playerObj);
            return playerPrefab;
        }
        
        private static void CreateVFXPool()
        {
            var poolObj = new GameObject("VFXPool");
            var pool = poolObj.AddComponent<VFXPool>();
            
            EnsureFolder(PrefabPath);
            string prefabPath = PrefabPath + "/VFXPool.prefab";
            PrefabUtility.SaveAsPrefabAsset(poolObj, prefabPath);
        }
        
        private static void CreateHUD()
        {
            var canvasObj = new GameObject("HUDCanvas");
            SetLayerIfExists(canvasObj, "UI");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            var hudManager = canvasObj.AddComponent<HUDManager>();
            
            // Create HUD prefab
            var hudPrefab = CreateHUDPrefab();
            SetObjectReference(hudManager, "playerHUDPrefab", hudPrefab);
            SetObjectReference(hudManager, "hudContainer", canvasObj.transform);
            
            EnsureFolder(PrefabPath);
            string prefabPath = PrefabPath + "/HUDCanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
        }
        
        private static GameObject CreateHUDPrefab()
        {
            var hudObj = new GameObject("PlayerHUD");
            hudObj.AddComponent<RectTransform>();
            var hud = hudObj.AddComponent<PlayerHUD>();
            
            // Create UI elements
            var bg = new GameObject("Background");
            bg.transform.SetParent(hudObj.transform);
            var bgImage = bg.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(200f, 100f);
            
            var nameText = new GameObject("NameText");
            nameText.transform.SetParent(hudObj.transform);
            var nameTMP = nameText.AddComponent<TextMeshProUGUI>();
            nameTMP.fontSize = 18;
            nameTMP.color = Color.white;
            nameTMP.alignment = TextAlignmentOptions.Center;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0f, 35f);
            nameRect.sizeDelta = new Vector2(180f, 30f);
            
            var damageText = new GameObject("DamageText");
            damageText.transform.SetParent(hudObj.transform);
            var damageTMP = damageText.AddComponent<TextMeshProUGUI>();
            damageTMP.fontSize = 36;
            damageTMP.color = Color.white;
            damageTMP.alignment = TextAlignmentOptions.Center;
            var damageRect = damageText.GetComponent<RectTransform>();
            damageRect.anchoredPosition = new Vector2(-60f, -10f);
            damageRect.sizeDelta = new Vector2(100f, 50f);
            
            var meterSlider = new GameObject("MeterSlider");
            meterSlider.transform.SetParent(hudObj.transform);
            var slider = meterSlider.AddComponent<UnityEngine.UI.Slider>();
            var sliderRect = meterSlider.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(40f, -10f);
            sliderRect.sizeDelta = new Vector2(100f, 20f);
            
            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(meterSlider.transform);
            var fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            var fillImage = fill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = Color.blue;
            var fillImgRect = fill.GetComponent<RectTransform>();
            fillImgRect.anchorMin = Vector2.zero;
            fillImgRect.anchorMax = Vector2.one;
            fillImgRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillImgRect;
            slider.targetGraphic = fillImage;
            
            EnsureFolder(PrefabPath);
            string prefabPath = PrefabPath + "/PlayerHUD.prefab";
            var hudPrefab = PrefabUtility.SaveAsPrefabAsset(hudObj, prefabPath);
            DestroyImmediate(hudObj);
            
            return hudPrefab;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void SetLayerIfExists(GameObject target, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                target.layer = layer;
        }

        private static void SetTagIfExists(GameObject target, string tagName)
        {
            foreach (string tag in InternalEditorUtility.tags)
            {
                if (tag == tagName)
                {
                    target.tag = tagName;
                    return;
                }
            }

            target.tag = "Untagged";
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            if (target == null)
                return;

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray<T>(Object target, string propertyName, T[] values) where T : Object
        {
            if (target == null)
                return;

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
                return;

            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
    #endif
}
