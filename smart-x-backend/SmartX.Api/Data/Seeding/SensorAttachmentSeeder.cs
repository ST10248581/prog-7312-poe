using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

public class SensorAttachmentSeeder : ISensorAttachmentSeeder
{
    private static readonly string[] Uploaders =
    [
        "t.kruger", "s.naidoo", "m.botha", "l.dlamini", "field.tech.01", "field.tech.02"
    ];

    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public SensorAttachmentSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.SensorAttachments.Count > 0)
        {
            return;
        }

        var random = new Random(_options.RandomSeed + 4);
        var now = DateTime.UtcNow;
        var attachmentTypes = Enum.GetValues<AttachmentType>();

        foreach (var sensor in _store.SensorProfiles)
        {
            var count = random.Next(_options.MinAttachmentsPerSensor, _options.MaxAttachmentsPerSensor + 1);

            for (var i = 0; i < count; i++)
            {
                var attachmentType = attachmentTypes[random.Next(attachmentTypes.Length)];
                var extension = ExtensionFor(attachmentType);
                var fileName = $"{sensor.NodeId.ToLowerInvariant()}-{NameFor(attachmentType)}-{i + 1}{extension}";

                _store.SensorAttachments.Add(new SensorAttachment
                {
                    Id = Guid.NewGuid(),
                    SensorProfileId = sensor.Id,
                    FileName = fileName,
                    StoredFileName = $"{Guid.NewGuid():N}{extension}",
                    ContentType = ContentTypeFor(attachmentType),
                    FileSizeBytes = SizeFor(random, attachmentType),
                    AttachmentType = attachmentType,
                    UploadedUtc = now.AddDays(-random.Next(0, 120)).AddMinutes(-random.Next(0, 1440)),
                    UploadedBy = Uploaders[random.Next(Uploaders.Length)],
                    Description = DescriptionFor(attachmentType, sensor)
                });
            }
        }
    }

    private static string NameFor(AttachmentType attachmentType) => attachmentType switch
    {
        AttachmentType.ConfigFile => "config",
        AttachmentType.DeploymentPhoto => "install",
        _ => "hardware-log"
    };

    private static string ExtensionFor(AttachmentType attachmentType) => attachmentType switch
    {
        AttachmentType.ConfigFile => ".json",
        AttachmentType.DeploymentPhoto => ".jpg",
        _ => ".log"
    };

    private static string ContentTypeFor(AttachmentType attachmentType) => attachmentType switch
    {
        AttachmentType.ConfigFile => "application/json",
        AttachmentType.DeploymentPhoto => "image/jpeg",
        _ => "text/plain"
    };

    private static long SizeFor(Random random, AttachmentType attachmentType) => attachmentType switch
    {
        AttachmentType.ConfigFile => random.Next(512, 8_192),
        AttachmentType.DeploymentPhoto => random.Next(180_000, 4_500_000),
        _ => random.Next(4_096, 260_000)
    };

    private static string DescriptionFor(AttachmentType attachmentType, SensorProfile sensor) => attachmentType switch
    {
        AttachmentType.ConfigFile => $"Provisioning configuration exported from {sensor.NodeId}.",
        AttachmentType.DeploymentPhoto => $"Installation photo of {sensor.Name} in {sensor.Room}.",
        _ => $"Diagnostic log pulled from {sensor.NodeId} running {sensor.FirmwareVersion}."
    };
}
