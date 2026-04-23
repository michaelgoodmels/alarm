using HomeAlarm.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAlarm.Data;

public sealed class AlarmDbContext : DbContext
{
    public AlarmDbContext(DbContextOptions<AlarmDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AlarmEventLog> AlarmEvents => Set<AlarmEventLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
        });

        b.Entity<AlarmEventLog>(e =>
        {
            e.HasIndex(x => x.Timestamp);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Zone).HasConversion<string?>().HasMaxLength(32);
            e.Property(x => x.StateBefore).HasConversion<string?>().HasMaxLength(32);
            e.Property(x => x.StateAfter).HasConversion<string?>().HasMaxLength(32);
        });
    }
}
