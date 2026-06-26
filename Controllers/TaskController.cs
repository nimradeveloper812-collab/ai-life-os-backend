using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _db;
    public TaskController(AppDbContext db) { _db = db; }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId) =>
        Ok(await _db.Tasks.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(TaskItem task)
    {
        task.CreatedAt = DateTime.UtcNow;
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TaskItem updated)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        task.Title = updated.Title;
        task.IsCompleted = updated.IsCompleted;
        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}