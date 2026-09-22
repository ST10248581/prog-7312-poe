namespace SmartX.Api.Data.Seeding;

/// <summary>
/// Runs the individual table seeders in dependency order at application start-up.
/// </summary>
public class DataSeeder : IDataSeeder
{
    private readonly ISensorProfileSeeder _sensorProfileSeeder;
    private readonly ISensorThresholdSeeder _sensorThresholdSeeder;
    private readonly ITelemetryReadingSeeder _telemetryReadingSeeder;
    private readonly IAlertSeeder _alertSeeder;
    private readonly ISensorAttachmentSeeder _sensorAttachmentSeeder;
    private readonly IIngestionBatchSeeder _ingestionBatchSeeder;
    private readonly IEngagementStateSeeder _engagementStateSeeder;
    private readonly ICommandSeeder _commandSeeder;
    private readonly ISmartXDataStore _store;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ISensorProfileSeeder sensorProfileSeeder,
        ISensorThresholdSeeder sensorThresholdSeeder,
        ITelemetryReadingSeeder telemetryReadingSeeder,
        IAlertSeeder alertSeeder,
        ISensorAttachmentSeeder sensorAttachmentSeeder,
        IIngestionBatchSeeder ingestionBatchSeeder,
        IEngagementStateSeeder engagementStateSeeder,
        ICommandSeeder commandSeeder,
        ISmartXDataStore store,
        ILogger<DataSeeder> logger)
    {
        _sensorProfileSeeder = sensorProfileSeeder;
        _sensorThresholdSeeder = sensorThresholdSeeder;
        _telemetryReadingSeeder = telemetryReadingSeeder;
        _alertSeeder = alertSeeder;
        _sensorAttachmentSeeder = sensorAttachmentSeeder;
        _ingestionBatchSeeder = ingestionBatchSeeder;
        _engagementStateSeeder = engagementStateSeeder;
        _commandSeeder = commandSeeder;
        _store = store;
        _logger = logger;
    }

    public void SeedAll()
    {
        // Order matters: thresholds need sensors, readings need thresholds,
        // alerts need readings, engagement totals need everything else, and the
        // command log needs the sensor profiles it dispatches to.
        _sensorProfileSeeder.Seed();
        _sensorThresholdSeeder.Seed();
        _telemetryReadingSeeder.Seed();
        _alertSeeder.Seed();
        _sensorAttachmentSeeder.Seed();
        _ingestionBatchSeeder.Seed();
        _engagementStateSeeder.Seed();
        _commandSeeder.Seed();

        _logger.LogInformation(
            "Seeded Smart-X demo data: {Sensors} sensors, {Thresholds} thresholds, {Readings} readings, {Alerts} alerts, {Attachments} attachments, {Batches} batches, {Engagement} engagement records, {Commands} commands.",
            _store.SensorProfiles.Count,
            _store.SensorThresholds.Count,
            _store.TelemetryReadings.Count,
            _store.Alerts.Count,
            _store.SensorAttachments.Count,
            _store.IngestionBatches.Count,
            _store.EngagementStates.Count,
            _store.DeviceCommands.Count);
    }
}
