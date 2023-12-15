using Base.EventBus;

namespace Shared;

public class EmailMessageIntegrationEvent : BaseIntegrationEvent
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }

    public EmailMessageIntegrationEvent(Guid id, DateTime creationTime) : base(id, creationTime)
    {
    }
}