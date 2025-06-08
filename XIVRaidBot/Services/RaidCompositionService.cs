using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Data;
using XIVRaidBot.Models;

namespace XIVRaidBot.Services;

public class RaidCompositionService
{
    private readonly RaidBotContext _context;
    private readonly JobIconService _jobIconService;
    
    // Event to notify that a raid composition has been updated
    public event Func<int, Task>? RaidCompositionChanged;
    
    public RaidCompositionService(RaidBotContext context, JobIconService jobIconService)
    {
        _context = context;
        _jobIconService = jobIconService;
    }
    
    public async Task<Character> RegisterCharacterAsync(ulong userId, string characterName, string world, JobType preferredJob, List<JobType>? secondaryJobs = null)
    {
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CharacterName == characterName);
            
        if (character == null)
        {
            character = new Character
            {
                UserId = userId,
                CharacterName = characterName,
                World = world,
                PreferredJob = preferredJob,
                SecondaryJobs = secondaryJobs ?? new List<JobType>()
            };
            
            _context.Characters.Add(character);
        }
        else
        {
            character.PreferredJob = preferredJob;
            if (secondaryJobs != null)
            {
                character.SecondaryJobs = secondaryJobs;
            }
        }
        
        await _context.SaveChangesAsync();
        return character;
    }
    
    public async Task<Character?> GetCharacterAsync(int characterId)
    {
        return await _context.Characters.FindAsync(characterId);
    }
    
    public async Task<List<Character>> GetUserCharactersAsync(ulong userId)
    {
        return await _context.Characters
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }
    
    public async Task<RaidComposition?> AssignJobToRaidAsync(int raidId, int characterId, JobType jobType, string? role = null)
    {
        var character = await _context.Characters.FindAsync(characterId);
        if (character == null) return null;
        
        var raid = await _context.Raids.FindAsync(raidId);
        if (raid == null) return null;
        
        var composition = await _context.RaidCompositions
            .FirstOrDefaultAsync(c => c.RaidId == raidId && c.CharacterId == characterId);
            
        if (composition == null)
        {
            composition = new RaidComposition
            {
                RaidId = raidId,
                CharacterId = characterId,
                AssignedJob = jobType,
                Role = role
            };
            
            _context.RaidCompositions.Add(composition);
        }
        else
        {
            composition.AssignedJob = jobType;
            composition.Role = role ?? composition.Role;
        }
        
        // Update the attendance status to confirmed for this user
        var attendance = await _context.RaidAttendances
            .FirstOrDefaultAsync(a => a.RaidId == raidId && a.UserId == character.UserId);
            
        if (attendance == null)
        {
            _context.RaidAttendances.Add(new RaidAttendance
            {
                RaidId = raidId,
                UserId = character.UserId,
                UserName = character.CharacterName,
                Status = AttendanceStatus.Confirmed,
                ResponseTime = DateTime.UtcNow
            });
        }
        else if (attendance.Status != AttendanceStatus.Confirmed)
        {
            attendance.Status = AttendanceStatus.Confirmed;
            attendance.ResponseTime = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        
        // Notify subscribers that raid composition has changed
        if (RaidCompositionChanged != null)
        {
            await RaidCompositionChanged.Invoke(raidId);
        }
        
        return composition;
    }
    
    public async Task<bool> RemoveJobAssignmentAsync(int raidId, int characterId)
    {
        var composition = await _context.RaidCompositions
            .FirstOrDefaultAsync(c => c.RaidId == raidId && c.CharacterId == characterId);
            
        if (composition == null) return false;
        
        _context.RaidCompositions.Remove(composition);
        await _context.SaveChangesAsync();
        
        // Notify subscribers that raid composition has changed
        if (RaidCompositionChanged != null)
        {
            await RaidCompositionChanged.Invoke(raidId);
        }
        
        return true;
    }
    
    public async Task<Dictionary<JobRole, int>> GetRaidRoleCounts(int raidId)
    {
        var compositions = await _context.RaidCompositions
            .Where(c => c.RaidId == raidId)
            .ToListAsync();
            
        var counts = new Dictionary<JobRole, int>
        {
            { JobRole.Tank, 0 },
            { JobRole.Healer, 0 },
            { JobRole.DPS, 0 }
        };
        
        foreach (var comp in compositions)
        {
            var role = GetRoleFromJobType(comp.AssignedJob);
            counts[role] = counts[role] + 1;
        }
        
        return counts;
    }
    
    public async Task<bool> IsValidPartyComposition(int raidId)
    {
        var counts = await GetRaidRoleCounts(raidId);
        
        // Standard party composition: 2 tanks, 2 healers, 4 DPS
        return counts[JobRole.Tank] == 2 &&
               counts[JobRole.Healer] == 2 &&
               counts[JobRole.DPS] == 4;
    }
    
    public string GetJobIconMarkdown(JobType jobType)
    {
        return $"[{jobType}]({_jobIconService.GetJobIconUrl(jobType)})";
    }
    
    public string GetJobEmoji(JobType jobType)
    {
        return _jobIconService.GetJobEmoji(jobType);
    }
    
    private JobRole GetRoleFromJobType(JobType jobType)
    {
        return jobType switch
        {
            JobType.PLD or JobType.WAR or JobType.DRK or JobType.GNB => JobRole.Tank,
            JobType.WHM or JobType.SCH or JobType.AST or JobType.SGE => JobRole.Healer,
            _ => JobRole.DPS
        };
    }
}