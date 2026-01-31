namespace AIPersonalAssistant.Web.Models;

public class TravelPin
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string PlaceName { get; set; } = string.Empty;
    public DateTime? DateVisited { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TravelPinRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string PlaceName { get; set; } = string.Empty;
    public DateTime? DateVisited { get; set; }
    public string Notes { get; set; } = string.Empty;
}
