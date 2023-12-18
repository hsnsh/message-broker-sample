namespace Base.EventBus;

public class EventBusConfig
{
    public string ExchangeName2 { get; set; } = "Default";
    public string ClientName2 { get; set; } = "Default_ClientName";
    public int ConnectionRetryCount { get; set; } = 5;
    public string EventNamePrefix { get; set; } = "";
    public string EventNameSuffix { get; set; } = "IntegrationEvent";

    public bool DeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);

    public bool DeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);

    public string ConsumerName =>
        (string.IsNullOrWhiteSpace(ExchangeName2) ? string.Empty : $"{ExchangeName2}_") +
        (string.IsNullOrWhiteSpace(ClientName2) ? string.Empty : $"{ClientName2}");
}