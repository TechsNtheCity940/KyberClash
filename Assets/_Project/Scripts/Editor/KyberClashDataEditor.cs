using UnityEngine;
using UnityEditor;
using KyberKlash.Data;
using System.Collections.Generic;

namespace KyberKlash.Editor
{
    /// <summary>
    /// Editor window for creating and managing AttackSOs, FormSOs, and Characters
    /// </summary>
    public class KyberClashDataEditor : EditorWindow
    {
        [MenuItem("KyberClash/Data/Create AttackSOs")]
        public static void CreateAttackSOs()
        {
            Debug.Log("=== Creating AttackSOs ===");
            
            string folder = "Assets/_Project/Data/Attacks";
            EnsureFolder(folder);
            
            var attacks = new List<AttackData>
            {
                new("Attack_Light", "Light Attack", FormSO.AttackSlot.Light, 5, 4, 9, 8f, 13f, 45f, 0.05f, 5f, 1f),
                new("Attack_Heavy", "Heavy Attack", FormSO.AttackSlot.Heavy, 12, 6, 15, 15f, 18f, 50f, 0.08f, 8f, 1.5f),
                new("Attack_UpAir", "Up Aerial", FormSO.AttackSlot.UpAerial, 6, 4, 12, 9f, 14f, 45f, 0.06f, 6f, 1.2f),
                new("Attack_DownAir", "Down Aerial", FormSO.AttackSlot.DownAerial, 8, 5, 14, 12f, 16f, 70f, 0.08f, 10f, 1.5f),
                new("Attack_ForwardAir", "Forward Aerial", FormSO.AttackSlot.ForwardAerial, 7, 4, 10, 10f, 15f, 50f, 0.06f, 7f, 1.3f),
                new("Attack_BackAir", "Back Aerial", FormSO.AttackSlot.BackAerial, 9, 5, 12, 13f, 17f, 55f, 0.07f, 8f, 1.4f),
                new("Attack_NeutralSpecial", "Neutral Special", FormSO.AttackSlot.NeutralSpecial, 15, 8, 20, 20f, 22f, 60f, 0.12f, 15f, 2f),
                new("Attack_SideSpecial", "Side Special", FormSO.AttackSlot.SideSpecial, 14, 7, 18, 18f, 20f, 55f, 0.1f, 12f, 1.8f),
                new("Attack_UpSpecial", "Up Special", FormSO.AttackSlot.UpSpecial, 12, 6, 16, 16f, 18f, 80f, 0.1f, 10f, 1.5f),
                new("Attack_DownSpecial", "Down Special", FormSO.AttackSlot.DownSpecial, 16, 6, 22, 22f, 25f, 65f, 0.15f, 18f, 2.2f),
            };
            
            foreach (var a in attacks)
            {
                CreateAttackSO(folder, a);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("=== AttackSOs Created ===");
        }
        
        struct AttackData
        {
            public string name;
            public string displayName;
            public FormSO.AttackSlot slot;
            public int startupFrames;
            public int activeFrames;
            public int recoveryFrames;
            public float baseDamage;
            public float baseKnockback;
            public float knockbackAngle;
            public float knockbackGrowth;
            public float baseKnockbackMin;
            public float maxKnockback;
            public float hitPauseFrames;
            public float hitStunFrames;
            public AttackSO.HitboxShape hitboxShape;
            public Vector3 hitboxSize;
            public Vector3 hitboxOffset;
            
            public AttackData(string n, string dn, FormSO.AttackSlot s, int su, int ac, int re, float dmg, float kb, float angle, float kg, float minKB, float maxKB)
            {
                name = n; displayName = dn; slot = s;
                startupFrames = su; activeFrames = ac; recoveryFrames = re;
                baseDamage = dmg; baseKnockback = kb; knockbackAngle = angle; knockbackGrowth = kg;
                baseKnockbackMin = minKB; maxKnockback = maxKB;
                hitPauseFrames = 4; hitStunFrames = 20;
                hitboxShape = AttackSO.HitboxShape.Box;
                hitboxSize = new Vector3(1.5f, 1.5f, 2f);
                hitboxOffset = new Vector3(1.2f, 1f, 0f);
            }
        }
        
        static AttackSO CreateAttackSO(string folder, AttackData data)
        {
            string path = $"{folder}/{data.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackSO>(path);
            if (existing != null) return existing;
            
            var attack = ScriptableObject.CreateInstance<AttackSO>();
            attack.name = data.name;
            attack.attackId = data.name;
            attack.displayName = data.displayName;
            attack.startupFrames = data.startupFrames;
            attack.activeFrames = data.activeFrames;
            attack.recoveryFrames = data.recoveryFrames;
            attack.baseDamage = data.baseDamage;
            attack.baseKnockback = data.baseKnockback;
            attack.knockbackAngle = data.knockbackAngle;
            attack.knockbackGrowth = data.knockbackGrowth;
            attack.minKnockback = data.baseKnockbackMin;
            attack.maxKnockback = data.maxKnockback;
            attack.hitPauseFrames = (int)data.hitPauseFrames;
            attack.hitStunFrames = (int)data.hitStunFrames;
            attack.blockStunFrames = 10;
            attack.blockPushback = 2f;
            attack.hitboxShape = data.hitboxShape;
            attack.hitboxSize = data.hitboxSize;
            attack.hitboxOffset = data.hitboxOffset;
            attack.hitboxRotation = Vector3.zero;
            attack.meterGainOnHit = 5f;
            attack.meterGainOnWhiff = 1f;
            attack.meterCost = 0f;
            attack.minMeterRequired = 0f;
            attack.hitPauseFrames = 4;
            attack.hitStunFrames = 20;
            attack.blockStunFrames = 10;
            attack.blockPushback = 2f;
            attack.canBeParried = true;
            attack.perfectParryWindowFrames = 8;
            attack.perfectParryMeterReward = 15f;
            attack.hasArmor = false;
            attack.armorFrames = 0;
            attack.maxHits = 1;
            attack.multiHitIntervalFrames = 5;
            
            AssetDatabase.CreateAsset(attack, $"{folder}/{data.name}.asset");
            EditorUtility.SetDirty(attack);
            return attack;
        }
        
        [MenuItem("KyberClash/Data/Setup FormSOs")]
        public static void SetupFormSOs()
        {
            Debug.Log("=== Setting up FormSOs ===");
            
            string folder = "Assets/_Project/Data/Forms";
            EnsureFolder(folder);
            
            // Load all attacks
            var attacks = new Dictionary<string, AttackSO>();
            var guids = AssetDatabase.FindAssets("t:AttackSO", new[] { "Assets/_Project/Data/Attacks" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<AttackSO>(path);
                if (attack != null) attacks[attack.name] = attack;
            }
            
            var forms = new List<FormSetup>
            {
                new("Form_ShiiCho", "Form I: Shii-Cho", "The ancient foundation form. Balanced and adaptable.", 
                    FormSO.FormMechanicType.CounterStance, Color.blue,
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial",
                    "Attack_SideSpecial", "Attack_UpSpecial", "Attack_DownSpecial",
                    new FormSO.FormMechanicData 
                    { 
                        counterWindowFrames = 10f, counterMeterGain = 10f, 
                    },
                    1f, 1f, 1f, 1f, 1f,
                    1, true, 1f, false,
                    1f, 1f, 1f, 1f, 1f,
                    0f, 5f, 20f,
                    "Attack_Heavy", null),
                    
                new("Form_Makashi", "Form II: Makashi", "Dueling form. Precise parries, ripostes.",
                    FormSO.FormMechanicType.PrecisionParry, Color.cyan,
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial",
                    "Attack_SideSpecial", "Attack_UpSpecial", "Attack_DownSpecial",
                    new FormSO.FormMechanicData 
                    { 
                        precisionWindowFrames = 4f, 
                        riposteDamageMultiplier = 1.5f, 
                        riposteKnockbackMultiplier = 1.3f 
                    },
                    1.1f, 1.1f, 1f, 1f, 1.1f,
                    1, true, 0.8f, false,
                    1.2f, 1f, 1.1f, 1f, 1f,
                    0f, 5f, 25f,
                    null, null),
                    
                new("Form_Soresu", "Form III: Soresu", "Defensive form. Perfect parries reflect projectiles.",
                    FormSO.FormMechanicType.PerfectDeflection, Color.green,
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial",
                    "Attack_SideSpecial", "Attack_UpSpecial", "Attack_DownSpecial",
                    new FormSO.FormMechanicData 
                    { 
                        canDeflectProjectiles = true, deflectionAngle = 45f,
                    },
                    0.9f, 0.9f, 1f, 1.1f, 0.9f,
                    1, true, 1.2f, true,
                    0.9f, 0.9f, 0.9f, 1.2f, 0.8f,
                    2f, 3f, 30f,
                    null, "Attack_NeutralSpecial"),
                    
                new("Form_Ataru", "Form IV: Ataru", "Acrobatic form. Air mobility, combo extension.",
                    FormSO.FormMechanicType.AcrobaticFlow, Color.yellow,
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial",
                    "Attack_SideSpecial", "Attack_UpSpecial", "Attack_DownSpecial",
                    new FormSO.FormMechanicData 
                    { 
                        comboWindowExtension = 0.2f, 
                        maxAerialChains = 3, 
                        aerialMomentumPreservation = 0.8f 
                    },
                    1.2f, 1.3f, 1.2f, 0.9f, 1.2f,
                    2, true, 0.7f, true,
                    1f, 1f, 1.2f, 1f, 1f,
                    0f, 5f, 15f,
                    null, null),
                    
                new("Form_ShienDjemSo", "Form V: Shien/Djem So", "Power form. Counter-attacks, heavy hits.",
                    FormSO.FormMechanicType.PowerCounter, Color.red,
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial",
                    "Attack_SideSpecial", "Attack_UpSpecial", "Attack_DownSpecial",
                    new FormSO.FormMechanicData 
                    { 
                        counterAbsorbThreshold = 15f, 
                        counterDamageBonus = 1.5f, 
                        counterKnockbackBonus = 1.5f 
                    },
                    0.9f, 0.8f, 0.9f, 1.1f, 0.9f,
                    1, false, 1.5f, false,
                    1.5f, 1.3f, 0.8f, 0.8f, 1.2f,
                    0f, 8f, 15f,
                    null, null),
                    
                new("Form_Niman", "Form VI: Niman", "Balanced form. Force powers, utility.",
                    FormSO.FormMechanicType.ForceUtility, Color.magenta,
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial",
                    "Attack_SideSpecial", "Attack_UpSpecial", "Attack_DownSpecial",
                    new FormSO.FormMechanicData 
                    { 
                        forcePushCooldown = 5f, forcePushForce = 20f, 
                        forcePullRange = 10f, forcePullForce = 15f 
                    },
                    1f, 1f, 1f, 1f, 1f,
                    1, true, 1f, true,
                    1f, 1f, 1f, 1f, 1f,
                    1f, 5f, 20f,
                    null, null),
                    
                new("Form_JuyoVaapad", "Form VII: Juyo/Vaapad", "Aggressive form. High risk/reward, meter drain for power.",
                    FormSO.FormMechanicType.BerserkerTrance, Color.red,
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial",
                    "Attack_SideSpecial", "Attack_UpSpecial", "Attack_DownSpecial",
                    new FormSO.FormMechanicData 
                    { 
                        tranceMeterCostPerSecond = 10f, 
                        tranceDamageBonus = 2f, 
                        tranceSpeedBonus = 1.3f, 
                        tranceArmorThreshold = 20f 
                    },
                    1.1f, 1.1f, 1.1f, 1f, 1.1f,
                    1, true, 0.8f, false,
                    2f, 1.5f, 1.1f, 0.5f, 0.8f,
                    -2f, 10f, 10f,
                    null, null),
            };
            
            foreach (var f in forms)
            {
                CreateFormSO(folder, f, attacks);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("=== FormSOs Setup Complete ===");
        }
        
        struct FormSetup
        {
            public string name;
            public string displayName;
            public string lore;
            public FormSO.FormMechanicType mechanicType;
            public Color saberColor;
            public string light, heavy, upAir, downAir, forwardAir, backAir, neutralSpec, sideSpec, upSpec, downSpec;
            public FormSO.FormMechanicData mechanic;
            public float moveSpeed, airSpeed, jumpHeight, gravity, dashMult;
            public int maxAirJumps;
            public bool canAirDash;
            public float airDashCD;
            public bool canWallJump;
            public float dmgMult, kbMult, atkSpeed, meterGainMult, meterCostMult;
            public float passiveGain, hitGain, parryGain;
            // For mechanic data that references attacks by name
            public string counterAttackName;
            public string reflectedProjectileAttackName;
            
            public FormSetup(string n, string dn, string l, FormSO.FormMechanicType mt, Color sc,
                string light_, string heavy_, string upAir_, string downAir_, string forwardAir_, 
                string backAir_, string neutralSpec_, string sideSpec_, string upSpec_, string downSpec_,
                FormSO.FormMechanicData m,
                float ms, float airSpeed_, float jh, float g, float dm,
                int maj, bool cad, float cd, bool cwj,
                float dm_, float kb_, float atkSpeed_, float mgm, float mcm,
                float pg, float hg, float pg_,
                string counterAttackName_ = null,
                string reflectedProjectileAttackName_ = null)
            {
                name = n; displayName = dn; lore = l; mechanicType = mt; saberColor = sc;
                light = light_; heavy = heavy_; upAir = upAir_; downAir = downAir_; forwardAir = forwardAir_; backAir = backAir_;
                neutralSpec = neutralSpec_; sideSpec = sideSpec_; upSpec = upSpec_; downSpec = downSpec_; mechanic = m;
                moveSpeed = ms; airSpeed = airSpeed_; jumpHeight = jh; gravity = g; dashMult = dm;
                maxAirJumps = maj; canAirDash = cad; airDashCD = cd; canWallJump = cwj;
                dmgMult = dm_; kbMult = kb_; atkSpeed = atkSpeed_; meterGainMult = mgm; meterCostMult = mcm;
                passiveGain = pg; hitGain = hg; parryGain = pg_;
                counterAttackName = counterAttackName_;
                reflectedProjectileAttackName = reflectedProjectileAttackName_;
            }
        }
        
        static FormSO CreateFormSO(string folder, FormSetup setup, Dictionary<string, AttackSO> attacks)
        {
            string path = $"{folder}/{setup.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<FormSO>(path);
            if (existing != null) return existing;
            
            var form = ScriptableObject.CreateInstance<FormSO>();
            form.name = setup.name;
            form.formId = setup.name;
            form.displayName = setup.displayName;
            form.loreDescription = setup.lore;
            form.saberColor = setup.saberColor;
            form.saberCoreColor = Color.white;
            form.saberGradient = CreateSaberGradient(setup.saberColor);
            form.formVFXPrefab = null;
            form.saberMaterial = null;
            
            form.moveSpeedMultiplier = setup.moveSpeed;
            form.airSpeedMultiplier = setup.airSpeed;
            form.jumpHeightMultiplier = setup.jumpHeight;
            form.gravityMultiplier = setup.gravity;
            form.dashMultiplier = setup.dashMult;
            form.maxAirJumps = setup.maxAirJumps;
            form.canAirDash = setup.canAirDash;
            form.airDashCooldown = setup.airDashCD;
            form.canWallJump = setup.canWallJump;
            form.damageMultiplier = setup.dmgMult;
            form.knockbackMultiplier = setup.kbMult;
            form.attackSpeedMultiplier = setup.atkSpeed;
            form.meterGainMultiplier = setup.meterGainMult;
            form.meterCostMultiplier = setup.meterCostMult;
            form.lightAttack = GetAttack(setup.light, attacks);
            form.heavyAttack = GetAttack(setup.heavy, attacks);
            form.upAerial = GetAttack(setup.upAir, attacks);
            form.downAerial = GetAttack(setup.downAir, attacks);
            form.forwardAerial = GetAttack(setup.forwardAir, attacks);
            form.backAerial = GetAttack(setup.backAir, attacks);
            form.neutralSpecial = GetAttack(setup.neutralSpec, attacks);
            form.sideSpecial = GetAttack(setup.sideSpec, attacks);
            form.upSpecial = GetAttack(setup.upSpec, attacks);
            form.downSpecial = GetAttack(setup.downSpec, attacks);
            form.mechanicType = setup.mechanicType;
            
            // Copy mechanicData but replace counterAttack/reflectedProjectileAttack with loaded attacks
            var mechanicData = setup.mechanic;
            if (!string.IsNullOrEmpty(setup.counterAttackName)) {
                mechanicData.counterAttack = GetAttack(setup.counterAttackName, attacks);
            }
            if (!string.IsNullOrEmpty(setup.reflectedProjectileAttackName)) {
                mechanicData.reflectedProjectileAttack = GetAttack(setup.reflectedProjectileAttackName, attacks);
            }
            form.mechanicData = mechanicData;
            
            form.passiveMeterGain = setup.passiveGain;
            form.meterGainOnHit = setup.hitGain;
            form.meterGainOnPerfectParry = setup.parryGain;
            
            AssetDatabase.CreateAsset(form, $"{folder}/{setup.name}.asset");
            EditorUtility.SetDirty(form);
            return form;
        }
        
        static AttackSO GetAttack(string name, Dictionary<string, AttackSO> attacks)
        {
            if (attacks.TryGetValue(name, out var attack)) return attack;
            Debug.LogWarning($"Attack not found: {name}");
            return null;
        }
        
        static AttackSO LoadAttack(string name)
        {
            var path = $"Assets/_Project/Data/Attacks/{name}.asset";
            return AssetDatabase.LoadAssetAtPath<AttackSO>(path);
        }
        
        static Gradient CreateSaberGradient(Color color)
        {
            var gradient = new Gradient();
            gradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.5f),
                new GradientColorKey(color, 1f)
            };
            gradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            return gradient;
        }
        
        [MenuItem("KyberClash/Data/Create Characters")]
        public static void CreateCharacters()
        {
            Debug.Log("=== Creating Characters ===");
            
            string folder = "Assets/_Project/Data/Characters";
            EnsureFolder(folder);
            
            // Load forms
            var forms = new Dictionary<string, FormSO>();
            var formGuids = AssetDatabase.FindAssets("t:FormSO", new[] { "Assets/_Project/Data/Forms" });
            foreach (var guid in formGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var form = AssetDatabase.LoadAssetAtPath<FormSO>(path);
                if (form != null) forms[form.name] = form;
            }
            
            var characters = new List<CharacterSetup>
            {
                new("Character_JollyKnight", "Jolly Knight", "A balanced warrior of the Order. Mind the manure.",
                    CharacterData.WeightClass.Medium, LoadForm("Form_ShiiCho"), 
                    new[] { LoadForm("Form_ShiiCho"), LoadForm("Form_Makashi"), LoadForm("Form_Soresu"), LoadForm("Form_Ataru") },
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial", "Attack_SideSpecial",
                    "Attack_UpSpecial", "Attack_DownSpecial"),
                    
                new("Character_SithfullyYours", "Sithfully Yours", "A fallen knight consumed by darkness.",
                    CharacterData.WeightClass.Medium, LoadForm("Form_JuyoVaapad"),
                    new[] { LoadForm("Form_JuyoVaapad"), LoadForm("Form_ShienDjemSo"), LoadForm("Form_Makashi"), LoadForm("Form_Niman") },
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial", "Attack_SideSpecial",
                    "Attack_UpSpecial", "Attack_DownSpecial"),
                    
                new("Character_DuelistVale", "Duelist Vale", "Elegant fencer who strikes with surgical precision.",
                    CharacterData.WeightClass.Light, LoadForm("Form_Makashi"),
                    new[] { LoadForm("Form_Makashi"), LoadForm("Form_ShiiCho"), LoadForm("Form_Soresu"), LoadForm("Form_Niman") },
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial", "Attack_SideSpecial",
                    "Attack_UpSpecial", "Attack_DownSpecial"),
                    
                new("Character_GuardianKael", "Guardian Kael", "Unbreakable defender who turns offense into defense.",
                    CharacterData.WeightClass.Heavy, LoadForm("Form_Soresu"),
                    new[] { LoadForm("Form_Soresu"), LoadForm("Form_ShiiCho"), LoadForm("Form_Makashi"), LoadForm("Form_ShienDjemSo") },
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial", "Attack_SideSpecial",
                    "Attack_UpSpecial", "Attack_DownSpecial"),
                    
                new("Character_AcrobatZin", "Acrobat Zin", "Aerial artist who dances through combat with gravity-defying grace.",
                    CharacterData.WeightClass.Light, LoadForm("Form_Ataru"),
                    new[] { LoadForm("Form_Ataru"), LoadForm("Form_ShiiCho"), LoadForm("Form_Niman"), LoadForm("Form_Makashi") },
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial", "Attack_SideSpecial",
                    "Attack_UpSpecial", "Attack_DownSpecial"),
                    
                new("Character_WarlordDrav", "Warlord Drav", "Brute force incarnate, crushing foes with overwhelming power.",
                    CharacterData.WeightClass.Heavy, LoadForm("Form_ShienDjemSo"),
                    new[] { LoadForm("Form_ShienDjemSo"), LoadForm("Form_Soresu"), LoadForm("Form_JuyoVaapad"), LoadForm("Form_ShiiCho") },
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial", "Attack_SideSpecial",
                    "Attack_UpSpecial", "Attack_DownSpecial"),
                    
                new("Character_SageMira", "Sage Mira", "Balanced scholar-warrior blending martial and mystical arts.",
                    CharacterData.WeightClass.Medium, LoadForm("Form_Niman"),
                    new[] { LoadForm("Form_Niman"), LoadForm("Form_ShiiCho"), LoadForm("Form_Ataru"), LoadForm("Form_Makashi") },
                    "Attack_Light", "Attack_Heavy", "Attack_UpAir", "Attack_DownAir",
                    "Attack_ForwardAir", "Attack_BackAir", "Attack_NeutralSpecial", "Attack_SideSpecial",
                    "Attack_UpSpecial", "Attack_DownSpecial"),
            };
            
            foreach (var c in characters)
            {
                CreateCharacter(folder, c, (name, attacks) => LoadAttack(name));
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("=== Characters Created ===");
        }
        
        struct CharacterSetup
        {
            public string name;
            public string displayName;
            public string description;
            public CharacterData.WeightClass weight;
            public FormSO defaultForm;
            public FormSO[] availableForms;
            public string light, heavy, upAir, downAir, forwardAir, backAir, neutralSpec, sideSpec, upSpec, downSpec;
            
            public CharacterSetup(string n, string dn, string d, CharacterData.WeightClass w, FormSO df, FormSO[] af,
                string l, string h, string ua, string da, string fa, string ba, string ns, string ss, string us, string ds)
            {
                name = n; displayName = dn; description = d; weight = w; defaultForm = df; availableForms = af;
                light = l; heavy = h; upAir = ua; downAir = da; forwardAir = fa; backAir = ba;
                neutralSpec = ns; sideSpec = ss; upSpec = us; downSpec = ds;
            }
        }
        
        static void CreateCharacter(string folder, CharacterSetup setup, System.Func<string, Dictionary<string, AttackSO>, AttackSO> getAttack)
        {
            string path = $"Assets/_Project/Data/Characters/{setup.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            
            CharacterData character;
            if (existing != null)
            {
                character = existing;
            }
            else
            {
                character = ScriptableObject.CreateInstance<CharacterData>();
                character.name = setup.name;
                character.characterId = setup.name;
                character.displayName = setup.displayName;
                character.characterDescription = setup.description;
                character.weightClass = setup.weight;
                AssetDatabase.CreateAsset(character, path);
            }
            
            var serialized = new SerializedObject(character);
            serialized.FindProperty("defaultForm").objectReferenceValue = setup.defaultForm;
            serialized.FindProperty("availableForms").arraySize = setup.availableForms.Length;
            for (int i = 0; i < setup.availableForms.Length; i++)
            {
                serialized.FindProperty("availableForms").GetArrayElementAtIndex(i).objectReferenceValue = setup.availableForms[i];
            }
            serialized.FindProperty("defaultLightAttack").objectReferenceValue = LoadAttack(setup.light);
            serialized.FindProperty("defaultHeavyAttack").objectReferenceValue = LoadAttack(setup.heavy);
            serialized.FindProperty("defaultUpAerial").objectReferenceValue = LoadAttack(setup.upAir);
            serialized.FindProperty("defaultDownAerial").objectReferenceValue = LoadAttack(setup.downAir);
            serialized.FindProperty("defaultForwardAerial").objectReferenceValue = LoadAttack(setup.forwardAir);
            serialized.FindProperty("defaultBackAerial").objectReferenceValue = LoadAttack(setup.backAir);
            serialized.FindProperty("defaultNeutralSpecial").objectReferenceValue = LoadAttack(setup.neutralSpec);
            serialized.FindProperty("defaultSideSpecial").objectReferenceValue = LoadAttack(setup.sideSpec);
            serialized.FindProperty("defaultUpSpecial").objectReferenceValue = LoadAttack(setup.upSpec);
            serialized.FindProperty("defaultDownSpecial").objectReferenceValue = LoadAttack(setup.downSpec);
            
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(character);
        }
        
        static FormSO LoadForm(string name)
        {
            var path = $"Assets/_Project/Data/Forms/{name}.asset";
            return AssetDatabase.LoadAssetAtPath<FormSO>(path);
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
}