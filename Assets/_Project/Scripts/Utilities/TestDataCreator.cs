using KyberKlash.Data;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KyberKlash.Utilities
{
    /// <summary>
    /// Utility to create test data assets programmatically for quick prototyping
    /// </summary>
    public static class TestDataCreator
    {
#if UNITY_EDITOR
        private const string DataPath = "Assets/_Project/Data";

        private static readonly string[] RequiredAssetPaths =
        {
            DataPath + "/Attacks/Attack_Light.asset",
            DataPath + "/Attacks/Attack_Heavy.asset",
            DataPath + "/Attacks/Attack_UpAir.asset",
            DataPath + "/Attacks/Attack_DownAir.asset",
            DataPath + "/Attacks/Attack_NeutralSpecial.asset",
            DataPath + "/Forms/Form_ShiiCho.asset",
            DataPath + "/Forms/Form_Makashi.asset",
            DataPath + "/Forms/Form_Soresu.asset",
            DataPath + "/Forms/Form_Ataru.asset",
            DataPath + "/Characters/Character_JollyKnight.asset",
            DataPath + "/Characters/Character_SithfullyYours.asset",
            DataPath + "/Stages/Stage_PrototypeArena.asset"
        };

        [UnityEditor.InitializeOnLoadMethod]
        private static void CreateDefaultAssetsOnEditorLoad()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                CreateDefaultAssets(silentIfExists: true);
            };
        }

        [UnityEditor.MenuItem("Kyber Clash/Create Test Data Assets")]
        public static void CreateDefaultAssets()
        {
            CreateDefaultAssets(silentIfExists: false);
        }

        private static void CreateDefaultAssets(bool silentIfExists)
        {
            if (AllRequiredAssetsExist())
            {
                if (!silentIfExists)
                    UnityEngine.Debug.Log("[TestDataCreator] Test data assets already exist!");

                return;
            }

            EnsureFolder(DataPath);
            EnsureFolder(DataPath + "/Characters");
            EnsureFolder(DataPath + "/Forms");
            EnsureFolder(DataPath + "/Attacks");
            EnsureFolder(DataPath + "/Stages");

            // Create basic attack data
            var attacks = CreateAttackAssets(DataPath + "/Attacks");
            
            // Create form data
            var forms = CreateFormAssets(DataPath + "/Forms");
            
            // Create character data (using the created attacks and forms)
            CreateCharacterAssets(DataPath + "/Characters", attacks, forms);
            
            // Create stage data
            CreateStageAssets(DataPath + "/Stages");

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            
            UnityEngine.Debug.Log("[TestDataCreator] Test data assets created!");
        }

        private static AttackSO[] CreateAttackAssets(string path)
        {
            // Light Attack
            var lightAttack = LoadOrCreateAsset<AttackSO>(path + "/Attack_Light.asset", asset =>
            {
                asset.attackId = "light";
                asset.displayName = "Light Attack";
                asset.startupFrames = 5;
                asset.activeFrames = 3;
                asset.recoveryFrames = 10;
                asset.baseDamage = 8f;
                asset.baseKnockback = 8f;
                asset.knockbackAngle = 50f;
                asset.hitboxShape = AttackSO.HitboxShape.SaberArc;
                asset.hitboxSize = new Vector3(0.3f, 0.3f, 0.3f);
                asset.meterGainOnHit = 5f;
                asset.animationTrigger = "LightAttack";
                asset.perfectParryWindowFrames = 8;
                asset.perfectParryMeterReward = 10f;
            });

            // Heavy Attack
            var heavyAttack = LoadOrCreateAsset<AttackSO>(path + "/Attack_Heavy.asset", asset =>
            {
                asset.attackId = "heavy";
                asset.displayName = "Heavy Attack";
                asset.startupFrames = 12;
                asset.activeFrames = 4;
                asset.recoveryFrames = 20;
                asset.baseDamage = 18f;
                asset.baseKnockback = 20f;
                asset.knockbackAngle = 60f;
                asset.hitboxShape = AttackSO.HitboxShape.SaberArc;
                asset.hitboxSize = new Vector3(0.4f, 0.4f, 0.4f);
                asset.meterGainOnHit = 10f;
                asset.animationTrigger = "HeavyAttack";
                asset.perfectParryWindowFrames = 6;
                asset.perfectParryMeterReward = 15f;
            });

            // Up Aerial
            var upAir = LoadOrCreateAsset<AttackSO>(path + "/Attack_UpAir.asset", asset =>
            {
                asset.attackId = "up_air";
                asset.displayName = "Up Aerial";
                asset.startupFrames = 6;
                asset.activeFrames = 4;
                asset.recoveryFrames = 12;
                asset.baseDamage = 12f;
                asset.baseKnockback = 15f;
                asset.knockbackAngle = 80f;
                asset.hitboxShape = AttackSO.HitboxShape.SaberArc;
                asset.hitboxSize = new Vector3(0.3f, 0.3f, 0.3f);
                asset.meterGainOnHit = 6f;
                asset.animationTrigger = "UpAir";
            });

            // Down Aerial
            var downAir = LoadOrCreateAsset<AttackSO>(path + "/Attack_DownAir.asset", asset =>
            {
                asset.attackId = "down_air";
                asset.displayName = "Down Aerial";
                asset.startupFrames = 8;
                asset.activeFrames = 3;
                asset.recoveryFrames = 15;
                asset.baseDamage = 14f;
                asset.baseKnockback = 18f;
                asset.knockbackAngle = 270f;
                asset.hitboxShape = AttackSO.HitboxShape.SaberArc;
                asset.hitboxSize = new Vector3(0.3f, 0.3f, 0.3f);
                asset.meterGainOnHit = 7f;
                asset.animationTrigger = "DownAir";
            });

            // Neutral Special
            var neutralSpecial = LoadOrCreateAsset<AttackSO>(path + "/Attack_NeutralSpecial.asset", asset =>
            {
                asset.attackId = "neutral_special";
                asset.displayName = "Force Push";
                asset.startupFrames = 20;
                asset.activeFrames = 6;
                asset.recoveryFrames = 30;
                asset.baseDamage = 15f;
                asset.baseKnockback = 25f;
                asset.knockbackAngle = 45f;
                asset.hitboxShape = AttackSO.HitboxShape.Box;
                asset.hitboxSize = new Vector3(2f, 1f, 3f);
                asset.hitboxOffset = new Vector3(0f, 0f, 2f);
                asset.meterCost = 25f;
                asset.minMeterRequired = 25f;
                asset.meterGainOnHit = 15f;
                asset.animationTrigger = "Special";
                asset.canBeParried = true;
            });

            return new AttackSO[] { lightAttack, heavyAttack, upAir, downAir, neutralSpecial };
        }

        private static FormSO[] CreateFormAssets(string path)
        {
            // Shii-Cho (Balanced)
            var shiiCho = LoadOrCreateAsset<FormSO>(path + "/Form_ShiiCho.asset", asset =>
            {
                asset.formId = "shii_cho";
                asset.displayName = "Form I: Shii-Cho";
                asset.loreDescription = "The ancient foundation form. Balanced and adaptable.";
                asset.saberColor = Color.blue;
                asset.saberCoreColor = Color.white;
                asset.mechanicType = FormSO.FormMechanicType.CounterStance;
                asset.mechanicData = new FormSO.FormMechanicData
                {
                    counterWindowFrames = 10f,
                    counterMeterGain = 10f
                };
            });

            // Makashi (Dueling)
            var makashi = LoadOrCreateAsset<FormSO>(path + "/Form_Makashi.asset", asset =>
            {
                asset.formId = "makashi";
                asset.displayName = "Form II: Makashi";
                asset.loreDescription = "The dueling form. Precision and economy of motion.";
                asset.saberColor = Color.green;
                asset.saberCoreColor = Color.white;
                asset.moveSpeedMultiplier = 1.1f;
                asset.attackSpeedMultiplier = 1.15f;
                asset.mechanicType = FormSO.FormMechanicType.PrecisionParry;
                asset.mechanicData = new FormSO.FormMechanicData
                {
                    precisionWindowFrames = 3f,
                    riposteDamageMultiplier = 1.5f,
                    riposteKnockbackMultiplier = 1.3f
                };
            });

            // Soresu (Defense)
            var soresu = LoadOrCreateAsset<FormSO>(path + "/Form_Soresu.asset", asset =>
            {
                asset.formId = "soresu";
                asset.displayName = "Form III: Soresu";
                asset.loreDescription = "The defensive form. Impenetrable defense, perfect deflection.";
                asset.saberColor = Color.cyan;
                asset.saberCoreColor = Color.white;
                asset.moveSpeedMultiplier = 0.9f;
                asset.gravityMultiplier = 0.8f;
                asset.mechanicType = FormSO.FormMechanicType.PerfectDeflection;
                asset.mechanicData = new FormSO.FormMechanicData
                {
                    canDeflectProjectiles = true,
                    deflectionAngle = 45f
                };
            });

            // Ataru (Acrobatic)
            var ataru = LoadOrCreateAsset<FormSO>(path + "/Form_Ataru.asset", asset =>
            {
                asset.formId = "ataru";
                asset.displayName = "Form IV: Ataru";
                asset.loreDescription = "The acrobatic form. Aggression through mobility.";
                asset.saberColor = Color.green;
                asset.saberCoreColor = Color.yellow;
                asset.moveSpeedMultiplier = 1.2f;
                asset.airSpeedMultiplier = 1.3f;
                asset.jumpHeightMultiplier = 1.2f;
                asset.maxAirJumps = 2;
                asset.canAirDash = true;
                asset.airDashCooldown = 0.5f;
                asset.mechanicType = FormSO.FormMechanicType.AcrobaticFlow;
                asset.mechanicData = new FormSO.FormMechanicData
                {
                    comboWindowExtension = 0.3f,
                    maxAerialChains = 4,
                    aerialMomentumPreservation = 0.9f
                };
            });

            return new FormSO[] { shiiCho, makashi, soresu, ataru };
        }

        private static void CreateCharacterAssets(string path, AttackSO[] attacks, FormSO[] forms)
        {
            // Use the created attack and form instances directly
            var lightAttack = attacks[0];
            var heavyAttack = attacks[1];
            var upAir = attacks[2];
            var downAir = attacks[3];
            var neutralSpecial = attacks[4];

            var shiiCho = forms[0];
            var makashi = forms[1];
            var soresu = forms[2];
            var ataru = forms[3];

            // Jedi Knight
            LoadOrCreateAsset<CharacterData>(path + "/Character_JollyKnight.asset", asset =>
            {
                asset.characterId = "jolly_knight";
                asset.displayName = "Jolly Knight";
                asset.characterDescription = "A balanced warrior of the Order.";
                asset.baseMoveSpeed = 8f;
                asset.baseAirSpeed = 6f;
                asset.baseJumpHeight = 5f;
                asset.baseGravity = 1f;
                asset.baseDashDistance = 6f;
                asset.baseDashDuration = 0.2f;
                asset.baseMaxAirJumps = 1;
                asset.baseCanAirDash = true;
                asset.baseAirDashCooldown = 1f;
                asset.weightClass = CharacterData.WeightClass.Medium;
                asset.defaultForm = shiiCho;
                asset.availableForms = new FormSO[] { shiiCho, makashi, soresu, ataru };
                asset.defaultLightAttack = lightAttack;
                asset.defaultHeavyAttack = heavyAttack;
                asset.defaultUpAerial = upAir;
                asset.defaultDownAerial = downAir;
                asset.defaultNeutralSpecial = neutralSpecial;
            });

            // Sith Warrior
            LoadOrCreateAsset<CharacterData>(path + "/Character_SithfullyYours.asset", asset =>
            {
                asset.characterId = "sithfully_yours";
                asset.displayName = "Sithfully Yours";
                asset.characterDescription = "Power through passion, victory through strength.";
                asset.baseMoveSpeed = 7f;
                asset.baseAirSpeed = 5f;
                asset.baseJumpHeight = 4.5f;
                asset.baseGravity = 1.1f;
                asset.baseDashDistance = 7f;
                asset.baseDashDuration = 0.25f;
                asset.baseMaxAirJumps = 1;
                asset.baseCanAirDash = true;
                asset.baseAirDashCooldown = 1.2f;
                asset.weightClass = CharacterData.WeightClass.Heavy;
                asset.defaultForm = makashi;
                asset.availableForms = new FormSO[] { makashi, shiiCho, ataru };
                asset.defaultLightAttack = lightAttack;
                asset.defaultHeavyAttack = heavyAttack;
                asset.defaultUpAerial = upAir;
                asset.defaultDownAerial = downAir;
                asset.defaultNeutralSpecial = neutralSpecial;
            });
        }

        private static void CreateStageAssets(string path)
        {
            LoadOrCreateAsset<StageData>(path + "/Stage_PrototypeArena.asset", asset =>
            {
                asset.stageId = "prototype_arena";
                asset.displayName = "Prototype Arena";
                asset.stageDescription = "A simple training arena for testing combat.";
                asset.leftBlastZone = -18f;
                asset.rightBlastZone = 18f;
                asset.topBlastZone = 14f;
                asset.bottomBlastZone = -14f;
                asset.hasHazards = true;
                asset.isTournamentLegal = true;
                asset.maxPlayers = 4;
            });
        }

        private static T LoadOrCreateAsset<T>(string assetPath, System.Action<T> configureAsset)
            where T : ScriptableObject
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            configureAsset(asset);
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static bool AllRequiredAssetsExist()
        {
            foreach (string assetPath in RequiredAssetPaths)
            {
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) == null)
                    return false;
            }

            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (UnityEditor.AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                return;

            EnsureFolder(parent);
            UnityEditor.AssetDatabase.CreateFolder(parent, folderName);
        }
#endif
    }
}
