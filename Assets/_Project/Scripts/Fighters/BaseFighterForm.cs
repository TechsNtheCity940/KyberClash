#pragma warning disable CS0649 // Variable is never assigned - allowed by design for form-specific behavior
#pragma warning disable CS1578 // Invalid reference to missing type - Unity will auto-resolve these as we run

/// <summary>
/// Form I: Shii-Cho (Determinations Form)
/// Oldest lightsaber combat style derived from traditional swordsmanship
/// Focus on disarming, simple strikes, crowd control
/// Padawan training standard - basic but effective
/// </summary>
using UnityEngine;
using System.Collections.Generic;

namespace KyberClash.FighterForms;

[RunTime]
public class FormILighterForm : BaseFighterForm
{
    // Form I characteristics: angular sweeping attacks, disarming focus
    // Basic strike patterns with minimal acrobatic complexity
    protected override List<CombatSkill> GetDefaultSkills()
    {
        return new List<CombatSkill>()
        {
            new CombatSkill("Sweeping Strike", 2.5f, "Angular blade rotation targeting weak zones"),
            new CombatSkill("Disarm Attempt", 1.8f, "Force-pushed parry designed to unbalance opponent"),
            new CombatSkill("Basic Parry", 1.5f, "Standard lightsaber block - low Risk/medium effect"),
            new CombatSkill("Rapid Slash", 3.2f, "Fast consecutive strikes with minimal recovery"),
            new CombatSkill("Defensive Guard", 1.0f, "Simple forward-facing blade position"),
            new CombatSkill("Force Push", 2.0f, "Standard Force-boosted thrust for distance")
        };
    }

    public override float GetCombatEfficiencyMultiplier()
    {
        return 1.0f; // Balanced efficiency - good for training/form I basics
    }

    public override void UpdateFormStats(Fighter fighter)
    {
        // Form I is beginner-friendly: simpler patterns, less risk of over-extension
        base.UpdateFormStats(fighter);
        fighter.maxHealth = 150f;
        fighter.energyRegenRate = 0.75f; // Slower regen - requires technique consistency
    }

    public override List<string> GetCombatStyleKeywords()
    {
        return new List<string>() {"Simple", "Balanced", "Disarming", "Angular", "Crowd Control"};
    }

    public override bool IsTraditionalJediCompatible => true;
}

/// <summary>
/// Form II: Makashi (Contentions Form)
/// Elegant, graceful, fencing-like precision duelist style
/// Counter to Form I's weaknesses - lightsaber vs lightsaber optimization
/// Single opponent focus, NOT suited for multiple foes or blasters
/// </summary>
public class FormIISaberForm : BaseFighterForm
{
    // Makashi characteristics: parry-then-parry, precise thrusts, linear footwork
    protected override List<CombatSkill> GetDefaultSkills()
    {
        return new List<CombatSkill>()
        {
            new CombatSkill("Parsley Point", 2.8f, "Quick parry-turn offense - feint into counterstrike"),
            new CombatSkill("Light Cut", 1.6f, "Minimal blade rotation for rapid engagement/disengagement"),
            new CombatSkill("Precision Thrust", 3.5f, "Focused penetrating attack at opponent's center line"),
            new CombatSkill("Elegant Parry", 2.0f, "High-precision blade deflection against saber users"),
            new CombatSkill("Feint Combo", 2.6f, "Series of light strikes testing timing for real impact"),
            new CombatSkill("One-Handed Focus", 3.8f, "Makashi duelists typically operate with single hand")
        };
    }

    public override float GetCombatEfficiencyMultiplier()
    {
        return 0.85f; // Slightly less damage but higher precision output
    }

    public override void UpdateFormStats(Fighter fighter)
    {
        // Form II excels in 1-on-1 duels, weak against multiple opponents
        base.UpdateFormStats(fighter);
        fighter.speedMultiplier = 1.05f; // Fencers are generally quick
        fighter.blockValue = 1.3f; // Higher parry efficiency due to Makashi precision focus
    }

    public override List<string> GetCombatStyleKeywords()
    {
        return new List<string>() {"Precision", "Fencing", "One-on-One", "Elegant", "Disarm-Counter"};
    }

    public override bool IsSithCompatible => false; // Not typical Sith style - they prefer Juyo form
}

/// <summary>
/// Form III: Soresu (Resilience Form)
/// Defensive master - tight movements, strict economy of action, tire enemies out
/// Response to blaster prevalence maxing lightsaber blade deflection capabilities
/// Multiple enemy engagement focus with distinct opening stance
/// </summary>
public class FormIIISoresuForm : BaseFighterForm
{
    // Soresu: economical defense that exhausts opponents through consistent pressure
    protected override List<CombatSkill> GetDefaultSkills()
    {
        return new List<CombatSkill>()
        {
            new CombatSkill("Defensive Stance", 1.8f, "Distinctive opening position - blade forward, body angled for maximum defense"),
            new CombatSkill("Blade Economy Parry", 2.3f, "Tight movements minimizing wasted energy"),
            new CombatSkill("Exhaustion Strike", 2.9f, "Patient waiting for opponent to tire before counterattack"),
            new CombatSkill("Multi-Enemy Grid", 3.1f, "Soresu excels at creating defensive perimeter around multiple foes"),
            new CombatSkill("Blaster Deflection", 3.8f, "Primary skill - maximum lightsaber blade angle vs ranged attacks"),
            new CombatSkill("Angle Control", 2.7f, "Maintain optimal angles against all attackers simultaneously")
        };
    }

    public override float GetCombatEfficiencyMultiplier()
    {
        return 1.15f; // Better defense/survival - trade speed for endurance
    }

    public override void UpdateFormStats(Fighter fighter)
    {
        // Soresu emphasizes defense and blaster countering over pure attack speed
        base.UpdateFormStats(fighter);
        fighter.defenseValue = 1.5f;
        fighter.staminaRegenRate = 0.8f; // Slower but more sustainable combat pace
    }

    public override List<string> GetCombatStyleKeywords()
    {
        return new List<string>() {"Defense", "Multi-Enemy", "Blaster Counter", "Economy", "Endurance"};
    }

    public override bool IsJediCompatible => true;
}

/// <summary>
/// Form VII: Juyo/Vaapad (Ferocity Form)
/// Most aggressive/exclusive style - forbidden by Jedi Council historically
/// Juyo variant: rage-fueled chaos, pure attack offense with minimal defense
/// Vaapad variant (Mace Windu's): controlled power draw, channel anger/passion feedback
/// </summary>
public class FormVIIAttackStyle : BaseFighterForm
{
    // Juyo characteristics: unpredictable, emotional assault, dark side-aligned fighting
    // Vaapad characteristics: structured aggression requiring opponent + user cooperation through darkness
    protected override List<CombatSkill> GetDefaultSkills()
    {
        return new List<CombatSkill>()
        {
            new CombatSkill("Frenzied Assault", 3.6f, "Juyo-style rapid-fire strikes with high risk of overshooting parry"),
            new CombatSkill("Dark Channel Strike", 2.5f, "Vaapad - channel opponent's darkness to amplify your own blade force"),
            new CombatSkill("Chaotic Maneuver", 4.1f, "Unpredictable movement patterns breaking standard fencing rhythm"),
            new CombatSkill("Power Feedback Loop", 3.9f, "Channel anger through lightsaber to create feedback pressure on opponent"),
            new CombatSkill("Frenzied Blade Spin", 3.3f, "Blade rotating for maximum speed/damage output (higher Risk of error)"),
            new CombatSkill("Jedi Darkness Strike", 3.2f, "Vaapad variant requires channeling anger through blade into opponent")
        };
    }

    public override float GetCombatEfficiencyMultiplier()
    {
        return 1.4f; // Maximum damage output but highest skill requirement
    }

    public override void UpdateFormStats(Fighter fighter)
    {
        // Form VII is exclusive - requires emotional control to prevent falling to dark side
        base.UpdateFormStats(fighter);
        fighter.attackSpeedMultiplier = 1.4f;
        fighter.complianceRisk = 0.65f; // Risk of Juyo overextension/dark side corruption
    }

    public override List<string> GetCombatStyleKeywords()
    {
        return new List<string>() {"Ferocity", "Aggressive", "Dark-Side", "Unpredictable", "Exclusive"};
    }

    public override bool IsJediCompatible => true; // Both Juyo and Vaapad variants were eventually allowed for select Masters
}

