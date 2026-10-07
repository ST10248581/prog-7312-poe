using SmartX.Api.Data;
using SmartX.Api.Data.Seeding;
using SmartX.Api.Logic;
using SmartX.Api.Models;
using SmartX.Api.Models.Stream;

namespace SmartX.Api.Tests;

/// <summary>
/// Builds a command engine over a small, freshly seeded in-memory store. Each
/// test gets its own, so no test sees another's queues or stacks.
/// </summary>
internal static class EngineFixture
{
    public static SmartXCommandEngine Create()
    {
        var options = new SeedOptions
        {
            SensorCount = 15,
            HistoryDays = 1,
            CommandHistoryHours = 2
        };

        var store = new SmartXDataStore();
        new SensorProfileSeeder(store, options).Seed();
        new SensorThresholdSeeder(store, options).Seed();
        new TelemetryReadingSeeder(store, options).Seed();
        new AlertSeeder(store, options).Seed();
        new CommandSeeder(store, options).Seed();
        store.RebuildIndexes();

        return new SmartXCommandEngine(store, options);
    }

    /// <summary>Reachable devices, optionally only those whose hardware accepts a command.</summary>
    public static List<LiveDevice> ReachableDevices(SmartXCommandEngine engine, CommandType? supporting = null) =>
        engine.GetLiveDevices(new DeviceQuery()).Items
            .Where(device => !device.IsDisconnected)
            .Where(device => supporting is null || CommandGenerator.Supports(device.Category, supporting.Value))
            .OrderBy(device => device.NodeId, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
