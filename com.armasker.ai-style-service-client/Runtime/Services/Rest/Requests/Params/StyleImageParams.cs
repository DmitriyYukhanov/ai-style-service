using System.Reflection;

namespace ArMasker.AiStyleService.Client.Services.Rest.Requests.Params
{
    [Obfuscation(Exclude = true)]
    public class StyleImageParams
    {
        public string Prompt { get; set; }

        public StyleImageParams(string prompt)
        {
            Prompt = prompt;
        }
    }
}