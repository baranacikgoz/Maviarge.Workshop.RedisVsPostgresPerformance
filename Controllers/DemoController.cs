using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedisDemo2.Data;
using RedisDemo2.Entities;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace RedisDemo2.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly IDatabase _redisInMemoryDatabase;

    public DemoController(ILogger<DemoController> logger, ApplicationDbContext context, IConnectionMultiplexer redisConnection)
    {
        _logger = logger;
        _context = context;
        _redisConnection = redisConnection;
        _redisInMemoryDatabase = _redisConnection.GetDatabase();
    }

    [HttpPost("InsertTelemetryJson")]
    public async Task<int> InsertTelemetryJson(Telemetry data)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        await _context.Telemetries.AddAsync(data);
        await _context.SaveChangesAsync();

        var elapsedMillisecond = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();
        _logger.LogInformation($"Telemetry kaydı atmak şu kadar sürdü: {elapsedMillisecond}");


        // ---------------------------------------

        string telemetryId = data.Id.ToString();
        var telemetryAsJson = JsonSerializer.Serialize(data);

        stopWatch.Start();
        await _redisInMemoryDatabase.SetAddAsync(telemetryId, telemetryAsJson);
        elapsedMillisecond = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();
        _logger.LogInformation($"Telemetry kaydını redis'e atmak şu kadar sürdü: {elapsedMillisecond}");

        return data.Id;
    }

    [HttpGet("GetTelemetryData")]
    public async Task<List<Telemetry>> GetTelemetryJson()
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        List<Telemetry> telemetryList = await _context.Telemetries.ToListAsync();
        //var telemetry1 = await _context.Telemetries.FirstOrDefaultAsync(t => t.Id == 1);
        await _context.SaveChangesAsync();

        var elapsedMillisecond = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();
        _logger.LogInformation($"Telemetry kaydını okumak şu kadar sürdü: {elapsedMillisecond}");



        return telemetryList;
    }
}