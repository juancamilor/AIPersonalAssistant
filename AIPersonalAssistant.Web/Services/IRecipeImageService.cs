namespace AIPersonalAssistant.Web.Services;

public interface IRecipeImageService
{
    Task<string> SaveImageAsync(string recipeId, Stream imageStream, string fileName);
    Task DeleteImageAsync(string recipeId, string imageId);
    Task<(Stream stream, string contentType)?> GetImageAsync(string recipeId, string imageId);
    Task DeleteAllImagesAsync(string recipeId);
}
