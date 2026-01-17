using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Northwind.App.Backend.Models;
using Serilog;

// Load .env file if it exists (for local development)
try
{
    Env.Load();
}
catch (Exception)
{
    // .env file not found - continue with environment variables or appsettings.json defaults
}

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

    // Root endpoint - API information page (reads app name from assembly metadata)
    app.MapGet("/", (HttpContext context) =>
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var productName = assembly.GetCustomAttribute<System.Reflection.AssemblyProductAttribute>()?.Product ?? "Northwind API Backend";
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Set no-cache headers
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";

        var html = $$"""
        <!DOCTYPE html>
        <html lang="da">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{{productName}}</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/fomantic-ui@2.9.3/dist/semantic.min.css">
            <style>
                body {
                    background: greywhitesmoke;
                    min-height: 100vh;
                    padding: 2rem 1rem;
                }
                .main-container {
                    max-width: 900px;
                    margin: 0 auto;
                }
                .ui.segment.hero {
                    background: white;
                    border: none;
                    box-shadow: 0 10px 40px rgba(0,0,0,0.15);
                }
                .version-badge {
                    position: absolute;
                    top: 1rem;
                    right: 1rem;
                }
                .api-title {
                    color: #667eea;
                    margin-top: 0.5rem;
                }
                .feature-icon {
                    color: #667eea !important;
                }
            </style>
        </head>
        <body>
            <div class="main-container">
                <div class="ui segment hero">
                    <span class="ui blue label version-badge">v{{version}}</span>
                    
                    <h1 class="ui header api-title">
                        <i class="server icon"></i>
                        <div class="content">
                            {{productName}}
                            <div class="sub header">RESTful API bygget med ASP.NET Core</div>
                        </div>
                    </h1>

                    <div class="ui divider"></div>

                    <div class="ui info message">
                        <div class="header">
                            <i class="info circle icon"></i>
                            Om denne API
                        </div>
                        <p>Dette er en demo/reference implementering der demonstrerer moderne web API best practices inklusiv JWT autentificering, Entity Framework Core, OpenAPI dokumentation og Docker containerisering.</p>
                    </div>

                    <h3 class="ui header">
                        <i class="linkify icon feature-icon"></i>
                        <div class="content">Hurtige Links</div>
                    </h3>

                    <div class="ui stackable four column grid">
                        <div class="column">
                            <a href="/swagger" class="ui fluid blue button">
                                <i class="book icon"></i>
                                API Dokumentation
                            </a>
                        </div>
                        <div class="column">
                            <a href="/health/live" class="ui fluid green button">
                                <i class="heartbeat icon"></i>
                                Sundhedstjek
                            </a>
                        </div>
                        <div class="column">
                            <a href="/appinfo" class="ui fluid teal button">
                                <i class="info icon"></i>
                                App Info
                            </a>
                        </div>
                        <div class="column">
                            <a href="https://northwind-backend-b088.onrender.com/" target="_blank" class="ui fluid purple button">
                                <i class="cloud icon"></i>
                                Live Demo
                            </a>
                        </div>
                    </div>

                    <div class="ui divider"></div>

                    <h3 class="ui header">
                        <i class="shield alternate icon feature-icon"></i>
                        <div class="content">Funktioner</div>
                    </h3>

                    <div class="ui three column stackable grid">
                        <div class="column">
                            <div class="ui segment center aligned">
                                <i class="huge lock icon" style="color: #21ba45;"></i>
                                <h4>JWT Autentificering</h4>
                                <p>Sikker token-baseret autentificering med refresh tokens</p>
                            </div>
                        </div>
                        <div class="column">
                            <div class="ui segment center aligned">
                                <i class="huge database icon" style="color: #2185d0;"></i>
                                <h4>Entity Framework</h4>
                                <p>Code-first ORM med SQLite database understøttelse</p>
                            </div>
                        </div>
                        <div class="column">
                            <div class="ui segment center aligned">
                                <i class="huge docker icon" style="color: #00b5ad;"></i>
                                <h4>Docker Klar</h4>
                                <p>Containeriseret deployment med sundhedstjek</p>
                            </div>
                        </div>
                    </div>

                    <div class="ui divider"></div>

                    <div class="ui horizontal list">
                        <div class="item">
                            <i class="code icon"></i>
                            <div class="content">
                                <div class="header">Framework</div>
                                .NET 10.0
                            </div>
                        </div>
                        <div class="item">
                            <i class="github icon"></i>
                            <div class="content">
                                <div class="header">Repository</div>
                                <a href="https://github.com/devcronberg/Northwind.App.Backend" target="_blank">GitHub</a>
                            </div>
                        </div>
                        <div class="item">
                            <i class="lightning icon"></i>
                            <div class="content">
                                <div class="header">Miljø</div>
                                {{environment}}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </body>
        </html>
        """;

        return Results.Content(html, "text/html");
    });

    Log.Information("Northwind.App.Backend starting...");

    // Log local URL in Development mode
    if (app.Environment.IsDevelopment())
    {
        var urls = app.Configuration["ASPNETCORE_URLS"] ?? app.Configuration["urls"] ?? "http://localhost:5000";
        Log.Information("Running in Development mode - Local URL: {Urls}", urls);
    }

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