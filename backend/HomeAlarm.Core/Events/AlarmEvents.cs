using HomeAlarm.Core.Domain;

namespace HomeAlarm.Core.Events;

public enum AlarmEventType
{
    StateChanged,
    SensorTriggered,
    KeypadInput,
    AuthSuccess,
    AuthFailure,
    OutputChanged,
    SystemInfo,
    SystemError
}

/// <summary>
/// Ereignis, das im Audit-Log landet und per SignalR an das UI gepusht wird.
/// </summary>
public sealed record AlarmEvent(
    DateTimeOffset Timestamp,
    AlarmEventType Type,
    string Message,
    Zone? Zone = null,
    string? SourceId = null,
    AlarmState? StateBefore = null,
    AlarmState? StateAfter = null);
