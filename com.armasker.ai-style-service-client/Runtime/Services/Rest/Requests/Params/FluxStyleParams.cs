using System.Reflection;
using UnityEngine;

namespace ArMasker.AiStyleService.Client.Services.Rest.Requests.Params
{
    [Obfuscation(Exclude = true)]
    public class FluxStyleParams
    {
        /// <summary>
        /// The input image texture to be styled
        /// </summary>
        public Texture2D InputTexture { get; set; }
        
        /// <summary>
        /// Style description (e.g., "Make this a 90s cartoon", "cyberpunk style")
        /// </summary>
        public string Prompt { get; set; }
        
        private string _aspectRatio = "match_input_image";
        /// <summary>
        /// Aspect ratio for the output image (e.g., "16:9", "1:1", "9:16", "4:3", "3:4") or "match_input_image" to match the input image aspect ratio
        /// </summary>
        public string AspectRatio 
        { 
            get => _aspectRatio;
            set => _aspectRatio = ValidateAspectRatio(value) ? value : "match_input_image";
        }

        public FluxStyleParams(Texture2D inputTexture, string prompt)
        {
            InputTexture = inputTexture;
            Prompt = prompt;
        }
        
        public FluxStyleParams(Texture2D inputTexture, string prompt, string aspectRatio = "16:9")
        {
            InputTexture = inputTexture;
            Prompt = prompt;
            AspectRatio = aspectRatio;
            
            if (aspectRatio != AspectRatio)
                Debug.LogWarning($"AspectRatio value '{aspectRatio}' was invalid, using default '16:9'");
        }
        
        private static bool ValidateAspectRatio(string aspectRatio)
        {
            if (string.IsNullOrEmpty(aspectRatio))
                return false;

            if (aspectRatio == "match_input_image")
                return true;

            var parts = aspectRatio.Split(':');
            if (parts.Length != 2)
                return false;

            return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
        }
    }
} 