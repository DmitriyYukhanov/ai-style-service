using System.Threading.Tasks;
using ArMasker.AiStyleService.Client.Services.Rest;
using ArMasker.AiStyleService.Client.Services.Rest.Config;
using ArMasker.AiStyleService.Client.Services.Rest.Data;
using ArMasker.AiStyleService.Client.Services.Rest.Requests;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Abstract;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Params;
using UnityEngine;

namespace ArMasker.AiStyleService.Client
{
    public static class AiStyleServiceClient
    {
        private static RestClient client;

        public static void Initialize()
        {
            client = new RestClient(new DefaultRestConfig());
        }
        
        public static void Initialize(IRestConfig config)
        {
            client = new RestClient(config);
        }
        
        /// <summary>
        /// Apply artistic style to a texture with advanced parameters
        /// </summary>
        /// <param name="inputTexture">The texture to style</param>
        /// <param name="prompt">Style description</param>
        /// <param name="negativePrompt">What to avoid in the output</param>
        /// <param name="strength">Style application strength (0.0-1.0)</param>
        /// <param name="inferenceSteps">Number of denoising steps (1-100)</param>
        /// <param name="guidanceScale">How closely to follow the prompt (1.0-20.0)</param>
        /// <param name="seed">Random seed for reproducible results</param>
        /// <returns>Full API response with texture and metadata</returns>
        public static async Task<ApiResponse<TextureResponse>> StyleImageAsync(Texture2D inputTexture, string prompt,
            string negativePrompt = null, float strength = 0.5f, int inferenceSteps = 30, 
            float guidanceScale = 7.5f, int? seed = null)
        {
            if (!inputTexture)
            {
                Debug.LogError("Input texture cannot be null");
                return ApiResponse<TextureResponse>.FromError(ApiErrorKind.InvalidInput, "Input texture is null");
            }
            
            var request = new StyleImageRequest(new StyleImageParams(inputTexture, prompt, negativePrompt, 
                strength, inferenceSteps, guidanceScale, seed));
            return await SendRequest(request);
        }
        
        /// <summary>
        /// Apply artistic style using Flux model with aspect ratio control
        /// </summary>
        /// <param name="inputTexture">The texture to style</param>
        /// <param name="prompt">Style description (e.g., "Make this a 90s cartoon")</param>
        /// <param name="aspectRatio">Output aspect ratio (e.g., "16:9", "1:1", "9:16", "4:3", "3:4") or "match_input_image" to match the input image aspect ratio</param>
        /// <returns>Full API response with texture and metadata</returns>
        public static async Task<ApiResponse<TextureResponse>> StyleImageFluxAsync(Texture2D inputTexture, string prompt,
            string aspectRatio = "match_input_image")
        {
            if (!inputTexture)
            {
                Debug.LogError("Input texture cannot be null");
                return ApiResponse<TextureResponse>.FromError(ApiErrorKind.InvalidInput, "Input texture is null");
            }
            
            var request = new FluxStyleRequest(new FluxStyleParams(inputTexture, prompt, aspectRatio));
            return await SendRequest(request);
        }
        
        private static async Task<ApiResponse<TextureResponse>> SendRequest(StyleImageRequest request)
        {
            if (client == null)
            {
                Debug.LogError("AiStyleServiceClient not initialized. Call Initialize() first.");
                return ApiResponse<TextureResponse>.FromError(ApiErrorKind.UnknownError, "Client not initialized");
            }
            
            var result = await client.Send<StyleImageParams, TextureResponse>(request);
            
            if (!result.Success)
            {
                Debug.LogError($"Style transfer failed: {result.Error?.ToString()}");
            }
            
            return result;
        }
        
        private static async Task<ApiResponse<TextureResponse>> SendRequest(FluxStyleRequest request)
        {
            if (client == null)
            {
                Debug.LogError("AiStyleServiceClient not initialized. Call Initialize() first.");
                return ApiResponse<TextureResponse>.FromError(ApiErrorKind.UnknownError, "Client not initialized");
            }
            
            var result = await client.Send<FluxStyleParams, TextureResponse>(request);
            
            if (!result.Success)
            {
                Debug.LogError($"Flux style transfer failed: {result.Error?.ToString()}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Generic method to handle API calls and reduce code duplication
        /// </summary>
        private static async Task<ApiResponse<TResponse>> SendRequest<TRequest, TResponse>(
            IRequest<TRequest, TResponse> request) 
            where TRequest : class 
            where TResponse : class
        {
            var result = await client.Send(request);
            
            if (!result.Success)
            {
                Debug.LogError(result.Error?.ToString());
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Custom REST config for user-provided URLs
    /// </summary>
    public class CustomRestConfig : IRestConfig
    {
        public string BaseEndpoint { get; }
        
        public CustomRestConfig(string baseUrl)
        {
            BaseEndpoint = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        }
    }
}