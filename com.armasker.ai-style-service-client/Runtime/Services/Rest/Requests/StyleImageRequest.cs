using System.Reflection;
using ArMasker.AiStyleService.Client.Services.Rest.Data;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Abstract;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Params;
using UnityEngine.Scripting;

namespace ArMasker.AiStyleService.Client.Services.Rest.Requests
{
    public sealed class StyleImageRequest : BasicRequest<StyleImageParams, StyleImageResponse>
    {
        protected override string FunctionName => Config.BaseEndpoint + "api/style";

        [Obfuscation(Exclude = true), Preserve]
        public override StyleImageParams RequestBody { get; }

        public StyleImageRequest(StyleImageParams requestParams)
        {
            RequestBody = requestParams;
        }
    }
}