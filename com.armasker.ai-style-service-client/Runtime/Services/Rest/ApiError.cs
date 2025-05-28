namespace ArMasker.AiStyleService.Client.Services.Rest
{
    /// <summary>
    /// Represents kind of the error returned by the API service.
    /// </summary>
    public enum ApiErrorKind
    {
        /// <summary>
        /// There were no errors during the API call.
        /// </summary>
        NoError,
	
        /// <summary>
        /// Can happen when calling APIs which are not available without authorization.
        /// </summary>
        AuthError,

        /// <summary>
        /// Json deserialization failed.
        /// </summary>
        JsonError,

        /// <summary>
        /// No internet connection?
        /// </summary>
        NetworkError,
		
        /// <summary>
        /// Server return error code.
        /// </summary>
        ServerError,
        
        /// <summary>
        /// Something else went wrong.
        /// </summary>
        UnknownError,
        InvalidInput
    }
	
    /// <summary>
    /// Hold information about possible error returned by the API service.
    /// </summary>
    public class ApiError
    {
        /// <summary>
        /// Represents kind of the error.
        /// </summary>
        public ApiErrorKind Kind { get; internal set; } = ApiErrorKind.NoError;
        
        /// <summary>
        /// Contains additional text information about the error.
        /// </summary>
        public string ErrorMessage { get; internal set; }

        /// <summary>
        /// Does prints the error in a human-readable form.
        /// </summary>
        /// <returns>Error in 'Kind: message' format.</returns>
        public override string ToString()
        {
            var result = Kind.ToString();
            if (!string.IsNullOrEmpty(ErrorMessage))
                result += $": {ErrorMessage}";
            return result;
        }
    }
}