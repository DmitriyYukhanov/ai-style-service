# AR Masker AI Style Service Client

Unity client package for integrating with AR Masker AI Style Service. Apply artistic styles to images using AI models directly from Unity.

## Features

- 🎨 Apply artistic styles to Texture2D objects
- 🚀 Async/await support with UnityWebRequest
- 🔧 Configurable style parameters (strength, steps, guidance)
- 📱 Cross-platform support (iOS, Android, WebGL, Desktop)
- 🛡️ Built-in error handling and timeout management
- 📦 Easy integration with existing Unity projects

## Installation

### Via Package Manager (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/your-username/ar-masker-ai-style-service-client.git`

### Via Git URL in manifest.json

Add this line to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.armasker.ai-style-service-client": "https://github.com/your-username/ar-masker-ai-style-service-client.git"
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
    
    private AiStyleServiceClient client;
    
    void Start()
    {
        client = new AiStyleServiceClient(serviceUrl);
        ApplyStyle();
    }
    
    private async void ApplyStyle()
    {
        var request = new StyleTransferRequest
        {
            Prompt = "anime style",
            Strength = 0.7f,
            InferenceSteps = 30
        };
        
        try
        {
            Texture2D result = await client.ApplyStyleAsync(inputTexture, request);
            // Use the styled texture
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Style transfer failed: {e.Message}");
        }
    }
}
```

## Requirements

- Unity 2021.3 or later
- Newtonsoft JSON package (automatically installed)
- Internet connection for API calls

## API Reference

See the [full documentation](Documentation~/api-reference.md) for detailed API reference.

## Samples

Import samples via Package Manager to see working examples:
- **Basic Style Transfer**: Simple style application example
- **Advanced Parameters**: Using all available style parameters
- **Batch Processing**: Processing multiple images

## Support

For issues and questions:
- Check the [troubleshooting guide](Documentation~/troubleshooting.md)
- Open an issue on [GitHub](https://github.com/your-username/ar-masker-ai-style-service-client/issues)

## License

[Your License Here] 