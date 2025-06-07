using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Data;
using XIVRaidBot.Models;

namespace XIVRaidBot.Services;

public class AttendanceService
{
    private readonly RaidBotContext _context;
    private readonly RaidService _raidService;
    
    public AttendanceService(RaidBotContext context, RaidService raidService)
    {
        _context = context;
        _raidService = raidService;
    }
    
    public async Task<RaidAttendance> UpdateAttendanceStatusAsync(int raidId, ulong userId, string userName, AttendanceStatus status, string? note = null)
    {
        var attendance = await _context.RaidAttendances
            .FirstOrDefaultAsync(a => a.RaidId == raidId && a.UserId == userId);
            
        if (attendance == null)
        {
            attendance = new RaidAttendance
            {
                RaidId = raidId,
                UserId = userId,
                UserName = userName,
                Status = status,
                ResponseTime = DateTime.Now,
                Note = note
            };
            
            _context.RaidAttendances.Add(attendance);
        }
        else
        {
            attendance.Status = status;
            attendance.ResponseTime = DateTime.Now;
            attendance.Note = note ?? attendance.Note;
        }
        
        await _context.SaveChangesAsync();
        
        // Update the raid message to reflect attendance changes
        await _raidService.UpdateRaidMessageAsync(raidId);
        
        return attendance;
    }
    
    public async Task<List<RaidAttendance>> GetAttendanceForRaidAsync(int raidId)
    {
        return await _context.RaidAttendances
            .Where(a => a.RaidId == raidId)
            .OrderBy(a => a.Status)
            .ThenBy(a => a.UserName)
            .ToListAsync();
    }
    
    public async Task<List<RaidAttendance>> GetConfirmedAttendeesAsync(int raidId)
    {
        return await _context.RaidAttendances
            .Where(a => a.RaidId == raidId && a.Status == AttendanceStatus.Confirmed)
            .ToListAsync();
    }
    
    public async Task<List<RaidAttendance>> GetBenchRequestsAsync(int raidId)
    {
        return await _context.RaidAttendances
            .Where(a => a.RaidId == raidId && 
                   (a.Status == AttendanceStatus.BenchRequested || a.Status == AttendanceStatus.OnBench))
            .ToListAsync();
    }
    
    public async Task<List<RaidAttendance>> GetPendingResponsesAsync(int raidId)
    {
        return await _context.RaidAttendances
            .Where(a => a.RaidId == raidId && a.Status == AttendanceStatus.Pending)
            .ToListAsync();
    }
    
    public async Task MoveToBenchAsync(int raidId, ulong userId)
    {
        var attendance = await _context.RaidAttendances
            .FirstOrDefaultAsync(a => a.RaidId == raidId && a.UserId == userId);
            
        if (attendance != null)
        {
            attendance.Status = AttendanceStatus.OnBench;
            await _context.SaveChangesAsync();
            await _raidService.UpdateRaidMessageAsync(raidId);
        }
    }
    
    public async Task MoveFromBenchToConfirmedAsync(int raidId, ulong userId)
    {
        var attendance = await _context.RaidAttendances
            .FirstOrDefaultAsync(a => a.RaidId == raidId && a.UserId == userId);
            
        if (attendance != null)
        {
            attendance.Status = AttendanceStatus.Confirmed;
            await _context.SaveChangesAsync();
            await _raidService.UpdateRaidMessageAsync(raidId);
        }
    }
    
    public async Task<int> GetAttendanceCountAsync(ulong userId, DateTime startDate, DateTime endDate)
    {
        return await _context.RaidAttendances
            .CountAsync(a => a.UserId == userId && 
                       a.Status == AttendanceStatus.Confirmed &&
                       a.Raid.ScheduledTime >= startDate && 
                       a.Raid.ScheduledTime <= endDate);
    }
}