using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon;
using Amazon.DynamoDBv2.Model;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace pdf2data.Providers;

public static class ConfigProvider
{
    private const string PDF_FOCUS_KEY_PARAM_NAME = "pdf_focus_key";

    private static readonly List<string> _ssmParamsToCache = new()
    {
        PDF_FOCUS_KEY_PARAM_NAME
    };

    private static ConcurrentDictionary<string, string> _ssmParamsCache = new();
    public static string AwsRegion => GetConfigurationValue("AWS_REGION") ?? throw new Exception("AWS_REGION environment variable is not set");
    public static string DepartmentName => GetConfigurationValue("DEPARTMENT_NAME") ?? throw new Exception("DEPARTMENT_NAME environment variable is not set");
    public static string EnvironmentName => GetConfigurationValue("ENV_NAME") ?? throw new Exception("ENV_NAME environment variable is not set");
    public static string StageName => GetConfigurationValue("STAGE_NAME") ?? throw new Exception("STAGE_NAME environment variable is not set");
    public static string ProjectName => GetConfigurationValue("PROJECT_NAME") ?? throw new Exception("PROJECT_NAME environment variable is not set");
    public static string ComponentName => GetConfigurationValue("COMPONENT_NAME") ?? throw new Exception("COMPONENT_NAME environment variable is not set");
    public static string LocalDynamoDbEndpoint => Environment.GetEnvironmentVariable("LOCAL_DYNAMODB_ENDPOINT") ?? throw new Exception("LOCAL_DYNAMODB_ENDPOINT environment variable is not set");
    public static bool ExposeApiExplorer => bool.TryParse(GetConfigurationValue("EXPOSE_API_EXPLORER"), out var result) && result;
    public static bool IsLocalEnvironment => EnvironmentName.Equals("loc", StringComparison.OrdinalIgnoreCase);
    public static bool IsProductionEnvironment => EnvironmentName.Equals("prod", StringComparison.OrdinalIgnoreCase);

    public static async Task<string> GetPdfFocusKeyAsync()
    {
        if (IsLocalEnvironment)
        {
            return GetConfigurationValue("PDF_FOCUS_KEY") ?? throw new Exception("PDF_FOCUS_KEY environment variable is not set for local environment");
        }

        _ssmParamsCache.TryGetValue(PDF_FOCUS_KEY_PARAM_NAME, out var pdfFocusKey);
        return pdfFocusKey ?? throw new Exception($"{PDF_FOCUS_KEY_PARAM_NAME} SSM parameter is not cached");
    }
    
    public static void EnsureRequiredConfigurationExists()
    {
        // This method can be expanded to check for all required configurations at startup
        _ = DepartmentName;
        _ = EnvironmentName;
        _ = StageName;
        _ = ProjectName;
        _ = ComponentName;
        _ = LocalDynamoDbEndpoint;
        _ = ExposeApiExplorer;
        _ = GetPdfFocusKeyAsync().GetAwaiter().GetResult();
    }

    public static async Task PreloadSsmParametersAsync()
    {
        foreach (var param in _ssmParamsToCache)
        {
            var value = await GetSsmParameterAsync(param, AwsRegion);
            _ssmParamsCache.AddOrUpdate(param, value, (key, oldValue) => value);
        }
    }

    private static string? GetConfigurationValue(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }

    private static async Task<string> GetSsmParameterAsync(string parameterName, string region)
    {
        string ssmParameterPath = $"/{DepartmentName}/{EnvironmentName}/{StageName}/{ProjectName}/{ComponentName}/{parameterName}";

        var request = new GetParameterRequest
        {
            Name = ssmParameterPath,
            WithDecryption = true
        };

        // Create client with automatic credential refresh
        //requires package: dotnet add package AWSSDK.SimpleSystemsManagement
        var config = new AmazonSimpleSystemsManagementConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region),
            MaxErrorRetry = 3,
            Timeout = TimeSpan.FromSeconds(30)
        };

        using var SsmClient = new AmazonSimpleSystemsManagementClient(config);
        var response = await SsmClient.GetParameterAsync(request);
        return response.Parameter.Value;

    }
    
    /* 
        aws ssm put-parameter --name "/reg/dev/dev1/cg/pdf2data/pdf_focus_key" --value "your-license-key-here" --type "SecureString"        
    */
}
