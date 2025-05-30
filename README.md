# AI Style Service

A REST API service for applying artistic styles to images using AI models via Replicate.

## Overview

This service transforms images by applying artistic styles based on text prompts. It uses advanced AI models to generate stylized versions of your input images while maintaining the original content structure.

## Quick Start

### Prerequisites

- Docker (for deployment)
- Replicate API token

### Environment Variables

```bash
REPLICATE_API_TOKEN=your_replicate_token_here
```

### Running with Docker

```bash
docker build -t ai-style-service .
docker run -p 80:80 -e REPLICATE_API_TOKEN=your_token ai-style-service
```

## API Reference

### Apply Style to Image (Standard)

Transforms an input image by applying an artistic style based on a text prompt using the standard model.

```http
POST /api/style
```

#### Request

**Content-Type:** `multipart/form-data`

#### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `file` | file | ✅ | - | Input image file (JPEG, PNG, WebP) |
| `prompt` | string | ✅ | - | Style description (e.g., "anime style", "oil painting") |
| `negative_prompt` | string | ❌ | Auto-generated | What to avoid in the output |
| `strength` | float | ❌ | `0.5` | Style application strength (0.0-1.0) |
| `inference_steps` | integer | ❌ | `30` | Number of denoising steps (1-100) |
| `guidance_scale` | float | ❌ | `7.5` | How closely to follow the prompt (1.0-20.0) |
| `seed` | integer | ❌ | Random | Random seed for reproducible results |

### Apply Style to Image (Flux) 🆕

Transforms an input image using the Flux model with aspect ratio control and faster processing.

```http
POST /api/style-flux
```

#### Request

**Content-Type:** `multipart/form-data`

#### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `file` | file | ✅ | - | Input image file (JPEG, PNG, WebP) |
| `prompt` | string | ✅ | - | Style transformation description (e.g., "Make this a 90s cartoon") |
| `aspect_ratio` | string | ❌ | `16:9` | Output aspect ratio (e.g., "16:9", "1:1", "9:16", "4:3", "3:4" or "match_input_image") |

#### Parameter Details

- **file**: Maximum recommended size 1024x1024px for Flux (higher than standard for better quality)
- **prompt**: Describe the desired transformation or style
- **aspect_ratio**: Controls the output image dimensions. Format: "width:height"

#### Response (Both Endpoints)

**Success (200 OK)**

```http
Content-Type: image/jpeg
Content-Length: [file_size]

[Binary image data]
```

**Error Responses**

```json
// 400 Bad Request
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "`file` and `prompt` are required"
}

// 500 Internal Server Error
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error", 
  "status": 500,
  "detail": "Prediction timed out after 180 seconds. The model may be experiencing high demand."
}
```

## Usage Examples

### Standard Style Transfer

```bash
curl -X POST http://localhost/api/style \
  -F "file=@portrait.jpg" \
  -F "prompt=anime style" \
  -o styled_output.jpg
```

### Flux Style Transfer

```bash
curl -X POST http://localhost/api/style-flux \
  -F "file=@portrait.jpg" \
  -F "prompt=Make this a 90s cartoon" \
  -F "aspect_ratio=16:9" \
  -o flux_styled_output.jpg
```

### Advanced Standard Style Transfer

```bash
curl -X POST http://localhost/api/style \
  -F "file=@landscape.jpg" \
  -F "prompt=impressionist oil painting in the style of Monet" \
  -F "negative_prompt=blurry, low quality, distorted" \
  -F "strength=0.7" \
  -F "inference_steps=40" \
  -F "guidance_scale=8.0" \
  -F "seed=12345" \
  -o monet_landscape.jpg
```

### JavaScript/Fetch Examples

#### Standard Style Transfer

```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);
formData.append('prompt', 'watercolor painting');
formData.append('strength', '0.6');

const response = await fetch('/api/style', {
  method: 'POST',
  body: formData
});

if (response.ok) {
  const blob = await response.blob();
  const imageUrl = URL.createObjectURL(blob);
  // Use imageUrl to display the result
} else {
  const error = await response.json();
  console.error('Error:', error.detail);
}
```

#### Flux Style Transfer

```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);
formData.append('prompt', 'Make this a 90s cartoon');
formData.append('aspect_ratio', '1:1');

const response = await fetch('/api/style-flux', {
  method: 'POST',
  body: formData
});

if (response.ok) {
  const blob = await response.blob();
  const imageUrl = URL.createObjectURL(blob);
  // Use imageUrl to display the result
} else {
  const error = await response.json();
  console.error('Error:', error.detail);
}
```

### Python Examples

#### Standard Style Transfer

```python
import requests

url = 'http://localhost/api/style'
files = {'file': open('input.jpg', 'rb')}
data = {
    'prompt': 'cyberpunk digital art',
    'strength': 0.8,
    'inference_steps': 35,
    'seed': 42
}

response = requests.post(url, files=files, data=data)

if response.status_code == 200:
    with open('output.jpg', 'wb') as f:
        f.write(response.content)
else:
    print(f"Error: {response.json()}")
```

#### Flux Style Transfer

```python
import requests

url = 'http://localhost/api/style-flux'
files = {'file': open('input.jpg', 'rb')}
data = {
    'prompt': 'Make this a 90s cartoon',
    'aspect_ratio': '16:9'
}

response = requests.post(url, files=files, data=data)

if response.status_code == 200:
    with open('flux_output.jpg', 'wb') as f:
        f.write(response.content)
else:
    print(f"Error: {response.json()}")
```

## Model Comparison

### Standard Style Transfer (`/api/style`)
- **Best for**: Fine-tuned artistic styles with precise control
- **Parameters**: Strength, inference steps, guidance scale, negative prompts, seed
- **Use cases**: Traditional art styles, detailed customization, reproducible results
- **Processing time**: 30-60 seconds
- **Model**: Stable Diffusion based

### Flux Style Transfer (`/api/style-flux`) 🆕
- **Best for**: Modern transformations and cartoon styles
- **Parameters**: Aspect ratio control, simplified interface
- **Use cases**: Cartoon conversion, modern art styles, aspect ratio changes
- **Processing time**: 20-40 seconds
- **Model**: Flux Kontext Pro
- **Advantages**: Faster processing, better cartoon/modern styles, aspect ratio control

## Performance Tips

### For Faster Processing
- Use Flux model (`/api/style-flux`) for cartoon/modern styles
- Use images smaller than 768x768px for standard model
- Use images smaller than 1024x1024px for Flux model
- Reduce `inference_steps` to 20-25 (standard model only)
- Use simpler, shorter prompts

### For Higher Quality
- Use standard model (`/api/style`) for traditional art styles
- Increase `inference_steps` to 40-50 (standard model only)
- Use detailed, specific prompts
- Adjust `strength` based on desired effect (standard model only)
- Experiment with different `guidance_scale` values (standard model only)

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

## Error Handling

The service returns standard HTTP status codes:

- `200` - Success
- `400` - Bad Request (missing required parameters, invalid aspect ratio)
- `422` - Unprocessable Entity (invalid parameter values)
- `500` - Internal Server Error (processing failed or timeout)

All error responses follow RFC 7807 Problem Details format.

## Rate Limits

Processing time varies based on:
- Model choice (Flux is typically faster)
- Image complexity and size
- Model availability (cold start vs warm)
- Current service demand
- Parameter values (steps, guidance scale for standard model)

Typical processing time:
- **Standard model**: 30-60 seconds per image
- **Flux model**: 20-40 seconds per image

## Technical Details

- **Framework**: ASP.NET Core 9.0
- **Image Processing**: SixLabors.ImageSharp
- **AI Provider**: Replicate API
- **Models**: 
  - Standard: Stable Diffusion based model
  - Flux: Black Forest Labs Flux Kontext Pro
- **Supported Formats**: JPEG, PNG, WebP (input), JPEG (output)
- **Max Timeout**: 180 seconds

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
# ... (see Dockerfile for complete configuration)
```

### Environment Configuration

```bash
# Required
REPLICATE_API_TOKEN=r8_your_token_here

# Optional
ASPNETCORE_URLS=http://*:80
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
```

## License

[Your License Here]

## Support

For issues and questions:
- Check the error response details
- Verify your Replicate API token
- Ensure image file is valid and not corrupted
- Try reducing image size or complexity
- For Flux model: ensure aspect ratio format is correct ("width:height")