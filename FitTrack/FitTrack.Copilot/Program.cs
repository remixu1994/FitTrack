using System.Text;
using FitTrack.Copilot.Api.Usda;
using FitTrack.Copilot.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Endpoints;
using FitTrack.Copilot.Extension;
using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
//dotnet user-secrets set "AI:ApiKey" "your-local-api-key"
builder.Configuration
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Services.AddOpenApi();
builder.Services.AddDataProtection();
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// 配置 NLog
var config = new LoggingConfiguration();

// 创建控制台目标
var consoleTarget = new ColoredConsoleTarget("console")
{
    Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${logger} | ${message} ${exception:format=tostring}"
};
consoleTarget.RowHighlightingRules.Clear();
consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Trace", ConsoleOutputColor.DarkGray, ConsoleOutputColor.NoChange));
consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Debug", ConsoleOutputColor.Gray, ConsoleOutputColor.NoChange));
consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Info", ConsoleOutputColor.White, ConsoleOutputColor.NoChange));
consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Warn", ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange));
consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Error", ConsoleOutputColor.Red, ConsoleOutputColor.NoChange));
consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Fatal", ConsoleOutputColor.Magenta, ConsoleOutputColor.NoChange));

// 设置日志规则 - 记录所有 Trace 级别及以上的日志
config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, consoleTarget);

// 应用配置
LogManager.Configuration = config;

builder.Services.AddHttpClient("nutrition", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Nutrition:BaseUrl"] ?? builder.Configuration["USDA:BaseUrl"] ?? "https://api.nal.usda.gov/fdc/v1/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

//Local DataBase
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Dev: allow login without email confirmation
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// JWT Bearer auth — must come AFTER AddIdentity so it overrides the default cookie schemes
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
    })
    .AddJwtBearer("Bearer", options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Prevent Identity cookie from redirecting to /Account/Login on API endpoints — return 401 instead
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme,
    options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// USDA Client (must be registered before CopilotServices)
builder.Services.AddUsdaClient(builder.Configuration);

// Semantic Kernel
builder.Services.AddCopilotServices(builder.Configuration);

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20MB
});

var app = builder.Build();
await ApplyDatabaseMigrationsAsync(app.Services);
await CleanupExpiredModelRequestLogsAsync(app.Services);
var frontendBaseUrl = (builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

MapFrontendRedirect("/", string.Empty);
MapFrontendRedirect("/login", "login");
MapFrontendRedirect("/chat", "chat");
MapFrontendRedirect("/food-records", "food-records");
MapFrontendRedirect("/workouts", "workouts");
MapFrontendRedirect("/progress", "progress");
MapFrontendRedirect("/settings/profile", "settings/profile");
MapFrontendRedirect("/settings/model-usage", "settings/model-usage");
MapFrontendRedirect("/settings/models", "settings/models");

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapModelConnectorEndpoints();
app.MapAdminTenantModelConnectorEndpoints();
app.MapAdminModelUsageEndpoints();
app.MapChatEndpoints();
app.MapFoodApiEndpoints();
app.MapWorkoutApiEndpoints();
app.MapProgressEndpoints();
app.MapFood();

// Seed data (admin user, roles, etc.)
await DataSeeder.SeedAsync(app.Services);

app.Run();

async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

async Task CleanupExpiredModelRequestLogsAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var modelUsageService = scope.ServiceProvider.GetRequiredService<IModelUsageService>();
    await modelUsageService.CleanupAsync();
}

void MapFrontendRedirect(string route, string targetPath)
{
    app.MapGet(route, () =>
    {
        var destination = string.IsNullOrWhiteSpace(targetPath)
            ? frontendBaseUrl
            : $"{frontendBaseUrl}/{targetPath}";
        return Results.Redirect(destination);
    });
}
