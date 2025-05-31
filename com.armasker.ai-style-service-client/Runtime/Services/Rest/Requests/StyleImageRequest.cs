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
    public sealed class StyleImageRequest : BasicRequest<StyleImageParams, TextureResponse>, IMultipartRequest
    {
        protected override string FunctionName => "api/style";
        
        public override string AcceptContentType => "image/jpeg";
        public override HttpMethod Method => HttpMethod.Post;

        [Obfuscation(Exclude = true), Preserve]
        public override StyleImageParams RequestBody { get; }

        public StyleImageRequest(StyleImageParams requestParams)
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
                Debug.Log($"AI Input Texture: {RequestBody.InputTexture.width}x{RequestBody.InputTexture.height}, {textureBytes.Length} bytes,\n{RequestBody.InputTexture.name}");
            }
            catch (System.Exception ex)
            {
                throw new System.InvalidOperationException($"Failed to encode texture to PNG: {ex.Message}", ex);
            }
            
            form.AddBinaryData("file", textureBytes, "image.png", "image/png");
            
            // Add text parameters
            form.AddField("prompt", RequestBody.Prompt);
            
            if (!string.IsNullOrEmpty(RequestBody.NegativePrompt))
                form.AddField("negative_prompt", RequestBody.NegativePrompt);
                
            // Use invariant culture to ensure proper decimal formatting (dots, not commas)
            form.AddField("strength", RequestBody.Strength.ToString("F2", CultureInfo.InvariantCulture));
            form.AddField("inference_steps", RequestBody.InferenceSteps.ToString(CultureInfo.InvariantCulture));
            form.AddField("guidance_scale", RequestBody.GuidanceScale.ToString("F1", CultureInfo.InvariantCulture));
            
            if (RequestBody.Seed.HasValue)
                form.AddField("seed", RequestBody.Seed.Value.ToString(CultureInfo.InvariantCulture));
                
            // Debug logging to verify values
            Debug.Log($"prompt:\n'{RequestBody.Prompt}'");
            Debug.Log($"negative_prompt:\n'{RequestBody.NegativePrompt ?? "null"}'");
            Debug.Log($"strength: {RequestBody.Strength.ToString("F2", CultureInfo.InvariantCulture)}");
            Debug.Log($"inference_steps: {RequestBody.InferenceSteps}");
            Debug.Log($"guidance_scale: {RequestBody.GuidanceScale.ToString("F1", CultureInfo.InvariantCulture)}");
            Debug.Log($"seed: {(RequestBody.Seed?.ToString() ?? "null")}");
        }
    }
}