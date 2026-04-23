using HomeAlarm.Core.Events;

namespace HomeAlarm.Core.Abstractions;

/// <summary>
/// Sehr schlanker In-Process Event-Bus. Hardware-Layer und Api subscriben darauf.
/// </summary>
public interface IEventBus
{
    event Func<AlarmEvent, Task>? OnEvent;
    Task PublishAsync(AlarmEvent evt, CancellationToken ct = default);
}
