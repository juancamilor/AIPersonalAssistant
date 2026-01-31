using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface ITravelService
{
    Task<List<TravelPin>> GetPinsAsync(string userId);
    Task<TravelPin?> GetPinAsync(string userId, string pinId);
    Task<TravelPin> CreatePinAsync(string userId, TravelPinRequest request);
    Task<TravelPin?> UpdatePinAsync(string userId, string pinId, TravelPinRequest request);
    Task<bool> DeletePinAsync(string userId, string pinId);
}
