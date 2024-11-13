using FitTrack.Copilot.Abstractions.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore.Metadata;
using MudBlazor;

namespace FitTrack.Copilot.Components.Pages;

public partial class FoodVision
{
    private string _serviceId = "openai"; // default provider key
    private string? _modelId;
    private string? _hint;
    private bool _busy;
    private bool _hasResult => _result is not null && _result.Items.Any();
    private byte[]? _fileBytes;
    private string? _fileContentType;
    private string? _previewDataUrl;
    private NutritionResult? _result;

    [Inject]
    private NavigationManager? navigation { get; set; }
    private async Task OnFileChange(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null) return;

        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
        _fileBytes = ms.ToArray();
        _fileContentType = file.ContentType;

        // preview
        _previewDataUrl = $"data:{_fileContentType};base64,{Convert.ToBase64String(_fileBytes)}";
    }

    private async Task Analyze()
    {
        if (_fileBytes is null || _fileContentType is null)
        {
            Snackbar.Add("Please select an image first.", Severity.Warning);
            return;
        }

        try
        {
            _busy = true;
            var client = HttpClientFactory.CreateClient();
            client.BaseAddress = new Uri(navigation?.BaseUri);
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(_fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_fileContentType);
            form.Add(fileContent, "image", "upload." + MimeToExt(_fileContentType));

            if (!string.IsNullOrWhiteSpace(_hint)) form.Add(new StringContent(_hint), "hint");
            if (!string.IsNullOrWhiteSpace(_serviceId)) form.Add(new StringContent(_serviceId), "serviceId");
            if (!string.IsNullOrWhiteSpace(_modelId)) form.Add(new StringContent(_modelId!), "modelId");

            var res = await client.PostAsync("/copilot/vision/estimate", form);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync();
                Snackbar.Add($"Analyze failed: {res.StatusCode} - {msg}", Severity.Error);
                return;
            }

            _result = await res.Content.ReadFromJsonAsync<NutritionResult>() ?? new NutritionResult();
            if (!_result.Items.Any())
                Snackbar.Add("No items detected. Please adjust hint or choose a different provider.", Severity.Info);
            else
                Snackbar.Add("Analysis complete. You can edit the rows before saving.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Analyze error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private async Task Save()
    {
        try
        {
            var api = HttpClientFactory.CreateClient();
            var payload = new
            {
                userId = "me",
                occurredAt = DateTimeOffset.Now,
                items = _result!.Items.Select(x => new
                {
                    name = x.Name, calories = x.Calories, proteinGrams = x.ProteinGrams,
                    carbsGrams = x.CarbsGrams, fatGrams = x.FatGrams,
                    confidence = x.Confidence, servingHint = x.ServingHint, source = "ai"
                })
            };
            var resp = await api.PostAsJsonAsync("/api/meals", payload);
            resp.EnsureSuccessStatusCode();
            Snackbar.Add("Saved to diary.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
    }

    private void Reset()
    {
        _result = null;
        _fileBytes = null;
        _fileContentType = null;
        _previewDataUrl = null;
        _modelId = null;
        _hint = null;
    }

    private void AddRow()
    {
        _result ??= new NutritionResult();
        _result.Items.Add(new NutritionItem { Name = "", Confidence = null, Source = "manual" });
    }

    private void RemoveRow(NutritionItem it)
    {
        _result?.Items.Remove(it);
    }

    private static string MimeToExt(string mime)
        => mime switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "bin"
        };
}