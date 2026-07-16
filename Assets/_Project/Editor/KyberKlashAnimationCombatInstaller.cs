using System.Collections.Generic;
using System.IO;
using KyberKlash.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KyberKlash.EditorTools
{
    public static class KyberKlashAnimationCombatInstaller
    {
        private const string AttackRoot = "Assets/_Project/Data/Attacks";
        private const string GeneratedAttackRoot = "Assets/_Project/Data/Attacks/Generated";
        private const string CharacterRoot = "Assets/_Project/Data/Characters";
        private const string GeneratedCharacterRoot = "Assets/_Project/Data/Characters/Generated";

        private static readonly string[] RequiredAnimatorStates =
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

        [MenuItem("Tools/Kyber Clash/Combat/Install Animation And Special Attack Wiring")]
        public static void InstallAnimationAndSpecialAttackWiring()
        {
            EnsureFolders();
            LudoGeneratedAssetOrganizer.OrganizeGeneratedCharacterAssets();
            EnsureSharedAttacks();
            AssignAttacksToCharacters();
            ValidateAnimatorControllers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[KyberKlash][AnimationCombat] Installed animation states, aerial attacks, unique specials, and recovery-special data.");
        }

        public static void InstallAnimationAndSpecialAttackWiringBatch()
        {
            InstallAnimationAndSpecialAttackWiring();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void EnsureSharedAttacks()
        {
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_Light.asset"), "light_slash", "Light Slash", "LightAttack", 8f, 13f, 50f, 5, 4, 9, new Vector3(1.1f, 0.8f, 0f), new Vector3(1.35f, 1.1f, 1.2f));
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_Heavy.asset"), "heavy_slash", "Heavy Slash", "HeavyAttack", 18f, 22f, 58f, 12, 5, 18, new Vector3(1.35f, 0.9f, 0f), new Vector3(1.7f, 1.25f, 1.2f));
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_UpAir.asset"), "up_air", "Rising Saber Arc", "UpAir", 12f, 17f, 82f, 6, 4, 12, new Vector3(0.2f, 1.6f, 0f), new Vector3(1.35f, 1.45f, 1.2f));
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_DownAir.asset"), "down_air", "Falling Saber Spike", "DownAir", 14f, 19f, 270f, 8, 5, 16, new Vector3(0.2f, -0.25f, 0f), new Vector3(1.35f, 1.25f, 1.2f));
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_ForwardAir.asset"), "forward_air", "Forward Saber Sweep", "ForwardAir", 11f, 16f, 38f, 7, 4, 13, new Vector3(1.2f, 0.8f, 0f), new Vector3(1.45f, 1.05f, 1.2f));
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_BackAir.asset"), "back_air", "Backhand Saber Sweep", "BackAir", 13f, 18f, 34f, 8, 4, 15, new Vector3(-1.1f, 0.85f, 0f), new Vector3(1.45f, 1.05f, 1.2f));
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_NeutralSpecial.asset"), "neutral_special", "Kyber Pulse", "NeutralSpecial", 15f, 20f, 45f, 16, 6, 22, new Vector3(1.25f, 0.9f, 0f), new Vector3(1.65f, 1.35f, 1.2f), 5f);
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_SideSpecial.asset"), "side_special", "Saber Rush", "SideSpecial", 13f, 19f, 32f, 10, 5, 18, new Vector3(1.55f, 0.8f, 0f), new Vector3(1.85f, 1.15f, 1.2f), 4f);
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_UpSpecial.asset"), "up_special", "Recovery Surge", "UpSpecial", 10f, 14f, 78f, 8, 5, 20, new Vector3(0.55f, 1.35f, 0f), new Vector3(1.4f, 1.6f, 1.2f), 0f);
            ConfigureAttack(LoadOrCreateAttack($"{AttackRoot}/Attack_DownSpecial.asset"), "down_special", "Guard Break Descent", "DownSpecial", 16f, 20f, 285f, 12, 5, 22, new Vector3(0.45f, -0.1f, 0f), new Vector3(1.55f, 1.35f, 1.2f), 6f);
        }

        private static void AssignAttacksToCharacters()
        {
            AttackSO light = AssetDatabase.LoadAssetAtPath<AttackSO>($"{AttackRoot}/Attack_Light.asset");
            AttackSO heavy = AssetDatabase.LoadAssetAtPath<AttackSO>($"{AttackRoot}/Attack_Heavy.asset");
            AttackSO upAir = AssetDatabase.LoadAssetAtPath<AttackSO>($"{AttackRoot}/Attack_UpAir.asset");
            AttackSO downAir = AssetDatabase.LoadAssetAtPath<AttackSO>($"{AttackRoot}/Attack_DownAir.asset");
            AttackSO forwardAir = AssetDatabase.LoadAssetAtPath<AttackSO>($"{AttackRoot}/Attack_ForwardAir.asset");
            AttackSO backAir = AssetDatabase.LoadAssetAtPath<AttackSO>($"{AttackRoot}/Attack_BackAir.asset");

            foreach (CharacterData character in LoadCharacters())
            {
                if (character == null)
                {
                    continue;
                }

                CharacterSpecialSet specials = CreateCharacterSpecialSet(character);
                character.defaultLightAttack = light;
                character.defaultHeavyAttack = heavy;
                character.defaultUpAerial = upAir;
                character.defaultDownAerial = downAir;
                character.defaultForwardAerial = forwardAir;
                character.defaultBackAerial = backAir;
                character.defaultNeutralSpecial = specials.Neutral;
                character.defaultSideSpecial = specials.Side;
                character.defaultUpSpecial = specials.Up;
                character.defaultDownSpecial = specials.Down;
                EditorUtility.SetDirty(character);
            }
        }

        private static CharacterSpecialSet CreateCharacterSpecialSet(CharacterData character)
        {
            string slug = !string.IsNullOrEmpty(character.characterId) ? character.characterId : character.name;
            string pascal = ToPascalCase(slug);
            SpecialStyle style = GetSpecialStyle(slug);

            AttackSO neutral = LoadOrCreateAttack($"{GeneratedAttackRoot}/Attack_{pascal}_NeutralSpecial.asset");
            AttackSO side = LoadOrCreateAttack($"{GeneratedAttackRoot}/Attack_{pascal}_SideSpecial.asset");
            AttackSO up = LoadOrCreateAttack($"{GeneratedAttackRoot}/Attack_{pascal}_UpSpecial.asset");
            AttackSO down = LoadOrCreateAttack($"{GeneratedAttackRoot}/Attack_{pascal}_DownSpecial.asset");

            ConfigureAttack(neutral, $"{slug}_neutral_special", style.NeutralName, "NeutralSpecial", style.NeutralDamage, style.NeutralKnockback, style.NeutralAngle, 14, 6, 20, new Vector3(1.25f, 0.9f, 0f), new Vector3(1.65f, 1.35f, 1.2f), 4f);
            ConfigureAttack(side, $"{slug}_side_special", style.SideName, "SideSpecial", style.SideDamage, style.SideKnockback, style.SideAngle, 10, 5, 18, new Vector3(1.55f, 0.8f, 0f), new Vector3(1.9f, 1.15f, 1.2f), 5f);
            ConfigureAttack(up, $"{slug}_up_special", style.UpName, "UpSpecial", style.UpDamage, style.UpKnockback, style.UpAngle, 8, 5, 22, new Vector3(0.55f, 1.35f, 0f), new Vector3(1.4f, 1.6f, 1.2f), 0f);
            ConfigureAttack(down, $"{slug}_down_special", style.DownName, "DownSpecial", style.DownDamage, style.DownKnockback, style.DownAngle, 12, 5, 22, new Vector3(0.45f, -0.1f, 0f), new Vector3(1.55f, 1.35f, 1.2f), 6f);

            return new CharacterSpecialSet(neutral, side, up, down);
        }

        private static void ConfigureAttack(AttackSO attack, string id, string displayName, string animationTrigger, float damage, float knockback, float angle, int startup, int active, int recovery, Vector3 hitboxOffset, Vector3 hitboxSize, float meterCost = 0f)
        {
            attack.attackId = id;
            attack.displayName = displayName;
            attack.startupFrames = startup;
            attack.activeFrames = active;
            attack.recoveryFrames = recovery;
            attack.baseDamage = damage;
            attack.baseKnockback = knockback;
            attack.knockbackAngle = angle;
            attack.knockbackGrowth = 0.055f;
            attack.minKnockback = Mathf.Max(5f, knockback * 0.4f);
            attack.maxKnockback = Mathf.Max(35f, knockback * 2.2f);
            attack.hitboxShape = AttackSO.HitboxShape.Box;
            attack.hitboxOffset = hitboxOffset;
            attack.hitboxSize = hitboxSize;
            attack.hitboxRotation = Vector3.zero;
            attack.meterGainOnHit = 5f;
            attack.meterGainOnWhiff = 1f;
            attack.meterCost = meterCost;
            attack.minMeterRequired = 0f;
            attack.hitPauseFrames = 4;
            attack.hitStunFrames = Mathf.Max(16, recovery);
            attack.blockStunFrames = 10;
            attack.blockPushback = 2f;
            attack.animationTrigger = animationTrigger;
            attack.canBeParried = true;
            attack.perfectParryWindowFrames = 8;
            attack.perfectParryMeterReward = 15f;
            attack.hasArmor = false;
            attack.armorFrames = 0;
            attack.maxHits = 1;
            attack.multiHitIntervalFrames = 5;
            EditorUtility.SetDirty(attack);
        }

        private static AttackSO LoadOrCreateAttack(string path)
        {
            AttackSO attack = AssetDatabase.LoadAssetAtPath<AttackSO>(path);
            if (attack != null)
            {
                return attack;
            }

            attack = ScriptableObject.CreateInstance<AttackSO>();
            AssetDatabase.CreateAsset(attack, path);
            return attack;
        }

        private static IEnumerable<CharacterData> LoadCharacters()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharacterRoot, GeneratedCharacterRoot });
            HashSet<CharacterData> characters = new HashSet<CharacterData>();
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (character != null)
                {
                    characters.Add(character);
                }
            }

            return characters;
        }

        private static void ValidateAnimatorControllers()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/_Project/Animations/Characters" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller == null || controller.layers.Length == 0)
                {
                    Debug.LogWarning($"[KyberKlash][AnimationCombat] Missing AnimatorController at {path}.");
                    continue;
                }

                HashSet<string> states = new HashSet<string>();
                ChildAnimatorState[] childStates = controller.layers[0].stateMachine.states;
                for (int stateIndex = 0; stateIndex < childStates.Length; stateIndex++)
                {
                    states.Add(childStates[stateIndex].state.name);
                }

                for (int requiredIndex = 0; requiredIndex < RequiredAnimatorStates.Length; requiredIndex++)
                {
                    if (!states.Contains(RequiredAnimatorStates[requiredIndex]))
                    {
                        Debug.LogWarning($"[KyberKlash][AnimationCombat] {controller.name} is missing animation state '{RequiredAnimatorStates[requiredIndex]}'. Re-run Ludo organizer if this persists.");
                    }
                }
            }
        }

        private static SpecialStyle GetSpecialStyle(string slug)
        {
            string lower = slug.ToLowerInvariant();
            if (lower.Contains("ataru") || lower.Contains("acrobatic"))
            {
                return new SpecialStyle("Cyclone Saber", "Vaulting Spiral Rush", "Force Flip Recovery", "Meteor Heel Slash", 13f, 12f, 10f, 14f, 19f, 17f, 14f, 20f, 70f, 30f, 82f, 285f);
            }

            if (lower.Contains("makashi") || lower.Contains("precision"))
            {
                return new SpecialStyle("Duelist Needle", "Piercing Lunge", "Elegant Rising Thrust", "Riposte Sweep", 11f, 13f, 9f, 12f, 17f, 19f, 13f, 17f, 42f, 24f, 88f, 270f);
            }

            if (lower.Contains("soresu") || lower.Contains("deflection"))
            {
                return new SpecialStyle("Reflective Kyber Guard", "Shielded Step", "Deflection Lift", "Anchor Counter", 10f, 11f, 8f, 12f, 15f, 16f, 12f, 16f, 55f, 36f, 86f, 250f);
            }

            if (lower.Contains("shien") || lower.Contains("djem") || lower.Contains("powercounter"))
            {
                return new SpecialStyle("Power Reversal", "Djem So Shoulder Rush", "Rising Power Counter", "Crushing Drop", 16f, 16f, 11f, 18f, 22f, 23f, 15f, 24f, 48f, 32f, 76f, 288f);
            }

            if (lower.Contains("niman") || lower.Contains("forceutility"))
            {
                return new SpecialStyle("Force Pulse", "Force Push Dash", "Force Lift Recovery", "Gravity Snare", 12f, 10f, 8f, 10f, 18f, 18f, 12f, 15f, 50f, 28f, 90f, 260f);
            }

            if (lower.Contains("juyo") || lower.Contains("vaapad") || lower.Contains("berserker"))
            {
                return new SpecialStyle("Vaapad Surge", "Frenzy Saber Burst", "Raging Diagonal Recovery", "Falling Fury", 17f, 18f, 12f, 17f, 21f, 24f, 16f, 22f, 44f, 28f, 72f, 292f);
            }

            return new SpecialStyle("Balanced Kyber Pulse", "Shii-Cho Saber Rush", "Guardian Recovery Slash", "Flowing Guard Break", 12f, 13f, 10f, 13f, 18f, 19f, 14f, 18f, 50f, 34f, 80f, 275f);
        }

        private static string ToPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Character";
            }

            string[] parts = value.Split(new[] { '_', '-', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            string result = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 1)
                {
                    result += part.ToUpperInvariant();
                }
                else
                {
                    result += char.ToUpperInvariant(part[0]) + part.Substring(1);
                }
            }

            return result;
        }

        private static void EnsureFolders()
        {
            EnsureFolder(AttackRoot);
            EnsureFolder(GeneratedAttackRoot);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct CharacterSpecialSet
        {
            public readonly AttackSO Neutral;
            public readonly AttackSO Side;
            public readonly AttackSO Up;
            public readonly AttackSO Down;

            public CharacterSpecialSet(AttackSO neutral, AttackSO side, AttackSO up, AttackSO down)
            {
                Neutral = neutral;
                Side = side;
                Up = up;
                Down = down;
            }
        }

        private readonly struct SpecialStyle
        {
            public readonly string NeutralName;
            public readonly string SideName;
            public readonly string UpName;
            public readonly string DownName;
            public readonly float NeutralDamage;
            public readonly float SideDamage;
            public readonly float UpDamage;
            public readonly float DownDamage;
            public readonly float NeutralKnockback;
            public readonly float SideKnockback;
            public readonly float UpKnockback;
            public readonly float DownKnockback;
            public readonly float NeutralAngle;
            public readonly float SideAngle;
            public readonly float UpAngle;
            public readonly float DownAngle;

            public SpecialStyle(string neutralName, string sideName, string upName, string downName, float neutralDamage, float sideDamage, float upDamage, float downDamage, float neutralKnockback, float sideKnockback, float upKnockback, float downKnockback, float neutralAngle, float sideAngle, float upAngle, float downAngle)
            {
                NeutralName = neutralName;
                SideName = sideName;
                UpName = upName;
                DownName = downName;
                NeutralDamage = neutralDamage;
                SideDamage = sideDamage;
                UpDamage = upDamage;
                DownDamage = downDamage;
                NeutralKnockback = neutralKnockback;
                SideKnockback = sideKnockback;
                UpKnockback = upKnockback;
                DownKnockback = downKnockback;
                NeutralAngle = neutralAngle;
                SideAngle = sideAngle;
                UpAngle = upAngle;
                DownAngle = downAngle;
            }
        }
    }
}
