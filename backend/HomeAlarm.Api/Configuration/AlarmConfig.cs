namespace HomeAlarm.Api.Configuration;

public sealed class AlarmConfig
{
    /// <summary>"real" fuer Raspberry Pi, "mock" fuer Windows/Mac-Entwicklung.</summary>
    public string GpioMode { get; set; } = "mock";

    public int AlarmDurationSeconds { get; set; } = 180;

    public int MaxPinDigits { get; set; } = 8;
    public int PinEntryTimeoutSeconds { get; set; } = 10;

    public List<SensorConfig> Sensors { get; set; } = new();
    public List<OutputConfig> Outputs { get; set; } = new();
    public List<KeypadConfig> Keypads { get; set; } = new();
}

public sealed class SensorConfig
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Zone { get; set; } = "";        // "GroundFloor", "UpperFloor", "Perimeter"
    public string Kind { get; set; } = "";        // "PirMotion", "Radar"
    public int GpioPin { get; set; }
    public bool ActiveHigh { get; set; } = true;
}

public sealed class OutputConfig
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Kind { get; set; } = "";        // "Siren", "AlarmTransmitter"
    public int GpioPin { get; set; }
    public bool ActiveHigh { get; set; } = true;
}

public sealed class KeypadConfig
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int[] RowPins { get; set; } = Array.Empty<int>();
    public int[] ColPins { get; set; } = Array.Empty<int>();
    public string[][] KeyMap { get; set; } = Array.Empty<string[]>();
}
