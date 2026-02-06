namespace AIPersonalAssistant.Web.Services;

public interface ITravelImageService
{
    Task<string> SaveImageAsync(string pinId, Stream imageStream, string fileName);
    Task DeleteImageAsync(string pinId, string imageId);
    Task<(Stream stream, string contentType)?> GetImageAsync(string pinId, string imageId);
    Task DeleteAllImagesAsync(string pinId);
}
