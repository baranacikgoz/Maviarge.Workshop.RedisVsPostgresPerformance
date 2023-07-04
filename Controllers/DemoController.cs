using Bogus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workshop.RedisVsPostgresPerformance.Data;
using Workshop.RedisVsPostgresPerformance.Entities;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Workshop.RedisVsPostgresPerformance.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly IDatabase _redisInMemoryDatabase;
    private readonly Stopwatch _stopwatch;

    public DemoController(ILogger<DemoController> logger, ApplicationDbContext context, IConnectionMultiplexer redisConnection)
    {
        _logger = logger;
        _context = context;
        _redisConnection = redisConnection;
        _redisInMemoryDatabase = _redisConnection.GetDatabase();
        _stopwatch = new Stopwatch();
    }

    [HttpGet("MeasurePerformance")]
    public async Task<string> MeasurePerformanceAsync(int numberOfRecordsToInsert)
    {
        var telemetryList = GetFakeTelemetry(numberOfRecordsToInsert);
        StringBuilder sb = new StringBuilder();

        var elapsedMilliseconds = await InsertMultipleIntoDatabase(telemetryList);
        sb.AppendLine($"Inserting {numberOfRecordsToInsert} telemetry data to Postgresql took {elapsedMilliseconds} milliseconds.");

        elapsedMilliseconds = await InsertMultipleIntoRedis(telemetryList);
        sb.AppendLine($"Inserting {numberOfRecordsToInsert} telemetry data to Redis took {elapsedMilliseconds} milliseconds.");

        elapsedMilliseconds = await InsertSingleIntoDatabase();
        sb.AppendLine($"Inserting single telemetry data to Postgresql took {elapsedMilliseconds} milliseconds.");

        elapsedMilliseconds = await InsertSingleIntoRedis();
        sb.AppendLine($"Inserting single telemetry data to Redis took {elapsedMilliseconds} milliseconds.");

        var randomInt = new Random().Next(1, numberOfRecordsToInsert);
        var singleTelemetry = telemetryList.ElementAt(randomInt);

        elapsedMilliseconds = await ReadSingleFromDatabase(singleTelemetry);
        sb.AppendLine($"Reading single telemetry data from Postgresql took {elapsedMilliseconds} milliseconds.");

        elapsedMilliseconds = await ReadSingleFromRedis(singleTelemetry);
        sb.AppendLine($"Reading single telemetry data from Redis took {elapsedMilliseconds} milliseconds.");

        (elapsedMilliseconds, var recordCount) = await ReadAllFromDatabase();
        sb.AppendLine($"Reading all ({recordCount}) telemetry data from Postgresql took {elapsedMilliseconds} milliseconds.");

        (elapsedMilliseconds, recordCount) = await ReadAllFromRedis();
        sb.AppendLine($"Reading all ({recordCount}) telemetry data from Redis took {elapsedMilliseconds} milliseconds.");

        return sb.ToString();
    }

    private async Task<long> InsertMultipleIntoDatabase(List<Telemetry> telemetryList)
    {
        _stopwatch.Start();
        await _context.Telemetries.AddRangeAsync(telemetryList);
        await _context.SaveChangesAsync();
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return elapsedMilliseconds;
    }

    private async Task<long> InsertMultipleIntoRedis(List<Telemetry> telemetryList)
    {
        HashEntry[] hashEntries = telemetryList
            .Select(t => new HashEntry($"telemetry:{t.Id}", JsonSerializer.Serialize(t)))
            .ToArray();
        _stopwatch.Start();
        await _redisInMemoryDatabase.HashSetAsync("telemetry", hashEntries);
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return elapsedMilliseconds;
    }

    private async Task<long> InsertSingleIntoDatabase()
    {
        var telemetry = GetFakeTelemetry(1).First();
        _stopwatch.Start();
        await _context.Telemetries.AddAsync(telemetry);
        await _context.SaveChangesAsync();
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return elapsedMilliseconds;
    }

    private async Task<long> InsertSingleIntoRedis()
    {
        var telemetry = GetFakeTelemetry(1).First();
        _stopwatch.Start();
        string telemetryAsJson = JsonSerializer.Serialize(telemetry);
        await _redisInMemoryDatabase.HashSetAsync("telemetry", $"telemetry:{telemetry.Id}", telemetryAsJson);
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return elapsedMilliseconds;
    }

    private async Task<long> ReadSingleFromDatabase(Telemetry singleTelemetry)
    {
        _stopwatch.Start();
        await _context.Telemetries.FindAsync(singleTelemetry.Id);
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return elapsedMilliseconds;
    }

    private async Task<long> ReadSingleFromRedis(Telemetry singleTelemetry)
    {
        _stopwatch.Start();
        var dataAsJson = await _redisInMemoryDatabase.HashGetAsync("telemetry", $"telemetry:{singleTelemetry.Id}");
        JsonSerializer.Deserialize<Telemetry>(dataAsJson!); // simulate deserialization
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return elapsedMilliseconds;
    }

    private async Task<(long elapsedTime, int recordCount)> ReadAllFromDatabase()
    {
        _stopwatch.Start();
        var allData = await _context.Telemetries.ToListAsync();
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return (elapsedMilliseconds, allData.Count);
    }

    private async Task<(long elapsedTime, int recordCount)> ReadAllFromRedis()
    {
        _stopwatch.Start();
        HashEntry[] hashEntries = await _redisInMemoryDatabase.HashGetAllAsync("telemetry");
        var telemetryList = hashEntries.Aggregate(new List<Telemetry>(), (list, hashEntry) =>
        {
            var telemetry = JsonSerializer.Deserialize<Telemetry>(hashEntry.Value!);
            list.Add(telemetry!);
            return list;
        });
        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        return (elapsedMilliseconds, telemetryList.Count);
    }

    private List<Telemetry> GetFakeTelemetry(int generateLength)
    {
        var telemetryFaker = new Faker<Telemetry>()
            .RuleFor(t => t.Id, f => Guid.NewGuid())
            .RuleFor(t => t.DeviceIMEI, f => f.Random.AlphaNumeric(15))
            .RuleFor(t => t.DeviceIP, f => f.Internet.Ip())
            .RuleFor(t => t.LocationX, f => f.Random.Float(-180, 180))
            .RuleFor(t => t.LocationY, f => f.Random.Float(-90, 90))
            .RuleFor(t => t.Altitude, f => f.Random.Short())
            .RuleFor(t => t.Angle, f => f.Random.Short())
            .RuleFor(t => t.Satellites, f => f.Random.Byte(1, 32))
            .RuleFor(t => t.Speed, f => f.Random.Short())
            .RuleFor(t => t.BatteryLevel, f => f.Random.Long())
            .RuleFor(t => t.BatteryCharging, f => f.Random.Long())
            .RuleFor(t => t.ErrorCode, f => f.Random.Long())
            .RuleFor(t => t.IgnitionStatus, f => f.Random.Long())
            .RuleFor(t => t.Movement, f => f.Random.Long())
            .RuleFor(t => t.GSMSignalStrength, f => f.Random.Long())
            .RuleFor(t => t.SleepMode, f => f.Random.Long())
            .RuleFor(t => t.GNSFStatus, f => f.Random.Long())
            .RuleFor(t => t.AxisX, f => f.Random.Long())
            .RuleFor(t => t.AxisY, f => f.Random.Long())
            .RuleFor(t => t.AxisZ, f => f.Random.Long())
            .RuleFor(t => t.LicencePlate, f => f.Vehicle.Vin())
            .RuleFor(t => t.BlueToothLockStatus, f => f.Random.Long())
            .RuleFor(t => t.BlueToothLockBatteryLevel, f => f.Random.Long())
            .RuleFor(t => t.TelemetryDateTime, f => f.Date.Recent())
            .RuleFor(t => t.RecievedAt, f => f.Date.Recent())
            .RuleFor(t => t.AlarmForBeingPushedInLockMode, f => f.Random.Long())
            .RuleFor(t => t.TamperDetectionEvent, f => f.Random.Long())
            .RuleFor(t => t.Unplug, f => f.Random.Long())
            .RuleFor(t => t.FallDown, f => f.Random.Long())
            .RuleFor(t => t.CurrentOperationMode, f => f.Random.Long());

        return telemetryFaker.Generate(generateLength);
    }
}