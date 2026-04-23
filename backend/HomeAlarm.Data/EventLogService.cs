using HomeAlarm.Core.Events;
using HomeAlarm.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAlarm.Data;

public sealed class EventLogService
{
    private readonly IDbContextFactory<AlarmDbContext> _factory;

    public EventLogService(IDbContextFactory<AlarmDbContext> factory) { _factory = factory; }

    public async Task AppendAsync(AlarmEvent evt, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AlarmEvents.Add(new AlarmEventLog
        {
            Timestamp = evt.Timestamp,
            Type = evt.Type,
            Message = evt.Message,
            Zone = evt.Zone,
            SourceId = evt.SourceId,
            StateBefore = evt.StateBefore,
            StateAfter = evt.StateAfter
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AlarmEventLog>> GetRecentAsync(int take = 200, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AlarmEvents
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .ToListAsync(ct);
    }
}
