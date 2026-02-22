using AIPersonalAssistant.Web.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class BlobRecipeService : IRecipeService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobRecipeService> _logger;

    public BlobRecipeService(IConfiguration configuration, ILogger<BlobRecipeService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["AzureStorage:ConnectionString"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured");
        }
        
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("recipes");
        _containerClient.CreateIfNotExists();
    }

    private string GetBlobName(string userId)
    {
        var safeUserId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userId))
            .Replace("/", "_").Replace("+", "-");
        return $"{safeUserId}.json";
    }

    private async Task<List<Recipe>> LoadRecipesAsync(string userId)
    {
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);
        
        try
        {
            if (!await blobClient.ExistsAsync())
            {
                return new List<Recipe>();
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();
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
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);
        
        var json = JsonSerializer.Serialize(recipes, new JsonSerializerOptions { WriteIndented = true });
        var content = BinaryData.FromString(json);
        
        await blobClient.UploadAsync(content, overwrite: true);
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
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                var response = await blobClient.DownloadContentAsync();
                var json = response.Value.Content.ToString();
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
