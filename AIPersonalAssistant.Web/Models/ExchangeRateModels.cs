namespace AIPersonalAssistant.Web.Models;

public class SourceRate
{
    public string Source { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AggregatedRate
{
    public string ToCurrency { get; set; } = string.Empty;
    public List<SourceRate> Sources { get; set; } = new();
    public decimal AverageRate { get; set; }
    public int SuccessfulSourcesCount { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
}

public class ExchangeRateResponse
{
    public string Date { get; set; } = string.Empty;
    public string FromCurrency { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 1;
    public List<AggregatedRate> Conversions { get; set; } = new();
}
