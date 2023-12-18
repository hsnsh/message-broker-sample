namespace Base.EventBus.Kafka;

public class KafkaConnectionSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 9092;
}

public class KafkaEventBusSettings
{
    public string ConsumerIdentifier { get; set; } = "HsnSoft_ClientName";
    public int ConnectionRetryCount { get; set; } = 5;
    public string EventNamePrefix { get; set; } = "";
    public string EventNameSuffix { get; set; } = "IntegrationEvent";
}