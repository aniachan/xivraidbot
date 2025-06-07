using System;

namespace XIVRaidBot.Models;

public enum AttendanceStatus
{
    Pending,
    Confirmed,
    Declined,
    BenchRequested,
    OnBench
}

public class RaidAttendance
{
    public int Id { get; set; }
    public int RaidId { get; set; }
    public ulong UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Pending;
    public DateTime? ResponseTime { get; set; }
    public string? Note { get; set; }
    
    // Navigation property
    public virtual Raid Raid { get; set; } = null!;
}