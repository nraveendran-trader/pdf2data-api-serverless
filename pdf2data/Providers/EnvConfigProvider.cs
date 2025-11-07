namespace pdf2data.Providers;

public static class EnvConfigProvider
{
    public static string DepartmentName => GetConfigurationValue("DEPARTMENT_NAME") ?? throw new Exception("DEPARTMENT_NAME environment variable is not set");
    public static string EnvironmentName => GetConfigurationValue("ENV_NAME") ?? throw new Exception("ENV_NAME environment variable is not set");
    public static string StageName => GetConfigurationValue("STAGE_NAME") ?? throw new Exception("STAGE_NAME environment variable is not set");
    public static string ProjectName => GetConfigurationValue("PROJECT_NAME") ?? throw new Exception("PROJECT_NAME environment variable is not set");
    public static string ComponentName => GetConfigurationValue("COMPONENT_NAME") ?? throw new Exception("COMPONENT_NAME environment variable is not set");
    public static string LocalDynamoDbEndpoint => Environment.GetEnvironmentVariable("LOCAL_DYNAMODB_ENDPOINT") ?? throw new Exception("LOCAL_DYNAMODB_ENDPOINT environment variable is not set");
    public static bool ExposeApiExplorer => bool.TryParse(GetConfigurationValue("EXPOSE_API_EXPLORER"), out var result) && result;
    public static bool IsLocalEnvironment => EnvironmentName.Equals("loc", StringComparison.OrdinalIgnoreCase);
    public static bool IsProductionEnvironment => EnvironmentName.Equals("prod", StringComparison.OrdinalIgnoreCase);

    private static string? GetConfigurationValue(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }
}
