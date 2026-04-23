using HomeAlarm.Core.Domain;

namespace HomeAlarm.Core.Abstractions;

public interface IKeypad : IAsyncDisposable
{
    KeypadDescriptor Descriptor { get; }
    event EventHandler<KeyPressedEventArgs>? KeyPressed;
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}

public sealed class KeyPressedEventArgs : EventArgs
{
    public string KeypadId { get; }
    public string Key { get; }
    public KeyPressedEventArgs(string keypadId, string key) { KeypadId = keypadId; Key = key; }
}
