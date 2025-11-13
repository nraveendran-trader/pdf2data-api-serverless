using pdf2data.Providers;

namespace pdf2data.Extensions;

public static class LifeTimeExtensions
{
    // Thread-safe, lazy singleton instance
    private static readonly Lazy<Guid> _instanceId = new(() => Guid.NewGuid());

    public static Guid ApplicationInstanceId => _instanceId.Value;

    public static async Task DoStartupTasks(this WebApplication app)
    {
         var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"COLD Starting - Insance Id: {ApplicationInstanceId}");

        if(ConfigProvider.IsLocalEnvironment)
        {
            logger.LogWarning("Application is running in LOCAL environment.");
        }else{
            logger.LogInformation("Application is running in NON-LOCAL environment.");
            await PreloadSsmParametersAsync(logger);
        }
        
        await EnsureRequiredConfigurationExists(logger);
    }

    private static async Task PreloadSsmParametersAsync(ILogger logger)
    {
        try
        {
            logger.LogInformation($"Start to preload SSM parameters - Insance Id: {ApplicationInstanceId}");
            await ConfigProvider.PreloadSsmParametersAsync();
            logger.LogInformation($"Finished preloading SSM parameters - Insance Id: {ApplicationInstanceId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while preloading SSM parameters. Exiting...");
            Environment.Exit(-1);   
        }
    }

    private static async Task EnsureRequiredConfigurationExists(ILogger logger)
    {
        try
        {
            logger.LogInformation($"Ensuring required configuration exists - Insance Id: {ApplicationInstanceId}");
            await ConfigProvider.EnsureRequiredConfigurationExistsAsync();
            logger.LogInformation($"Finished ensuring required configuration exists - Insance Id: {ApplicationInstanceId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Some environment variables are missing or invalid. Exiting...");
            Environment.Exit(-1);
        }
    }
}