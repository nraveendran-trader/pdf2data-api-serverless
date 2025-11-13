using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using pdf2data.Models.Common;
using pdf2data.Providers;

namespace pdf2data.Services;

public class BedrockAnalysisService : IAnalysisService
{
    private readonly IAmazonBedrockRuntime _bedrockClient;
    private readonly ILogger<BedrockAnalysisService> _logger;
    
    public BedrockAnalysisService(IAmazonBedrockRuntime bedrockClient, ILogger<BedrockAnalysisService> logger)
    {
        _bedrockClient = bedrockClient;
        _logger = logger;
    }

    public async Task<string> AnalyzePdfAsync(MemoryStream stream, string prompt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new InvalidOperationException("Analysis prompt is required");

            _logger.LogInformation($"Processing PDF analysis with Bedrock. Prompt: {prompt}");

            var bedrockResponse = await InvokeBedrockModelWithPdf(stream, prompt);

            _logger.LogInformation("Successfully analyzed PDF with Bedrock");

            return bedrockResponse;

        }
        catch (AmazonBedrockRuntimeException bedrockEx)
        {
            _logger.LogError(bedrockEx, "Bedrock service error");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while analyzing text with Bedrock");
            throw;
        }
    }

    public async Task<string> AnalyzeTextAsync(string prompt)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(prompt))
                throw new InvalidOperationException("Analysis prompt is required");

            _logger.LogInformation($"Processing text analysis with Bedrock. Prompt: {prompt}");

            var bedrockResponse = await InvokeBedrockModelForText(prompt);

            _logger.LogInformation("Successfully analyzed text with Bedrock");

            return bedrockResponse;

        }
        catch (AmazonBedrockRuntimeException bedrockEx)
        {
            _logger.LogError(bedrockEx, "Bedrock service error");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while analyzing text with Bedrock");
            throw;
        }
    }

    private async Task<string> InvokeBedrockModelForText(string prompt)
    {
        // Using Claude 3 Sonnet model for text analysis
        const string modelId = "anthropic.claude-3-sonnet-20240229-v1:0";

        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 4000,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var requestBodyJson = JsonSerializer.Serialize(requestBody);
        var requestBodyBytes = Encoding.UTF8.GetBytes(requestBodyJson);

        var invokeRequest = new InvokeModelRequest
        {
            ModelId = modelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(requestBodyBytes)
        };

        _logger.LogInformation("Invoking Bedrock model for text analysis: {ModelId}", modelId);

        var response = await _bedrockClient.InvokeModelAsync(invokeRequest);

        using var reader = new StreamReader(response.Body);
        var responseBody = await reader.ReadToEndAsync();

        _logger.LogDebug("Bedrock response: {Response}", responseBody);

        // Parse the response
        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        if (responseJson.TryGetProperty("content", out var contentArray) && 
            contentArray.ValueKind == JsonValueKind.Array &&
            contentArray.GetArrayLength() > 0)
        {
            var firstContent = contentArray[0];
            if (firstContent.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? "No analysis text received";
            }
        }

        return responseBody; // Return raw response if parsing fails
    }


    private async Task<string> InvokeBedrockModelWithPdf(MemoryStream pdfStream, string prompt)
    {
        const string modelId = "anthropic.claude-3-haiku-20240307-v1:0";
        pdfStream.Position = 0;

        try 
        {
            var request = new ConverseRequest
            {
                ModelId = modelId,
                Messages = new List<Message>
                {
                    new Message
                    {
                        Role = ConversationRole.User, // Use proper enum
                        Content = new List<ContentBlock>
                        {
                            new ContentBlock { Text = prompt },
                            new ContentBlock
                            {
                                Document = new DocumentBlock
                                {
                                    Name = "document",
                                    Format = DocumentFormat.Pdf, // Use proper enum
                                    Source = new DocumentSource 
                                    { 
                                        Bytes = pdfStream
                                    }
                                }
                            }
                        }
                    }
                },
                InferenceConfig = new InferenceConfiguration
                {
                    MaxTokens = 1000,
                    Temperature = 0.2f,
                    TopP = 0.95f
                }
            };

            _logger.LogInformation("Invoking Bedrock ConverseAsync with PDF: {ModelId}", modelId);

            var response = await _bedrockClient.ConverseAsync(request);

            // Extract text from response
            var result = new StringBuilder();
            if (response.Output?.Message?.Content != null)
            {
                foreach (var block in response.Output.Message.Content)
                {
                    if (!string.IsNullOrEmpty(block.Text))
                    {
                        result.AppendLine(block.Text);
                    }
                }
            }

            var finalResult = result.ToString();
            if (string.IsNullOrWhiteSpace(finalResult))
            {
                return "No analysis text received from Bedrock";
            }

            return finalResult.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PDF with Bedrock");
            throw;
        }
    }


}