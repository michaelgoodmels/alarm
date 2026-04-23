using HomeAlarm.Core.Abstractions;
using HomeAlarm.Core.Domain;
using HomeAlarm.Hardware.Gpio;
using Microsoft.Extensions.Logging;

namespace HomeAlarm.Hardware.Outputs;

/// <summary>
/// Schaltet einen Ausgang (Sirene/Alarmgeber) ueber ein Relais auf GPIO.
/// Falls dein Relais-Modul low-active ist: ActiveHigh=false im Descriptor setzen.
/// </summary>
public sealed class GpioRelayOutput : IOutput
{
    private readonly IGpioController _gpio;
    private readonly ILogger<GpioRelayOutput> _log;
    private bool _opened;

    public OutputDescriptor Descriptor { get; }
    public bool IsActive { get; private set; }

    public GpioRelayOutput(OutputDescriptor d, IGpioController gpio, ILogger<GpioRelayOutput> log)
    {
        Descriptor = d;
        _gpio = gpio;
        _log = log;
    }

    private void EnsureOpen()
    {
        if (_opened) return;
        // Beim Oeffnen sofort auf "inaktiv" setzen.
        var initialHigh = !Descriptor.ActiveHigh;
        _gpio.OpenOutput(Descriptor.GpioPin, initialHigh);
        _opened = true;
    }

    public Task ActivateAsync(CancellationToken ct = default)
    {
        EnsureOpen();
        _gpio.Write(Descriptor.GpioPin, Descriptor.ActiveHigh);
        IsActive = true;
        _log.LogWarning("Output {Id} AKTIV (pin {Pin})", Descriptor.Id, Descriptor.GpioPin);
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        EnsureOpen();
        _gpio.Write(Descriptor.GpioPin, !Descriptor.ActiveHigh);
        IsActive = false;
        _log.LogInformation("Output {Id} aus", Descriptor.Id);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        try { await DeactivateAsync(); } catch { /* ignore */ }
        if (_opened) _gpio.Close(Descriptor.GpioPin);
    }
}
