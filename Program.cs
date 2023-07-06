using Microsoft.EntityFrameworkCore;
using Workshop.RedisVsPostgresPerformance.Data;
using StackExchange.Redis;
using BenchmarkDotNet.Running;
using Workshop.RedisVsPostgresPerformance.Benchmark;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

var config = DefaultConfig.Instance.WithSummaryStyle(
    new SummaryStyle(null, false, SizeUnit.KB, TimeUnit.Millisecond)
);

config.BuildTimeout = TimeSpan.FromMinutes(10);

BenchmarkRunner.Run<RedisVsPostgresBenchmark>(config);