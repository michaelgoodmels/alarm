using HomeAlarm.Core.Abstractions;
using HomeAlarm.Core.Domain;
using HomeAlarm.Hardware.Gpio;
using Microsoft.Extensions.Logging;

namespace HomeAlarm.Hardware.Keypad;

/// <summary>
/// Treiber fuer eine klassische 4x3/4x4 Matrix-Tastatur.
///
/// Prinzip:
///  - Zeilen (Rows) = Outputs, werden reihum auf LOW gezogen (alle anderen HIGH).
///  - Spalten (Cols) = Inputs mit Pull-Up. Wird eine Taste gedrueckt,
///    verbindet sie die aktuell aktive Row (LOW) mit der zugehoerigen Col -> Col liest LOW.
///  - Ein Hintergrund-Task pollt alle ~30 ms. Das ist fuer Menschen mehr als schnell genug,
///    verursacht aber minimale CPU-Last.
/// </summary>
public sealed class MatrixKeypad : IKeypad
{
    private readonly IGpioController _gpio;
    private readonly ILogger<MatrixKeypad> _log;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private readonly bool[,] _pressed;

    public KeypadDescriptor Descriptor { get; }
    public event EventHandler<KeyPressedEventArgs>? KeyPressed;

    public MatrixKeypad(KeypadDescriptor d, IGpioController gpio, ILogger<MatrixKeypad> log)
    {
        Descriptor = d;
        _gpio = gpio;
        _log = log;
        _pressed = new bool[d.RowPins.Length, d.ColPins.Length];
    }

    public Task StartAsync(CancellationToken ct)
    {
        // Rows: Outputs, inaktiv = HIGH
        foreach (var row in Descriptor.RowPins)
            _gpio.OpenOutput(row, initialHigh: true);

        // Cols: Inputs mit Pull-Up, ruhend = HIGH
        foreach (var col in Descriptor.ColPins)
            _gpio.OpenInput(col, pullUp: true);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loop = Task.Run(() => ScanLoopAsync(_cts.Token), _cts.Token);
        _log.LogInformation("Keypad {Id} gestartet ({Rows}x{Cols})",
            Descriptor.Id, Descriptor.RowPins.Length, Descriptor.ColPins.Length);
        return Task.CompletedTask;
    }

    private async Task ScanLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            for (int r = 0; r < Descriptor.RowPins.Length; r++)
            {
                _gpio.Write(Descriptor.RowPins[r], false); // active row = LOW

                for (int c = 0; c < Descriptor.ColPins.Length; c++)
                {
                    var isLow = !_gpio.Read(Descriptor.ColPins[c]);
                    var wasPressed = _pressed[r, c];
                    if (isLow && !wasPressed)
                    {
                        _pressed[r, c] = true;
                        var key = Descriptor.KeyMap[r][c];
                        _log.LogDebug("Keypad {Id} key '{Key}' gedrueckt", Descriptor.Id, key);
                        KeyPressed?.Invoke(this, new KeyPressedEventArgs(Descriptor.Id, key));
                    }
                    else if (!isLow && wasPressed)
                    {
                        _pressed[r, c] = false;
                    }
                }

                _gpio.Write(Descriptor.RowPins[r], true); // row wieder HIGH
            }

            try { await Task.Delay(30, ct); } catch (TaskCanceledException) { break; }
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _cts?.Cancel();
        if (_loop is not null)
        {
            try { await _loop; } catch { /* ignore */ }
        }

        foreach (var row in Descriptor.RowPins) _gpio.Close(row);
        foreach (var col in Descriptor.ColPins) _gpio.Close(col);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _cts?.Dispose();
    }
}
