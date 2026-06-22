using System.ComponentModel.DataAnnotations.Schema;

namespace AiLifeOS.API.Models;

public class Expense
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public User? User { get; set; }
}