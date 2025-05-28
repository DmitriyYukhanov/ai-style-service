# AR Masker AI Style Service Client

Unity client package for integrating with AR Masker AI Style Service. Apply artistic styles to images using AI models directly from Unity.

## Features

- 🎨 Apply artistic styles to Texture2D objects
- 🚀 Async/await support with UnityWebRequest
- 🔧 Configurable style parameters (strength, steps, guidance, seed)
- 📱 Cross-platform support (iOS, Android, WebGL, Desktop)
- 🛡️ Built-in error handling and timeout management
- 📦 Easy integration with existing Unity projects
- 🖼️ Direct Texture2D input/output - no file handling needed

## Installation

### Via Package Manager (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/DmitriyYukhanov/ar-masker-ai-style-service-client.git`

### Via Git URL in manifest.json

Add this line to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.armasker.ai-style-service-client": "https://github.com/DmitriyYukhanov/ar-masker-ai-style-service-client.git"
  }
}
```

### Manual Installation

1. Download the package files
2. Place the `com.armasker.ai-style-service-client` folder in your project's `Packages` directory

## Quick Start

```csharp
using ArMasker.AiStyleService.Client;
using UnityEngine;

public class StyleTransferExample : MonoBehaviour
{
    [SerializeField] private Texture2D inputTexture;
    [SerializeField] private string serviceUrl = "https://your-service.com";
    
    void Start()
    {
        // Initialize the client
        AiStyleServiceClient.Initialize(serviceUrl);
        ApplyStyle();
    }
    
    private async void ApplyStyle()
    {
        try
        {
            // Simple style transfer
            var response = await AiStyleServiceClient.StyleImageAsync(
                inputTexture, "anime style");
            
            if (response.Success && response.Data?.TextureData != null)
            {
                // Use the styled texture (e.g., apply to a material)
                GetComponent<Renderer>().material.mainTexture = response.Data.TextureData;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Style transfer failed: {e.Message}");
        }
    }
}
```

## Advanced Usage

```csharp
using ArMasker.AiStyleService.Client;
using ArMasker.AiStyleService.Client.Services.Rest;
using UnityEngine;

public class AdvancedStyleExample : MonoBehaviour
{
    [SerializeField] private Texture2D inputTexture;
    [SerializeField] private string serviceUrl = "https://your-service.com";
    
    private async void Start()
    {
        AiStyleServiceClient.Initialize(serviceUrl);
        
        // Advanced style transfer with all parameters
        var response = await AiStyleServiceClient.StyleImageAsync(
            inputTexture: inputTexture,
            prompt: "oil painting in the style of Van Gogh",
            negativePrompt: "blurry, low quality, distorted",
            strength: 0.7f,
            inferenceSteps: 40,
            guidanceScale: 8.0f,
            seed: 12345
        );
            
        if (response.Success && response.Data?.TextureData != null)
        {
            Texture2D styledTexture = response.Data.TextureData;
            Debug.Log($"Processing completed with status: {response.StatusCode}");
            // Use the texture...
        }
        else
        {
            Debug.LogError($"Error: {response.Error}");
        }
    }
}
```

## API Reference

### Initialization

```csharp
// Initialize with service URL
AiStyleServiceClient.Initialize("https://your-service.com");

// Or with custom config
AiStyleServiceClient.Initialize(new CustomRestConfig("https://your-service.com"));
```

### Style Transfer Method

```csharp
Task<ApiResponse<TextureResponse>> StyleImageAsync(
    Texture2D inputTexture, 
    string prompt,
    string negativePrompt = null,
    float strength = 0.5f,
    int inferenceSteps = 30,
    float guidanceScale = 7.5f,
    int? seed = null)
```

Returns full API response with texture data and error handling information.

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `inputTexture` | Texture2D | - | Required | Input image to style |
| `prompt` | string | - | Required | Style description |
| `negativePrompt` | string | - | null | What to avoid |
| `strength` | float | 0.0-1.0 | 0.5 | Style application strength |
| `inferenceSteps` | int | 1-100 | 30 | Number of denoising steps |
| `guidanceScale` | float | 1.0-20.0 | 7.5 | Prompt adherence |
| `seed` | int? | - | null | Random seed |

## Error Handling

```csharp
var response = await AiStyleServiceClient.StyleImageAsync(texture, "anime style");

if (response.Success && response.Data?.TextureData != null)
{
    Texture2D result = response.Data.TextureData;
    // Success
}
else
{
    switch (response.Error.Kind)
    {
        case ApiErrorKind.NetworkError:
            Debug.LogError("Network connection failed");
            break;
        case ApiErrorKind.ServerError:
            Debug.LogError($"Server error: {response.StatusCode}");
            break;
        case ApiErrorKind.AuthError:
            Debug.LogError("Authentication failed");
            break;
        default:
            Debug.LogError($"Unknown error: {response.Error}");
            break;
    }
}
```

## Performance Tips

- **Texture Size**: Smaller textures (512x512 or less) process faster
- **Inference Steps**: Use 20-25 for faster results, 40-50 for higher quality
- **Caching**: Store results to avoid re-processing the same images
- **Background Processing**: Use coroutines or async/await to avoid blocking the main thread

## Common Style Prompts

| Style | Example Prompt |
|-------|----------------|
| Anime | `anime style, cel shading, vibrant colors` |
| Oil Painting | `oil painting, thick brushstrokes, classical art` |
| Watercolor | `watercolor painting, soft edges, flowing colors` |
| Digital Art | `digital art, concept art, detailed illustration` |
| Sketch | `pencil sketch, black and white, hand drawn` |
| Photography | `professional photography, cinematic lighting` |

## Requirements

- Unity 2021.3 or later
- Newtonsoft JSON package (automatically installed)
- Internet connection for API calls
- Valid AI Style Service endpoint

## Troubleshooting

### Common Issues

1. **"Client not initialized"**: Call `AiStyleServiceClient.Initialize()` before making requests
2. **Network timeouts**: Check internet connection and service availability
3. **Texture format errors**: Ensure input textures are readable (Read/Write enabled)
4. **Memory issues**: Dispose of unused textures to free memory

### Debug Logging

Enable debug logging in development builds to see detailed request/response information.

## Support

For issues and questions:
- Check the [troubleshooting guide](Documentation~/troubleshooting.md)
- Open an issue on [GitHub](https://github.com/DmitriyYukhanov/ar-masker-ai-style-service-client/issues)

## License

[Your License Here] 