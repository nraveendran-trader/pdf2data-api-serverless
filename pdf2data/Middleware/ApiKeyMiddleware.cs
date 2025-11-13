namespace pdf2data.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger, string apiKey)
    {
        _next = next;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health checks and Swagger UI
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (path == "/" ||
            path.StartsWith("/health") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/_framework") ||
            path.StartsWith("/index.html") ||
            path == "/favicon.ico")
        {
            await _next(context);
            return;
        }

        // Check for API key in header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            _logger.LogWarning("API Key was not provided. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key was not provided");
            return;
        }

        // Validate API key
        if (!string.Equals(extractedApiKey, _apiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Unauthorized API Key attempted. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized client");
            return;
        }

        _logger.LogDebug("API Key validated successfully for path: {Path}", context.Request.Path);
        await _next(context);
    }
}