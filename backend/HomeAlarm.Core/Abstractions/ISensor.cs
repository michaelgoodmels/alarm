using HomeAlarm.Core.Domain;

namespace HomeAlarm.Core.Abstractions;

/// <summary>
/// Ein Sensor publiziert Detection-Events, solange er aktiv ist.
/// </summary>
public interface ISensor : IAsyncDisposable
{
    SensorDescriptor Descriptor { get; }
    bool IsTriggered { get; }
    event EventHandler<SensorTriggeredEventArgs>? Triggered;
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}

public sealed class SensorTriggeredEventArgs : EventArgs
{
    public SensorDescriptor Descriptor { get; }
    public DateTimeOffset At { get; }
    public SensorTriggeredEventArgs(SensorDescriptor d, DateTimeOffset at) { Descriptor = d; At = at; }
}
