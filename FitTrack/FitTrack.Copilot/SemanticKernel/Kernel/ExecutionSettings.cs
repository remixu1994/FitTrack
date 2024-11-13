using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FitTrack.Copilot.SemanticKernel.Kernel;


/// <summary>
/// Default prompt execution configuration for OpenAI/Azure models.
/// Encapsulates temperature, max tokens, response format, etc.
/// </summary>
public sealed class ExecutionSettings
{
    public double Temperature { get; set; } = 0.2;
    public double TopP { get; set; } = 1.0;
    public int? MaxTokens { get; set; } = null;
    public string ResponseFormat { get; set; } = "json_object"; // openai json mode
    public string? ModelOverride { get; set; }

    public ExecutionSettings() { }

    /// <summary>Convert to OpenAI prompt settings.</summary>
    public OpenAIPromptExecutionSettings AsOpenAIJsonMode()
    {
        return new OpenAIPromptExecutionSettings
        {
            Temperature = Temperature,
            TopP = TopP,
            MaxTokens = MaxTokens,
            ResponseFormat = ResponseFormat
        };
    }
}