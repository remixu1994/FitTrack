# Troubleshooting & Best Practices

<cite>
**Referenced Files in This Document**   
- [PerformanceMonitorChatClient.cs](file://FitTrack.Copilot/Middleware/PerformanceMonitorChatClient.cs)
- [SensitiveWordFilterChatClient.cs](file://FitTrack.Copilot/Middleware/SensitiveWordFilterChatClient.cs)
- [FoodParsingSkill.cs](file://FitTrack.Copilot/Tools/FoodParsingSkill.cs)
- [DbInitializer.cs](file://FitTrack/Data/DbInitializer.cs)
- [Program.cs](file://FitTrack.Copilot/Program.cs)
- [UsdaClient.cs](file://FitTrack.Copilot/Api/Usda/UsdaClient.cs)
- [ApplicationDbContext.cs](file://FitTrack/Data/ApplicationDbContext.cs)
- [Chat.razor.cs](file://FitTrack.Copilot/Components/Pages/Chat.razor.cs)
- [FoodVision.razor.cs](file://FitTrack.Copilot/Components/Pages/FoodVision.razor.cs)
- [appsettings.json](file://FitTrack.Copilot/appsettings.json)
- [FitTrack.Copilot.csproj](file://FitTrack.Copilot/FitTrack.Copilot.csproj)
</cite>

## Table of Contents
1. [AI-Related Issues](#ai-related-issues)
2. [Database Issues](#database-issues)
3. [Blazor Component Debugging](#blazor-component-debugging)
4. [Authentication Flow Troubleshooting](#authentication-flow-troubleshooting)
5. [API Endpoint Errors](#api-endpoint-errors)
6. [Performance Optimization](#performance-optimization)
7. [Security Best Practices](#security-best-practices)
8. [Logging and Monitoring](#logging-and-monitoring)
9. [Production Readiness Checklist](#production-readiness-checklist)
10. [Incident Response Procedures](#incident-response-procedures)

## AI-Related Issues

### Slow Response Times with PerformanceMonitorChatClient

The `PerformanceMonitorChatClient` middleware provides comprehensive performance tracking for AI interactions. When experiencing slow responses, check the performance logs which include request duration, token usage, and average response times. The system automatically logs warnings when response times exceed 5 seconds. Use the `GetPerformanceSummary()` method to retrieve aggregate statistics including total requests, average response time, and token consumption metrics.

**Section sources**
- [PerformanceMonitorChatClient.cs](file://FitTrack.Copilot/Middleware/PerformanceMonitorChatClient.cs#L1-L139)

### Inappropriate Content via SensitiveWordFilterChatClient

The `SensitiveWordFilterChatClient` middleware detects and logs sensitive content in both user inputs and AI responses. It maintains a configurable list of sensitive words (currently including "垃圾", "废物", "骗子", "投诉", "举报"). The filter logs warnings when sensitive content is detected but does not block responses by default. For stricter enforcement, modify the middleware to either block requests containing sensitive words or replace them with asterisks using the `ReplaceSensitiveWords()` method.

**Section sources**
- [SensitiveWordFilterChatClient.cs](file://FitTrack.Copilot/Middleware/SensitiveWordFilterChatClient.cs#L1-L148)

### Parsing Failures in FoodParsingSkill

The `FoodLookupSkill` class handles food parsing and calorie lookup through the USDA API. Common parsing failures occur when:
- Food names don't match USDA database entries
- Network issues prevent API calls
- Response parsing fails due to unexpected data formats

The skill returns null when no matching food item is found. Ensure proper error handling in calling code to manage these scenarios gracefully.

**Section sources**
- [FoodParsingSkill.cs](file://FitTrack.Copilot/Tools/FoodParsingSkill.cs#L1-L25)

## Database Issues

### Migration Conflicts

Migration conflicts can occur when multiple developers create migrations simultaneously. The project uses Entity Framework Core with SQLite and includes migrations in the `Data/Migrations` directory. To resolve conflicts:
1. Ensure all team members pull the latest migrations before creating new ones
2. Use descriptive migration names
3. Test migrations on a clean database before deployment

**Section sources**
- [ApplicationDbContext.cs](file://FitTrack/Data/ApplicationDbContext.cs#L1-L17)
- [FitTrack.Copilot.csproj](file://FitTrack.Copilot/FitTrack.Copilot.csproj#L1-L71)

### Connection Timeouts

Database connection timeouts are typically caused by:
- High query load
- Long-running transactions
- Connection pool exhaustion

The application uses SQLite with a shared cache connection string. For production environments, consider switching to a server-based database like SQL Server or PostgreSQL to handle higher concurrency.

**Section sources**
- [appsettings.json](file://FitTrack.Copilot/appsettings.json#L1-L55)
- [Program.cs](file://FitTrack.Copilot/Program.cs#L70-L74)

### Seeding Problems with DbInitializer.cs

The `DbInitializer` class handles database seeding from `foods.json`. Common issues include:
- Missing `foods.json` file in wwwroot
- Invalid JSON format
- Database already contains data (seeding only occurs on empty tables)

Ensure the `foods.json` file exists in the wwwroot directory and is properly formatted. The initializer throws exceptions for missing files or invalid data, which should be handled during application startup.

**Section sources**
- [DbInitializer.cs](file://FitTrack/Data/DbInitializer.cs#L1-L40)
- [FitTrack.csproj](file://FitTrack/FitTrack.csproj#L1-L37)

## Blazor Component Debugging

### Chat Component Issues

The `Chat.razor` component manages AI interactions and file uploads. Common issues include:
- Image upload failures due to size limits (5MB for stream, 10MB for file)
- JavaScript interop failures for scrolling
- State management issues during async operations

Ensure proper handling of `IsSending` state and use `StateHasChanged()` after asynchronous operations that affect UI state.

**Section sources**
- [Chat.razor.cs](file://FitTrack.Copilot/Components/Pages/Chat.razor.cs#L1-L174)

### FoodVision Component Issues

The `FoodVision.razor` component handles image analysis workflows. Key debugging points:
- Verify image data is properly converted to base64
- Check API endpoint URLs during POST requests
- Handle empty analysis results gracefully
- Ensure proper error messaging for failed operations

The component uses `HttpClientFactory` for API calls, which should be properly configured in dependency injection.

**Section sources**
- [FoodVision.razor.cs](file://FitTrack.Copilot/Components/Pages/FoodVision.razor.cs#L1-L127)

## Authentication Flow Troubleshooting

Authentication uses ASP.NET Core Identity with Razor Components. Common issues include:
- Redirect loops after login
- Missing claims in authentication state
- Cookie authentication failures

The application configures identity cookies and uses `IdentityRevalidatingAuthenticationStateProvider` for state management. Ensure proper configuration of authentication schemes in `Program.cs`.

**Section sources**
- [Program.cs](file://FitTrack.Copilot/Program.cs#L58-L80)

## API Endpoint Errors

### Copilot Vision Endpoints

The `/copilot/vision/estimate` endpoint processes image analysis requests. Issues often stem from:
- Incorrect multipart form data formatting
- Missing or invalid image content type headers
- Base address configuration in HttpClient

Verify the HttpClient base address is set correctly using `NavigationManager.BaseUri`.

### Food Endpoints

The `/api/meals` endpoint saves analyzed food data. Common errors include:
- Invalid JSON payload structure
- Missing required fields (userId, occurredAt)
- Authentication token issues

Ensure payloads match the expected structure with proper field names and data types.

**Section sources**
- [Program.cs](file://FitTrack.Copilot/Program.cs#L99-L100)
- [FoodVision.razor.cs](file://FitTrack.Copilot/Components/Pages/FoodVision.razor.cs#L78-L92)

## Performance Optimization

### Image Size Reduction for Vision Analysis

Reduce image sizes before uploading to improve processing speed and reduce bandwidth:
- Limit image dimensions to 1024px on the longest side
- Compress images to appropriate quality levels
- Use modern formats like WebP when possible

The current implementation allows up to 10MB files, but smaller images typically provide sufficient quality for food recognition.

### Caching USDA Responses

The application has built-in caching configuration through `appsettings.json`:
```json
"AI": {
  "EnableCaching": true,
  "CacheDurationMinutes": 30
}
```

This enables response caching for AI interactions, including USDA API calls. The `AddMemoryCache()` service in `Program.cs` provides the caching infrastructure.

**Section sources**
- [appsettings.json](file://FitTrack.Copilot/appsettings.json#L1-L55)
- [Program.cs](file://FitTrack.Copilot/Program.cs#L26)

### Efficient EF Core Queries

Optimize database queries by:
- Using `Any()` for existence checks instead of retrieving full datasets
- Implementing proper indexing on frequently queried fields
- Using asynchronous methods to avoid blocking threads
- Minimizing data retrieval with projection queries

The current implementation uses `context.Foods.Any()` for efficient existence checking during seeding.

**Section sources**
- [DbInitializer.cs](file://FitTrack/Data/DbInitializer.cs#L13)
- [ApplicationDbContext.cs](file://FitTrack/Data/ApplicationDbContext.cs#L1-L17)

## Security Best Practices

### Handling AI-Generated Content

Implement multiple layers of content safety:
- Use `SensitiveWordFilterChatClient` for content scanning
- Validate all AI responses before display
- Implement user reporting mechanisms for inappropriate content
- Regularly update sensitive word lists

### Protecting User Data

Key security measures include:
- Using HTTPS in production (enforced by HSTS)
- Implementing anti-forgery tokens
- Properly configuring authentication cookies
- Storing API keys securely using user secrets

The application uses `UseHttpsRedirection()` and `UseHsts()` for transport security.

**Section sources**
- [Program.cs](file://FitTrack.Copilot/Program.cs#L114-L117)
- [Program.cs](file://FitTrack.Copilot/Program.cs#L122)

## Logging and Monitoring

### NLog Configuration

The application uses NLog for comprehensive logging with console output. Key configuration elements:
- Custom layout with timestamp, level, logger, and message
- Trace-level logging for detailed diagnostics
- Structured logging with exception formatting

NLog is configured programmatically in `Program.cs` rather than through configuration files.

### Monitoring Key Metrics

Monitor these critical metrics:
- AI response times (alert on >5s)
- Token consumption rates
- Database operation durations
- API error rates
- User authentication success/failure ratios

The `PerformanceMonitorChatClient` provides built-in metrics collection for AI interactions.

**Section sources**
- [Program.cs](file://FitTrack.Copilot/Program.cs#L28-L46)
- [PerformanceMonitorChatClient.cs](file://FitTrack.Copilot/Middleware/PerformanceMonitorChatClient.cs#L1-L139)

## Production Readiness Checklist

- [ ] Configure proper API keys in production environment
- [ ] Set up monitoring and alerting for critical metrics
- [ ] Implement backup strategy for SQLite database
- [ ] Configure proper logging retention policies
- [ ] Test disaster recovery procedures
- [ ] Validate SSL certificate configuration
- [ ] Implement rate limiting for API endpoints
- [ ] Conduct security audit of all dependencies
- [ ] Verify data privacy compliance (GDPR, CCPA)
- [ ] Test performance under expected load

## Incident Response Procedures

1. **Identify**: Monitor logs and metrics for anomalies
2. **Contain**: Isolate affected components if possible
3. **Diagnose**: Use logging data to identify root cause
4. **Resolve**: Apply fix and verify resolution
5. **Communicate**: Notify stakeholders of incident status
6. **Document**: Record incident details and resolution steps
7. **Improve**: Implement preventive measures to avoid recurrence

For database incidents, maintain regular backups and test restoration procedures. For AI service outages, implement graceful degradation to maintain core functionality.