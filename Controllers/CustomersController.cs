using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.App.Backend.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Northwind.App.Backend.Controllers;

/// <summary>
/// API for managing customers. Contains both public and authenticated endpoints.
/// </summary>
[ApiController]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly NorthwindContext _db;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(NorthwindContext db, ILogger<CustomersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ============================================
    // PUBLIC ENDPOINTS (No authentication)
    // ============================================

    /// <summary>
    /// Get all customers
    /// </summary>
    /// <param name="skip">Number of customers to skip (default: 0)</param>
    /// <param name="take">Number of customers to return (default: 1000)</param>
    [HttpGet("api/public/customers")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Get all customers", Description = "Returns a paginated list of all customers")]
    [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAllPublic([FromQuery] int skip = 0, [FromQuery] int take = 1000)
    {
        _logger.LogInformation("Getting customers with skip={Skip}, take={Take} (public)", skip, take);

        var customers = await _db.Customers
            .AsNoTracking()
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return Ok(customers);
    }

    /// <summary>
    /// Get a customer by ID
    /// </summary>
    [HttpGet("api/public/customers/{id:int}")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Get customer by ID", Description = "Returns a single customer by their ID")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Customer>> GetByIdPublic(int id)
    {
        _logger.LogInformation("Getting customer {CustomerId} (public)", id);

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"Customer with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(customer);
    }

    /// <summary>
    /// Get all customers with their orders sorted by revenue
    /// </summary>
    /// <param name="skip">Number of customers to skip (default: 0)</param>
    /// <param name="take">Number of customers to return (default: 1000)</param>
    /// <param name="maxOrdersPerCustomer">Maximum number of orders per customer (default: 10)</param>
    [HttpGet("api/public/customers-with-orders")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Get all customers with orders", Description = "Returns paginated customers with their orders, sorted by highest revenue first")]
    [ProducesResponseType(typeof(IEnumerable<CustomerWithOrdersResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CustomerWithOrdersResponse>>> GetAllCustomersWithOrdersPublic(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 1000,
        [FromQuery] int maxOrdersPerCustomer = 10)
    {
        _logger.LogInformation("Getting customers with orders, skip={Skip}, take={Take}, maxOrdersPerCustomer={MaxOrdersPerCustomer} (public)", skip, take, maxOrdersPerCustomer);

        // First get customers sorted by revenue
        var customerIdsWithRevenue = await _db.Customers
            .AsNoTracking()
            .Select(c => new
            {
                CustomerId = c.CustomerId,
                TotalRevenue = c.Orders
                    .SelectMany(o => o.OrderDetails)
                    .Sum(od => (od.Product!.Price ?? 0) * (od.Quantity ?? 0))
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Skip(skip)
            .Take(take)
            .Select(x => x.CustomerId)
            .ToListAsync();

        // Then get full customer data with orders for those customers
        var result = new List<CustomerWithOrdersResponse>();

        foreach (var customerId in customerIdsWithRevenue)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer != null)
            {
                var orders = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.CustomerId == customerId)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(maxOrdersPerCustomer)
                    .Include(o => o.Employee)
                    .Include(o => o.Shipper)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product!)
                            .ThenInclude(p => p.Supplier)
                    .ToListAsync();

                var totalOrderCount = await _db.Orders.CountAsync(o => o.CustomerId == customerId);

                result.Add(new CustomerWithOrdersResponse
                {
                    Customer = customer,
                    Orders = orders,
                    TotalOrderCount = totalOrderCount,
                    ReturnedOrderCount = orders.Count
                });
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a customer by ID including their orders
    /// </summary>
    [HttpGet("api/public/customers/{id:int}/orders")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Get customer with orders", Description = "Returns a customer with their most recent orders (limited)")]
    [ProducesResponseType(typeof(CustomerWithOrdersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerWithOrdersResponse>> GetByIdWithOrdersPublic(int id, [FromQuery] int maxOrders = 10)
    {
        _logger.LogInformation("Getting customer {CustomerId} with max {MaxOrders} orders (public)", id, maxOrders);

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"Customer with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Get orders separately with limit
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.OrderDate)
            .Take(maxOrders)
            .Include(o => o.Employee)
            .Include(o => o.Shipper)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product!)
                    .ThenInclude(p => p.Supplier)
            .ToListAsync();

        var totalOrderCount = await _db.Orders.CountAsync(o => o.CustomerId == id);

        return Ok(new CustomerWithOrdersResponse
        {
            Customer = customer,
            Orders = orders,
            TotalOrderCount = totalOrderCount,
            ReturnedOrderCount = orders.Count
        });
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost("api/public/customers")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Create a new customer", Description = "Creates a new customer and returns the created resource")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Customer>> CreatePublic([FromBody] Customer customer)
    {
        _logger.LogInformation("Creating new customer: {CustomerName} (public)", customer.CustomerName);

        // Ensure ID is not set by client
        customer.CustomerId = 0;
        customer.Orders = new List<Order>();

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created customer {CustomerId}", customer.CustomerId);

        return CreatedAtAction(
            nameof(GetByIdPublic),
            new { id = customer.CustomerId },
            customer);
    }

    /// <summary>
    /// Update a customer (full replacement)
    /// </summary>
    [HttpPut("api/public/customers/{id:int}")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Update a customer", Description = "Replaces all properties of an existing customer")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Customer>> UpdatePublic(int id, [FromBody] Customer input)
    {
        _logger.LogInformation("Updating customer {CustomerId} (public)", id);

        var customer = await _db.Customers.FindAsync(id);

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found for update", id);
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"Customer with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        customer.CustomerName = input.CustomerName;
        customer.ContactName = input.ContactName;
        customer.Address = input.Address;
        customer.City = input.City;
        customer.PostalCode = input.PostalCode;
        customer.Country = input.Country;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated customer {CustomerId}", id);

        return Ok(customer);
    }

    /// <summary>
    /// Partially update a customer
    /// </summary>
    [HttpPatch("api/public/customers/{id:int}")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Partially update a customer", Description = "Updates only the provided properties of an existing customer")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Customer>> PatchPublic(int id, [FromBody] Customer input)
    {
        _logger.LogInformation("Patching customer {CustomerId} (public)", id);

        var customer = await _db.Customers.FindAsync(id);

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found for patch", id);
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"Customer with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Only update properties that are provided (not null)
        if (input.CustomerName != null) customer.CustomerName = input.CustomerName;
        if (input.ContactName != null) customer.ContactName = input.ContactName;
        if (input.Address != null) customer.Address = input.Address;
        if (input.City != null) customer.City = input.City;
        if (input.PostalCode != null) customer.PostalCode = input.PostalCode;
        if (input.Country != null) customer.Country = input.Country;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Patched customer {CustomerId}", id);

        return Ok(customer);
    }

    /// <summary>
    /// Delete a customer
    /// </summary>
    [HttpDelete("api/public/customers/{id:int}")]
    [Tags("Public Customers")]
    [SwaggerOperation(Summary = "Delete a customer", Description = "Permanently deletes a customer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePublic(int id)
    {
        _logger.LogInformation("Deleting customer {CustomerId} (public)", id);

        var customer = await _db.Customers.FindAsync(id);

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found for deletion", id);
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"Customer with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted customer {CustomerId}", id);

        return NoContent();
    }

    // ============================================
    // AUTHENTICATED ENDPOINTS (Requires JWT)
    // ============================================

    /// <summary>
    /// Get all customers (requires authentication)
    /// </summary>
    /// <param name="skip">Number of customers to skip (default: 0)</param>
    /// <param name="take">Number of customers to return (default: 1000)</param>
    [HttpGet("api/customers")]
    [Authorize]
    [Tags("Customers (Authenticated)")]
    [SwaggerOperation(Summary = "Get all customers", Description = "Returns a paginated list of all customers. Requires authentication.")]
    [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAllAuthenticated([FromQuery] int skip = 0, [FromQuery] int take = 1000)
    {
        var username = User.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {Username} getting customers with skip={Skip}, take={Take} (authenticated)", username, skip, take);

        var customers = await _db.Customers
            .AsNoTracking()
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return Ok(customers);
    }
}
