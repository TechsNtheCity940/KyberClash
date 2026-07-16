using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KyberKlash.Core;
using KyberKlash.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;
using WebP;

namespace KyberKlash.EditorTools
{
    public static class LudoGeneratedAssetOrganizer
    {
        private const string GeneratedCharactersRoot = "Assets/_Project/Generated/LudoAI/Characters";
        private const string CharacterArtRoot = "Assets/_Project/Art/Characters";
        private const string CharacterAnimationRoot = "Assets/_Project/Animations/Characters";
        private const string CharacterPrefabRoot = "Assets/_Project/Prefabs/Characters";
        private const string GeneratedCharacterDataRoot = "Assets/_Project/Data/Characters/Generated";
        private const string FormDataRoot = "Assets/_Project/Data/Forms";
        private const string AutoRunFlagPath = "Assets/_Project/Generated/LudoAI/Characters/.organize_generated_assets_once";
        private const string VisualChildName = "GeneratedVisual";
        private const int SpriteSheetColumns = 6;
        private const int SpriteSheetRows = 4;

        private static readonly string[] RuntimeStateNames =
        {
            "Idle",
            "Move",
            "Jump",
            "DoubleJump",
            "Fall",
            "Dash",
            "AirDash",
            "LightAttack",
            "HeavyAttack",
            "UpAir",
            "DownAir",
            "ForwardAir",
            "BackAir",
            "Attack",
            "Special",
            "NeutralSpecial",
            "SideSpecial",
            "UpSpecial",
            "DownSpecial",
            "Block",
            "Parry",
            "HitStun",
            "Knockback",
            "Launch",
            "Death",
            "Respawn",
            "SaberOpen",
            "SaberClose"
        };

        [MenuItem("Tools/Kyber Clash/Ludo AI/Organize Generated Character Assets")]
        public static void OrganizeGeneratedCharacterAssets()
        {
            EnsureFolders();

            if (!Directory.Exists(GeneratedCharactersRoot))
            {
                Debug.LogWarning($"[KyberKlash][LudoOrganizer] No generated character folder found at {GeneratedCharactersRoot}.");
                return;
            }

            EnsureMissingFormAssets();
            List<FormSO> allForms = LoadAllForms();
            int processed = 0;

            foreach (string sourceDirectory in Directory.GetDirectories(GeneratedCharactersRoot))
            {
                string slug = Path.GetFileName(sourceDirectory);
                if (string.IsNullOrWhiteSpace(slug))
                {
                    continue;
                }

                string keyArtSource = FindSource(sourceDirectory, $"{slug}_key_art");
                string sheetSource = FindSource(sourceDirectory, $"{slug}_animation_spritesheet");
                if (string.IsNullOrEmpty(keyArtSource) && string.IsNullOrEmpty(sheetSource))
                {
                    continue;
                }

                ProcessCharacter(slug, keyArtSource, sheetSource, allForms);
                processed++;
            }

            AssignGeneratedCharactersToGameManagers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[KyberKlash][LudoOrganizer] Organized {processed} generated Ludo character asset sets.");
        }

        [InitializeOnLoadMethod]
        private static void RunQueuedOrganizerAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoRunFlagPath))
                {
                    return;
                }

                File.Delete(AutoRunFlagPath);
                OrganizeGeneratedCharacterAssets();
            };
        }

        private static void ProcessCharacter(string slug, string keyArtSource, string sheetSource, IReadOnlyList<FormSO> allForms)
        {
            string artDirectory = $"{CharacterArtRoot}/{slug}";
            string portraitDirectory = $"{artDirectory}/Portraits";
            string spriteDirectory = $"{artDirectory}/Sprites";
            string animationDirectory = $"{CharacterAnimationRoot}/{slug}";

            EnsureFolder(artDirectory);
            EnsureFolder(portraitDirectory);
            EnsureFolder(spriteDirectory);
            EnsureFolder(animationDirectory);

            Sprite portraitSprite = null;
            if (!string.IsNullOrEmpty(keyArtSource))
            {
                string portraitPath = $"{portraitDirectory}/{slug}_portrait.png";
                ConvertImageToPngAsset(keyArtSource, portraitPath);
                ConfigureSingleSprite(portraitPath, 256);
                portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            }

            Sprite[] sprites = Array.Empty<Sprite>();
            if (!string.IsNullOrEmpty(sheetSource))
            {
                string sheetPath = $"{spriteDirectory}/{slug}_animation_spritesheet.png";
                ConvertImageToPngAsset(sheetSource, sheetPath);
                ConfigureSpriteSheet(sheetPath, SpriteSheetColumns, SpriteSheetRows, 256);
                sprites = LoadSprites(sheetPath);
            }

            RuntimeAnimatorController controller = null;
            if (sprites.Length > 0)
            {
                string controllerPath = $"{animationDirectory}/{slug}.controller";
                controller = CreateAnimatorController(slug, sprites, animationDirectory, controllerPath);
            }

            GameObject visualPrefab = CreateVisualPrefab(slug, sprites.FirstOrDefault() != null ? sprites[0] : portraitSprite);
            CreateOrUpdateCharacterData(slug, portraitSprite, controller, visualPrefab, allForms);
        }

        private static RuntimeAnimatorController CreateAnimatorController(string slug, Sprite[] sprites, string animationDirectory, string controllerPath)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = null;

            foreach (string stateName in RuntimeStateNames)
            {
                string clipPath = $"{animationDirectory}/{slug}_{stateName}.anim";
                AnimationClip clip = CreateSpriteClip(clipPath, stateName, GetSpritesForState(stateName, sprites));
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clip;

                if (stateName == "Idle")
                {
                    idleState = state;
                }
            }

            string previewClipPath = $"{animationDirectory}/{slug}_LudoPreview.anim";
            AnimationClip previewClip = CreatePreviewClip(previewClipPath, sprites);
            AnimatorState previewState = stateMachine.AddState("LudoPreview");
            previewState.motion = previewClip;

            if (idleState != null)
            {
                stateMachine.defaultState = idleState;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip CreateSpriteClip(string clipPath, string clipName, Sprite[] stateSprites)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            {
                AssetDatabase.DeleteAsset(clipPath);
            }

            AnimationClip clip = new AnimationClip
            {
                name = clipName,
                frameRate = 12f
            };

            if (stateSprites == null || stateSprites.Length == 0)
            {
                stateSprites = new Sprite[] { null };
            }

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[stateSprites.Length];
            for (int i = 0; i < stateSprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = stateSprites[i]
                };
            }

            SetSpriteCurve(clip, keyframes);

            if (stateSprites.Length > 1)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = clipName == "Idle" || clipName == "Move" || clipName == "Fall";
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static AnimationClip CreatePreviewClip(string clipPath, Sprite[] sprites)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            {
                AssetDatabase.DeleteAsset(clipPath);
            }

            AnimationClip clip = new AnimationClip
            {
                name = "LudoPreview",
                frameRate = 12f
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }

            SetSpriteCurve(clip, keyframes);
            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static void SetSpriteCurve(AnimationClip clip, ObjectReferenceKeyframe[] keyframes)
        {
            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = VisualChildName,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        }

        private static GameObject CreateVisualPrefab(string slug, Sprite sprite)
        {
            string prefabPath = $"{CharacterPrefabRoot}/{slug}_Visual.prefab";
            GameObject root = new GameObject(VisualChildName);
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;
            root.transform.localPosition = new Vector3(0f, 1f, 0f);
            root.transform.localScale = Vector3.one * 1.8f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateOrUpdateCharacterData(string slug, Sprite portraitSprite, RuntimeAnimatorController controller, GameObject visualPrefab, IReadOnlyList<FormSO> allForms)
        {
            string path = $"{GeneratedCharacterDataRoot}/Character_{ToPascalCase(slug)}.asset";
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.characterId = slug;
            data.displayName = ToDisplayName(slug);
            data.characterDescription = $"Generated Ludo AI fighter art and animation set for {ToDisplayName(slug)}.";
            data.characterPortrait = portraitSprite;
            data.characterIcon = portraitSprite;
            data.animatorController = controller;
            data.characterModelPrefab = visualPrefab;

            FormSO defaultForm = FindDefaultForm(slug, allForms);
            data.defaultForm = defaultForm;
            data.availableForms = allForms.Where(form => form != null).ToArray();

            data.defaultLightAttack = AssetDatabase.LoadAssetAtPath<AttackSO>("Assets/_Project/Data/Attacks/Attack_Light.asset");
            data.defaultHeavyAttack = AssetDatabase.LoadAssetAtPath<AttackSO>("Assets/_Project/Data/Attacks/Attack_Heavy.asset");
            data.defaultUpAerial = AssetDatabase.LoadAssetAtPath<AttackSO>("Assets/_Project/Data/Attacks/Attack_UpAir.asset");
            data.defaultDownAerial = AssetDatabase.LoadAssetAtPath<AttackSO>("Assets/_Project/Data/Attacks/Attack_DownAir.asset");
            data.defaultNeutralSpecial = AssetDatabase.LoadAssetAtPath<AttackSO>("Assets/_Project/Data/Attacks/Attack_NeutralSpecial.asset");

            EditorUtility.SetDirty(data);
        }

        private static void ConvertImageToPngAsset(string sourcePath, string destinationPath)
        {
            byte[] sourceBytes = File.ReadAllBytes(sourcePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(sourceBytes))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                WebP.Error error;
                texture = Texture2DExt.CreateTexture2DFromWebP(sourceBytes, false, false, out error, null, false);
                if (texture == null || error != WebP.Error.Success)
                {
                    throw new InvalidDataException($"Could not decode generated image {sourcePath}. WebP decoder returned {error}.");
                }
            }

            byte[] pngBytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            File.WriteAllBytes(destinationPath, pngBytes);
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureSingleSprite(string assetPath, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureSpriteSheet(string assetPath, int columns, int rows, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }

            if (texture == null)
            {
                return;
            }

            float cellWidth = texture.width / (float)columns;
            float cellHeight = texture.height / (float)rows;
            var spriteMetaData = new List<SpriteMetaData>(columns * rows);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    float x = Mathf.Floor(column * cellWidth);
                    float nextX = column == columns - 1 ? texture.width : Mathf.Floor((column + 1) * cellWidth);
                    float y = Mathf.Floor(texture.height - ((row + 1) * cellHeight));
                    float nextY = row == 0 ? texture.height : Mathf.Floor(texture.height - (row * cellHeight));

                    spriteMetaData.Add(new SpriteMetaData
                    {
                        name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{index:00}",
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        rect = new Rect(x, y, Mathf.Max(1f, nextX - x), Mathf.Max(1f, nextY - y))
                    });
                }
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritesheet = spriteMetaData.ToArray();
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadSprites(string assetPath)
        {
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static string FindSource(string sourceDirectory, string stem)
        {
            string[] preferredExtensions = { ".webp", ".png", ".jpg", ".jpeg" };
            foreach (string extension in preferredExtensions)
            {
                string path = Path.Combine(sourceDirectory, stem + extension).Replace("\\", "/");
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return Directory.GetFiles(sourceDirectory, stem + ".*")
                .Select(path => path.Replace("\\", "/"))
                .FirstOrDefault(path => preferredExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()));
        }

        private static Sprite[] GetSpritesForState(string stateName, Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return Array.Empty<Sprite>();
            }

            int[] preferredIndices = stateName switch
            {
                "Idle" => new[] { 0, 1 },
                "Move" => new[] { 2, 3, 4, 5 },
                "Jump" => new[] { 6, 7 },
                "DoubleJump" => new[] { 6, 8 },
                "Fall" => new[] { 7, 8 },
                "Dash" => new[] { 2, 3, 4, 5 },
                "AirDash" => new[] { 15, 16 },
                "LightAttack" => new[] { 9, 10 },
                "Attack" => new[] { 9, 10 },
                "HeavyAttack" => new[] { 11, 12 },
                "UpAir" => new[] { 17, 18 },
                "DownAir" => new[] { 19, 20 },
                "ForwardAir" => new[] { 9, 10 },
                "BackAir" => new[] { 11, 12 },
                "Special" => new[] { 15, 16 },
                "NeutralSpecial" => new[] { 15, 16 },
                "SideSpecial" => new[] { 15, 16 },
                "UpSpecial" => new[] { 17, 18 },
                "DownSpecial" => new[] { 19, 20 },
                "Block" => new[] { 13, 14 },
                "Parry" => new[] { 13, 14 },
                "HitStun" => new[] { 21, 22 },
                "Knockback" => new[] { 21, 22 },
                "Launch" => new[] { 21, 22, 23 },
                "Death" => new[] { 23 },
                "Respawn" => new[] { 0 },
                "SaberOpen" => new[] { 0, 1 },
                "SaberClose" => new[] { 1, 0 },
                _ => new[] { 0 }
            };

            return preferredIndices
                .Select(index => sprites[Mathf.Clamp(index, 0, sprites.Length - 1)])
                .Where(sprite => sprite != null)
                .ToArray();
        }

        private static void EnsureMissingFormAssets()
        {
            EnsureForm("Form_ShienDjemSo.asset", "form_shien_djem_so", "Form V: Shien/Djem So", FormSO.FormMechanicType.PowerCounter, new Color(0.1f, 0.45f, 1f));
            EnsureForm("Form_Niman.asset", "form_niman", "Form VI: Niman", FormSO.FormMechanicType.ForceUtility, new Color(0.25f, 0.8f, 1f));
            EnsureForm("Form_JuyoVaapad.asset", "form_juyo_vaapad", "Form VII: Juyo/Vaapad", FormSO.FormMechanicType.BerserkerTrance, new Color(0.85f, 0.05f, 0.95f));
        }

        private static void EnsureForm(string fileName, string formId, string displayName, FormSO.FormMechanicType mechanicType, Color saberColor)
        {
            string path = $"{FormDataRoot}/{fileName}";
            FormSO form = AssetDatabase.LoadAssetAtPath<FormSO>(path);
            if (form == null)
            {
                form = ScriptableObject.CreateInstance<FormSO>();
                AssetDatabase.CreateAsset(form, path);
            }

            form.formId = formId;
            form.displayName = displayName;
            form.loreDescription = $"Generated gameplay form data for {displayName}.";
            form.mechanicType = mechanicType;
            form.saberColor = saberColor;
            form.saberCoreColor = Color.white;
            if (form.mechanicData == null)
            {
                form.mechanicData = new FormSO.FormMechanicData();
            }

            EditorUtility.SetDirty(form);
        }

        private static List<FormSO> LoadAllForms()
        {
            return AssetDatabase.FindAssets("t:FormSO", new[] { FormDataRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FormSO>)
                .Where(form => form != null)
                .OrderBy(form => form.displayName, StringComparer.Ordinal)
                .ToList();
        }

        private static FormSO FindDefaultForm(string slug, IReadOnlyList<FormSO> forms)
        {
            string normalizedSlug = Normalize(slug);

            return forms.FirstOrDefault(form => normalizedSlug.Contains(Normalize(form.formId)))
                ?? forms.FirstOrDefault(form => normalizedSlug.Contains("shii") && Normalize(form.displayName).Contains("shiicho"))
                ?? forms.FirstOrDefault(form => normalizedSlug.Contains("makashi") && Normalize(form.displayName).Contains("makashi"))
                ?? forms.FirstOrDefault(form => normalizedSlug.Contains("soresu") && Normalize(form.displayName).Contains("soresu"))
                ?? forms.FirstOrDefault(form => normalizedSlug.Contains("ataru") && Normalize(form.displayName).Contains("ataru"))
                ?? forms.FirstOrDefault(form => normalizedSlug.Contains("djem") && Normalize(form.displayName).Contains("djem"))
                ?? forms.FirstOrDefault(form => normalizedSlug.Contains("niman") && Normalize(form.displayName).Contains("niman"))
                ?? forms.FirstOrDefault(form => normalizedSlug.Contains("juyo") && Normalize(form.displayName).Contains("juyo"))
                ?? forms.FirstOrDefault();
        }

        private static void AssignGeneratedCharactersToGameManagers()
        {
            List<CharacterData> generatedCharacters = AssetDatabase.FindAssets("t:CharacterData", new[] { GeneratedCharacterDataRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CharacterData>)
                .Where(character => character != null)
                .OrderBy(character => character.displayName, StringComparer.Ordinal)
                .ToList();

            if (generatedCharacters.Count == 0)
            {
                return;
            }

            GameManager prefabManager = LoadPrefabComponent<GameManager>("Assets/_Project/Prefabs/GameManager.prefab");
            if (prefabManager != null)
            {
                AssignCharacters(prefabManager, generatedCharacters);
                PrefabUtility.SavePrefabAsset(prefabManager.gameObject);
            }

            foreach (GameManager manager in Resources.FindObjectsOfTypeAll<GameManager>())
            {
                if (manager == null || EditorUtility.IsPersistent(manager))
                {
                    continue;
                }

                AssignCharacters(manager, generatedCharacters);
                if (manager.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                }
            }
        }

        private static void AssignCharacters(GameManager manager, IReadOnlyList<CharacterData> generatedCharacters)
        {
            var serializedObject = new SerializedObject(manager);
            SerializedProperty property = serializedObject.FindProperty("availableCharacters");
            if (property == null)
            {
                return;
            }

            property.arraySize = generatedCharacters.Count;
            for (int i = 0; i < generatedCharacters.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = generatedCharacters[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static T LoadPrefabComponent<T>(string prefabPath) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(value ?? string.Empty, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
        }

        private static string ToDisplayName(string slug)
        {
            string value = Regex.Replace(slug, "[-_]+", " ");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
        }

        private static string ToPascalCase(string slug)
        {
            string display = ToDisplayName(slug);
            return Regex.Replace(display, "[^a-zA-Z0-9]", string.Empty);
        }

        private static void EnsureFolders()
        {
            EnsureFolder(CharacterArtRoot);
            EnsureFolder(CharacterAnimationRoot);
            EnsureFolder(CharacterPrefabRoot);
            EnsureFolder(GeneratedCharacterDataRoot);
            EnsureFolder(FormDataRoot);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
