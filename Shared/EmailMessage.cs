using Base.EventBus;

namespace Shared;

public class EmailMessage : IntegrationEvent
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
}