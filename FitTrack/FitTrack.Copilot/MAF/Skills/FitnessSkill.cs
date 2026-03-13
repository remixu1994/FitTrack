using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FitTrack.Copilot.MAF.Skills;

public class FitnessSkill
{
    [KernelFunction, Description("Calculates BMI based on weight and height")]
    public async Task<string> CalculateBMIAsync(
        [Description("Weight in kilograms")] double weight,
        [Description("Height in meters")] double height,
        CancellationToken cancellationToken = default)
    {
        var bmi = weight / (height * height);
        string category;

        if (bmi < 18.5)
            category = "Underweight";
        else if (bmi < 25)
            category = "Normal weight";
        else if (bmi < 30)
            category = "Overweight";
        else
            category = "Obese";

        return $"Your BMI is {bmi:F2}, which is classified as {category}.";
    }

    [KernelFunction, Description("Suggests workout plans based on fitness level and goals")]
    public async Task<string> SuggestWorkoutPlanAsync(
        [Description("Fitness level (beginner, intermediate, advanced)")] string fitnessLevel,
        [Description("Fitness goal (weight loss, muscle gain, endurance)")] string fitnessGoal,
        CancellationToken cancellationToken = default)
    {
        return $"Workout plan for {fitnessLevel} level with {fitnessGoal} goal:\n" +
               "- Monday: Strength training\n" +
               "- Tuesday: Cardio\n" +
               "- Wednesday: Rest\n" +
               "- Thursday: Strength training\n" +
               "- Friday: Cardio\n" +
               "- Saturday: Flexibility training\n" +
               "- Sunday: Rest";
    }
}
