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

            EnsureEnvironmentVariablesExist(logger);
        });
    }


    private static void EnsureEnvironmentVariablesExist(ILogger logger)
    {
        try
        {
            var departmentName = EnvConfigProvider.DepartmentName;
            var envName = EnvConfigProvider.EnvironmentName;
            var stageName = EnvConfigProvider.StageName;
            var projectName = EnvConfigProvider.ProjectName;
            var componentName = EnvConfigProvider.ComponentName;
            var exposeApiExplorer = EnvConfigProvider.ExposeApiExplorer ? "true" : "false";

            logger.LogInformation($"Environment Configuration - Department: {departmentName}, Environment: {envName}, Stage: {stageName}, Project: {projectName}, Component: {componentName}, Expose API Explorer: {exposeApiExplorer}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error during environment variable check. Exiting...");
            Environment.Exit(-1);
        }
    }
}