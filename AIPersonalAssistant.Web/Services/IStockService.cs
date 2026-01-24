using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface IStockService
{
    Task<StockDataResponse> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate);
}
