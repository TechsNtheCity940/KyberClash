using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace KyberKlash.EditorTools
{
    public sealed class LudoCharacterArtBatchGenerator : EditorWindow
    {
        private const string ApiKeyPrefsKey = "LudoAI_API_Key";
        private const string ApiBaseUrl = "https://api.ludo.ai/api";
        private const string PromptFilePath = "AssetPrompts/CharacterFormPrompts.md";
        private const string OutputRoot = "Assets/_Project/Generated/LudoAI/Characters";
        private const string AutoRunFlagPath = "Assets/_Project/Generated/LudoAI/Characters/.run_remaining_forms_once";
        private const int LudoRequestTimeoutSeconds = 1200;
        private const int DownloadTimeoutSeconds = 600;

        private readonly List<CharacterPrompt> prompts = new List<CharacterPrompt>();
        private Vector2 scrollPosition;
        private bool skipExisting = true;
        private bool generateKeyArt = true;
        private bool generateAnimations = true;
        private int startIndex;
        private int endIndex = -1;
        private string status = "Load prompts to begin.";
        private EditorCoroutine activeCoroutine;

        [MenuItem("Tools/Kyber Clash/Ludo AI/Batch Generate Character Art")]
        public static void ShowWindow()
        {
            GetWindow<LudoCharacterArtBatchGenerator>("Ludo Character Art");
        }

        [MenuItem("Tools/Kyber Clash/Ludo AI/Generate Missing Character Art From Prompts")]
        public static void GenerateMissingFromPrompts()
        {
            var window = GetWindow<LudoCharacterArtBatchGenerator>("Ludo Character Art");
            window.LoadPrompts();
            window.skipExisting = true;
            window.generateKeyArt = true;
            window.generateAnimations = true;
            window.StartGeneration();
        }

        [MenuItem("Tools/Kyber Clash/Ludo AI/Generate Remaining Character Art From Prompts")]
        public static void GenerateRemainingFromPrompts()
        {
            var window = GetWindow<LudoCharacterArtBatchGenerator>("Ludo Character Art");
            window.LoadPrompts();
            window.skipExisting = true;
            window.generateKeyArt = true;
            window.generateAnimations = true;
            window.startIndex = 1;
            window.endIndex = window.prompts.Count - 1;
            window.StartGeneration();
        }

        [InitializeOnLoadMethod]
        private static void RunQueuedBatchAfterReload()
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
                    EditorUtility.DisplayDialog("Ludo AI API Key Missing", "The queued KyberKlash Ludo batch cannot run because the Ludo AI API key is not saved. Open Ludo AI > Ludo AI Plugin, save your API key, then run Tools > Kyber Clash > Ludo AI > Generate Remaining Character Art From Prompts.", "OK");
                    return;
                }

                File.Delete(AutoRunFlagPath);
                GenerateRemainingFromPrompts();
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
            GUILayout.Label("Ludo AI Character Art Batch", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Uses the installed Ludo AI plugin API key from EditorPrefs and the same /assets/image and /assets/sprite/animate endpoints.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload Prompts"))
                {
                    LoadPrompts();
                }

                GUI.enabled = activeCoroutine == null;
                if (GUILayout.Button("Generate Missing"))
                {
                    skipExisting = true;
                    StartGeneration();
                }

                if (GUILayout.Button("Generate Selected Range"))
                {
                    StartGeneration();
                }
                GUI.enabled = true;
            }

            skipExisting = EditorGUILayout.ToggleLeft("Skip forms with existing generated outputs", skipExisting);
            generateKeyArt = EditorGUILayout.ToggleLeft("Generate character/key art", generateKeyArt);
            generateAnimations = EditorGUILayout.ToggleLeft("Generate animation spritesheets", generateAnimations);

            using (new EditorGUILayout.HorizontalScope())
            {
                startIndex = EditorGUILayout.IntField("Start Index", Mathf.Max(0, startIndex));
                endIndex = EditorGUILayout.IntField("End Index", endIndex < 0 ? prompts.Count - 1 : endIndex);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(status, MessageType.None);

            EditorGUILayout.Space();
            GUILayout.Label($"Prompts ({prompts.Count})", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < prompts.Count; i++)
            {
                CharacterPrompt prompt = prompts[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GUILayout.Label($"{i + 1}. {prompt.Title}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Slug", prompt.Slug);
                    EditorGUILayout.LabelField("Output", prompt.OutputDirectory);
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
                status = $"Prompt file not found: {PromptFilePath}";
                return;
            }

            string markdown = File.ReadAllText(fullPath, Encoding.UTF8);
            var matches = Regex.Matches(markdown, @"^##\s+(?<number>\d+)\.\s+(?<title>.+?)\r?\n>\s+(?<prompt>.+?)(?=\r?\n\r?\n##|\r?\n---|\z)", RegexOptions.Multiline | RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                string title = match.Groups["title"].Value.Trim();
                string prompt = Regex.Replace(match.Groups["prompt"].Value.Trim(), @"\s+", " ");
                string slug = ToSlug(title);
                prompts.Add(new CharacterPrompt
                {
                    Number = int.Parse(match.Groups["number"].Value),
                    Title = title,
                    Prompt = prompt,
                    Slug = slug,
                    OutputDirectory = $"{OutputRoot}/{slug}"
                });
            }

            endIndex = prompts.Count - 1;
            status = prompts.Count == 0
                ? "No prompts found. Expected headings followed by blockquoted prompt lines."
                : $"Loaded {prompts.Count} character prompts.";
        }

        private void StartGeneration()
        {
            if (activeCoroutine != null)
            {
                status = "Generation is already running.";
                return;
            }

            string apiKey = EditorPrefs.GetString(ApiKeyPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                EditorUtility.DisplayDialog("Ludo AI API Key Missing", "Open Ludo AI > Ludo AI Plugin, enter your API key in Settings, then run this batch again.", "OK");
                status = "Missing Ludo AI API key.";
                return;
            }

            if (prompts.Count == 0)
            {
                LoadPrompts();
            }

            Directory.CreateDirectory(OutputRoot);
            activeCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(GenerateRoutine(apiKey));
        }

        private IEnumerator GenerateRoutine(string apiKey)
        {
            int first = Mathf.Clamp(startIndex, 0, Mathf.Max(0, prompts.Count - 1));
            int last = Mathf.Clamp(endIndex < 0 ? prompts.Count - 1 : endIndex, first, Mathf.Max(0, prompts.Count - 1));

            for (int i = first; i <= last; i++)
            {
                CharacterPrompt prompt = prompts[i];
                Directory.CreateDirectory(prompt.OutputDirectory);

                string metadataPath = $"{prompt.OutputDirectory}/{prompt.Slug}_ludo_metadata.json";
                CharacterGenerationMetadata metadata = LoadMetadata(metadataPath);
                bool hasKeyArt = HasExistingOutput(prompt.OutputDirectory, $"{prompt.Slug}_key_art");
                bool hasAnimation = HasExistingOutput(prompt.OutputDirectory, $"{prompt.Slug}_animation_spritesheet");

                if (skipExisting && (!generateKeyArt || hasKeyArt) && (!generateAnimations || hasAnimation))
                {
                    status = $"Skipping existing output for {prompt.Title}.";
                    Repaint();
                    continue;
                }

                if (generateKeyArt && (!skipExisting || !hasKeyArt || string.IsNullOrEmpty(metadata.ImageUrl)))
                {
                    if (!hasKeyArt && !string.IsNullOrEmpty(metadata.ImageUrl))
                    {
                        status = $"Downloading existing key art {i + 1}/{prompts.Count}: {prompt.Title}";
                        Repaint();

                        yield return DownloadFile(metadata.ImageUrl, prompt.OutputDirectory, $"{prompt.Slug}_key_art", metadata);
                    }
                    else
                    {
                        status = $"Generating key art {i + 1}/{prompts.Count}: {prompt.Title}";
                        Repaint();

                        yield return GenerateImage(apiKey, prompt, metadata);
                    }

                    SaveMetadata(metadataPath, metadata);
                    hasKeyArt = HasExistingOutput(prompt.OutputDirectory, $"{prompt.Slug}_key_art");
                }

                if (generateAnimations)
                {
                    if (string.IsNullOrEmpty(metadata.ImageUrl))
                    {
                        status = $"Cannot animate {prompt.Title}; no Ludo image URL is available.";
                        Debug.LogWarning($"[KyberKlash][LudoBatch] Missing image URL for {prompt.Title}. Regenerate key art first.");
                        Repaint();
                        continue;
                    }

                    if (!skipExisting || !hasAnimation)
                    {
                        status = $"Generating animation spritesheet {i + 1}/{prompts.Count}: {prompt.Title}";
                        Repaint();

                        if (!string.IsNullOrEmpty(metadata.AnimationSpritesheetUrl))
                        {
                            status = $"Downloading existing animation spritesheet {i + 1}/{prompts.Count}: {prompt.Title}";
                            Repaint();

                            yield return DownloadFile(metadata.AnimationSpritesheetUrl, prompt.OutputDirectory, $"{prompt.Slug}_animation_spritesheet", metadata);
                            if (!string.IsNullOrEmpty(metadata.AnimationGifUrl))
                            {
                                yield return DownloadFile(metadata.AnimationGifUrl, prompt.OutputDirectory, $"{prompt.Slug}_animation_preview", metadata);
                            }

                            if (!string.IsNullOrEmpty(metadata.AnimationVideoUrl))
                            {
                                yield return DownloadFile(metadata.AnimationVideoUrl, prompt.OutputDirectory, $"{prompt.Slug}_animation_video", metadata);
                            }
                        }
                        else
                        {
                            yield return GenerateAnimation(apiKey, prompt, metadata);
                        }

                        SaveMetadata(metadataPath, metadata);
                    }
                }
            }

            AssetDatabase.Refresh();
            status = "Ludo AI character art batch complete.";
            activeCoroutine = null;
            Repaint();
            EditorUtility.DisplayDialog("Ludo AI Batch Complete", $"Generated assets under:\n{OutputRoot}", "OK");
        }

        private IEnumerator GenerateImage(string apiKey, CharacterPrompt prompt, CharacterGenerationMetadata metadata)
        {
            var requestData = new Dictionary<string, object>
            {
                ["image_type"] = "sprite",
                ["prompt"] = prompt.Prompt,
                ["n"] = 1,
                ["augment_prompt"] = true,
                ["genre"] = "Fighting",
                ["platform"] = "Desktop",
                ["art_style"] = "Cel-Shaded",
                ["perspective"] = "Side-Scroll",
                ["aspect_ratio"] = "ar_1_1"
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
                            Debug.LogWarning($"[KyberKlash][LudoBatch] Image generation retry {attempt}/3 for {prompt.Title}: {request.error}");
                            yield return null;
                            continue;
                        }

                        LogRequestError("image generation", prompt, request);
                        yield break;
                    }

                    List<GeneratedImage> images = null;
                    try
                    {
                        images = JsonConvert.DeserializeObject<List<GeneratedImage>>(request.downloadHandler.text);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[KyberKlash][LudoBatch] Failed to parse image response for {prompt.Title}: {ex.Message}\n{request.downloadHandler.text}");
                    }

                    if (images == null || images.Count == 0 || string.IsNullOrEmpty(images[0].Url))
                    {
                        Debug.LogError($"[KyberKlash][LudoBatch] Ludo returned no image URL for {prompt.Title}.");
                        yield break;
                    }

                    metadata.Title = prompt.Title;
                    metadata.Slug = prompt.Slug;
                    metadata.Prompt = prompt.Prompt;
                    metadata.ImageUrl = images[0].Url;
                    metadata.GeneratedAtUtc = DateTime.UtcNow.ToString("o");

                    yield return DownloadFile(images[0].Url, prompt.OutputDirectory, $"{prompt.Slug}_key_art", metadata);
                    yield break;
                }
            }
        }

        private IEnumerator GenerateAnimation(string apiKey, CharacterPrompt prompt, CharacterGenerationMetadata metadata)
        {
            var requestData = new Dictionary<string, object>
            {
                ["motion_prompt"] = BuildMotionPrompt(prompt),
                ["initial_image"] = metadata.ImageUrl,
                ["loop"] = true,
                ["crop"] = true,
                ["frames"] = 9,
                ["frame_size"] = 256,
                ["model"] = "standard",
                ["duration"] = 2.0f,
                ["image_type"] = "sprite",
                ["augment_prompt"] = true,
                ["margin_ratio_mode"] = "auto"
            };

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using (UnityWebRequest request = CreateJsonPost("/assets/sprite/animate", apiKey, requestData))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        if (attempt < 3 && IsRetryable(request))
                        {
                            Debug.LogWarning($"[KyberKlash][LudoBatch] Animation retry {attempt}/3 for {prompt.Title}: {request.error}");
                            yield return null;
                            continue;
                        }

                        LogRequestError("sprite animation", prompt, request);
                        yield break;
                    }

                    AnimatedSpriteResponse response = null;
                    try
                    {
                        response = JsonConvert.DeserializeObject<AnimatedSpriteResponse>(request.downloadHandler.text);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[KyberKlash][LudoBatch] Failed to parse animation response for {prompt.Title}: {ex.Message}\n{request.downloadHandler.text}");
                    }

                    if (response == null || string.IsNullOrEmpty(response.SpritesheetUrl))
                    {
                        Debug.LogError($"[KyberKlash][LudoBatch] Ludo returned no spritesheet URL for {prompt.Title}.");
                        yield break;
                    }

                    metadata.AnimationSpritesheetUrl = response.SpritesheetUrl;
                    metadata.AnimationGifUrl = response.GifUrl;
                    metadata.AnimationVideoUrl = response.VideoUrl;
                    metadata.GeneratedAtUtc = DateTime.UtcNow.ToString("o");

                    yield return DownloadFile(response.SpritesheetUrl, prompt.OutputDirectory, $"{prompt.Slug}_animation_spritesheet", metadata);
                    if (!string.IsNullOrEmpty(response.GifUrl))
                    {
                        yield return DownloadFile(response.GifUrl, prompt.OutputDirectory, $"{prompt.Slug}_animation_preview", metadata);
                    }

                    if (!string.IsNullOrEmpty(response.VideoUrl))
                    {
                        yield return DownloadFile(response.VideoUrl, prompt.OutputDirectory, $"{prompt.Slug}_animation_video", metadata);
                    }

                    yield break;
                }
            }
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

        private IEnumerator DownloadFile(string url, string outputDirectory, string baseFileName, CharacterGenerationMetadata metadata)
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
                            Debug.LogWarning($"[KyberKlash][LudoBatch] Download retry {attempt}/3 for {url}: {request.error}");
                            yield return null;
                            continue;
                        }

                        Debug.LogError($"[KyberKlash][LudoBatch] Failed to download {url}: {request.error}. Stored the URL in metadata so a later run can retry without regenerating.");
                        yield break;
                    }

                    byte[] data = request.downloadHandler.data;
                    string extension = GetExtension(data, url);
                    string assetPath = $"{outputDirectory}/{baseFileName}.{extension}";
                    File.WriteAllBytes(assetPath, data);
                    metadata.Files[baseFileName] = assetPath;
                    Debug.Log($"[KyberKlash][LudoBatch] Saved {assetPath}");
                    yield break;
                }
            }
        }

        private static bool IsRetryable(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return true;
            }

            long code = request.responseCode;
            return code == 408 || code == 429 || code >= 500;
        }

        private static CharacterGenerationMetadata LoadMetadata(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                return new CharacterGenerationMetadata();
            }

            try
            {
                CharacterGenerationMetadata metadata = JsonConvert.DeserializeObject<CharacterGenerationMetadata>(File.ReadAllText(assetPath)) ?? new CharacterGenerationMetadata();
                if (metadata.Files == null)
                {
                    metadata.Files = new Dictionary<string, string>();
                }

                return metadata;
            }
            catch
            {
                return new CharacterGenerationMetadata();
            }
        }

        private static void SaveMetadata(string assetPath, CharacterGenerationMetadata metadata)
        {
            File.WriteAllText(assetPath, JsonConvert.SerializeObject(metadata, Formatting.Indented));
        }

        private static bool HasExistingOutput(string directory, string baseFileName)
        {
            if (!Directory.Exists(directory))
            {
                return false;
            }

            string[] files = Directory.GetFiles(directory, baseFileName + ".*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                if (!files[i].EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildMotionPrompt(CharacterPrompt prompt)
        {
            string title = prompt.Title.ToLowerInvariant();
            if (title.Contains("makashi")) return "precise dueling footwork, elegant thrust, parry flash, fast riposte, seamless loop";
            if (title.Contains("soresu")) return "defensive guard, deflecting incoming bolts with curved energy arcs, compact stance, seamless loop";
            if (title.Contains("ataru")) return "acrobatic leap, spinning saber flourish, aerial combo motion trail, seamless loop";
            if (title.Contains("shien") || title.Contains("djem")) return "heavy two-handed saber swing, power counter shockwave, grounded impact, seamless loop";
            if (title.Contains("niman")) return "balanced saber slash with force push aura and expanding energy ring, seamless loop";
            if (title.Contains("juyo") || title.Contains("vaapad")) return "wild aggressive saber flurry with dark energy aura and ember sparks, seamless loop";
            return "balanced saber idle, walk, jump, slash, block counter, hit react, seamless loop";
        }

        private static string ToSlug(string title)
        {
            string withoutNotes = Regex.Replace(title, @"\s*\(.*?\)\s*$", string.Empty);
            string normalized = Regex.Replace(withoutNotes, @"[^A-Za-z0-9]+", "_").Trim('_').ToLowerInvariant();
            return string.IsNullOrEmpty(normalized) ? "character" : normalized;
        }

        private static void LogRequestError(string operation, CharacterPrompt prompt, UnityWebRequest request)
        {
            string response = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            Debug.LogError($"[KyberKlash][LudoBatch] Ludo {operation} failed for {prompt.Title}: {request.error}\n{response}");
        }

        private static string GetExtension(byte[] data, string url)
        {
            if (StartsWith(data, 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A)) return "png";
            if (StartsWith(data, 0xFF, 0xD8, 0xFF)) return "jpg";
            if (StartsWith(data, (byte)'G', (byte)'I', (byte)'F', (byte)'8')) return "gif";
            if (data != null && data.Length >= 12 &&
                data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F' &&
                data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P') return "webp";

            string extension = Path.GetExtension(Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ? uri.AbsolutePath : url).TrimStart('.').ToLowerInvariant();
            if (extension == "jpeg") return "jpg";
            return string.IsNullOrEmpty(extension) ? "download" : extension;
        }

        private static bool StartsWith(byte[] data, params byte[] signature)
        {
            if (data == null || data.Length < signature.Length)
            {
                return false;
            }

            for (int i = 0; i < signature.Length; i++)
            {
                if (data[i] != signature[i])
                {
                    return false;
                }
            }

            return true;
        }

        [Serializable]
        private sealed class CharacterPrompt
        {
            public int Number;
            public string Title;
            public string Prompt;
            public string Slug;
            public string OutputDirectory;
        }

        [Serializable]
        private sealed class CharacterGenerationMetadata
        {
            public string Title;
            public string Slug;
            public string Prompt;
            public string ImageUrl;
            public string AnimationSpritesheetUrl;
            public string AnimationGifUrl;
            public string AnimationVideoUrl;
            public string GeneratedAtUtc;
            public Dictionary<string, string> Files = new Dictionary<string, string>();
        }

        [Serializable]
        private sealed class GeneratedImage
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }
        }

        [Serializable]
        private sealed class AnimatedSpriteResponse
        {
            [JsonProperty("spritesheet_url")]
            public string SpritesheetUrl { get; set; }

            [JsonProperty("video_url")]
            public string VideoUrl { get; set; }

            [JsonProperty("gif_url")]
            public string GifUrl { get; set; }
        }
    }
}
