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

        public static void Initialize(IRestConfig config)
        {
            client = new RestClient(config);
        }
        
        public static async Task<ApiResponse<StyleImageResponse>> StyleImage(string prompt)
        {
            return await SendRequest(new StyleImageRequest(new StyleImageParams(prompt)));
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
}