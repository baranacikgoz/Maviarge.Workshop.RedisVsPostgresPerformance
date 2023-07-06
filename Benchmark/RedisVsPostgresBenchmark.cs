using BenchmarkDotNet.Attributes;
using Bogus;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;
using Workshop.RedisVsPostgresPerformance.Data;
using Workshop.RedisVsPostgresPerformance.Entities;

namespace Workshop.RedisVsPostgresPerformance.Benchmark;

[MemoryDiagnoser(false)]
public class RedisVsPostgresBenchmark
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly IDatabase _redisInMemoryDatabase;
    private Telemetry _singleTelemetry;
    private ICollection<Telemetry> _80000Telemetry;
    private HashEntry[] _80000TelemetryAsHashEntries;

    public RedisVsPostgresBenchmark()
    {
        _context = new ApplicationDbContext();
        _redisConnection = ConnectionMultiplexer.Connect("localhost");
        _redisInMemoryDatabase = _redisConnection.GetDatabase();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        try
        {
            _redisInMemoryDatabase.Execute("FLUSHDB");
        }
        catch
        {
            // ignored
        }

        try
        {
            _context.Telemetries.ExecuteDelete();
        }
        catch
        {
            // ignored
        }
    }

    // ------------------------------------------------------------------------------------
    [IterationSetup(Targets = new[] { nameof(InsertSingleIntoDatabase), nameof(InsertSingleIntoRedisAsJson), nameof(InsertSingleIntoRedisAsHashEntry) })]
    public void IterationSetupForInsertSingle()
    {
        _singleTelemetry = GetFakeTelemetry(1).First();
    }

    [Benchmark]
    public async Task InsertSingleIntoDatabase()
    {
        await _context.Telemetries.AddAsync(_singleTelemetry);
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task InsertSingleIntoRedisAsJson()
    {
        string telemetryAsJson = JsonSerializer.Serialize(_singleTelemetry);
        await _redisInMemoryDatabase.HashSetAsync("telemetry", $"telemetry:{_singleTelemetry.Id}", telemetryAsJson);
    }

    [Benchmark]
    public async Task InsertSingleIntoRedisAsHashEntry()
    {
        HashEntry[] hashEntries = new HashEntry[]
        {
            new HashEntry($"telemetry:{_singleTelemetry.Id}", JsonSerializer.Serialize(_singleTelemetry))
        };
        await _redisInMemoryDatabase.HashSetAsync("telemetry", hashEntries);
    }

    // ------------------------------------------------------------------------------------

    [IterationSetup(Targets = new[] { nameof(Insert80000IntoDatabase), nameof(Insert80000IntoRedis) })]
    public void IterationSetupForInsert80000()
    {
        _80000Telemetry = GetFakeTelemetry(80000);
    }

    [Benchmark]
    public async Task Insert80000IntoDatabase()
    {
        await _context.Telemetries.AddRangeAsync(_80000Telemetry);
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task Insert80000IntoRedis()
    {
        HashEntry[] hashEntries = _80000Telemetry
            .Select(t => new HashEntry($"telemetry:{t.Id}", JsonSerializer.Serialize(t)))
            .ToArray();
        await _redisInMemoryDatabase.HashSetAsync("telemetry", hashEntries);
    }

    // ------------------------------------------------------------------------------------
    [IterationSetup(Targets = new[] { nameof(ReadSingleFromDatabase), nameof(ReadSingleFromRedis) })]
    public void IterationSetupForReadSingle()
    {
        _singleTelemetry = GetFakeTelemetry(1).First();

        _context.Telemetries.Add(_singleTelemetry);
        _context.SaveChanges();

        _redisInMemoryDatabase.HashSet("telemetry", $"telemetry:{_singleTelemetry.Id}", JsonSerializer.Serialize(_singleTelemetry));
    }

    [Benchmark]
    public async Task ReadSingleFromDatabase()
    {
        await _context.Telemetries.FindAsync(_singleTelemetry.Id);
    }

    [Benchmark]
    public async Task ReadSingleFromRedis()
    {
        await _redisInMemoryDatabase.HashGetAsync("telemetry", $"telemetry:{_singleTelemetry.Id}");
    }

    // ------------------------------------------------------------------------------------

    [IterationSetup(Targets = new[] { nameof(Read80000FromDatabase), nameof(Read80000FromRedisWithKeyList), nameof(Read80000FromRedisWithGetAll) })]
    public void IterationSetupForRead80000()
    {
        _80000Telemetry = GetFakeTelemetry(80000);

        _context.Telemetries.AddRange(_80000Telemetry);
        _context.SaveChanges();

        _80000TelemetryAsHashEntries = _80000Telemetry
            .Select(t => new HashEntry($"telemetry:{t.Id}", JsonSerializer.Serialize(t)))
            .ToArray();

        _redisInMemoryDatabase.HashSet("telemetry", _80000TelemetryAsHashEntries);
    }

    [Benchmark]
    public async Task Read80000FromDatabase()
    {
        await _context.Telemetries.ToListAsync();
    }

    [Benchmark]
    public async Task Read80000FromRedisWithKeyList()
    {
        var rawRedisValues = await _redisInMemoryDatabase.HashGetAsync("telemetry", _80000Telemetry.Select(t => (RedisValue)$"telemetry:{t.Id}").ToArray());
        var telemetryList = rawRedisValues.Select(rv => JsonSerializer.Deserialize<Telemetry>(rv!)).ToList();
    }

    [Benchmark]
    public async Task Read80000FromRedisWithGetAll()
    {
        var rawRedisValues = await _redisInMemoryDatabase.HashGetAllAsync("telemetry");
        var telemetryList = rawRedisValues.Select(rv => JsonSerializer.Deserialize<Telemetry>(rv.Value)).ToList();
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