using System.Collections.Concurrent;
using HomeAlarm.Hardware.Gpio;

namespace HomeAlarm.Hardware.Mock;

/// <summary>
/// In-Memory-GPIO fuer Entwicklung auf Windows/Mac.
/// Kann von aussen (z.B. ueber einen Debug-Controller) manipuliert werden,
/// um Sensor-Events zu simulieren.
/// </summary>
public sealed class MockGpioController : IGpioController
{
    private readonly ConcurrentDictionary<int, bool> _pins = new();
    private readonly ConcurrentDictionary<int, Action<int, bool>> _callbacks = new();

    public void OpenInput(int pin, bool pullUp) => _pins[pin] = !pullUp;

    public void OpenOutput(int pin, bool initialHigh) => _pins[pin] = initialHigh;

    public bool Read(int pin) => _pins.GetValueOrDefault(pin, false);

    public void Write(int pin, bool high)
    {
        var changed = !_pins.TryGetValue(pin, out var prev) || prev != high;
        _pins[pin] = high;
        if (changed && _callbacks.TryGetValue(pin, out var cb)) cb(pin, high);
    }

    public void RegisterCallback(int pin, Action<int, bool> onChange) =>
        _callbacks[pin] = onChange;

    public void Close(int pin)
    {
        _pins.TryRemove(pin, out _);
        _callbacks.TryRemove(pin, out _);
    }

    /// <summary>
    /// Test-Helfer: simuliere extern einen Pin-Zustand (z.B. Sensor ausgeloest).
    /// </summary>
    public void Simulate(int pin, bool high) => Write(pin, high);

    public void Dispose() { }
}
