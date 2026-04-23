using HomeAlarm.Api.Configuration;
using HomeAlarm.Api.Hubs;
using HomeAlarm.Core.Abstractions;
using HomeAlarm.Core.Domain;
using HomeAlarm.Core.Events;
using HomeAlarm.Core.StateMachine;
using HomeAlarm.Data;
using HomeAlarm.Data.Entities;
using HomeAlarm.Hardware;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace HomeAlarm.Api.Services;

/// <summary>
/// Langlebiger Hintergrunddienst. Verdrahtet die Hardware mit der State Machine,
/// schaltet bei Alarm die Outputs und schiebt alle Events an:
///   a) EventLogService (MySQL)
///   b) SignalR-Clients (UI)
/// Bietet auch die Logik fuer Keypad-PIN-Eingabe.
/// </summary>
public sealed class AlarmService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly HardwareRegistry _hw;
    private readonly AlarmStateMachine _sm;
    private readonly IEventBus _bus;
    private readonly IHubContext<AlarmHub> _hub;
    private readonly EventLogService _log;
    private readonly AlarmConfig _cfg;
    private readonly ILogger<AlarmService> _logger;

    // PIN-Buffer pro Keypad-Id
    private readonly Dictionary<string, PinBuffer> _pinBuffers = new();
    // Laufender Alarm-Task (Auto-Reset nach Dauer)
    private CancellationTokenSource? _alarmCts;

    public AlarmService(
        IServiceProvider services,
        HardwareRegistry hw,
        AlarmStateMachine sm,
        IEventBus bus,
        IHubContext<AlarmHub> hub,
        EventLogService log,
        IOptions<AlarmConfig> cfg,
        ILogger<AlarmService> logger)
    {
        _services = services;
        _hw = hw;
        _sm = sm;
        _bus = bus;
        _hub = hub;
        _log = log;
        _cfg = cfg.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bus.OnEvent += OnEventAsync;

        foreach (var s in _hw.Sensors)
        {
            s.Triggered += OnSensorTriggered;
            await s.StartAsync(stoppingToken);
        }

        foreach (var k in _hw.Keypads)
        {
            k.KeyPressed += OnKeyPressed;
            await k.StartAsync(stoppingToken);
        }

        _logger.LogInformation("AlarmService gestartet. {S} Sensoren, {O} Outputs, {K} Keypads",
            _hw.Sensors.Count, _hw.Outputs.Count, _hw.Keypads.Count);

        await _bus.PublishAsync(new AlarmEvent(
            DateTimeOffset.UtcNow, AlarmEventType.SystemInfo,
            "Alarmsystem online"), stoppingToken);

        // Bis Cancellation schlafen.
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (TaskCanceledException) { }

        foreach (var s in _hw.Sensors) await s.StopAsync(CancellationToken.None);
        foreach (var k in _hw.Keypads) await k.StopAsync(CancellationToken.None);
        foreach (var o in _hw.Outputs) await o.DeactivateAsync(CancellationToken.None);
    }

    // -------- Sensor -> State Machine --------

    private async void OnSensorTriggered(object? _, SensorTriggeredEventArgs e)
    {
        try
        {
            await _bus.PublishAsync(new AlarmEvent(
                e.At, AlarmEventType.SensorTriggered,
                $"Sensor '{e.Descriptor.DisplayName}' erkannt", e.Descriptor.Zone, e.Descriptor.Id));

            var triggered = await _sm.TriggerAlarmAsync(e.Descriptor.Zone, e.Descriptor.Id);
            if (triggered) await EnterAlarmAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei Sensor-Event");
        }
    }

    // -------- Keypad -> PIN-Eingabe --------

    private async void OnKeyPressed(object? _, KeyPressedEventArgs e)
    {
        try
        {
            await _bus.PublishAsync(new AlarmEvent(
                DateTimeOffset.UtcNow, AlarmEventType.KeypadInput,
                $"Keypad {e.KeypadId}: Taste '{e.Key}'", SourceId: e.KeypadId));

            if (!_pinBuffers.TryGetValue(e.KeypadId, out var buf))
            {
                buf = new PinBuffer(TimeSpan.FromSeconds(_cfg.PinEntryTimeoutSeconds), _cfg.MaxPinDigits);
                _pinBuffers[e.KeypadId] = buf;
            }

            if (e.Key == "*") { buf.Clear(); return; }
            if (e.Key != "#")
            {
                buf.Append(e.Key);
                return;
            }

            // '#' = Enter -> PIN pruefen
            var pin = buf.Take();
            if (string.IsNullOrEmpty(pin)) return;

            using var scope = _services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await users.AuthenticateAsync(pin);
            if (user is null)
            {
                await _bus.PublishAsync(new AlarmEvent(
                    DateTimeOffset.UtcNow, AlarmEventType.AuthFailure,
                    $"Ungueltiger PIN an Keypad {e.KeypadId}", SourceId: e.KeypadId));
                return;
            }

            await _bus.PublishAsync(new AlarmEvent(
                DateTimeOffset.UtcNow, AlarmEventType.AuthSuccess,
                $"'{user.UserName}' authentifiziert an Keypad {e.KeypadId}", SourceId: e.KeypadId));

            if (_sm.State == AlarmState.Disarmed)
            {
                await _sm.ArmAsync(user.UserName);
            }
            else
            {
                await _sm.DisarmAsync(user.UserName);
                await ExitAlarmAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei Keypad-Eingabe");
        }
    }

    // -------- Output-Steuerung --------

    private async Task EnterAlarmAsync()
    {
        _alarmCts?.Cancel();
        _alarmCts = new CancellationTokenSource();
        var ct = _alarmCts.Token;

        foreach (var o in _hw.Outputs) await o.ActivateAsync(ct);
        await _bus.PublishAsync(new AlarmEvent(
            DateTimeOffset.UtcNow, AlarmEventType.OutputChanged,
            "Alle Sirenen/Alarmgeber aktiviert"));

        // Auto-Reset nach AlarmDurationSeconds
        _ = Task.Run(async () =>
        {
            try { await Task.Delay(TimeSpan.FromSeconds(_cfg.AlarmDurationSeconds), ct); }
            catch { return; }

            if (_sm.State == AlarmState.Alarm)
            {
                foreach (var o in _hw.Outputs) await o.DeactivateAsync();
                await _bus.PublishAsync(new AlarmEvent(
                    DateTimeOffset.UtcNow, AlarmEventType.OutputChanged,
                    "Max. Alarmdauer erreicht – Sirenen deaktiviert (Zustand bleibt Alarm)"));
            }
        });
    }

    private async Task ExitAlarmAsync()
    {
        _alarmCts?.Cancel();
        foreach (var o in _hw.Outputs) await o.DeactivateAsync();
        await _bus.PublishAsync(new AlarmEvent(
            DateTimeOffset.UtcNow, AlarmEventType.OutputChanged,
            "Alle Outputs deaktiviert"));
    }

    // -------- Event-Fanout (DB + SignalR) --------

    private async Task OnEventAsync(AlarmEvent evt)
    {
        try { await _log.AppendAsync(evt); }
        catch (Exception ex) { _logger.LogError(ex, "Event-Persistenz fehlgeschlagen"); }

        try
        {
            await _hub.Clients.All.SendAsync("eventOccurred", new
            {
                evt.Timestamp,
                type = evt.Type.ToString(),
                evt.Message,
                zone = evt.Zone?.ToString(),
                evt.SourceId,
                stateBefore = evt.StateBefore?.ToString(),
                stateAfter = evt.StateAfter?.ToString(),
                currentState = _sm.State.ToString()
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "SignalR-Broadcast fehlgeschlagen"); }
    }

    // -------- Public fuer Controller --------

    public Task<bool> ArmFromUiAsync(string pin)    => AuthAndRunAsync(pin, u => _sm.ArmAsync(u.UserName));
    public async Task<bool> DisarmFromUiAsync(string pin)
    {
        var ok = await AuthAndRunAsync(pin, u => _sm.DisarmAsync(u.UserName));
        if (ok) await ExitAlarmAsync();
        return ok;
    }

    private async Task<bool> AuthAndRunAsync(string pin, Func<User, Task<bool>> action)
    {
        using var scope = _services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserService>();
        var user = await users.AuthenticateAsync(pin);
        if (user is null)
        {
            await _bus.PublishAsync(new AlarmEvent(
                DateTimeOffset.UtcNow, AlarmEventType.AuthFailure, "Ungueltiger PIN (UI)"));
            return false;
        }
        await _bus.PublishAsync(new AlarmEvent(
            DateTimeOffset.UtcNow, AlarmEventType.AuthSuccess, $"'{user.UserName}' authentifiziert (UI)"));
        return await action(user);
    }

    public AlarmState CurrentState => _sm.State;
}

/// <summary>
/// Hilfsbuffer fuer die PIN-Eingabe auf einem Keypad. Loescht sich selbst
/// nach Timeout, damit halb eingegebene PINs nicht ewig im RAM leben.
/// </summary>
internal sealed class PinBuffer
{
    private readonly TimeSpan _timeout;
    private readonly int _max;
    private string _value = "";
    private DateTimeOffset _lastInput = DateTimeOffset.MinValue;

    public PinBuffer(TimeSpan timeout, int max) { _timeout = timeout; _max = max; }

    public void Append(string key)
    {
        if (DateTimeOffset.UtcNow - _lastInput > _timeout) _value = "";
        if (_value.Length < _max) _value += key;
        _lastInput = DateTimeOffset.UtcNow;
    }

    public string Take()
    {
        var v = _value; _value = ""; return v;
    }

    public void Clear() => _value = "";
}
