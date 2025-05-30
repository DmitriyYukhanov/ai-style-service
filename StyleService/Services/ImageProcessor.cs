using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace StyleService.Services;

public class ImageProcessor : IImageProcessor
{
    public async Task<byte[]> ProcessImageAsync(Stream imageStream, int maxWidth = 768, int maxHeight = 768)
    {
        using var originalImage = await Image.LoadAsync(imageStream);
        int originalWidth = originalImage.Width;
        int originalHeight = originalImage.Height;

        Console.WriteLine($"Original image size: {originalWidth}x{originalHeight}");

        // Optimize image size for faster processing
        var targetSize = GetResizeDimensions(originalWidth, originalHeight, maxWidth, maxHeight);
        
        // Only resize if the image is larger than target
        if (originalWidth > targetSize.Width || originalHeight > targetSize.Height)
        {
            originalImage.Mutate(x => x.Resize(targetSize.Width, targetSize.Height, KnownResamplers.Lanczos3));
            Console.WriteLine($"Resized image size: {targetSize.Width}x{targetSize.Height}");
        }
        else
        {
            Console.WriteLine($"Image size unchanged: {originalWidth}x{originalHeight} (already optimal)");
        }

        using var ms = new MemoryStream();
        await originalImage.SaveAsPngAsync(ms);
        return ms.ToArray();
    }

    public (int Width, int Height) GetResizeDimensions(int width, int height, int maxWidth, int maxHeight)
    {
        float ratio = Math.Min((float)maxWidth / width, (float)maxHeight / height);
        return ((int)Math.Round(width * ratio), (int)Math.Round(height * ratio));
    }

    public async Task<byte[]> ResizeImageToOriginalAsync(byte[] imageBytes, int originalWidth, int originalHeight)
    {
        using var outputImage = await Image.LoadAsync(new MemoryStream(imageBytes));
        
        // Resize back to original size
        outputImage.Mutate(x => x.Resize(originalWidth, originalHeight));

        using var outStream = new MemoryStream();
        await outputImage.SaveAsJpegAsync(outStream);
        return outStream.ToArray();
    }
} 