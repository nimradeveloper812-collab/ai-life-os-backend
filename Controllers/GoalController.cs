using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoalController : ControllerBase
{
    private readonly AppDbContext _db;
    public GoalController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Goals.ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(Goal goal)
    {
        goal.CreatedAt = DateTime.Now;
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();
        return Ok(goal);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Goal updated)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal == null) return NotFound();
        goal.Title = updated.Title;
        goal.TargetValue = updated.TargetValue;
        goal.CurrentValue = updated.CurrentValue;
        goal.Deadline = updated.Deadline;
        await _db.SaveChangesAsync();
        return Ok(goal);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal == null) return NotFound();
        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}