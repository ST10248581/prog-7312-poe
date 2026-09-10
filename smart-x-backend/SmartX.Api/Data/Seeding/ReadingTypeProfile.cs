using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

/// <summary>
/// Shared description of what a reading type looks like in the real world.
/// Used by the threshold and telemetry seeders so the two stay consistent.
/// </summary>
public record ReadingTypeProfile(
    ReadingType ReadingType,
    string Unit,
    double BaseValue,
    double DailySwing,
    double Noise,
    double MinThreshold,
    double MaxThreshold,
    bool IsBoolean = false)
{
    private static readonly Dictionary<ReadingType, ReadingTypeProfile> Profiles = new()
    {
        [ReadingType.Temperature] = new(ReadingType.Temperature, "°C", 22.0, 4.0, 0.6, 16.0, 30.0),
        [ReadingType.Humidity] = new(ReadingType.Humidity, "%", 48.0, 10.0, 2.0, 30.0, 70.0),
        [ReadingType.Pressure] = new(ReadingType.Pressure, "kPa", 101.3, 0.8, 0.2, 98.0, 104.0),
        [ReadingType.Power] = new(ReadingType.Power, "kW", 1.8, 1.1, 0.25, 0.2, 4.0),
        [ReadingType.Vibration] = new(ReadingType.Vibration, "mm/s", 0.9, 0.4, 0.2, 0.0, 2.5),
        [ReadingType.Motion] = new(ReadingType.Motion, "state", 0, 0, 0, 0, 1, IsBoolean: true)
    };

    private static readonly Dictionary<SensorCategory, ReadingType[]> CategoryReadingTypes = new()
    {
        [SensorCategory.Environmental] = [ReadingType.Temperature, ReadingType.Humidity, ReadingType.Pressure],
        [SensorCategory.PowerConsumption] = [ReadingType.Power],
        [SensorCategory.Actuator] = [ReadingType.Power, ReadingType.Vibration],
        [SensorCategory.Motion] = [ReadingType.Motion],
        [SensorCategory.Connectivity] = [ReadingType.Temperature, ReadingType.Power]
    };

    public static ReadingTypeProfile For(ReadingType readingType) => Profiles[readingType];

    public static ReadingType[] ForCategory(SensorCategory category) => CategoryReadingTypes[category];
}
