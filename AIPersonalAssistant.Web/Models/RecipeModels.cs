namespace AIPersonalAssistant.Web.Models;

public class Recipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PrepTime { get; set; } = string.Empty;
    public string CookTime { get; set; } = string.Empty;
    public string Servings { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string ShareToken { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RecipeRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PrepTime { get; set; } = string.Empty;
    public string CookTime { get; set; } = string.Empty;
    public string Servings { get; set; } = string.Empty;
    public List<string>? ImageUrls { get; set; }
}
