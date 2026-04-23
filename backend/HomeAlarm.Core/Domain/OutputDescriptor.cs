namespace HomeAlarm.Core.Domain;

public enum OutputKind
{
    Siren = 1,
    AlarmTransmitter = 2   // z.B. GSM-Dialer, stiller Alarm, externer Melder
}

public sealed record OutputDescriptor(
    string Id,
    string DisplayName,
    OutputKind Kind,
    int GpioPin,
    bool ActiveHigh = true);
