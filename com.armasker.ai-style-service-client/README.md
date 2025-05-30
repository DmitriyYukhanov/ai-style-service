# AR Masker AI Style Service Client

Unity client package for integrating with AR Masker AI Style Service. Apply artistic styles to images using AI models directly from Unity.

## API Reference

### Initialization

```csharp
// Initialize with default configuration
AiStyleServiceClient.Initialize();

// Initialize with custom configuration
AiStyleServiceClient.Initialize(new CustomRestConfig("https://your-service.com"));
```

### Style Transfer Methods

#### Standard Style Transfer

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

#### Flux Style Transfer (New!)

```csharp
Task<ApiResponse<TextureResponse>> StyleImageFluxAsync(
    Texture2D inputTexture, 
    string prompt,
    string aspectRatio = "match_input_image")
```

### Parameters

#### Standard Style Transfer Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `inputTexture` | Texture2D | - | Required | Input image to style |
| `prompt` | string | - | Required | Style description |
| `negativePrompt` | string | - | null | What to avoid |
| `strength` | float | 0.0-1.0 | 0.5 | Style application strength |
| `inferenceSteps` | int | 1-100 | 30 | Number of denoising steps |
| `guidanceScale` | float | 1.0-20.0 | 7.5 | Prompt adherence |
| `seed` | int? | - | null | Random seed |

#### Flux Style Transfer Parameters

| Parameter | Type | Options | Default | Description |
|-----------|------|---------|---------|-------------|
| `inputTexture` | Texture2D | - | Required | Input image to style |
| `prompt` | string | - | Required | Style transformation description |
| `aspectRatio` | string | "16:9", "1:1", "9:16", "4:3", "3:4", etc. or match_input_image to match input texture aspect ratio | "16:9" | Output aspect ratio |

### Usage Examples

#### Standard Style Transfer

```csharp
using ArMasker.AiStyleService.Client;
using UnityEngine;

public class StyleExample : MonoBehaviour
{
    [SerializeField] private Texture2D inputTexture;
    
    async void Start()
    {
        // Initialize the client
        AiStyleServiceClient.Initialize();
        
        // Apply style
        var response = await AiStyleServiceClient.StyleImageAsync(
            inputTexture, "anime style");
        
        if (response.Success && response.Data?.TextureData != null)
        {
            // Use the styled texture
            GetComponent<Renderer>().material.mainTexture = response.Data.TextureData;
        }
        else
        {
            Debug.LogError($"Style transfer failed: {response.Error}");
        }
    }
}
```

#### Flux Style Transfer

```csharp
using ArMasker.AiStyleService.Client;
using UnityEngine;

public class FluxStyleExample : MonoBehaviour
{
    [SerializeField] private Texture2D inputTexture;
    
    async void Start()
    {
        // Initialize the client
        AiStyleServiceClient.Initialize();
        
        // Apply Flux style transformation
        var response = await AiStyleServiceClient.StyleImageFluxAsync(
            inputTexture, 
            "Make this a 90s cartoon", 
            "16:9");
        
        if (response.Success && response.Data?.TextureData != null)
        {
            // Use the styled texture
            GetComponent<Renderer>().material.mainTexture = response.Data.TextureData;
        }
        else
        {
            Debug.LogError($"Flux style transfer failed: {response.Error}");
        }
    }
}
```

### Response

Returns `ApiResponse<TextureResponse>` containing:
- `Success`: Whether the operation succeeded
- `Data.TextureData`: The styled Texture2D (if successful)
- `Error`: Error information (if failed)
- `StatusCode`: HTTP status code

## Requirements

- Unity 2021.3 or later
- Internet connection for API calls

## Features

- 🎨 Apply artistic styles to Texture2D objects
- ⚡ **NEW**: Flux model support with aspect ratio control
- 🚀 Async/await support with UnityWebRequest
- 🔧 Configurable style parameters (strength, steps, guidance, seed)
- 📱 Cross-platform support (iOS, Android, WebGL, Desktop)
- 🛡️ Built-in error handling and timeout management
- 📦 Easy integration with existing Unity projects
- 🖼️ Direct Texture2D input/output - no file handling needed

## Model Comparison

### Standard Style Transfer
- **Best for**: Fine-tuned artistic styles with precise control
- **Parameters**: Strength, inference steps, guidance scale, negative prompts
- **Use cases**: Traditional art styles, detailed customization
- **Processing time**: 30-60 seconds

### Flux Style Transfer
- **Best for**: Modern transformations and cartoon styles
- **Parameters**: Aspect ratio control, simplified interface
- **Use cases**: Cartoon conversion, modern art styles, aspect ratio changes
- **Processing time**: 20-40 seconds
- **Advantages**: Faster processing, better cartoon/modern styles

## Performance Tips

- **Texture Size**: Smaller textures (512x512 or less) process faster
- **Model Choice**: 
  - Use **Flux** for cartoon/modern styles and faster processing
  - Use **Standard** for traditional art styles and fine control
- **Inference Steps**: Use 20-25 for faster results, 40-50 for higher quality (Standard only)
- **Caching**: Store results to avoid re-processing the same images
- **Background Processing**: Use coroutines or async/await to avoid blocking the main thread

## Common Style Prompts

### Standard Style Transfer

| Style | Example Prompt |
|-------|----------------|
| Anime | `anime style, cel shading, vibrant colors` |
| Oil Painting | `oil painting, thick brushstrokes, classical art` |
| Watercolor | `watercolor painting, soft edges, flowing colors` |
| Digital Art | `digital art, concept art, detailed illustration` |
| Sketch | `pencil sketch, black and white, hand drawn` |
| Photography | `professional photography, cinematic lighting` |

### Flux Style Transfer

| Style | Example Prompt |
|-------|----------------|
| Cartoon | `Make this a 90s cartoon` |
| Cyberpunk | `Transform into cyberpunk style` |
| Pixel Art | `Convert to pixel art style` |
| Comic Book | `Make this look like a comic book` |
| Vintage | `Give this a vintage 80s look` |
| Minimalist | `Convert to minimalist art style` |

## Aspect Ratios (Flux Only)

| Ratio | Description | Use Case |
|-------|-------------|----------|
| `16:9` | Widescreen | Landscape, cinematic |
| `1:1` | Square | Social media, profile pics |
| `9:16` | Vertical | Mobile, stories |
| `4:3` | Traditional | Classic photography |
| `3:4` | Portrait | Vertical photography |

## Troubleshooting

### Common Issues

1. **"Client not initialized"**: Call `AiStyleServiceClient.Initialize()` before making requests
2. **Network timeouts**: Check internet connection and service availability
3. **Texture format errors**: Ensure input textures are readable (Read/Write enabled)
4. **Memory issues**: Dispose of unused textures to free memory
5. **Invalid aspect ratio**: Ensure format is "width:height" (e.g., "16:9")

### Debug Logging

Enable debug logging in development builds to see detailed request/response information.