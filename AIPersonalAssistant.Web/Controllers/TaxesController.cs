using AIPersonalAssistant.Web.Models;
using AIPersonalAssistant.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaxesController : ControllerBase
{
    private readonly ITaxesService _taxesService;
    private readonly ILogger<TaxesController> _logger;

    public TaxesController(ITaxesService taxesService, ILogger<TaxesController> logger)
    {
        _taxesService = taxesService;
        _logger = logger;
    }

    [HttpPost("analyze-w2")]
    public async Task<IActionResult> AnalyzeW2(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { error = "Unsupported file type. Please upload JPEG, PNG, or PDF." });

        try
        {
            using var stream = file.OpenReadStream();
            var w2Data = await _taxesService.ExtractW2Async(stream, file.ContentType);
            return Ok(w2Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze W2");
            return StatusCode(500, new { error = "Failed to analyze W2 document. Please try again." });
        }
    }

    [HttpPost("parse-stock-sales")]
    public IActionResult ParseStockSales(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        try
        {
            using var stream = file.OpenReadStream();
            var sales = _taxesService.ParseStockSalesExcel(stream);
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse stock sales Excel");
            return StatusCode(500, new { error = "Failed to parse Excel file. Please check the format." });
        }
    }

    [HttpPost("estimate")]
    public IActionResult Estimate([FromBody] TaxEstimateRequest request)
    {
        if (request.W2Data == null)
            return BadRequest(new { error = "W2 data is required" });

        try
        {
            var result = _taxesService.CalculateTaxEstimate(request.W2Data, request.StockSales ?? new());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate tax estimate");
            return StatusCode(500, new { error = "Failed to calculate tax estimate." });
        }
    }
}
