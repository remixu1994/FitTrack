using FitTrack.Copilot.Api.Usda;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FitTrack.Copilot.Components;
using FitTrack.Copilot.Components.Account;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Endpoints;
using FitTrack.Copilot.Extension;
using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Http.Features;
using MudBlazor.Services;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
//dotnet user-secrets set "AI:ApiKey" "your-local-api-key"
builder.Configuration
    .AddUserSecrets<Program>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// 配置 NLog
var config = new LoggingConfiguration();

// 创建控制台目标
var consoleTarget = new ColoredConsoleTarget("console")
{
    Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${logger} | ${message} ${exception:format=tostring}"
};

// 设置日志规则 - 记录所有 Trace 级别及以上的日志
config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, consoleTarget);

// 应用配置
LogManager.Configuration = config;

builder.Services.AddNLog();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("nutrition", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Nutrition:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

//Local DataBase
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Semantic Kernel
builder.Services.AddCopilotServices(builder.Configuration);

// MudBlazor
builder.Services.AddMudServices();
builder.Services.AddHttpClient();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20MB
});

builder.Services.AddScoped<IFoodAiService, FoodAiServiceHttp>();
builder.Services.AddUsdaClient(builder.Configuration);
var app = builder.Build();
app.MapCopilotVision();
app.MapFood();
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();