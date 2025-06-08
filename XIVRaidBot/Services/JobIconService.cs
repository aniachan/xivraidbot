using System.Collections.Generic;
using XIVRaidBot.Models;

namespace XIVRaidBot.Services;

/// <summary>
/// Service for providing FFXIV job icons for use in Discord embeds
/// </summary>
public class JobIconService
{
    private const string BaseIconUrl = "https://raw.githubusercontent.com/aniachan/xivraidbot/refs/heads/main/XIVRaidBot/Resources/job_icons/";

    private readonly Dictionary<JobType, string> _jobIconMap;

    public JobIconService()
    {
        _jobIconMap = new Dictionary<JobType, string>
        {
            // Tanks
            { JobType.PLD, "paladin.png" },
            { JobType.WAR, "warrior.png" },
            { JobType.DRK, "darkknight.png" },
            { JobType.GNB, "gunbreaker.png" },
            
            // Healers
            { JobType.WHM, "whitemage.png" },
            { JobType.SCH, "scholar.png" },
            { JobType.AST, "astrologian.png" },
            { JobType.SGE, "sage.png" },
            
            // DPS - Melee
            { JobType.MNK, "monk.png" },
            { JobType.DRG, "dragoon.png" },
            { JobType.NIN, "ninja.png" },
            { JobType.SAM, "samurai.png" },
            { JobType.RPR, "reaper.png" },
            
            // DPS - Ranged Physical
            { JobType.BRD, "bard.png" },
            { JobType.MCH, "machinist.png" },
            { JobType.DNC, "dancer.png" },
            
            // DPS - Casters
            { JobType.BLM, "blackmage.png" },
            { JobType.SMN, "summoner.png" },
            { JobType.RDM, "redmage.png" }
        };
    }

    /// <summary>
    /// Gets the full URL to the job icon for a specific job
    /// </summary>
    /// <param name="jobType">The FFXIV job type</param>
    /// <returns>The URL to the job icon</returns>
    public string GetJobIconUrl(JobType jobType)
    {
        if (_jobIconMap.TryGetValue(jobType, out string? iconFileName))
        {
            return BaseIconUrl + iconFileName;
        }
        
        // Return a default icon if the job is not found
        return BaseIconUrl + "default.png";
    }

    /// <summary>
    /// Gets an emoji representation of a job for use in plain text
    /// </summary>
    /// <param name="jobType">The FFXIV job type</param>
    /// <returns>A text emoji representation of the job</returns>
    public string GetJobEmoji(JobType jobType)
    {
        return jobType switch
        {
            // Tanks
            JobType.PLD => "🛡️",
            JobType.WAR => "🛡️",
            JobType.DRK => "🛡️",
            JobType.GNB => "🛡️",
            
            // Healers
            JobType.WHM => "💚",
            JobType.SCH => "💚",
            JobType.AST => "💚",
            JobType.SGE => "💚",
            
            // DPS
            JobType.MNK => "⚔️",
            JobType.DRG => "⚔️",
            JobType.NIN => "⚔️",
            JobType.SAM => "⚔️",
            JobType.RPR => "⚔️",
            JobType.BRD => "🏹",
            JobType.MCH => "🏹",
            JobType.DNC => "🏹",
            JobType.BLM => "🔮",
            JobType.SMN => "🔮",
            JobType.RDM => "🔮",
            
            // Default
            _ => "❓"
        };
    }
}