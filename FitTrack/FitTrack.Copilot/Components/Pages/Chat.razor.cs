using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace FitTrack.Copilot.Components.Pages;

public partial class Chat
{
    private List<ChatMessage> Messages = new();
    private string? InputText;
    private bool IsSending;
    private ElementReference? bottomRef;
    private string? PreviewImage;
    private ElementReference? fileInput;

    protected override void OnInitialized()
    {
        Messages.Add(new ChatMessage(Role.Assistant,
            "Hi! Send a food photo or describe your meal, and I’ll estimate calories & macros."));
    }


    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private async Task<string?> ReadAsDataUrl(IBrowserFile file)
    {
        using var stream = file.OpenReadStream(5 * 1024 * 1024); // 5MB
        var buffer = new byte[file.Size];
        await stream.ReadAsync(buffer);
        var base64 = Convert.ToBase64String(buffer);
        return $"data:{file.ContentType};base64,{base64}";
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null) return;
        PreviewImage = await ReadAsDataUrl(file);
        StateHasChanged();
    }

    private async Task Send()
    {
        if (IsSending) return;
        if (string.IsNullOrWhiteSpace(InputText) && string.IsNullOrEmpty(PreviewImage))
        {
            Snackbar.Add("Please type a description or attach an image.", Severity.Info);
            return;
        }

        IsSending = true;

        // push user message
        var userMsg = new ChatMessage(Role.User, InputText ?? string.Empty, PreviewImage);
        Messages.Add(userMsg);

        // push assistant placeholder (loading)
        var aiMsg = new ChatMessage(Role.Assistant, null, null) { Status = MessageStatus.Loading };
        Messages.Add(aiMsg);
        ScrollToBottom();

        try
        {
            // Call AI
            var result = await FoodAi.AnalyzeAsync(new FoodRequest
            {
                Text = InputText,
                ImageDataUrl = PreviewImage
            });

            aiMsg.Status = MessageStatus.Done;
            aiMsg.Nutrition = result;
        }
        catch (Exception ex)
        {
            aiMsg.Status = MessageStatus.Error;
            aiMsg.Error = ex.Message;
        }
        finally
        {
            InputText = string.Empty;
            PreviewImage = null;
            IsSending = false;
            ScrollToBottom();
        }
    }

    private void InsertSample()
    {
        InputText = "One bowl of beef noodles with scallions, plus a boiled egg.";
    }

    private void ClearChat()
    {
        Messages.Clear();
        Messages.Add(new ChatMessage(Role.Assistant, "Chat cleared. Send a food photo or description to begin."));
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await Send();
        }
    }

    private async void ScrollToBottom()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("fittrackChat.scrollBottom");
        }
        catch
        {
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

    public class ChatMessage
    {
        public ChatMessage(Role role, string? text, string? image = null)
        {
            Id = Guid.NewGuid().ToString("N");
            Role = role;
            Text = text;
            ImageDataUrl = image;
            Time = DateTimeOffset.Now;
        }

        public string Id { get; set; }
        public Role Role { get; set; }
        public string? Text { get; set; }
        public string? ImageDataUrl { get; set; }
        public DateTimeOffset Time { get; set; }
        public MessageStatus Status { get; set; }
        public NutritionResult? Nutrition { get; set; }
        public string? Error { get; set; }
    }


    private byte[]? _fileBytes;
    private string? _fileContentType;

    private async Task UploadFiles(IBrowserFile? file)
    {
        if (file is null) return;

        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
        _fileBytes = ms.ToArray();
        _fileContentType = file.ContentType;

        // preview
        PreviewImage = $"data:{_fileContentType};base64,{Convert.ToBase64String(_fileBytes)}";
    }
}