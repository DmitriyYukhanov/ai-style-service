using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ArMasker.AiStyleService.Client.Services.Rest.Config;
using ArMasker.AiStyleService.Client.Services.Rest.Data;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Abstract;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

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
					UnityWebRequest webRequest;
					
					// Handle multipart form data for image uploads
					if (request is IMultipartRequest multipartRequest)
					{
						var form = new WWWForm();
						multipartRequest.AddFormData(form);
						webRequest = UnityWebRequest.Post(url, form);
						
						Debug.Log($"📤 Sending multipart request to: {url}");
						Debug.Log($"📤 Form fields count: {form.headers.Count}");
					}
					else
					{
						var postBody = await JsonTools.SerializeObjectAsync(request.RequestBody);
						webRequest = request.Method == HttpMethod.Post
							? UnityWebRequest.Post(url, postBody, "application/json")
							: UnityWebRequest.Get(url);
						
						Debug.Log($"📤 Sending JSON request to: {url}");
						Debug.Log($"📤 Request body length: {postBody?.Length ?? 0}");
					}

#if DEBUG
					Debug.Log($"Request URL: {url}");
#endif
					if (request.Headers is { Count: > 0 })
					{
						foreach (var defaultHeader in request.Headers)
						{
							Debug.Log($"Setting header: {defaultHeader.Key}:{defaultHeader.Value}");
							webRequest.SetRequestHeader(defaultHeader.Key, defaultHeader.Value);
						}
					}

					webRequest.timeout = 180; // Increased timeout for AI processing
					
					ApplyExtraConfigToWebRequest(webRequest);

#if DEBUG
					if (!string.IsNullOrWhiteSpace(request.OverrideResponseJson))
					{
						return await GetResponse(url, request, request.OverrideResponseJson, HttpStatusCode.OK);
					}
#endif
					
					// Fix: Use proper async operation
					var operation = webRequest.SendWebRequest();
					
					// Wait for completion
					while (!operation.isDone)
					{
						await Task.Yield();
					}
					
					// Store the status code
					statusCode = (HttpStatusCode)webRequest.responseCode;
					
					if (webRequest.result != UnityWebRequest.Result.Success)
					{
						// Check if we should retry based on the error
						if (ShouldRetry(webRequest.error, statusCode) && currentRetry < maxRetries - 1)
						{
							currentRetry++;
							Debug.LogWarning($"Request failed (attempt {currentRetry}/{maxRetries}): {webRequest.error} (code {statusCode}). Retrying...");
							webRequest.Dispose();
							continue;
						}

						var error = webRequest.error;
						var responseText = webRequest.downloadHandler?.text;
						
						Debug.LogError($"Request failed: {error}");
						Debug.LogError($"Response code: {statusCode}");
						Debug.LogError($"Response: {responseText}");
						
						var errorResponse = ApiResponse<TU>.FromError(ApiErrorKind.NetworkError, $"Couldn't get a response from {url}: {error}\n{responseText}", statusCode: statusCode);
						webRequest.Dispose();
						return errorResponse;
					}
					
					// Check response content type and size for debugging
					string contentType = webRequest.GetResponseHeader("Content-Type") ?? "unknown";
					long contentLength = long.TryParse(webRequest.GetResponseHeader("Content-Length"), out var length) ? length : 0;
					
					Debug.Log($"Response info - Status: {statusCode}, Content-Type: {contentType}, Content-Length: {contentLength}");
					
					// Handle binary response (images) - specifically for TextureResponse
					if (typeof(TU) == typeof(TextureResponse) || request.AcceptContentType.Contains("image"))
					{
						// Check if we actually received image data (be more flexible with content type detection)
						bool isImageResponse = contentType.StartsWith("image/") || 
						                      contentType.Contains("jpeg") || 
						                      contentType.Contains("jpg") || 
						                      contentType.Contains("png") ||
						                      contentLength > 1000; // Fallback: assume large response is image
						
						if (isImageResponse)
						{
							try
							{
								// Get raw bytes instead of using DownloadHandlerTexture.GetContent()
								byte[] imageBytes = webRequest.downloadHandler.data;
								Debug.Log($"📷 Received image data: {imageBytes.Length} bytes");
								
								// Save raw response for debugging (optional)
								#if DEBUG
								try
								{
									string debugPath = System.IO.Path.Combine(Application.persistentDataPath, $"debug_response_{System.DateTime.Now:yyyyMMdd_HHmmss}.jpg");
									System.IO.File.WriteAllBytes(debugPath, imageBytes);
									Debug.Log($"🔍 Raw response saved to: {debugPath}");
								}
								catch (System.Exception debugEx)
								{
									Debug.LogWarning($"Could not save debug file: {debugEx.Message}");
								}
								#endif
								
								// Create texture from raw bytes
								Texture2D texture = new Texture2D(2, 2); // Temporary size, will be replaced by LoadImage
								bool loaded = texture.LoadImage(imageBytes);
								
								if (!loaded)
								{
									Debug.LogError("Failed to load image data into texture");
									Object.DestroyImmediate(texture);
									webRequest.Dispose();
									return ApiResponse<TU>.FromError(ApiErrorKind.UnknownError, 
										"Failed to load image data into texture", statusCode: statusCode);
								}
								
								Debug.Log($"✅ Successfully created texture: {texture.width}x{texture.height}");
								webRequest.Dispose();
								
								// Create TextureResponse object
								if (typeof(TU) == typeof(TextureResponse))
								{
									var textureResponse = new TextureResponse { TextureData = texture };
									return new ApiResponse<TU>(textureResponse as TU, statusCode);
								}
								else
								{
									return new ApiResponse<TU>(texture as TU, statusCode);
								}
							}
							catch (System.Exception ex)
							{
								Debug.LogError($"Failed to create texture from response: {ex.Message}");
								Debug.LogException(ex);
								webRequest.Dispose();
								return ApiResponse<TU>.FromError(ApiErrorKind.UnknownError, 
									$"Failed to process image response: {ex.Message}", statusCode: statusCode);
							}
						}
						else
						{
							// Not an image response, probably an error - log the response
							string responseText = webRequest.downloadHandler?.text;
							Debug.LogWarning($"Expected image response but got Content-Type: {contentType}, Length: {contentLength}");
							Debug.LogWarning($"Response body: {responseText}");
							
							webRequest.Dispose();
							return ApiResponse<TU>.FromError(ApiErrorKind.ServerError, 
								$"Expected image response but received: {contentType}. Response: {responseText}", 
								statusCode: statusCode);
						}
					}
					
					// Only process as text/JSON if we didn't handle it as an image above
					responseString = webRequest.downloadHandler.text;
					webRequest.Dispose();
					
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
    
    // Interface for requests that need multipart form data
    public interface IMultipartRequest
    {
        void AddFormData(WWWForm form);
    }
}