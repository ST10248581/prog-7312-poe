using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

/// <summary>
/// Reads and writes the command log. Filtering, ordering, paging and the
/// throughput buckets all happen here so the dashboard can ask for one slice
/// and render exactly what it is given.
/// </summary>
public class CommandRepository : ICommandRepository
{
    /// <summary>Upper bound on the live tail, so a wide window cannot flood the stream.</summary>
    private const int MaxStreamSize = 200;

    /// <summary>Bars in the throughput strip. Fixed, so the chart keeps its shape as the window changes.</summary>
    private const int ThroughputBuckets = 30;

    private readonly ISmartXDataStore _store;

    public CommandRepository(ISmartXDataStore store)
    {
        _store = store;
    }

    public PagedResult<DeviceCommand> Query(CommandQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var matches = Snapshot(query);

        return new PagedResult<DeviceCommand>
        {
            Items = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = matches.Count,
            TotalPages = (int)Math.Ceiling(matches.Count / (double)pageSize)
        };
    }

    public List<DeviceCommand> GetStream(CommandQuery query, int take)
    {
        return Snapshot(query).Take(Math.Clamp(take, 1, MaxStreamSize)).ToList();
    }

    public CommandSummary GetSummary(CommandQuery query)
    {
        var windowMinutes = NormaliseWindow(query.WindowMinutes);
        var now = DateTime.UtcNow;
        var matches = Snapshot(query);

        var acknowledged = matches.Where(command => command.Status == CommandStatus.Acknowledged).ToList();
        var failed = matches.Count(command => command.Status == CommandStatus.Failed);
        var expired = matches.Count(command => command.Status == CommandStatus.Expired);
        var queued = matches.Count(command => command.Status == CommandStatus.Queued);
        var sent = matches.Count(command => command.Status == CommandStatus.Sent);
        var manual = matches.Where(command => command.Origin == CommandOrigin.Manual).ToList();

        // Settled means the command reached a terminal state. Anything still in
        // flight is not a failure yet, so it stays out of the success rate.
        var settled = acknowledged.Count + failed + expired;

        return new CommandSummary
        {
            WindowMinutes = windowMinutes,
            DispatchRate = Math.Round(matches.Count / (double)windowMinutes, 2),
            InFlightCount = queued + sent,
            QueuedCount = queued,
            RetryingCount = matches.Count(command =>
                command.Retries > 0 &&
                command.Status is CommandStatus.Queued or CommandStatus.Sent),
            AcknowledgedRate = settled == 0
                ? 0
                : Math.Round(acknowledged.Count * 100d / settled, 1),
            FailedCount = failed,
            ExpiredCount = expired,
            ManualOverrideCount = manual.Count,
            OperatorCount = manual.Select(command => command.IssuedBy).Distinct().Count(),
            MedianRoundTripMs = Median(acknowledged
                .Where(command => command.RoundTripMs.HasValue)
                .Select(command => command.RoundTripMs!.Value)),
            TotalCount = matches.Count,
            Throughput = BuildThroughput(matches, now, windowMinutes),
            GeneratedUtc = now
        };
    }

    public CommandFilterOptions GetFilterOptions()
    {
        return new CommandFilterOptions
        {
            Statuses = Enum.GetNames<CommandStatus>().ToList(),
            Origins = Enum.GetNames<CommandOrigin>().ToList(),
            CommandTypes = Enum.GetNames<CommandType>().ToList(),
            Priorities = Enum.GetNames<CommandPriority>().ToList(),
            Zones = _store.SensorProfiles
                .Select(sensor => sensor.Zone)
                .Distinct()
                .OrderBy(zone => zone)
                .ToList(),
            // Only nodes that can actually accept a dispatch are offered as targets.
            Nodes = _store.SensorProfiles
                .Where(sensor => sensor.IsActive && sensor.Status != SensorStatus.Offline)
                .Select(sensor => sensor.NodeId)
                .OrderBy(nodeId => nodeId)
                .ToList()
        };
    }

    public DeviceCommand Append(DeviceCommand command)
    {
        lock (_store.CommandsSyncRoot)
        {
            _store.DeviceCommands.Add(command);
        }

        return command;
    }

    /// <summary>
    /// One filtered, newest-first copy of the log. The list is copied under the
    /// lock and then worked on outside it, so a background dispatch tick cannot
    /// mutate it midway through a request.
    /// </summary>
    private List<DeviceCommand> Snapshot(CommandQuery query)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-NormaliseWindow(query.WindowMinutes));

        List<DeviceCommand> commands;
        lock (_store.CommandsSyncRoot)
        {
            commands = _store.DeviceCommands
                .Where(command => command.IssuedUtc >= cutoff)
                .ToList();
        }

        IEnumerable<DeviceCommand> matches = commands;

        if (query.Statuses is { Count: > 0 })
        {
            matches = matches.Where(command => query.Statuses.Contains(command.Status));
        }

        if (query.Origins is { Count: > 0 })
        {
            matches = matches.Where(command => query.Origins.Contains(command.Origin));
        }

        if (query.CommandTypes is { Count: > 0 })
        {
            matches = matches.Where(command => query.CommandTypes.Contains(command.CommandType));
        }

        if (!string.IsNullOrWhiteSpace(query.Zone))
        {
            matches = matches.Where(command =>
                string.Equals(command.Zone, query.Zone, StringComparison.OrdinalIgnoreCase));
        }

        if (query.ManualOnly)
        {
            matches = matches.Where(command => command.Origin == CommandOrigin.Manual);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            matches = matches.Where(command =>
                command.NodeId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                command.SensorName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                command.Parameters.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                command.IssuedBy.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return matches.OrderByDescending(command => command.IssuedUtc).ToList();
    }

    /// <summary>
    /// Counts per bucket, oldest first. The bucket count is fixed, so the bar
    /// width stays constant and only the span each bar covers changes.
    /// </summary>
    private static List<int> BuildThroughput(List<DeviceCommand> commands, DateTime now, int windowMinutes)
    {
        var buckets = new int[ThroughputBuckets];
        var bucketMinutes = windowMinutes / (double)ThroughputBuckets;
        var start = now.AddMinutes(-windowMinutes);

        foreach (var command in commands)
        {
            var index = (int)((command.IssuedUtc - start).TotalMinutes / bucketMinutes);

            if (index < 0 || index >= ThroughputBuckets)
            {
                continue;
            }

            buckets[index]++;
        }

        // The strip is labelled per minute, so each bucket is converted to a
        // rate rather than reported as a raw count — a 24h bucket spans 48
        // minutes and a 15m bucket only 30 seconds.
        return buckets
            .Select(count => (int)Math.Round(count / bucketMinutes))
            .ToList();
    }

    private static int Median(IEnumerable<int> values)
    {
        var ordered = values.OrderBy(value => value).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        var middle = ordered.Count / 2;
        return ordered.Count % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2;
    }

    private static int NormaliseWindow(int windowMinutes)
    {
        return Math.Clamp(windowMinutes, 5, 60 * 24 * 7);
    }
}
