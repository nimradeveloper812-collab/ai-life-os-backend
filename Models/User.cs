namespace AiLifeOS.API.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public List<Expense> Expenses { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public List<Goal> Goals { get; set; } = new();
}