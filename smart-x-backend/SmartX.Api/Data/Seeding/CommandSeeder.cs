using SmartX.Api.Logic;
using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

/// <summary>
/// Back-fills the command log so the history table and the widest time window
/// have something behind them at start-up. Everything seeded here is settled to
/// a terminal state — live in-flight traffic is the dispatch simulator's job.
/// </summary>
public class CommandSeeder : ICommandSeeder
{
    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public CommandSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.DeviceCommands.Count > 0)
        {
            return;
        }

        // Offset from the other seeders so the command log is not a replay of
        // the same random sequence the telemetry already used.
        var random = new Random(_options.RandomSeed + 7);
        var now = DateTime.UtcNow;

        // Commands only go to nodes that are reachable, so an offline sensor has
        // a history but no recent traffic.
        var targets = _store.SensorProfiles.Where(sensor => sensor.IsActive).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        var totalMinutes = _options.CommandHistoryHours * 60;
        var commandCount = (int)(totalMinutes * _options.CommandsPerMinute);
        var commands = new List<DeviceCommand>(commandCount);

        for (var i = 0; i < commandCount; i++)
        {
            var sensor = targets[random.Next(targets.Count)];

            // Spread over the whole window rather than bunched, then settled, so
            // the throughput strip shows variation instead of a flat line.
            var minutesAgo = random.NextDouble() * totalMinutes;
            var issuedUtc = now.AddMinutes(-minutesAgo).AddSeconds(-random.Next(0, 60));

            var command = CommandGenerator.Compose(random, sensor, issuedUtc);
            CommandGenerator.Settle(random, command, now);
            commands.Add(command);
        }

        commands.Sort((left, right) => left.IssuedUtc.CompareTo(right.IssuedUtc));
        _store.DeviceCommands.AddRange(commands);
    }
}
