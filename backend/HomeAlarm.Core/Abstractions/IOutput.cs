using HomeAlarm.Core.Domain;

namespace HomeAlarm.Core.Abstractions;

/// <summary>
/// Digitaler Ausgang (Relais). Siren oder Alarmgeber.
/// </summary>
public interface IOutput : IAsyncDisposable
{
    OutputDescriptor Descriptor { get; }
    bool IsActive { get; }
    Task ActivateAsync(CancellationToken ct = default);
    Task DeactivateAsync(CancellationToken ct = default);
}
