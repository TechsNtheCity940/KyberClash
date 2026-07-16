using TMPro;
using UnityEngine;

namespace KyberKlash.VFX
{
    public sealed class HitPointFeedback : MonoBehaviour
    {
        private const float Lifetime = 0.55f;

        private LineRenderer ring;
        private TextMeshPro damageText;
        private float age;
        private float startRadius;
        private float endRadius;
        private Color color;
        private Vector3 drift;

        public static void Spawn(Vector3 position, float damage, Vector3 launchVelocity, bool strongLaunch)
        {
            GameObject root = new GameObject("HitPointFeedback");
            root.transform.position = position;

            HitPointFeedback feedback = root.AddComponent<HitPointFeedback>();
            feedback.Initialize(damage, launchVelocity, strongLaunch);
        }

        private void Initialize(float damage, Vector3 launchVelocity, bool strongLaunch)
        {
            color = strongLaunch ? new Color(1f, 0.18f, 0.42f, 1f) : new Color(0.45f, 0.9f, 1f, 1f);
            startRadius = strongLaunch ? 0.35f : 0.2f;
            endRadius = strongLaunch ? 2.1f : 1.25f;
            drift = launchVelocity.sqrMagnitude > 0.01f ? launchVelocity.normalized * 1.5f : Vector3.up;
            drift.z = 0f;

            CreateRing();
            CreateSparks(strongLaunch);
            CreateDamageText(damage);
            Destroy(gameObject, Lifetime + 0.05f);
        }

        private void CreateRing()
        {
            ring = gameObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 28;
            ring.widthMultiplier = 0.06f;
            ring.numCapVertices = 2;
            ring.numCornerVertices = 2;
            ring.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = color
            };
        }

        private void CreateSparks(bool strongLaunch)
        {
            ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = Lifetime;
            main.loop = false;
            main.startLifetime = strongLaunch ? 0.38f : 0.28f;
            main.startSpeed = strongLaunch ? 8f : 5f;
            main.startSize = strongLaunch ? 0.12f : 0.08f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, strongLaunch ? (short)34 : (short)18)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = strongLaunch ? 0.5f : 0.28f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 40;

            particles.Play();
        }

        private void CreateDamageText(float damage)
        {
            GameObject textObject = new GameObject("DamageText");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            damageText = textObject.AddComponent<TextMeshPro>();
            damageText.text = $"+{Mathf.RoundToInt(damage)}%";
            damageText.fontSize = 3.2f;
            damageText.alignment = TextAlignmentOptions.Center;
            damageText.color = Color.white;
            damageText.outlineWidth = 0.25f;
            damageText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
            damageText.sortingOrder = 45;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Lifetime);
            float radius = Mathf.Lerp(startRadius, endRadius, EaseOut(t));
            float alpha = 1f - t;

            if (ring != null)
            {
                Color ringColor = color;
                ringColor.a = alpha;
                ring.startColor = ringColor;
                ring.endColor = ringColor;
                ring.widthMultiplier = Mathf.Lerp(0.08f, 0.01f, t);
                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = (i / (float)ring.positionCount) * Mathf.PI * 2f;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
                }
            }

            if (damageText != null)
            {
                Color textColor = damageText.color;
                textColor.a = alpha;
                damageText.color = textColor;
                damageText.transform.localPosition = Vector3.Lerp(new Vector3(0f, 0.55f, 0f), drift, t);
                damageText.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.78f, t);
            }
        }

        private static float EaseOut(float t)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        }
    }
}
