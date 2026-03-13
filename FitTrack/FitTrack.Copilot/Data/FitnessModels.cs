using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public class FitnessGoal
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string GoalType { get; set; } = string.Empty; // 如：weight_loss, muscle_gain, endurance
    
    [Required]
    public string GoalDescription { get; set; } = string.Empty;
    
    public double? TargetWeight { get; set; }
    public double? TargetBodyFat { get; set; }
    public int? TargetDuration { get; set; } // 目标持续时间（天）
    
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsCompleted { get; set; } = false;
}

public class WorkoutPlan
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string PlanName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string? FitnessLevel { get; set; } // 如：beginner, intermediate, advanced
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    
    public List<WorkoutDay> WorkoutDays { get; set; } = new();
}

public class WorkoutDay
{
    public int Id { get; set; }
    
    public int WorkoutPlanId { get; set; }
    
    [Required]
    public string DayOfWeek { get; set; } = string.Empty; // 如：Monday, Tuesday
    
    public List<Exercise> Exercises { get; set; } = new();
    
    public WorkoutPlan WorkoutPlan { get; set; } = null!;
}

public class Exercise
{
    public int Id { get; set; }
    
    public int WorkoutDayId { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public int? Sets { get; set; }
    public int? Reps { get; set; }
    public int? Duration { get; set; } // 持续时间（分钟）
    public int? RestTime { get; set; } // 休息时间（秒）
    
    public WorkoutDay WorkoutDay { get; set; } = null!;
}

public class WorkoutSession
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public int? WorkoutPlanId { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public int? Duration { get; set; } // 持续时间（分钟）
    public double? CaloriesBurned { get; set; }
    public string? Notes { get; set; }
    
    public List<ExerciseSession> ExerciseSessions { get; set; } = new();
    public WorkoutPlan? WorkoutPlan { get; set; }
}

public class ExerciseSession
{
    public int Id { get; set; }
    
    public int WorkoutSessionId { get; set; }
    
    [Required]
    public string ExerciseName { get; set; } = string.Empty;
    
    public int? Sets { get; set; }
    public int? Reps { get; set; }
    public int? Duration { get; set; } // 持续时间（分钟）
    
    public WorkoutSession WorkoutSession { get; set; } = null!;
}

public class FoodRecord
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string FoodName { get; set; } = string.Empty;
    
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    
    public double? ServingSize { get; set; }
    public string? ServingUnit { get; set; }
    
    [Required]
    public DateTime ConsumptionDate { get; set; } = DateTime.UtcNow;
    
    public string? MealType { get; set; } // 如：breakfast, lunch, dinner, snack
}
