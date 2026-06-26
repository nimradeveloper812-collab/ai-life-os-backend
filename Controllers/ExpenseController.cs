using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpenseController : ControllerBase
{
    private readonly AppDbContext _db;
    public ExpenseController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Expenses.ToListAsync());

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId) =>
        Ok(await _db.Expenses.Where(e => e.UserId == userId).OrderByDescending(e => e.Date).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense == null) return NotFound();
        return Ok(expense);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Expense expense)
    {
        expense.Date = DateTime.UtcNow;
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        return Ok(expense);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Expense updated)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense == null) return NotFound();
        expense.Title = updated.Title;
        expense.Amount = updated.Amount;
        expense.Type = updated.Type;
        expense.Category = updated.Category;
        await _db.SaveChangesAsync();
        return Ok(expense);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense == null) return NotFound();
        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}