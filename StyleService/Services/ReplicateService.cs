using System.Net.Http.Headers;
using System.Text.Json;

namespace StyleService.Services;

public class ReplicateService : IReplicateService
{
    private readonly HttpClient _httpClient;
    private const string NEGATIVE_PROMPT = "blurry, low quality, low resolution, out of focus, bad anatomy, extra limbs, poorly drawn face, deformed eyes, unbalanced lighting, noisy, jpeg artifacts, double face, mutated hands, grainy, text, watermark";

    public ReplicateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> ProcessStyleTransferAsync(string imageDataUri, string prompt, string negativePrompt, 
        float strength, int inferenceSteps, float guidanceScale, int? seed)
    {
        var inputObject = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["image"] = imageDataUri,
            ["prompt_strength"] = strength,
            ["num_inference_steps"] = inferenceSteps,
            ["guidance_scale"] = guidanceScale,
            ["negative_prompt"] = negativePrompt ?? NEGATIVE_PROMPT
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

        return await ProcessReplicateRequestAsync(requestBody, "style transfer");
    }

    public async Task<byte[]> ProcessFluxStyleTransferAsync(string imageDataUri, string prompt, string aspectRatio)
    {
        var inputObject = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["input_image"] = imageDataUri,
            ["aspect_ratio"] = aspectRatio
        };

        var requestBody = new
        {
            version = "black-forest-labs/flux-kontext-pro",
            input = inputObject
        };

        return await ProcessReplicateRequestAsync(requestBody, "flux style transfer");
    }

    private async Task<byte[]> ProcessReplicateRequestAsync(object requestBody, string operationType)
    {
        var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        Console.WriteLine($"Request payload size: {jsonContent.Length} characters");

        var content = new StringContent(jsonContent);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        var response = await _httpClient.PostAsync("https://api.replicate.com/v1/predictions", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Replicate API Error ({response.StatusCode}): {errorContent}");
            throw new HttpRequestException($"Replicate API Error: {errorContent}");
        }

        using var respStream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(respStream);
        
        // Check if the response has the expected structure
        if (!doc.RootElement.TryGetProperty("urls", out var urlsProperty) ||
            !urlsProperty.TryGetProperty("get", out var getUrlProperty))
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Unexpected response structure: {responseContent}");
            throw new InvalidOperationException("Unexpected response structure from Replicate API");
        }

        var predictionUrl = getUrlProperty.GetString();
        if (string.IsNullOrEmpty(predictionUrl))
        {
            throw new InvalidOperationException("Prediction URL is null or empty");
        }
        
        Console.WriteLine($"Prediction URL: {predictionUrl}");

        return await PollForCompletionAsync(predictionUrl, operationType);
    }

    private async Task<byte[]> PollForCompletionAsync(string predictionUrl, string operationType)
    {
        string? outputUrl = null;
        int maxWaitTime = 180; // 3 minutes
        int pollInterval = 500; // Start with 500ms
        int maxPollInterval = 3000; // Max 3 seconds between polls
        
        for (int i = 0; i < maxWaitTime; i++)
        {
            await Task.Delay(pollInterval);
            var pollResponse = await _httpClient.GetAsync(predictionUrl);
            pollResponse.EnsureSuccessStatusCode();
            
            using var pollStream = await pollResponse.Content.ReadAsStreamAsync();
            using var pollDoc = await JsonDocument.ParseAsync(pollStream);
            
            var status = pollDoc.RootElement.GetProperty("status").GetString();
            
            // Log progress with more detail
            if (pollDoc.RootElement.TryGetProperty("logs", out var logsProperty))
            {
                var logs = logsProperty.GetString();
                if (!string.IsNullOrEmpty(logs))
                {
                    var lastLogLine = logs.Split('\n').LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
                    if (!string.IsNullOrEmpty(lastLogLine))
                    {
                        Console.WriteLine($"Status: {status} - {lastLogLine}");
                    }
                    else
                    {
                        Console.WriteLine($"Status: {status}");
                    }
                }
                else
                {
                    Console.WriteLine($"Status: {status}");
                }
            }
            else
            {
                Console.WriteLine($"Status: {status}");
            }
            
            if (status == "succeeded")
            {
                if (pollDoc.RootElement.TryGetProperty("output", out var outputProperty))
                {
                    // Handle different output formats
                    if (outputProperty.ValueKind == JsonValueKind.Array && outputProperty.GetArrayLength() > 0)
                    {
                        outputUrl = outputProperty[0].GetString();
                    }
                    else if (outputProperty.ValueKind == JsonValueKind.String)
                    {
                        outputUrl = outputProperty.GetString();
                    }
                    
                    if (outputUrl != null)
                        break;
                }
            }
            else if (status == "failed")
            {
                var error = pollDoc.RootElement.TryGetProperty("error", out var errorProp) 
                    ? errorProp.GetString() 
                    : "Unknown error";
                Console.WriteLine($"Prediction failed: {error}");
                throw new InvalidOperationException($"Prediction failed: {error}");
            }
            else if (status == "canceled")
            {
                Console.WriteLine("Prediction was canceled");
                throw new InvalidOperationException("Prediction was canceled");
            }
            
            // Exponential backoff: gradually increase poll interval
            if (pollInterval < maxPollInterval)
            {
                pollInterval = Math.Min(pollInterval + 200, maxPollInterval);
            }
        }

        if (outputUrl == null)
        {
            Console.WriteLine($"Prediction timed out after {maxWaitTime} seconds");
            throw new TimeoutException($"Prediction timed out after {maxWaitTime} seconds. The model may be experiencing high demand.");
        }

        Console.WriteLine($"Output URL: {outputUrl}");
        return await _httpClient.GetByteArrayAsync(outputUrl);
    }
} 