namespace HomeAlarm.Hardware.Gpio;

/// <summary>
/// Minimaler Wrapper um System.Device.Gpio.GpioController, damit wir in Tests mocken koennen.
/// </summary>
public interface IGpioController : IDisposable
{
    void OpenInput(int pin, bool pullUp);
    void OpenOutput(int pin, bool initialHigh);
    bool Read(int pin);
    void Write(int pin, bool high);
    void RegisterCallback(int pin, Action<int, bool> onChange);
    void Close(int pin);
}
