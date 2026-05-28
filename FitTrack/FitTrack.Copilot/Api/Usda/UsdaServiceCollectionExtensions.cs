namespace FitTrack.Copilot.Api.Usda;

public static class UsdaServiceCollectionExtensions
{
    public static IServiceCollection AddUsdaClient(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<UsdaOptions>(config.GetSection("USDA"));

        services.AddHttpClient<IUsdaClient, UsdaClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<UsdaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}