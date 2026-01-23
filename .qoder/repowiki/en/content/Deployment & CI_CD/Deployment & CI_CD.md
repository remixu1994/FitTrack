# Deployment & CI/CD

<cite>
**Referenced Files in This Document**   
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile)
- [appsettings.json](file://FitTrack/FitTrack/appsettings.json)
- [appsettings.json](file://FitTrack/FitTrack.Copilot/appsettings.json)
- [Program.cs](file://FitTrack/FitTrack/Program.cs)
- [Program.cs](file://FitTrack/FitTrack.Copilot/Program.cs)
- [.dockerignore](file://FitTrack/.dockerignore)
</cite>

## Table of Contents
1. [Docker Build Instructions](#docker-build-instructions)
2. [Container Configuration](#container-configuration)
3. [Production Considerations](#production-considerations)
4. [Platform Deployment](#platform-deployment)
5. [CI/CD Pipeline Setup](#cicd-pipeline-setup)
6. [Performance Tuning](#performance-tuning)
7. [Health Checks and Monitoring](#health-checks-and-monitoring)

## Docker Build Instructions

The FitTrack application utilizes multi-stage Docker builds for both the main application and the Copilot service. The Dockerfiles implement optimization techniques to reduce image size and improve build efficiency.

Both Dockerfiles follow a standard multi-stage pattern:
1. Base stage with ASP.NET runtime
2. Build stage with .NET SDK
3. Publish stage for optimized output
4. Final stage copying published artifacts

Key optimization features include:
- Use of `mcr.microsoft.com/dotnet/aspnet:9.0` and `mcr.microsoft.com/dotnet/sdk:9.0` base images
- Multi-stage build process that separates build dependencies from runtime
- Copying only the project file first for efficient layer caching
- Use of `--from=publish` to copy only published artifacts to the final image
- Setting `USER $APP_UID` for security
- Exposing ports 8080 and 8081 for HTTP/HTTPS traffic

The build process can be executed with:
```bash
docker build -t fittrack-app -f FitTrack/FitTrack/Dockerfile .
docker build -t fittrack-copilot -f FitTrack/Fitilot/Dockerfile .
```

Build-time configuration is supported via the `BUILD_CONFIGURATION` argument (default: Release).

**Section sources**
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile)

## Container Configuration

When running FitTrack containers in production, proper environment variable configuration is essential for database connectivity and AI services.

### Environment Variables

The following environment variables should be configured:

**Database Configuration:**
- `ConnectionStrings__DefaultConnection`: SQLite connection string (default: `DataSource=Data\\app.db;Cache=Shared`)

**AI Service Configuration (Copilot):**
- `AI__ApiKey`: API key for Azure OpenAI service
- `TokenAI__ApiKey`: API key for Token AI service
- `USDA__ApiKey`: API key for USDA FoodData Central
- `AI__Endpoint`: Endpoint URL for Azure OpenAI
- `TokenAI__Endpoint`: Endpoint URL for Token AI

These can be set via Docker environment variables, Kubernetes secrets, or platform-specific configuration mechanisms.

### Runtime Configuration

The application uses NLog for logging in the Copilot service, configured programmatically in `Program.cs`. The logging configuration includes:
- Colored console output with timestamp, log level, logger name, and message
- Trace-level logging for comprehensive diagnostics
- Integration with ASP.NET Core logging system

**Section sources**
- [appsettings.json](file://FitTrack/FitTrack/appsettings.json)
- [appsettings.json](file://FitTrack/FitTrack.Copilot/appsettings.json)
- [Program.cs](file://FitTrack/FitTrack.Copilot/Program.cs)

## Production Considerations

### HTTPS Termination

The application implements HTTPS in production environments:
- `app.UseHttpsRedirection()` redirects HTTP to HTTPS
- `app.UseHsts()` enables HTTP Strict Transport Security in non-development environments
- Ports 8080 (HTTP) and 8081 (HTTPS) are exposed in containers

For production deployments, consider terminating SSL at the load balancer or reverse proxy level and communicating with containers over HTTP.

### Logging Aggregation

The Copilot service uses NLog for structured logging with the following configuration:
- Console target with detailed layout including timestamp, log level, logger name, message, and exception details
- Trace-level logging capability for comprehensive monitoring
- Integration with ASP.NET Core logging pipeline

In production environments, consider redirecting logs to centralized logging solutions like ELK stack, Splunk, or cloud-native logging services.

### Database Migration Automation

The application automatically applies database migrations at startup:
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    DbInitializer.Initialize(context, env);
}
```

This ensures the database schema is always up-to-date when the application starts. The migrations include:
- Identity schema for user authentication
- Foods table for nutritional data
- DailyFoodRecords table for tracking user consumption

**Section sources**
- [Program.cs](file://FitTrack/FitTrack/Program.cs)
- [Program.cs](file://FitTrack/FitTrack.Copilot/Program.cs)
- [AddFoodsAndDailyFoodRecords.cs](file://FitTrack/FitTrack/Data\Migrations/20250826084318_AddFoodsAndDailyFoodRecords.cs)

## Platform Deployment

### Azure App Service

To deploy to Azure App Service:
1. Create a Web App with container support
2. Configure the container to use the FitTrack Docker image
3. Set environment variables for database connection and AI services
4. Configure HTTPS with a custom domain and TLS certificate
5. Enable application logging and configure log retention
6. Set up deployment slots for staging/production workflow

### AWS ECS

For AWS ECS deployment:
1. Push Docker images to Amazon ECR
2. Create an ECS task definition with appropriate CPU and memory limits
3. Configure environment variables as task definition parameters
4. Set up an Application Load Balancer with HTTPS termination
5. Configure CloudWatch for logging and monitoring
6. Use ECS service auto-scaling based on CPU/memory metrics

### Kubernetes

Kubernetes deployment requires:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: fittrack-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: fittrack
  template:
    metadata:
      labels:
        app: fittrack
    spec:
      containers:
      - name: app
        image: fittrack-app:latest
        ports:
        - containerPort: 8080
        envFrom:
        - secretRef:
            name: fittrack-secrets
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
```

Include separate deployments for the main application and Copilot service, with appropriate service definitions and ingress rules.

**Section sources**
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile)
- [Program.cs](file://FitTrack/FitTrack/Program.cs)

## CI/CD Pipeline Setup

While no explicit CI/CD configuration files were found in the repository, a robust pipeline should include:

### GitHub Actions Pipeline

```yaml
name: Build and Deploy
on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v2
      
    - name: Login to DockerHub
      uses: docker/login-action@v2
      with:
        username: ${{ secrets.DOCKERHUB_USERNAME }}
        password: ${{ secrets.DOCKERHUB_TOKEN }}
        
    - name: Build and push
      uses: docker/build-push-action@v4
      with:
        context: .
        file: FitTrack/FitTrack/Dockerfile
        push: true
        tags: user/fittrack:latest
        
    - name: Run tests
      run: dotnet test
```

The pipeline should:
1. Trigger on pull requests and main branch pushes
2. Build Docker images using the provided Dockerfiles
3. Run automated tests
4. Push images to a container registry
5. Deploy to staging/production environments with approval gates

Consider implementing separate workflows for the main application and Copilot service, with appropriate testing and deployment strategies.

**Section sources**
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile)

## Performance Tuning

### Container Resource Limits

Recommended container resource configuration:
- **Memory**: Minimum 512MB, recommended 1-2GB depending on load
- **CPU**: Minimum 0.25 vCPU, recommended 0.5-1 vCPU for production
- **Storage**: Ensure adequate storage for the SQLite database and potential log files

### Thread Pool Settings

The application benefits from ASP.NET Core's default thread pool configuration. No custom thread pool settings are configured, relying on the runtime defaults which automatically adjust based on workload.

### AI Request Batching

The Copilot service handles AI requests through:
- HTTP client configuration with 10-second timeout
- Memory cache for response caching (when enabled)
- Sequential processing of vision nutrition requests
- Function calling capabilities for tool integration

For high-volume scenarios, consider implementing request queuing and batch processing to manage AI service rate limits and costs.

**Section sources**
- [Program.cs](file://FitTrack/FitTrack.Copilot/Program.cs)
- [appsettings.json](file://FitTrack/FitTrack.Copilot/appsettings.json)

## Health Checks and Monitoring

### Health Check Endpoint

The application does not expose a dedicated health check endpoint, but standard ASP.NET Core middleware provides indicators:
- HTTP 200 response from root endpoint indicates application is running
- `/openapi/v1.json` endpoint in Copilot service indicates API availability
- Static asset endpoints indicate file serving capability

Consider implementing dedicated health checks for production:
- Database connectivity check
- AI service availability check
- Storage space monitoring
- Background service health

### Monitoring Strategies

Recommended monitoring approach:
1. **Application Performance**: Monitor response times, error rates, and throughput
2. **Resource Utilization**: Track CPU, memory, and storage usage
3. **AI Service Integration**: Monitor API call rates, latency, and error rates for AI services
4. **Database Performance**: Monitor query performance and connection pool usage
5. **User Activity**: Track active users, session duration, and feature usage

Leverage the NLog integration for detailed application logging, and consider adding Application Insights (Azure), CloudWatch (AWS), or other APM solutions for comprehensive monitoring.

**Section sources**
- [Program.cs](file://FitTrack/FitTrack.Copilot/Program.cs)
- [CopilotVisionEndpoints.cs](file://FitTrack/FitTrack.Copilot/Endpoints/CopilotVisionEndpoints.cs)