namespace AiLifeOS.API.Models;

public class HealthLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public decimal SleepHours { get; set; }
    public int WaterGlasses { get; set; }
    public int ExerciseMinutes { get; set; }
    public int MoodScore { get; set; } = 5;
    public int EnergyLevel { get; set; } = 5;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}