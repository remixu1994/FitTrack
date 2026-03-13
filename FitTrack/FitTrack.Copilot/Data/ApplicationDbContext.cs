using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatSessionMessage> ChatSessionMessages { get; set; }
    public DbSet<FitnessGoal> FitnessGoals { get; set; }
    public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
    public DbSet<WorkoutDay> WorkoutDays { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutSession> WorkoutSessions { get; set; }
    public DbSet<ExerciseSession> ExerciseSessions { get; set; }
    public DbSet<FoodRecord> FoodRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<ChatSessionMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired();
            entity.HasOne(e => e.ChatSession)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FitnessGoal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.GoalType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.GoalDescription).IsRequired();
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<WorkoutPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PlanName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<WorkoutDay>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DayOfWeek).IsRequired().HasMaxLength(20);
            entity.HasOne(e => e.WorkoutPlan)
                .WithMany(e => e.WorkoutDays)
                .HasForeignKey(e => e.WorkoutPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.WorkoutDay)
                .WithMany(e => e.Exercises)
                .HasForeignKey(e => e.WorkoutDayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.WorkoutPlan)
                .WithMany()
                .HasForeignKey(e => e.WorkoutPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExerciseSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExerciseName).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.WorkoutSession)
                .WithMany(e => e.ExerciseSessions)
                .HasForeignKey(e => e.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FoodRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.FoodName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ConsumptionDate);
        });
    }
}