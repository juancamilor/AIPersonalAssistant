namespace AIPersonalAssistant.Web.Services;

public interface IExchangeRateHistoryService
{
    Task SaveRateAsync(DateTime date, string fromCurrency, string toCurrency, decimal rate);
    Task<List<Models.ExchangeRateHistoryEntry>> GetHistoryAsync(string fromCurrency, string toCurrency);
}
