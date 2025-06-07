namespace XIVRaidBot.Models;

public class RaidComposition
{
    public int Id { get; set; }
    public int RaidId { get; set; }
    public int CharacterId { get; set; }
    public JobType AssignedJob { get; set; }
    public string? Role { get; set; } // Additional role info (e.g., Main Tank, Off-tank, Main Healer, etc.)
    
    // Navigation properties
    public virtual Raid Raid { get; set; } = null!;
    public virtual Character Character { get; set; } = null!;
}