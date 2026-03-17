using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.SemanticKernel.Plugins;
using FitTrack.Copilot.SemanticKernel.RAG;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace FitTrack.Copilot.MAF.Agents;

public class FitnessAgent
{
    private const string DefaultUserId = "default";

    private readonly IChatClient _chatClient;
    private readonly NutritionPlugin _nutritionPlugin;
    private readonly WorkoutPlugin _workoutPlugin;
    private readonly HealthDataPlugin _healthDataPlugin;
    private readonly FitnessRAGService _ragService;
    private readonly AIAgent _agent;

    public FitnessAgent(
        IChatClient chatClient,
        NutritionPlugin nutritionPlugin,
        WorkoutPlugin workoutPlugin,
        HealthDataPlugin healthDataPlugin,
        FitnessRAGService ragService)
    {
        _chatClient = chatClient;
        _nutritionPlugin = nutritionPlugin;
        _workoutPlugin = workoutPlugin;
        _healthDataPlugin = healthDataPlugin;
        _ragService = ragService;

        _agent = _chatClient.AsAIAgent(
            "fitness-agent",
            "A fitness, nutrition, and health assistant for FitTrack users.",
            """
            You are FitTrack's fitness copilot.

            Your job is to help users with:
            - food and nutrition analysis
            - workout planning and exercise instructions
            - workout logging and workout history
            - body metrics, BMI, calorie needs, and health reports

            Rules:
            - Prefer tools for factual answers and data operations.
            - If a request needs user data, use the default user id unless the user explicitly provides another one.
            - If required inputs are missing, ask a concise follow-up question instead of guessing.
            - Use the knowledge-base search tool for broader educational fitness questions.
            - Keep answers practical and concise.
            """,
            BuildTools());
    }

    [Description("Main entry point for fitness-related queries")]
    public async Task<string> HandleFitnessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var response = await _agent.RunAsync(
            new[]
            {
                new ChatMessage(ChatRole.User, query)
            },
            cancellationToken: cancellationToken);

        return response.Text ?? "No response";
    }

    private IList<AITool> BuildTools()
    {
        return
        [
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)AnalyzeNutritionAsync,
                "analyze_nutrition",
                "Analyze foods and return calories and macronutrients for the given food description."),
            AIFunctionFactory.Create(
                (Func<string, string, CancellationToken, Task<string>>)GetWorkoutPlanAsync,
                "get_workout_plan",
                "Recommend a workout plan using fitness level and goal. Valid levels: beginner, intermediate, advanced. Valid goals: weight_loss, muscle_gain, endurance, general_fitness."),
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)GetExerciseInstructionsAsync,
                "get_exercise_instructions",
                "Return exercise form and instructions for a named exercise."),
            AIFunctionFactory.Create(
                (Func<int, double?, string?, CancellationToken, Task<string>>)LogWorkoutAsync,
                "log_workout",
                "Log a completed workout session for the current user with duration in minutes, optional calories burned, and optional notes."),
            AIFunctionFactory.Create(
                (Func<int, CancellationToken, Task<string>>)GetWorkoutHistoryAsync,
                "get_workout_history",
                "Get recent workout history for the current user. The count parameter controls how many recent workouts to return."),
            AIFunctionFactory.Create(
                (Func<double, double, string>)CalculateBmi,
                "calculate_bmi",
                "Calculate BMI from weight in kilograms and height in centimeters."),
            AIFunctionFactory.Create(
                (Func<double, int?, CancellationToken, Task<string>>)RecordWeightAsync,
                "record_weight",
                "Record the user's body weight in kilograms. Optionally provide daysAgo for backfilled measurements."),
            AIFunctionFactory.Create(
                (Func<double, double, int, string, string, string>)CalculateCalorieNeeds,
                "calculate_calorie_needs",
                "Calculate calorie needs using weightKg, heightCm, age, gender, and activityLevel."),
            AIFunctionFactory.Create(
                (Func<double?, CancellationToken, Task<string>>)GenerateHealthReportAsync,
                "generate_health_report",
                "Generate a health report for the current user. Optionally provide height in centimeters; defaults to 170."),
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)SearchFitnessKnowledgeAsync,
                "search_fitness_knowledge",
                "Search the fitness knowledge base for general guidance, educational content, or broader fitness questions."),
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)GetExerciseKnowledgeAsync,
                "get_exercise_knowledge",
                "Get exercise knowledge-base content when specific exercise instructions are not in the standard workout tool."),
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)GetNutritionKnowledgeAsync,
                "get_nutrition_knowledge",
                "Get nutrition knowledge-base content for general topics like protein, carbs, meal timing, and recovery nutrition.")
        ];
    }

    private async Task<string> AnalyzeNutritionAsync(string foodDescription, CancellationToken ct)
    {
        var foods = ExtractFoods(foodDescription);
        if (foods.Count == 0)
        {
            foods.Add(foodDescription);
        }

        var result = await _nutritionPlugin.GetNutritionAsync(foods, ct);
        return FormatNutritionResult(result);
    }

    private Task<string> GetWorkoutPlanAsync(string fitnessLevel, string fitnessGoal, CancellationToken ct)
        => _workoutPlugin.GetWorkoutPlanRecommendationAsync(fitnessLevel, fitnessGoal, ct);

    private async Task<string> GetExerciseInstructionsAsync(string exerciseName, CancellationToken ct)
    {
        var instructions = await _workoutPlugin.GetExerciseInstructionsAsync(exerciseName, ct);
        return instructions.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? await _ragService.GetExerciseInfoAsync(exerciseName, ct)
            : instructions;
    }

    private Task<string> LogWorkoutAsync(int durationMinutes, double? caloriesBurned, string? notes, CancellationToken ct)
        => _workoutPlugin.LogWorkoutSessionAsync(
            DefaultUserId,
            "General Workout",
            durationMinutes,
            caloriesBurned,
            notes,
            ct);

    private Task<string> GetWorkoutHistoryAsync(int count, CancellationToken ct)
        => _workoutPlugin.GetWorkoutHistoryAsync(DefaultUserId, count, ct);

    private string CalculateBmi(double weightKg, double heightCm)
        => _healthDataPlugin.CalculateBMI(weightKg, heightCm);

    private Task<string> RecordWeightAsync(double weightKg, int? daysAgo, CancellationToken ct)
    {
        DateTime? measurementDate = daysAgo.HasValue
            ? DateTime.UtcNow.AddDays(-daysAgo.Value)
            : null;

        return _healthDataPlugin.RecordWeightAsync(DefaultUserId, weightKg, measurementDate, ct);
    }

    private string CalculateCalorieNeeds(double weightKg, double heightCm, int age, string gender, string activityLevel)
        => _healthDataPlugin.CalculateCalorieNeeds(weightKg, heightCm, age, gender, activityLevel);

    private Task<string> GenerateHealthReportAsync(double? heightCm, CancellationToken ct)
        => _healthDataPlugin.GenerateHealthReportAsync(DefaultUserId, heightCm ?? 170d, ct);

    private Task<string> SearchFitnessKnowledgeAsync(string query, CancellationToken ct)
        => _ragService.SearchAsync(query, ct);

    private Task<string> GetExerciseKnowledgeAsync(string exerciseName, CancellationToken ct)
        => _ragService.GetExerciseInfoAsync(exerciseName, ct);

    private Task<string> GetNutritionKnowledgeAsync(string topic, CancellationToken ct)
        => _ragService.GetNutritionInfoAsync(topic, ct);

    private List<string> ExtractFoods(string query)
    {
        var foods = new List<string>();
        var foodNames = new[]
        {
            "chicken", "rice", "egg", "apple", "banana", "beef", "fish", "salmon", "tuna",
            "broccoli", "spinach", "carrot", "potato", "bread", "pasta", "milk", "yogurt",
            "cheese", "tofu", "bean"
        };

        foreach (var food in foodNames)
        {
            if (!query.Contains(food, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = System.Text.RegularExpressions.Regex.Match(
                query,
                $@"(\d+(?:\.\d+)?)\s*(g|gram|grams|oz|ounce|ounces|cup|cups|piece|pieces|slice|slices|tbsp|tsp|serving)?\s*{food}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foods.Add(match.Success ? match.Value : food);
        }

        return foods.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string FormatNutritionResult(NutritionResult result)
    {
        if (result.Items.Count == 0)
        {
            return "No nutrition data found for the specified foods.";
        }

        var output = "Nutrition Analysis:\n\n";

        foreach (var item in result.Items)
        {
            output += $"**{item.Name}**\n";
            output += $"Calories: {item.Calories:F0} kcal\n";
            output += $"Protein: {item.ProteinGrams:F1}g\n";
            output += $"Carbs: {item.CarbsGrams:F1}g\n";
            output += $"Fat: {item.FatGrams:F1}g\n";
            if (!string.IsNullOrWhiteSpace(item.ServingHint))
            {
                output += $"Serving: {item.ServingHint}\n";
            }

            output += "\n";
        }

        output += $"Total: {result.TotalCalories:F0} kcal";
        return output;
    }
}
