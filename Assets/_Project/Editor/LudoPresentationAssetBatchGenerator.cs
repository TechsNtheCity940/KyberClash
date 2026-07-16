using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using WebP;

namespace KyberKlash.EditorTools
{
    public sealed class LudoPresentationAssetBatchGenerator : EditorWindow
    {
        private const string ApiKeyPrefsKey = "LudoAI_API_Key";
        private const string ApiBaseUrl = "https://api.ludo.ai/api";
        private const string UiOutputRoot = "Assets/Resources/KyberKlash/UI";
        private const string AudioOutputRoot = "Assets/Resources/KyberKlash/Audio";
        private const string AutoRunFlagPath = "Assets/_Project/Generated/LudoAI/Presentation/.run_presentation_assets_once";
        private const int RequestTimeoutSeconds = 1200;
        private const int DownloadTimeoutSeconds = 600;

        private readonly List<PresentationJob> jobs = new List<PresentationJob>();
        private Vector2 scrollPosition;
        private string status = "Ready.";
        private bool skipExisting = true;
        private EditorCoroutine activeCoroutine;
        private JobKind? jobKindFilter;

        [MenuItem("Tools/Kyber Clash/Ludo AI/Generate Presentation UI And Audio")]
        public static void GeneratePresentationAssets()
        {
            var window = GetWindow<LudoPresentationAssetBatchGenerator>("Ludo Presentation");
            window.LoadJobs();
            window.StartGeneration();
        }

        public static void GeneratePresentationAssetsBatch()
        {
            var window = CreateInstance<LudoPresentationAssetBatchGenerator>();
            window.skipExisting = true;
            window.LoadJobs();
            window.StartGeneration();
        }

        public static void GeneratePresentationUiAssetsBatch()
        {
            var window = CreateInstance<LudoPresentationAssetBatchGenerator>();
            window.skipExisting = true;
            window.jobKindFilter = JobKind.UiImage;
            window.LoadJobs();
            window.StartGeneration();
        }

        [MenuItem("Tools/Kyber Clash/Ludo AI/Batch Presentation Assets")]
        public static void ShowWindow()
        {
            GetWindow<LudoPresentationAssetBatchGenerator>("Ludo Presentation");
        }

        [InitializeOnLoadMethod]
        private static void RunQueuedGenerationAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode)
                {
                    return;
                }

                if (!File.Exists(AutoRunFlagPath))
                {
                    return;
                }

                string apiKey = EditorPrefs.GetString(ApiKeyPrefsKey, string.Empty);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Debug.LogWarning("[KyberKlash][LudoPresentation] Presentation asset generation is queued, but no Ludo API key is saved.");
                    return;
                }

                File.Delete(AutoRunFlagPath);
                GeneratePresentationAssets();
            };
        }

        private void OnEnable()
        {
            LoadJobs();
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
            GUILayout.Label("Ludo AI Presentation Assets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generates start/loading/character select/HUD UI art plus menu SFX, combat SFX, and music into Resources for runtime loading.", MessageType.Info);

            skipExisting = EditorGUILayout.ToggleLeft("Skip existing generated files", skipExisting);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload Jobs")) LoadJobs();

                GUI.enabled = activeCoroutine == null;
                if (GUILayout.Button("Generate Missing")) StartGeneration();
                GUI.enabled = true;
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (PresentationJob job in jobs)
            {
                EditorGUILayout.LabelField($"{job.Kind}: {job.Key}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(job.Prompt, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(6);
            }
            EditorGUILayout.EndScrollView();
        }

        private void LoadJobs()
        {
            jobs.Clear();

            jobs.Add(new PresentationJob("start_screen_background", JobKind.UiImage, "Super Smash Bros Ultimate style sci-fi fantasy fighting game start screen background for Kyber Clash, blue-white kyber crystal energy crossing red saber sparks, floating arenas in the distance, strong center space for title logo, clean cel-shaded key art, no readable text, no characters, 16:9 game UI background."));
            jobs.Add(new PresentationJob("loading_screen_background", JobKind.UiImage, "Super Smash Bros Ultimate style loading screen background for Kyber Clash, abstract hyperspace streaks, kyber crystal particles, silhouettes of floating platform arenas, clean cel-shaded game UI art, no readable text, 16:9 background."));
            jobs.Add(new PresentationJob("character_select_background", JobKind.UiImage, "Super Smash Bros Ultimate style character select screen background, sci-fi Jedi archive mixed with battle arena holograms, grid-friendly dark panels, glowing blue and magenta accents, clean readable UI composition, no readable text, 16:9 game UI background."));
            jobs.Add(new PresentationJob("hud_panel_frame", JobKind.UiImage, "Platform fighter HUD panel frame asset, compact angular sci-fi card frame for player damage percent and meter, transparent background, blue-white kyber glow accents, clean cel-shaded UI asset, no text, no icons."));
            jobs.Add(new PresentationJob("stock_icon", JobKind.UiImage, "Small platform fighter stock icon, stylized kyber saber emblem, transparent background, clean cel-shaded UI asset, readable at small size, no text."));
            jobs.Add(new PresentationJob("menu_button_frame", JobKind.UiImage, "Sci-fi platform fighter menu button frame, blue-white kyber edge glow with dark inner panel, transparent background, no text, clean cel-shaded UI asset."));
            jobs.Add(new PresentationJob("menu_button_normal", JobKind.UiImage, "Space opera platform fighter menu button normal state, angular dark control panel with blue-white kyber edge light, transparent background, no text, clean cel-shaded UI asset."));
            jobs.Add(new PresentationJob("menu_button_hover", JobKind.UiImage, "Space opera platform fighter menu button hover state, angular dark control panel with brighter blue-white kyber glow and subtle saber energy, transparent background, no text, clean cel-shaded UI asset."));
            jobs.Add(new PresentationJob("menu_button_pressed", JobKind.UiImage, "Space opera platform fighter menu button pressed state, angular dark control panel with magenta-red saber pulse and kyber highlights, transparent background, no text, clean cel-shaded UI asset."));
            jobs.Add(new PresentationJob("menu_panel_frame", JobKind.UiImage, "Large space opera holographic menu panel frame for fighting game UI, dark inner panel, blue-white kyber border, transparent background, no readable text."));
            jobs.Add(new PresentationJob("character_card_frame", JobKind.UiImage, "Character selection card frame for platform fighter, sci-fi kyber hologram panel, blue-white edge lighting, transparent background, no readable text."));
            jobs.Add(new PresentationJob("character_card_selected", JobKind.UiImage, "Selected character card frame for platform fighter, sci-fi kyber hologram panel with magenta saber highlight, transparent background, no readable text."));
            jobs.Add(new PresentationJob("loading_bar_frame", JobKind.UiImage, "Space opera fighting game loading bar frame, angular sci-fi kyber metal frame, transparent background, no text."));
            jobs.Add(new PresentationJob("loading_bar_fill", JobKind.UiImage, "Space opera fighting game loading bar fill, bright blue-white kyber energy strip, transparent background, no text."));
            for (int spinnerFrame = 0; spinnerFrame < 8; spinnerFrame++)
            {
                jobs.Add(new PresentationJob($"loading_spinner_{spinnerFrame}", JobKind.UiImage, $"Single frame {spinnerFrame + 1} of 8 for a rotating kyber crystal loading spinner, space opera fighting game UI, transparent background, no text, clean cel-shaded asset."));
            }
            jobs.Add(new PresentationJob("kyber_klash_logo", JobKind.UiImage, "Game logo art reading Kyber Klash, energetic platform fighter style, kyber crystal typography, blue-white and magenta saber glow, transparent background."));

            jobs.Add(new PresentationJob("ui_confirm", JobKind.SoundEffect, "Crisp futuristic menu confirm sound, short kyber crystal chime with subtle saber ignition accent, polished fighting game UI feedback.", 0.6f));
            jobs.Add(new PresentationJob("ui_move", JobKind.SoundEffect, "Short futuristic menu cursor movement sound, soft kyber tick and digital swipe, polished fighting game UI feedback.", 0.25f));
            jobs.Add(new PresentationJob("light_slash", JobKind.SoundEffect, "Fast lightsaber light slash sound effect, clean energetic swing, brief plasma trail, platform fighter impact-ready.", 0.7f));
            jobs.Add(new PresentationJob("heavy_slash", JobKind.SoundEffect, "Heavy lightsaber slash sound effect, powerful plasma sweep with low impact weight, platform fighter attack feedback.", 1.0f));
            jobs.Add(new PresentationJob("parry_clash", JobKind.SoundEffect, "Bright lightsaber parry clash sound, sharp metallic plasma crack with sparkling kyber resonance, platform fighter defensive feedback.", 0.8f));
            jobs.Add(new PresentationJob("player_hit", JobKind.SoundEffect, "Platform fighter hit impact sound, stylized energy burst and body hit, clear but not gory.", 0.5f));
            jobs.Add(new PresentationJob("ringout_ko", JobKind.SoundEffect, "Dramatic platform fighter ring-out KO sound, rising energy whoosh into punchy victory blast.", 1.5f));
            jobs.Add(new PresentationJob("jump", JobKind.SoundEffect, "Short sci-fi force jump sound, light boot push and airy kyber shimmer.", 0.4f));
            jobs.Add(new PresentationJob("land", JobKind.SoundEffect, "Short platform fighter landing thud, cloth and boot impact with subtle dust puff.", 0.4f));

            jobs.Add(new PresentationJob("music_title", JobKind.Music, "Loopable title screen music for Kyber Clash, heroic sci-fi fantasy platform fighter theme, energetic orchestral percussion, kyber crystal synth shimmer, no vocals."));
            jobs.Add(new PresentationJob("music_character_select", JobKind.Music, "Loopable character select music, upbeat competitive fighting game groove, sci-fi synth bass, light heroic strings, no vocals."));
            jobs.Add(new PresentationJob("music_gameplay_battle", JobKind.Music, "Loopable gameplay battle music, high-energy Super Smash Bros style arena fight theme, sci-fi orchestral percussion, fast strings, saber-like synth accents, no vocals."));

            status = $"Loaded {jobs.Count} presentation jobs.";
        }

        private void StartGeneration()
        {
            string apiKey = EditorPrefs.GetString(ApiKeyPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (Application.isBatchMode)
                {
                    Debug.LogError("[KyberKlash][LudoPresentation] Ludo API key is missing. Save it in the Ludo AI Unity plugin before running batch presentation generation.");
                    EditorApplication.Exit(1);
                    return;
                }

                EditorUtility.DisplayDialog("Ludo AI API Key Missing", "Save your Ludo AI API key from Ludo AI > Ludo AI Plugin before generating presentation assets.", "OK");
                return;
            }

            EnsureFolders();
            activeCoroutine = EditorCoroutineUtility.StartCoroutine(GenerateRoutine(apiKey), this);
        }

        private IEnumerator GenerateRoutine(string apiKey)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                PresentationJob job = jobs[i];
                if (jobKindFilter.HasValue && job.Kind != jobKindFilter.Value)
                {
                    continue;
                }

                if (skipExisting && HasExistingOutput(job))
                {
                    continue;
                }

                status = $"Generating {i + 1}/{jobs.Count}: {job.Key}";
                Repaint();

                if (job.Kind == JobKind.UiImage)
                {
                    yield return GenerateUiImage(apiKey, job);
                }
                else
                {
                    yield return GenerateAudio(apiKey, job);
                }
            }

            status = "Presentation asset generation complete.";
            activeCoroutine = null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KyberKlash][LudoPresentation] Presentation asset generation complete.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private IEnumerator GenerateUiImage(string apiKey, PresentationJob job)
        {
            var requestData = new Dictionary<string, object>
            {
                ["prompt"] = job.Prompt,
                ["image_type"] = "ui_asset",
                ["n"] = 1,
                ["genre"] = "Fighting",
                ["platform"] = "Desktop",
                ["art_style"] = "Cel-Shaded",
                ["perspective"] = "2.5D",
                ["aspect_ratio"] = "ar_16_9",
                ["augment_prompt"] = true
            };

            using (UnityWebRequest request = CreateJsonPost("/assets/image", apiKey, requestData))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[KyberKlash][LudoPresentation] UI image failed for {job.Key}: {request.error}\n{request.downloadHandler.text}");
                    yield break;
                }

                List<GeneratedImage> images = JsonConvert.DeserializeObject<List<GeneratedImage>>(request.downloadHandler.text);
                if (images == null || images.Count == 0 || string.IsNullOrEmpty(images[0].Url))
                {
                    Debug.LogError($"[KyberKlash][LudoPresentation] Ludo returned no image URL for {job.Key}.");
                    yield break;
                }

                yield return DownloadAndSaveImage(images[0].Url, $"{UiOutputRoot}/{job.Key}.png");
            }
        }

        private IEnumerator GenerateAudio(string apiKey, PresentationJob job)
        {
            string endpoint = job.Kind == JobKind.Music ? "/audio/music" : "/audio/sound-effect";
            var requestData = new Dictionary<string, object>
            {
                ["description"] = job.Prompt,
                ["augment_prompt"] = true
            };

            if (job.Kind == JobKind.SoundEffect)
            {
                requestData["duration"] = job.Duration;
            }

            using (UnityWebRequest request = CreateJsonPost(endpoint, apiKey, requestData))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[KyberKlash][LudoPresentation] Audio failed for {job.Key}: {request.error}\n{request.downloadHandler.text}");
                    yield break;
                }

                GeneratedAudio audio = JsonConvert.DeserializeObject<GeneratedAudio>(request.downloadHandler.text);
                if (audio == null || string.IsNullOrEmpty(audio.Url))
                {
                    Debug.LogError($"[KyberKlash][LudoPresentation] Ludo returned no audio URL for {job.Key}.");
                    yield break;
                }

                yield return DownloadFile(audio.Url, $"{AudioOutputRoot}/{job.Key}.wav");
                AssetDatabase.ImportAsset($"{AudioOutputRoot}/{job.Key}.wav", ImportAssetOptions.ForceUpdate);
            }
        }

        private IEnumerator DownloadAndSaveImage(string url, string outputPath)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("x-ludo-tool", "unity");
                request.timeout = DownloadTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[KyberKlash][LudoPresentation] Failed to download image {url}: {request.error}");
                    yield break;
                }

                Texture2D texture = DecodeTexture(request.downloadHandler.data);
                if (texture == null)
                {
                    Debug.LogError($"[KyberKlash][LudoPresentation] Failed to decode image for {outputPath}.");
                    yield break;
                }

                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
                ConfigureSprite(outputPath);
            }
        }

        private IEnumerator DownloadFile(string url, string outputPath)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("x-ludo-tool", "unity");
                request.timeout = DownloadTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[KyberKlash][LudoPresentation] Failed to download {url}: {request.error}");
                    yield break;
                }

                File.WriteAllBytes(outputPath, request.downloadHandler.data);
            }
        }

        private static Texture2D DecodeTexture(byte[] bytes)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
            {
                return texture;
            }

            UnityEngine.Object.DestroyImmediate(texture);
            WebP.Error error;
            texture = Texture2DExt.CreateTexture2DFromWebP(bytes, false, false, out error, null, false);
            return error == WebP.Error.Success ? texture : null;
        }

        private static void ConfigureSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private UnityWebRequest CreateJsonPost(string endpoint, string apiKey, Dictionary<string, object> requestData)
        {
            string json = JsonConvert.SerializeObject(requestData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            byte[] body = Encoding.UTF8.GetBytes(json);
            var request = new UnityWebRequest(ApiBaseUrl + endpoint, "POST");
            request.SetRequestHeader("x-ludo-tool", "unity");
            request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
            request.SetRequestHeader("Content-Type", "application/json");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            return request;
        }

        private static bool HasExistingOutput(PresentationJob job)
        {
            string path = job.Kind == JobKind.UiImage
                ? $"{UiOutputRoot}/{job.Key}.png"
                : $"{AudioOutputRoot}/{job.Key}.wav";
            return File.Exists(path);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/KyberKlash");
            EnsureFolder(UiOutputRoot);
            EnsureFolder(AudioOutputRoot);
            EnsureFolder("Assets/_Project/Generated/LudoAI/Presentation");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private enum JobKind
        {
            UiImage,
            SoundEffect,
            Music
        }

        private sealed class PresentationJob
        {
            public readonly string Key;
            public readonly JobKind Kind;
            public readonly string Prompt;
            public readonly float Duration;

            public PresentationJob(string key, JobKind kind, string prompt, float duration = 0f)
            {
                Key = key;
                Kind = kind;
                Prompt = prompt;
                Duration = duration;
            }
        }

        [Serializable]
        private sealed class GeneratedImage
        {
            [JsonProperty("url")]
            public string Url;
        }

        [Serializable]
        private sealed class GeneratedAudio
        {
            [JsonProperty("url")]
            public string Url;
        }
    }
}
