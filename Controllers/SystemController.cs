using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Northwind.App.Backend.Controllers;

[ApiController]
public class SystemController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemController> _logger;
    private readonly HealthCheckService _healthCheckService;

    public SystemController(
        IConfiguration configuration,
        ILogger<SystemController> logger,
        HealthCheckService healthCheckService)
    {
        _configuration = configuration;
        _logger = logger;
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Liveness check - is the application alive?
    /// Supports both GET and HEAD requests for container orchestration
    /// </summary>
    [HttpGet("health/live")]
    [HttpHead("health/live")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public IActionResult LivenessCheck()
    {
        _logger.LogDebug("Liveness check called");
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness check - is the application ready to receive traffic?
    /// Supports both GET and HEAD requests for container orchestration
    /// </summary>
    [HttpGet("health/ready")]
    [HttpHead("health/ready")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> ReadinessCheck()
    {
        _logger.LogDebug("Readiness check called");

        var healthReport = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = healthReport.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = healthReport.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };

        if (healthReport.Status == HealthStatus.Healthy)
            return Ok(response);

        return StatusCode(503, response);
    }

    [HttpGet("test")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(500)]
    [ProducesResponseType(400)]
    [Produces("text/plain")]
    public IActionResult Test(string? text)
    {
        _logger.LogInformation("GET /test: {Text}", text);
        return Content(text ?? "", "text/plain");
    }

    /// <summary>
    /// Test endpoint that throws an exception - to demonstrate Problem Details
    /// </summary>
    [HttpGet("test/error")]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public IActionResult TestError()
    {
        _logger.LogWarning("Test error endpoint called - will throw exception");
        throw new InvalidOperationException("This is a test exception to demonstrate Problem Details");
    }

    [HttpGet("version")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(500)]
    [Produces("text/plain")]
    public IActionResult Version()
    {
        _logger.LogDebug("GET /version");
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return Content(version!.ToString(), "text/plain");
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    [Produces("application/json")]
    public IActionResult Config()
    {
        _logger.LogDebug("GET /config");

        var info = new
        {
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
            ApplicationName = _configuration["ApplicationName"] ?? Assembly.GetExecutingAssembly().GetName().Name,
            Urls = _configuration["urls"] ?? _configuration["ASPNETCORE_URLS"] ?? "Not configured",
            ContentRoot = _configuration["contentRoot"] ?? Directory.GetCurrentDirectory(),
            Logging = new
            {
                DefaultLevel = _configuration["Logging:LogLevel:Default"] ?? "Not configured",
                AspNetCoreLevel = _configuration["Logging:LogLevel:Microsoft.AspNetCore"] ?? "Not configured"
            }
        };

        return Ok(info);
    }

}
