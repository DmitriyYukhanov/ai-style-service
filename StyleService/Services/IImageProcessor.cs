using SixLabors.ImageSharp;

namespace StyleService.Services;

public interface IImageProcessor
{
    Task<byte[]> ProcessImageAsync(Stream imageStream, int maxWidth = 768, int maxHeight = 768);
    (int Width, int Height) GetResizeDimensions(int width, int height, int maxWidth, int maxHeight);
    Task<byte[]> ResizeImageToOriginalAsync(byte[] imageBytes, int originalWidth, int originalHeight);
} 