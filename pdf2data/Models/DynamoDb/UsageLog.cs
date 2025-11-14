using Amazon.DynamoDBv2.DataModel;
using pdf2data.Models.Common;

namespace pdf2data.Models.DynamoDb;

[DynamoDBTable("logs")]
public class UsageLog : ItemBase
{
    [DynamoDBProperty("prompt")]
    public required string Prompt { get; set; }

    [DynamoDBProperty("response")]
    public string? Response { get; set; }

    [DynamoDBProperty("input_tokens")]
    public int InputTokens { get; set; }

    [DynamoDBProperty("output_tokens")]
    public int OutputTokens { get; set; }

    [DynamoDBProperty("date_utc")]
    public required DateTime DateUtc { get; set; }
}


/* 
docker run --rm -it -p 8000:8000 amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb
aws dynamodb create-table --table-name tbl-reg-loc-loc-cg-logs --attribute-definitions AttributeName=id,AttributeType=S --key-schema AttributeName=id,KeyType=HASH --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5 --endpoint-url http://localhost:8000
aws dynamodb list-tables --endpoint-url http://localhost:8000
aws dynamodb describe-table --table-name tbl-reg-loc-loc-cg-logs --endpoint-url http://localhost:8000
aws dynamodb put-item --table-name tbl-reg-loc-loc-cg-logs --item '{"id": {"S": "1"}, "message": {"S": "Test log message"}, "timestamp": {"S": "2024-06-10T12:00:00Z"}}' --endpoint-url http://localhost:8000
aws dynamodb scan --table-name tbl-reg-loc-loc-cg-logs --endpoint-url http://localhost:8000
*/