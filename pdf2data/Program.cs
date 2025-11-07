using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using pdf2data.Extensions;
using pdf2data.Models.Common;
using pdf2data.Providers;
using pdf2data.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
// builder.Logging.AddConsole();
builder.Logging.AddSerilog(new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    // .WriteTo.File("logs/pdf2data_log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger());
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add controllers.
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

//add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PDF2Data API",
        Version = "v1",
        Description = "API for extracting data from PDF files"
    });
});

//add aws options
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

if (EnvConfigProvider.IsLocalEnvironment)
{
    //use singleton because the client internally uses HttpClient which is intended to be reused
    builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
    {
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = EnvConfigProvider.LocalDynamoDbEndpoint, // Local DynamoDB endpoint
            UseHttp = true
        };
        return new AmazonDynamoDBClient(config);
    });
}
else
{
    builder.Services.AddAWSService<IAmazonDynamoDB>();
}

builder.Services.AddScoped<IDynamoDBContext>(sp =>
{
    var client = sp.GetRequiredService<IAmazonDynamoDB>();

    // Ensure no null values in table name prefix
    var departmentName = EnvConfigProvider.DepartmentName;
    var envName = EnvConfigProvider.EnvironmentName;
    var stageName = EnvConfigProvider.StageName;
    var projectName = EnvConfigProvider.ProjectName;

    //format: tbl-dept-env-stage-project-<table>
    string tableNamePrefix = $"tbl-{departmentName}-{envName}-{stageName}-{projectName}-";
    var dynamoDbBuilder = new DynamoDBContextBuilder()
                            .WithDynamoDBClient(() => client)
                            .ConfigureContext(c => c.TableNamePrefix = tableNamePrefix);

    return dynamoDbBuilder.Build();
});

//add other services and repositories
builder.Services.AddScoped<IPdfParsingService, SautinSoftPdfParsingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!EnvConfigProvider.ExposeApiExplorer)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PDF2Data API v1");
        c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    });
}

// app.UseHttpsRedirection();
app.MapLifeTimeEvents(); //defined in LifeTimeEvents.cs
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

