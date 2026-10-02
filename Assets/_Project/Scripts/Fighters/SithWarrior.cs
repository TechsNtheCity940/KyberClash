namespace KyberClash.Fighters;

/// <summary>
/// Sith Warrior - Dark Side archetype  
/// Class Role: Damage-Dealer (Juggernaut/Immortal) or Vigilance
/// Philosophy: Passion and Strength, anger manipulation, fear/aggression
/// Default Form: Juyo/Vaapad (aggressive assault style favored by Sith)
/// Default Color: Red (canonical Sith hue - passion/strength alignment)
/// </summary>
public class SithWarrior : LightsaberFighter
{
    // Sith characteristics: pure DPS, emotional channeling through lightsaber combat
    public override void InitializeCharacterProperties()
    {
        base.InitializeCharacterProperties();
        
        this.name = "Sith Warrior";
        this.archetypeRole = ClassRoles.DPS;
        this.classAlignment = Alignment.Evil/DarkSide;
        
        // Sith: pure damage output, high aggression threshold
        this.maxHealth = 140f;
        this.attackSpeedMultiplier = 1.2f;
        this.blockValue = 0.9f; // Lower defense - trust in offensive pressure
        this.staminaRegenRate = 0.5f; // Slower regen, need to keep attacking
        this.damageOutput = 4.5f; // Highest damage output class
        
        // Dark side philosophy: channeling passion and aggression through combat
        this.combatPhilosophy = "Passion Through Aggression";
    }

    public override void SelectFighterClass(FighterForm formType)
    {
        this.formType = formType;
        
        // Sith prefers Juyo for pure aggressive assault, Vaapad optional for masters
        switch (formType)
        {
            case FormType.JuyoVaapad: 
                // Standard Sith preference - Juyo
                this.combatStyle = "Aggressive Assault";
                break;
            default:
                // Non-Juyo forms not favored by typical Sith combat style
                this.combatStyle = "Dark Side Pressure";
                break;
        }
    }

    protected override void LoadFighterAppearance(string color, string armorStyle)
    {
        base.LoadFighterAppearance(color, armorStyle);
        
        // Sith visual: black/dark grey robes with crimson red accent, sometimes dark purple
        if (!string.IsNullOrEmpty(color))
        {
            this.bladeColor = color;
        }
        else
        {
            this.bladeColor = "red"; // Default Sith
        }

        this.armorStyle = armorStyle;
        this.combatAnimations = CombatAnimationType.SaberSlash + CombatAnimationType.DarkSideCharge;
    }

    public override void RegisterCombatMechanics(LightsaberSystem lightsaber)
    {
        // Standard Sith: aggressive pressure, emotional amplification techniques
        base.RegisterCombatMechanics(lightsaber);
        
        this.ultimateMoveName = "Darkness Unleashed";
        this.ultimateMoveDescription = "Draw dark side rage into weapon, unleash destructive combo for 3 seconds, then enter cooldown";
        this.ultimateMoveCooldown = 25f; // Longer cooldown due to high risk
        
        // Sith uses lightsaber to channel pain/anger - unique mechanic
        var abilities = new List<CombatAbility>
        {
            new CombatAbility("Dark Rage", 1.5f, "Temporarily boost damage output by absorbing opponent's aggression"),
            new CombatAbility("Pressure Strike", 2.1f, "High-pressure attacks that force opponents back")
        };
        
        abilities.ForEach(skill => lightsaber.AddCombatAbility(skill));
    }

    public override List<string> GetCharacterFlavorNotes()
    {
        return new List<string>()
        {
            "\"There is no escape!\"", // Classic quote style per character
            "Passion and Strength define Sith philosophy",
            "Anger can be harnessed through the lightsaber blade"
        };
    }

    public override List<StarWarsQuote> GetCharacterQuotes()
    {
        return new List<StarWarsQuote>()
        {
            new StarWarsQuote { name = "Darth Maul", text = "\"I'll be back.\""},
            new StarWarsQuote { name = "Darth Vader", text = "\"I've got a bad feeling about this.\""},
            new StarWarsQuote { name = "Count Dooku", text = "\"The force does not love the weak.\""} // Quote about Force power vs weakness
        };
    }

    public override void ApplyCharacterCustomization(LightsaberSetup setup)
    {
        base.ApplyCharacterCustomization(setup);
        
        // Sith can customize blade color (red/blue/black for dark side users)
        if (setup.customColor) 
            this.bladeColor = "blue"; // Alternative red variant

        this.combatStance = CombatStance.Aggressive;
    }

    protected override void ValidateCharacter(FighterRegistry registry)
    {
        base.ValidateCharacter(registry);
        
        if (!registry.CheckClassRole(this.name, ClassRoles.DPS))
            throw new ArgumentException("DPS role not found in character roster");
    }

    public override List<string> GetUniqueCharacteristics()
    {
        return new List<string>(){"Aggressive Pressure", "Emotional Channeling", "Damage Amplification"};
    }
}
