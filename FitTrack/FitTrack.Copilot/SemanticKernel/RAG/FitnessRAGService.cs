using FitTrack.Copilot.AI.RAG;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.AI.RAG;

public interface IRAGService
{
    Task<string> SearchAsync(string query, CancellationToken ct = default);
    Task<string> GetExerciseInfoAsync(string exerciseName, CancellationToken ct = default);
    Task<string> GetNutritionInfoAsync(string topic, CancellationToken ct = default);
    Task<string> GetWorkoutPlanAsync(string goal, string level, CancellationToken ct = default);
}

public class FitnessRAGService : IRAGService
{
    private readonly FitnessKnowledgeBase _knowledgeBase;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;

    public FitnessRAGService(IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        _knowledgeBase = new FitnessKnowledgeBase();
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<string> SearchAsync(string query, CancellationToken ct = default)
    {
        var results = _knowledgeBase.Search(query);
        
        if (!results.Any())
        {
            return "No matching fitness knowledge found. Please try a different search term.";
        }

        return FormatSearchResults(results);
    }

    public async Task<string> GetExerciseInfoAsync(string exerciseName, CancellationToken ct = default)
    {
        var results = _knowledgeBase.Search(exerciseName);
        var exercise = results.FirstOrDefault(r => 
            r.Type == "exercise" && 
            r.Name.ToLower().Contains(exerciseName.ToLower()));

        if (exercise == null)
        {
            var allExercises = _knowledgeBase.GetAll().Where(k => k.Type == "exercise").ToList();
            var suggestions = string.Join(", ", allExercises.Take(10).Select(e => e.Name));
            return $"Exercise '{exerciseName}' not found. Try one of these: {suggestions}";
        }

        return FormatExercise(exercise);
    }

    public async Task<string> GetNutritionInfoAsync(string topic, CancellationToken ct = default)
    {
        var results = _knowledgeBase.Search(topic);
        var nutrition = results.FirstOrDefault(r => r.Type == "nutrition");

        if (nutrition == null)
        {
            return $"No nutrition information found for '{topic}'. Try topics like: protein requirements, pre-workout nutrition, post-workout nutrition";
        }

        return FormatNutrition(nutrition);
    }

    public async Task<string> GetWorkoutPlanAsync(string goal, string level, CancellationToken ct = default)
    {
        var results = _knowledgeBase.Search($"{goal} {level}");
        var plan = results.FirstOrDefault(r => 
            r.Type == "plan" && 
            (r.Name.ToLower().Contains(goal.ToLower()) || r.Tags.Any(t => t.ToLower().Contains(goal.ToLower()))));

        if (plan == null)
        {
            return $"No specific plan found for {goal} at {level} level. Would you like a general recommendation?";
        }

        return FormatWorkoutPlan(plan);
    }

    private string FormatSearchResults(List<FitnessKnowledge> results)
    {
        var formatted = new System.Text.StringBuilder();
        formatted.AppendLine("🔍 Search Results:\n");

        foreach (var item in results.Take(5))
        {
            formatted.AppendLine($"**{item.Name}** ({item.Difficulty})");
            formatted.AppendLine($"Type: {item.Type}");
            formatted.AppendLine($"Target: {item.TargetMuscle}");
            formatted.AppendLine($"{item.Description}");
            
            if (item.Type == "exercise")
            {
                formatted.AppendLine($"Equipment needed: {item.Equipment}");
            }
            formatted.AppendLine("---\n");
        }

        return formatted.ToString();
    }

    private string FormatExercise(FitnessKnowledge exercise)
    {
        return $@"🏋️ **{exercise.Name}**

📍 Target Muscle: {exercise.TargetMuscle}
📊 Difficulty: {exercise.Difficulty}
🔧 Equipment: {exercise.Equipment}

💬 Description:
{exercise.Description}

📝 Instructions:
{exercise.Instructions}

⚠️ Common Mistakes:
{exercise.CommonMistakes}";
    }

    private string FormatNutrition(FitnessKnowledge nutrition)
    {
        return $@"🥗 **{nutrition.Name}**

{nutrition.Instructions}

⚠️ Common Mistakes:
{nutrition.CommonMistakes}";
    }

    private string FormatWorkoutPlan(FitnessKnowledge plan)
    {
        return $@"📋 **{plan.Name}**

📊 Difficulty: {plan.Difficulty}
🎯 Goal: {plan.TargetMuscle}

{plan.Instructions}

💡 Key Tips:
{plan.CommonMistakes}";
    }
}
