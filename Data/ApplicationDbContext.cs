using Microsoft.EntityFrameworkCore;
using Workshop.RedisVsPostgresPerformance.Entities;

namespace Workshop.RedisVsPostgresPerformance.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Telemetry> Telemetries => Set<Telemetry>();
}