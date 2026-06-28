using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetController : ControllerBase
{
    private readonly AppDbContext _db;
    public BudgetController(AppDbContext db) { _db = db; }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetBudgets(int userId)
    {
        var now = DateTime.UtcNow;
        var budgets = await _db.Budgets
            .Where(b => b.UserId == userId && b.Month == now.Month && b.Year == now.Year)
            .ToListAsync();

        var expenses = await _db.Expenses
            .Where(e => e.UserId == userId &&
                e.Type == "Expense" &&
                e.Date >= new DateTime(now.Year, now.Month, 1))
            .ToListAsync();

        var result = budgets.Select(b => new
        {
            b.Id,
            b.Category,
            b.MonthlyLimit,
            b.Month,
            b.Year,
            spent = expenses.Where(e => e.Category == b.Category).Sum(e => e.Amount),
            percent = b.MonthlyLimit > 0
                ? Math.Round((double)expenses.Where(e => e.Category == b.Category).Sum(e => e.Amount) / (double)b.MonthlyLimit * 100, 1)
                : 0
        });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Budget budget)
    {
        var now = DateTime.UtcNow;
        budget.Month = now.Month;
        budget.Year = now.Year;
        budget.CreatedAt = DateTime.UtcNow;

        var existing = await _db.Budgets.FirstOrDefaultAsync(b =>
            b.UserId == budget.UserId &&
            b.Category == budget.Category &&
            b.Month == budget.Month &&
            b.Year == budget.Year);

        if (existing != null)
        {
            existing.MonthlyLimit = budget.MonthlyLimit;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();
        return Ok(budget);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var budget = await _db.Budgets.FindAsync(id);
        if (budget == null) return NotFound();
        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync();
        return Ok();
    }
}