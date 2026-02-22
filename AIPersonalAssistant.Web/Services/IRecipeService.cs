using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface IRecipeService
{
    Task<List<Recipe>> GetAllByUserIdAsync(string userId);
    Task<Recipe?> GetByIdAsync(string userId, string id);
    Task<Recipe?> GetByShareTokenAsync(string shareToken);
    Task<Recipe> CreateAsync(string userId, RecipeRequest request);
    Task<Recipe?> UpdateAsync(string userId, string id, RecipeRequest request);
    Task<bool> DeleteAsync(string userId, string id);
    Task<List<string>> GetCategoriesAsync(string userId);
}
