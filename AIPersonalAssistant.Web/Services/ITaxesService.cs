using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface ITaxesService
{
    Task<W2Data> ExtractW2Async(Stream imageStream, string contentType);
    List<StockSale> ParseStockSalesExcel(Stream excelStream);
    TaxEstimateResult CalculateTaxEstimate(W2Data w2, List<StockSale> stockSales);
}
