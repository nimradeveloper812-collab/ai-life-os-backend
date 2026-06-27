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
    private readonly HttpClient _http = new();
    private readonly string _groqKey = "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";

    public AiController(AppDbContext db) { _db = db; }

    [HttpPost("analyze/{userId}")]
    public async Task<IActionResult> Analyze(int userId, [FromBody] AiRequestDto dto)
    {
        var expenses = await _db.Expenses.Where(e => e.UserId == userId).OrderByDescending(e => e.Date).Take(10).ToListAsync();
        var tasks = await _db.Tasks.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).Take(10).ToListAsync();
        var goals = await _db.Goals.Where(g => g.UserId == userId).Take(5).ToListAsync();

        var totalIncome = expenses.Where(e => e.Type == "Income").Sum(e => e.Amount);
        var totalExpense = expenses.Where(e => e.Type == "Expense").Sum(e => e.Amount);
        var completedTasks = tasks.Count(t => t.IsCompleted);

        var systemPrompt = $@"You are AI Life OS — a personal life assistant. 
Be friendly, concise, and actionable. Use emojis. Max 150 words per response.
User data:
- Income: Rs.{totalIncome}, Expenses: Rs.{totalExpense}, Balance: Rs.{totalIncome - totalExpense}
- Tasks: {completedTasks}/{tasks.Count} completed
- Goals: {goals.Count} active
- Recent: {string.Join(", ", expenses.Take(3).Select(e => $"{e.Title}(Rs.{e.Amount})"))}";

        var groqBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = dto.Message ?? "Give me a quick life summary." }
            },
            max_tokens = 300,
            temperature = 0.7
        };

        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqKey}");

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
        var aiResponse = parsed.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
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

        var prompt = $@"Generate a motivating daily life report. Use emojis, be encouraging. Max 200 words.
Include: Financial summary, Task productivity, Goal progress, Motivational insight, Top 3 actions for today.
Data: Income Rs.{totalIncome}, Expenses Rs.{totalExpense}, Balance Rs.{totalIncome - totalExpense}
Tasks: {completedTasks}/{tasks.Count} done. Goals: {goals.Count} active.";

        var groqBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = "You are AI Life OS, a personal life coach. Be motivating, use emojis." },
                new { role = "user", content = prompt }
            },
            max_tokens = 400,
            temperature = 0.8
        };

        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqKey}");

        var json = JsonSerializer.Serialize(groqBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);
        var aiResponse = parsed.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return Ok(new { report = aiResponse });
    }
}

public class AiRequestDto
{
    public string? Message { get; set; }
}