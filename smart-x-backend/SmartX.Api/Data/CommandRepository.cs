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

    /// <summary>
    /// How far back an alert still counts as describing the node's state now.
    ///
    /// Deliberately not the command window: an alert raised two hours ago still
    /// matters to a command being sent this minute, so a 15m stream would
    /// otherwise report every node as clear. Deliberately not unbounded either —
    /// a mesh accumulates unresolved alerts, and if every node is alerting then
    /// the filter says nothing. A day is the horizon over which an open alert is
    /// still the node's current condition rather than its history.
    /// </summary>
    private const int AlertHorizonHours = 24;

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

        // Commands aimed at a node that is alerting right now — the overlap
        // between the two filters, reported whether or not either is applied.
        var alerting = matches
            .Where(command => command.NodeAlertState == NodeAlertState.Active)
            .ToList();

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
            AlertingCommandCount = alerting.Count,
            AlertingNodeCount = alerting.Select(command => command.SensorProfileId).Distinct().Count(),
            // Every category is present, zeroes included, so the filter bar can
            // show a count against a category that currently has no traffic.
            CategoryCounts = Enum.GetValues<OperationCategory>().ToDictionary(
                category => category.ToString(),
                category => matches.Count(command => command.OperationCategory == category)),
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
            // The category list carries its command types, so the UI can say
            // what a category selects without repeating the mapping the API
            // filters by.
            OperationCategories = Enum.GetValues<OperationCategory>()
                .Select(category => new OperationCategoryOption
                {
                    Category = category.ToString(),
                    CommandTypes = CommandOperations.TypesIn(category)
                        .Select(type => type.ToString())
                        .ToList()
                })
                .ToList(),
            AlertStates = Enum.GetNames<NodeAlertState>().ToList(),
            AlertSeverities = Enum.GetNames<AlertSeverity>().ToList(),
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

        // Alert context is attached before the filters run, because two of them
        // are conditions on it. Every command that survives therefore carries
        // the context the dashboard renders, so the badge on a row and the
        // reason the row came back always agree.
        var alertContexts = BuildAlertContexts();

        IEnumerable<DeviceCommand> matches = commands.Select(command =>
        {
            var context = alertContexts.TryGetValue(command.SensorProfileId, out var found)
                ? found
                : NodeAlertContext.None;

            return command.WithAlertContext(context.State, context.Severity, context.OpenCount);
        });

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

        if (query.OperationCategories is { Count: > 0 })
        {
            matches = matches.Where(command =>
                query.OperationCategories.Contains(command.OperationCategory));
        }

        if (query.AlertStates is { Count: > 0 })
        {
            matches = matches.Where(command => query.AlertStates.Contains(command.NodeAlertState));
        }

        if (query.MinAlertSeverity.HasValue)
        {
            // A node with nothing open has no severity, so it cannot clear a
            // severity floor: asking for Critical and above is asking for the
            // nodes that are alerting that badly.
            matches = matches.Where(command =>
                command.NodeAlertSeverity.HasValue &&
                command.NodeAlertSeverity.Value >= query.MinAlertSeverity.Value);
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
                command.Zone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                command.Parameters.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                command.IssuedBy.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                // Typed words match the labels an operator reads on screen, not
                // the PascalCase names behind them: both "restart node" and
                // "maintenance" find a RestartNode command.
                MatchesLabel(command.CommandType.ToString(), term) ||
                MatchesLabel(command.OperationCategory.ToString(), term) ||
                MatchesLabel(command.Status.ToString(), term) ||
                MatchesLabel(command.Origin.ToString(), term));
        }

        return matches.OrderByDescending(command => command.IssuedUtc).ToList();
    }

    /// <summary>
    /// The worst alert state per node, in one pass over the alert log. Alerts
    /// are grouped rather than looked up per command because a busy node has
    /// many commands and one alert history.
    /// </summary>
    private Dictionary<Guid, NodeAlertContext> BuildAlertContexts()
    {
        var horizon = DateTime.UtcNow.AddHours(-AlertHorizonHours);

        return _store.Alerts
            .Where(alert => alert.TriggeredUtc >= horizon)
            .GroupBy(alert => alert.SensorProfileId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var open = group
                        .Where(alert => alert.Status != AlertStatus.Resolved)
                        .ToList();

                    // Resolved rather than Clear: the node did alert inside the
                    // horizon, it just has nothing outstanding now.
                    var state = open.Count == 0
                        ? NodeAlertState.Resolved
                        : open.Any(alert => alert.Status == AlertStatus.Active)
                            ? NodeAlertState.Active
                            : NodeAlertState.Acknowledged;

                    return new NodeAlertContext(
                        state,
                        open.Count == 0 ? null : open.Max(alert => alert.Severity),
                        open.Count);
                });
    }

    /// <summary>
    /// Case-insensitive match that also ignores the word break a PascalCase
    /// name loses when it is displayed, so "restart node" matches RestartNode.
    /// </summary>
    private static bool MatchesLabel(string name, string term)
    {
        if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var collapsed = term.Replace(" ", string.Empty);
        return collapsed.Length > 0 &&
               name.Contains(collapsed, StringComparison.OrdinalIgnoreCase);
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

    /// <summary>The alert picture for one node, as the command query needs it.</summary>
    private readonly record struct NodeAlertContext(
        NodeAlertState State,
        AlertSeverity? Severity,
        int OpenCount)
    {
        /// <summary>A node with nothing recent against it.</summary>
        public static readonly NodeAlertContext None = new(NodeAlertState.Clear, null, 0);
    }
}
