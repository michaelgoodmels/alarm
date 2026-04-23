namespace HomeAlarm.Core.Domain;

/// <summary>
/// 4x3 (oder 4x4) Matrix-Keypad. Zeilen sind Outputs (getrieben), Spalten Inputs mit Pull-Up.
/// </summary>
public sealed record KeypadDescriptor(
    string Id,
    string DisplayName,
    int[] RowPins,
    int[] ColPins,
    string[][] KeyMap);
