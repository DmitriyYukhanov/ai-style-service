using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;

const string NEGATIVE_PROMPT = "blurry, low quality, low resolution, out of focus, bad anatomy, extra limbs, poorly drawn face, deformed eyes, unbalanced lighting, noisy, jpeg artifacts, double face, mutated hands, grainy, text, watermark";

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Получение токена из переменной окружения
string replicateToken = Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN")
                        ?? throw new InvalidOperationException("REPLICATE_API_TOKEN is not set");

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", replicateToken);

app.MapPost("/api/style", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data");

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    var prompt = form["prompt"].ToString();
    var negative_prompt = form["negative_prompt"].ToString() ?? NEGATIVE_PROMPT;
    var strength = float.TryParse(form["strength"], out var s) ? s : 0.5f;
    var inference_steps = int.TryParse(form["inference_steps"], out var isteps) ? isteps : 30;
    var guidance_scale = float.TryParse(form["guidance_scale"], out var gscale) ? gscale : 7.5f;
    var seed = int.TryParse(form["seed"], out var sd) ? sd : (int?)null;

    if (file == null || string.IsNullOrEmpty(prompt))
        return Results.BadRequest("`file` and `prompt` are required");

    Console.WriteLine($"Processing request - Prompt: {prompt}, Strength: {strength}, Seed: {seed}");

    using var originalImage = Image.Load(file.OpenReadStream());
    int originalWidth = originalImage.Width;
    int originalHeight = originalImage.Height;

    Console.WriteLine($"Original image size: {originalWidth}x{originalHeight}");

    var targetSize = GetResizeDimensions(originalWidth, originalHeight, 1024, 1024);
    
    // Resize using ImageSharp
    originalImage.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

    Console.WriteLine($"Resized image size: {targetSize.Width}x{targetSize.Height}");

    byte[] imageBytes;
    using (var ms = new MemoryStream())
    {
        await originalImage.SaveAsPngAsync(ms);
        imageBytes = ms.ToArray();
    }

    // Create data URI format that Replicate expects
    string imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";

    var inputObject = new Dictionary<string, object>
    {
        ["prompt"] = prompt,
        ["image"] = imageDataUri,
        ["prompt_strength"] = strength,
        ["num_inference_steps"] = inference_steps,
        ["guidance_scale"] = guidance_scale,
        ["negative_prompt"] = negative_prompt
    };

    // Only add seed if it has a value
    if (seed.HasValue)
    {
        inputObject["seed"] = seed.Value;
    }

    var requestBody = new
    {
        version = "15a3689ee13b0d2616e98820eca31d4c3abcd36672df6afce5cb6feb1d66087d",
        input = inputObject
    };

    var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    });
    
    Console.WriteLine($"Request payload size: {jsonContent.Length} characters");

    var content = new StringContent(jsonContent);
    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

    try
    {
        var response = await httpClient.PostAsync("https://api.replicate.com/v1/predictions", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Replicate API Error ({response.StatusCode}): {errorContent}");
            return Results.Problem($"Replicate API Error: {errorContent}", statusCode: (int)response.StatusCode);
        }

        using var respStream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(respStream);
        
        // Check if the response has the expected structure
        if (!doc.RootElement.TryGetProperty("urls", out var urlsProperty) ||
            !urlsProperty.TryGetProperty("get", out var getUrlProperty))
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Unexpected response structure: {responseContent}");
            return Results.Problem("Unexpected response structure from Replicate API");
        }

        var predictionUrl = getUrlProperty.GetString();
        Console.WriteLine($"Prediction URL: {predictionUrl}");

        // Poll for completion
        string? outputUrl = null;
        for (int i = 0; i < 60; i++) // Wait up to 60 seconds
        {
            await Task.Delay(1000);
            var pollResponse = await httpClient.GetAsync(predictionUrl);
            pollResponse.EnsureSuccessStatusCode();
            
            using var pollStream = await pollResponse.Content.ReadAsStreamAsync();
            using var pollDoc = await JsonDocument.ParseAsync(pollStream);
            
            var status = pollDoc.RootElement.GetProperty("status").GetString();
            Console.WriteLine($"Status: {status}");
            
            if (status == "succeeded")
            {
                if (pollDoc.RootElement.TryGetProperty("output", out var outputProperty) && 
                    outputProperty.ValueKind == JsonValueKind.Array && 
                    outputProperty.GetArrayLength() > 0)
                {
                    outputUrl = outputProperty[0].GetString();
                    break;
                }
            }
            else if (status == "failed")
            {
                var error = pollDoc.RootElement.TryGetProperty("error", out var errorProp) 
                    ? errorProp.GetString() 
                    : "Unknown error";
                Console.WriteLine($"Prediction failed: {error}");
                return Results.Problem($"Prediction failed: {error}");
            }
        }

        if (outputUrl == null)
        {
            return Results.Problem("Prediction timed out or failed to complete");
        }

        Console.WriteLine($"Output URL: {outputUrl}");

        var outputBytes = await httpClient.GetByteArrayAsync(outputUrl);
        using var outputImage = Image.Load(outputBytes);
        
        // Resize back to original size
        outputImage.Mutate(x => x.Resize(originalWidth, originalHeight));

        using var outStream = new MemoryStream();
        await outputImage.SaveAsJpegAsync(outStream);

        Console.WriteLine("Successfully processed image");
        return Results.File(outStream.ToArray(), "image/jpeg");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Internal error: {ex.Message}");
    }
});

app.Run();

static (int Width, int Height) GetResizeDimensions(int w, int h, int maxW, int maxH)
{
    float ratio = Math.Min((float)maxW / w, (float)maxH / h);
    return ((int)Math.Round(w * ratio), (int)Math.Round(h * ratio));
}