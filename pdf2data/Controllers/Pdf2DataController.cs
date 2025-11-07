using Microsoft.AspNetCore.Mvc;
using pdf2data.Services;

namespace pdf2data.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfDataController : ControllerBase
{
    private const string _apiVersion = "api/pdf2data/v1";
    private readonly ILogger<PdfDataController> _logger;
    private readonly IPdfParsingService _pdfParsingService;

    public PdfDataController(ILogger<PdfDataController> logger, IPdfParsingService pdfParsingService)
    {
        _logger = logger;
        _pdfParsingService = pdfParsingService;
    }

    [HttpPost($"{_apiVersion}/pdf2xml")]
    public async Task<IActionResult> Pdf2Xml([FromBody] Stream requestStream)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await requestStream.CopyToAsync(memoryStream);
            byte[] pdfBytes = memoryStream.ToArray();
            // Use the _pdfParsingService to convert PDF to XML
            string xmlContent = await _pdfParsingService.ConvertPdfToXmlAsync(pdfBytes);

            return Ok(xmlContent);            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while converting PDF to XML");
            return StatusCode(500, "Internal server error");
        }
    }


}