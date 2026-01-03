# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Northwind.App.Backend.csproj", "./"]
RUN dotnet restore "Northwind.App.Backend.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Northwind.App.Backend.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Northwind.App.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create a non-root user for security
RUN addgroup --group appuser --gid 1000 && \
    adduser --uid 1000 --gid 1000 --disabled-password --gecos "" appuser && \
    chown -R appuser:appuser /app

# Copy published app from publish stage
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Expose port (Render.com uses PORT environment variable)
EXPOSE 8080

# Set ASP.NET Core to listen on port from environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Northwind.App.Backend.dll"]
