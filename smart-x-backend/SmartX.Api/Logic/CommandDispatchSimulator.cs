using SmartX.Api.Data;
using SmartX.Api.Data.Seeding;
using SmartX.Api.Models;

namespace SmartX.Api.Logic;

/// <summary>
/// Stands in for the mesh gateway: issues automated and scheduled traffic on a
/// timer and moves every in-flight command through its lifecycle.
/// <para>
/// This is what makes the command stream genuinely real-time rather than a
/// polled snapshot of a frozen list — a dashboard left open sees new rows
/// arrive and pending ones settle without anyone touching it. Overrides issued
/// from the console land in the same log and are advanced by the same tick.
/// </para>
/// </summary>
public class CommandDispatchSimulator : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(2);

    /// <summary>Expected new commands per tick. Fractional, so most ticks add one or none.</summary>
    private const double CommandsPerTick = 0.8;

    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;
    private readonly ILogger<CommandDispatchSimulator> _logger;
    private readonly Random _random;

    public CommandDispatchSimulator(
        ISmartXDataStore store,
        SeedOptions options,
        ILogger<CommandDispatchSimulator> logger)
    {
        _store = store;
        _options = options;
        _logger = logger;
        _random = new Random(options.RandomSeed + 11);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Command dispatch simulator running; ticking every {Seconds}s.",
            TickInterval.TotalSeconds);

        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                Tick();
            }
            catch (Exception ex)
            {
                // A bad tick must not take the host down — the next one retries.
                _logger.LogError(ex, "Command dispatch tick failed.");
            }
        }
    }

    private void Tick()
    {
        var now = DateTime.UtcNow;

        // The whole tick runs under the lock: readers take a copy, so holding it
        // for one pass over the log is cheaper than fighting over each command.
        lock (_store.CommandsSyncRoot)
        {
            foreach (var command in _store.DeviceCommands)
            {
                if (command.Status is CommandStatus.Queued or CommandStatus.Sent)
                {
                    CommandGenerator.Advance(_random, command, now);
                }
            }

            var targets = _store.SensorProfiles
                .Where(sensor => sensor.IsActive && sensor.Status != SensorStatus.Offline)
                .ToList();

            if (targets.Count > 0)
            {
                foreach (var _ in Enumerable.Range(0, IssueCount()))
                {
                    var sensor = targets[_random.Next(targets.Count)];
                    _store.DeviceCommands.Add(CommandGenerator.Compose(_random, sensor, now));
                }
            }

            Trim();
        }
    }

    /// <summary>
    /// How many commands this tick issues. Occasional bursts on top of the base
    /// rate, so the throughput strip has peaks worth looking at.
    /// </summary>
    private int IssueCount()
    {
        if (_random.NextDouble() < 0.06)
        {
            return _random.Next(3, 8);
        }

        return _random.NextDouble() < CommandsPerTick ? 1 : 0;
    }

    /// <summary>Drops the oldest commands once the log outgrows its cap.</summary>
    private void Trim()
    {
        var excess = _store.DeviceCommands.Count - _options.MaxCommandLogSize;
        if (excess > 0)
        {
            // The log is kept in issue order, so the oldest are at the front.
            _store.DeviceCommands.RemoveRange(0, excess);
        }
    }
}
