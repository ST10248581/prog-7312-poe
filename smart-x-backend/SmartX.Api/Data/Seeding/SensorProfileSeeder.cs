using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

public class SensorProfileSeeder : ISensorProfileSeeder
{
    private static readonly string[] Zones = ["Zone A", "Zone B", "Zone C", "Zone D", "Zone E"];

    private static readonly string[] Rooms =
    [
        "Server Room", "Reception", "Cold Storage", "Plant Room", "Workshop",
        "Loading Bay", "Boardroom", "Roof Deck", "Substation", "Corridor East",
        "Corridor West", "Generator Room"
    ];

    private static readonly string[] FirmwareVersions =
    [
        "v1.9.4", "v2.0.1", "v2.2.0", "v2.3.7", "v2.4.1", "v3.0.0-beta"
    ];

    private static readonly Dictionary<SensorCategory, string> CategoryPrefixes = new()
    {
        [SensorCategory.Environmental] = "ENV",
        [SensorCategory.PowerConsumption] = "PWR",
        [SensorCategory.Actuator] = "ACT",
        [SensorCategory.Motion] = "MOT",
        [SensorCategory.Connectivity] = "NET"
    };

    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public SensorProfileSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.SensorProfiles.Count > 0)
        {
            return;
        }

        var random = new Random(_options.RandomSeed);
        var now = DateTime.UtcNow;
        var categories = Enum.GetValues<SensorCategory>();

        for (var i = 0; i < _options.SensorCount; i++)
        {
            // Spread categories evenly rather than randomly so every pillar of the
            // dashboard has something to show.
            var category = categories[i % categories.Length];
            var prefix = CategoryPrefixes[category];
            var registered = now.AddDays(-random.Next(30, 400)).AddHours(-random.Next(0, 24));
            var status = PickStatus(random);

            _store.SensorProfiles.Add(new SensorProfile
            {
                Id = Guid.NewGuid(),
                MacAddress = BuildMacAddress(random),
                SerialNumber = $"SX-{prefix}-{(i + 1) * 137 % 9999:D4}-{random.Next(100, 999)}",
                Name = $"{category} Node {i + 1:D2}",
                Category = category,
                Room = Rooms[random.Next(Rooms.Length)],
                Zone = Zones[i % Zones.Length],
                NodeId = $"{prefix}-{i + 1:D3}",
                Status = status,
                FirmwareVersion = FirmwareVersions[random.Next(FirmwareVersions.Length)],
                RegisteredUtc = registered,
                LastSeenUtc = BuildLastSeen(random, now, status),
                IsActive = status != SensorStatus.Offline || random.NextDouble() < 0.4
            });
        }
    }

    private static SensorStatus PickStatus(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.70) return SensorStatus.Online;
        if (roll < 0.90) return SensorStatus.Warning;
        return SensorStatus.Offline;
    }

    private static DateTime BuildLastSeen(Random random, DateTime now, SensorStatus status) => status switch
    {
        SensorStatus.Online => now.AddSeconds(-random.Next(5, 120)),
        SensorStatus.Warning => now.AddMinutes(-random.Next(5, 90)),
        _ => now.AddHours(-random.Next(2, 96))
    };

    private static string BuildMacAddress(Random random)
    {
        var octets = new string[6];
        octets[0] = "5C";
        octets[1] = "A1";
        for (var i = 2; i < 6; i++)
        {
            octets[i] = random.Next(0, 256).ToString("X2");
        }

        return string.Join(':', octets);
    }
}
