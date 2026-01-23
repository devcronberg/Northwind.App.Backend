namespace Northwind.App.Backend.Models;

/// <summary>
/// Response model for OpenRouter API calls
/// </summary>
public class OpenRouterResponse
{
    /// <summary>
    /// Indicates if the API call was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The AI response message or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
