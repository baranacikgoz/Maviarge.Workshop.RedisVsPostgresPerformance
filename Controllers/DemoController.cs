using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedisDemo2.Data;
using RedisDemo2.Entities;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;
using System.Diagnostics;
using System.Text;
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

    [HttpPost("MeasurePerformance")]
    public async Task<string> MeasurePerformanceAsync(Telemetry data)
    {
        int numberOfRecords = 10000;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Inserting {numberOfRecords} telemetry data to database...");

        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        // Bulk insert using AddRange
        var telemetryList = Enumerable.Repeat(data, numberOfRecords).ToList();
        await _context.Telemetries.AddRangeAsync(telemetryList);
        await _context.SaveChangesAsync();

        var elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();
        sb.AppendLine($"Inserting {numberOfRecords} telemetry data to database took {elapsedMilliseconds} milliseconds.");

        // ---------------------------------------

        sb.AppendLine($"Inserting {numberOfRecords} telemetry data to Redis...");

        string telemetryAsJson = JsonSerializer.Serialize(data);
        stopWatch.Start();

        var tasks = Enumerable.Range(0, numberOfRecords)
            .Select(i =>
            {
                string key = $"telemetry:{i}";
                return _redisInMemoryDatabase.StringSetAsync(key, telemetryAsJson);
            })
            .ToArray();

        await Task.WhenAll(tasks);

        elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();
        sb.AppendLine($"Inserting {numberOfRecords} telemetry data to Redis took {elapsedMilliseconds} milliseconds.");

        sb.AppendLine($"Reading {numberOfRecords} telemetry data from database...");

        stopWatch.Start();
        await _context.Telemetries.ToListAsync();
        elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();

        sb.AppendLine($"Reading {numberOfRecords} telemetry data from database took {elapsedMilliseconds} milliseconds.");

        // ---------------------------------------

        sb.AppendLine($"Reading {numberOfRecords} telemetry data from Redis...");

        stopWatch.Start();
        var tasks2 = Enumerable.Range(0, numberOfRecords)
            .Select(i =>
            {
                string key = $"telemetry:{i}";
                return _redisInMemoryDatabase.StringGetAsync(key);
            })
            .ToArray();

        await Task.WhenAll(tasks);

        elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();

        sb.AppendLine($"Reading {numberOfRecords} telemetry data from Redis took {elapsedMilliseconds} milliseconds.");

        return sb.ToString();
    }
}