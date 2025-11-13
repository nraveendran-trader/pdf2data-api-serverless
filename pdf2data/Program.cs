using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Http.Features;
using pdf2data.Extensions;
using pdf2data.Middleware;
using pdf2data.Models.Common;
using pdf2data.Providers;
using pdf2data.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if(builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.TraversePath().Load(); // Load environment variables from .env file in development
}


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

    // ✅ Add API Key security definition
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "API Key needed to access the endpoints. Enter your API key in the field below.",
        Scheme = "ApiKeyScheme"
    });

    // ✅ Require API Key globally
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

//add aws options
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

// Configure form options for file uploads in Lambda containers
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 100_000_000; // 100MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.BufferBody = true; // Important for Lambda containers
    options.MemoryBufferThreshold = int.MaxValue;
    options.MultipartBoundaryLengthLimit = int.MaxValue;
});

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true; // Enforce lowercase URLs
});

// Configure Kestrel for file uploads (if not in Lambda)
if (!Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")?.Any() == true)
{
    builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = 100_000_000; // 100MB
    });
}

if (ConfigProvider.IsLocalEnvironment)
{
    //use singleton because the client internally uses HttpClient which is intended to be reused
    builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
    {
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = ConfigProvider.LocalDynamoDbEndpoint, // Local DynamoDB endpoint
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
    var departmentName = ConfigProvider.DepartmentName;
    var envName = ConfigProvider.EnvironmentName;
    var stageName = ConfigProvider.StageName;
    var projectName = ConfigProvider.ProjectName;

    //format: tbl-dept-env-stage-project-<table>
    string tableNamePrefix = $"tbl-{departmentName}-{envName}-{stageName}-{projectName}-";
    var dynamoDbBuilder = new DynamoDBContextBuilder()
                            .WithDynamoDBClient(() => client)
                            .ConfigureContext(c => c.TableNamePrefix = tableNamePrefix);

    return dynamoDbBuilder.Build();
});

builder.Services.AddAWSService<IAmazonBedrockRuntime>(builder.Configuration.GetAWSOptions());
//add other services and repositories
builder.Services.AddScoped<IPdfParsingService, PdfPigParsingService>();

var app = builder.Build();


app.UseMiddleware<ApiKeyMiddleware>(await ConfigProvider.GetApiKeyAsync());

// Configure the HTTP request pipeline.
if (ConfigProvider.ExposeApiExplorer)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

await app.DoStartupTasks(); //defined in LifeTimeExtensions.cs
app.Run();

