namespace Base.EventBus;

public class EventBusConfig
{
    public string ExchangeName { get; set; } = "Default";
    public string ClientName { get; set; } = "Default_ClientName";
    public int ConnectionRetryCount { get; set; } = 5;
    public string EventNamePrefix { get; set; } = "";
    public string EventNameSuffix { get; set; } = "IntegrationEvent";

    public bool DeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);

    public bool DeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);

    public string ConsumerName =>
        (string.IsNullOrWhiteSpace(ExchangeName) ? string.Empty : $"{ExchangeName}_") +
        (string.IsNullOrWhiteSpace(ClientName) ? string.Empty : $"{ClientName}");
}