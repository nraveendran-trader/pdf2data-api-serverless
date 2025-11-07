using Amazon.DynamoDBv2.DataModel;

namespace pdf2data.Models.DynamoDb;

public class ItemBase
{
    [DynamoDBHashKey("id")]
    public required string Id { get; set; }
}
