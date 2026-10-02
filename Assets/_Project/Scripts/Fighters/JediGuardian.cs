namespace KyberClash.Fighters;

/// <summary>
/// Jedi Guardian - Standard Jedi Knight archetype
/// Class Role: DPS + Tank (Guardian/Vigilance)
/// Philosophy: Non-attachment, arbitration, Force integration
/// Default Form: Form III (Soresu - balance between offense/defense)
/// Default Color: Blue (canonical Jedi hue), also Green
/// </summary>
public class JediGuardian : LightsaberFighter
{
    // Guardian role characteristics from Star Wars lore
    public override void InitializeCharacterProperties()
    {
        base.InitializeCharacterProperties();
        
        this.name = "Jedi Guard";
        this.archetypeRole = ClassRoles.Guardian;
        this.classAlignment = Alignment.Good/LightSide;
        
        // Standard Jedi Guardian: slightly tank-oriented with strong defense
        this.maxHealth = 160f;
        this.attackSpeedMultiplier = 0.95f;
        this.blockValue = 1.25f;
        this.staminaRegenRate = 0.7f;
        this.damageOutput = 3.2f;
        this.speedMultiplier = 0.85f; // Slightly slower due to defensive posturing
        
        // Jedi philosophy: non-attachment means controlled aggression
        this.combatPhilosophy = "Arbitration and Balance";
    }

    public override void SelectFighterClass(FighterForm formType)
    {
        this.formType = formType;
        
        // Guardian role has strong preferences across forms (especially III, V, Niman)
        switch (formType)
        {
            case FormType.JuyoVaapad: 
                // Vaapad variant only for elite masters, Juyo rare
                this.combatStyle = "Aggressive Mastery";
                break;
            default:
                this.combatStyle = "Balanced Defense-Offense";
                break;
        }
    }

    protected override void LoadFighterAppearance(string color, string armorStyle)
    {
        // Jedi Guardian - typically blue or green lightsaber
        base.LoadFighterAppearance(color, armorStyle);
        
        // Jedi visual style: white robes (sometimes brown/grey for older masters), blue/green/hud overlay
        if (!string.IsNullOrEmpty(color))
        {
            this.bladeColor = color;
        }
        else
        {
            this.bladeColor = "blue"; // Default Jedi
        }

        this.armorStyle = armorStyle;
        this.combatAnimations = CombatAnimationType.SaberSwing + CombatAnimationType.DefensivePolearm;
    }

    public override void RegisterCombatMechanics(LightsaberSystem lightsaber)
    {
        // Standard Jedi Guardian: Force integration + defensive focus
        base.RegisterCombatMechanics(lightsaber);
        
        this.ultimateMoveName = "Jedi Defend";
        this.ultimateMoveDescription = "Create an energy shield that absorbs damage for 4 seconds, then unleash a counter-strike";
        this.ultimateMoveCooldown = 20f;
        
        // Force powers integration - Jedi can use lightspeed blaster deflector, energy shields, and telekinesis
        var forceSkills = new List<CombatAbility>
        {
            new CombatAbility("Jedi Teleport", 3.0f, "Instantly move to any visible location within sight"),
            new CombatAbility("Force Deflect Blasters", 1.8f, "Dodge incoming ranged attacks automatically"),
            new CombatAbility("Lightspeed Saber Strike", 3.5f, "Move forward with lightspeed speed")
        };
        
        forceSkills.ForEach(skill => lightsaber.AddForceSkill(skill));
    }

    public override List<string> GetCharacterFlavorNotes()
    {
        return new List<string>()
        {
            "\"The Force is in motion.\"", // Quote style per character
            "Guardians protect the Republic by upholding Jedi ideals",
            "Balance between aggression and restraint defines Jedi philosophy"
        };
    }

    public override List<StarWarsQuote> GetCharacterQuotes()
    {
        return new List<StarWarsQuote>()
        {
            new StarWarsQuote { name = "Yoda (Mentor Reference)", text = "\"Do or do not. There is no try.\""},
            new StarWarsQuote { name = "Darth Vader", text = "\"I'll be back.\""},
            new StarWarsQuote { name = "Obi-Wan Kenobi", text = "\"Never tell me the odds\""} // Classic quote, used as flavor text for Guardian type
        };
    }

    public override void ApplyCharacterCustomization(LightsaberSetup setup)
    {
        base.ApplyCharacterCustomization(setup);
        
        // Jedi can customize blade color (blue/green/purple for Force-sensitive users)
        if (setup.customColor) setup.bladeColor = "green"; // Default Guardian preference
        this.combatStance = CombatStance.Defensive;
    }

    protected override void ValidateCharacter(FighterRegistry registry)
    {
        // Verify character is registered properly for Jedi Guardian role
        base.ValidateCharacter(registry);
        
        if (!registry.CheckClassRole(this.name, ClassRoles.Guardian))
            throw new ArgumentException("Guardian role not found in character roster");
    }

    public override List<string> GetUniqueCharacteristics()
    {
        // Jedi Guardians have distinctive combat profile: good defense + decent offense
        return new List<string>(){"Force Integration", "Combat Shielding", "Jedi Teleport", "Lightspeed Movement"};
    }
}
