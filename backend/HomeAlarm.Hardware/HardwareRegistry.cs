using HomeAlarm.Core.Abstractions;

namespace HomeAlarm.Hardware;

/// <summary>
/// Laufzeit-Verzeichnis aller aktiven Sensoren, Outputs und Keypads.
/// Der AlarmService holt sich hier die Instanzen fuer Broadcast-Kommandos
/// (z.B. alle Sirenen aktivieren).
/// </summary>
public sealed class HardwareRegistry
{
    public IReadOnlyList<ISensor> Sensors { get; }
    public IReadOnlyList<IOutput> Outputs { get; }
    public IReadOnlyList<IKeypad> Keypads { get; }

    public HardwareRegistry(
        IEnumerable<ISensor> sensors,
        IEnumerable<IOutput> outputs,
        IEnumerable<IKeypad> keypads)
    {
        Sensors = sensors.ToList();
        Outputs = outputs.ToList();
        Keypads = keypads.ToList();
    }
}
