using FitTrack.Copilot.Api.Usda.Models;

namespace FitTrack.Copilot.Api.Usda;

public interface IUsdaClient
{
    Task<FoodItem?> SearchAsync(string query);
    Task<FoodDetail?> GetFoodAsync(long fdcId);
}