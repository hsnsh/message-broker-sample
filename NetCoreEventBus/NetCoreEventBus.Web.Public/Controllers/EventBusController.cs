using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.AspNetCore.Mvc;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Public.Dtos;

namespace NetCoreEventBus.Web.Public.Controllers;

[ApiController]
[Route("api/event-bus")]
[Produces("application/json")]
public class EventBusController : Controller
{
    private readonly IEventBus _eventBus;

    public EventBusController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    /// Sends a message through the event bus. This route is here for testing purposes.
    /// </summary>
    /// <param name="input">Message to send.</param>
    /// <returns>Message sent confirmation.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<IActionResult> SendMessage([FromBody] TestDto input)
    {
        input ??= new TestDto();
        if (input.TestCount < 1) input.TestCount = 1;


        // var parent= JsonConvert.DeserializeObject<ParentMessageEnvelope>(JsonConvert.SerializeObject(@event));
        var parent = new ParentMessageEnvelope
        {
            CorrelationId = "bfb8ffd3a71a4b9e92d24e8b0e5f9957",
            UserId = "hasan",
            UserRoleUniqueName = "admin",
            Channel = "test-app",
            Producer = "PublicApiTest"
        };

        for (var i = 0; i < input.TestCount; i++)
        {
            await _eventBus.PublishAsync(new OrderStartedEto(Guid.Parse("4fe789ab-0652-4e7b-bd35-07019058081d"), i + 1), parentMessage: parent);
        }

        return Ok("Message sent.");
    }
}