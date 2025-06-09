using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XIVRaidBot.Data;
using XIVRaidBot.Models;

namespace XIVRaidBot.Services;

public class UserSettingsService
{
    private readonly RaidBotContext _context;
    
    public UserSettingsService(RaidBotContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Get a user's settings, creating a new record if one doesn't exist
    /// </summary>
    public async Task<UserSettings> GetUserSettingsAsync(ulong userId)
    {
        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(us => us.UserId == userId);
            
        if (userSettings == null)
        {
            userSettings = new UserSettings
            {
                UserId = userId,
                TimeZoneId = string.Empty,
                LastUpdated = DateTime.UtcNow
            };
            
            _context.UserSettings.Add(userSettings);
            await _context.SaveChangesAsync();
        }
        
        return userSettings;
    }
    
    /// <summary>
    /// Set a user's timezone
    /// </summary>
    public async Task<bool> SetUserTimezoneAsync(ulong userId, string timezoneId)
    {
        try
        {
            // Validate timezone
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            
            var userSettings = await GetUserSettingsAsync(userId);
            
            userSettings.TimeZoneId = timezoneId;
            userSettings.LastUpdated = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Check if a user has set their timezone
    /// </summary>
    public async Task<bool> HasUserSetTimezoneAsync(ulong userId)
    {
        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(us => us.UserId == userId);
            
        return userSettings != null && !string.IsNullOrEmpty(userSettings.TimeZoneId);
    }
    
    /// <summary>
    /// Convert a datetime from a user's timezone to UTC
    /// </summary>
    public async Task<DateTime> ConvertToUtcAsync(ulong userId, DateTime localTime)
    {
        var userSettings = await GetUserSettingsAsync(userId);
        
        if (string.IsNullOrEmpty(userSettings.TimeZoneId))
            return localTime; // Default to assuming the time is already UTC
            
        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(userSettings.TimeZoneId);
            return TimeZoneInfo.ConvertTimeToUtc(localTime, timezone);
        }
        catch
        {
            return localTime; // Default to assuming the time is already UTC
        }
    }
    
    /// <summary>
    /// Gets a list of valid timezone IDs for the user to choose from
    /// </summary>
    public List<string> GetAvailableTimezones()
    {
        return TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => tz.Id)
            .OrderBy(id => id)
            .ToList();
    }
}
