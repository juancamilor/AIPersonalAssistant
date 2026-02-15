using SkiaSharp;

namespace AIPersonalAssistant.Web.Services;

public class LocalTravelImageService : ITravelImageService
{
    private readonly string _imageDirectory;
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    public LocalTravelImageService(IWebHostEnvironment environment)
    {
        _imageDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "travel-images");
        if (!Directory.Exists(_imageDirectory))
        {
            Directory.CreateDirectory(_imageDirectory);
        }
    }

    public async Task<string> SaveImageAsync(string pinId, Stream imageStream, string fileName)
    {
        var imageId = Guid.NewGuid().ToString();
        var pinDir = Path.Combine(_imageDirectory, pinId);
        if (!Directory.Exists(pinDir))
        {
            Directory.CreateDirectory(pinDir);
        }

        var filePath = Path.Combine(pinDir, $"{imageId}.jpg");

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        ms.Position = 0;

        if (ms.Length > MaxFileSizeBytes)
        {
            var resized = ResizeImage(ms);
            await File.WriteAllBytesAsync(filePath, resized);
        }
        else
        {
            await using var fs = new FileStream(filePath, FileMode.Create);
            await ms.CopyToAsync(fs);
        }

        return imageId;
    }

    public Task DeleteImageAsync(string pinId, string imageId)
    {
        var filePath = Path.Combine(_imageDirectory, pinId, $"{imageId}.jpg");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public Task<(Stream stream, string contentType)?> GetImageAsync(string pinId, string imageId)
    {
        var filePath = Path.Combine(_imageDirectory, pinId, $"{imageId}.jpg");
        if (!File.Exists(filePath))
        {
            return Task.FromResult<(Stream stream, string contentType)?>(null);
        }

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<(Stream stream, string contentType)?>((stream, "image/jpeg"));
    }

    public Task DeleteAllImagesAsync(string pinId)
    {
        var pinDir = Path.Combine(_imageDirectory, pinId);
        if (Directory.Exists(pinDir))
        {
            Directory.Delete(pinDir, true);
        }
        return Task.CompletedTask;
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
