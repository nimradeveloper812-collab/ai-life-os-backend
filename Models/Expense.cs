namespace AiLifeOS.API.Models;

public class Expense
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // "Income" ya "Expense"
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    // Navigation property
    public User? User { get; set; }
}