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

        for (int i = 0; i < numberOfRecords; i++)
        {
            await _context.Telemetries.AddAsync(data);
        }

        await _context.SaveChangesAsync();

        var elapsedMillisecond = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();
        sb.AppendLine($"Inserting {numberOfRecords} telemetry data to database took {elapsedMillisecond} milliseconds.");

        // ---------------------------------------

        sb.AppendLine($"Inserting {numberOfRecords} telemetry data to redis...");

        string telemetryAsJson = JsonSerializer.Serialize(data);
        stopWatch.Start();

        for (int i = 0; i < numberOfRecords; i++)
        {
            string key = $"telemetry:{i}";
            
            await _redisInMemoryDatabase.StringSetAsync(key, telemetryAsJson);
        }

        elapsedMillisecond = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();

        sb.AppendLine($"Inserting {numberOfRecords} telemetry data to redis took {elapsedMillisecond} milliseconds.");


        sb.AppendLine($"Reading {numberOfRecords} telemetry data from database...");

        stopWatch.Start();
        await _context.Telemetries.ToListAsync();
        elapsedMillisecond = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();
        
        sb.AppendLine($"Reading {numberOfRecords} telemetry data from database took {elapsedMillisecond} milliseconds.");

        // ---------------------------------------

        sb.AppendLine($"Reading {numberOfRecords} telemetry data from redis...");

        stopWatch.Start();
        for (int i = 0; i < numberOfRecords; i++)
        {
            string key = $"telemetry:{i}";
            await _redisInMemoryDatabase.StringGetAsync(key);
        }

        elapsedMillisecond = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset();

        sb.AppendLine($"Reading {numberOfRecords} telemetry data from redis took {elapsedMillisecond} milliseconds.");

        return sb.ToString();
    }
}