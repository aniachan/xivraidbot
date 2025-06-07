using System;
using System.Collections.Generic;

namespace XIVRaidBot.Models;

public class Raid
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong ReminderMessageId { get; set; }
    
    // Navigation properties
    public virtual ICollection<RaidAttendance> Attendees { get; set; } = new List<RaidAttendance>();
    public virtual ICollection<RaidComposition> Compositions { get; set; } = new List<RaidComposition>();
}