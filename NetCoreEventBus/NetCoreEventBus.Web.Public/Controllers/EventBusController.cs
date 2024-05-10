using Microsoft.AspNetCore.Mvc;
using NetCoreEventBus.Infra.EventBus.Bus;
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
    public IActionResult SendMessage([FromBody] TestDto input)
    {
        input ??= new TestDto();
        if (input.TestCount < 1) input.TestCount = 1;

        for (var i = 0; i < input.TestCount; i++)
        {
            _eventBus.Publish(new OrderStartedEto(Guid.NewGuid()));
        }

        return Ok("Message sent.");
    }
}