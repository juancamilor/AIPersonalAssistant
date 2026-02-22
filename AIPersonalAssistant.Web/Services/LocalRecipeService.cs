using AIPersonalAssistant.Web.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class LocalRecipeService : IRecipeService
{
    private readonly string _dataDirectory;
    private readonly ILogger<LocalRecipeService> _logger;
    private static readonly object _fileLock = new();

    public LocalRecipeService(IWebHostEnvironment environment, ILogger<LocalRecipeService> logger)
    {
        _dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "recipes");
        _logger = logger;
        
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    private string GetUserFilePath(string userId)
    {
        var safeUserId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userId))
            .Replace("/", "_").Replace("+", "-");
        return Path.Combine(_dataDirectory, $"{safeUserId}.json");
    }

    private async Task<List<Recipe>> LoadRecipesAsync(string userId)
    {
        var filePath = GetUserFilePath(userId);
        
        if (!File.Exists(filePath))
        {
            return new List<Recipe>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Recipe>>(json) ?? new List<Recipe>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recipes for user {UserId}", userId);
            return new List<Recipe>();
        }
    }

    private async Task SaveRecipesAsync(string userId, List<Recipe> recipes)
    {
        var filePath = GetUserFilePath(userId);
        var json = JsonSerializer.Serialize(recipes, new JsonSerializerOptions { WriteIndented = true });
        
        lock (_fileLock)
        {
            File.WriteAllText(filePath, json);
        }
        
        await Task.CompletedTask;
    }

    public async Task<List<Recipe>> GetAllByUserIdAsync(string userId)
    {
        return await LoadRecipesAsync(userId);
    }

    public async Task<Recipe?> GetByIdAsync(string userId, string id)
    {
        var recipes = await LoadRecipesAsync(userId);
        return recipes.FirstOrDefault(r => r.Id == id);
    }

    public async Task<Recipe?> GetByShareTokenAsync(string shareToken)
    {
        try
        {
            var files = Directory.GetFiles(_dataDirectory, "*.json");
            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var recipes = JsonSerializer.Deserialize<List<Recipe>>(json);
                var recipe = recipes?.FirstOrDefault(r => r.ShareToken == shareToken);
                if (recipe != null)
                {
                    return recipe;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for shared recipe with token {ShareToken}", shareToken);
        }

        return null;
    }

    public async Task<Recipe> CreateAsync(string userId, RecipeRequest request)
    {
        var recipes = await LoadRecipesAsync(userId);
        
        var recipe = new Recipe
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Ingredients = request.Ingredients,
            Instructions = request.Instructions,
            Category = request.Category,
            PrepTime = request.PrepTime,
            CookTime = request.CookTime,
            Servings = request.Servings,
            ShareToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        recipes.Add(recipe);
        await SaveRecipesAsync(userId, recipes);
        
        return recipe;
    }

    public async Task<Recipe?> UpdateAsync(string userId, string id, RecipeRequest request)
    {
        var recipes = await LoadRecipesAsync(userId);
        var recipe = recipes.FirstOrDefault(r => r.Id == id);
        
        if (recipe == null)
        {
            return null;
        }
        
        recipe.Title = request.Title;
        recipe.Description = request.Description;
        recipe.Ingredients = request.Ingredients;
        recipe.Instructions = request.Instructions;
        recipe.Category = request.Category;
        recipe.PrepTime = request.PrepTime;
        recipe.CookTime = request.CookTime;
        recipe.Servings = request.Servings;
        if (request.ImageUrls != null)
            recipe.ImageUrls = request.ImageUrls;
        recipe.UpdatedAt = DateTime.UtcNow;
        
        await SaveRecipesAsync(userId, recipes);
        
        return recipe;
    }

    public async Task<bool> DeleteAsync(string userId, string id)
    {
        var recipes = await LoadRecipesAsync(userId);
        var recipe = recipes.FirstOrDefault(r => r.Id == id);
        
        if (recipe == null)
        {
            return false;
        }
        
        recipes.Remove(recipe);
        await SaveRecipesAsync(userId, recipes);
        
        return true;
    }

    public async Task<List<string>> GetCategoriesAsync(string userId)
    {
        var recipes = await LoadRecipesAsync(userId);
        return recipes
            .Select(r => r.Category)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }
}
