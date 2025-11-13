using Microsoft.AspNetCore.Mvc;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Text.Json;
using System.Text;

namespace pdf2data.Controllers;

[ApiController]
[Route("api/v1/bedrock")]
public class BedrockController : ControllerBase
{
    private readonly ILogger<BedrockController> _logger;
    private readonly IAmazonBedrockRuntime _bedrockClient;

    public BedrockController(ILogger<BedrockController> logger, IAmazonBedrockRuntime bedrockClient)
    {
        _logger = logger;
        _bedrockClient = bedrockClient;
    }

    [HttpPost("text-analysis")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzeText([FromBody] TextAnalysisRequest request)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest("Analysis prompt is required");
            }

            _logger.LogInformation($"Processing text analysis with Bedrock. Prompt: {request.Prompt}");

            var bedrockResponse = await InvokeBedrockModelForText(request.Prompt);

            _logger.LogInformation("Successfully analyzed text with Bedrock");

            return Ok(new
            {
                ProcessedAt = DateTime.UtcNow,
                Prompt = request.Prompt,
                Analysis = bedrockResponse,
                Status = "Success"
            });
        }
        catch (AmazonBedrockRuntimeException bedrockEx)
        {
            _logger.LogError(bedrockEx, "Bedrock service error");
            return StatusCode(502, $"Bedrock service error: {bedrockEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while analyzing text with Bedrock");
            return StatusCode(500, "Internal server error occurred while processing the text");
        }
    }

    [HttpPost("pdf-analysis")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzePdfWithBedrock(IFormFile file, string prompt)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No PDF file uploaded");
            }

            if (!IsValidPdfFile(file))
            {
                return BadRequest("Invalid PDF file");
            }

            using (var memoryStream = new MemoryStream()){
                _logger.LogInformation($"Processing PDF analysis with Bedrock. File: {file.FileName}, Prompt: {prompt}");
                
                // Fix for Lambda container: Ensure proper file stream handling
                try 
                {
                    // Use OpenReadStream() for better compatibility in Lambda containers
                    using var fileStream = file.OpenReadStream();
                    
                    // Ensure stream is at the beginning
                    if (fileStream.CanSeek)
                    {
                        fileStream.Position = 0;
                    }
                    
                    // Copy to memory stream with explicit buffer size for Lambda
                    await fileStream.CopyToAsync(memoryStream, bufferSize: 81920); // 80KB buffer
                    
                    _logger.LogInformation("File copied to memory stream. Original size: {OriginalSize}, Memory stream size: {MemorySize}", 
                    file.Length, memoryStream.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read uploaded file in Lambda environment");
                    return BadRequest($"Failed to process uploaded file: {ex.Message}");
                }


                // Validate that we actually got data
                if (memoryStream.Length == 0)
                {
                    return BadRequest("PDF file appears to be empty");
                }

                _logger.LogInformation("PDF file copied to memory stream. Size: {StreamSize}", memoryStream.Length);


                var bedrockResponse = await InvokeBedrockModelWithPdf(memoryStream, prompt, file.FileName);

                _logger.LogInformation("Successfully analyzed PDF with Bedrock");
                // var pdfBytes = memoryStream.ToArray();
                // var base64Pdf = Convert.ToBase64String(pdfBytes);

                return Ok(new
                {
                    ProcessedAt = DateTime.UtcNow,
                    FileName = file.FileName,
                    Prompt = prompt,
                    Analysis = bedrockResponse,
                    Status = "Success"
                });
            
            }
            

            // _logger.LogInformation($"Processing PDF analysis with Bedrock. File: {file.FileName}, Prompt: {prompt}");

            // var bedrockResponse = await InvokeBedrockModelWithPdf(base64Pdf, prompt, file.FileName);

            // _logger.LogInformation("Successfully analyzed PDF with Bedrock");

            
        }
        catch (AmazonBedrockRuntimeException bedrockEx)
        {
            _logger.LogError(bedrockEx, "Bedrock service error");
            return StatusCode(502, $"Bedrock service error: {bedrockEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while analyzing PDF with Bedrock");
            return StatusCode(500, $"Internal server error occurred while processing the PDF: {ex.Message}");
        }
    }



    
    private async Task<string> InvokeBedrockModel(string base64Pdf, string prompt, string fileName)
    {
        // Using Claude 3 Sonnet model for text-based analysis
        const string modelId = "anthropic.claude-3-sonnet-20240229-v1:0";

        // Note: Claude models don't directly support PDF documents
        // For PDF analysis, you would typically need to:
        // 1. Convert PDF to text first, or
        // 2. Convert PDF pages to images
        // For now, we'll provide a text-based analysis instruction

        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 4000,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = $@"I have a PDF document named '{fileName}' that I need analyzed. 
                    
                    {prompt}
                    
                    Please note: I'm unable to directly process the PDF content through this interface. 
                    To properly analyze the PDF, you would need to:
                    1. Extract the text content from the PDF first using a PDF processing library
                    2. Then send the extracted text to this model for analysis
                    
                    Alternatively, if the PDF contains primarily visual content, convert it to images first.
                    
                    Please provide guidance on how to extract and analyze the key information from a PDF document given the prompt: {prompt}"
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

        _logger.LogInformation("Invoking Bedrock model: {ModelId}", modelId);

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

    private async Task<string> InvokeBedrockModelWithPdf(MemoryStream pdfStream, string prompt, string fileName)
    {
        const string modelId = "anthropic.claude-3-haiku-20240307-v1:0";

        // Ensure clean byte array conversion
        pdfStream.Position = 0;
        // var pdfBytes = pdfStream.ToArray();
        
        // // Validate PDF header to ensure it's a valid PDF
        // if (pdfBytes.Length < 4 || 
        //     pdfBytes[0] != 0x25 || pdfBytes[1] != 0x50 || pdfBytes[2] != 0x44 || pdfBytes[3] != 0x46)
        // {
        //     throw new ArgumentException("Invalid PDF file format");
        // }

        // _logger.LogInformation("Processing PDF with size: {Size} bytes", pdfBytes.Length);

        try 
        {
            // Use raw PDF bytes directly for DocumentSource.Bytes (not base64)
            // Bedrock expects the actual binary PDF data, not base64-encoded text
            // using var documentStream = new MemoryStream(pdfBytes);
            
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
            throw new InvalidOperationException($"Bedrock PDF processing failed: {ex.Message}", ex);
        }
    }

    private static bool IsValidPdfFile(IFormFile file)
    {
        // Check content type
        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}

public class TextAnalysisRequest
{
    // public string Text { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
}