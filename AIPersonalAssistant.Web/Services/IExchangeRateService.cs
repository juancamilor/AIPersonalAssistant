using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface IExchangeRateService
{
    Task<List<AggregatedRate>> GetExchangeRatesAsync(string fromCurrency, List<string> toCurrencies, DateTime date);
}
