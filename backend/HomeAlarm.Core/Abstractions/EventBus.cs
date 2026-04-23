using HomeAlarm.Core.Events;

namespace HomeAlarm.Core.Abstractions;

public sealed class EventBus : IEventBus
{
    public event Func<AlarmEvent, Task>? OnEvent;

    public async Task PublishAsync(AlarmEvent evt, CancellationToken ct = default)
    {
        var handler = OnEvent;
        if (handler is null) return;

        foreach (var sub in handler.GetInvocationList().Cast<Func<AlarmEvent, Task>>())
        {
            try
            {
                await sub(evt).ConfigureAwait(false);
            }
            catch
            {
                // Subscriber-Fehler duerfen den Bus nicht anhalten. Logging macht der jeweilige Subscriber.
            }
        }
    }
}
