using System.ComponentModel.DataAnnotations;
using HomeAlarm.Core.Domain;
using HomeAlarm.Core.Events;

namespace HomeAlarm.Data.Entities;

/// <summary>
/// Persistente Form eines AlarmEvent fuer Audit-Zwecke.
/// </summary>
public sealed class AlarmEventLog
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public AlarmEventType Type { get; set; }

    [MaxLength(512)]
    public string Message { get; set; } = "";

    public Zone? Zone { get; set; }

    [MaxLength(64)]
    public string? SourceId { get; set; }

    public AlarmState? StateBefore { get; set; }
    public AlarmState? StateAfter { get; set; }
}
