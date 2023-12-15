namespace Base.EventBus;

public record IntegrationEvent(Guid Id, DateTime CreationTime)
{
    public Guid Id { get; } = Id;
    public DateTime CreationTime { get; } = CreationTime;
}