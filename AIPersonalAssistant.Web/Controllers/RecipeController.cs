using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;
using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IRecipeImageService _imageService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<RecipeController> _logger;

    public RecipeController(IRecipeService recipeService, IRecipeImageService imageService, IWebHostEnvironment env, ILogger<RecipeController> logger)
    {
        _recipeService = recipeService;
        _imageService = imageService;
        _env = env;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? "anonymous";
    }

    [HttpGet]
    public async Task<IActionResult> GetRecipes()
    {
        var userId = GetUserId();
        var recipes = await _recipeService.GetAllByUserIdAsync(userId);
        return Ok(recipes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecipe(string id)
    {
        var userId = GetUserId();
        var recipe = await _recipeService.GetByIdAsync(userId, id);
        
        if (recipe == null)
        {
            return NotFound(new { error = "Recipe not found" });
        }
        
        return Ok(recipe);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var userId = GetUserId();
        var categories = await _recipeService.GetCategoriesAsync(userId);
        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipe([FromBody] RecipeRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var recipe = await _recipeService.CreateAsync(userId, request);
        
        return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecipe(string id, [FromBody] RecipeRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var recipe = await _recipeService.UpdateAsync(userId, id, request);
        
        if (recipe == null)
        {
            return NotFound(new { error = "Recipe not found" });
        }
        
        return Ok(recipe);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecipe(string id)
    {
        var userId = GetUserId();
        var recipe = await _recipeService.GetByIdAsync(userId, id);
        
        if (recipe == null)
        {
            return NotFound(new { error = "Recipe not found" });
        }

        await _imageService.DeleteAllImagesAsync(id);
        await _recipeService.DeleteAsync(userId, id);
        
        return NoContent();
    }

    [HttpPost("{recipeId}/images")]
    public async Task<IActionResult> UploadImage(string recipeId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Only JPEG, PNG, and WebP images are allowed");

        var userId = GetUserId();
        var recipe = await _recipeService.GetByIdAsync(userId, recipeId);
        if (recipe == null)
            return NotFound("Recipe not found");
        if (recipe.ImageUrls?.Count >= 5)
            return BadRequest("Maximum 5 images per recipe");

        try
        {
            using var stream = file.OpenReadStream();
            var imageId = await _imageService.SaveImageAsync(recipeId, stream, file.FileName);

            recipe.ImageUrls ??= new List<string>();
            recipe.ImageUrls.Add(imageId);
            await _recipeService.UpdateAsync(userId, recipeId, new RecipeRequest
            {
                Title = recipe.Title,
                Description = recipe.Description,
                Ingredients = recipe.Ingredients,
                Instructions = recipe.Instructions,
                Category = recipe.Category,
                PrepTime = recipe.PrepTime,
                CookTime = recipe.CookTime,
                Servings = recipe.Servings,
                ImageUrls = recipe.ImageUrls
            });

            return Ok(new { imageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image upload failed for recipe {RecipeId}, file size: {FileSize} bytes", recipeId, file.Length);
            var message = _env.IsDevelopment()
                ? $"Image upload failed: {ex.Message}"
                : "Image upload failed. Please try again.";
            return StatusCode(500, new { error = message });
        }
    }

    [HttpDelete("{recipeId}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(string recipeId, string imageId)
    {
        var userId = GetUserId();
        var recipe = await _recipeService.GetByIdAsync(userId, recipeId);
        if (recipe == null)
            return NotFound("Recipe not found");

        await _imageService.DeleteImageAsync(recipeId, imageId);
        recipe.ImageUrls?.Remove(imageId);
        await _recipeService.UpdateAsync(userId, recipeId, new RecipeRequest
        {
            Title = recipe.Title,
            Description = recipe.Description,
            Ingredients = recipe.Ingredients,
            Instructions = recipe.Instructions,
            Category = recipe.Category,
            PrepTime = recipe.PrepTime,
            CookTime = recipe.CookTime,
            Servings = recipe.Servings,
            ImageUrls = recipe.ImageUrls
        });

        return Ok();
    }

    [HttpGet("{recipeId}/images/{imageId}")]
    public async Task<IActionResult> GetImage(string recipeId, string imageId)
    {
        var result = await _imageService.GetImageAsync(recipeId, imageId);
        if (result == null)
            return NotFound();

        return File(result.Value.stream, result.Value.contentType);
    }

    [AllowAnonymous]
    [HttpGet("shared/{shareToken}")]
    public async Task<IActionResult> GetSharedRecipe(string shareToken)
    {
        var recipe = await _recipeService.GetByShareTokenAsync(shareToken);
        
        if (recipe == null)
        {
            return NotFound(new { error = "Shared recipe not found" });
        }

        return Ok(new
        {
            recipe.Id,
            recipe.Title,
            recipe.Description,
            recipe.Ingredients,
            recipe.Instructions,
            recipe.Category,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            recipe.ImageUrls,
            recipe.ShareToken,
            recipe.CreatedAt,
            recipe.UpdatedAt
        });
    }
}
