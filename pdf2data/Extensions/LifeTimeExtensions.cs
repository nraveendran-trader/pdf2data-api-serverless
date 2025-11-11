using pdf2data.Providers;

namespace pdf2data.Extensions;

public static class LifeTimeExtensions
{
    // Thread-safe, lazy singleton instance
    private static readonly Lazy<Guid> _instanceId = new(() => Guid.NewGuid());

    public static Guid ApplicationInstanceId => _instanceId.Value;

    public static void MapLifeTimeEvents(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(async () =>
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"COLD Starting - Insance Id: {ApplicationInstanceId}");

            await PreloadSsmParametersAsync(logger);
            EnsureRequiredConfigurationExists(logger);
        });
    }

    private static async Task PreloadSsmParametersAsync(ILogger logger)
    {
        try
        {
            await ConfigProvider.PreloadSsmParametersAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while preloading SSM parameters. Exiting...");
            Environment.Exit(-1);   
        }
    }

    private static void EnsureRequiredConfigurationExists(ILogger logger)
    {
        try
        {
            ConfigProvider.EnsureRequiredConfigurationExists();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Some environment variables are missing or invalid. Exiting...");
            Environment.Exit(-1);
        }
    }
}