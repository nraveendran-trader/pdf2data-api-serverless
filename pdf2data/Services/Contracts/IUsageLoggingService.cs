using pdf2data.Models.Common;
using pdf2data.Models.DynamoDb;

namespace pdf2data.Services;

public interface IUsageLoggingService
{

    Task LogUsageAsync(UsageLog log);
    Task<List<UsageLog>> GetAllUsageLogsAsync();
}