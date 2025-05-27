using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class JsonTools
{
    private static readonly Lazy<JsonSerializerSettings> LazySettings = new(CreateSettings);
    public static JsonSerializerSettings Settings => LazySettings.Value;

    public static HashSet<JsonConverter> Converters { get; private set; } = new();

    private static JsonSerializerSettings CreateSettings()
    {
        var settings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
        };

        if (Converters != null)
            settings.Converters = Converters.ToList();
			
        return settings;
    }

    public static bool AddConverter(JsonConverter converters)
    {
        return Converters.Add(converters);
    }
		
    public static async Task<string> SerializeObjectAsync(object input)
    {
        return await Task.Run(() => JsonConvert.SerializeObject(input, Settings));
    }
        
    public static string SerializeObject<T>(T input)
    {
        return JsonConvert.SerializeObject(input, Settings);
    }

    public static async Task<T> DeserializeObjectAsync<T>(string input) where T : class
    {
        return await Task.Run(() => JsonConvert.DeserializeObject<T>(input, Settings));
    }

    public static T DeserializeObject<T>(string input) where T : class
    {
        return JsonConvert.DeserializeObject<T>(input, Settings);
    }
}