using Newtonsoft.Json;

namespace GeneralLibrary.Base.Kafka.Converters;

public static class DefaultJsonOptions
{
    public static JsonSerializerSettings Get()
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new CustomeTimeSpanConverter(),
            }
        };
        return settings;
    }
}