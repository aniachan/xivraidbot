using System;

namespace XIVRaidBot.Models;

/// <summary>
/// Stores user-specific settings across the application
/// </summary>
public class UserSettings
{
    /// <summary>
    /// The unique identifier for this settings record
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The Discord user ID
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// The user's preferred timezone (e.g., "Europe/Stockholm", "America/New_York")
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;
    
    /// <summary>
    /// When the user's settings were last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}