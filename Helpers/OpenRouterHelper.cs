using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Northwind.App.Backend.Models;

namespace Northwind.App.Backend.Helpers;

/// <summary>
/// Helper class for calling OpenRouter AI API
/// </summary>
public static class OpenRouterHelper
{
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Calls OpenRouter API with a prompt and returns the response
    /// </summary>
    /// <param name="url">OpenRouter API URL</param>
    /// <param name="apiKey">OpenRouter API key</param>
    /// <param name="model">Model to use (e.g., anthropic/claude-sonnet-4.5)</param>
    /// <param name="prompt">User prompt/question</param>
    /// <param name="logger">Logger instance for structured logging</param>
    /// <returns>OpenRouterResponse with success status and message</returns>
    public static async Task<OpenRouterResponse> CallOpenRouterAsync(
        string url,
        string? apiKey,
        string model,
        string prompt,
        ILogger logger)
    {
        // Validate API key
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogError("OpenRouter API key is not configured");
            return new OpenRouterResponse
            {
                Success = false,
                Message = "OpenRouter API key is not configured. Please set OpenRouter__ApiKey in environment variables."
            };
        }

        try
        {
            logger.LogInformation("Calling OpenRouter API with model {Model}", model);

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Set headers
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            // Build request body
            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send request
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("OpenRouter API returned error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                return new OpenRouterResponse
                {
                    Success = false,
                    Message = $"OpenRouter API error: {response.StatusCode}. {errorContent}"
                };
            }

            // Parse response
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            logger.LogInformation("OpenRouter API call successful, received {Length} characters",
                content?.Length ?? 0);

            return new OpenRouterResponse
            {
                Success = true,
                Message = content ?? string.Empty
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error calling OpenRouter API: {Message}", ex.Message);
            return new OpenRouterResponse
            {
                Success = false,
                Message = $"Network error: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON parsing error from OpenRouter API: {Message}", ex.Message);
            return new OpenRouterResponse
            {
                Success = false,
                Message = $"Failed to parse API response: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error calling OpenRouter API: {Message}", ex.Message);
            return new OpenRouterResponse
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
