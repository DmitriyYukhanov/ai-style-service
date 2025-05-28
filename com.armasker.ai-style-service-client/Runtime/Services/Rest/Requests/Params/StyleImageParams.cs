using System.Reflection;
using UnityEngine;

namespace ArMasker.AiStyleService.Client.Services.Rest.Requests.Params
{
    [Obfuscation(Exclude = true)]
    public class StyleImageParams
    {
        /// <summary>
        /// The input image texture to be styled
        /// </summary>
        public Texture2D InputTexture { get; set; }
        
        /// <summary>
        /// Style description (e.g., "anime style", "oil painting")
        /// </summary>
        public string Prompt { get; set; }
        
        /// <summary>
        /// What to avoid in the output (optional)
        /// </summary>
        public string NegativePrompt { get; set; }
        
        private float _strength = 0.5f;
        /// <summary>
        /// Style application strength (0.0-1.0, default: 0.5)
        /// </summary>
        public float Strength 
        { 
            get => _strength;
            set => _strength = Mathf.Clamp01(value);
        }
        
        private int _inferenceSteps = 30;
        /// <summary>
        /// Number of denoising steps (1-100, default: 30)
        /// </summary>
        public int InferenceSteps 
        { 
            get => _inferenceSteps;
            set => _inferenceSteps = Mathf.Clamp(value, 1, 100);
        }
        
        private float _guidanceScale = 7.5f;
        /// <summary>
        /// How closely to follow the prompt (1.0-20.0, default: 7.5)
        /// </summary>
        public float GuidanceScale 
        { 
            get => _guidanceScale;
            set => _guidanceScale = Mathf.Clamp(value, 1.0f, 20.0f);
        }
        
        /// <summary>
        /// Random seed for reproducible results (optional)
        /// </summary>
        public int? Seed { get; set; }

        public StyleImageParams(Texture2D inputTexture, string prompt)
        {
            InputTexture = inputTexture;
            Prompt = prompt;
        }
        
        public StyleImageParams(Texture2D inputTexture, string prompt, string negativePrompt = null, 
            float strength = 0.5f, int inferenceSteps = 30, float guidanceScale = 7.5f, int? seed = null)
        {
            InputTexture = inputTexture;
            Prompt = prompt;
            NegativePrompt = negativePrompt;
            Strength = strength;
            InferenceSteps = inferenceSteps;
            GuidanceScale = guidanceScale;
            Seed = seed;
            
            if (strength != Strength)
                Debug.LogWarning($"Strength value {strength} was clamped to {Strength}");
            if (inferenceSteps != InferenceSteps)
                Debug.LogWarning($"InferenceSteps value {inferenceSteps} was clamped to {InferenceSteps}");
            if (guidanceScale != GuidanceScale)
                Debug.LogWarning($"GuidanceScale value {guidanceScale} was clamped to {GuidanceScale}");
        }
    }
}