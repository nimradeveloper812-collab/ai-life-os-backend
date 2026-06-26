using Microsoft.EntityFrameworkCore;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Har DbSet ek database table hai
    public DbSet<User> Users { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<Goal> Goals { get; set; }

    public DbSet<HealthLog> HealthLogs { get; set; }

    public DbSet<Habit> Habits { get; set; }
    public DbSet<HabitLog> HabitLogs { get; set; }

    public DbSet<BrainLog> BrainLogs { get; set; }
}
