using System;
using System.Net;

namespace ArMasker.AiStyleService.Client.Services.Rest
{
    /// <summary>
    /// Holds API response information with possible error.
    /// </summary>
    /// <typeparam name="T">Expected data type to deserialize from the response.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// HTTP status code of the response.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }
        
        /// <summary>
        /// Shows if API call went successfully. Will be true if Data is not null and there are no errors,
        /// or if the status code indicates success (2xx range) even with null data.
        /// </summary>
        public bool Success => (Data != null && (Error == null || Error.Kind == ApiErrorKind.NoError)) || 
                              (IsSuccessStatusCode && Error == null);
        
        /// <summary>
        /// Indicates if the status code is in the success range (2xx).
        /// </summary>
        public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode < 300;
	
        /// <summary>
        /// Deserialized data from the response.
        /// </summary>
        public T Data { get; }
	
        /// <summary>
        /// Holds information about possible error returned by the REST service.
        /// </summary>
        /// Will have RestErrorKind.NoError Kind if there were no errors.
        public ApiError Error { get; private set; }

        internal ApiResponse(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Data = data;
            StatusCode = statusCode;
        }
	
        private ApiResponse(HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            StatusCode = statusCode;
        }
	
        internal static ApiResponse<T> FromError(ApiErrorKind kind, Exception e, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            return FromError(kind, e, statusCode);
        }

        internal static ApiResponse<T> FromError(ApiErrorKind kind, string errorMessage = null, Exception e = null, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            if (e != null && errorMessage != null)
                errorMessage += $"\n{e}";
            
            return new ApiResponse<T>(statusCode)
            {
                Error = new ApiError
                {
                    Kind = kind,
                    ErrorMessage = errorMessage
                }
            };
        }
        
        /// <summary>
        /// Creates a success response with null data but a success status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code (should be in 2xx range)</param>
        /// <returns>A success response with null data</returns>
        internal static ApiResponse<T> EmptySuccess(HttpStatusCode statusCode = HttpStatusCode.NoContent)
        {
            return new ApiResponse<T>(default(T), statusCode);
        }
    }
}