using FitTrack.Copilot.MAF.Agents;

namespace FitTrack.Copilot.Service;

public sealed class CopilotChatService : ICopilotChatService
{
    private readonly FitnessAgent _fitnessAgent;
    private readonly ImageCalorieAgent _imageCalorieAgent;
    private readonly IFoodAiService _foodAiService;

    public CopilotChatService(
        FitnessAgent fitnessAgent,
        ImageCalorieAgent imageCalorieAgent,
        IFoodAiService foodAiService)
    {
        _fitnessAgent = fitnessAgent;
        _imageCalorieAgent = imageCalorieAgent;
        _foodAiService = foodAiService;
    }

    public async Task<CopilotChatResponse> SendAsync(CopilotChatRequest request, CancellationToken ct = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var hasText = !string.IsNullOrWhiteSpace(request.Text);
        var hasImage = !string.IsNullOrWhiteSpace(request.ImageDataUrl);

        if (!hasText && !hasImage)
        {
            throw new ArgumentException("At least one input is required: text or image.", nameof(request));
        }

        if (hasImage)
        {
            var nutrition = await _foodAiService.AnalyzeAsync(new FoodRequest
            {
                Text = request.Text,
                ImageDataUrl = request.ImageDataUrl,
                UserId = request.UserId
            }, ct);

            var agentMessage = await _imageCalorieAgent.HandleImageCalorieQueryAsync(
                request.ImageDataUrl!,
                request.Text,
                ct);

            return new CopilotChatResponse
            {
                AgentName = "Image Calorie Agent",
                Message = agentMessage,
                Nutrition = nutrition
            };
        }

        var fitnessMessage = await _fitnessAgent.HandleFitnessQueryAsync(request.Text!, ct);
        return new CopilotChatResponse
        {
            AgentName = "Fitness Agent",
            Message = fitnessMessage
        };
    }
}
