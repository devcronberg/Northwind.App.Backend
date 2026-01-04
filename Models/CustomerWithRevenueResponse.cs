using Northwind.App.Backend.Models;

namespace Northwind.App.Backend.Controllers;

/// <summary>
/// Response for customer with revenue information
/// </summary>
public class CustomerWithRevenueResponse
{
    public Customer Customer { get; set; } = null!;
    public int TotalOrderCount { get; set; }
    public double TotalRevenue { get; set; }
}
