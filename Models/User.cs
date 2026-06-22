namespace AiLifeOS.API.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? GoogleId { get; set; }
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Expense> Expenses { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public List<Goal> Goals { get; set; } = new();
}