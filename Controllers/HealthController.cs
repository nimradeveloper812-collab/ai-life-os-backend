using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    public HealthController(AppDbContext db) { _db = db; }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId) =>
        Ok(await _db.HealthLogs.Where(h => h.UserId == userId)
            .OrderByDescending(h => h.Date).ToListAsync());

    [HttpGet("user/{userId}/today")]
    public async Task<IActionResult> GetToday(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var log = await _db.HealthLogs.FirstOrDefaultAsync(h => h.UserId == userId && h.Date == today);
        return Ok(log);
    }

    [HttpPost]
    public async Task<IActionResult> Save(HealthLog log)
    {
        var existing = await _db.HealthLogs.FirstOrDefaultAsync(
            h => h.UserId == log.UserId && h.Date == log.Date);

        if (existing != null)
        {
            existing.SleepHours = log.SleepHours;
            existing.WaterGlasses = log.WaterGlasses;
            existing.ExerciseMinutes = log.ExerciseMinutes;
            existing.MoodScore = log.MoodScore;
            existing.EnergyLevel = log.EnergyLevel;
            existing.Notes = log.Notes;
        }
        else
        {
            log.CreatedAt = DateTime.UtcNow;
            _db.HealthLogs.Add(log);
        }

        await _db.SaveChangesAsync();
        return Ok(log);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await _db.HealthLogs.FindAsync(id);
        if (log == null) return NotFound();
        _db.HealthLogs.Remove(log);
        await _db.SaveChangesAsync();
        return Ok();
    }
}