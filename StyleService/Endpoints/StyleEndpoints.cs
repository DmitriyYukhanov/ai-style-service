using Microsoft.AspNetCore.Http;
using StyleService.Services;
using SixLabors.ImageSharp;

namespace StyleService.Endpoints;

public static class StyleEndpoints
{
    public static async Task<IResult> HandleStyleTransfer(HttpRequest request, IImageProcessor imageProcessor, IReplicateService replicateService)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("Expected multipart/form-data");

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        var prompt = form["prompt"].ToString();
        var negative_prompt = form["negative_prompt"].ToString();
        var strength = float.TryParse(form["strength"], out var s) ? s : 0.5f;
        var inference_steps = int.TryParse(form["inference_steps"], out var isteps) ? isteps : 30;
        var guidance_scale = float.TryParse(form["guidance_scale"], out var gscale) ? gscale : 7.5f;
        var seed = int.TryParse(form["seed"], out var sd) ? sd : (int?)null;

        if (file == null || string.IsNullOrEmpty(prompt))
            return Results.BadRequest("`file` and `prompt` are required");

        Console.WriteLine($"Processing style transfer - Prompt: {prompt}, Strength: {strength}, Seed: {seed}");

        try
        {
            // Get original dimensions for later resizing
            using var originalImage = await Image.LoadAsync(file.OpenReadStream());
            int originalWidth = originalImage.Width;
            int originalHeight = originalImage.Height;

            // Process image
            var imageBytes = await imageProcessor.ProcessImageAsync(file.OpenReadStream());
            string imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";

            // Process with Replicate
            var outputBytes = await replicateService.ProcessStyleTransferAsync(
                imageDataUri, prompt, negative_prompt, strength, inference_steps, guidance_scale, seed);

            // Resize back to original size
            var finalBytes = await imageProcessor.ResizeImageToOriginalAsync(outputBytes, originalWidth, originalHeight);

            Console.WriteLine("Successfully processed style transfer");
            return Results.File(finalBytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Results.Problem($"Internal error: {ex.Message}");
        }
    }

    public static async Task<IResult> HandleFluxStyleTransfer(HttpRequest request, IImageProcessor imageProcessor, IReplicateService replicateService)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("Expected multipart/form-data");

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        var prompt = form["prompt"].ToString();
        var aspect_ratio = form["aspect_ratio"].ToString() ?? "match_input_image";

        if (file == null || string.IsNullOrEmpty(prompt))
            return Results.BadRequest("`file` and `prompt` are required");

        // Validate aspect ratio
        if (!IsValidAspectRatio(aspect_ratio))
            return Results.BadRequest("Invalid aspect_ratio. Supported formats: '16:9', '1:1', '9:16', '4:3', '3:4', etc. or 'match_input_image' to match input texture aspect ratio");

        Console.WriteLine($"Processing flux style transfer - Prompt: {prompt}, Aspect Ratio: {aspect_ratio}");

        try
        {
            // Get original dimensions for later resizing
            using var originalImage = await Image.LoadAsync(file.OpenReadStream());
            int originalWidth = originalImage.Width;
            int originalHeight = originalImage.Height;

            // Process image (flux models typically work better with higher resolution)
            var imageBytes = await imageProcessor.ProcessImageAsync(file.OpenReadStream(), 1024, 1024);
            string imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";

            // Process with Replicate Flux
            var outputBytes = await replicateService.ProcessFluxStyleTransferAsync(imageDataUri, prompt, aspect_ratio);

            // Resize back to original size
            var finalBytes = await imageProcessor.ResizeImageToOriginalAsync(outputBytes, originalWidth, originalHeight);

            Console.WriteLine("Successfully processed flux style transfer");
            return Results.File(finalBytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Results.Problem($"Internal error: {ex.Message}");
        }
    }

    private static bool IsValidAspectRatio(string aspectRatio)
    {
        if (string.IsNullOrEmpty(aspectRatio))
            return false;

        if (aspectRatio == "match_input_image")
            return true;

        var parts = aspectRatio.Split(':');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
    }
} 