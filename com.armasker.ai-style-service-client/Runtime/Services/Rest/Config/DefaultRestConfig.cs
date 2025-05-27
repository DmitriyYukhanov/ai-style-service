namespace ArMasker.AiStyleService.Client.Services.Rest.Config
{
    public interface IRestConfig
    {
        public string BaseEndpoint { get; }
    }
    
    public class DefaultRestConfig : IRestConfig
    {
#if DEBUG
        public string BaseEndpoint => "https://ai-style-service.onrender.com/";
#else
        public string BaseEndpoint => "https://ai-style-service.onrender.com/";
#endif
    }
}