using Microsoft.AspNetCore.Mvc;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Text.Json;
using System.Text;
using pdf2data.Models.Common;
using pdf2data.Services;
using pdf2data.Providers;
using pdf2data.Models.DynamoDb;

namespace pdf2data.Controllers;

[ApiController]
[Route($"api/{ConfigProvider.API_VERSION}/[controller]")]
public class BedrockController : ControllerBase
{
    private readonly ILogger<BedrockController> _logger;
    private readonly IAnalysisService _analysisService;
    private readonly IUsageLoggingService _usageLoggingService;

    public BedrockController(ILogger<BedrockController> logger, IAnalysisService analysisService, IUsageLoggingService usageLoggingService)
    {
        _logger = logger;
        _analysisService = analysisService;
        _usageLoggingService = usageLoggingService;
    }

    [HttpGet("usage-logs")]
    public async Task<List<UsageLog>> GetAllUsageLogsAsync()
    {
        return await _usageLoggingService.GetAllUsageLogsAsync();
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
                return BadRequest("Analysis prompt is required");
            
            var response = await _analysisService.AnalyzeTextAsync(request.Prompt);


            await LogUsageAsync(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text Analysis service error");

           var errorMessage = ex.InnerException != null ? $"{ex.Message}, Inner Exception: {ex.InnerException.Message}" : ex.Message; 
            return StatusCode(502, $"Text Analysis service error: {errorMessage}");
        }
    }

    [HttpPost("pdf-analysis")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzePdf([FromForm] PdfAnalysisRequest request)
    {
        try
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No PDF file uploaded");
            
            if (!IsValidPdfFile(request.File))
                return BadRequest("Invalid PDF file");
            
            _logger.LogInformation($"Processing PDF analysis. File: {request.File.FileName}, Prompt: {request.Prompt}");

            using (var memoryStream = new MemoryStream()){
                try 
                {
                    using var fileStream = request.File.OpenReadStream();
                    if (fileStream.CanSeek) // Ensure stream is at the beginning
                        fileStream.Position = 0;
                    
                    // Copy to memory stream with explicit buffer size for Lambda
                    await fileStream.CopyToAsync(memoryStream, bufferSize: 81920); // 80KB buffer
                    
                    _logger.LogInformation("File copied to memory stream. Original size: {OriginalSize}, Memory stream size: {MemorySize}", 
                    request.File.Length, memoryStream.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read uploaded file into memory stream");
                    return BadRequest($"Failed to process uploaded file: {ex.Message}");
                }

                // Validate that we actually got data
                if (memoryStream.Length == 0)
                {
                    return BadRequest("PDF file appears to be empty");
                }

                _logger.LogInformation("PDF file copied to memory stream. Size: {StreamSize}", memoryStream.Length);

                var response = await _analysisService.AnalyzePdfAsync(memoryStream, request.Prompt, request.File.FileName);

                
                _logger.LogInformation("Successfully analyzed PDF with Bedrock");


                await LogUsageAsync(response);

                return Ok(response);
        
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF Analysis service error");

           var errorMessage = ex.InnerException != null ? $"{ex.Message}, Inner Exception: {ex.InnerException.Message}" : ex.Message; 
            return StatusCode(502, $"PDF Analysis service error: {errorMessage}");
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

    private async Task LogUsageAsync(AnalysisResponse log)
    {
        try
        {
            _logger.LogInformation($"Logging usage data for RequestId: {log.RequestId}");

            await _usageLoggingService.LogUsageAsync(new UsageLog
            {
                Id = log.RequestId,
                DateUtc = log.ProcessedAtUtc,
                Prompt = log.Prompt ?? string.Empty,
                Response = log.Response,
                InputTokens = log.InputTokens,
                OutputTokens = log.OutputTokens
            });

            _logger.LogInformation($"Usage data logged successfully for RequestId: {log.RequestId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while logging usage data for RequestId: {RequestId}", log.RequestId);
            throw;
        }
    }
}

