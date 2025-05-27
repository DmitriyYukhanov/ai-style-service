using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

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
    var strength = float.TryParse(form["strength"], out var s) ? s : 0.5f;
    var seed = int.TryParse(form["seed"], out var sd) ? sd : (int?)null;

    if (file == null || string.IsNullOrEmpty(prompt))
        return Results.BadRequest("`file` and `prompt` are required");

    using var originalImage = Image.FromStream(file.OpenReadStream());
    int originalWidth = originalImage.Width;
    int originalHeight = originalImage.Height;

    var targetSize = GetResizeDimensions(originalWidth, originalHeight, 768, 768);
    using var resized = ResizeImage(originalImage, targetSize.Width, targetSize.Height);

    byte[] imageBytes;
    using (var ms = new MemoryStream())
    {
        resized.Save(ms, ImageFormat.Png);
        imageBytes = ms.ToArray();
    }

    var requestBody = new
    {
        version = "db21e45d431bdc6155d93c6f4ef8327df0ebe9e53e81bf77cebc6a83ce2f79be",
        input = new { image = Convert.ToBase64String(imageBytes), prompt, strength, seed }
    };

    var content = new StringContent(JsonSerializer.Serialize(requestBody));
    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

    var response = await httpClient.PostAsync("https://api.replicate.com/v1/predictions", content);
    response.EnsureSuccessStatusCode();

    using var respStream = await response.Content.ReadAsStreamAsync();
    using var doc = await JsonDocument.ParseAsync(respStream);
    var outputUrl = doc.RootElement.GetProperty("prediction").GetProperty("output")[0].GetString();

    var outputBytes = await httpClient.GetByteArrayAsync(outputUrl);
    using var outputImage = Image.FromStream(new MemoryStream(outputBytes));
    using var finalImage = ResizeImage(outputImage, originalWidth, originalHeight);

    using var outStream = new MemoryStream();
    finalImage.Save(outStream, ImageFormat.Jpeg);

    return Results.File(outStream.ToArray(), "image/jpeg");
});

app.Run();

static Image ResizeImage(Image image, int width, int height)
{
    var dest = new Bitmap(width, height);
    dest.SetResolution(image.HorizontalResolution, image.VerticalResolution);
    using var g = Graphics.FromImage(dest);
    g.CompositingQuality = CompositingQuality.HighQuality;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.SmoothingMode = SmoothingMode.HighQuality;
    g.DrawImage(image, 0, 0, width, height);
    return dest;
}

static (int Width, int Height) GetResizeDimensions(int w, int h, int maxW, int maxH)
{
    float ratio = Math.Min((float)maxW / w, (float)maxH / h);
    return ((int)Math.Round(w * ratio), (int)Math.Round(h * ratio));
}