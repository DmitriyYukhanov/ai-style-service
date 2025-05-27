using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ArMasker.AiStyleService.Client.Services.Rest.Config;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Abstract;
using UnityEngine;
using UnityEngine.Networking;

namespace ArMasker.AiStyleService.Client.Services.Rest
{
    public class RestClient
    {
        public IRestConfig config;
        
        public RestClient(IRestConfig config)
        {
            this.config = config;
        }
        
        public async Task<ApiResponse<TU>> Send<T, TU>(IRequest<T, TU> request) where T : class where TU : class
		{
			string responseString = null;
			request.ApplyConfig(config);
			
			var url = request.UrlPart;
			var statusCode = HttpStatusCode.SeeOther;
			var maxRetries = 3;
			var currentRetry = 0;
			
			while (currentRetry < maxRetries)
			{
				try
				{
					var postBody = await JsonTools.SerializeObjectAsync(request.RequestBody);

					var webRequest = request.Method == HttpMethod.Post
						? UnityWebRequest.Post(url, postBody, "application/json")
						: UnityWebRequest.Get(url);

#if DEBUG
					Debug.Log($"Request URL: {url}");
					Debug.Log($"Request Body:\n{postBody}");
#endif
					if (request.Headers is { Count: > 0 })
					{
						foreach (var defaultHeader in request.Headers)
						{
							Debug.Log($"Setting header: {defaultHeader.Key}:{defaultHeader.Value}");
							webRequest.SetRequestHeader(defaultHeader.Key, defaultHeader.Value);
						}
					}

					webRequest.timeout = 15;
					
					ApplyExtraConfigToWebRequest(webRequest);

#if DEBUG
					if (!string.IsNullOrWhiteSpace(request.OverrideResponseJson))
					{
						return await GetResponse(url, request, request.OverrideResponseJson, HttpStatusCode.OK);
					}
#endif
					await webRequest.SendWebRequest();
					
					// Store the status code
					statusCode = (HttpStatusCode)webRequest.responseCode;
					
					if (webRequest.result != UnityWebRequest.Result.Success)
					{
						// Check if we should retry based on the error
						if (ShouldRetry(webRequest.error, statusCode) && currentRetry < maxRetries - 1)
						{
							currentRetry++;
							Debug.LogWarning($"Request failed (attempt {currentRetry}/{maxRetries}): {webRequest.error} (code {statusCode}). Retrying...");
							continue;
						}

						var error = webRequest.error;
						var responseText = webRequest.downloadHandler?.text;
						
						Debug.LogError($"Request failed: {error}");
						Debug.LogError($"Response code: {statusCode}");
						Debug.LogError($"Response: {responseText}");
						return ApiResponse<TU>.FromError(ApiErrorKind.NetworkError, $"Couldn't get a response from {url}: {error}\n{responseText}", statusCode: statusCode);
					}
					
					responseString = webRequest.downloadHandler.text;
#if DEBUG				
					Debug.Log($"Request to {url} succeeded with status code {statusCode}");
					Debug.Log($"Raw response:\n{responseString}");
#endif
					break; // Success, exit retry loop
				}
				catch (Exception e)
				{
					if (ShouldRetry(e.Message, HttpStatusCode.InternalServerError) && currentRetry < maxRetries - 1)
					{
						currentRetry++;
						Debug.LogWarning($"Exception during request (attempt {currentRetry}/{maxRetries}): {e.Message}. Retrying...");
						continue;
					}
					
					Debug.LogException(e);
					Debug.LogError($"Exception during request to {url}: {e.Message}");
					return ApiResponse<TU>.FromError(ApiErrorKind.UnknownError, $"Exception while trying to get a response from {url}", e);
				}
			}
			
			return await GetResponse(url, request, responseString, statusCode);
		}

		private bool ShouldRetry(string error, HttpStatusCode statusCode)
		{
			// Retry on network-related issues
			if (error.Contains("timeout") || 
				error.Contains("connection") || 
				error.Contains("network") ||
				error.Contains("host") ||
				error.Contains("dns"))
			{
				return true;
			}
			
			// Don't retry on server errors (5xx) except for 503 Service Unavailable
			if ((int)statusCode > 500 && statusCode != HttpStatusCode.ServiceUnavailable)
			{
				return false;
			}
			
			// Don't retry on client errors (4xx)
			if ((int)statusCode >= 400 && (int)statusCode < 500)
			{
				return false;
			}
			
			return false;
		}

		private async Task<ApiResponse<TU>> GetResponse<T, TU>(string url, IRequest<T, TU> request, string responseString, HttpStatusCode statusCode) where T : class  where TU : class
		{
			if (string.IsNullOrEmpty(responseString))
			{
				// If the response is empty but the request was successful (status code 2xx),
				// create a default success response instead of treating it as an error
				bool isSuccessStatusCode = (int)statusCode >= 200 && (int)statusCode < 300;
				
				if (isSuccessStatusCode)
				{
					Debug.Log($"Empty response received from {url} with status code {statusCode}. Creating empty success response.");
					
					// Use the new EmptySuccess method to create a success response with null data
					return ApiResponse<TU>.EmptySuccess(statusCode);
				}
				else
				{
					// If status code is not in success range, treat as error
					return ApiResponse<TU>.FromError(ApiErrorKind.UnknownError,
						$"{url} response is empty with status code {statusCode}!", statusCode: statusCode);
				}
			}

			try
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				var responseData = await JsonTools.DeserializeObjectAsync<TU>(responseString);
				stopwatch.Stop();
				
				Debug.Log($"JSON deserialization took {stopwatch.ElapsedMilliseconds}ms for type {typeof(TU).Name}");
				
				if (responseData == null)
					throw new InvalidOperationException($"Deserialized to null {nameof(responseData)}!");

				return new ApiResponse<TU>(responseData, statusCode);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return ApiResponse<TU>.FromError(ApiErrorKind.JsonError, $"Couldn't deserialize {typeof(T).FullName} from {responseString}", e, statusCode);
			}
		}

		protected virtual void ApplyExtraConfigToWebRequest(UnityWebRequest webRequest) {}
    }
}