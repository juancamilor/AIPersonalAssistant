using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface IWishesService
{
    Task<List<WishesDocument>> GetByUserIdAsync(string userId);
    Task<WishesDocument?> GetByIdAsync(string userId, string id);
    Task<WishesDocument?> GetByShareTokenAsync(string shareToken);
    Task<WishesDocument> CreateAsync(string userId, WishesRequest request);
    Task<WishesDocument?> UpdateAsync(string userId, string id, WishesRequest request);
    Task<bool> DeleteAsync(string userId, string id);
}
