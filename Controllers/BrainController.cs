using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrainController : ControllerBase
{
    private readonly AppDbContext _db;
    public BrainController(AppDbContext db) { _db = db; }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetAll(int userId) =>
        Ok(await _db.BrainLogs.Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(BrainLog log)
    {
        log.CreatedAt = DateTime.UtcNow;
        _db.BrainLogs.Add(log);
        await _db.SaveChangesAsync();
        return Ok(log);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await _db.BrainLogs.FindAsync(id);
        if (log == null) return NotFound();
        _db.BrainLogs.Remove(log);
        await _db.SaveChangesAsync();
        return Ok();
    }
}