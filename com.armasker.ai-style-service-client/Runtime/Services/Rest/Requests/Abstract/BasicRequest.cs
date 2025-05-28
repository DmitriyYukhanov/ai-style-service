using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using ArMasker.AiStyleService.Client.Services.Rest.Config;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace ArMasker.AiStyleService.Client.Services.Rest.Requests.Abstract
{
    public interface IRequest { }

    /// <summary>
    /// Common interface for all REST requests.
    /// </summary>
    /// <typeparam name="TRequest">Request data, will be passed as "params" object.</typeparam>
    /// <typeparam name="TResult">Request result datatype. Used to properly serialize server reply.</typeparam>
    public interface IRequest<TRequest, TResult> : IRequest where TRequest : class where TResult : class
    {
        /// <summary>
        /// Request-specific URL part which is automatically joined with REST API Endpoint URL.
        /// </summary>
        [JsonIgnore] string UrlPart { get; }

        /// <summary>
        /// What method to use for this request.
        /// </summary>
        [JsonIgnore] HttpMethod Method { get; }
		
        [JsonIgnore] Dictionary<string, string> Headers { get; }
		
        [Preserve] TRequest RequestBody { get; }
        
#if DEBUG
        [Preserve] string OverrideResponseJson { get; }
#endif
		
        IRestConfig Config { get; }
        string AcceptContentType { get; }
		
        void ApplyConfig(IRestConfig config);
    }

    public abstract class BasicRequest<TRequest, TResult> : IRequest<TRequest, TResult>
        where TRequest : class where TResult : class
    {
        [JsonIgnore] public virtual string UrlPart => Config != null ? Config.BaseEndpoint + FunctionName : FunctionName;
        [JsonIgnore] public IRestConfig Config { get; private set; }
        [JsonIgnore] public virtual string AcceptContentType => "application/json";
        [JsonIgnore] public virtual Dictionary<string, string> Headers { get; protected set; }
        [JsonIgnore] public virtual HttpMethod Method => HttpMethod.Post;
        [JsonIgnore] protected abstract string FunctionName { get; }

        [Obfuscation(Exclude = true), Preserve]
        public abstract TRequest RequestBody { get; }

#if DEBUG
        [Obfuscation(Exclude = true), Preserve]
        public virtual string OverrideResponseJson { get; }
#endif

        public virtual void ApplyConfig(IRestConfig config)
        {
            Config = config;
        }
    }
}