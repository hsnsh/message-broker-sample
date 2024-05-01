using Microsoft.AspNetCore.Mvc;
using Multithread.Api.Application;

namespace Multithread.Api.Controllers;

[ApiController]
[Route("weather")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly ISampleAppService _sampleAppService;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, ISampleAppService sampleAppService)
    {
        _logger = logger;
        _sampleAppService = sampleAppService;
    }

    [HttpGet("data")]
    public async Task<IEnumerable<WeatherForecast>> Get(CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 1000; i++)
        {
            await _sampleAppService.InsertOperation(i, cancellationToken);
            Console.WriteLine("Published: {0}", i);
        }

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}