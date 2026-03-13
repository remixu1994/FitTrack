using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FitTrack.Copilot.SemanticKernel.Plugins;

public class HealthDataPlugin
{
    private readonly IFitnessService _fitnessService;
    private readonly IWorkoutSessionService _workoutSessionService;
    private readonly IFoodRecordService _foodRecordService;

    public HealthDataPlugin(
        IFitnessService fitnessService,
        IWorkoutSessionService workoutSessionService,
        IFoodRecordService foodRecordService)
    {
        _fitnessService = fitnessService;
        _workoutSessionService = workoutSessionService;
        _foodRecordService = foodRecordService;
    }

    [KernelFunction, Description("Record user's body weight")]
    public async Task<string> RecordWeightAsync(
        [Description("User ID")] string userId,
        [Description("Weight in kg")] double weight,
        [Description("Date of measurement (optional, defaults to today)")] DateTime? date = null,
        CancellationToken ct = default)
    {
        var goal = new FitnessGoal
        {
            UserId = userId,
            GoalType = "weight_tracking",
            GoalDescription = $"Weight: {weight}kg",
            TargetWeight = weight,
            StartDate = date ?? DateTime.UtcNow,
            EndDate = (date ?? DateTime.UtcNow).AddMonths(3)
        };

        await _fitnessService.CreateFitnessGoalAsync(goal);

        return $"Weight recorded: {weight}kg on {(date ?? DateTime.UtcNow):yyyy-MM-dd}\n" +
               "Keep tracking your progress!";
    }

    [KernelFunction, Description("Get weight tracking history")]
    public async Task<string> GetWeightHistoryAsync(
        [Description("User ID")] string userId,
        [Description("Number of days to look back")] int days = 30,
        CancellationToken ct = default)
    {
        var goals = await _fitnessService.GetFitnessGoalsByUserIdAsync(userId);
        var weightGoals = goals.Where(g => g.GoalType == "weight_tracking")
            .Where(g => g.StartDate >= DateTime.UtcNow.AddDays(-days))
            .OrderByDescending(g => g.StartDate)
            .ToList();

        if (!weightGoals.Any())
        {
            return "No weight records found in the last " + days + " days.";
        }

        var result = $"Weight History (Last {days} days):\n\n";
        foreach (var goal in weightGoals)
        {
            result += $"Date: {goal.StartDate:yyyy-MM-dd}\n";
            result += $"Weight: {goal.TargetWeight} kg\n";
            result += "---\n";
        }

        return result;
    }

    [KernelFunction, Description("Calculate BMI based on weight and height")]
    public string CalculateBMI(
        [Description("Weight in kg")] double weightKg,
        [Description("Height in cm")] double heightCm)
    {
        var heightM = heightCm / 100.0;
        var bmi = weightKg / (heightM * heightM);
        
        string category;
        string advice;
        
        if (bmi < 18.5)
        {
            category = "Underweight";
            advice = "Consider consulting a nutritionist to develop a healthy weight gain plan.";
        }
        else if (bmi < 25)
        {
            category = "Normal Weight";
            advice = "Great job! Maintain your current lifestyle with regular exercise and balanced diet.";
        }
        else if (bmi < 30)
        {
            category = "Overweight";
            advice = "Consider incorporating more regular exercise and watching your calorie intake.";
        }
        else
        {
            category = "Obese";
            advice = "Please consult a healthcare professional for personalized advice.";
        }

        return $"BMI Calculator:\n\n" +
               $"Weight: {weightKg} kg\n" +
               $"Height: {heightCm} cm\n" +
               $"BMI: {bmi:F1}\n" +
               $"Category: {category}\n\n" +
               $"Advice: {advice}";
    }

    [KernelFunction, Description("Get nutrition summary for a specific date")]
    public async Task<string> GetNutritionSummaryAsync(
        [Description("User ID")] string userId,
        [Description("Date for nutrition summary")] DateTime date,
        CancellationToken ct = default)
    {
        var summary = await _foodRecordService.GetNutritionSummaryByDateAsync(userId, date);

        if (summary == null || summary.RecordCount == 0)
        {
            return $"Nutrition Summary for {date:yyyy-MM-dd}:\n\nNo food records found for this date.";
        }

        return $"Nutrition Summary for {date:yyyy-MM-dd}:\n\n" +
               $"Total Calories: {summary.TotalCalories} kcal\n" +
               $"Protein: {summary.TotalProtein}g\n" +
               $"Carbohydrates: {summary.TotalCarbs}g\n" +
               $"Fat: {summary.TotalFat}g\n" +
               $"Food Items: {summary.RecordCount}";
    }

    [KernelFunction, Description("Calculate daily calorie needs based on user stats")]
    public string CalculateCalorieNeeds(
        [Description("Weight in kg")] double weightKg,
        [Description("Height in cm")] double heightCm,
        [Description("Age in years")] int age,
        [Description("Gender: male or female")] string gender,
        [Description("Activity level: sedentary, light, moderate, active, or very_active")] string activityLevel)
    {
        var bmr = gender.ToLower() == "male"
            ? 10 * weightKg + 6.25 * heightCm - 5 * age + 5
            : 10 * weightKg + 6.25 * heightCm - 5 * age - 161;

        var activityMultiplier = activityLevel.ToLower() switch
        {
            "sedentary" => 1.2,
            "light" => 1.375,
            "moderate" => 1.55,
            "active" => 1.725,
            "very_active" => 1.9,
            _ => 1.55
        };

        var tdee = bmr * activityMultiplier;

        return $"Daily Calorie Needs:\n\n" +
               $"Basal Metabolic Rate (BMR): {bmr:F0} kcal\n" +
               $"Activity Level: {activityLevel}\n" +
               $"TDEE (Total Daily Energy Expenditure): {tdee:F0} kcal\n\n" +
               $"Weight Maintenance: {tdee:F0} kcal/day\n" +
               $"Weight Loss (-500 kcal): {tdee - 500:F0} kcal/day\n" +
               $"Weight Gain (+500 kcal): {tdee + 500:F0} kcal/day";
    }

    [KernelFunction, Description("Generate a comprehensive health report")]
    public async Task<string> GenerateHealthReportAsync(
        [Description("User ID")] string userId,
        [Description("Height in cm for BMI calculation")] double heightCm,
        CancellationToken ct = default)
    {
        var goals = await _fitnessService.GetFitnessGoalsByUserIdAsync(userId);
        var workouts = await _workoutSessionService.GetWorkoutSessionsByUserIdAsync(userId);
        var foodRecords = await _foodRecordService.GetFoodRecordsByUserIdAsync(userId);

        var latestWeight = goals.Where(g => g.TargetWeight.HasValue)
            .OrderByDescending(g => g.StartDate)
            .FirstOrDefault()?.TargetWeight ?? 0;

        var weekWorkouts = workouts.Count(w => w.StartTime >= DateTime.UtcNow.AddDays(-7));
        var weekFoodRecords = foodRecords.Count(f => f.ConsumptionDate >= DateTime.UtcNow.AddDays(-7));

        var weekCalories = foodRecords
            .Where(f => f.ConsumptionDate >= DateTime.UtcNow.AddDays(-7))
            .Sum(f => f.Calories);

        var bmi = latestWeight > 0 ? latestWeight / ((heightCm / 100) * (heightCm / 100)) : 0;
        var bmiCategory = bmi > 0 ? (bmi < 18.5 ? "Underweight" : bmi < 25 ? "Normal" : bmi < 30 ? "Overweight" : "Obese") : "N/A";

        var report = @"╔══════════════════════════════════════╗
║       WEEKLY HEALTH REPORT           ║
╚══════════════════════════════════════╝

📊 BODY METRICS
─────────────────";
        
        if (latestWeight > 0)
        {
            report += $"\nWeight: {latestWeight} kg\n";
            report += $"Height: {heightCm} cm\n";
            report += $"BMI: {bmi:F1} ({bmiCategory})";
        }
        else
        {
            report += "\nNo weight data available.\nRecord your weight to get BMI calculation.";
        }

        report += @"

🏋️ WORKOUT ACTIVITY
─────────────────
This Week: " + weekWorkouts + @" workouts
";

        if (weekWorkouts > 0)
        {
            var avgDuration = workouts
                .Where(w => w.StartTime >= DateTime.UtcNow.AddDays(-7))
                .Average(w => w.Duration) ?? 0;
            var avgCalories = workouts
                .Where(w => w.StartTime >= DateTime.UtcNow.AddDays(-7))
                .Average(w => w.CaloriesBurned) ?? 0;
            
            report += $"Avg Duration: {avgDuration:F0} min\n";
            report += $"Avg Calories: {avgCalories:F0} kcal";
        }

        report += @"

🥗 NUTRITION
─────────────────
This Week: " + weekFoodRecords + @" meals logged
Weekly Calories: " + weekCalories + @" kcal
";

        if (weekFoodRecords > 0)
        {
            var avgDaily = weekCalories / 7;
            report += $"Avg Daily: {avgDaily:F0} kcal";
        }

        report += @"

💡 RECOMMENDATIONS
─────────────────";

        if (weekWorkouts < 3)
            report += "\n• Aim for at least 3 workouts per week";
        if (weekFoodRecords < 21)
            report += "\n• Log all your meals for better tracking";
        if (bmi > 25 || bmi < 18.5)
            report += "\n• Consider consulting a health professional";

        report += "\n• Stay consistent with your fitness goals!";

        return report;
    }
}
