using Microsoft.AspNetCore.Mvc;
using RedisDemo2.Data;
using RedisDemo2.Entities;

namespace RedisDemo2.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;
    private readonly ApplicationDbContext _context;

    public DemoController(ILogger<DemoController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpPost("/InsertTelemetryJson")]
    public async Task<int> InsertTelemetryJson(Telemetry data)
    {
        await _context.Telemetries.AddAsync(data);
        await _context.SaveChangesAsync();

        return data.Id;
    }
}