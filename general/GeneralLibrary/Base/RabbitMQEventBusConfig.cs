namespace GeneralLibrary.Base;

public class RabbitMqEventBusConfig : EventBusConfig
{
    public ushort ConsumerMaxThreadCount { get; set; } = 5;
}