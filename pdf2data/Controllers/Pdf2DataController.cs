using System.Text;
using Microsoft.AspNetCore.Mvc;
using pdf2data.Providers;
using pdf2data.Services;
using UglyToad.PdfPig;

namespace pdf2data.Controllers;

[ApiController]
[Route("api/[controller]/v1")]
public class Pdf2DataController : ControllerBase
{
    private readonly ILogger<Pdf2DataController> _logger;
    private readonly IPdfParsingService _pdfParsingService;

    public Pdf2DataController(ILogger<Pdf2DataController> logger, IPdfParsingService pdfParsingService)
    {
        _logger = logger;
        _pdfParsingService = pdfParsingService;
    }

    [HttpGet("key")]
    public async Task<IActionResult> GetKey()
    {
        try
        {
            return Ok(new { Key = await ConfigProvider.GetPdfFocusKeyAsync(), Timestamp = DateTime.UtcNow });        
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching PDF Focus key");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("text")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Pdf2Text(IFormFile file)
    {
        try
        {

            _logger.LogInformation("Received PDF file: {FileName}, Size: {FileSize} bytes", file.FileName, file.Length);

            using (var memoryStream = new MemoryStream())
            {
                await file.OpenReadStream().CopyToAsync(memoryStream);
                byte[] pdfBytes = memoryStream.ToArray();
                string text = await _pdfParsingService.ConvertPdfToText(pdfBytes);

                _logger.LogInformation("Extracted text length: {TextLength}", text.Length);
                _logger.LogInformation("Extracted text content: {TextContent}", text);
                return Ok(text);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while converting PDF to XML");
            return StatusCode(500, "Internal server error");
        }
    }

}