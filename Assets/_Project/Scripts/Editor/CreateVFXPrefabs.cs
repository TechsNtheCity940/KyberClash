using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace KyberKlash.Editor
{
    /// <summary>
    /// Editor script to create VFX/SFX prefabs for attacks - simplified version
    /// </summary>
    public class CreateVFXPrefabs : EditorWindow
    {
        [MenuItem("KyberClash/VFX/Create Attack VFX Prefabs")]
        public static void CreateAttackVFXPrefabs()
        {
            Debug.Log("=== Creating Attack VFX Prefabs ===");
            
            EnsureFolder("Assets/_Project/Prefabs/VFX");
            
            CreateHitImpactPrefabs();
            CreateTrailPrefabs();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("=== VFX Prefabs Created Successfully ===");
        }
        
        static void CreateHitImpactPrefabs()
        {
            // Light hit impact
            CreateHitImpactPrefab("HitImpact_Light", Color.yellow, 0.5f, 10, 0.5f);
            
            // Medium hit impact
            CreateHitImpactPrefab("HitImpact_Medium", Color.orange, 0.7f, 20, 1f);
            
            // Heavy hit impact
            CreateHitImpactPrefab("HitImpact_Heavy", Color.red, 1f, 30, 1.5f);
            
            // Clash impact
            CreateHitImpactPrefab("HitImpact_Clash", Color.white, 0.8f, 25, 1f, true);
            
            // Parry impact
            CreateHitImpactPrefab("HitImpact_Parry", Color.cyan, 0.6f, 15, 0.8f);
        }
        
        static void CreateHitImpactPrefab(string name, Color color, float size, int count, float lifetime, bool isClash = false)
        {
            var prefab = new GameObject(name);
            
            // Add particle system directly to the root
            var ps = prefab.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = 5f;
            main.startSize = size;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
            main.gravityModifier = 0f;
            main.maxParticles = 100;
            
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)30, (short)30)
            });
            
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
            velocity.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color * 0.5f, 0.5f),
                new GradientColorKey(new Color(color.r, color.g, color.b, 0f), 1f)
            };
            gradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            };
            colorOverLifetime.color = gradient;
            
            // Add light for clash
            if (isClash)
            {
                var lightObj = new GameObject("ImpactLight");
                lightObj.transform.SetParent(prefab.transform);
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = 3f;
                light.range = 5f;
            }
            
            // Add auto-destroy script
            var autoDestroy = prefab.AddComponent<AutoDestroyVFX>();
            autoDestroy.lifetime = 1f;
            
            // Save prefab
            string path = $"Assets/_Project/Prefabs/VFX/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            DestroyImmediate(prefab);
            Debug.Log($"Created {name} at {path}");
        }
        
        static void CreateTrailPrefabs()
        {
            // Light attack trail
            CreateTrailPrefab("Trail_Light", Color.yellow, 0.15f, 0.05f, 0.2f);
            
            // Heavy attack trail
            CreateTrailPrefab("Trail_Heavy", Color.orange, 0.25f, 0.05f, 0.3f);
            
            // Special attack trail
            CreateTrailPrefab("Trail_Special", Color.magenta, 0.3f, 0.1f, 0.4f);
            
            // Saber trail
            CreateTrailPrefab("Trail_Saber", Color.cyan, 0.2f, 0.05f, 0.5f);
        }
        
        static void CreateTrailPrefab(string name, Color color, float startWidth, float endWidth, float time)
        {
            var prefab = new GameObject(name);
            
            var trailObj = new GameObject("Trail");
            trailObj.transform.SetParent(prefab.transform);
            var trail = trailObj.AddComponent<TrailRenderer>();
            
            trail.time = time;
            trail.startWidth = startWidth;
            trail.endWidth = endWidth;
            trail.minVertexDistance = 0.01f;
            
            // Create gradient
            var gradient = new Gradient();
            gradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color * 0.5f, 0.5f),
                new GradientColorKey(new Color(color.r, color.g, color.b, 0f), 1f)
            };
            gradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            };
            trail.colorGradient = gradient;
            
            // Material
            trail.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.white
            };
            trail.emitting = false;
            
            // Save prefab
            string path = $"Assets/_Project/Prefabs/VFX/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            DestroyImmediate(prefab);
            Debug.Log($"Created {name} at {path}");
        }
        
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName)) return;
            
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
    
    /// <summary>
    /// Simple auto-destroy script for VFX prefabs
    /// </summary>
    public class AutoDestroyVFX : MonoBehaviour
    {
        public float lifetime = 2f;
        
        void OnEnable()
        {
            Invoke(nameof(DestroySelf), lifetime);
        }
        
        void OnDisable()
        {
            CancelInvoke();
        }
        
        void DestroySelf()
        {
            if (gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
            }
        }
    }
}