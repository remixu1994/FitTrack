using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using System.ComponentModel;

namespace FitTrack.Copilot.AI.Plugins;

public class WorkoutPlugin
{
    private readonly IFitnessService _fitnessService;
    private readonly IWorkoutSessionService _workoutSessionService;

    public WorkoutPlugin(IFitnessService fitnessService, IWorkoutSessionService workoutSessionService)
    {
        _fitnessService = fitnessService;
        _workoutSessionService = workoutSessionService;
    }

    [Description("Get workout plan recommendations based on fitness level and goals")]
    public async Task<string> GetWorkoutPlanRecommendationAsync(
        [Description("User's fitness level: beginner, intermediate, or advanced")] string fitnessLevel,
        [Description("User's fitness goal: weight_loss, muscle_gain, endurance, or general_fitness")] string fitnessGoal,
        CancellationToken ct = default)
    {
        var plans = await _fitnessService.GetWorkoutPlansByUserIdAsync("default", ct);
        
        var filteredPlans = plans.Where(p => 
            (string.IsNullOrEmpty(p.FitnessLevel) || p.FitnessLevel.Equals(fitnessLevel, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(p.Description) || p.Description.Contains(fitnessGoal, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        if (filteredPlans.Any())
        {
            var plan = filteredPlans.First();
            return $"Recommended Plan: {plan.PlanName}\n\nDescription: {plan.Description}\n\nDays: {string.Join(", ", plan.WorkoutDays.Select(d => d.DayOfWeek))}";
        }

        return GenerateDefaultWorkoutPlan(fitnessLevel, fitnessGoal);
    }

    private string GenerateDefaultWorkoutPlan(string fitnessLevel, string fitnessGoal)
    {
        return fitnessGoal.ToLower() switch
        {
            "weight_loss" => GenerateWeightLossPlan(fitnessLevel),
            "muscle_gain" => GenerateMuscleGainPlan(fitnessLevel),
            "endurance" => GenerateEndurancePlan(fitnessLevel),
            _ => GenerateGeneralFitnessPlan(fitnessLevel)
        };
    }

    private string GenerateWeightLossPlan(string level)
    {
        return level.ToLower() switch
        {
            "beginner" => @"Weight Loss Plan (Beginner):

Monday: Full Body + 20min Cardio
Tuesday: Rest
Wednesday: Upper Body + 15min HIIT
Thursday: Rest
Friday: Lower Body + 20min Cardio
Saturday: Active Recovery (Walk/Yoga)
Sunday: Rest",

            "intermediate" => @"Weight Loss Plan (Intermediate):

Monday: Upper Body Strength + 30min Cardio
Tuesday: Lower Body Strength + 25min HIIT
Wednesday: Rest
Thursday: Full Body + 30min Cardio
Friday: Upper Body HIIT + 25min Cardio
Saturday: Lower Body + 30min Steady Cardio
Sunday: Rest",

            "advanced" => @"Weight Loss Plan (Advanced):

Monday: Upper Body Strength + 45min HIIT
Tuesday: Lower Body Strength + 45min Cardio
Wednesday: Active Recovery + 20min Yoga
Thursday: Full Body + 45min HIIT
Friday: Upper Body + 45min Cardio
Saturday: Lower Body + 60min Mixed Cardio
Sunday: Active Recovery",
            
            _ => "Please specify your fitness level (beginner, intermediate, or advanced)"
        };
    }

    private string GenerateMuscleGainPlan(string level)
    {
        return level.ToLower() switch
        {
            "beginner" => @"Muscle Gain Plan (Beginner):

Monday: Chest + Back
Tuesday: Legs + Core
Wednesday: Rest
Thursday: Shoulders + Arms
Friday: Full Body
Saturday: Rest
Sunday: Light Activity",

            "intermediate" => @"Muscle Gain Plan (Intermediate):

Monday: Chest + Triceps
Tuesday: Back + Biceps
Wednesday: Legs + Abs
Thursday: Rest
Friday: Shoulders + Arms
Saturday: Full Body
Sunday: Rest",

            "advanced" => @"Muscle Gain Plan (Advanced):

Monday: Chest + Triceps + Cardio
Tuesday: Back + Biceps
Wednesday: Legs + Calves
Thursday: Rest
Friday: Shoulders + Abs + Cardio
Saturday: Arms + Forearms
Sunday: Rest or Active Recovery",

            _ => "Please specify your fitness level (beginner, intermediate, or advanced)"
        };
    }

    private string GenerateEndurancePlan(string level)
    {
        return level.ToLower() switch
        {
            "beginner" => @"Endurance Plan (Beginner):

Monday: 20min Steady Cardio
Tuesday: Strength Training
Wednesday: 25min Interval Cardio
Thursday: Rest
Friday: 20min Cardio + Strength
Saturday: 30min Long Cardio
Sunday: Rest",

            "intermediate" => @"Endurance Plan (Intermediate):

Monday: 30min HIIT
Tuesday: Strength + 20min Cardio
Wednesday: 40min Steady Cardio
Thursday: Rest
Friday: 30min Interval + Strength
Saturday: 60min Long Cardio
Sunday: Rest",

            "advanced" => @"Endurance Plan (Advanced):

Monday: 45min HIIT + Strength
Tuesday: 45min Tempo Run
Wednesday: Strength + 30min Cardio
Thursday: Rest
Friday: 45min Intervals + Strength
Saturday: 90min Long Run/Cycle
Sunday: Recovery Yoga",

            _ => "Please specify your fitness level (beginner, intermediate, or advanced)"
        };
    }

    private string GenerateGeneralFitnessPlan(string level)
    {
        return level.ToLower() switch
        {
            "beginner" => @"General Fitness Plan (Beginner):

Monday: Full Body Strength
Tuesday: 15min Cardio
Wednesday: Rest
Thursday: Full Body Strength
Friday: 20min Cardio
Saturday: Light Activity
Sunday: Rest",

            "intermediate" => @"General Fitness Plan (Intermediate):

Monday: Upper Body + Cardio
Tuesday: Lower Body + Core
Wednesday: Rest
Thursday: Upper Body + Core
Friday: Lower Body + Cardio
Saturday: Full Body
Sunday: Rest",

            "advanced" => @"General Fitness Plan (Advanced):

Monday: Push + Cardio
Tuesday: Pull + Core
Wednesday: Legs + Cardio
Thursday: Rest
Friday: Full Body + HIIT
Saturday: Long Cardio Session
Sunday: Recovery",

            _ => "Please specify your fitness level (beginner, intermediate, or advanced)"
        };
    }

    [Description("Get exercise instructions with proper form")]
    public async Task<string> GetExerciseInstructionsAsync(
        [Description("Name of the exercise")] string exerciseName,
        CancellationToken ct = default)
    {
        var exerciseInstructions = GetExerciseDatabase();
        
        if (exerciseInstructions.TryGetValue(exerciseName.ToLower(), out var instructions))
        {
            return instructions;
        }

        return $"Exercise '{exerciseName}' not found in database. Please try another exercise or consult a fitness professional.";
    }

    private Dictionary<string, string> GetExerciseDatabase()
    {
        return new Dictionary<string, string>
        {
            ["squat"] = @"Squat - Proper Form:

1. Stand with feet shoulder-width apart
2. Keep your chest up and core engaged
3. Lower your body as if sitting back into a chair
4. Keep your knees in line with your toes
5. Go down until thighs are parallel to the ground
6. Drive through your heels to stand back up

Common Mistakes:
- Knees caving inward
- Rounding the lower back
- heels lifting off the ground",

            ["bench press"] = @"Bench Press - Proper Form:

1. Lie on bench with eyes under the bar
2. Grip slightly wider than shoulder-width
3. Plant feet firmly on the ground
4. Lower bar to mid-chest with control
5. Press bar up in slight arc
6. Lock out at the top without flaring elbows

Common Mistakes:
- Bouncing bar off chest
- Flaring elbows too wide
- Lifting hips off bench",

            ["deadlift"] = @"Deadlift - Proper Form:

1. Stand with feet hip-width apart, bar over mid-foot
2. Hinge at hips and grip bar just outside legs
3. Keep back flat, chest up
4. Engage lats and brace core
5. Drive through heels, extend hips and knees together
6. Stand tall, squeeze glutes at top
7. Return bar to floor with control

Common Mistakes:
- Rounding lower back
- Jerking the bar off the floor
- Hyperextending at the top",

            ["pull-up"] = @"Pull-Up - Proper Form:

1. Hang from bar with overhand grip, slightly wider than shoulders
2. Engage lats and pull shoulder blades down
3. Pull yourself up until chin clears the bar
4. Lower with control to full arm extension
5. Keep core tight throughout

Common Mistakes:
- Using momentum/kipping
- Not going full range of motion
- Shrugging shoulders at the top",

            ["plank"] = @"Plank - Proper Form:

1. Start in push-up position on forearms
2. Keep body in straight line from head to heels
3. Engage core and squeeze glutes
4. Don't let hips sag or pike up
5. Hold position while breathing normally

Common Mistakes:
- Hips too high
- Hips too low
- Holding breath",

            ["lunge"] = @"Lunge - Proper Form:

1. Stand with feet hip-width apart
2. Step forward with one leg
3. Lower until both knees are at 90 degrees
4. Keep front knee over ankle, not past toes
5. Push through front heel to return
6. Alternate legs

Common Mistakes:
- Front knee extending past toes
- Taking too short steps
- Leaning forward"
        };
    }

    [Description("Log a completed workout session")]
    public async Task<string> LogWorkoutSessionAsync(
        [Description("User ID")] string userId,
        [Description("Workout type or plan name")] string workoutType,
        [Description("Duration in minutes")] int durationMinutes,
        [Description("Calories burned (optional)")] double? caloriesBurned = null,
        [Description("Notes about the workout (optional)")] string? notes = null,
        CancellationToken ct = default)
    {
        var session = new WorkoutSession
        {
            UserId = userId,
            WorkoutPlanId = null,
            StartTime = DateTime.UtcNow.AddMinutes(-durationMinutes),
            EndTime = DateTime.UtcNow,
            Duration = durationMinutes,
            CaloriesBurned = caloriesBurned,
            Notes = notes
        };

        await _workoutSessionService.CreateWorkoutSessionAsync(session);
        
        return $"Workout logged successfully!\n" +
               $"Type: {workoutType}\n" +
               $"Duration: {durationMinutes} minutes\n" +
               $"Calories: {caloriesBurned ?? 0} kcal\n" +
               $"Keep up the great work!";
    }

    [Description("Get workout history for a user")]
    public async Task<string> GetWorkoutHistoryAsync(
        [Description("User ID")] string userId,
        [Description("Number of recent workouts to retrieve")] int count = 5,
        CancellationToken ct = default)
    {
        var sessions = await _workoutSessionService.GetWorkoutSessionsByUserIdAsync(userId);
        
        if (!sessions.Any())
        {
            return "No workout history found. Start your fitness journey today!";
        }

        var recentSessions = sessions.OrderByDescending(s => s.StartTime).Take(count);
        
        var result = "Recent Workouts:\n\n";
        foreach (var session in recentSessions)
        {
            result += $"Date: {session.StartTime:yyyy-MM-dd}\n";
            result += $"Duration: {session.Duration} minutes\n";
            result += $"Calories: {session.CaloriesBurned ?? 0} kcal\n";
            if (!string.IsNullOrEmpty(session.Notes))
                result += $"Notes: {session.Notes}\n";
            result += "---\n";
        }

        return result;
    }
}
