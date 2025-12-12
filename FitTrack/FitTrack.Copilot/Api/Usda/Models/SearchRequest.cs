namespace FitTrack.Copilot.Api.Usda.Models;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int PageSize { get; set; } = 1;
    public string DataType { get; set; } = "Survey (FNDDS)";
}

public class SearchResponse
{
    public List<FoodItem> Foods { get; set; } = new();
}

public class FoodItem
{
    public string Description { get; set; }
    public long FdcId { get; set; }
}

public class FoodDetail
{
    public long FdcId { get; set; }
    public string Description { get; set; }
    public List<Nutrient> FoodNutrients { get; set; } = new();
}

public class Nutrient
{
    public int NutrientId { get; set; }
    public string Name { get; set; }
    public double Amount { get; set; }
    public string UnitName { get; set; }
}