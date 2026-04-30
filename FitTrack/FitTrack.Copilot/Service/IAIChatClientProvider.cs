namespace FitTrack.Copilot.Service;

public interface IAIChatClientProvider
{
    string CurrentProvider { get; }
    Microsoft.Extensions.AI.IChatClient ChatClient { get; }
}
