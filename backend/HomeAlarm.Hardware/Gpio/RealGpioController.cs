using System.Device.Gpio;

namespace HomeAlarm.Hardware.Gpio;

/// <summary>
/// Produktive Implementierung. Nutzt System.Device.Gpio (libgpiod-Driver auf Linux ARM).
/// Nur auf Raspberry Pi instanziieren.
/// </summary>
public sealed class RealGpioController : IGpioController
{
    private readonly GpioController _ctrl;
    private readonly Dictionary<int, PinChangeEventHandler> _callbacks = new();

    public RealGpioController()
    {
        _ctrl = new GpioController();
    }

    public void OpenInput(int pin, bool pullUp)
    {
        var mode = pullUp ? PinMode.InputPullUp : PinMode.InputPullDown;
        _ctrl.OpenPin(pin, mode);
    }

    public void OpenOutput(int pin, bool initialHigh)
    {
        _ctrl.OpenPin(pin, PinMode.Output);
        _ctrl.Write(pin, initialHigh ? PinValue.High : PinValue.Low);
    }

    public bool Read(int pin) => _ctrl.Read(pin) == PinValue.High;

    public void Write(int pin, bool high) =>
        _ctrl.Write(pin, high ? PinValue.High : PinValue.Low);

    public void RegisterCallback(int pin, Action<int, bool> onChange)
    {
        PinChangeEventHandler handler = (sender, args) =>
            onChange(args.PinNumber, args.ChangeType == PinEventTypes.Rising);

        _callbacks[pin] = handler;
        _ctrl.RegisterCallbackForPinValueChangedEvent(
            pin, PinEventTypes.Rising | PinEventTypes.Falling, handler);
    }

    public void Close(int pin)
    {
        if (_callbacks.Remove(pin, out var handler))
        {
            try { _ctrl.UnregisterCallbackForPinValueChangedEvent(pin, handler); }
            catch { /* pin evtl. schon geschlossen */ }
        }
        if (_ctrl.IsPinOpen(pin)) _ctrl.ClosePin(pin);
    }

    public void Dispose() => _ctrl.Dispose();
}
