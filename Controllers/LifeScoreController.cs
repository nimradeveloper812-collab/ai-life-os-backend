using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LifeScoreController : ControllerBase
{
    private readonly AppDbContext _db;
    public LifeScoreController(AppDbContext db) { _db = db; }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetScore(int userId)
    {
        var expenses = await _db.Expenses.Where(e => e.UserId == userId).ToListAsync();
        var tasks = await _db.Tasks.Where(t => t.UserId == userId).ToListAsync();
        var goals = await _db.Goals.Where(g => g.UserId == userId).ToListAsync();

        // Money Score (0-25)
        var totalIncome = expenses.Where(e => e.Type == "Income").Sum(e => e.Amount);
        var totalExpense = expenses.Where(e => e.Type == "Expense").Sum(e => e.Amount);
        var savingsRate = totalIncome > 0 ? (double)(totalIncome - totalExpense) / (double)totalIncome : 0;
        var moneyScore = (int)Math.Min(25, Math.Max(0, savingsRate * 25));

        // Task Score (0-25)
        var taskScore = tasks.Count > 0
            ? (int)(((double)tasks.Count(t => t.IsCompleted) / tasks.Count) * 25)
            : 0;

        // Goal Score (0-25)
        var goalScore = goals.Count > 0
            ? (int)(goals.Average(g => g.TargetValue > 0
                ? Math.Min(100, (double)g.CurrentValue / (double)g.TargetValue * 100) : 0) / 4)
            : 0;

        // Activity Score (0-25) — based on recent activity
        var recentExpenses = expenses.Count(e => e.Date >= DateTime.UtcNow.AddDays(-7));
        var recentTasks = tasks.Count(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-7));
        var activityScore = Math.Min(25, (recentExpenses + recentTasks) * 3);

        var totalScore = moneyScore + taskScore + goalScore + activityScore;

        string grade = totalScore switch
        {
            >= 90 => "S",
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            _ => "F"
        };

        string message = totalScore switch
        {
            >= 90 => "Outstanding! You're crushing life! 🔥",
            >= 80 => "Excellent! Keep up the great work! ⚡",
            >= 70 => "Good job! Small improvements ahead! 💪",
            >= 60 => "Decent! Focus on your goals! 🎯",
            >= 50 => "Room for improvement! Start today! 🌱",
            _ => "Let's get back on track! 💡"
        };

        return Ok(new
        {
            totalScore,
            grade,
            message,
            breakdown = new
            {
                moneyScore,
                taskScore,
                goalScore,
                activityScore
            }
        });
    }
}