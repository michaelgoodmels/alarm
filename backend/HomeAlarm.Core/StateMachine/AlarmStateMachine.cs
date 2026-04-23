using HomeAlarm.Core.Abstractions;
using HomeAlarm.Core.Domain;
using HomeAlarm.Core.Events;
using Microsoft.Extensions.Logging;

namespace HomeAlarm.Core.StateMachine;

/// <summary>
/// Herzstueck der Alarmanlage.
///
/// Uebergaenge:
///  Disarmed  --arm(code)--> Armed
///  Armed     --disarm(code)--> Disarmed
///  Armed     --sensorTriggered--> Alarm
///  Alarm     --disarm(code)--> Disarmed
///
/// Thread-safe: jede Zustandsänderung geht durch ein SemaphoreSlim.
/// </summary>
public sealed class AlarmStateMachine
{
    private readonly IEventBus _bus;
    private readonly ILogger<AlarmStateMachine> _log;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AlarmState State { get; private set; } = AlarmState.Disarmed;

    public AlarmStateMachine(IEventBus bus, ILogger<AlarmStateMachine> log)
    {
        _bus = bus;
        _log = log;
    }

    public async Task<bool> ArmAsync(string userName, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (State != AlarmState.Disarmed)
            {
                _log.LogWarning("Arm rejected in state {State}", State);
                return false;
            }
            await TransitionAsync(AlarmState.Armed, $"Scharf geschaltet durch {userName}", ct).ConfigureAwait(false);
            return true;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DisarmAsync(string userName, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (State == AlarmState.Disarmed) return true;
            await TransitionAsync(AlarmState.Disarmed, $"Unscharf geschaltet durch {userName}", ct).ConfigureAwait(false);
            return true;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Wird vom AlarmService gerufen, wenn ein Sensor im Zustand Armed ausgeloest wurde.
    /// </summary>
    public async Task<bool> TriggerAlarmAsync(Zone zone, string sensorId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (State != AlarmState.Armed)
            {
                // Sensor-Detektion im Disarmed/Alarm wird nur geloggt.
                await _bus.PublishAsync(new AlarmEvent(
                    DateTimeOffset.UtcNow, AlarmEventType.SensorTriggered,
                    $"Sensor {sensorId} ausgeloest (ignoriert, Zustand={State})",
                    zone, sensorId), ct).ConfigureAwait(false);
                return false;
            }

            await TransitionAsync(AlarmState.Alarm,
                $"ALARM ausgeloest durch {sensorId} in Zone {zone}",
                ct).ConfigureAwait(false);
            return true;
        }
        finally { _gate.Release(); }
    }

    private async Task TransitionAsync(AlarmState next, string message, CancellationToken ct)
    {
        var prev = State;
        State = next;
        _log.LogInformation("State transition {Prev} -> {Next} | {Msg}", prev, next, message);
        await _bus.PublishAsync(new AlarmEvent(
            DateTimeOffset.UtcNow,
            AlarmEventType.StateChanged,
            message,
            StateBefore: prev,
            StateAfter: next), ct).ConfigureAwait(false);
    }
}
