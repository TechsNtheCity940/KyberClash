namespace KyberClash.Fighters;

/// <summary>
/// Grey Knight - Neutral archetype that walks between light and dark sides
/// Class Role: Tank + DPS (Darkness cast by Light, flicker of Light within the Dark)  
/// Philosophy: Uses Light AND Dark as tools, balanced power wielding
/// Default Form: Form V (Djem So/Shien - Shien/Shien for blaster defense, DjemSo for saber offense)
/// Default Color: White (neutral), Grey or Black for fallen/light/dark balance users
/// </summary>
public class GreyKnight : LightsaberFighter
{
    // Grey Knight characteristics from lore: "Used Light AND Dark as tools, balanced approach"
    // Theme: "I serve the Republic with powers men call evil or good" - KOTOR character
    // Form flexibility due to neutral stance and ability to absorb Sith darkness
    
    public override void InitializeCharacterProperties()
    {
        base.InitializeCharacterProperties();
        
        this.name = "Grey Knight";
        this.archetypeRole = ClassRoles.Sentinel; // Tank + DPS hybrid role
        this.classAlignment = Alignment.Neutral/Sideless; // Neutral alignment
        
        // Grey Knight: balanced tank/defense with offensive capabilities
        this.maxHealth = 155f; // Above average health for tank role
        this.attackSpeedMultiplier = 1.0f; // Balanced - not fast but sustainable
        this.blockValue = 1.25f; // Strong defense like Guardian
        this.staminaRegenRate = 0.75f; // Good stamina management
        this.damageOutput = 3.4f; // Above average DPS
        
        // Grey philosophy: Light + Dark as tools, balanced combat approach
        this.combatPhilosophy = "Light and Darkness as Tools";
    }

    public override void SelectFighterClass(FighterForm formType)
    {
        this.formType = formType;
        
        // Grey Knight can use ANY form, but prefers V (Shien/DjemSo) for its defense-attack balance
        switch (formType)
        {
            case FormType.ShiiCho: 
                // Can start with basic Form I for beginners
                this.combatStyle = "Balanced Basic";
                break;
            case FormType.JuyoVaapad: 
                // Vaapad works - Grey can absorb Sith darkness and control it
                // Juyo requires more skill but possible
                this.combatStyle = "Dual-Sided Mastery";
                break;
            default:
                // Most Grey Knights prefer form V for its balance
                this.combatStyle = "Form Five Balance";
                break;
        }
    }

    protected override void LoadFighterAppearance(string color, string armorStyle)
    {
        base.LoadFighterAppearance(color, armorStyle);
        
        // Grey Knight visual: dark grey/black robes with light (white/grey) trim, sometimes purple
        if (!string.IsNullOrEmpty(color))
        {
            this.bladeColor = color;
        }
        else
        {
            this.bladeColor = "white"; // Default neutral/Grey
        }

        this.armorStyle = armorStyle;
        this.combatAnimations = CombatAnimationType.SaberCombo + CombatAnimationType.NeutralParry;
    }

    public override void RegisterCombatMechanics(LightsaberSystem lightsaber)
    {
        // Grey Knight: unique ability to channel both light and dark through blade
        base.RegisterCombatMechanics(lightsaber);
        
        this.ultimateMoveName = "Grey Flash";
        this.ultimateMoveDescription = "Channel both light and dark together for rapid successive strikes with 50% chance to mirror opponent's darkness back";
        this.ultimateMoveCooldown = 18f;
        
        // Grey Knight: unique mechanic - absorbs dark side power while maintaining light side control
        var abilities = new List<CombatAbility>
        {
            new CombatAbility("Dual-Blade Channel", 2.5f, "Use both light and dark as combined energy for attacks"),
            new CombatAbility("Absorb Dark Side", 1.8f, "Draw dark power from Sith opponent to increase own blade pressure")
        };
        
        abilities.ForEach(skill => lightsaber.AddCombatAbility(skill));
    }

    public override List<string> GetCharacterFlavorNotes()
    {
        return new List<string>()
        {
            "\"I wield Light and Dark as tools.\"", // Classic Grey Knight quote style per character
            "Balanced aggression with Force control and lightsaber skill",
            "Can channel Sith darkness without falling to dark side"
        };
    }

    public override List<StarWarsQuote> GetCharacterQuotes()
    {
        return new List<StarWarsQuote>()
        {
            new StarWarsQuote { name = "Sith vs Jedi", text = "\"They only see things in shades of black and white, but I see things in shades...\""},
            new StarWarsQuote { name = "Grey Knight Philosophy", text = "\"I am the furtive Darkness cast by Light, and flicker of Light within the Dark.\""} // KOTOR theme
        };
    }

    public override void ApplyCharacterCustomization(LightsaberSetup setup)
    {
        base.ApplyCharacterCustomization(setup);
        
        // Grey Knight: neutral blades (white/grey/black/purple), form flexibility
        if (setup.customColor) 
            this.bladeColor = "black"; // Dark side lean variant
        
        this.combatStance = CombatStance.Balanced;
    }

    protected override void ValidateCharacter(FighterRegistry registry)
    {
        base.ValidateCharacter(registry);
        
        if (!registry.CheckClassRole(this.name, ClassRoles.Sentinel))
            throw new ArgumentException("Sentinel role not found in character roster");
    }

    public override List<string> GetUniqueCharacteristics()
    {
        return new List<string>(){"Light/Dark Channeling", "Absorb Opposition Power", "Form Flexibility"};
    }
}
