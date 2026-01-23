# Build Process

<cite>
**Referenced Files in This Document**   
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile)
- [.dockerignore](file://FitTrack/.dockerignore)
- [FitTrack.csproj](file://FitTrack/FitTrack/FitTrack.csproj)
- [FitTrack.Copilot.csproj](file://FitTrack/FitTrack.Copilot/FitTrack.Copilot.csproj)
- [FitTrack.sln](file://FitTrack/FitTrack.sln)
</cite>

## Table of Contents
1. [Multi-Stage Docker Build Strategy](#multi-stage-docker-build-strategy)
2. [.dockerignore Configuration and Build Context Optimization](#dockerignore-configuration-and-build-context-optimization)
3. [Project-Specific Build Configuration](#project-specific-build-configuration)
4. [Build Optimization Techniques](#build-optimization-techniques)
5. [Environment-Specific Build Customization](#environment-specific-build-customization)
6. [CI/CD Integration](#cicd-integration)
7. [Troubleshooting Common Build Issues](#troubleshooting-common-build-issues)

## Multi-Stage Docker Build Strategy

The FitTrack solution implements a multi-stage Docker build process for both the main FitTrack application and the FitTrack.Copilot service. This strategy separates the build environment from the runtime environment, resulting in smaller, more secure production images.

The build process consists of four distinct stages:

1. **Base Stage**: Uses the `mcr.microsoft.com/dotnet/aspnet:9.0` runtime image as the foundation for the final container. This minimal image contains only the .NET 9.0 runtime, reducing the attack surface and image size.

2. **Build Stage**: Utilizes the `mcr.microsoft.com/dotnet/sdk:9.0` image, which includes the full .NET SDK required for compilation, NuGet package restoration, and build operations.

3. **Publish Stage**: Inherits from the build stage and executes the `dotnet publish` command to create a self-contained, optimized deployment package with trimmed dependencies.

4. **Final Stage**: Copies the published output from the publish stage to the base runtime image, creating a lean production container.

Both projects follow identical build patterns with project-specific configurations, ensuring consistency across the solution.

**Section sources**
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile#L1-L24)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile#L1-L24)

## .dockerignore Configuration and Build Context Optimization

The `.dockerignore` file located in the root directory plays a critical role in optimizing the Docker build process by excluding unnecessary files and directories from the build context. This reduces the amount of data transferred to the Docker daemon and prevents irrelevant files from being cached, significantly improving build performance.

The current configuration excludes:
- Version control artifacts (`.git`, `.gitignore`)
- IDE-specific files and directories (`.vs`, `.vscode`, `.idea`)
- Build output directories (`bin`, `obj`)
- Configuration files that should not be included in the container (`*.env`, `azds.yaml`)
- Documentation and license files (README.md, LICENSE)

By excluding these files, the build context is minimized, reducing build times and preventing potential security issues from exposing development artifacts in production containers.

**Section sources**
- [.dockerignore](file://FitTrack/.dockerignore#L1-L25)

## Project-Specific Build Configuration

Each project in the solution contains specific build configurations defined in their respective `.csproj` files. Both FitTrack and FitTrack.Copilot target .NET 9.0 and include Docker-specific settings that integrate with Visual Studio's container tools.

Key build parameters include:
- `TargetFramework`: Set to `net9.0` for both projects, ensuring compatibility with the .NET 9.0 SDK and runtime images
- `DockerDefaultTargetOS`: Configured to `Linux` to ensure proper containerization on Linux-based Docker hosts
- `UserSecretsId`: Unique identifiers for each project to manage sensitive configuration data during development

The Dockerfiles use build arguments (`BUILD_CONFIGURATION`) to specify the build configuration (defaulting to Release), allowing flexibility between development and production builds.

**Section sources**
- [FitTrack.csproj](file://FitTrack/FitTrack/FitTrack.csproj#L1-L37)
- [FitTrack.Copilot.csproj](file://FitTrack/FitTrack.Copilot/FitTrack.Copilot.csproj#L1-L71)

## Build Optimization Techniques

The Docker build process incorporates several optimization techniques to improve efficiency and reduce image size:

### Layer Caching
The Dockerfile structure is designed to maximize layer caching by ordering commands from least to most frequently changing:
1. SDK and base image selection
2. Project file copying and package restoration
3. Source code copying
4. Build and publish operations

This approach ensures that NuGet package restoration is cached separately from source code changes, meaning dependency downloads only occur when project files change.

### Trimmed Dependencies
The publish command includes the `/p:UseAppHost=false` parameter, which disables the application host and enables trimming of unused assemblies. This reduces the final image size by eliminating unused code from the .NET runtime and dependencies.

### Minimal Runtime Images
By separating the build and runtime stages, the final container only includes the necessary runtime components from the `mcr.microsoft.com/dotnet/aspnet:9.0` image, rather than the much larger SDK image. This results in a smaller attack surface and faster container startup times.

**Section sources**
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile#L7-L24)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile#L7-L24)

## Environment-Specific Build Customization

The build process can be customized for different environments through several mechanisms:

### Build Configuration
The `BUILD_CONFIGURATION` argument allows switching between Debug and Release configurations. Release builds include optimizations like code trimming and size reduction, while Debug builds prioritize debugging capabilities.

### Conditional Publishing
The `/p:UseAppHost=false` parameter in the publish command ensures consistent behavior across environments by disabling the application host, which can cause issues in containerized environments.

### Environment Variables
While not explicitly defined in the Dockerfiles, environment-specific settings can be injected through the `appsettings.json` files or container environment variables at runtime, allowing the same built image to adapt to different deployment environments.

**Section sources**
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile#L8-L18)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile#L8-L18)

## CI/CD Integration

The standardized Docker build process facilitates seamless integration with CI/CD pipelines. The multi-stage build approach ensures consistent builds across development, staging, and production environments.

Key integration points include:
- Automated builds triggered by code commits
- Consistent build environment via Docker images
- Reproducible builds through versioned base images
- Easy deployment of container images to various hosting platforms

The use of standard .NET Docker images and conventional build patterns ensures compatibility with popular CI/CD platforms like GitHub Actions, Azure DevOps, and Jenkins.

**Section sources**
- [FitTrack.sln](file://FitTrack/FitTrack.sln#L1-L23)
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile#L1-L24)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile#L1-L24)

## Troubleshooting Common Build Issues

### Missing SDKs or Runtime Images
Ensure Docker can pull the required .NET 9.0 images from `mcr.microsoft.com`. Network restrictions or authentication issues may prevent image downloads. Verify connectivity to the Microsoft Container Registry.

### Package Restoration Failures
If `dotnet restore` fails, check:
- Internet connectivity from the build environment
- NuGet feed availability
- Correct project file paths in the COPY commands
- Proper .dockerignore configuration to prevent file conflicts

### Assembly Conflicts
The FitTrack.Copilot project references multiple Semantic Kernel packages with potentially conflicting versions. Ensure all Microsoft.SemanticKernel packages use compatible versions (currently 1.66.0) to prevent runtime assembly loading issues.

### Build Context Issues
Verify that the `.dockerignore` file is properly linked in both project files via the Content Include directive. Missing or incorrect links may result in build context issues or failed container builds.

### Port Configuration
Both containers expose ports 8080 and 8081. When running both services, ensure these ports are properly mapped to avoid conflicts. The USER directive with $APP_UID supports running containers with specific user IDs for security.

**Section sources**
- [Dockerfile](file://FitTrack/FitTrack/Dockerfile#L1-L24)
- [Dockerfile](file://FitTrack/FitTrack.Copilot/Dockerfile#L1-L24)
- [FitTrack.csproj](file://FitTrack/FitTrack/FitTrack.csproj#L27-L28)
- [FitTrack.Copilot.csproj](file://FitTrack/FitTrack.Copilot/FitTrack.Copilot.csproj#L51-L52)