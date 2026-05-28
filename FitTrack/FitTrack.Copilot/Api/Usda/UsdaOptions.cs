namespace FitTrack.Copilot.Api.Usda;

public class UsdaOptions
{
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>
    /// https://api.nal.usda.gov/fdc/v1/foods/search
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.nal.usda.gov/fdc/v1";
}