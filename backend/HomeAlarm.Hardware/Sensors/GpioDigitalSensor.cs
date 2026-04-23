using HomeAlarm.Core.Abstractions;
using HomeAlarm.Core.Domain;
using HomeAlarm.Hardware.Gpio;
using Microsoft.Extensions.Logging;

namespace HomeAlarm.Hardware.Sensors;

/// <summary>
/// Generischer digitaler Sensor (PIR oder Radar) an einem GPIO-Pin.
/// Interrupt-/Callback-basiert: wir lassen uns bei Pin-Changes benachrichtigen.
/// Ein Minimal-Debounce verhindert Event-Floods.
/// </summary>
public sealed class GpioDigitalSensor : ISensor
{
    private readonly IGpioController _gpio;
    private readonly ILogger<GpioDigitalSensor> _log;
    private readonly TimeSpan _debounce;
    private DateTimeOffset _lastTrigger = DateTimeOffset.MinValue;
    private bool _started;

    public SensorDescriptor Descriptor { get; }
    public bool IsTriggered { get; private set; }
    public event EventHandler<SensorTriggeredEventArgs>? Triggered;

    public GpioDigitalSensor(SensorDescriptor d, IGpioController gpio, ILogger<GpioDigitalSensor> log, TimeSpan? debounce = null)
    {
        Descriptor = d;
        _gpio = gpio;
        _log = log;
        _debounce = debounce ?? TimeSpan.FromMilliseconds(250);
    }

    public Task StartAsync(CancellationToken ct)
    {
        if (_started) return Task.CompletedTask;
        _gpio.OpenInput(Descriptor.GpioPin, pullUp: !Descriptor.ActiveHigh);
        _gpio.RegisterCallback(Descriptor.GpioPin, OnChange);
        _started = true;
        _log.LogInformation("Sensor {Id} ({Kind}, pin {Pin}) gestartet", Descriptor.Id, Descriptor.Kind, Descriptor.GpioPin);
        return Task.CompletedTask;
    }

    private void OnChange(int _, bool isHigh)
    {
        var isActive = Descriptor.ActiveHigh ? isHigh : !isHigh;
        IsTriggered = isActive;
        if (!isActive) return;

        var now = DateTimeOffset.UtcNow;
        if (now - _lastTrigger < _debounce) return;
        _lastTrigger = now;

        _log.LogDebug("Sensor {Id} getriggert", Descriptor.Id);
        Triggered?.Invoke(this, new SensorTriggeredEventArgs(Descriptor, now));
    }

    public Task StopAsync(CancellationToken ct)
    {
        if (!_started) return Task.CompletedTask;
        _gpio.Close(Descriptor.GpioPin);
        _started = false;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }
}
