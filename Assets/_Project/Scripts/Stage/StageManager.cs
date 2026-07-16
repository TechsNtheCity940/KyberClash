using UnityEngine;
using KyberKlash.Data;
using KyberKlash.Combat;
using UnityEngine.Rendering;
using System.Linq;

namespace KyberKlash.Stage
{
    /// <summary>
    /// Manages the current stage, blast zones, and hazards.
    /// Singleton for easy access from players.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Current Stage")]
        [SerializeField] private StageData currentStageData;
        [SerializeField] private GameObject stageInstance;

        [Header("Blast Zone Visualization")]
        [SerializeField] private bool showBlastZones = true;
        [SerializeField] private Material blastZoneMaterial;

        private HazardZone[] activeHazards;
        private Transform[] ledgePoints = new Transform[0];
        private Transform platformRoot;
        private Transform backgroundRoot;

        public StageData CurrentStageData => currentStageData;
        public GameObject StageInstance => stageInstance;
        public Transform[] LedgePoints => ledgePoints;

        public event System.Action<StageData> OnStageLoaded;
        public event System.Action OnStageUnloaded;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void Start()
        {
            if (currentStageData != null)
            {
                if (currentStageData.stagePrefab != null)
                {
                    DisableEmbeddedStageChildren();
                    LoadStage(currentStageData);
                }
                else if (stageInstance == null && transform.childCount > 0)
                {
                    stageInstance = gameObject;
                    RepairStageVisuals(stageInstance);
                    BuildBackground(stageInstance, currentStageData);
                    ThemePlatforms(stageInstance);
                    activeHazards = GetComponentsInChildren<HazardZone>();
                    if (showBlastZones)
                    {
                        CreateBlastZoneVisuals();
                    }
                    OnStageLoaded?.Invoke(currentStageData);
                }
                else
                {
                    LoadStage(currentStageData);
                }
            }
        }

        /// <summary>
        /// Load a new stage
        /// </summary>
        public void LoadStage(StageData stageData)
        {
            if (stageData == null) return;

            // Unload current stage
            UnloadCurrentStage();

            currentStageData = stageData;

            // Instantiate stage prefab
            if (stageData.stagePrefab != null)
            {
                DisableEmbeddedStageChildren();
                stageInstance = Instantiate(stageData.stagePrefab);
                stageInstance.name = $"Stage_{stageData.stageName}";
                RepairStageVisuals(stageInstance);
                // Smash-style vertical backdrop (fixes horizontal "floor" backgrounds)
                // plus flat Star Wars-themed platform materials.
                BuildBackground(stageInstance, stageData);
                ThemePlatforms(stageInstance);
                // GUARANTEE a solid main platform exists under the spawn area so players
                // never fall through into the blast zone (the authored prefabs can be empty
                // or use the legacy default material that URP renders magenta).
                EnsureMainPlatform(stageInstance, stageData);
            }
            else
            {
                stageInstance = gameObject;
                RepairStageVisuals(stageInstance);
                BuildBackground(stageInstance, stageData);
                ThemePlatforms(stageInstance);
                EnsureMainPlatform(stageInstance, stageData);
            }

            // Get hazards
            activeHazards = stageInstance?.GetComponentsInChildren<HazardZone>() ?? new HazardZone[0];

            // Build data-driven platforms + ledges from StageData
            BuildPlatformsAndLedges(currentStageData);

            // Setup blast zone visualization
            if (showBlastZones)
            {
                CreateBlastZoneVisuals();
            }

            // Play stage music
            if (stageData.stageMusic != null)
            {
                // AudioManager.Instance.PlayMusic(stageData.stageMusic, stageData.musicVolume);
            }

            OnStageLoaded?.Invoke(stageData);
        }

        /// <summary>
        /// Unload current stage
        /// </summary>
        public void UnloadCurrentStage()
        {
            if (platformRoot != null)
            {
                Destroy(platformRoot.gameObject);
                platformRoot = null;
            }
            if (backgroundRoot != null)
            {
                Destroy(backgroundRoot.gameObject);
                backgroundRoot = null;
            }
            ledgePoints = new Transform[0];

            if (stageInstance != null)
            {
                if (stageInstance != gameObject)
                {
                    Destroy(stageInstance);
                }
                stageInstance = null;
            }

            OnStageUnloaded?.Invoke();
        }

        private void DisableEmbeddedStageChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (stageInstance != null && child.gameObject == stageInstance)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Create visual blast zone indicators
        /// </summary>
        private void CreateBlastZoneVisuals()
        {
            if (currentStageData == null) return;

            // Left blast zone
            CreateBlastZonePlane("LeftBlastZone",
                new Vector3(currentStageData.leftBlastZone, 0f, 0f),
                Vector3.forward * 20f, Vector3.up * 30f);

            // Right blast zone
            CreateBlastZonePlane("RightBlastZone",
                new Vector3(currentStageData.rightBlastZone, 0f, 0f),
                Vector3.forward * 20f, Vector3.up * 30f);

            // Top blast zone
            CreateBlastZonePlane("TopBlastZone",
                new Vector3(0f, currentStageData.topBlastZone, 0f),
                Vector3.right * 40f, Vector3.forward * 20f);

            // Bottom blast zone
            CreateBlastZonePlane("BottomBlastZone",
                new Vector3(0f, currentStageData.bottomBlastZone, 0f),
                Vector3.right * 40f, Vector3.forward * 20f);
        }

        /// <summary>
        /// Build soft platforms and ledge anchors from StageData (Smash-style Battlefield/FD/Omega).
        /// FinalDestination and Omega layouts ignore the platforms array (flat single stage).
        /// </summary>
        private void BuildPlatformsAndLedges(StageData data)
        {
            if (platformRoot != null)
            {
                Destroy(platformRoot.gameObject);
                platformRoot = null;
            }
            ledgePoints = new Transform[0];

            if (data == null) return;

            bool usePlatforms = data.layoutType == StageLayoutType.Battlefield ||
                                data.layoutType == StageLayoutType.Custom;

            if (!usePlatforms) return; // FinalDestination / Omega are flat

            // Build from authored data first.
            if (data.platforms != null && data.platforms.Length > 0)
            {
                BuildPlatformsFromData(data);
            }
            else if (stageInstance != null)
            {
                // Fallback: auto-discover platform children already placed in the stage
                // (e.g. the prototype arena's Platform_Left/Right/Top) and treat them as
                // soft, pass-through platforms with ledge anchors. Keeps the Battlefield
                // layout data-driven without hand-editing the ScriptableObject.
                AutoDiscoverPlatforms(stageInstance.transform);
            }
        }

        private GameObject CreateSoftPlatform(string name)
        {
            var go = new GameObject(name);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var mr = go.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = new Color(0.30f, 0.42f, 0.52f, 1f) };
            mr.material = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private Transform CreateLedgeAnchor(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("LedgeAnchor");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<LedgeAnchor>();
            return go.transform;
        }

        /// <summary>
        /// Build soft platforms from authored StagePlatform[] data.
        /// </summary>
        private void BuildPlatformsFromData(StageData data)
        {
            platformRoot = new GameObject("SoftPlatforms").transform;
            platformRoot.SetParent(stageInstance?.transform ?? transform, false);

            var ledges = new System.Collections.Generic.List<Transform>();

            for (int i = 0; i < data.platforms.Length; i++)
            {
                var p = data.platforms[i];
                GameObject plat = p.platformPrefab != null
                    ? Instantiate(p.platformPrefab, platformRoot)
                    : CreateSoftPlatform($"SoftPlatform_{i}");
                plat.name = string.IsNullOrEmpty(p.platformName) ? $"SoftPlatform_{i}" : p.platformName;
                plat.transform.SetParent(platformRoot, false);
                plat.transform.localPosition = p.position;
                plat.transform.localScale = p.size;

                var col = plat.GetComponent<Collider>();
                if (col == null) plat.AddComponent<BoxCollider>();
                if (plat.GetComponent<SoftPlatform>() == null)
                {
                    var sp = plat.AddComponent<SoftPlatform>();
                    sp.dropThrough = p.dropThrough;
                }

                ledges.Add(CreateLedgeAnchor(plat.transform, p.position + new Vector3(-p.size.x * 0.5f, p.size.y * 0.5f, 0f)));
                ledges.Add(CreateLedgeAnchor(plat.transform, p.position + new Vector3(p.size.x * 0.5f, p.size.y * 0.5f, 0f)));
            }

            if (data.ledgeAnchors != null && data.ledgeAnchors.Length > 0)
            {
                ledges.AddRange(System.Array.ConvertAll(data.ledgeAnchors, t => t));
            }

            ledgePoints = ledges.ToArray();
        }

        /// <summary>
        /// Auto-discover platform children already placed in the stage instance (Battlefield
        /// fallback). Marks them soft/pass-through and adds ledge anchors at their edges.
        /// </summary>
        private void AutoDiscoverPlatforms(Transform stageRoot)
        {
            platformRoot = new GameObject("SoftPlatforms").transform;
            platformRoot.SetParent(stageRoot, false);

            var ledges = new System.Collections.Generic.List<Transform>();
            int discovered = 0;

            // A platform is any direct child with a collider (ignore the main ground/walls
            // by skipping very wide/long pieces and the hazard zone).
            foreach (Transform child in stageRoot)
            {
                if (child == platformRoot) continue;
                var col = child.GetComponent<Collider>();
                if (col == null) continue;
                if (child.GetComponent<HazardZone>() != null) continue;
                if (child.GetComponent<SoftPlatform>() != null) continue; // already handled

                // Heuristic: platforms are relatively small/thin vs the main ground plane.
                Vector3 size = col.bounds.size;
                if (size.y > 3f) continue; // tall walls, not platforms

                var sp = child.gameObject.AddComponent<SoftPlatform>();
                sp.dropThrough = true;

                // Ledge anchors at left/right edges (world-space, parented to the platform).
                Vector3 half = size * 0.5f;
                ledges.Add(CreateLedgeAnchor(child, new Vector3(-half.x, half.y, 0f)));
                ledges.Add(CreateLedgeAnchor(child, new Vector3(half.x, half.y, 0f)));
                discovered++;
            }

            // Also pick up any ledges already authored in the scene.
            ledges.AddRange(stageRoot.GetComponentsInChildren<LedgeAnchor>().Select(a => a.transform));

            ledgePoints = ledges.ToArray();
            if (discovered > 0) Debug.Log($"[StageManager] Auto-discovered {discovered} soft platforms.");
        }

        /// <summary>
        /// Find the nearest ledge a fighter can grab given a position and facing.
        /// Returns false if none in range.
        /// </summary>
        public bool TryGetNearestLedge(Vector3 position, out Transform ledge, float maxDistance = 2.5f)
        {
            ledge = null;
            if (ledgePoints == null || ledgePoints.Length == 0) return false;

            float best = maxDistance * maxDistance;
            bool found = false;
            foreach (var l in ledgePoints)
            {
                if (l == null) continue;
                float d = (l.position - position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    ledge = l;
                    found = true;
                }
            }
            return found;
        }

        private void CreateBlastZonePlane(string name, Vector3 position, Vector3 sizeX, Vector3 sizeY)
        {
            GameObject plane = new GameObject(name);
            plane.transform.SetParent(stageInstance?.transform ?? transform);
            plane.transform.position = position;

            var meshFilter = plane.AddComponent<MeshFilter>();
            var meshRenderer = plane.AddComponent<MeshRenderer>();

            // Create quad mesh
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[4];
            vertices[0] = -sizeX * 0.5f - sizeY * 0.5f;
            vertices[1] = sizeX * 0.5f - sizeY * 0.5f;
            vertices[2] = -sizeX * 0.5f + sizeY * 0.5f;
            vertices[3] = sizeX * 0.5f + sizeY * 0.5f;
            mesh.vertices = vertices;
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.normals = new Vector3[4] { Vector3.left, Vector3.left, Vector3.left, Vector3.left };
            mesh.uv = new Vector2[4] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            meshFilter.mesh = mesh;

            // Material
            if (blastZoneMaterial != null)
            {
                meshRenderer.material = blastZoneMaterial;
            }
            else
            {
                // Create default translucent material
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);
                mat.SetFloat("_ZWrite", 0);
                mat.SetFloat("_SrcBlend", 5);
                mat.SetFloat("_DstBlend", 10);
                mat.color = new Color(1f, 0f, 0f, 0.1f);
                meshRenderer.material = mat;
            }

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static void RepairStageVisuals(GameObject root)
        {
            if (root == null) return;

            MeshRenderer[] meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer renderer = meshRenderers[i];
                if (renderer == null) continue;

                bool isBackground = renderer.name.Contains("Background");
                Material source = renderer.sharedMaterial;
                if (!NeedsReplacement(source))
                {
                    continue;
                }

                Texture texture = FindMainTexture(source);
                renderer.sharedMaterial = isBackground && texture != null
                    ? CreateRuntimeBackgroundMaterial(texture)
                    : CreateRuntimePlatformMaterial(renderer.transform.GetSiblingIndex());

                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = !isBackground;
            }
        }

        private static bool NeedsReplacement(Material material)
        {
            if (material == null || material.shader == null)
            {
                return true;
            }

            string shaderName = material.shader.name;
            if (shaderName == "Hidden/InternalErrorShader")
            {
                return true;
            }

            return GraphicsSettings.currentRenderPipeline == null &&
                   shaderName.StartsWith("Universal Render Pipeline/", System.StringComparison.Ordinal);
        }

        private static Texture FindMainTexture(Material material)
        {
            if (material == null) return null;
            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null) return material.GetTexture("_MainTex");
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null) return material.GetTexture("_BaseMap");
            return null;
        }

        private static Material CreateRuntimeBackgroundMaterial(Texture texture)
        {
            Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = "Runtime_ArenaBackground"
            };

            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            return material;
        }

        private static Material CreateRuntimePlatformMaterial(int index)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            Material material = new Material(shader)
            {
                name = "Runtime_ArenaPlatform"
            };

            Color[] palette =
            {
                new Color(0.20f, 0.25f, 0.33f, 1f),
                new Color(0.35f, 0.30f, 0.46f, 1f),
                new Color(0.24f, 0.40f, 0.35f, 1f),
                new Color(0.48f, 0.36f, 0.22f, 1f),
                new Color(0.26f, 0.34f, 0.52f, 1f)
            };

            Color color = palette[Mathf.Abs(index) % palette.Length];
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            return material;
        }

        /// <summary>
        /// Guarantee a solid main platform under the spawn area. If the stage already
        /// contains a wide low platform we leave it; otherwise we add one so fighters
        /// always have ground to land on. Uses a real URP material (never the legacy
        /// default that renders magenta under URP).
        /// </summary>
        private static void EnsureMainPlatform(GameObject stageRoot, StageData data)
        {
            if (stageRoot == null || data == null) return;

            // Already have a wide, low platform? Then we're fine.
            foreach (Transform child in stageRoot.transform)
            {
                var col = child.GetComponent<BoxCollider>();
                if (col == null) continue;
                Vector3 size = col.bounds.size;
                if (size.x >= (data.rightBlastZone - data.leftBlastZone) * 0.5f && size.y <= 3f)
                {
                    return; // main platform present
                }
            }

            float width = (data.rightBlastZone - data.leftBlastZone) * 0.8f;
            float centerY = data.bottomBlastZone + 2f;

            var go = new GameObject("MainPlatform");
            go.transform.SetParent(stageRoot.transform, false);
            go.transform.localPosition = new Vector3(0f, centerY, 0f);
            go.transform.localScale = new Vector3(width, 1f, 3f);

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var mr = go.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "Runtime_MainPlatform" };
            Color c = new Color(0.30f, 0.33f, 0.38f, 1f);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);
            mr.material = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            var bc = go.AddComponent<BoxCollider>();
            bc.size = Vector3.one;
            bc.center = Vector3.zero;

            Debug.Log("[StageManager] Added guaranteed MainPlatform (no usable main platform found in stage).");
        }

        // --- Smash-style stage presentation -----------------------------------------

        /// <summary>
        /// Rebuilds the stage backdrop as a vertical billboard behind the fighters (Smash
        /// convention) plus a far parallax layer for 3D depth. The authored stages shipped
        /// with horizontal "floor" planes that read as a top-down angle from the side camera;
        /// this forces the correct vertical orientation and applies the themed texture.
        /// </summary>
        private void BuildBackground(GameObject stageRoot, StageData stageData)
        {
            if (stageRoot == null || stageData == null) return;

            backgroundRoot = new GameObject("BackgroundRoot").transform;
            backgroundRoot.SetParent(stageRoot.transform, false);

            string theme = PickBackgroundTheme(stageData.stageName);
            Texture2D tex = Resources.Load<Texture2D>("Backgrounds/" + theme);
            Texture2D far = Resources.Load<Texture2D>("Backgrounds/bg_far_parallax_nebula");

            // Near themed backdrop (vertical, facing the side camera at -Z).
            var near = CreateBackdropPlane("Backdrop_Near", tex, 64f, 34f);
            near.SetParent(backgroundRoot, false);
            near.localPosition = new Vector3(0f, 8f, 16f);
            if (tex == null) PaintBackdropGradient(near);

            // Far parallax layer (subtler, deeper) for depth.
            if (far != null)
            {
                var farPlane = CreateBackdropPlane("Backdrop_Far", far, 90f, 48f);
                farPlane.SetParent(backgroundRoot, false);
                farPlane.localPosition = new Vector3(0f, 10f, 26f);
                // Darken/emphasize depth.
                foreach (var r in farPlane.GetComponentsInChildren<Renderer>())
                {
                    if (r != null) r.material.color = new Color(0.5f, 0.5f, 0.6f);
                }
            }

            // If the authored stage contained a horizontal "Background" plane, neutralize it
            // so it no longer renders as a top-down floor (the new billboards replace it).
            foreach (Transform child in stageRoot.transform)
            {
                if (child != null && child.name.Contains("Background") && child != backgroundRoot)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private Transform CreateBackdropPlane(string name, Texture2D tex, float w, float h)
        {
            var go = new GameObject(name);
            var mf = go.AddComponent<MeshFilter>();
            // Vertical quad in the XY plane (faces +Z toward the side camera).
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var mr = go.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "Runtime_" + name };
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 0f); // Opaque
            mr.material = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = -10;
            go.transform.localScale = new Vector3(w, h, 1f);
            return go.transform;
        }

        /// <summary>
        /// Paint a procedural kyber-void gradient onto a backdrop that failed to load a
        /// texture, so the stage always has a visible background (never blank/black).
        /// </summary>
        private static void PaintBackdropGradient(Transform plane)
        {
            if (plane == null) return;
            var mr = plane.GetComponent<MeshRenderer>();
            if (mr == null) return;

            int res = 256;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            Color top = new Color(0.02f, 0.03f, 0.10f);    // deep void navy
            Color mid = new Color(0.04f, 0.16f, 0.22f);    // kyber teal
            Color bottom = new Color(0.10f, 0.02f, 0.16f);  // magenta horizon
            for (int y = 0; y < res; y++)
            {
                float t = y / (float)(res - 1);
                Color c = t < 0.5f
                    ? Color.Lerp(bottom, mid, t * 2f)
                    : Color.Lerp(mid, top, (t - 0.5f) * 2f);
                for (int x = 0; x < res; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "Runtime_BackdropGradient" };
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 0f);
            mr.material = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = -10;
        }

        /// <summary>
        /// Map a stage name to its Star Wars backdrop resource (falls back to cloud city).
        /// </summary>
        private static string PickBackgroundTheme(string stageName)
        {
            if (string.IsNullOrEmpty(stageName)) return "bg_cloud_city_bespin";
            string s = stageName.ToLowerInvariant();
            if (s.Contains("cloud")) return "bg_cloud_city_bespin";
            if (s.Contains("jedi") || s.Contains("archive") || s.Contains("observatory") || s.Contains("coruscant")) return "bg_jedi_temple_coruscant";
            if (s.Contains("forest") || s.Contains("moon") || s.Contains("endor")) return "bg_forest_moon_endor";
            if (s.Contains("sith") || s.Contains("foundry") || s.Contains("crucible")) return "bg_sith_foundry";
            if (s.Contains("neon") || s.Contains("underworld") || s.Contains("dock")) return "bg_neon_underworld";
            if (s.Contains("star_destroyer") || s.Contains("debris")) return "bg_star_destroyer_debris";
            if (s.Contains("kyber") || s.Contains("temple")) return "bg_kyber_temple";
            return "bg_cloud_city_bespin";
        }

        /// <summary>
        /// Give platforms/ground/walls a flat, matte Star Wars material so they read cleanly
        /// from the side camera (no shiny surfaces, subtle theme tint).
        /// </summary>
        private static void ThemePlatforms(GameObject stageRoot)
        {
            if (stageRoot == null) return;

            // Star Wars duracrete / hull palette (matte).
            Color[] palette =
            {
                new Color(0.32f, 0.33f, 0.36f), // gunmetal grey
                new Color(0.40f, 0.36f, 0.30f), // duracrete tan
                new Color(0.26f, 0.28f, 0.33f), // blue-grey hull
                new Color(0.36f, 0.30f, 0.28f), // rusted metal
                new Color(0.30f, 0.34f, 0.32f)  // olive durasteel
            };

            int i = 0;
            foreach (var r in stageRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null) continue;
                string n = r.name;
                // Skip the backdrop we just built and any background.
                if (n.Contains("Backdrop") || n.Contains("Background")) continue;
                // Only theme structural pieces (platforms / ground / walls), not characters/VFX.
                if (!(n.Contains("Platform") || n.Contains("Ground") || n.Contains("Wall") || n.Contains("Hazard"))) continue;

                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader) { name = "Runtime_StageMat_" + i };
                Color c = palette[i % palette.Length];
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);
                r.material = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                i++;
            }
        }

        /// <summary>
        /// Check if position is out of bounds (ring out)
        /// </summary>
        public bool IsOutOfBounds(Vector3 position)
        {
            return currentStageData?.IsOutOfBounds(position) ?? false;
        }

        /// <summary>
        /// Get nearest blast zone direction
        /// </summary>
        public Vector3 GetBlastZoneDirection(Vector3 position)
        {
            return currentStageData?.GetBlastZoneDirection(position) ?? Vector3.down;
        }

        /// <summary>
        /// Activate a specific hazard by name
        /// </summary>
        public void ActivateHazard(string hazardName, float duration = -1f)
        {
            if (activeHazards == null) return;

            foreach (var hazard in activeHazards)
            {
                if (hazard.name.Contains(hazardName))
                {
                    hazard.ForceActivate(duration);
                    break;
                }
            }
        }

        /// <summary>
        /// Enable/disable all hazards
        /// </summary>
        public void SetHazardsEnabled(bool enabled)
        {
            if (activeHazards == null) return;

            foreach (var hazard in activeHazards)
            {
                hazard.enabled = enabled;
            }
        }

        /// <summary>
        /// Get spawn points for players
        /// </summary>
        public Transform[] GetSpawnPoints()
        {
            if (stageInstance == null) return new Transform[0];

            var spawnPoints = stageInstance.GetComponentsInChildren<SpawnPoint>();
            System.Array.Sort(spawnPoints, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

            Transform[] points = new Transform[spawnPoints.Length];
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                points[i] = spawnPoints[i].transform;
            }
            return points;
        }

        protected virtual void OnDrawGizmos()
        {
            if (currentStageData == null || !showBlastZones) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            // Draw blast zone boxes
            Vector3 center = new Vector3(
                (currentStageData.leftBlastZone + currentStageData.rightBlastZone) * 0.5f,
                (currentStageData.bottomBlastZone + currentStageData.topBlastZone) * 0.5f,
                0f
            );
            Vector3 size = new Vector3(
                currentStageData.rightBlastZone - currentStageData.leftBlastZone,
                currentStageData.topBlastZone - currentStageData.bottomBlastZone,
                20f
            );

            Gizmos.DrawWireCube(center, size);
        }
    }

    /// <summary>
    /// Spawn point component for player positioning
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] public int playerIndex = 0;

        public int PlayerIndex => playerIndex;
    }
}
