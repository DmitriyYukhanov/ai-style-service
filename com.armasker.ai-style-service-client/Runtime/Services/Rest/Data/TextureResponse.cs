using UnityEngine;

namespace ArMasker.AiStyleService.Client.Services.Rest.Data
{
    public class TextureResponse
    {
        /// <summary>
        /// The styled image texture ready for use in Unity
        /// </summary>
        public Texture2D TextureData { get; set; }
    }
}