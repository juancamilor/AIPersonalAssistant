namespace AIPersonalAssistant.Web.Models;

public class ExchangeRateHistoryEntry
{
    public DateTime Date { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}
