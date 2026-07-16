using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KyberKlash.Core;
using KyberKlash.Data;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace KyberKlash.EditorTools
{
    public sealed class LudoBattleArenaBatchGenerator : EditorWindow
    {
        private const string ApiKeyPrefsKey = "LudoAI_API_Key";
        private const string ApiBaseUrl = "https://api.ludo.ai/api";
        private const string PromptFilePath = "AssetPrompts/BattleArenaPrompts.md";
        private const string GeneratedRoot = "Assets/_Project/Generated/LudoAI/Arenas";
        private const string ArtRoot = "Assets/_Project/Art/Stages";
        private const string PrefabRoot = "Assets/_Project/Prefabs/Stages";
        private const string StageDataRoot = "Assets/_Project/Data/Stages";
        private const string AutoRunFlagPath = "Assets/_Project/Generated/LudoAI/Arenas/.run_arenas_once";
        private const int LudoRequestTimeoutSeconds = 1200;
        private const int DownloadTimeoutSeconds = 600;

        private readonly List<ArenaPrompt> prompts = new List<ArenaPrompt>();
        private Vector2 scrollPosition;
        private bool skipExisting = true;
        private string status = "Load arena prompts to begin.";
        private EditorCoroutine activeCoroutine;

        [MenuItem("Tools/Kyber Clash/Ludo AI/Batch Generate Battle Arenas")]
        public static void ShowWindow()
        {
            GetWindow<LudoBattleArenaBatchGenerator>("Ludo Arenas");
        }

        [MenuItem("Tools/Kyber Clash/Ludo AI/Generate 7 Battle Arenas")]
        public static void GenerateAllArenas()
        {
            var window = GetWindow<LudoBattleArenaBatchGenerator>("Ludo Arenas");
            window.LoadPrompts();
            window.skipExisting = true;
            window.StartGeneration();
        }

        [MenuItem("Tools/Kyber Clash/Stages/Rebuild Generated Arena Prefabs")]
        public static void RebuildArenaPrefabsOnly()
        {
            var window = GetWindow<LudoBattleArenaBatchGenerator>("Ludo Arenas");
            window.LoadPrompts();
            window.EnsureArenaStageAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KyberKlash][LudoArenas] Rebuilt generated arena StageData and prefabs.");
        }

        [InitializeOnLoadMethod]
        private static void RunQueuedArenaBatchAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoRunFlagPath))
                {
                    return;
                }

                string apiKey = EditorPrefs.GetString(ApiKeyPrefsKey, string.Empty);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    EditorUtility.DisplayDialog("Ludo AI API Key Missing", "The queued arena batch cannot run because the Ludo AI API key is not saved. Open Ludo AI > Ludo AI Plugin, save your API key, then run Tools > Kyber Clash > Ludo AI > Generate 7 Battle Arenas.", "OK");
                    return;
                }

                File.Delete(AutoRunFlagPath);
                GenerateAllArenas();
            };
        }

        private void OnEnable()
        {
            LoadPrompts();
        }

        private void OnDisable()
        {
            if (activeCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Ludo AI Floating Battle Arenas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generates seven Smash-style arena images via Ludo AI, then builds floating multi-level StageData/prefabs with ring-out blast zones.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload Prompts")) LoadPrompts();

                GUI.enabled = activeCoroutine == null;
                if (GUILayout.Button("Generate Missing Arena Art")) StartGeneration();
                if (GUILayout.Button("Rebuild Stage Prefabs")) EnsureArenaStageAssets();
                GUI.enabled = true;
            }

            skipExisting = EditorGUILayout.ToggleLeft("Skip arenas with existing background files", skipExisting);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < prompts.Count; i++)
            {
                ArenaPrompt prompt = prompts[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GUILayout.Label($"{i + 1}. {prompt.Title}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Slug", prompt.Slug);
                    EditorGUILayout.LabelField("Output", prompt.GeneratedDirectory);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void LoadPrompts()
        {
            prompts.Clear();
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), PromptFilePath);
            if (!File.Exists(fullPath))
            {
                status = $"Arena prompt file not found: {PromptFilePath}";
                return;
            }

            string markdown = File.ReadAllText(fullPath, Encoding.UTF8);
            var matches = Regex.Matches(markdown, @"^##\s+(?<number>\d+)\.\s+(?<title>.+?)\r?\n>\s+(?<prompt>.+?)(?=\r?\n\r?\n##|\z)", RegexOptions.Multiline | RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                string title = match.Groups["title"].Value.Trim();
                string slug = ToSlug(title);
                prompts.Add(new ArenaPrompt
                {
                    Number = int.Parse(match.Groups["number"].Value),
                    Title = title,
                    Prompt = Regex.Replace(match.Groups["prompt"].Value.Trim(), @"\s+", " "),
                    Slug = slug,
                    GeneratedDirectory = $"{GeneratedRoot}/{slug}",
                    ArtDirectory = $"{ArtRoot}/{slug}"
                });
            }

            status = $"Loaded {prompts.Count} arena prompts.";
        }

        private void StartGeneration()
        {
            if (activeCoroutine != null)
            {
                status = "Arena generation is already running.";
                return;
            }

            string apiKey = EditorPrefs.GetString(ApiKeyPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                EditorUtility.DisplayDialog("Ludo AI API Key Missing", "Open Ludo AI > Ludo AI Plugin, enter your API key in Settings, then run this batch again.", "OK");
                status = "Missing Ludo AI API key.";
                return;
            }

            if (prompts.Count == 0) LoadPrompts();
            activeCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(GenerateRoutine(apiKey));
        }

        private IEnumerator GenerateRoutine(string apiKey)
        {
            EnsureFolders();
            EnsureArenaStageAssets();

            for (int i = 0; i < prompts.Count; i++)
            {
                ArenaPrompt prompt = prompts[i];
                Directory.CreateDirectory(prompt.GeneratedDirectory);
                Directory.CreateDirectory(prompt.ArtDirectory);

                string backgroundPath = FindExistingFile(prompt.ArtDirectory, $"{prompt.Slug}_background");
                if (skipExisting && !string.IsNullOrEmpty(backgroundPath))
                {
                    status = $"Skipping existing arena art {i + 1}/{prompts.Count}: {prompt.Title}";
                    Repaint();
                    continue;
                }

                status = $"Generating arena art {i + 1}/{prompts.Count}: {prompt.Title}";
                Repaint();

                ArenaMetadata metadata = LoadMetadata($"{prompt.GeneratedDirectory}/{prompt.Slug}_ludo_metadata.json");
                yield return GenerateArenaImage(apiKey, prompt, metadata);
                SaveMetadata($"{prompt.GeneratedDirectory}/{prompt.Slug}_ludo_metadata.json", metadata);
            }

            EnsureArenaStageAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            activeCoroutine = null;
            status = "Arena generation complete.";
            Repaint();
            EditorUtility.DisplayDialog("Ludo AI Arena Batch Complete", $"Arena art and stage prefabs are under:\n{ArtRoot}\n{PrefabRoot}", "OK");
        }

        private IEnumerator GenerateArenaImage(string apiKey, ArenaPrompt prompt, ArenaMetadata metadata)
        {
            var requestData = new Dictionary<string, object>
            {
                ["image_type"] = "fixed_background",
                ["prompt"] = prompt.Prompt,
                ["n"] = 1,
                ["augment_prompt"] = true,
                ["genre"] = "Fighting",
                ["platform"] = "Desktop",
                ["art_style"] = "Cel-Shaded",
                ["perspective"] = "Side-Scroll",
                ["aspect_ratio"] = "ar_16_9"
            };

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using (UnityWebRequest request = CreateJsonPost("/assets/image", apiKey, requestData))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        if (attempt < 3 && IsRetryable(request))
                        {
                            Debug.LogWarning($"[KyberKlash][LudoArenas] Arena image retry {attempt}/3 for {prompt.Title}: {request.error}");
                            yield return null;
                            continue;
                        }

                        Debug.LogError($"[KyberKlash][LudoArenas] Ludo arena image failed for {prompt.Title}: {request.error}\n{request.downloadHandler?.text}");
                        yield break;
                    }

                    List<GeneratedImage> images = null;
                    try
                    {
                        images = JsonConvert.DeserializeObject<List<GeneratedImage>>(request.downloadHandler.text);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[KyberKlash][LudoArenas] Failed to parse arena response for {prompt.Title}: {ex.Message}\n{request.downloadHandler.text}");
                    }

                    if (images == null || images.Count == 0 || string.IsNullOrEmpty(images[0].Url))
                    {
                        Debug.LogError($"[KyberKlash][LudoArenas] Ludo returned no arena image URL for {prompt.Title}.");
                        yield break;
                    }

                    metadata.Title = prompt.Title;
                    metadata.Slug = prompt.Slug;
                    metadata.Prompt = prompt.Prompt;
                    metadata.ImageUrl = images[0].Url;
                    metadata.GeneratedAtUtc = DateTime.UtcNow.ToString("o");
                    yield return DownloadFile(images[0].Url, prompt.ArtDirectory, $"{prompt.Slug}_background", metadata);
                    yield break;
                }
            }
        }

        private void EnsureArenaStageAssets()
        {
            EnsureFolders();
            if (prompts.Count == 0) LoadPrompts();

            for (int i = 0; i < prompts.Count; i++)
            {
                ArenaPrompt prompt = prompts[i];
                Directory.CreateDirectory(prompt.ArtDirectory);
                GameObject prefab = CreateArenaPrefab(prompt, i);
                CreateOrUpdateStageData(prompt, prefab);
            }

            AssignGeneratedStagesToGameManagers();
        }

        private GameObject CreateArenaPrefab(ArenaPrompt prompt, int layoutIndex)
        {
            string prefabPath = $"{PrefabRoot}/Stage_{prompt.Slug}.prefab";
            var root = new GameObject($"Stage_{prompt.Slug}");
            root.transform.position = Vector3.zero;

            CreateBackground(root.transform, prompt);

            Vector3[] positions = GetPlatformPositions(layoutIndex);
            Vector3[] sizes = GetPlatformSizes(layoutIndex);
            for (int i = 0; i < positions.Length; i++)
            {
                CreatePlatform(root.transform, $"Platform_{i + 1}", positions[i], sizes[i], layoutIndex, i);
            }

            CreateSpawnPoint(root.transform, "Spawn_1", new Vector3(-5.5f, 1.2f, 0f), 0);
            CreateSpawnPoint(root.transform, "Spawn_2", new Vector3(5.5f, 1.2f, 0f), 1);
            CreateSpawnPoint(root.transform, "Spawn_3", new Vector3(-2.5f, 4.4f, 0f), 2);
            CreateSpawnPoint(root.transform, "Spawn_4", new Vector3(2.5f, 4.4f, 0f), 3);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);
            return prefab;
        }

        private static void CreateBackground(Transform parent, ArenaPrompt prompt)
        {
            string artPath = FindExistingFile(prompt.ArtDirectory, $"{prompt.Slug}_background");
            if (string.IsNullOrEmpty(artPath))
            {
                return;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(artPath);
            if (texture == null)
            {
                return;
            }

            string materialPath = $"{prompt.ArtDirectory}/{prompt.Slug}_background.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || UsesUnsupportedShader(material))
            {
                Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else
                {
                    material.shader = shader;
                }
            }

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Arena_Background";
            quad.transform.SetParent(parent);
            quad.transform.localPosition = new Vector3(0f, 4.5f, 8f);
            quad.transform.localScale = new Vector3(28f, 15.75f, 1f);
            DestroyImmediate(quad.GetComponent<Collider>());
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private static void CreatePlatform(Transform parent, string name, Vector3 position, Vector3 size, int layoutIndex, int platformIndex)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent);
            platform.transform.localPosition = position;
            platform.transform.localScale = size;
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) platform.layer = groundLayer;

            var renderer = platform.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetPlatformMaterial(layoutIndex, platformIndex);
        }

        private static Material GetPlatformMaterial(int layoutIndex, int platformIndex)
        {
            EnsureFolder("Assets/_Project/Materials/Stages");
            string path = $"Assets/_Project/Materials/Stages/ArenaPlatform_{layoutIndex}_{platformIndex}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (UsesUnsupportedShader(material))
            {
                material.shader = shader;
            }

            Color[] palette =
            {
                new Color(0.22f, 0.26f, 0.34f),
                new Color(0.45f, 0.19f, 0.12f),
                new Color(0.72f, 0.64f, 0.48f),
                new Color(0.18f, 0.36f, 0.25f),
                new Color(0.30f, 0.32f, 0.40f),
                new Color(0.25f, 0.34f, 0.50f),
                new Color(0.25f, 0.18f, 0.32f)
            };
            Color color = palette[Mathf.Abs(layoutIndex) % palette.Length] * (platformIndex == 0 ? 1.15f : 1f);
            color.a = 1f;
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool UsesUnsupportedShader(Material material)
        {
            if (material == null || material.shader == null)
            {
                return true;
            }

            string shaderName = material.shader.name;
            return shaderName == "Hidden/InternalErrorShader" ||
                   shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal);
        }

        private static void CreateSpawnPoint(Transform parent, string name, Vector3 position, int index)
        {
            var spawn = new GameObject(name);
            spawn.transform.SetParent(parent);
            spawn.transform.localPosition = position;
            var spawnPoint = spawn.AddComponent<KyberKlash.Stage.SpawnPoint>();
            spawnPoint.playerIndex = index;
        }

        private static void CreateOrUpdateStageData(ArenaPrompt prompt, GameObject prefab)
        {
            string path = $"{StageDataRoot}/Stage_{prompt.Slug}.asset";
            StageData stageData = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stageData == null)
            {
                stageData = CreateInstance<StageData>();
                AssetDatabase.CreateAsset(stageData, path);
            }

            stageData.stageId = prompt.Slug;
            stageData.displayName = prompt.Title;
            stageData.stageDescription = "Generated floating multi-level arena for platform-fighter ring-out battles.";
            stageData.stagePrefab = prefab;
            stageData.leftBlastZone = -18f;
            stageData.rightBlastZone = 18f;
            stageData.topBlastZone = 16f;
            stageData.bottomBlastZone = -10f;
            stageData.hasHazards = false;
            stageData.isTournamentLegal = true;
            stageData.maxPlayers = 4;
            EditorUtility.SetDirty(stageData);
        }

        private void AssignGeneratedStagesToGameManagers()
        {
            if (prompts.Count == 0)
            {
                return;
            }

            var generatedStages = new List<StageData>();
            for (int i = 0; i < prompts.Count; i++)
            {
                StageData stage = AssetDatabase.LoadAssetAtPath<StageData>($"{StageDataRoot}/Stage_{prompts[i].Slug}.asset");
                if (stage != null)
                {
                    generatedStages.Add(stage);
                }
            }

            if (generatedStages.Count == 0)
            {
                return;
            }

            GameManager prefabManager = LoadPrefabComponent<GameManager>("Assets/_Project/Prefabs/GameManager.prefab");
            if (prefabManager != null)
            {
                AssignStages(prefabManager, generatedStages);
                PrefabUtility.SavePrefabAsset(prefabManager.gameObject);
            }

            foreach (GameManager manager in Resources.FindObjectsOfTypeAll<GameManager>())
            {
                if (manager == null || EditorUtility.IsPersistent(manager))
                {
                    continue;
                }

                AssignStages(manager, generatedStages);
                if (manager.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                }
            }
        }

        private static void AssignStages(GameManager manager, IReadOnlyList<StageData> generatedStages)
        {
            var serializedObject = new SerializedObject(manager);
            SerializedProperty testStageProperty = serializedObject.FindProperty("testStage");
            SerializedProperty stagesProperty = serializedObject.FindProperty("availableStages");
            SerializedProperty randomizeProperty = serializedObject.FindProperty("randomizeStage");

            if (testStageProperty != null)
            {
                testStageProperty.objectReferenceValue = generatedStages[0];
            }

            if (stagesProperty != null)
            {
                stagesProperty.arraySize = generatedStages.Count;
                for (int i = 0; i < generatedStages.Count; i++)
                {
                    stagesProperty.GetArrayElementAtIndex(i).objectReferenceValue = generatedStages[i];
                }
            }

            if (randomizeProperty != null)
            {
                randomizeProperty.boolValue = true;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static T LoadPrefabComponent<T>(string prefabPath) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static Vector3[] GetPlatformPositions(int index)
        {
            switch (index % 7)
            {
                case 1: return new[] { new Vector3(0f, 0f, 0f), new Vector3(-6f, 3.2f, 0f), new Vector3(5.5f, 5.4f, 0f), new Vector3(0f, 8f, 0f) };
                case 2: return new[] { new Vector3(0f, 0f, 0f), new Vector3(-7f, 3.4f, 0f), new Vector3(7f, 3.4f, 0f), new Vector3(0f, 6.8f, 0f) };
                case 3: return new[] { new Vector3(0f, 0f, 0f), new Vector3(-5.5f, 3f, 0f), new Vector3(5.5f, 4.5f, 0f), new Vector3(0f, 7.3f, 0f) };
                case 4: return new[] { new Vector3(0f, 0f, 0f), new Vector3(-8f, 2.8f, 0f), new Vector3(8f, 2.8f, 0f), new Vector3(-2f, 6.2f, 0f), new Vector3(4.5f, 8.5f, 0f) };
                case 5: return new[] { new Vector3(0f, 0f, 0f), new Vector3(-5.8f, 3.2f, 0f), new Vector3(5.8f, 3.2f, 0f), new Vector3(0f, 7.2f, 0f) };
                case 6: return new[] { new Vector3(0f, 0f, 0f), new Vector3(-7f, 3.5f, 0f), new Vector3(1.5f, 5.7f, 0f), new Vector3(7f, 8.1f, 0f) };
                default: return new[] { new Vector3(0f, 0f, 0f), new Vector3(-6f, 3.2f, 0f), new Vector3(6f, 3.2f, 0f), new Vector3(0f, 6.5f, 0f) };
            }
        }

        private static Vector3[] GetPlatformSizes(int index)
        {
            Vector3 main = new Vector3(11f, 0.55f, 4f);
            Vector3 medium = new Vector3(4.8f, 0.45f, 3f);
            Vector3 small = new Vector3(3.6f, 0.45f, 3f);
            return (index % 7) == 4
                ? new[] { main, medium, medium, small, small }
                : new[] { main, medium, medium, small };
        }

        private static UnityWebRequest CreateJsonPost(string endpoint, string apiKey, Dictionary<string, object> requestData)
        {
            string json = JsonConvert.SerializeObject(requestData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            byte[] body = Encoding.UTF8.GetBytes(json);
            var request = new UnityWebRequest(ApiBaseUrl + endpoint, "POST");
            request.SetRequestHeader("x-ludo-tool", "unity");
            request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
            request.SetRequestHeader("Content-Type", "application/json");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = LudoRequestTimeoutSeconds;
            return request;
        }

        private static IEnumerator DownloadFile(string url, string outputDirectory, string baseFileName, ArenaMetadata metadata)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("x-ludo-tool", "unity");
                    request.timeout = DownloadTimeoutSeconds;
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        if (attempt < 3 && IsRetryable(request))
                        {
                            Debug.LogWarning($"[KyberKlash][LudoArenas] Download retry {attempt}/3 for {url}: {request.error}");
                            yield return null;
                            continue;
                        }

                        Debug.LogError($"[KyberKlash][LudoArenas] Failed to download {url}: {request.error}");
                        yield break;
                    }

                    byte[] data = request.downloadHandler.data;
                    string extension = GetExtension(data, url);
                    string assetPath = $"{outputDirectory}/{baseFileName}.{extension}";
                    File.WriteAllBytes(assetPath, data);
                    metadata.Files[baseFileName] = assetPath;
                    Debug.Log($"[KyberKlash][LudoArenas] Saved {assetPath}");
                    yield break;
                }
            }
        }

        private static ArenaMetadata LoadMetadata(string path)
        {
            if (!File.Exists(path)) return new ArenaMetadata();
            try
            {
                ArenaMetadata metadata = JsonConvert.DeserializeObject<ArenaMetadata>(File.ReadAllText(path)) ?? new ArenaMetadata();
                if (metadata.Files == null) metadata.Files = new Dictionary<string, string>();
                return metadata;
            }
            catch
            {
                return new ArenaMetadata();
            }
        }

        private static void SaveMetadata(string path, ArenaMetadata metadata)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(metadata, Formatting.Indented));
        }

        private static bool IsRetryable(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError) return true;
            long code = request.responseCode;
            return code == 408 || code == 429 || code >= 500;
        }

        private static string FindExistingFile(string directory, string baseFileName)
        {
            if (!Directory.Exists(directory)) return string.Empty;
            string[] files = Directory.GetFiles(directory, baseFileName + ".*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');
                if (!normalized.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) &&
                    !normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                        ? normalized
                        : normalized.Replace(Directory.GetCurrentDirectory().Replace('\\', '/') + "/", string.Empty);
                }
            }

            return string.Empty;
        }

        private static string GetExtension(byte[] data, string url)
        {
            if (StartsWith(data, 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A)) return "png";
            if (StartsWith(data, 0xFF, 0xD8, 0xFF)) return "jpg";
            if (data != null && data.Length >= 12 &&
                data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F' &&
                data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P') return "webp";
            string extension = Path.GetExtension(Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ? uri.AbsolutePath : url).TrimStart('.').ToLowerInvariant();
            return string.IsNullOrEmpty(extension) ? "download" : extension;
        }

        private static bool StartsWith(byte[] data, params byte[] signature)
        {
            if (data == null || data.Length < signature.Length) return false;
            for (int i = 0; i < signature.Length; i++)
            {
                if (data[i] != signature[i]) return false;
            }
            return true;
        }

        private static void EnsureFolders()
        {
            EnsureFolder(GeneratedRoot);
            EnsureFolder(ArtRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(StageDataRoot);
        }

        private static void EnsureFolder(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folder = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folder)) return;
            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder(parent, folder);
        }

        private static string ToSlug(string title)
        {
            string normalized = Regex.Replace(title, @"[^A-Za-z0-9]+", "_").Trim('_').ToLowerInvariant();
            return string.IsNullOrEmpty(normalized) ? "arena" : normalized;
        }

        [Serializable]
        private sealed class ArenaPrompt
        {
            public int Number;
            public string Title;
            public string Prompt;
            public string Slug;
            public string GeneratedDirectory;
            public string ArtDirectory;
        }

        [Serializable]
        private sealed class ArenaMetadata
        {
            public string Title;
            public string Slug;
            public string Prompt;
            public string ImageUrl;
            public string GeneratedAtUtc;
            public Dictionary<string, string> Files = new Dictionary<string, string>();
        }

        [Serializable]
        private sealed class GeneratedImage
        {
            [JsonProperty("url")]
            public string Url { get; set; }
        }
    }
}
