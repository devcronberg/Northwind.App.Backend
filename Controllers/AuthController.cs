using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace Northwind.App.Backend.Controllers;

/// <summary>
/// Authentication controller for JWT token management
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    // Demo users - in production, use a database
    private static readonly List<DemoUser> _users = new()
    {
        new DemoUser { Username = "admin", Password = "admin", Role = "Admin" },
        new DemoUser { Username = "user", Password = "user", Role = "User" }
    };

    // In-memory refresh token store - in production, use a database
    private static readonly Dictionary<string, RefreshTokenInfo> _refreshTokens = new();

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Login", Description = "Authenticate with username and password to receive JWT tokens")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var user = _users.FirstOrDefault(u => 
            u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) && 
            u.Password == request.Password);

        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = "Invalid username or password",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var tokens = GenerateTokens(user);
        
        _logger.LogInformation("User {Username} logged in successfully", request.Username);
        
        return Ok(tokens);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Refresh token", Description = "Get a new access token using a valid refresh token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        _logger.LogInformation("Token refresh attempt");

        if (!_refreshTokens.TryGetValue(request.RefreshToken, out var tokenInfo))
        {
            _logger.LogWarning("Invalid refresh token used");
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid refresh token",
                Detail = "The refresh token is invalid or has been revoked",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (tokenInfo.ExpiresAt < DateTime.UtcNow)
        {
            _refreshTokens.Remove(request.RefreshToken);
            _logger.LogWarning("Expired refresh token used for user: {Username}", tokenInfo.Username);
            return Unauthorized(new ProblemDetails
            {
                Title = "Refresh token expired",
                Detail = "The refresh token has expired. Please login again.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var user = _users.FirstOrDefault(u => u.Username == tokenInfo.Username);
        if (user == null)
        {
            _refreshTokens.Remove(request.RefreshToken);
            return Unauthorized(new ProblemDetails
            {
                Title = "User not found",
                Detail = "The user associated with this token no longer exists",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Remove old refresh token
        _refreshTokens.Remove(request.RefreshToken);

        // Generate new tokens
        var tokens = GenerateTokens(user);
        
        _logger.LogInformation("Token refreshed for user: {Username}", user.Username);
        
        return Ok(tokens);
    }

    /// <summary>
    /// Logout and invalidate refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(Summary = "Logout", Description = "Invalidate the refresh token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        var username = User.Identity?.Name ?? "unknown";
        _logger.LogInformation("Logout for user: {Username}", username);

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            _refreshTokens.Remove(request.RefreshToken);
        }

        return NoContent();
    }

    /// <summary>
    /// Get current user info from token
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(Summary = "Get current user", Description = "Returns information about the authenticated user")]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var username = User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new UserInfo
        {
            Username = username,
            Role = role
        });
    }

    private TokenResponse GenerateTokens(DemoUser user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenExpDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        
        _refreshTokens[refreshToken] = new RefreshTokenInfo
        {
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpDays)
        };

        var accessTokenExpMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = accessTokenExpMinutes * 60,
            TokenType = "Bearer"
        };
    }

    private string GenerateAccessToken(DemoUser user)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "Northwind.App.Backend";
        var audience = _configuration["Jwt:Audience"] ?? "Northwind.App.Frontend";
        var expirationMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

#region Models

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class RefreshRequest
{
    public required string RefreshToken { get; set; }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class UserInfo
{
    public string? Username { get; set; }
    public string? Role { get; set; }
}

internal class DemoUser
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
}

internal class RefreshTokenInfo
{
    public required string Username { get; set; }
    public DateTime ExpiresAt { get; set; }
}

#endregion
