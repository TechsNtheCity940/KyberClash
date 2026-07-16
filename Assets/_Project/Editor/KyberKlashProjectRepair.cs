using System.IO;
using KyberKlash.Core;
using KyberKlash.Data;
using KyberKlash.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KyberKlash.EditorTools
{
    public static class KyberKlashProjectRepair
    {
        private const string AutoRunFlagPath = "Assets/_Project/Generated/LudoAI/.repair_project_once";
        private const string PrototypeScenePath = "Assets/_Project/Scenes/PrototypeArena.unity";

        [MenuItem("Tools/Kyber Clash/Repair/Repair Runtime Scenes And Visuals")]
        public static void RepairRuntimeScenesAndVisuals()
        {
            RepairGeneratedMaterials();
            LudoBattleArenaBatchGenerator.RebuildArenaPrefabsOnly();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            StageData[] generatedStages = LoadGeneratedStages();
            Debug.Log($"[KyberKlash][Repair] Loaded {generatedStages.Length} generated stage assets for scene repair.");
            OpenPrototypeArenaScene();
            AssignGeneratedStagesToOpenGameManagers(generatedStages);
            AssignGeneratedStagesToOpenStageManagers(generatedStages);
            RepairOpenSceneRenderers();

            Scene prototypeScene = SceneManager.GetSceneByPath(PrototypeScenePath);
            if (prototypeScene.IsValid())
            {
                EditorSceneManager.SaveScene(prototypeScene);
            }
            else
            {
                EditorSceneManager.SaveOpenScenes();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KyberKlash][Repair] Runtime scene, arena, and visual repair complete.");
        }

        [InitializeOnLoadMethod]
        private static void RunQueuedRepairAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoRunFlagPath))
                {
                    return;
                }

                File.Delete(AutoRunFlagPath);
                RepairRuntimeScenesAndVisuals();
            };
        }

        private static void RepairGeneratedMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[]
            {
                "Assets/_Project/Materials/Stages",
                "Assets/_Project/Art/Stages"
            });

            for (int i = 0; i < materialGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                bool isBackground = path.Contains("/Art/Stages/") && path.EndsWith("_background.mat");
                Shader shader = isBackground
                    ? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard")
                    : Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");

                if (shader != null && NeedsReplacement(material))
                {
                    material.shader = shader;
                }

                if (isBackground)
                {
                    Texture texture = FindSiblingBackgroundTexture(path);
                    if (texture != null)
                    {
                        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                    }

                    if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                }
                else
                {
                    Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : new Color(0.24f, 0.30f, 0.40f, 1f);
                    color.a = 1f;
                    if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                }

                EditorUtility.SetDirty(material);
            }
        }

        private static void OpenPrototypeArenaScene()
        {
            if (File.Exists(PrototypeScenePath))
            {
                EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            }
        }

        private static void AssignGeneratedStagesToOpenGameManagers(StageData[] stages)
        {
            if (stages.Length == 0)
            {
                return;
            }

            GameManager[] managers = Resources.FindObjectsOfTypeAll<GameManager>();
            for (int i = 0; i < managers.Length; i++)
            {
                GameManager manager = managers[i];
                if (manager == null || EditorUtility.IsPersistent(manager))
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(manager);
                serializedObject.Update();
                SerializedProperty testStage = serializedObject.FindProperty("testStage");
                SerializedProperty availableStages = serializedObject.FindProperty("availableStages");
                SerializedProperty randomizeStage = serializedObject.FindProperty("randomizeStage");

                if (testStage != null) testStage.objectReferenceValue = stages[0];
                if (availableStages != null)
                {
                    availableStages.arraySize = stages.Length;
                    for (int j = 0; j < stages.Length; j++)
                    {
                        availableStages.GetArrayElementAtIndex(j).objectReferenceValue = stages[j];
                    }
                }
                if (randomizeStage != null) randomizeStage.boolValue = true;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(manager.gameObject);
                if (manager.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                }
            }
        }

        private static void AssignGeneratedStagesToOpenStageManagers(StageData[] stages)
        {
            if (stages.Length == 0)
            {
                return;
            }

            StageManager[] managers = Resources.FindObjectsOfTypeAll<StageManager>();
            for (int i = 0; i < managers.Length; i++)
            {
                StageManager manager = managers[i];
                if (manager == null || EditorUtility.IsPersistent(manager))
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(manager);
                serializedObject.Update();
                SerializedProperty currentStageData = serializedObject.FindProperty("currentStageData");
                SerializedProperty stageInstance = serializedObject.FindProperty("stageInstance");

                if (currentStageData != null) currentStageData.objectReferenceValue = stages[0];
                if (stageInstance != null) stageInstance.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();

                for (int childIndex = 0; childIndex < manager.transform.childCount; childIndex++)
                {
                    manager.transform.GetChild(childIndex).gameObject.SetActive(false);
                }

                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(manager.gameObject);
                if (manager.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                }
            }
        }

        private static StageData[] LoadGeneratedStages()
        {
            string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/_Project/Data/Stages" });
            var stages = new System.Collections.Generic.List<StageData>();
            System.Array.Sort(guids, (left, right) =>
                string.Compare(AssetDatabase.GUIDToAssetPath(left), AssetDatabase.GUIDToAssetPath(right), System.StringComparison.Ordinal));

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!Path.GetFileNameWithoutExtension(path).StartsWith("Stage_"))
                {
                    continue;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage != null && stage.stagePrefab != null && path.Contains("Stage_PrototypeArena") == false)
                {
                    stages.Add(stage);
                    Debug.Log($"[KyberKlash][Repair] Stage ref ready: {path} -> {stage.stagePrefab.name}");
                }
            }

            return stages.ToArray();
        }

        private static void RepairOpenSceneRenderers()
        {
            MeshRenderer[] renderers = Resources.FindObjectsOfTypeAll<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null || EditorUtility.IsPersistent(renderer))
                {
                    continue;
                }

                Material material = renderer.sharedMaterial;
                if (!NeedsReplacement(material))
                {
                    continue;
                }

                bool isBackground = renderer.name.Contains("Background");
                Shader shader = isBackground
                    ? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard")
                    : Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");

                Material replacement = new Material(shader != null ? shader : Shader.Find("Standard"));
                if (material != null)
                {
                    Texture texture = null;
                    if (material.HasProperty("_MainTex")) texture = material.GetTexture("_MainTex");
                    if (texture == null && material.HasProperty("_BaseMap")) texture = material.GetTexture("_BaseMap");
                    if (texture != null && replacement.HasProperty("_MainTex")) replacement.SetTexture("_MainTex", texture);
                }

                if (replacement.HasProperty("_Color"))
                {
                    replacement.SetColor("_Color", isBackground ? Color.white : new Color(0.24f, 0.30f, 0.40f, 1f));
                }

                renderer.sharedMaterial = replacement;
                if (renderer.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
                }
            }
        }

        private static bool NeedsReplacement(Material material)
        {
            if (material == null || material.shader == null)
            {
                return true;
            }

            string shaderName = material.shader.name;
            return shaderName == "Hidden/InternalErrorShader" ||
                   shaderName.StartsWith("Universal Render Pipeline/", System.StringComparison.Ordinal);
        }

        private static Texture FindSiblingBackgroundTexture(string materialPath)
        {
            string directory = Path.GetDirectoryName(materialPath)?.Replace('\\', '/');
            string baseName = Path.GetFileNameWithoutExtension(materialPath).Replace("_background", string.Empty);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseName))
            {
                return null;
            }

            string[] candidates =
            {
                $"{directory}/{baseName}_background.png",
                $"{directory}/{baseName}_background.webp",
                $"{directory}/{baseName}_background.jpg",
                $"{directory}/{baseName}_background.jpeg"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(candidates[i]);
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }
    }
}
