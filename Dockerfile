# ===================================================================
# STAGE 1: Build
# Uses the full .NET SDK image (includes compiler, build tools, etc.)
# This stage compiles the application but won't be in the final image
# ===================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first to leverage Docker layer caching
# If dependencies haven't changed, Docker will reuse this layer
# This significantly speeds up rebuilds when only source code changes
COPY ["Northwind.App.Backend.csproj", "./"]

# Restore NuGet packages based on the project file
# This downloads all dependencies defined in the .csproj file
RUN dotnet restore "Northwind.App.Backend.csproj"

# Now copy the rest of the source code
# This is done after restore to maximize cache efficiency
COPY . .

# Build the application in Release mode
# -c Release: optimized compilation with no debug symbols
# -o /app/build: output directory for build artifacts
# --warnaserror: treat all warnings as errors - build fails if warnings exist
RUN dotnet build "Northwind.App.Backend.csproj" -c Release -o /app/build --warnaserror

# ===================================================================
# STAGE 2: Publish
# Creates a deployment-ready version of the app
# ===================================================================
FROM build AS publish

# Publish creates a self-contained deployment package
# -c Release: use release configuration
# -o /app/publish: output directory for published files
# /p:UseAppHost=false: don't create a native executable (use 'dotnet' command instead)
#   This makes the container smaller and more portable
# --warnaserror: treat all warnings as errors - publish fails if warnings exist
RUN dotnet publish "Northwind.App.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false --warnaserror

# ===================================================================
# STAGE 3: Final Runtime
# Uses lightweight ASP.NET runtime image (no SDK, much smaller)
# This is the only stage that ends up in the final Docker image
# ===================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create a non-root user for security best practices
# Running as root is a security risk if the container is compromised
# GID/UID 1001 to avoid conflicts with existing groups in base image
RUN groupadd -g 1001 appuser && \
    useradd -u 1001 -g 1001 -m -s /bin/bash appuser && \
    chown -R appuser:appuser /app

# Copy the published application from the publish stage
# --from=publish: copies from the previous build stage
# --chown=appuser:appuser: sets correct ownership for the non-root user
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch to the non-root user
# All subsequent commands and the application will run as this user
USER appuser

# Document which port the application uses
# EXPOSE doesn't actually publish the port - it's documentation for users
# Render.com and other platforms will use the PORT environment variable
EXPOSE 8080

# Configure ASP.NET Core to listen on the correct port
# ${PORT:-8080}: uses PORT env var if set, otherwise defaults to 8080
# http://+: listen on all network interfaces (0.0.0.0)
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

# Set the environment to Production
# This disables developer exception pages and enables optimizations
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check - verifies the container is healthy and ready to serve traffic
# Runs every 30 seconds, times out after 3 seconds, starts checking after 10 seconds
# Checks the /health/live endpoint which returns 200 if the application is running
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:${PORT:-8080}/health/live || exit 1

# Define how to start the application
# Uses 'dotnet' to run the compiled DLL
ENTRYPOINT ["dotnet", "Northwind.App.Backend.dll"]
