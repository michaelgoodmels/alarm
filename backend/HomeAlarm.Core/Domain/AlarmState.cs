namespace HomeAlarm.Core.Domain;

/// <summary>
/// Globaler Zustand der Alarmanlage.
/// </summary>
public enum AlarmState
{
    /// <summary>Unscharf – Sensor-Events werden nur protokolliert, aber nicht ausgelöst.</summary>
    Disarmed = 0,

    /// <summary>Scharf – jede Sensor-Detektion triggert einen Alarm.</summary>
    Armed = 1,

    /// <summary>Alarm ausgelöst – Sirenen/Alarmgeber aktiv, bis ein gültiger Code eingegeben wird.</summary>
    Alarm = 2
}
