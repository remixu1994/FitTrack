using FitTrack.Copilot.Abstractions;

namespace FitTrack.Copilot.Models;

/// <summary>
/// Input model for vision-based nutrition analysis
/// </summary>
public sealed record VisionNutritionInput(
    IReadOnlyList<FilePart>? Images,
    string? Hint,
    string? UserId);