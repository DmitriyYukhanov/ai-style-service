using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using ArMasker.AiStyleService.Client.Services.Rest.Data;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Abstract;
using ArMasker.AiStyleService.Client.Services.Rest.Requests.Params;
using UnityEngine;
using UnityEngine.Scripting;

namespace ArMasker.AiStyleService.Client.Services.Rest.Requests
{
    public sealed class FluxStyleRequest : BasicRequest<FluxStyleParams, TextureResponse>, IMultipartRequest
    {
        protected override string FunctionName => "api/style-flux";
        
        public override string AcceptContentType => "image/jpeg";
        public override HttpMethod Method => HttpMethod.Post;

        [Obfuscation(Exclude = true), Preserve]
        public override FluxStyleParams RequestBody { get; }

        public FluxStyleRequest(FluxStyleParams requestParams)
        {
            RequestBody = requestParams;
        }
        
        public void AddFormData(WWWForm form)
        {
            // Validate input texture
            if (RequestBody.InputTexture == null)
            {
                throw new System.ArgumentNullException(nameof(RequestBody.InputTexture), "Input texture cannot be null");
            }
            
            if (!RequestBody.InputTexture.isReadable)
            {
                Debug.LogWarning("Input texture is not readable. This may cause issues. Ensure 'Read/Write Enabled' is checked in texture import settings.");
            }
            
            // Convert texture to PNG bytes
            byte[] textureBytes;
            try
            {
                textureBytes = RequestBody.InputTexture.EncodeToPNG();
                Debug.Log($"📷 Encoded texture: {RequestBody.InputTexture.width}x{RequestBody.InputTexture.height}, {textureBytes.Length} bytes");
            }
            catch (System.Exception ex)
            {
                throw new System.InvalidOperationException($"Failed to encode texture to PNG: {ex.Message}", ex);
            }
            
            form.AddBinaryData("file", textureBytes, "image.png", "image/png");
            
            // Add text parameters
            form.AddField("prompt", RequestBody.Prompt);
            form.AddField("aspect_ratio", RequestBody.AspectRatio);
                
            // Debug logging to verify values
            Debug.Log($"📝 Flux form data being sent:");
            Debug.Log($"   - prompt: '{RequestBody.Prompt}'");
            Debug.Log($"   - aspect_ratio: '{RequestBody.AspectRatio}'");
            Debug.Log($"   - file size: {textureBytes.Length} bytes");
        }
    }
} 