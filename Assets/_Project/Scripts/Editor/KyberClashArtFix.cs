using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using KyberKlash.Data;

namespace KyberKlash.Editor
{
    /// <summary>
    /// Art-integration repair: slices character sprite sheets into a uniform grid and
    /// re-points each animation clip's m_Sprite keyframes to real sliced frames.
    /// First-pass mapping is a reasonable default; tune after visual review.
    /// </summary>
    public class KyberClashArtFix
    {
        // Default grid. Override per character base-name if a sheet differs.
        static Dictionary<string, Vector2Int> GridOverride = new Dictionary<string, Vector2Int>
        {
            // base folder name -> cols,rows
        };

        static Vector2Int GridFor(string folder)
        {
            if (GridOverride.TryGetValue(folder, out var g)) return g;
            return new Vector2Int(3, 3);
        }

        [MenuItem("KyberClash/Art Fix/Slice Sheets + Wire Animations + Repair VFX")]
        public static void RunAll()
        {
            Debug.Log("=== KyberClashArtFix: start ===");
            SliceSheets();
            AssetDatabase.Refresh();
            WireAnimations();
            RepairVFXPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("=== KyberClashArtFix: complete ===");
        }

        static void SliceSheets()
        {
            var sheets = Directory.GetFiles("Assets/_Project/Art/Characters", "*animation_spritesheet.png", SearchOption.AllDirectories);
            foreach (var path in sheets)
            {
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) { Debug.LogError("No importer: " + path); continue; }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) { Debug.LogError("No texture: " + path); continue; }
                int w = tex.width, h = tex.height;
                var folder = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                var grid = GridFor(folder);
                int cols = grid.x, rows = grid.y;
                float cellW = (float)w / cols, cellH = (float)h / rows;
                int ppu = ti.spritePixelsPerUnit > 0 ? (int)ti.spritePixelsPerUnit : 256;

                ti.spriteImportMode = SpriteImportMode.Multiple;
                ti.spritePixelsPerUnit = ppu;
                ti.filterMode = FilterMode.Point; // crisp pixel-art
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;

                var metas = new List<SpriteMetaData>();
                int idx = 0;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        // Unity rect origin is bottom-left; sheet row 0 is top.
                        float y = h - (r + 1) * cellH;
                        metas.Add(new SpriteMetaData
                        {
                            name = Path.GetFileNameWithoutExtension(path) + "_" + idx,
                            rect = new Rect(c * cellW, y, cellW, cellH),
                            alignment = (int)SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f)
                        });
                        idx++;
                    }
                }
                ti.spritesheet = metas.ToArray();
                ti.SaveAndReimport();
                Debug.Log($"Sliced {path} -> {cols}x{rows} ({metas.Count} frames)");
            }
        }

        // Per-clip-name -> list of frame indices (row-major, 0..8 for 3x3).
        static List<int> FramesForClip(string clipName)
        {
            string n = clipName.ToLower();
            if (n.Contains("idle")) return new List<int> { 0, 1, 2, 1 };
            if (n.Contains("move") || n.Contains("walk")) return new List<int> { 3, 4, 5, 4 };
            if (n.Contains("attack")) return new List<int> { 6, 7, 8 };
            if (n.Contains("special")) return new List<int> { 6, 7, 8 };
            if (n.Contains("jump") || n.Contains("doublejump") || n.Contains("air") || n.Contains("upair") || n.Contains("downair") || n.Contains("backair") || n.Contains("forwardair")) return new List<int> { 6, 7, 8 };
            if (n.Contains("fall")) return new List<int> { 5 };
            if (n.Contains("hit")) return new List<int> { 2 };
            if (n.Contains("block") || n.Contains("parry")) return new List<int> { 1 };
            if (n.Contains("death")) return new List<int> { 8 };
            if (n.Contains("dash")) return new List<int> { 4 };
            return new List<int> { 0 };
        }

        static void WireAnimations()
        {
            var controllers = Directory.GetFiles("Assets/_Project/Animations/Characters", "*.controller", SearchOption.AllDirectories);
            foreach (var ctrlPath in controllers)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
                if (ctrl == null) continue;
                var clips = ctrl.animationClips;
                // Find the sprite sheet that matches this character folder.
                string charFolder = Path.GetFileName(Path.GetDirectoryName(ctrlPath));
                string sheetPath = Directory.GetFiles("Assets/_Project/Art/Characters/" + charFolder, "*animation_spritesheet.png", SearchOption.AllDirectories).FirstOrDefault();
                if (sheetPath == null) { Debug.LogWarning("No sheet for " + charFolder); continue; }
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
                var sprites = allAssets.OfType<Sprite>().ToArray();
                if (sprites.Length == 0) { Debug.LogWarning("No sliced sprites in " + sheetPath); continue; }
                System.Array.Sort(sprites, (a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));

                foreach (var clip in clips)
                {
                    var frames = FramesForClip(clip.name);
                    // Clamp indices to available sprites.
                    var keys = new ObjectReferenceKeyframe[frames.Count];
                    float step = clip.length > 0 ? clip.length / frames.Count : 0.1f;
                    for (int i = 0; i < frames.Count; i++)
                    {
                        int fi = Mathf.Clamp(frames[i], 0, sprites.Length - 1);
                        keys[i] = new ObjectReferenceKeyframe { time = i * step, value = sprites[fi] };
                    }
                    var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                    // Preserve any existing path if present.
                    var existing = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    foreach (var b in existing)
                    {
                        if (b.propertyName == "m_Sprite" && !string.IsNullOrEmpty(b.path))
                            binding.path = b.path;
                    }
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                    EditorUtility.SetDirty(clip);
                    Debug.Log($"Wired {clip.name} ({charFolder}) -> {frames.Count} frames");
                }
            }
        }

        static int ExtractIndex(string spriteName)
        {
            var parts = spriteName.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int i)) return i;
            return 0;
        }

        static void RepairVFXPrefabs()
        {
            var vfxGuids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/_Project/Prefabs/VFX" });
            foreach (var g in vfxGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                // Strip missing-script components, then ensure HitImpactVFX is present.
                var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    Debug.Log($"Removed {count} missing-script component(s) on {path}");
                }
                if (go.GetComponent<KyberKlash.VFX.HitImpactVFX>() == null)
                {
                    go.AddComponent<KyberKlash.VFX.HitImpactVFX>();
                    Debug.Log($"Added HitImpactVFX to {path}");
                }
                EditorUtility.SetDirty(go);
                PrefabUtility.SavePrefabAsset(go);
            }
        }
    }
}
