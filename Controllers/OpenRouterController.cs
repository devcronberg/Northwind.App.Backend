using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.App.Backend.Helpers;
using Northwind.App.Backend.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Northwind.App.Backend.Controllers;

/// <summary>
/// API for OpenRouter AI integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OpenRouterController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterController> _logger;
    private readonly NorthwindContext _db;

    public OpenRouterController(IConfiguration configuration, ILogger<OpenRouterController> logger, NorthwindContext db)
    {
        _configuration = configuration;
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Test OpenRouter API with a prompt
    /// </summary>
    /// <param name="prompt">The prompt to send to the AI</param>
    [HttpGet("test")]
    [SwaggerOperation(Summary = "Test OpenRouter AI", Description = "Sends a prompt to OpenRouter and returns the AI response")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Test([FromQuery] string prompt)
    {
        try
        {
            // Validate prompt
            if (string.IsNullOrWhiteSpace(prompt))
            {
                _logger.LogWarning("Test called with empty prompt");
                return Ok(new { success = false, message = "Prompt is required" });
            }

            // Get configuration from environment
            var url = _configuration["OpenRouter:Url"];
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"];

            // Validate configuration
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogError("OpenRouter:Url is not configured");
                return Ok(new { success = false, message = "OpenRouter URL is not configured" });
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("OpenRouter:ApiKey is not configured");
                return Ok(new { success = false, message = "OpenRouter API key is not configured" });
            }

            if (string.IsNullOrEmpty(model))
            {
                _logger.LogError("OpenRouter:Model is not configured");
                return Ok(new { success = false, message = "OpenRouter model is not configured" });
            }

            _logger.LogInformation("Processing OpenRouter test request with prompt: {Prompt}", prompt);

            // Call OpenRouter
            var response = await OpenRouterHelper.CallOpenRouterAsync(
                url,
                apiKey,
                model,
                prompt,
                _logger);

            return Ok(new { success = response.Success, message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in OpenRouter test: {Message}", ex.Message);
            return Ok(new { success = false, message = $"Unexpected error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Find a recipe based on products
    /// </summary>
    /// <param name="productIds">Array of product IDs to include in the recipe</param>
    [HttpGet("FindRecipe")]
    [SwaggerOperation(Summary = "Find recipe from products", Description = "Generates a recipe based on selected products using AI")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> FindRecipe([FromQuery] int[] productIds)
    {
        try
        {
            // Validate product IDs
            if (productIds == null || productIds.Length == 0)
            {
                _logger.LogWarning("FindRecipe called with no product IDs");
                return Ok(new { success = false, message = "At least one product ID is required" });
            }

            _logger.LogInformation("Finding recipe for {Count} products: {ProductIds}",
                productIds.Length, string.Join(", ", productIds));

            // Fetch products with categories
            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.ProductId))
                .AsNoTracking()
                .ToListAsync();

            // Check if all products were found
            if (products.Count != productIds.Length)
            {
                var foundIds = products.Select(p => p.ProductId).ToList();
                var missingIds = productIds.Except(foundIds).ToList();
                _logger.LogWarning("Some product IDs not found: {MissingIds}", string.Join(", ", missingIds));
                return Ok(new
                {
                    success = false,
                    message = $"Following product IDs were not found: {string.Join(", ", missingIds)}"
                });
            }

            // Build prompt
            var productList = string.Join(", ", products.Select(p =>
                $"{p.ProductName}" + (p.Category != null ? $" ({p.Category.CategoryName})" : "")));

            var prompt = $@"Du er en kreativ kok. Lav en munter og frisk opskrift på dansk der indeholder ALLE følgende ingredienser: {productList}

Opskriften skal være i HTML format med følgende struktur:
- Hele opskriften skal være indpakket i en <div>
- <h2> til titlen (en sjov og kreativ titel)
- <h3> til sektioner (Ingredienser, Fremgangsmåde)
- <ul> og <li> til ingredienslisten - inkluder ALLE produkter fra listen ovenfor
- <ol> og <li> til fremgangsmåden
- Ingen emojis

VIGTIGT: Alle produkter fra listen skal fremgå tydeligt i ingredienslisten.

Vær kreativ og munter i din tilgang! Returner KUN HTML - ingen forklaringer eller markdown.";

            _logger.LogInformation("Generated prompt for {Count} products", products.Count);
            _logger.LogInformation(prompt);

            // Get OpenRouter configuration
            var url = _configuration["OpenRouter:Url"];
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"];

            // Validate configuration
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
            {
                _logger.LogError("OpenRouter configuration is incomplete");
                return Ok(new { success = false, message = "OpenRouter is not properly configured" });
            }

            // Call OpenRouter
            var response = await OpenRouterHelper.CallOpenRouterAsync(
                url,
                apiKey,
                model,
                prompt,
                _logger);

            if (response.Success)
            {
                _logger.LogInformation("Successfully generated recipe for products: {ProductIds}",
                    string.Join(", ", productIds));
            }

            return Ok(new { success = response.Success, message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in FindOpskrift: {Message}", ex.Message);
            return Ok(new { success = false, message = $"Unexpected error: {ex.Message}" });
        }
    }
}
