using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.SemanticKernel.Plugins;
using FitTrack.Copilot.SemanticKernel.RAG;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace FitTrack.Copilot.MAF.Agents;

public class FitnessAgent
{
    private readonly IChatClient _chatClient;
    private readonly NutritionPlugin _nutritionPlugin;
    private readonly WorkoutPlugin _workoutPlugin;
    private readonly HealthDataPlugin _healthDataPlugin;
    private readonly FitnessRAGService _ragService;

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
    }

    [Description("Main entry point for fitness-related queries")]
    public async Task<string> HandleFitnessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var queryLower = query.ToLower();

        if (queryLower.Contains("food") || queryLower.Contains("eat") || queryLower.Contains("nutrition") || queryLower.Contains("calories") || queryLower.Contains("protein"))
        {
            return await HandleNutritionQueryAsync(query, cancellationToken);
        }
        else if (queryLower.Contains("workout") || queryLower.Contains("exercise") || queryLower.Contains("train") || queryLower.Contains("fitness") || queryLower.Contains("gym"))
        {
            return await HandleWorkoutQueryAsync(query, cancellationToken);
        }
        else if (queryLower.Contains("weight") || queryLower.Contains("bmi") || queryLower.Contains("health") || queryLower.Contains("body") || queryLower.Contains("report"))
        {
            return await HandleHealthQueryAsync(query, cancellationToken);
        }
        else
        {
            return await HandleGeneralQueryAsync(query, cancellationToken);
        }
    }

    private async Task<string> HandleNutritionQueryAsync(string query, CancellationToken ct)
    {
        var queryLower = query.ToLower();

        if (queryLower.Contains("analyze") || queryLower.Contains("what") || queryLower.Contains("how many"))
        {
            var foods = ExtractFoods(query);
            if (foods.Any())
            {
                var result = await _nutritionPlugin.GetNutritionAsync(foods, ct);
                return FormatNutritionResult(result);
            }
        }

        if (queryLower.Contains("protein") || queryLower.Contains("carbs") || queryLower.Contains("fat"))
        {
            return await _ragService.GetNutritionInfoAsync(query, ct);
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"You are a nutrition expert. Help users with:
- Food nutrition analysis
- Calorie counting
- Macronutrient guidance
- Meal planning
- Dietary recommendations

Provide practical, evidence-based advice."),
            new(ChatRole.User, query)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text ?? "No response";
    }

    private async Task<string> HandleWorkoutQueryAsync(string query, CancellationToken ct)
    {
        var queryLower = query.ToLower();

        if (queryLower.Contains("plan") || queryLower.Contains("recommend") || queryLower.Contains("suggest"))
        {
            var (level, goal) = ExtractFitnessLevelAndGoal(query);
            return await _workoutPlugin.GetWorkoutPlanRecommendationAsync(level, goal, ct);
        }

        if (queryLower.Contains("how to") || queryLower.Contains("form") || queryLower.Contains("instruction"))
        {
            var exerciseName = ExtractExerciseName(query);
            if (!string.IsNullOrEmpty(exerciseName))
            {
                return await _workoutPlugin.GetExerciseInstructionsAsync(exerciseName, ct);
            }
            return await _ragService.GetExerciseInfoAsync(query, ct);
        }

        if (queryLower.Contains("log") || queryLower.Contains("record") || queryLower.Contains("track"))
        {
            return await HandleWorkoutLoggingAsync(query, ct);
        }

        if (queryLower.Contains("history") || queryLower.Contains("past"))
        {
            return await _workoutPlugin.GetWorkoutHistoryAsync("default", 5, ct);
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"You are a fitness expert. Help users with:
- Workout recommendations
- Exercise form and instructions
- Training program design
- Workout logging
- Progress tracking

Provide motivating, practical advice."),
            new(ChatRole.User, query)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text ?? "No response";
    }

    private async Task<string> HandleWorkoutLoggingAsync(string query, CancellationToken ct)
    {
        var durationMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+)\s*(minute|min|hour|hr)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var caloriesMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+)\s*(cal|kcal)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (durationMatch.Success)
        {
            var duration = int.Parse(durationMatch.Groups[1].Value);
            var calories = caloriesMatch.Success ? double.Parse(caloriesMatch.Groups[1].Value) : (double?)null;

            return await _workoutPlugin.LogWorkoutSessionAsync(
                "default",
                "General Workout",
                duration,
                calories,
                null,
                ct);
        }

        return "I couldn't understand the workout details. Please provide duration (e.g., '30 minutes') and optionally calories burned.";
    }

    private async Task<string> HandleHealthQueryAsync(string query, CancellationToken ct)
    {
        var queryLower = query.ToLower();

        if (queryLower.Contains("bmi"))
        {
            var weightMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+(?:\.\d+)?)\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var heightMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+(?:\.\d+)?)\s*cm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (weightMatch.Success && heightMatch.Success)
            {
                var weight = double.Parse(weightMatch.Groups[1].Value);
                var height = double.Parse(heightMatch.Groups[1].Value);
                return _healthDataPlugin.CalculateBMI(weight, height);
            }

            return "To calculate BMI, please provide both weight (e.g., 70kg) and height (e.g., 175cm).";
        }

        if (queryLower.Contains("report") || queryLower.Contains("summary") || queryLower.Contains("status"))
        {
            var heightMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+(?:\.\d+)?)\s*cm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var height = heightMatch.Success ? double.Parse(heightMatch.Groups[1].Value) : 170.0;

            return await _healthDataPlugin.GenerateHealthReportAsync("default", height, ct);
        }

        if (queryLower.Contains("calorie") || queryLower.Contains("tdee"))
        {
            return HandleCalorieCalculation(query);
        }

        if (queryLower.Contains("weight"))
        {
            var weightMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+(?:\.\d+)?)\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (weightMatch.Success)
            {
                var weight = double.Parse(weightMatch.Groups[1].Value);
                return await _healthDataPlugin.RecordWeightAsync("default", weight, null, ct);
            }
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"You are a health expert. Help users with:
- Weight tracking and BMI calculation
- Health reports and summaries
- Calorie needs calculation
- Body composition advice

Provide accurate, encouraging advice."),
            new(ChatRole.User, query)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text ?? "No response";
    }

    private string HandleCalorieCalculation(string query)
    {
        var weightMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+(?:\.\d+)?)\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var heightMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+(?:\.\d+)?)\s*cm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var ageMatch = System.Text.RegularExpressions.Regex.Match(query, @"(\d+)\s*(year|yr|age)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var genderMatch = System.Text.RegularExpressions.Regex.Match(query, @"\b(male|female|man|woman)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var activityMatch = System.Text.RegularExpressions.Regex.Match(query, @"\b(sedentary|light|moderate|active|very_active)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (weightMatch.Success && heightMatch.Success && ageMatch.Success && genderMatch.Success)
        {
            var weight = double.Parse(weightMatch.Groups[1].Value);
            var height = double.Parse(heightMatch.Groups[1].Value);
            var age = int.Parse(ageMatch.Groups[1].Value);
            var gender = genderMatch.Groups[1].Value;
            var activity = activityMatch.Success ? activityMatch.Value : "moderate";

            return _healthDataPlugin.CalculateCalorieNeeds(weight, height, age, gender, activity);
        }

        return "To calculate calorie needs, please provide:\n- Weight (e.g., 70kg)\n- Height (e.g., 175cm)\n- Age (e.g., 30 years)\n- Gender (male/female)\n- Activity level (sedentary/light/moderate/active/very_active)";
    }

    private async Task<string> HandleGeneralQueryAsync(string query, CancellationToken ct)
    {
        var ragResult = await _ragService.SearchAsync(query, ct);
        
        if (ragResult.Contains("No matching"))
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, @"You are a friendly and knowledgeable fitness and nutrition coach. 

You can help users with:
- Nutrition and diet advice
- Workout recommendations and exercise form
- Weight tracking and health metrics
- Fitness goal setting
- Personalized fitness plans

Be encouraging, practical, and provide actionable advice."),
                new(ChatRole.User, query)
            };

            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
            return response.Text ?? "No response";
        }

        return ragResult;
    }

    private List<string> ExtractFoods(string query)
    {
        var foods = new List<string>();
        
        var patterns = new[]
        {
            @"(\d+(?:\.\d+)?)\s*(g|gram|grams|oz|ounce|ounces|cup|cups|piece|pieces|slice|slices| tbsp| tsp| serving)",
            @"(\d+(?:\.\d+)?)\s*(chicken|rice|egg|apple|banana|meat|fish|vegetable|fruit|bread|pasta)"
        };

        var foodNames = new[] { "chicken", "rice", "egg", "apple", "banana", "beef", "fish", "salmon", "tuna", "broccoli", "spinach", "carrot", "potato", "bread", "pasta", "milk", "yogurt", "cheese", "tofu", "bean" };

        foreach (var food in foodNames)
        {
            if (query.ToLower().Contains(food))
            {
                var match = System.Text.RegularExpressions.Regex.Match(query, $@"(\d+(?:\.\d+)?)\s*(g|gram|grams|oz)?\s*{food}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    foods.Add(match.Value);
                }
                else
                {
                    foods.Add(food);
                }
            }
        }

        return foods.Distinct().ToList();
    }

    private (string level, string goal) ExtractFitnessLevelAndGoal(string query)
    {
        var level = "intermediate";
        var goal = "general_fitness";

        var levels = new[] { "beginner", "intermediate", "advanced" };
        foreach (var l in levels)
        {
            if (query.ToLower().Contains(l))
            {
                level = l;
                break;
            }
        }

        if (query.ToLower().Contains("weight_loss") || query.ToLower().Contains("lose weight") || query.ToLower().Contains("fat loss") || query.ToLower().Contains("slim"))
            goal = "weight_loss";
        else if (query.ToLower().Contains("muscle") || query.ToLower().Contains("gain") || query.ToLower().Contains("build"))
            goal = "muscle_gain";
        else if (query.ToLower().Contains("endurance") || query.ToLower().Contains("cardio") || query.ToLower().Contains("run"))
            goal = "endurance";

        return (level, goal);
    }

    private string ExtractExerciseName(string query)
    {
        var exercises = new[] { "squat", "bench press", "deadlift", "pull-up", "push-up", "plank", "lunge", "row", "curl", "press" };
        
        foreach (var ex in exercises)
        {
            if (query.ToLower().Contains(ex))
            {
                return ex;
            }
        }

        return string.Empty;
    }

    private string FormatNutritionResult(NutritionResult result)
    {
        if (result.Items.Count == 0)
        {
            return "No nutrition data found for the specified foods.";
        }

        var output = "🥗 Nutrition Analysis:\n\n";
        
        foreach (var item in result.Items)
        {
            output += $"**{item.Name}**\n";
            output += $"  Calories: {item.Calories:F0} kcal\n";
            output += $"  Protein: {item.ProteinGrams:F1}g\n";
            output += $"  Carbs: {item.CarbsGrams:F1}g\n";
            output += $"  Fat: {item.FatGrams:F1}g\n";
            if (!string.IsNullOrEmpty(item.ServingHint))
                output += $"  Serving: {item.ServingHint}\n";
            output += "\n";
        }

        output += $"**Total: {result.TotalCalories:F0} kcal**";
        
        return output;
    }
}
