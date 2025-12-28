using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Northwind.App.Backend.Models;
using Serilog;

// Configure Serilog bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog as the logging provider - reads configuration from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

    // Entity Framework - SQLite database (read-only in container)
    builder.Services.AddDbContext<NorthwindContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("NorthwindDb")));

    // JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Northwind.App.Backend";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Northwind.App.Frontend";
    
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });
    builder.Services.AddAuthorization();

    // Health checks
    builder.Services.AddHealthChecks();

    // Problem Details for consistent error responses (RFC 7807)
    builder.Services.AddProblemDetails();

    // Response compression for better performance
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // Add CORS policy to allow all origins
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
                });
    });

    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.EnableAnnotations();
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Northwind.App.Backend",
            Version = "v1",
            Description = "Northwind.App.Backend API"
        });
        
        // JWT Bearer authentication in Swagger
        c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme."
        });
        c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearer", document)] = []
        });
    });

    var app = builder.Build();

    // Global exception handling - returns Problem Details
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    // Response compression
    app.UseResponseCompression();

    // Serilog request logging - logs all HTTP requests
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseRouting();
    app.UseCors();
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();

    // Health check endpoints
    app.MapHealthChecks("/health");

    Log.Information("Northwind.App.Backend starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}