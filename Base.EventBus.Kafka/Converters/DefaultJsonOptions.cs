using Newtonsoft.Json;

namespace Base.EventBus.Kafka.Converters;

public static class DefaultJsonOptions
{
    public static JsonSerializerSettings Get()
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new TimeSpanConverter(),
            }
        };
        return settings;
    }
}