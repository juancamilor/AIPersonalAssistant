using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SkiaSharp;

namespace AIPersonalAssistant.Web.Services;

public class BlobRecipeImageService : IRecipeImageService
{
    private readonly BlobContainerClient _containerClient;
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    public BlobRecipeImageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("recipe-images");
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> SaveImageAsync(string recipeId, Stream imageStream, string fileName)
    {
        var imageId = Guid.NewGuid().ToString();
        var blobPath = $"{recipeId}/{imageId}.jpg";

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        ms.Position = 0;

        byte[] imageBytes;
        if (ms.Length > MaxFileSizeBytes)
        {
            imageBytes = ResizeImage(ms);
        }
        else
        {
            imageBytes = ms.ToArray();
        }

        try
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            await using var uploadStream = new MemoryStream(imageBytes);
            await blobClient.UploadAsync(uploadStream, new BlobHttpHeaders { ContentType = "image/jpeg" });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload image to blob storage: {ex.Message}", ex);
        }

        return imageId;
    }

    public async Task DeleteImageAsync(string recipeId, string imageId)
    {
        var blobPath = $"{recipeId}/{imageId}.jpg";
        var blobClient = _containerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<(Stream stream, string contentType)?> GetImageAsync(string recipeId, string imageId)
    {
        var blobPath = $"{recipeId}/{imageId}.jpg";
        var blobClient = _containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync();
        return (response.Value.Content, "image/jpeg");
    }

    public async Task DeleteAllImagesAsync(string recipeId)
    {
        var prefix = $"{recipeId}/";
        await foreach (var blob in _containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None))
        {
            await _containerClient.DeleteBlobIfExistsAsync(blob.Name);
        }
    }

    private static byte[] ResizeImage(MemoryStream input)
    {
        input.Position = 0;
        using var original = SKBitmap.Decode(input);

        var width = original.Width;
        var height = original.Height;
        var quality = 85;

        while (true)
        {
            var scaledBitmap = original;
            bool resized = false;

            if (width != original.Width || height != original.Height)
            {
                scaledBitmap = original.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear));
                resized = true;
            }

            using var image = SKImage.FromBitmap(scaledBitmap);
            var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            if (resized)
            {
                scaledBitmap.Dispose();
            }

            if (data.Size <= MaxFileSizeBytes || quality <= 20)
            {
                return data.ToArray();
            }

            if (quality > 40)
            {
                quality -= 10;
            }
            else
            {
                quality = 20;
                width = (int)(width * 0.8);
                height = (int)(height * 0.8);
            }
        }
    }
}
