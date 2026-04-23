namespace HomeAlarm.Core.Domain;

/// <summary>
/// Statische Beschreibung eines Sensors: Id, Zone, Art, GPIO-Pin.
/// Wird beim Start aus appsettings.json erzeugt.
/// </summary>
public sealed record SensorDescriptor(
    string Id,
    string DisplayName,
    Zone Zone,
    SensorKind Kind,
    int GpioPin,
    bool ActiveHigh = true);
