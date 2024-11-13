using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using OpenAI;

namespace FitTrack.Copilot.Configurations;

public class OpenAiCompatibleAiRegister : AiProviderRegister
{
    public override AiProviderType AiProviderType => AiProviderType.OpenAI_Compatible;

    protected override void RegisterChatCompletionService(IKernelBuilder builder, IServiceProvider provider,
        AiProvider aiProvider)
    {
        var chatModelId = aiProvider.GetChatCompletionApiService()?.ModelId;
        if (string.IsNullOrWhiteSpace(chatModelId))
        {
            return;
        }

        OpenAIClientOptions clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(aiProvider.ApiEndpoint)
        };

        OpenAIClient client = new(new ApiKeyCredential(aiProvider.ApiKey), clientOptions);

        builder.AddOpenAIChatCompletion(modelId: chatModelId, openAIClient: client, serviceId: aiProvider.Code);
    }

    [Experimental("SKEXP0010")]
    protected override void RegisterEmbeddingService(IKernelBuilder builder, IServiceProvider provider,
        AiProvider aiProvider)
    {
        var embeddingModelId = aiProvider.GetEmbeddingApiService()?.ModelId;
        if (string.IsNullOrWhiteSpace(embeddingModelId))
        {
            return;
        }

        OpenAIClientOptions clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(aiProvider.ApiEndpoint)
        };

        OpenAIClient client = new(new ApiKeyCredential(aiProvider.ApiKey), clientOptions);

        // builder.AddOpenAITextEmbeddingGeneration(embeddingModelId, openAIClient: client);

        builder.AddOpenAIEmbeddingGenerator(
            modelId: embeddingModelId,
            openAIClient: client);
    }
}