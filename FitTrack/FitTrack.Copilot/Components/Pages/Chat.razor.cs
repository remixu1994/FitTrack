using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace FitTrack.Copilot.Components.Pages;

public partial class Chat
{
    private List<ChatTurn> Messages = [];
    private string? InputText;
    private bool IsSending;
    private string? PreviewImage;

    [Inject]
    private ICopilotChatService CopilotChat { get; set; } = default!;

    protected override void OnInitialized()
    {
        Messages.Add(new ChatTurn(
            Role.Assistant,
            "Hi. Ask a fitness question, or upload a meal photo and I will switch to the image calorie agent.",
            agentName: "Copilot"));
    }

    private bool HasPendingImage => !string.IsNullOrWhiteSpace(PreviewImage);

    private async Task Send()
    {
        if (IsSending)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(InputText) && string.IsNullOrWhiteSpace(PreviewImage))
        {
            Snackbar.Add("Enter a prompt or upload an image first.", Severity.Info);
            return;
        }

        IsSending = true;

        var userTurn = new ChatTurn(Role.User, InputText, PreviewImage, "You");
        Messages.Add(userTurn);

        var assistantTurn = new ChatTurn(Role.Assistant, null, null, HasPendingImage ? "Image Calorie Agent" : "Fitness Agent")
        {
            Status = MessageStatus.Loading
        };
        Messages.Add(assistantTurn);

        try
        {
            var response = await CopilotChat.SendAsync(new CopilotChatRequest
            {
                Text = InputText,
                ImageDataUrl = PreviewImage
            });

            assistantTurn.Status = MessageStatus.Done;
            assistantTurn.Text = response.Message;
            assistantTurn.AgentName = response.AgentName;
            assistantTurn.Nutrition = response.Nutrition;
        }
        catch (Exception ex)
        {
            assistantTurn.Status = MessageStatus.Error;
            assistantTurn.Error = ex.Message;
        }
        finally
        {
            InputText = string.Empty;
            PreviewImage = null;
            IsSending = false;
        }
    }

    private async Task UploadFiles(IBrowserFile? file)
    {
        if (file is null)
        {
            return;
        }

        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
        PreviewImage = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    private void ClearPendingImage()
    {
        PreviewImage = null;
    }

    private void InsertFitnessSample()
    {
        InputText = "Create a beginner workout plan for muscle gain.";
    }

    private void InsertMealSample()
    {
        InputText = "Estimate this meal and call out any uncertainty in the portion size.";
    }

    private void ClearChat()
    {
        Messages.Clear();
        PreviewImage = null;
        InputText = string.Empty;
        Messages.Add(new ChatTurn(
            Role.Assistant,
            "Chat cleared. Start with a fitness question or upload a meal photo.",
            agentName: "Copilot"));
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await Send();
        }
    }

    public enum Role
    {
        User,
        Assistant
    }

    public enum MessageStatus
    {
        None,
        Loading,
        Done,
        Error
    }

    public sealed class ChatTurn
    {
        public ChatTurn(Role role, string? text, string? image = null, string? agentName = null)
        {
            Id = Guid.NewGuid().ToString("N");
            Role = role;
            Text = text;
            ImageDataUrl = image;
            AgentName = agentName;
            Time = DateTimeOffset.Now;
        }

        public string Id { get; set; }
        public Role Role { get; set; }
        public string? AgentName { get; set; }
        public string? Text { get; set; }
        public string? ImageDataUrl { get; set; }
        public DateTimeOffset Time { get; set; }
        public MessageStatus Status { get; set; }
        public NutritionResult? Nutrition { get; set; }
        public string? Error { get; set; }
    }
}
