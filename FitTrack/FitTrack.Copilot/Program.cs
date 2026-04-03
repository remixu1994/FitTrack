using System.Text;
using FitTrack.Copilot.Api.Usda;
using FitTrack.Copilot.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Endpoints;
using FitTrack.Copilot.Extension;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
//dotnet user-secrets set "AI:ApiKey" "your-local-api-key"
builder.Configuration
    .AddUserSecrets<Program>();

builder.Services.AddOpenApi();
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

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapChatEndpoints();
app.MapFoodApiEndpoints();
app.MapWorkoutApiEndpoints();
app.MapProgressEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapInternalAgentToolEndpoints();
}
app.MapFood();

app.Run();
