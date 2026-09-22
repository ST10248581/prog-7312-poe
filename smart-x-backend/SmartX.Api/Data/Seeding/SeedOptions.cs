namespace SmartX.Api.Data.Seeding;

/// <summary>
/// Controls how much demo data the seeders generate. Defaults are deliberately
/// heavy so the dashboard has realistic high-throughput telemetry to render.
/// </summary>
public class SeedOptions
{
    /// <summary>Fixed seed so every run produces the same data set.</summary>
    public int RandomSeed { get; set; } = 73_12;

    public int SensorCount { get; set; } = 40;

    /// <summary>How far back the generated telemetry history reaches.</summary>
    public int HistoryDays { get; set; } = 7;

    /// <summary>Gap between readings in a series, in minutes.</summary>
    public int ReadingIntervalMinutes { get; set; } = 10;

    /// <summary>Roughly what proportion of readings are pushed outside their threshold.</summary>
    public double AnomalyRate { get; set; } = 0.015;

    public int MinAttachmentsPerSensor { get; set; } = 1;
    public int MaxAttachmentsPerSensor { get; set; } = 4;

    /// <summary>Ingestion batches are generated per sensor per this many minutes.</summary>
    public int BatchIntervalMinutes { get; set; } = 60;

    public int EngagementUserCount { get; set; } = 8;

    /// <summary>How far back the back-filled command log reaches.</summary>
    public int CommandHistoryHours { get; set; } = 48;

    /// <summary>Average dispatch rate used to size the back-filled command log.</summary>
    public double CommandsPerMinute { get; set; } = 1.2;

    /// <summary>
    /// Newest-first cap on the live command log. The simulator adds to it on a
    /// timer, so without a cap a long-running process grows without bound.
    /// </summary>
    public int MaxCommandLogSize { get; set; } = 20_000;
}
