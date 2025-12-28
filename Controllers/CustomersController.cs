using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.App.Backend.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Northwind.App.Backend.Controllers;

/// <summary>
/// Protected API for managing customers. Requires authentication.
/// </summary>
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
[Tags("Customers (Authenticated)")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly NorthwindContext _db;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(NorthwindContext db, ILogger<CustomersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all customers (requires authentication)
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get all customers", Description = "Returns a list of all customers. Requires authentication.")]
    [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll()
    {
        var username = User.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {Username} getting all customers", username);

        var customers = await _db.Customers
            .AsNoTracking()
            .ToListAsync();

        return Ok(customers);
    }
}
