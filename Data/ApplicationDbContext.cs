using Microsoft.EntityFrameworkCore;
using RedisDemo2.Entities;

namespace RedisDemo2.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Telemetry> Telemetries => Set<Telemetry>();
}