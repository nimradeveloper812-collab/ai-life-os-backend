using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Data;
using System.Text;
using System.Text.Json;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly HttpClient _http = new();

    public AiController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("analyze/{userId}")]
    public async Task<IActionResult> Analyze(int userId, [FromBody] AiRequestDto dto)
    {
        var expenses = await _db.Expenses
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Date)
            .Take(10).ToListAsync();

        var tasks = await _db.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10).ToListAsync();

        var goals = await _db.Goals
            .Where(g => g.UserId == userId)
            .Take(5).ToListAsync();

        var totalIncome = expenses.Where(e => e.Type == "Income").Sum(e => e.Amount);
        var totalExpense = expenses.Where(e => e.Type == "Expense").Sum(e => e.Amount);
        var completedTasks = tasks.Count(t => t.IsCompleted);

        var systemPrompt = $@"You are AI Life OS — a personal life assistant. 
You help users manage their life like an operating system manages a computer.
Be friendly, concise, and actionable. Use emojis. Max 150 words per response.

User's current data:
- Total Income: Rs. {totalIncome}
- Total Expenses: Rs. {totalExpense}  
- Balance: Rs. {totalIncome - totalExpense}
- Tasks: {completedTasks}/{tasks.Count} completed
- Active Goals: {goals.Count}
- Recent expenses: {string.Join(", ", expenses.Take(3).Select(e => $"{e.Title}(Rs.{e.Amount})"))}";

        var userMessage = dto.Message ?? "Give me a quick life summary and top 3 suggestions.";

        var groqBody = new
        {
            model = "llama3-8b-8192",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = 300,
            temperature = 0.7
        };

        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");

        var json = JsonSerializer.Serialize(groqBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return BadRequest(new { message = "AI error: " + err });
        }

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);
        var aiResponse = parsed.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return Ok(new { response = aiResponse });
    }

    [HttpPost("daily-report/{userId}")]
    public async Task<IActionResult> DailyReport(int userId)
    {
        var expenses = await _db.Expenses.Where(e => e.UserId == userId).ToListAsync();
        var tasks = await _db.Tasks.Where(t => t.UserId == userId).ToListAsync();
        var goals = await _db.Goals.Where(g => g.UserId == userId).ToListAsync();

        var totalIncome = expenses.Where(e => e.Type == "Income").Sum(e => e.Amount);
        var totalExpense = expenses.Where(e => e.Type == "Expense").Sum(e => e.Amount);
        var completedTasks = tasks.Count(t => t.IsCompleted);

        var prompt = $@"Generate a motivating daily life report for this user. 
Use emojis, be encouraging. Max 200 words. Include:
1. Financial summary
2. Task productivity
3. Goal progress  
4. One motivational insight
5. Top 3 action items for today

Data:
- Income: Rs.{totalIncome}, Expenses: Rs.{totalExpense}, Balance: Rs.{totalIncome - totalExpense}
- Tasks: {completedTasks}/{tasks.Count} completed
- Goals: {goals.Count} active, avg progress: {(goals.Any() ? goals.Average(g => g.TargetValue > 0 ? (double)g.CurrentValue / (double)g.TargetValue * 100 : 0) : 0):F0}%";

        var groqBody = new
        {
            model = "llama3-8b-8192",
            messages = new[]
            {
                new { role = "system", content = "You are AI Life OS, a personal life coach assistant. Be motivating, use emojis, be concise." },
                new { role = "user", content = prompt }
            },
            max_tokens = 400,
            temperature = 0.8
        };

        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");

        var json = JsonSerializer.Serialize(groqBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);
        var aiResponse = parsed.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return Ok(new { report = aiResponse });
    }
}

public class AiRequestDto
{
    public string? Message { get; set; }
}