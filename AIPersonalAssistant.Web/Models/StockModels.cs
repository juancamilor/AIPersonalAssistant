namespace AIPersonalAssistant.Web.Models;

public class StockDataRequest
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class StockDataPoint
{
    public DateTime Date { get; set; }
    public decimal Close { get; set; }
}

public class StockDataResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public List<StockDataPoint> DataPoints { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
