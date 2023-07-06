using Microsoft.EntityFrameworkCore;
using Workshop.RedisVsPostgresPerformance.Entities;

namespace Workshop.RedisVsPostgresPerformance.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public DbSet<Telemetry> Telemetries => Set<Telemetry>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Server=localhost;Port=22222;Database=redisdemo;User Id=postgres;Password=postgres;Include Error Detail=true");
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        base.OnConfiguring(optionsBuilder);
    }
}