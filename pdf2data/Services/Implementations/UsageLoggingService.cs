using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.DynamoDBv2.DataModel;
using pdf2data.Models.Common;
using pdf2data.Models.DynamoDb;
using pdf2data.Providers;

namespace pdf2data.Services;

public class UsageLoggingService : IUsageLoggingService
{
    private readonly ILogger<UsageLoggingService> _logger;

    private readonly IDynamoDBContext _dynamoDbContext;
    
    public UsageLoggingService(ILogger<UsageLoggingService> logger, IDynamoDBContext dynamoDbContext)
    {
        _logger = logger;
        _dynamoDbContext = dynamoDbContext;
    }

    public async Task<List<UsageLog>> GetAllUsageLogsAsync()
    {
        return await _dynamoDbContext.ScanAsync<UsageLog>(new List<ScanCondition>()).GetRemainingAsync();
    }

    public async Task LogUsageAsync(UsageLog log)
    {
        await _dynamoDbContext.SaveAsync(log);
    }
}