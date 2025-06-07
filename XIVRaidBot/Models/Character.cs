using System.Collections.Generic;

namespace XIVRaidBot.Models;

public enum JobRole
{
    Tank,
    Healer,
    DPS
}

public enum JobType
{
    // Tanks
    PLD, // Paladin
    WAR, // Warrior
    DRK, // Dark Knight
    GNB, // Gunbreaker
    
    // Healers
    WHM, // White Mage
    SCH, // Scholar
    AST, // Astrologian
    SGE, // Sage
    
    // DPS - Melee
    MNK, // Monk
    DRG, // Dragoon
    NIN, // Ninja
    SAM, // Samurai
    RPR, // Reaper
    
    // DPS - Ranged Physical
    BRD, // Bard
    MCH, // Machinist
    DNC, // Dancer
    
    // DPS - Casters
    BLM, // Black Mage
    SMN, // Summoner
    RDM  // Red Mage
}

public class Character
{
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;
    public string? LodestoneId { get; set; }
    public JobType PreferredJob { get; set; }
    public List<JobType> SecondaryJobs { get; set; } = new List<JobType>();
    
    // Navigation properties
    public virtual ICollection<RaidComposition> RaidCompositions { get; set; } = new List<RaidComposition>();
}