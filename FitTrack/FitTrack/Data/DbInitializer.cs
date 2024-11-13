using System.Text.Json;

namespace FitTrack.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context, IWebHostEnvironment env)
    {
        // 自动创建数据库（如果不存在）
        context.Database.EnsureCreated();

        // 如果 Foods 表已经有数据，就不再导入
        if (context.Foods.Any())
            return;

        // 获取 foods.json 路径
        var jsonPath = Path.Combine(env.WebRootPath, "foods.json");

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"foods.json not found at: {jsonPath}");

        // 读取并反序列化 JSON
        var jsonContent = File.ReadAllText(jsonPath);
        var foods = JsonSerializer.Deserialize<List<Food>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true 
        });

        if (foods == null || foods.Count == 0)
            throw new Exception("foods.json is empty or invalid.");

        // 设置 IsDefault 标记
        foreach (var food in foods)
            food.IsDefault = true;

        // 批量写入数据库
        context.Foods.AddRange(foods);
        context.SaveChanges();
    }
}