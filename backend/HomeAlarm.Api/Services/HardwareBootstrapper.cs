using HomeAlarm.Api.Configuration;
using HomeAlarm.Core.Abstractions;
using HomeAlarm.Core.Domain;
using HomeAlarm.Hardware;
using HomeAlarm.Hardware.Gpio;
using HomeAlarm.Hardware.Keypad;
using HomeAlarm.Hardware.Mock;
using HomeAlarm.Hardware.Outputs;
using HomeAlarm.Hardware.Sensors;
using Microsoft.Extensions.Options;

namespace HomeAlarm.Api.Services;

/// <summary>
/// Liest AlarmConfig und baut daraus konkrete Sensor-/Output-/Keypad-Instanzen.
/// Registriert sie dann in einer HardwareRegistry.
/// </summary>
public static class HardwareBootstrapper
{
    public static IServiceCollection AddHomeAlarmHardware(this IServiceCollection services)
    {
        services.AddSingleton<IGpioController>(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<AlarmConfig>>().Value;
            return cfg.GpioMode.Equals("real", StringComparison.OrdinalIgnoreCase)
                ? new RealGpioController()
                : new MockGpioController();
        });

        services.AddSingleton<HardwareRegistry>(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<AlarmConfig>>().Value;
            var gpio = sp.GetRequiredService<IGpioController>();
            var logFactory = sp.GetRequiredService<ILoggerFactory>();

            var sensors = cfg.Sensors.Select(sc =>
            {
                var desc = new SensorDescriptor(
                    sc.Id, sc.DisplayName,
                    Enum.Parse<Zone>(sc.Zone),
                    Enum.Parse<SensorKind>(sc.Kind),
                    sc.GpioPin, sc.ActiveHigh);
                return (ISensor)new GpioDigitalSensor(desc, gpio, logFactory.CreateLogger<GpioDigitalSensor>());
            }).ToList();

            var outputs = cfg.Outputs.Select(oc =>
            {
                var desc = new OutputDescriptor(
                    oc.Id, oc.DisplayName,
                    Enum.Parse<OutputKind>(oc.Kind),
                    oc.GpioPin, oc.ActiveHigh);
                return (IOutput)new GpioRelayOutput(desc, gpio, logFactory.CreateLogger<GpioRelayOutput>());
            }).ToList();

            var keypads = cfg.Keypads.Select(kc =>
            {
                var desc = new KeypadDescriptor(kc.Id, kc.DisplayName, kc.RowPins, kc.ColPins, kc.KeyMap);
                return (IKeypad)new MatrixKeypad(desc, gpio, logFactory.CreateLogger<MatrixKeypad>());
            }).ToList();

            return new HardwareRegistry(sensors, outputs, keypads);
        });

        return services;
    }
}
