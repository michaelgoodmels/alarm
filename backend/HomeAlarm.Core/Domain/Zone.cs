namespace HomeAlarm.Core.Domain;

/// <summary>
/// Zone = Gruppe physikalisch zusammengehöriger Sensoren.
/// Für dein Eigenheim: Erdgeschoss (EG), Obergeschoss (OG), Perimeter (Umgebung).
/// </summary>
public enum Zone
{
    GroundFloor = 1,
    UpperFloor = 2,
    Perimeter = 3
}
