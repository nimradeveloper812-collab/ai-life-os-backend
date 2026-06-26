namespace AiLifeOS.API.Models;

public class Habit
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = "⭐";
    public int FrequencyDays { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public List<HabitLog> Logs { get; set; } = new();
}

public class HabitLog
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public bool Completed { get; set; } = false;
    public Habit? Habit { get; set; }
}