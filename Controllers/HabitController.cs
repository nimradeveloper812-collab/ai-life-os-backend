using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HabitController : ControllerBase
{
    private readonly AppDbContext _db;
    public HabitController(AppDbContext db) { _db = db; }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetHabits(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var habits = await _db.Habits
            .Where(h => h.UserId == userId)
            .Include(h => h.Logs.Where(l => l.Date == today))
            .ToListAsync();
        return Ok(habits);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHabit(Habit habit)
    {
        habit.CreatedAt = DateTime.UtcNow;
        _db.Habits.Add(habit);
        await _db.SaveChangesAsync();
        return Ok(habit);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(int id)
    {
        var habit = await _db.Habits.FindAsync(id);
        if (habit == null) return NotFound();
        _db.Habits.Remove(habit);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("log")]
    public async Task<IActionResult> ToggleLog([FromBody] HabitLog log)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = await _db.HabitLogs.FirstOrDefaultAsync(
            l => l.HabitId == log.HabitId && l.Date == today);

        if (existing != null)
            existing.Completed = !existing.Completed;
        else
        {
            log.Date = today;
            _db.HabitLogs.Add(log);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}