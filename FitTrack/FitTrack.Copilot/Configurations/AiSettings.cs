namespace FitTrack.Copilot.Configurations;

public static class AiSettings
{
    public static AiOptions LoadAiProvidersFromFile()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(Directory.GetCurrentDirectory()).FullName)
            .AddJsonFile("Config/appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var aiOptions = configuration.GetSection("AI").Get<AiOptions>();
        return aiOptions;
    }
}