using HsnSoft.Base.Logging;
using Microsoft.AspNetCore.Mvc;
using NetCoreEventBus.Web.Order.Infra.Domain;

namespace NetCoreEventBus.Web.Order.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly IPersistentLogger _logger;
    private readonly IOrderGenericRepository<OrderEntity> _repository;

    public WeatherForecastController(IPersistentLogger logger, IOrderGenericRepository<OrderEntity> repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        _logger.LogDebug("DEBUG LOG");
        _logger.LogWarning("WARNING LOG");
        _logger.LogError("ERROR LOG");
        _logger.LogInformation("INFORMATION LOG");

        _logger.PersistentInfoLog(new TestPersistentLog { Title = "PERSISTENT INFO TITLE", Detail = "PERSISTENT INFO DETAIL" });
        _logger.PersistentErrorLog(new TestPersistentLog { Title = "PERSISTENT ERROR TITLE", Detail = "PERSISTENT ERROR DETAIL" });

        //var result = await _repository.GetFilterListAsync();


        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}

public class TestPersistentLog : IPersistentLog
{
    public string Title { get; set; }
    public string Detail { get; set; }
}