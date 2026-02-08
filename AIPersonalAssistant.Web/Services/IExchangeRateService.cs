using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface IExchangeRateService
{
    Task<List<AggregatedRate>> GetExchangeRatesAsync(string fromCurrency, List<string> toCurrencies, DateTime date);
    Task<Dictionary<string, List<TimeSeriesPoint>>> GetTimeSeriesAsync(string fromCurrency, List<string> toCurrencies, DateTime startDate, DateTime endDate);
}

public class TimeSeriesPoint
{
    public string Date { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}
