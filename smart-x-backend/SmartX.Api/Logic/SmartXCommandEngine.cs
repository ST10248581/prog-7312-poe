// =============================================================================
// CODE ATTRIBUTION — Part 2 Technical Requirements (Real-Time Command Stream and History)
//
// This class is where the Part 2 data structures and the automated action
// engine do their work. The collection types and algorithms used for each were
// written with reference to the sources below. The individual sections below
// are marked with the matching [n] numbers, and the full reference list is
// repeated in README.md.
//
// Code Attribution [14]
// Author: Microsoft
// Year: 2025
// Title: Queue<T> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1>
// Accessed: [Accessed 29 September 2026]
// Modifications: Used Queue<T> for the standard intake lane (drained a fixed
//   budget per tick, oldest packets shed past capacity), the bounded
//   recent-alert and recent-reading windows, and the operator-activity and
//   trigger windows of the action engine.
// Reference: Microsoft, 2025. Queue<T> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1> [Accessed 29 September 2026].
//
// Code Attribution [15]
// Author: Microsoft
// Year: 2025
// Title: PriorityQueue<TElement,TPriority> Class
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2>
// Accessed: [Accessed 29 September 2026]
// Modifications: Used PriorityQueue<TElement,TPriority> for the critical intake
//   lane with a (rank, arrival ticks) tuple priority, so the largest breach is
//   dequeued first and ties stay in arrival order. A second PriorityQueue ranks
//   the suggested actions by score.
// Reference: Microsoft, 2025. PriorityQueue<TElement,TPriority> Class [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2> [Accessed 29 September 2026].
//
// Code Attribution [16]
// Author: Microsoft
// Year: 2025
// Title: Stack<T> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1>
// Accessed: [Accessed 29 September 2026]
// Modifications: Used two Stack<T> instances for undo and redo of manual
//   overrides: undo pops the most recent override and cancels or reverts it,
//   redo re-issues it, and a new override clears the redo stack.
// Reference: Microsoft, 2025. Stack<T> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1> [Accessed 29 September 2026].
//
// Code Attribution [17]
// Author: Microsoft
// Year: 2025
// Title: Dictionary<TKey,TValue> Class
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2>
// Accessed: [Accessed 29 September 2026]
// Modifications: Used Dictionary<TKey,TValue> for the live device registry
//   keyed by node id, MAC address (case-insensitive comparers) and profile id,
//   and for the threshold, node-setting and action-engine count tables.
// Reference: Microsoft, 2025. Dictionary<TKey,TValue> Class [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2> [Accessed 29 September 2026].
//
// Code Attribution [18]
// Author: Microsoft
// Year: 2025
// Title: SortedList<TKey,TValue> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedlist-2>
// Accessed: [Accessed 8 October 2026]
// Modifications: Used SortedList<DateTime, SensorLogEntry> per node for the
//   time-ordered sensor log, and added a hand-written LowerBound binary search
//   over its Keys to find the start of a time window. Timestamp collisions are
//   nudged forward a tick rather than overwritten.
// Reference: Microsoft, 2025. SortedList<TKey,TValue> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedlist-2> [Accessed 8 October 2026].
//
// Code Attribution [19]
// Author: Microsoft
// Year: 2025
// Title: HashSet<T> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1>
// Accessed: [Accessed 29 September 2026]
// Modifications: Used HashSet<T> for disconnected nodes and active error states
//   so a repeat alert is recognised with one Add/Contains check, and UnionWith,
//   ExceptWith and IntersectWith for the set differences reported to the
//   client.
// Reference: Microsoft, 2025. HashSet<T> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1> [Accessed 29 September 2026].
//
// Code Attribution [20]
// Author: Agrawal, R., Imieliński, T. and Swami, A.
// Year: 1993
// Title: Mining association rules between sets of items in large databases
// Type: [Source code]
// Available at: <https://doi.org/10.1145/170035.170072>
// Accessed: [Accessed 29 September 2026]
// Modifications: Adapted the support and confidence measures to rules of the
//   form condition => operator action, counted over a sliding window of
//   operator activity. The paper's itemset-mining algorithm itself is not
//   implemented.
// Reference: Agrawal, R., Imieliński, T. and Swami, A., 1993. Mining association rules between sets of items in large databases [Source code] Available at: <https://doi.org/10.1145/170035.170072> [Accessed 29 September 2026].
//
// Code Attribution [21]
// Author: Jurafsky, D. and Martin, J.H.
// Year: 2025
// Title: Speech and Language Processing, Chapter 3: N-gram Language Models
// Version: 3rd edition draft
// Type: [Source code]
// Available at: <https://web.stanford.edu/~jurafsky/slp3/>
// Accessed: [Accessed 29 September 2026]
// Modifications: Applied the bigram estimate count(prev -> next) / count(prev
//   -> *) to sequences of operator actions to suggest the likely next step. No
//   smoothing applied.
// Reference: Jurafsky, D. and Martin, J.H., 2025. Speech and Language Processing, Chapter 3: N-gram Language Models (3rd edition draft) [Source code] Available at: <https://web.stanford.edu/~jurafsky/slp3/> [Accessed 29 September 2026].
//
// Code Attribution [22]
// Author: Welford, B.P.
// Year: 1962
// Title: Note on a method for calculating corrected sums of squares and products
// Type: [Source code]
// Available at: <https://doi.org/10.1080/00401706.1962.10490022>
// Accessed: [Accessed 29 September 2026]
// Modifications: Implemented Welford's single-pass running mean and variance
//   update as the RunningStats record struct, and added a z-score of the latest
//   reading used to flag drifting devices.
// Reference: Welford, B.P., 1962. Note on a method for calculating corrected sums of squares and products [Source code] Available at: <https://doi.org/10.1080/00401706.1962.10490022> [Accessed 29 September 2026].
// =============================================================================

using System.Diagnostics;
using System.Text.RegularExpressions;
using SmartX.Api.Data;
using SmartX.Api.Data.Seeding;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;
using SmartX.Api.Models.Stream;
using SmartX.Api.Models.Validation;

namespace SmartX.Api.Logic;

/// <summary>
/// Central application-logic service for the Real-Time Command Stream and
/// History page — the Part 2 counterpart of <see cref="SmartXTelemetryEngine"/>.
/// <para>
/// The command queries, manual dispatch, undo, the telemetry intake, the live
/// dispatch loop and the recommendation engine used to be spread across a
/// repository, a service and a background worker. They all work on the same
/// volatile state — packets arriving, commands settling, nodes dropping off —
/// so they now live in one singleton that owns that state and guards it with
/// one lock.
/// </para>
/// <para>
/// Each structure is chosen for the operation it has to make cheap:
/// <list type="bullet">
///   <item><description><b>Queue&lt;T&gt;</b> — routine packets are processed first-in, first-out, a budget per tick.</description></item>
///   <item><description><b>PriorityQueue</b> — critical packets bypass that queue and are drained at once, worst breach first.</description></item>
///   <item><description><b>Stack&lt;T&gt;</b> — manual overrides, so undo always reverses the most recent one; a second stack holds what was undone, for redo.</description></item>
///   <item><description><b>Dictionary</b> — the live device registry, keyed by node id and by MAC address, for O(1) lookup per packet.</description></item>
///   <item><description><b>SortedList</b> — each node's log keyed by timestamp, so a timeline reads out already in order and a time window is found by binary search.</description></item>
///   <item><description><b>HashSet&lt;T&gt;</b> — active error states and disconnected nodes, so a repeat alert is recognised in O(1) and dropped.</description></item>
/// </list>
/// </para>
/// </summary>
public class SmartXCommandEngine : ISmartXCommandEngine
{
    /* ---------- Command log ---------- */

    /// <summary>Upper bound on the live tail, so a wide window cannot flood the stream.</summary>
    private const int MaxStreamSize = 200;

    /// <summary>Bars in the throughput strip. Fixed, so the chart keeps its shape as the window changes.</summary>
    private const int ThroughputBuckets = 30;

    /// <summary>
    /// How far back a logged alert still counts as describing the node's state
    /// now. A day: an alert raised two hours ago still matters to a command
    /// sent this minute, but an unbounded horizon would mark every node alerting.
    /// </summary>
    private const int AlertHorizonHours = 24;

    /* ---------- Intake ---------- */

    /// <summary>Standard-lane packets processed per tick. The critical lane has no budget.</summary>
    private const int StandardBudgetPerTick = 20;

    /// <summary>
    /// Backpressure. Beyond this the oldest routine packets are shed rather than
    /// letting memory grow and every later packet wait longer. Sized at fifty
    /// ticks of drain budget: a backlog older than that is stale telemetry.
    /// </summary>
    private const int MaxStandardQueueDepth = 1_000;

    /// <summary>A breach this far past the limit (as a share of the normal span) is critical.</summary>
    private const double CriticalMargin = 0.25;

    private const int RecentAlertCapacity = 60;

    /// <summary>Weight of the newest sample in the rolling queue-wait averages.</summary>
    private const double WaitSmoothing = 0.2;

    /* ---------- Simulated mesh traffic ---------- */

    /// <summary>Expected new automated commands per tick. Fractional, so most ticks add one or none.</summary>
    private const double CommandsPerTick = 0.8;

    /// <summary>Ordinary nodes reporting per tick, on top of the flaky ones.</summary>
    private const int ReportingNodesPerTick = 5;

    /* ---------- Sensor logs ---------- */

    private const int LogRetentionHours = 6;
    private const int MaxLogEntriesPerNode = 4_000;
    private const int MaxTimelineEvents = 80;

    /// <summary>Values kept per node and metric for the live panel's sparklines.</summary>
    private const int RecentReadingDepth = 24;

    /* ---------- Undo ---------- */

    private const int MaxUndoDepth = 50;

    /* ---------- Action engine ---------- */

    /// <summary>An action this soon after a condition is counted as a response to it.</summary>
    private static readonly TimeSpan AssociationWindow = TimeSpan.FromMinutes(10);

    /// <summary>
    /// A condition only counts as evidence while an operator is around to react
    /// to it. Otherwise every unattended overnight breach would read as
    /// "operators ignore this", and every learned rule would decay to nothing.
    /// </summary>
    private static readonly TimeSpan OperatorPresence = TimeSpan.FromMinutes(15);

    /// <summary>Two actions further apart than this are not one sequence.</summary>
    private static readonly TimeSpan SessionGap = TimeSpan.FromMinutes(15);

    private const int MaxActivityWindow = 500;
    private const int MinRuleSupport = 3;
    private const int MinRuleCount = 2;
    private const double MinRuleConfidence = 0.4;
    private const double MinTransitionProbability = 0.3;
    private const int ProblemWindowMinutes = 30;
    private const int FailureWindowMinutes = 60;
    private const double ProblemScoreThreshold = 3.0;
    private const double DriftZScore = 2.5;
    private const int MaxSuggestions = 8;

    /// <summary>How long a dismissal keeps pushing the same suggestion down the ranking.</summary>
    private static readonly TimeSpan DismissalMemory = TimeSpan.FromMinutes(30);

    /// <summary>Each dismissal multiplies the suggestion's score by this.</summary>
    private const double DismissalPenalty = 0.4;

    /// <summary>Each time a suggestion is applied its score grows by this share, up to a cap.</summary>
    private const double AcceptanceBoost = 0.15;
    private const int MaxAcceptanceBoosts = 4;

    private const int TopRuleCount = 5;
    private const int RecentActivityCount = 8;

    /// <summary>Placeholder node in a learned action, meaning "the node the condition fired on".</summary>
    private const string SelfNode = "@self";

    /// <summary>The firmware a failing beta node is rolled back to.</summary>
    private const string StableFirmware = "v2.4.1";

    /* ---------- Parameter grammar, per command type ---------- */

    private static readonly Regex ThresholdPattern = new(
        @"^(?<key>[a-z][a-z0-9_.]*)=(?<value>-?\d+(\.\d+)?[a-z%]*|default)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OffsetPattern = new(
        @"^offset=(?<value>auto|factory|[+-]?\d+(\.\d+)?)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ActuatorPattern = new(
        @"^relay=(?<relay>\d{1,2}),state=(?<state>on|off)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RestartPattern = new(
        @"^mode=(soft|hard)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FirmwarePattern = new(
        @"^v\d+\.\d+\.\d+(-[a-z0-9]+)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SamplePattern = new(
        @"^count=(?<count>\d{1,3})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;
    private readonly Random _random;

    /// <summary>
    /// The one lock over all engine state. It is the store's command lock, so
    /// the command log and the structures built around it can never be seen
    /// half-updated relative to each other.
    /// </summary>
    private readonly object _sync;

    private bool _initialised;
    private int _registeredProfileCount;
    private int _registeredThresholdCount;

    /* ---------- Queues [14], [15] ---------- */

    /// <summary>Routine packets, first in first out.</summary>
    private readonly Queue<StreamPacket> _standardLane = new();

    /// <summary>
    /// Critical packets. Lower priority dequeues first: the rank puts the
    /// largest breach ahead, and the arrival ticks break ties in FIFO order.
    /// </summary>
    private readonly PriorityQueue<StreamPacket, (int Rank, long ReceivedTicks)> _criticalLane = new();

    /// <summary>Alerts the pipeline raised; a bounded FIFO, so the oldest falls off.</summary>
    private readonly Queue<StreamAlert> _recentAlerts = new();

    /* ---------- Undo stack [16] ---------- */

    private readonly Stack<OverrideHistoryEntry> _overrideHistory = new();

    /// <summary>
    /// Overrides that were undone, most recent on top. Redo pops from here; any
    /// new manual override clears it, as in every editor's undo/redo.
    /// </summary>
    private readonly Stack<OverrideHistoryEntry> _redoHistory = new();

    /// <summary>The commands on the stack, by id, so undo finds its target in O(1).</summary>
    private readonly Dictionary<Guid, DeviceCommand> _overrideCommands = new();

    /* ---------- Registry and state dictionaries [17] ---------- */

    private readonly Dictionary<string, SensorProfile> _devicesByNode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SensorProfile> _devicesByMac = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, SensorProfile> _devicesById = new();
    private readonly Dictionary<(Guid SensorId, ReadingType Type), SensorThreshold> _thresholds = new();

    /// <summary>Last value set per node and setting key, e.g. ENV-001 → temp.max → 28.5. Undo reads it.</summary>
    private readonly Dictionary<string, Dictionary<string, string>> _nodeSettings = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Last parameters used per learned action, so a suggestion arrives filled in.</summary>
    private readonly Dictionary<string, string> _typicalParameters = new();

    /* ---------- Sorted sensor logs — SortedList [18] ---------- */

    /// <summary>
    /// Each node's log, keyed and kept in timestamp order. A SortedList rather
    /// than a SortedDictionary: both keep keys sorted, but SortedList stores them
    /// in an array, so <see cref="LowerBound"/> can binary-search straight to the
    /// start of a time window and read forward from there — O(log n + k) for k
    /// entries in the window — instead of walking the whole log. Readings arrive
    /// almost entirely in time order, so inserts land at the end and stay cheap.
    /// </summary>
    private readonly Dictionary<string, SortedList<DateTime, SensorLogEntry>> _sensorLogs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The last few values per node and metric — a bounded Queue&lt;T&gt; [14]
    /// behind a Dictionary [17] — so the live device panel reads every device's
    /// latest readings in O(1) each instead of walking its full sorted log.
    /// </summary>
    private readonly Dictionary<(string NodeId, ReadingType Type), Queue<TimelinePoint>> _recentReadings = new();

    /* ---------- Sets [19] ---------- */

    private readonly HashSet<string> _disconnectedNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<ErrorStateKey> _activeErrorStates = new();
    private readonly Dictionary<string, DateTime> _disconnectedSince = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ErrorStateKey, ErrorStateInfo> _errorStateInfo = new();

    /// <summary>Repeat "link lost" reports absorbed per disconnected node.</summary>
    private readonly Dictionary<string, int> _disconnectRepeats = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A few nodes misbehave more than the rest, so the live demo has recurring conditions.</summary>
    private readonly HashSet<string> _flakyNodes = new(StringComparer.OrdinalIgnoreCase);

    /* ---------- Live loop ---------- */

    /// <summary>
    /// Only the commands still moving. The tick advances these rather than
    /// scanning the whole 20 000-row log for the handful not yet settled.
    /// </summary>
    private readonly List<DeviceCommand> _inFlight = new();

    private readonly Dictionary<(string NodeId, ReadingType Type), Excursion> _excursions = new();
    private readonly Dictionary<string, int> _outageTicks = new(StringComparer.OrdinalIgnoreCase);

    private long _totalReceived;
    private long _standardProcessed;
    private long _criticalProcessed;
    private long _duplicatesSuppressed;
    private long _dropped;
    private long _bypassedStandard;
    private int _lastCriticalBypassed;
    private double _averageStandardWaitMs;
    private double _averageCriticalWaitMs;

    /* ---------- Action engine state [20], [21] ---------- */

    /// <summary>Recent operator actions, a bounded sliding window.</summary>
    private readonly Queue<OperatorActivity> _activityWindow = new();

    /// <summary>Conditions still inside the association window, oldest first.</summary>
    private readonly Queue<TriggerEvent> _triggerWindow = new();

    /// <summary>How often each condition was seen while an operator was present.</summary>
    private readonly Dictionary<string, int> _triggerSupport = new();

    /// <summary>Condition → action → how often the action followed the condition.</summary>
    private readonly Dictionary<string, Dictionary<string, int>> _associations = new();

    /// <summary>Action → next action → count. A first-order Markov chain over operator sessions.</summary>
    private readonly Dictionary<string, Dictionary<string, int>> _transitions = new();

    private readonly Dictionary<string, OperatorActivity> _lastActionByOperator = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _lastActionAt = new();
    private OperatorActivity? _lastActivity;
    private long _observedActions;
    private DateTime _lastOperatorActivityUtc = DateTime.MinValue;

    /* ---------- Suggestion feedback ---------- */

    /// <summary>Per suggestion id: how often it was dismissed, and when last.</summary>
    private readonly Dictionary<string, (int Count, DateTime LastUtc)> _dismissals = new();

    /// <summary>Per suggestion id: how often it was applied.</summary>
    private readonly Dictionary<string, int> _acceptances = new();

    private int _suggestionsApplied;
    private int _suggestionsDismissed;

    public SmartXCommandEngine(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
        _sync = store.CommandsSyncRoot;
        _random = new Random(options.RandomSeed + 11);
    }

    // =====================================================================
    // Command log
    // =====================================================================

    public PagedResult<DeviceCommand> GetCommands(CommandQuery query)
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

        // Settled means the command reached a terminal state it earned. Anything
        // still in flight is not a failure yet, and a cancelled override never
        // reached the node, so neither counts towards the success rate.
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
        List<SensorProfile> targets;
        List<string> zones;

        lock (_sync)
        {
            EnsureReady();

            // Only nodes that can actually accept a dispatch are offered as
            // targets — the disconnected set answers that per node in O(1).
            targets = _devicesByNode.Values
                .Where(IsReachable)
                .OrderBy(sensor => sensor.NodeId)
                .ToList();

            zones = _devicesByNode.Values
                .Select(sensor => sensor.Zone)
                .Distinct()
                .OrderBy(zone => zone)
                .ToList();
        }

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
            SensorCategories = Enum.GetNames<SensorCategory>().ToList(),
            Zones = zones,
            Nodes = targets.Select(sensor => sensor.NodeId).ToList(),
            NodeCapabilities = targets.ToDictionary(
                sensor => sensor.NodeId,
                sensor => CommandGenerator.SupportedCommands(sensor.Category)
                    .Select(type => type.ToString())
                    .ToList())
        };
    }

    /// <summary>
    /// One filtered, newest-first copy of the log. Taken under the lock and
    /// worked on outside it, so a dispatch tick cannot mutate it midway through
    /// a request.
    /// </summary>
    private List<DeviceCommand> Snapshot(CommandQuery query)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-NormaliseWindow(query.WindowMinutes));

        List<DeviceCommand> commands;
        Dictionary<Guid, NodeAlertContext> liveContexts;
        Dictionary<Guid, SensorCategory> categories;

        lock (_sync)
        {
            EnsureReady();

            // A command carries its node, not the node's category; this copy of
            // the registry answers that per command in O(1) once the lock is released.
            categories = _devicesById.ToDictionary(pair => pair.Key, pair => pair.Value.Category);

            // The log is kept in issue order, so the window is a suffix of it:
            // walk back from the newest and stop at the first command older
            // than the cutoff, rather than testing all 20 000 rows.
            commands = new List<DeviceCommand>();
            for (var index = _store.DeviceCommands.Count - 1; index >= 0; index--)
            {
                var command = _store.DeviceCommands[index];
                if (command.IssuedUtc < cutoff)
                {
                    break;
                }

                commands.Add(command);
            }

            liveContexts = BuildLiveAlertContexts();
        }

        // Alert context is attached before the filters run, because two of them
        // are conditions on it. Every command that survives therefore carries
        // the context the dashboard renders, so the badge on a row and the
        // reason the row came back always agree.
        var alertContexts = BuildAlertContexts(liveContexts);

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

        if (query.SensorCategories is { Count: > 0 })
        {
            matches = matches.Where(command =>
                categories.TryGetValue(command.SensorProfileId, out var category) &&
                query.SensorCategories.Contains(category));
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
                MatchesLabel(command.Origin.ToString(), term) ||
                (categories.TryGetValue(command.SensorProfileId, out var category) &&
                 MatchesLabel(category.ToString(), term)));
        }

        return matches.OrderByDescending(command => command.IssuedUtc).ToList();
    }

    /// <summary>
    /// The worst alert state per node: the logged alerts, merged with what the
    /// intake is holding open right now. Grouped in one pass rather than looked
    /// up per command, because a busy node has many commands and one history.
    /// </summary>
    private Dictionary<Guid, NodeAlertContext> BuildAlertContexts(Dictionary<Guid, NodeAlertContext> live)
    {
        var horizon = DateTime.UtcNow.AddHours(-AlertHorizonHours);

        var contexts = _store.Alerts
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

        foreach (var (sensorId, current) in live)
        {
            if (!contexts.TryGetValue(sensorId, out var logged))
            {
                contexts[sensorId] = current;
                continue;
            }

            contexts[sensorId] = new NodeAlertContext(
                current.State > logged.State ? current.State : logged.State,
                MaxSeverity(logged.Severity, current.Severity),
                logged.OpenCount + current.OpenCount);
        }

        return contexts;
    }

    /// <summary>
    /// The alert state the intake is holding: every open error state and every
    /// disconnected node is an unacknowledged alert on that node. Caller holds the lock.
    /// </summary>
    private Dictionary<Guid, NodeAlertContext> BuildLiveAlertContexts()
    {
        var live = new Dictionary<Guid, NodeAlertContext>();

        void Add(string nodeId, AlertSeverity severity)
        {
            if (!_devicesByNode.TryGetValue(nodeId, out var device))
            {
                return;
            }

            live[device.Id] = live.TryGetValue(device.Id, out var existing)
                ? new NodeAlertContext(NodeAlertState.Active, MaxSeverity(existing.Severity, severity), existing.OpenCount + 1)
                : new NodeAlertContext(NodeAlertState.Active, severity, 1);
        }

        foreach (var (key, info) in _errorStateInfo)
        {
            Add(key.NodeId, info.Severity);
        }

        foreach (var nodeId in _disconnectedNodes)
        {
            Add(nodeId, AlertSeverity.Warning);
        }

        return live;
    }

    // =====================================================================
    // Manual overrides and the undo stack [16]
    // Code attribution: overrides are pushed onto and popped from Stack<T> using
    // the Push/Pop/TryPeek members documented in [16]. The second redo stack, the
    // revert plan stored with each entry and the idempotent undo/redo are
    // additions of this project.
    // =====================================================================

    public (DeviceCommand? command, string? error) Dispatch(DispatchCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NodeId))
        {
            return (null, "A target node is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Parameters))
        {
            return (null, "Command parameters are required.");
        }

        var parameters = request.Parameters.Trim();
        var issuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? "operator" : request.IssuedBy.Trim();

        lock (_sync)
        {
            EnsureReady();

            // Registry lookup: one hash probe on the node id.
            if (!_devicesByNode.TryGetValue(request.NodeId.Trim(), out var sensor))
            {
                return (null, $"No node registered with id '{request.NodeId}'.");
            }

            // A dry run is validated and logged against an unreachable node, but
            // a real dispatch to one would sit queued until it expired.
            if (!request.DryRun && !IsReachable(sensor))
            {
                return (null, $"{sensor.NodeId} is offline and cannot accept a dispatch. Send it as a dry run to log the intent.");
            }

            var invalid = ValidateParameters(sensor, request.CommandType, parameters);
            if (invalid is not null)
            {
                return (null, invalid);
            }

            var now = DateTime.UtcNow;

            // The inverse has to be worked out before the command is applied:
            // afterwards the value it replaced is gone.
            var plan = PlanRevert(sensor, request.CommandType, parameters);

            var command = IssueCommand(
                sensor, request.CommandType, parameters, CommandOrigin.Manual,
                request.Priority, issuedBy, request.DryRun, now);

            // A dry run never changed the node, so there is nothing to undo.
            if (!request.DryRun)
            {
                PushOverride(new OverrideHistoryEntry
                {
                    CommandId = command.Id,
                    NodeId = sensor.NodeId,
                    SensorName = sensor.Name,
                    CommandType = command.CommandType,
                    Parameters = command.Parameters,
                    Priority = command.Priority,
                    IssuedBy = issuedBy,
                    IssuedUtc = now,
                    RevertCommandType = plan.CommandType,
                    RevertParameters = plan.Parameters,
                    UndoDescription = plan.Description
                }, command);

                // A new change makes whatever was undone before it unreachable.
                _redoHistory.Clear();
            }

            // Every override is also something the action engine learns from.
            var token = ActionToken("command", request.CommandType.ToString(), sensor.NodeId);
            _typicalParameters[token] = parameters;
            _typicalParameters[ActionToken("command", request.CommandType.ToString(), SelfNode)] = parameters;
            RecordAction(issuedBy, token, sensor.NodeId, now);

            // Queued only. The next tick picks it up and advances it exactly as
            // it does automated traffic.
            return (command, null);
        }
    }

    public OverrideHistoryResponse GetOverrideHistory()
    {
        lock (_sync)
        {
            EnsureReady();

            // Stack<T> enumerates from the top, so both lists are already most recent first.
            return new OverrideHistoryResponse
            {
                Undo = _overrideHistory.Select(entry =>
                {
                    entry.Status = _overrideCommands.TryGetValue(entry.CommandId, out var command)
                        ? command.Status
                        : null;
                    return entry;
                }).ToList(),
                Redo = _redoHistory.ToList()
            };
        }
    }

    /// <summary>
    /// Reverses the most recent override. LIFO is the point: undo always acts
    /// on the last thing issued, so pressing it twice walks back two steps in
    /// the order they were taken.
    /// </summary>
    public (UndoResult? result, string? error) UndoLastOverride(string? issuedBy, Guid? expectedCommandId = null)
    {
        var operatorId = string.IsNullOrWhiteSpace(issuedBy) ? "operator" : issuedBy.Trim();

        lock (_sync)
        {
            EnsureReady();

            // Idempotency: the client names the entry it means to undo. If that
            // is no longer on top, a second click (or a retried request) must
            // not go on to undo the next override down.
            if (expectedCommandId is Guid expected &&
                (!_overrideHistory.TryPeek(out var head) || head.CommandId != expected))
            {
                if (_redoHistory.TryPeek(out var alreadyUndone) && alreadyUndone.CommandId == expected)
                {
                    return (new UndoResult
                    {
                        Outcome = UndoOutcome.AlreadyUndone,
                        Message = $"{Humanise(alreadyUndone.CommandType.ToString())} on {alreadyUndone.NodeId} was already undone. Nothing else was changed.",
                        Undone = alreadyUndone,
                        RemainingDepth = _overrideHistory.Count,
                        RedoDepth = _redoHistory.Count
                    }, null);
                }

                return (null, "The undo history changed since it was loaded, so nothing was undone. Check the stack and try again.");
            }

            if (!_overrideHistory.TryPop(out var entry))
            {
                return (null, "There are no manual overrides to undo.");
            }

            _overrideCommands.Remove(entry.CommandId, out var original);
            var now = DateTime.UtcNow;

            // Still queued, or failed outright: either way the node never acted
            // on it, so there is nothing to send back — only something to withdraw.
            if (original is { Status: CommandStatus.Queued or CommandStatus.Failed or CommandStatus.Expired })
            {
                var wasQueued = original.Status == CommandStatus.Queued;
                if (wasQueued)
                {
                    original.Status = CommandStatus.Cancelled;
                    _inFlight.Remove(original);
                }

                // The setting it would have written never took effect.
                if (entry.RevertCommandType.HasValue && entry.RevertParameters is not null)
                {
                    ApplySettings(entry.NodeId, entry.RevertCommandType.Value, entry.RevertParameters);
                }

                AppendLog(entry.NodeId, new SensorLogEntry
                {
                    TimestampUtc = now,
                    Kind = SensorLogKind.Command,
                    Label = $"{(wasQueued ? "Cancelled" : "Withdrew")} {Humanise(entry.CommandType.ToString())} ({operatorId}, undo)"
                });

                entry.Status = original.Status;
                PushRedo(entry);
                RecordAction(operatorId, ActionToken("undo", entry.CommandType.ToString(), entry.NodeId), entry.NodeId, now);

                return (new UndoResult
                {
                    Outcome = UndoOutcome.Cancelled,
                    Message = wasQueued
                        ? $"{Humanise(entry.CommandType.ToString())} on {entry.NodeId} was still queued and has been cancelled."
                        : $"{Humanise(entry.CommandType.ToString())} on {entry.NodeId} {original.Status.ToString().ToLowerInvariant()} without reaching the node, so there was nothing to revert.",
                    Undone = entry,
                    RemainingDepth = _overrideHistory.Count,
                    RedoDepth = _redoHistory.Count
                }, null);
            }

            if (entry.RevertCommandType is not CommandType revertType || entry.RevertParameters is null)
            {
                entry.Status = original?.Status;
                return (new UndoResult
                {
                    Outcome = UndoOutcome.Irreversible,
                    Message = $"{Humanise(entry.CommandType.ToString())} on {entry.NodeId} has already reached the node. {entry.UndoDescription}",
                    Undone = entry,
                    RemainingDepth = _overrideHistory.Count,
                    RedoDepth = _redoHistory.Count
                }, null);
            }

            if (!_devicesByNode.TryGetValue(entry.NodeId, out var sensor) || !IsReachable(sensor))
            {
                // Put it back: the override still stands and can be undone once
                // the node is reachable again.
                PushOverride(entry, original);
                return (null, $"{entry.NodeId} is offline, so the revert cannot be dispatched. The override is still on the undo stack.");
            }

            // The compensating command goes out at Immediate: whoever pressed
            // undo wants the node back the way it was now, not behind the queue.
            // It is not itself pushed, so undo never ping-pongs between two states.
            var revert = IssueCommand(
                sensor, revertType, entry.RevertParameters, CommandOrigin.Manual,
                CommandPriority.Immediate, $"{operatorId} (undo)", dryRun: false, now);

            entry.Status = original?.Status;
            PushRedo(entry);
            RecordAction(operatorId, ActionToken("undo", entry.CommandType.ToString(), entry.NodeId), entry.NodeId, now);

            return (new UndoResult
            {
                Outcome = UndoOutcome.Reverted,
                Message = $"{entry.UndoDescription}: sent to {entry.NodeId} at Immediate priority.",
                Undone = entry,
                RevertCommand = revert,
                RemainingDepth = _overrideHistory.Count,
                RedoDepth = _redoHistory.Count
            }, null);
        }
    }

    /// <summary>
    /// Re-applies the most recently undone override: the same command, sent
    /// again, which goes back on the undo stack with a fresh revert plan. Like
    /// undo it is idempotent when the client names the entry it expects.
    /// </summary>
    public (RedoResult? result, string? error) RedoLastUndo(string? issuedBy, Guid? expectedCommandId = null)
    {
        var operatorId = string.IsNullOrWhiteSpace(issuedBy) ? "operator" : issuedBy.Trim();

        lock (_sync)
        {
            EnsureReady();

            if (expectedCommandId is Guid expected &&
                (!_redoHistory.TryPeek(out var head) || head.CommandId != expected))
            {
                if (_overrideHistory.TryPeek(out var top) && top.RedoOf == expected)
                {
                    return (new RedoResult
                    {
                        Outcome = RedoOutcome.AlreadyRedone,
                        Message = $"{Humanise(top.CommandType.ToString())} on {top.NodeId} was already redone. Nothing else was changed.",
                        Redone = top,
                        UndoDepth = _overrideHistory.Count,
                        RedoDepth = _redoHistory.Count
                    }, null);
                }

                return (null, "The redo history changed since it was loaded, so nothing was redone. Check the stack and try again.");
            }

            if (!_redoHistory.TryPeek(out var entry))
            {
                return (null, "There is nothing to redo.");
            }

            if (!_devicesByNode.TryGetValue(entry.NodeId, out var sensor) || !IsReachable(sensor))
            {
                return (null, $"{entry.NodeId} is offline, so the override cannot be re-applied. It stays on the redo stack.");
            }

            var invalid = ValidateParameters(sensor, entry.CommandType, entry.Parameters);
            if (invalid is not null)
            {
                return (null, invalid);
            }

            _redoHistory.Pop();
            var now = DateTime.UtcNow;

            // Planned against the node as it is now, which is what a later undo restores.
            var plan = PlanRevert(sensor, entry.CommandType, entry.Parameters);
            var command = IssueCommand(
                sensor, entry.CommandType, entry.Parameters, CommandOrigin.Manual,
                entry.Priority, $"{operatorId} (redo)", dryRun: false, now);

            var redone = new OverrideHistoryEntry
            {
                CommandId = command.Id,
                NodeId = sensor.NodeId,
                SensorName = sensor.Name,
                CommandType = command.CommandType,
                Parameters = command.Parameters,
                Priority = command.Priority,
                IssuedBy = operatorId,
                IssuedUtc = now,
                RevertCommandType = plan.CommandType,
                RevertParameters = plan.Parameters,
                UndoDescription = plan.Description,
                RedoOf = entry.CommandId
            };

            PushOverride(redone, command);

            // A redo is the operator choosing that action again: it teaches the engine the same way.
            RecordAction(operatorId, ActionToken("command", entry.CommandType.ToString(), sensor.NodeId), sensor.NodeId, now);

            return (new RedoResult
            {
                Outcome = RedoOutcome.Redone,
                Message = $"{Humanise(entry.CommandType.ToString())} {entry.Parameters} re-queued for {entry.NodeId}.",
                Redone = redone,
                Command = command,
                UndoDepth = _overrideHistory.Count,
                RedoDepth = _redoHistory.Count
            }, null);
        }
    }

    /// <summary>Pushes an undone override onto the redo stack, keeping it to the same cap. Caller holds the lock.</summary>
    private void PushRedo(OverrideHistoryEntry entry)
    {
        _redoHistory.Push(entry);
        if (_redoHistory.Count <= MaxUndoDepth)
        {
            return;
        }

        var kept = _redoHistory.Take(MaxUndoDepth).ToList();
        _redoHistory.Clear();
        for (var index = kept.Count - 1; index >= 0; index--)
        {
            _redoHistory.Push(kept[index]);
        }
    }

    /// <summary>Pushes an override, keeping the stack to its cap. Caller holds the lock.</summary>
    private void PushOverride(OverrideHistoryEntry entry, DeviceCommand? command)
    {
        _overrideHistory.Push(entry);
        if (command is not null)
        {
            _overrideCommands[entry.CommandId] = command;
        }

        if (_overrideHistory.Count <= MaxUndoDepth)
        {
            return;
        }

        // Stack<T> cannot drop from the bottom, so rebuild it from the newest
        // entries. Rare — once per MaxUndoDepth pushes at most — so O(n) is fine.
        var kept = _overrideHistory.Take(MaxUndoDepth).ToList();
        foreach (var dropped in _overrideHistory.Skip(MaxUndoDepth))
        {
            _overrideCommands.Remove(dropped.CommandId);
        }

        _overrideHistory.Clear();
        for (var index = kept.Count - 1; index >= 0; index--)
        {
            _overrideHistory.Push(kept[index]);
        }
    }

    /// <summary>
    /// Checks the parameters against the grammar for the command and the
    /// hardware against the command. Returns the reason for a rejection, or null.
    /// </summary>
    private static string? ValidateParameters(SensorProfile sensor, CommandType commandType, string parameters)
    {
        if (!CommandGenerator.Supports(sensor.Category, commandType))
        {
            var category = Humanise(sensor.Category.ToString());
            var article = "AEIOU".Contains(category[0]) ? "an" : "a";
            return $"{sensor.NodeId} is {article} {category} node and does not accept {Humanise(commandType.ToString())}.";
        }

        switch (commandType)
        {
            case CommandType.SetThreshold when !ThresholdPattern.IsMatch(parameters):
                return "Set Threshold expects key=value, e.g. temp.max=28.5 or power.max=4.5kW.";

            case CommandType.Recalibrate when !OffsetPattern.IsMatch(parameters):
                return "Recalibrate expects offset=auto, offset=factory or a signed offset such as offset=-0.4.";

            case CommandType.ToggleActuator when !ActuatorPattern.IsMatch(parameters):
                return "Toggle Actuator expects relay=<n>,state=on|off, e.g. relay=1,state=off.";

            case CommandType.RestartNode when !RestartPattern.IsMatch(parameters):
                return "Restart Node expects mode=soft or mode=hard.";

            case CommandType.FirmwarePush when !FirmwarePattern.IsMatch(parameters):
                return "Firmware Push expects a version such as v2.4.1.";

            case CommandType.RequestSample:
                var match = SamplePattern.Match(parameters);
                if (!match.Success || !int.TryParse(match.Groups["count"].Value, out var count) || count is < 1 or > 50)
                {
                    return "Request Sample expects count=<1–50>, e.g. count=5.";
                }

                break;
        }

        return null;
    }

    /// <summary>
    /// The command that would put the node back, from the value the override
    /// is about to replace. Caller holds the lock.
    /// </summary>
    private RevertPlan PlanRevert(SensorProfile sensor, CommandType commandType, string parameters)
    {
        switch (commandType)
        {
            case CommandType.SetThreshold:
            {
                var match = ThresholdPattern.Match(parameters);
                var key = match.Groups["key"].Value;
                var previous = Setting(sensor.NodeId, key);
                return new RevertPlan(
                    CommandType.SetThreshold,
                    $"{key}={previous ?? "default"}",
                    previous is null ? $"Reset {key} to its default" : $"Restore {key} to {previous}");
            }

            case CommandType.Recalibrate:
            {
                var previous = Setting(sensor.NodeId, "offset") ?? "factory";
                return new RevertPlan(
                    CommandType.Recalibrate,
                    $"offset={previous}",
                    previous == "factory" ? "Return the calibration to factory" : $"Restore the calibration offset to {previous}");
            }

            case CommandType.ToggleActuator:
            {
                var match = ActuatorPattern.Match(parameters);
                var relay = match.Groups["relay"].Value;
                var state = match.Groups["state"].Value.ToLowerInvariant();
                var previous = Setting(sensor.NodeId, $"relay.{relay}") ?? (state == "on" ? "off" : "on");
                return new RevertPlan(
                    CommandType.ToggleActuator,
                    $"relay={relay},state={previous}",
                    previous == state
                        ? $"Relay {relay} was already {previous}; re-assert it {previous}"
                        : $"Switch relay {relay} back {previous}");
            }

            case CommandType.FirmwarePush:
            {
                var previous = Setting(sensor.NodeId, "firmware") ?? sensor.FirmwareVersion;
                return new RevertPlan(
                    CommandType.FirmwarePush,
                    previous,
                    string.Equals(previous, parameters, StringComparison.OrdinalIgnoreCase)
                        ? $"Firmware was already {previous}; re-flash {previous}"
                        : $"Roll the firmware back to {previous}");
            }

            case CommandType.RestartNode:
                return new RevertPlan(null, null,
                    "A restart cannot be reversed; undo can only cancel one that is still queued.");

            default:
                return new RevertPlan(null, null,
                    "A sample request has no inverse; undo can only cancel one that is still queued.");
        }
    }

    private string? Setting(string nodeId, string key)
    {
        return _nodeSettings.TryGetValue(nodeId, out var settings) && settings.TryGetValue(key, out var value)
            ? value
            : null;
    }

    /// <summary>Records what a command sets on the node, so a later undo knows what it replaced.</summary>
    private void ApplySettings(string nodeId, CommandType commandType, string parameters)
    {
        if (!_nodeSettings.TryGetValue(nodeId, out var settings))
        {
            settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _nodeSettings[nodeId] = settings;
        }

        switch (commandType)
        {
            case CommandType.SetThreshold:
            {
                var match = ThresholdPattern.Match(parameters);
                if (match.Success)
                {
                    var value = match.Groups["value"].Value;
                    if (value == "default")
                    {
                        settings.Remove(match.Groups["key"].Value);
                    }
                    else
                    {
                        settings[match.Groups["key"].Value] = value;
                    }
                }

                break;
            }

            case CommandType.Recalibrate:
            {
                var match = OffsetPattern.Match(parameters);
                if (match.Success)
                {
                    settings["offset"] = match.Groups["value"].Value;
                }

                break;
            }

            case CommandType.ToggleActuator:
            {
                var match = ActuatorPattern.Match(parameters);
                if (match.Success)
                {
                    settings[$"relay.{match.Groups["relay"].Value}"] = match.Groups["state"].Value.ToLowerInvariant();
                }

                break;
            }

            case CommandType.FirmwarePush when FirmwarePattern.IsMatch(parameters):
                settings["firmware"] = parameters;
                break;
        }
    }

    /// <summary>Builds, logs and queues a command. Caller holds the lock.</summary>
    private DeviceCommand IssueCommand(
        SensorProfile sensor,
        CommandType commandType,
        string parameters,
        CommandOrigin origin,
        CommandPriority priority,
        string issuedBy,
        bool dryRun,
        DateTime now)
    {
        var command = new DeviceCommand
        {
            Id = Guid.NewGuid(),
            SensorProfileId = sensor.Id,
            NodeId = sensor.NodeId,
            SensorName = sensor.Name,
            Zone = sensor.Zone,
            CommandType = commandType,
            Parameters = parameters,
            Origin = origin,
            Priority = priority,
            Status = CommandStatus.Queued,
            IssuedUtc = now,
            IssuedBy = issuedBy,
            Retries = 0,
            IsDryRun = dryRun
        };

        AppendCommand(command);
        return command;
    }

    /// <summary>Adds a command to the log, the in-flight list and the node's timeline. Caller holds the lock.</summary>
    private void AppendCommand(DeviceCommand command)
    {
        _store.DeviceCommands.Add(command);
        _inFlight.Add(command);

        if (!command.IsDryRun)
        {
            ApplySettings(command.NodeId, command.CommandType, command.Parameters);
        }

        AppendLog(command.NodeId, CommandLogEntry(command));
    }

    private static SensorLogEntry CommandLogEntry(DeviceCommand command) => new()
    {
        TimestampUtc = command.IssuedUtc,
        Kind = SensorLogKind.Command,
        Label = $"{Humanise(command.CommandType.ToString())} {command.Parameters} ({command.IssuedBy}{(command.IsDryRun ? ", dry run" : string.Empty)})"
    };

    // =====================================================================
    // Telemetry intake — Queue<T> [14] and PriorityQueue [15]
    // Code attribution: routine packets use the Enqueue/TryDequeue FIFO members
    // in [14]; critical packets use Enqueue(element, priority) and TryDequeue in
    // [15], with a (rank, arrival ticks) tuple as the priority so ties keep their
    // arrival order. Classifying before queueing, the per-tick budget and the
    // backpressure shedding are additions of this project.
    // =====================================================================

    /// <summary>
    /// Takes in packets posted by a gateway. Each is resolved against the
    /// registry, classified, and routed: routine packets join the FIFO queue and
    /// wait their turn; critical ones go to the priority queue, which is drained
    /// before this call returns.
    /// </summary>
    public PacketIntakeResult IngestPackets(IEnumerable<StreamPacketRequest> packets)
    {
        var result = new PacketIntakeResult();
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            EnsureReady();
            var droppedBefore = _dropped;
            var bypassedBefore = _bypassedStandard;

            foreach (var request in packets)
            {
                result.Received++;

                // O(1) registry lookup, by whichever identifier the gateway sent.
                var device = ResolveDevice(request.NodeId, request.MacAddress);
                if (device is null)
                {
                    result.Rejected.Add($"No registered device matches '{request.NodeId ?? request.MacAddress ?? "(none)"}'.");
                    continue;
                }

                var value = request.Value is double raw && (double.IsNaN(raw) || double.IsInfinity(raw))
                    ? null
                    : request.Value;

                var packet = BuildPacket(device, request.ReadingType, value, request.TimestampUtc ?? now, request.LinkUp, now, "api");

                switch (Enqueue(packet))
                {
                    case IntakeOutcome.Standard:
                        result.QueuedStandard++;
                        break;
                    case IntakeOutcome.Critical:
                        result.ProcessedCritical++;
                        break;
                    default:
                        result.SuppressedDuplicates++;
                        break;
                }
            }

            // "Processed immediately": the critical lane does not wait for the
            // next tick, it is emptied here, ahead of everything already queued.
            result.CriticalAlerts = DrainCriticalLane();
            result.StandardQueueDepth = _standardLane.Count;
            result.DroppedStandard = (int)(_dropped - droppedBefore);
            result.BypassedStandard = (int)(_bypassedStandard - bypassedBefore);
        }

        return result;
    }

    public PipelineStatus GetPipelineStatus(IReadOnlyCollection<string>? knownDisconnected = null)
    {
        lock (_sync)
        {
            EnsureReady();

            return new PipelineStatus
            {
                StandardQueueDepth = _standardLane.Count,
                CriticalQueueDepth = _criticalLane.Count,
                StandardQueueCapacity = MaxStandardQueueDepth,
                StandardBudgetPerTick = StandardBudgetPerTick,
                TotalReceived = _totalReceived,
                StandardProcessed = _standardProcessed,
                CriticalProcessed = _criticalProcessed,
                BypassedStandard = _bypassedStandard,
                LastCriticalBypassed = _lastCriticalBypassed,
                DuplicatesSuppressed = _duplicatesSuppressed,
                Dropped = _dropped,
                AverageStandardWaitMs = Math.Round(_averageStandardWaitMs, 1),
                AverageCriticalWaitMs = Math.Round(_averageCriticalWaitMs, 1),
                RegisteredDevices = _devicesByNode.Count,
                UndoDepth = _overrideHistory.Count,
                RedoDepth = _redoHistory.Count,
                DisconnectedNodes = _disconnectedNodes
                    .Select(nodeId =>
                    {
                        _devicesByNode.TryGetValue(nodeId, out var device);
                        return new DisconnectedNode
                        {
                            NodeId = nodeId,
                            SensorName = device?.Name ?? string.Empty,
                            Zone = device?.Zone ?? string.Empty,
                            SinceUtc = _disconnectedSince.TryGetValue(nodeId, out var since) ? since : null,
                            SuppressedCount = _disconnectRepeats.GetValueOrDefault(nodeId)
                        };
                    })
                    // Most recent drop first: that is the one still worth reacting to.
                    .OrderByDescending(node => node.SinceUtc)
                    .ToList(),
                ErrorStates = _errorStateInfo
                    .Select(pair => new ActiveErrorState
                    {
                        NodeId = pair.Key.NodeId,
                        AlertType = pair.Key.AlertType,
                        ReadingType = pair.Key.ReadingType,
                        Direction = pair.Value.Direction,
                        Severity = pair.Value.Severity,
                        SinceUtc = pair.Value.SinceUtc,
                        SuppressedCount = pair.Value.Suppressed
                    })
                    .OrderByDescending(state => state.Severity)
                    .ThenByDescending(state => state.SinceUtc)
                    .ToList(),
                SetChanges = CompareDisconnected(knownDisconnected),
                RecentAlerts = _recentAlerts.Reverse().ToList(),
                GeneratedUtc = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Set algebra over the disconnected nodes [19] (UnionWith, ExceptWith and
    /// IntersectWith). The caller sends the set it
    /// saw on its last poll; the differences are what is new to it. Every
    /// operation is O(n) over the smaller sets with O(1) membership tests, and
    /// nothing has to be kept per client on the server. Caller holds the lock.
    /// </summary>
    private PipelineSetChanges CompareDisconnected(IReadOnlyCollection<string>? knownDisconnected)
    {
        var changes = new PipelineSetChanges();

        // Needs attention = disconnected ∪ nodes with a critical open breach.
        var attention = new HashSet<string>(_disconnectedNodes, StringComparer.OrdinalIgnoreCase);
        attention.UnionWith(_errorStateInfo
            .Where(pair => pair.Value.Severity == AlertSeverity.Critical)
            .Select(pair => pair.Key.NodeId));
        changes.NeedsAttention = attention.Order(StringComparer.OrdinalIgnoreCase).ToList();

        if (knownDisconnected is null)
        {
            return changes;
        }

        // A client whose previous set was empty sends one blank entry, so it still gets a comparison.
        var known = new HashSet<string>(
            knownDisconnected.Where(nodeId => !string.IsNullOrWhiteSpace(nodeId)),
            StringComparer.OrdinalIgnoreCase);

        // Newly disconnected = current \ known.
        var newly = new HashSet<string>(_disconnectedNodes, StringComparer.OrdinalIgnoreCase);
        newly.ExceptWith(known);

        // Recovered = known \ current.
        var recovered = new HashSet<string>(known, StringComparer.OrdinalIgnoreCase);
        recovered.ExceptWith(_disconnectedNodes);

        // Still down = current ∩ known.
        var still = new HashSet<string>(_disconnectedNodes, StringComparer.OrdinalIgnoreCase);
        still.IntersectWith(known);

        changes.Compared = true;
        changes.NewlyDisconnected = newly.Order(StringComparer.OrdinalIgnoreCase).ToList();
        changes.Recovered = recovered.Order(StringComparer.OrdinalIgnoreCase).ToList();
        changes.StillDisconnected = still.Count;
        return changes;
    }

    /// <summary>Resolves a device by node id first, then MAC address. Caller holds the lock.</summary>
    private SensorProfile? ResolveDevice(string? nodeId, string? macAddress)
    {
        if (!string.IsNullOrWhiteSpace(nodeId) && _devicesByNode.TryGetValue(nodeId.Trim(), out var byNode))
        {
            return byNode;
        }

        // MAC keys are stored canonical (upper case, colon separated), so
        // "5c-a1-..." and "5C:A1:..." reach the same entry.
        if (!string.IsNullOrWhiteSpace(macAddress) &&
            _devicesByMac.TryGetValue(MacAddress.Normalise(macAddress) ?? macAddress.Trim(), out var byMac))
        {
            return byMac;
        }

        return null;
    }

    private StreamPacket BuildPacket(
        SensorProfile device,
        ReadingType readingType,
        double? value,
        DateTime timestampUtc,
        bool linkUp,
        DateTime receivedUtc,
        string source)
    {
        var packet = new StreamPacket
        {
            SensorProfileId = device.Id,
            NodeId = device.NodeId,
            ReadingType = readingType,
            Value = value.HasValue ? Math.Round(value.Value, 2) : null,
            Unit = ReadingTypeProfile.For(readingType).Unit,
            TimestampUtc = timestampUtc,
            ReceivedUtc = receivedUtc,
            LinkUp = linkUp,
            Source = source
        };

        Classify(packet);
        return packet;
    }

    /// <summary>
    /// Decides the lane before the packet is queued. A breach far enough past
    /// its limit — a power spike, a moisture crash — or on a threshold marked
    /// critical goes to the priority queue; so does a lost link.
    /// </summary>
    private void Classify(StreamPacket packet)
    {
        packet.Lane = PacketLane.Standard;

        if (!packet.LinkUp)
        {
            packet.Lane = PacketLane.Critical;
            packet.Severity = AlertSeverity.Critical;
            return;
        }

        if (packet.Value is not double value)
        {
            return;
        }

        var limits = Limits(packet.SensorProfileId, packet.ReadingType);
        if (limits.IsBoolean)
        {
            return;
        }

        var span = Math.Max(limits.Max - limits.Min, 1e-6);

        if (value < limits.Min)
        {
            packet.Breach = BreachDirection.Low;
            packet.Limit = limits.Min;
            packet.BreachMargin = (limits.Min - value) / span;
        }
        else if (value > limits.Max)
        {
            packet.Breach = BreachDirection.High;
            packet.Limit = limits.Max;
            packet.BreachMargin = (value - limits.Max) / span;
        }
        else
        {
            return;
        }

        packet.Severity = packet.BreachMargin >= CriticalMargin || limits.Severity == AlertSeverity.Critical
            ? AlertSeverity.Critical
            : AlertSeverity.Warning;

        if (packet.Severity == AlertSeverity.Critical)
        {
            packet.Lane = PacketLane.Critical;
        }
    }

    /// <summary>Routes a classified packet into its lane. Caller holds the lock.</summary>
    private IntakeOutcome Enqueue(StreamPacket packet)
    {
        _totalReceived++;

        // A node already in the disconnected set has had its alert. Another
        // "link lost" report for it is recognised by one hash probe and never
        // enters a queue at all.
        if (!packet.LinkUp && _disconnectedNodes.Contains(packet.NodeId))
        {
            _duplicatesSuppressed++;
            _disconnectRepeats[packet.NodeId] = _disconnectRepeats.GetValueOrDefault(packet.NodeId) + 1;
            return IntakeOutcome.Suppressed;
        }

        if (packet.Lane == PacketLane.Critical)
        {
            // Largest breach first. A lost link ranks behind any live breach: a
            // node we can still hear spiking is the more urgent of the two.
            var rank = packet.LinkUp ? -(int)Math.Round(packet.BreachMargin * 1000) : 0;
            _criticalLane.Enqueue(packet, (rank, packet.ReceivedUtc.Ticks));
            return IntakeOutcome.Critical;
        }

        _standardLane.Enqueue(packet);

        // Shed the oldest routine data rather than grow without bound. The
        // critical lane is never shed.
        while (_standardLane.Count > MaxStandardQueueDepth)
        {
            _standardLane.Dequeue();
            _dropped++;
        }

        return IntakeOutcome.Standard;
    }

    /// <summary>Empties the priority queue, worst first. Caller holds the lock.</summary>
    private List<StreamAlert> DrainCriticalLane()
    {
        var raised = new List<StreamAlert>();

        while (_criticalLane.TryDequeue(out var packet, out _))
        {
            // Everything still in the standard queue arrived before or alongside
            // this packet, and is now being overtaken by it.
            _lastCriticalBypassed = _standardLane.Count;
            _bypassedStandard += _standardLane.Count;

            var alert = Process(packet);
            if (alert is not null)
            {
                raised.Add(alert);
            }
        }

        return raised;
    }

    /// <summary>Processes up to <paramref name="budget"/> routine packets in arrival order. Caller holds the lock.</summary>
    private void DrainStandardLane(int budget)
    {
        for (var processed = 0; processed < budget && _standardLane.TryDequeue(out var packet); processed++)
        {
            Process(packet);
        }
    }

    /// <summary>
    /// Applies one packet: logs the reading, and updates the error-state and
    /// disconnected sets — raising an alert only when a set actually changes.
    /// Caller holds the lock.
    /// </summary>
    private StreamAlert? Process(StreamPacket packet)
    {
        var processedUtc = DateTime.UtcNow;
        var waitMs = Math.Max(0, (processedUtc - packet.ReceivedUtc).TotalMilliseconds);

        if (packet.Lane == PacketLane.Critical)
        {
            _criticalProcessed++;
            _averageCriticalWaitMs = _criticalProcessed == 1
                ? waitMs
                : _averageCriticalWaitMs + WaitSmoothing * (waitMs - _averageCriticalWaitMs);
        }
        else
        {
            _standardProcessed++;
            _averageStandardWaitMs = _standardProcessed == 1
                ? waitMs
                : _averageStandardWaitMs + WaitSmoothing * (waitMs - _averageStandardWaitMs);
        }

        if (!_devicesById.TryGetValue(packet.SensorProfileId, out var device))
        {
            return null;
        }

        var nodeId = device.NodeId;

        // ---- Link state: HashSet<string> of disconnected nodes [19] ----
        if (!packet.LinkUp)
        {
            // Add returns false when the node is already in the set: that is a
            // duplicate disconnect, and it is dropped without a second alert.
            if (!_disconnectedNodes.Add(nodeId))
            {
                _duplicatesSuppressed++;
                _disconnectRepeats[nodeId] = _disconnectRepeats.GetValueOrDefault(nodeId) + 1;
                return null;
            }

            _disconnectedSince[nodeId] = processedUtc;
            var offline = RaiseAlert(device, packet, AlertType.DeviceOffline, AlertSeverity.Critical,
                $"{nodeId} dropped off the mesh — the gateway lost its link.", processedUtc, waitMs,
                SensorLogKind.Disconnected);
            RegisterTrigger(nodeId, "Offline", processedUtc);
            return offline;
        }

        if (_disconnectedNodes.Remove(nodeId))
        {
            _disconnectedSince.Remove(nodeId);
            _disconnectRepeats.Remove(nodeId);
            AppendLog(nodeId, new SensorLogEntry
            {
                TimestampUtc = packet.TimestampUtc,
                Kind = SensorLogKind.Reconnected,
                Label = "Link restored"
            });
        }

        if (packet.Value is not double value)
        {
            return null;
        }

        AppendLog(nodeId, new SensorLogEntry
        {
            TimestampUtc = packet.TimestampUtc,
            Kind = SensorLogKind.Reading,
            ReadingType = packet.ReadingType,
            Value = value
        });

        // ---- Error states: HashSet<ErrorStateKey> [19] ----
        var key = new ErrorStateKey(nodeId, AlertType.ThresholdBreach, packet.ReadingType);

        if (packet.Breach is not BreachDirection direction)
        {
            // Back in range: the error state clears, so the next breach is new.
            if (_activeErrorStates.Remove(key))
            {
                _errorStateInfo.Remove(key);
                AppendLog(nodeId, new SensorLogEntry
                {
                    TimestampUtc = packet.TimestampUtc,
                    Kind = SensorLogKind.Recovered,
                    ReadingType = packet.ReadingType,
                    Value = value,
                    Label = $"{packet.ReadingType} back within limits"
                });
            }

            return null;
        }

        var severity = packet.Severity ?? AlertSeverity.Warning;
        StreamAlert? alert = null;

        if (_activeErrorStates.Add(key))
        {
            _errorStateInfo[key] = new ErrorStateInfo(processedUtc, severity, direction);
            alert = RaiseAlert(device, packet, AlertType.ThresholdBreach, severity,
                DescribeBreach(packet, value), processedUtc, waitMs, SensorLogKind.Alert);
            RegisterTrigger(nodeId, Condition(packet.ReadingType, direction), processedUtc);
        }
        else if (severity > _errorStateInfo[key].Severity)
        {
            // Already breaching, but worse now: an escalation is new information.
            _errorStateInfo[key] = _errorStateInfo[key] with { Severity = severity };
            alert = RaiseAlert(device, packet, AlertType.ThresholdBreach, severity,
                $"Escalated: {DescribeBreach(packet, value)}", processedUtc, waitMs, SensorLogKind.Alert);
        }
        else
        {
            // Same breach, still going. The reading is logged above; the alert is not repeated.
            _duplicatesSuppressed++;
            _errorStateInfo[key] = _errorStateInfo[key] with { Suppressed = _errorStateInfo[key].Suppressed + 1 };
        }

        if (alert is not null && severity == AlertSeverity.Critical)
        {
            RespondToCritical(device, packet, alert, processedUtc);
        }

        return alert;
    }

    private StreamAlert RaiseAlert(
        SensorProfile device,
        StreamPacket packet,
        AlertType alertType,
        AlertSeverity severity,
        string message,
        DateTime processedUtc,
        double waitMs,
        SensorLogKind logKind)
    {
        var alert = new StreamAlert
        {
            NodeId = device.NodeId,
            SensorName = device.Name,
            Zone = device.Zone,
            AlertType = alertType,
            ReadingType = packet.LinkUp ? packet.ReadingType : null,
            Value = packet.Value,
            Limit = packet.Limit,
            Unit = packet.Unit,
            Severity = severity,
            Lane = packet.Lane,
            Message = message,
            ReceivedUtc = packet.ReceivedUtc,
            ProcessedUtc = processedUtc,
            QueueWaitMs = (int)Math.Round(waitMs)
        };

        _recentAlerts.Enqueue(alert);
        while (_recentAlerts.Count > RecentAlertCapacity)
        {
            _recentAlerts.Dequeue();
        }

        AppendLog(device.NodeId, new SensorLogEntry
        {
            TimestampUtc = packet.TimestampUtc,
            Kind = logKind,
            ReadingType = alert.ReadingType,
            Value = packet.Value,
            Label = message,
            Severity = severity
        });

        return alert;
    }

    /// <summary>
    /// The critical lane acts as well as alerts: an actuator that spikes is
    /// switched off, anything else is asked for a confirming sample — issued at
    /// Immediate so it overtakes the queue as the packet did.
    /// </summary>
    private void RespondToCritical(SensorProfile device, StreamPacket packet, StreamAlert alert, DateTime now)
    {
        (CommandType Type, string Parameters)? response =
            packet.Breach == BreachDirection.High && CommandGenerator.Supports(device.Category, CommandType.ToggleActuator)
                ? (CommandType.ToggleActuator, "relay=1,state=off")
                : CommandGenerator.Supports(device.Category, CommandType.RequestSample)
                    ? (CommandType.RequestSample, "count=5")
                    : null;

        if (response is not { } chosen || IsInFlight(device.NodeId, chosen.Type))
        {
            return;
        }

        var command = IssueCommand(
            device, chosen.Type, chosen.Parameters, CommandOrigin.Automation,
            CommandPriority.Immediate, "critical-lane", dryRun: false, now);

        alert.AutoCommandId = command.Id;
        alert.AutoCommandSummary = $"{Humanise(chosen.Type.ToString())} {chosen.Parameters}";
    }

    private static string DescribeBreach(StreamPacket packet, double value)
    {
        var direction = packet.Breach == BreachDirection.Low ? "below" : "above";
        return $"{packet.ReadingType} {value:0.##} {packet.Unit} is {direction} the {packet.Limit:0.##} {packet.Unit} limit";
    }

    // =====================================================================
    // Sorted sensor logs — SortedList [18]
    // Code attribution: each node's log is a SortedList<TKey,TValue> as in [18],
    // read through its indexed Keys collection. The LowerBound binary search over
    // those keys, the tick nudge on duplicate timestamps and the retention trim
    // from the front are additions of this project.
    // =====================================================================

    /// <summary>
    /// A node's log over the window, read out of its SortedList already in
    /// timestamp order — including packets that arrived late, which were slotted
    /// into place on insert rather than appended out of sequence.
    /// </summary>
    public NodeTimeline? GetNodeTimeline(string nodeId, int minutes, int maxPoints)
    {
        var window = Math.Clamp(minutes, 5, LogRetentionHours * 60);
        var pointBudget = Math.Clamp(maxPoints, 10, 500);
        var now = DateTime.UtcNow;
        var from = now.AddMinutes(-window);

        SensorProfile device;
        List<SensorLogEntry> entries;
        int logSize;
        var skipped = 0;
        TimeSpan rangeRead;
        bool disconnected;
        var limits = new Dictionary<ReadingType, LimitSet>();

        lock (_sync)
        {
            EnsureReady();

            if (!_devicesByNode.TryGetValue(nodeId.Trim(), out var found))
            {
                return null;
            }

            device = found;
            disconnected = _disconnectedNodes.Contains(device.NodeId);

            var started = Stopwatch.GetTimestamp();

            if (_sensorLogs.TryGetValue(device.NodeId, out var log))
            {
                logSize = log.Count;

                // Range read: binary-search to the first entry inside the window,
                // then copy forward. Older entries are skipped without being visited.
                skipped = LowerBound(log, from);
                var values = log.Values;
                entries = new List<SensorLogEntry>(log.Count - skipped);
                for (var index = skipped; index < values.Count; index++)
                {
                    entries.Add(values[index]);
                }
            }
            else
            {
                logSize = 0;
                entries = new List<SensorLogEntry>();
            }

            rangeRead = Stopwatch.GetElapsedTime(started);

            foreach (var readingType in ReadingTypeProfile.ForCategory(device.Category))
            {
                limits[readingType] = Limits(device.Id, readingType);
            }
        }

        var series = new List<TimelineSeries>();
        foreach (var readingType in ReadingTypeProfile.ForCategory(device.Category))
        {
            var points = entries
                .Where(entry => entry.Kind == SensorLogKind.Reading && entry.ReadingType == readingType && entry.Value.HasValue)
                .ToList();

            var limit = limits[readingType];
            series.Add(new TimelineSeries
            {
                ReadingType = readingType,
                Unit = ReadingTypeProfile.For(readingType).Unit,
                MinThreshold = limit.IsBoolean ? null : limit.Min,
                MaxThreshold = limit.IsBoolean ? null : limit.Max,
                Points = Downsample(points, from, now, pointBudget)
            });
        }

        var events = entries
            .Where(entry => entry.Kind != SensorLogKind.Reading)
            .ToList();

        return new NodeTimeline
        {
            NodeId = device.NodeId,
            SensorName = device.Name,
            Zone = device.Zone,
            WindowMinutes = window,
            FromUtc = from,
            ToUtc = now,
            LogSize = logSize,
            IsDisconnected = disconnected,
            EntriesInWindow = entries.Count,
            EntriesSkipped = skipped,
            RangeReadMicroseconds = Math.Round(rangeRead.TotalMicroseconds, 2),
            Series = series,
            Events = events.Skip(Math.Max(0, events.Count - MaxTimelineEvents)).ToList()
        };
    }

    /// <summary>
    /// The live device panel: every registered device that matches, each with
    /// its latest value per metric and a short trail behind it. The registry is
    /// small enough (tens of nodes, not thousands) that all matches are returned
    /// in one response, so the panel shows the whole fleet rather than a page of it.
    /// </summary>
    public LiveDeviceResult GetLiveDevices(DeviceQuery query)
    {
        var now = DateTime.UtcNow;
        var devices = new List<LiveDevice>();
        var sensorIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        Dictionary<Guid, NodeAlertContext> liveContexts;

        lock (_sync)
        {
            EnsureReady();
            liveContexts = BuildLiveAlertContexts();

            foreach (var device in _devicesById.Values)
            {
                sensorIds[device.NodeId] = device.Id;
                devices.Add(BuildLiveDevice(device));
            }
        }

        // Same alert picture the command filter uses, so a device and the
        // commands sent to it can never disagree about whether it is alerting.
        var alertContexts = BuildAlertContexts(liveContexts);
        foreach (var device in devices)
        {
            var context = sensorIds.TryGetValue(device.NodeId, out var id) && alertContexts.TryGetValue(id, out var found)
                ? found
                : NodeAlertContext.None;

            device.AlertState = context.State;
            device.AlertSeverity = context.Severity;
            device.OpenAlertCount = context.OpenCount;
        }

        // Each facet is counted with every other filter applied but its own, so
        // a category chip says how many devices selecting it would bring in.
        var forCategoryCounts = devices.Where(device => MatchesDevice(device, query, skipCategories: true, skipAlertStates: false)).ToList();
        var forAlertCounts = devices.Where(device => MatchesDevice(device, query, skipCategories: false, skipAlertStates: true)).ToList();

        var matches = devices
            .Where(device => MatchesDevice(device, query, skipCategories: false, skipAlertStates: false))
            // Worst first: the device most in need of an override leads.
            .OrderByDescending(device => device.AlertState)
            .ThenByDescending(device => device.AlertSeverity ?? (AlertSeverity)(-1))
            .ThenByDescending(device => device.IsDisconnected)
            .ThenBy(device => device.NodeId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new LiveDeviceResult
        {
            Items = matches,
            TotalRegistered = devices.Count,
            CategoryCounts = Enum.GetValues<SensorCategory>().ToDictionary(
                category => category.ToString(),
                category => forCategoryCounts.Count(device => device.Category == category)),
            AlertStateCounts = Enum.GetValues<NodeAlertState>().ToDictionary(
                state => state.ToString(),
                state => forAlertCounts.Count(device => device.AlertState == state)),
            GeneratedUtc = now
        };
    }

    /// <summary>
    /// Exact lookup by node id, then by MAC address, against the registry
    /// dictionaries [17]. The probes are timed on their own, so the panel can
    /// show that finding one device in the registry costs the same however
    /// many are registered.
    /// </summary>
    public DeviceLookupResult LookupDevice(string key)
    {
        var trimmed = key?.Trim() ?? string.Empty;
        var result = new DeviceLookupResult { Key = trimmed, NormalisedKey = trimmed };
        Dictionary<Guid, NodeAlertContext> liveContexts;
        LiveDevice liveDevice;
        Guid sensorId;

        lock (_sync)
        {
            EnsureReady();

            var started = Stopwatch.GetTimestamp();
            SensorProfile? device = null;

            if (_devicesByNode.TryGetValue(trimmed, out var byNode))
            {
                device = byNode;
                result.MatchedBy = "NodeId";
                result.NormalisedKey = byNode.NodeId;
                result.RegistrySize = _devicesByNode.Count;
            }
            else if (MacAddress.Normalise(trimmed) is string mac)
            {
                result.NormalisedKey = mac;
                result.RegistrySize = _devicesByMac.Count;
                if (_devicesByMac.TryGetValue(mac, out var byMac))
                {
                    device = byMac;
                    result.MatchedBy = "MacAddress";
                }
            }
            else
            {
                result.RegistrySize = _devicesByNode.Count;
            }

            result.ElapsedMicroseconds = Math.Round(Stopwatch.GetElapsedTime(started).TotalMicroseconds, 3);
            result.Found = device is not null;

            if (device is null)
            {
                return result;
            }

            liveDevice = BuildLiveDevice(device);
            sensorId = device.Id;
            liveContexts = BuildLiveAlertContexts();
        }

        // Same alert picture as the live panel, worked out outside the lock as there.
        var context = BuildAlertContexts(liveContexts).TryGetValue(sensorId, out var found)
            ? found
            : NodeAlertContext.None;

        liveDevice.AlertState = context.State;
        liveDevice.AlertSeverity = context.Severity;
        liveDevice.OpenAlertCount = context.OpenCount;
        result.Device = liveDevice;
        return result;
    }

    /// <summary>One device's live card: latest value and trail per metric. Caller holds the lock.</summary>
    private LiveDevice BuildLiveDevice(SensorProfile device)
    {
        var readings = new List<LiveReading>();
        DateTime? lastReading = null;

        foreach (var readingType in ReadingTypeProfile.ForCategory(device.Category))
        {
            var limits = Limits(device.Id, readingType);
            var reading = new LiveReading
            {
                ReadingType = readingType,
                Unit = ReadingTypeProfile.For(readingType).Unit,
                MinThreshold = limits.IsBoolean ? null : limits.Min,
                MaxThreshold = limits.IsBoolean ? null : limits.Max,
                IsBoolean = limits.IsBoolean
            };

            // One hash probe for the node's window on this metric.
            if (_recentReadings.TryGetValue((device.NodeId, readingType), out var recent) && recent.Count > 0)
            {
                var latest = recent.Last();
                reading.Value = latest.Value;
                reading.TimestampUtc = latest.TimestampUtc;
                reading.OutOfRange = !limits.IsBoolean && (latest.Value < limits.Min || latest.Value > limits.Max);
                reading.Recent = recent.Select(point => point.Value).ToList();

                if (lastReading is null || latest.TimestampUtc > lastReading)
                {
                    lastReading = latest.TimestampUtc;
                }
            }

            readings.Add(reading);
        }

        return new LiveDevice
        {
            NodeId = device.NodeId,
            SensorName = device.Name,
            MacAddress = device.MacAddress,
            Category = device.Category,
            Zone = device.Zone,
            Room = device.Room,
            IsDisconnected = !IsReachable(device),
            LastReadingUtc = lastReading,
            Readings = readings
        };
    }

    private static bool MatchesDevice(LiveDevice device, DeviceQuery query, bool skipCategories, bool skipAlertStates)
    {
        if (!skipCategories && query.SensorCategories is { Count: > 0 } && !query.SensorCategories.Contains(device.Category))
        {
            return false;
        }

        if (!skipAlertStates && query.AlertStates is { Count: > 0 } && !query.AlertStates.Contains(device.AlertState))
        {
            return false;
        }

        if (query.MinAlertSeverity.HasValue &&
            (!device.AlertSeverity.HasValue || device.AlertSeverity.Value < query.MinAlertSeverity.Value))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Zone) &&
            !string.Equals(device.Zone, query.Zone, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query.Search))
        {
            return true;
        }

        var term = query.Search.Trim();
        return device.NodeId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               device.SensorName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               device.Zone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               device.Room.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               device.MacAddress.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               MatchesLabel(device.Category.ToString(), term) ||
               MatchesLabel(device.AlertState.ToString(), term);
    }

    /// <summary>
    /// Adds an entry to a node's sorted log. Two entries can share a timestamp
    /// (a gateway flushes several metrics at once), so a collision is nudged
    /// forward a tick rather than overwriting. Caller holds the lock.
    /// </summary>
    private void AppendLog(string nodeId, SensorLogEntry entry)
    {
        if (!_sensorLogs.TryGetValue(nodeId, out var log))
        {
            log = new SortedList<DateTime, SensorLogEntry>();
            _sensorLogs[nodeId] = log;
        }

        var key = entry.TimestampUtc;
        while (log.ContainsKey(key))
        {
            key = key.AddTicks(1);
        }

        log.Add(key, entry);
        TrackRecent(nodeId, entry);

        // The smallest key is the oldest entry, so retention trims from the front.
        var cutoff = DateTime.UtcNow.AddHours(-LogRetentionHours);
        while (log.Count > 0)
        {
            if (log.Count <= MaxLogEntriesPerNode && log.Keys[0] >= cutoff)
            {
                break;
            }

            log.RemoveAt(0);
        }
    }

    /// <summary>
    /// Index of the first entry at or after <paramref name="from"/>, by binary
    /// search over the sorted keys: O(log n). Everything before it is older than
    /// the window and is never touched.
    /// </summary>
    private static int LowerBound(SortedList<DateTime, SensorLogEntry> log, DateTime from)
    {
        var keys = log.Keys;
        var low = 0;
        var high = keys.Count;

        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (keys[middle] < from)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    /// <summary>
    /// Pushes a reading onto its node and metric's recent window, dropping the
    /// oldest once the window is full. A packet that arrives late belongs in the
    /// sorted log but not at the end of a "latest" trail, so it is skipped here.
    /// Caller holds the lock.
    /// </summary>
    private void TrackRecent(string nodeId, SensorLogEntry entry)
    {
        if (entry.Kind != SensorLogKind.Reading || entry.ReadingType is not ReadingType readingType || entry.Value is not double value)
        {
            return;
        }

        var key = (nodeId, readingType);
        if (!_recentReadings.TryGetValue(key, out var recent))
        {
            recent = new Queue<TimelinePoint>(RecentReadingDepth + 1);
            _recentReadings[key] = recent;
        }
        else if (recent.Count > 0 && recent.Last().TimestampUtc > entry.TimestampUtc)
        {
            return;
        }

        recent.Enqueue(new TimelinePoint { TimestampUtc = entry.TimestampUtc, Value = value });
        while (recent.Count > RecentReadingDepth)
        {
            recent.Dequeue();
        }
    }

    /// <summary>
    /// Refills every recent window from the sorted logs. Run once after the
    /// start-up back-fill, which arrives in seed order rather than time order:
    /// each log already iterates oldest first, so this is one forward pass per
    /// node with no sort. Caller holds the lock.
    /// </summary>
    private void RebuildRecentReadings()
    {
        _recentReadings.Clear();

        foreach (var (nodeId, log) in _sensorLogs)
        {
            foreach (var entry in log.Values)
            {
                TrackRecent(nodeId, entry);
            }
        }
    }

    /// <summary>
    /// Averages the points into fixed time buckets. The input is already in
    /// order, so this is one forward pass with no sort.
    /// </summary>
    private static List<TimelinePoint> Downsample(List<SensorLogEntry> points, DateTime from, DateTime to, int budget)
    {
        if (points.Count <= budget)
        {
            return points
                .Select(point => new TimelinePoint { TimestampUtc = point.TimestampUtc, Value = point.Value!.Value })
                .ToList();
        }

        var bucketTicks = Math.Max(1, (to - from).Ticks / budget);
        var result = new List<TimelinePoint>(budget);

        var currentBucket = -1L;
        var sum = 0d;
        var count = 0;
        var bucketStart = from;

        foreach (var point in points)
        {
            var bucket = (point.TimestampUtc - from).Ticks / bucketTicks;
            if (bucket != currentBucket && count > 0)
            {
                result.Add(new TimelinePoint { TimestampUtc = bucketStart, Value = Math.Round(sum / count, 3) });
                sum = 0;
                count = 0;
            }

            if (bucket != currentBucket)
            {
                currentBucket = bucket;
                bucketStart = from.AddTicks(bucket * bucketTicks + bucketTicks / 2);
            }

            sum += point.Value!.Value;
            count++;
        }

        if (count > 0)
        {
            result.Add(new TimelinePoint { TimestampUtc = bucketStart, Value = Math.Round(sum / count, 3) });
        }

        return result;
    }

    // =====================================================================
    // Live loop
    // =====================================================================

    public void RunDispatchCycle()
    {
        var now = DateTime.UtcNow;

        // The whole tick runs under the lock: requests take copies, so holding
        // it for one pass is cheaper than fighting over each structure.
        lock (_sync)
        {
            EnsureReady();

            AdvanceInFlight(now);
            IssueAutomatedTraffic(now);

            // Telemetry is not generated here: it arrives over HTTP from the
            // device simulator, through the same intake endpoint a real device
            // posts to. This tick only drains what has arrived since the last.
            // Critical first, always; then the routine queue, as far as the
            // budget allows. A burst backs the FIFO up and it drains over the
            // following ticks, while critical packets never wait.
            DrainCriticalLane();
            DrainStandardLane(StandardBudgetPerTick);

            TrimCommandLog();
        }
    }

    private void AdvanceInFlight(DateTime now)
    {
        foreach (var command in _inFlight)
        {
            if (command.Status is CommandStatus.Queued or CommandStatus.Sent)
            {
                CommandGenerator.Advance(_random, command, now);
            }
        }

        _inFlight.RemoveAll(command => command.Status is not (CommandStatus.Queued or CommandStatus.Sent));
    }

    private void IssueAutomatedTraffic(DateTime now)
    {
        var targets = _devicesById.Values.Where(IsReachable).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var _ in Enumerable.Range(0, IssueCount()))
        {
            var sensor = targets[_random.Next(targets.Count)];
            AppendCommand(CommandGenerator.Compose(_random, sensor, now));
        }
    }

    /// <summary>
    /// How many automated commands this tick issues. Occasional bursts on top of
    /// the base rate, so the throughput strip has peaks worth looking at.
    /// </summary>
    private int IssueCount()
    {
        if (_random.NextDouble() < 0.06)
        {
            return _random.Next(3, 8);
        }

        return _random.NextDouble() < CommandsPerTick ? 1 : 0;
    }

    // =====================================================================
    // Device emulation
    // =====================================================================

    /// <summary>
    /// Stands in for the mesh: a handful of devices report each tick, the flaky
    /// ones more often and with sustained excursions, a gateway reports nodes
    /// it has lost, and now and then a gateway flushes a backlog in one burst.
    /// <para>
    /// This only composes what each device would say. None of it enters the
    /// intake from here — <see cref="DeviceTelemetrySimulator"/> posts every
    /// transmission to <c>POST /api/commands/packets</c>, so emulated traffic
    /// takes exactly the path a real ESP32 would.
    /// </para>
    /// </summary>
    public IReadOnlyList<DeviceTransmission> ComposeDeviceTransmissions()
    {
        var now = DateTime.UtcNow;
        var transmissions = new List<DeviceTransmission>();

        lock (_sync)
        {
            EnsureReady();

            var reachable = _devicesById.Values.Where(IsReachable).ToList();
            if (reachable.Count == 0)
            {
                return transmissions;
            }

            var gatewayReports = new List<StreamPacketRequest>();
            var reporting = new HashSet<Guid>();

            foreach (var nodeId in _flakyNodes)
            {
                if (!_devicesByNode.TryGetValue(nodeId, out var flaky))
                {
                    continue;
                }

                if (flaky.Category == SensorCategory.Connectivity && StepOutage(flaky, now, gatewayReports))
                {
                    continue;
                }

                if (_random.NextDouble() < 0.5 && IsReachable(flaky))
                {
                    reporting.Add(flaky.Id);
                }
            }

            var target = Math.Min(reachable.Count, reporting.Count + ReportingNodesPerTick);
            while (reporting.Count < target)
            {
                reporting.Add(reachable[_random.Next(reachable.Count)].Id);
            }

            // One request per device, identified by its MAC address the way an
            // ESP32 announces itself — the intake resolves it through the MAC
            // dictionary.
            foreach (var sensorId in reporting)
            {
                var device = _devicesById[sensorId];
                transmissions.Add(new DeviceTransmission
                {
                    Sender = device.NodeId,
                    Packets = ComposeReadings(device, now)
                });
            }

            if (gatewayReports.Count > 0)
            {
                transmissions.Add(new DeviceTransmission { Sender = "gateway", Packets = gatewayReports });
            }

            // A gateway flushing its buffer: routine data only, but enough of it
            // to back the FIFO up past a single tick's budget.
            if (_random.NextDouble() < 0.05)
            {
                var burst = _random.Next(25, 46);
                var flush = new List<StreamPacketRequest>(burst);
                for (var index = 0; index < burst; index++)
                {
                    var device = reachable[_random.Next(reachable.Count)];
                    var readingTypes = ReadingTypeProfile.ForCategory(device.Category);
                    var readingType = readingTypes[_random.Next(readingTypes.Length)];
                    flush.Add(new StreamPacketRequest
                    {
                        NodeId = device.NodeId,
                        ReadingType = readingType,
                        Value = RoutineValue(readingType, now),
                        TimestampUtc = now.AddSeconds(-_random.Next(1, 60))
                    });
                }

                transmissions.Add(new DeviceTransmission { Sender = "gateway", Packets = flush });
            }
        }

        return transmissions;
    }

    /// <summary>
    /// Advances a flaky link's outage. While it lasts the gateway keeps
    /// reporting the node lost every tick — the duplicates the disconnected set
    /// exists to absorb. Returns true while the node is down.
    /// </summary>
    private bool StepOutage(SensorProfile device, DateTime now, List<StreamPacketRequest> gatewayReports)
    {
        if (_outageTicks.TryGetValue(device.NodeId, out var remaining))
        {
            if (remaining <= 0)
            {
                _outageTicks.Remove(device.NodeId);
                return false;
            }

            _outageTicks[device.NodeId] = remaining - 1;
            gatewayReports.Add(new StreamPacketRequest
            {
                NodeId = device.NodeId,
                ReadingType = ReadingType.Temperature,
                Value = null,
                TimestampUtc = now,
                LinkUp = false
            });
            return true;
        }

        if (!_disconnectedNodes.Contains(device.NodeId) && _random.NextDouble() < 0.02)
        {
            _outageTicks[device.NodeId] = _random.Next(6, 16);
        }

        return false;
    }

    /// <summary>One reading per metric the device carries, addressed by its MAC.</summary>
    private List<StreamPacketRequest> ComposeReadings(SensorProfile device, DateTime now)
    {
        var isFlaky = _flakyNodes.Contains(device.NodeId);
        var packets = new List<StreamPacketRequest>();

        foreach (var readingType in ReadingTypeProfile.ForCategory(device.Category))
        {
            var profile = ReadingTypeProfile.For(readingType);
            double value;

            if (profile.IsBoolean)
            {
                value = _random.NextDouble() < 0.2 ? 1 : 0;
            }
            else
            {
                value = ExcursionValue(device, readingType, isFlaky) ?? RoutineValue(readingType, now);
            }

            packets.Add(new StreamPacketRequest
            {
                // Fall back to the node id for a device registered without a MAC.
                MacAddress = string.IsNullOrWhiteSpace(device.MacAddress) ? null : device.MacAddress,
                NodeId = string.IsNullOrWhiteSpace(device.MacAddress) ? device.NodeId : null,
                ReadingType = readingType,
                Value = Math.Round(value, 3),
                TimestampUtc = now
            });
        }

        return packets;
    }

    /// <summary>
    /// The value during an excursion, or null when there is none. An excursion
    /// is a spell rather than a blip — it holds for several ticks — which is
    /// what makes repeat breaches, and so duplicate suppression, happen.
    /// </summary>
    private double? ExcursionValue(SensorProfile device, ReadingType readingType, bool isFlaky)
    {
        var key = (device.NodeId, readingType);
        var limits = Limits(device.Id, readingType);
        var span = limits.Max - limits.Min;

        if (!_excursions.TryGetValue(key, out var excursion))
        {
            var chance = isFlaky ? 0.08 : 0.004;
            if (_random.NextDouble() >= chance)
            {
                return null;
            }

            // Flaky hardware fails in character: soil moisture crashes, meters
            // spike, motors run hot and shake.
            var direction = readingType switch
            {
                ReadingType.Humidity => BreachDirection.Low,
                ReadingType.Power or ReadingType.Temperature or ReadingType.Vibration => BreachDirection.High,
                _ => _random.NextDouble() < 0.5 ? BreachDirection.Low : BreachDirection.High
            };

            excursion = new Excursion(direction, 0.05 + _random.NextDouble() * 0.45, _random.Next(3, 11));
        }

        if (excursion.TicksLeft <= 0)
        {
            _excursions.Remove(key);
            return null;
        }

        _excursions[key] = excursion with { TicksLeft = excursion.TicksLeft - 1 };

        var margin = Math.Max(0.01, excursion.Margin + Gaussian() * 0.03);
        var value = excursion.Direction == BreachDirection.Low
            ? limits.Min - margin * span
            : limits.Max + margin * span;

        return Math.Max(0, value);
    }

    /// <summary>A normal reading: the daily cycle plus noise, well inside the limits.</summary>
    private double RoutineValue(ReadingType readingType, DateTime now)
    {
        var profile = ReadingTypeProfile.For(readingType);
        if (profile.IsBoolean)
        {
            return _random.NextDouble() < 0.2 ? 1 : 0;
        }

        var dayPhase = (now.TimeOfDay.TotalHours - 9) / 24 * 2 * Math.PI;
        var value = profile.BaseValue + Math.Sin(dayPhase) * profile.DailySwing * 0.5 + Gaussian() * profile.Noise;
        return Math.Max(0, value);
    }

    /// <summary>Standard normal sample (Box–Muller).</summary>
    private double Gaussian()
    {
        var u1 = 1.0 - _random.NextDouble();
        var u2 = _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>Drops the oldest commands once the log outgrows its cap.</summary>
    private void TrimCommandLog()
    {
        var excess = _store.DeviceCommands.Count - _options.MaxCommandLogSize;
        if (excess > 0)
        {
            // Issue order, so the oldest are at the front.
            _store.DeviceCommands.RemoveRange(0, excess);
        }
    }

    // =====================================================================
    // Automated action engine [20], [21], [22]
    // Code attribution: rule suggestions keep a condition => action rule only
    // when it meets the support and confidence thresholds defined in [20];
    // next-step suggestions use the bigram estimate count(prev -> next) /
    // count(prev -> *) from [21]; problem devices are scored with Welford's
    // running mean and variance [22]. Candidates are ranked with a
    // PriorityQueue [15]. The thresholds, sliding windows and operator feedback
    // weighting are additions of this project.
    // =====================================================================

    public void RecordActivity(OperatorActivityRequest request)
    {
        var value = request.Value?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return;
        }

        var issuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? "operator" : request.IssuedBy.Trim();

        lock (_sync)
        {
            EnsureReady();
            var now = DateTime.UtcNow;

            if (request.Kind == OperatorActivityKind.SelectNode)
            {
                if (_devicesByNode.TryGetValue(value, out var selected))
                {
                    RecordAction(issuedBy, ActionToken("select", string.Empty, selected.NodeId), selected.NodeId, now);
                }

                return;
            }

            if (request.Kind == OperatorActivityKind.Filter)
            {
                // "facet:value", e.g. "zone:Zone B". Kept as typed; the token
                // separator is the only character that cannot appear in it.
                var filter = value.Replace('|', '/');
                if (filter.Contains(':'))
                {
                    RecordAction(issuedBy, ActionToken("filter", filter, null), null, now);
                }

                return;
            }

            // A search that names a node — by id or by name — is about that
            // node, which is what lets the engine connect it to the node's alerts.
            var named = ResolveDevice(value, null)
                ?? _devicesById.Values.FirstOrDefault(device =>
                    string.Equals(device.Name, value, StringComparison.OrdinalIgnoreCase));

            RecordAction(issuedBy, ActionToken("search", value.ToLowerInvariant(), named?.NodeId), named?.NodeId, now);
        }
    }

    /// <summary>
    /// Builds the suggestion list. Three independent sources feed it, and a
    /// bounded min-heap keeps only the best-scoring handful:
    /// <list type="number">
    ///   <item><description>association rules — actions that usually follow a condition that is active now;</description></item>
    ///   <item><description>a Markov chain — what usually follows the operator's own last action;</description></item>
    ///   <item><description>anomaly scoring — nodes whose recent behaviour looks faulty.</description></item>
    /// </list>
    /// </summary>
    public InsightsResponse GetInsights(string? issuedBy)
    {
        lock (_sync)
        {
            EnsureReady();
            var now = DateTime.UtcNow;

            // Keyed on what the suggestion does, so two sources recommending the
            // same command on the same node collapse into the stronger one.
            var candidates = new Dictionary<string, SuggestedAction>();
            var triggers = ActiveTriggers(now);

            foreach (var trigger in triggers)
            {
                AddRuleSuggestions(candidates, trigger, $"{trigger.NodeId}|{trigger.Condition}", general: false);
                AddRuleSuggestions(candidates, trigger, $"*|{trigger.Condition}", general: true);
            }

            AddNextStepSuggestions(candidates, issuedBy, now);
            AddProblemDeviceSuggestions(candidates, now);
            ApplyFeedback(candidates.Values, now);

            // Top-k by score with a min-heap of size k: each candidate costs
            // O(log k), and the weakest of the kept set is always the one evicted.
            var heap = new PriorityQueue<SuggestedAction, double>();
            foreach (var candidate in candidates.Values)
            {
                heap.Enqueue(candidate, candidate.Score);
                if (heap.Count > MaxSuggestions)
                {
                    heap.Dequeue();
                }
            }

            var ranked = new List<SuggestedAction>(heap.Count);
            while (heap.TryDequeue(out var suggestion, out _))
            {
                ranked.Add(suggestion);
            }

            ranked.Reverse();

            return new InsightsResponse
            {
                Suggestions = ranked,
                ObservedActions = (int)_observedActions,
                LearnedAssociations = _associations.Values.Sum(actions => actions.Count),
                ActiveTriggers = triggers.Count,
                Learning = BuildLearningStats(issuedBy),
                GeneratedUtc = now
            };
        }
    }

    /// <summary>
    /// Records what the operator did with a suggestion. A dismissal pushes that
    /// suggestion down the ranking for a while; an application lifts it. The
    /// action an application performs (a dispatch, a search, a filter) is
    /// recorded by its own endpoint, so the habit is learned there.
    /// </summary>
    public void RecordSuggestionFeedback(SuggestionFeedbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SuggestionId))
        {
            return;
        }

        lock (_sync)
        {
            EnsureReady();
            var id = request.SuggestionId.Trim();

            if (request.Outcome == SuggestionFeedback.Applied)
            {
                _suggestionsApplied++;
                _acceptances[id] = _acceptances.GetValueOrDefault(id) + 1;

                // Applying it settles any earlier dismissal.
                _dismissals.Remove(id);
            }
            else
            {
                _suggestionsDismissed++;
                var previous = _dismissals.GetValueOrDefault(id);
                _dismissals[id] = (previous.Count + 1, DateTime.UtcNow);
            }
        }
    }

    /// <summary>
    /// Forgets everything learned — rules, sequences, feedback and the activity
    /// window — so the engine can be shown learning a habit from nothing.
    /// </summary>
    public void ResetLearning()
    {
        lock (_sync)
        {
            EnsureReady();

            _activityWindow.Clear();
            _triggerWindow.Clear();
            _triggerSupport.Clear();
            _associations.Clear();
            _transitions.Clear();
            _lastActionByOperator.Clear();
            _lastActionAt.Clear();
            _lastActivity = null;
            _observedActions = 0;
            _lastOperatorActivityUtc = DateTime.MinValue;

            _dismissals.Clear();
            _acceptances.Clear();
            _suggestionsApplied = 0;
            _suggestionsDismissed = 0;
        }
    }

    /// <summary>Re-weights candidates by the feedback each has had. Caller holds the lock.</summary>
    private void ApplyFeedback(IEnumerable<SuggestedAction> candidates, DateTime now)
    {
        foreach (var suggestion in candidates)
        {
            if (_dismissals.TryGetValue(suggestion.Id, out var dismissed) && now - dismissed.LastUtc <= DismissalMemory)
            {
                suggestion.Score *= Math.Pow(DismissalPenalty, dismissed.Count);
                suggestion.Signals.Add(dismissed.Count == 1 ? "dismissed once, ranked lower" : $"dismissed ×{dismissed.Count}, ranked lower");
            }

            if (_acceptances.TryGetValue(suggestion.Id, out var applied))
            {
                suggestion.Score *= 1 + AcceptanceBoost * Math.Min(applied, MaxAcceptanceBoosts);
                suggestion.Signals.Add(applied == 1 ? "you applied this before" : $"you applied this ×{applied}");
            }
        }
    }

    /// <summary>What has been learned, for the learning panel. Caller holds the lock.</summary>
    private LearningStats BuildLearningStats(string? issuedBy)
    {
        var rules = new List<LearnedRule>();

        foreach (var (ruleKey, actions) in _associations)
        {
            if (!_triggerSupport.TryGetValue(ruleKey, out var support) || support < MinRuleSupport)
            {
                continue;
            }

            var split = ruleKey.IndexOf('|');
            var node = ruleKey[..split];
            var trigger = new ActiveTrigger(node, ruleKey[(split + 1)..], DateTime.MinValue, AlertSeverity.Warning);
            var condition = node == "*" ? DescribeGeneralCondition(trigger) : DescribeCondition(trigger);

            foreach (var (token, count) in actions)
            {
                var confidence = count / (double)support;
                if (count < MinRuleCount || confidence < MinRuleConfidence)
                {
                    continue;
                }

                rules.Add(new LearnedRule
                {
                    Condition = condition,
                    Action = PastTense(token),
                    Count = count,
                    Support = support,
                    Confidence = Math.Round(confidence, 2)
                });
            }
        }

        var operatorId = string.IsNullOrWhiteSpace(issuedBy) ? null : issuedBy.Trim();
        var feedback = _suggestionsApplied + _suggestionsDismissed;

        return new LearningStats
        {
            RulesLearned = rules.Count,
            TransitionsLearned = _transitions.Values.Sum(followers => followers.Count),
            TopRules = rules
                .OrderByDescending(rule => rule.Confidence * Math.Log2(2 + rule.Count))
                .Take(TopRuleCount)
                .ToList(),
            Applied = _suggestionsApplied,
            Dismissed = _suggestionsDismissed,
            AcceptanceRate = feedback == 0 ? null : Math.Round(_suggestionsApplied / (double)feedback, 2),
            RecentActivity = _activityWindow
                .Reverse()
                .Where(activity => operatorId is null || string.Equals(activity.Operator, operatorId, StringComparison.OrdinalIgnoreCase))
                .Take(RecentActivityCount)
                .Select(activity => new ActivityEntry
                {
                    Description = Capitalise(PastTense(activity.Token)),
                    AtUtc = activity.AtUtc
                })
                .ToList()
        };
    }

    private static string Capitalise(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    /// <summary>
    /// Association-rule suggestions for one active condition. A rule
    /// condition ⇒ action is kept when it has enough support (the condition was
    /// seen often enough with an operator present) and enough confidence (the
    /// action followed it often enough) — the two measures from [20].
    /// </summary>
    private void AddRuleSuggestions(Dictionary<string, SuggestedAction> candidates, ActiveTrigger trigger, string ruleKey, bool general)
    {
        if (!_associations.TryGetValue(ruleKey, out var actions) ||
            !_triggerSupport.TryGetValue(ruleKey, out var support) ||
            support < MinRuleSupport)
        {
            return;
        }

        foreach (var (token, count) in actions)
        {
            var confidence = count / (double)support;
            if (count < MinRuleCount || confidence < MinRuleConfidence)
            {
                continue;
            }

            var suggestion = FromToken(token, trigger.NodeId);
            if (suggestion is null)
            {
                continue;
            }

            // Already done since the condition started: nothing left to suggest.
            var concrete = ConcreteToken(token, trigger.NodeId);
            if (_lastActionAt.TryGetValue(concrete, out var doneAt) && doneAt >= trigger.SinceUtc)
            {
                continue;
            }

            var condition = general ? DescribeGeneralCondition(trigger) : DescribeCondition(trigger);
            suggestion.Kind = SuggestionKind.PredictedAction;
            suggestion.Confidence = Math.Round(confidence, 2);
            suggestion.Support = count;
            suggestion.Reason = $"After {condition}, operators {PastTense(token)} {count} of {support} times.";
            suggestion.Signals.Add(DescribeCondition(trigger));
            suggestion.Signals.Add($"{Math.Round(confidence * 100)}% confidence");

            // Confidence weighted by how much evidence stands behind it, and
            // lifted when the condition is critical. A node-specific rule is
            // preferred over the general one when both fire.
            var severityWeight = trigger.Severity == AlertSeverity.Critical ? 1.5 : 1.0;
            suggestion.Score = confidence * Math.Log2(2 + count) * 2 * severityWeight * (general ? 0.9 : 1.0);

            AddCandidate(candidates, suggestion);
        }
    }

    /// <summary>
    /// Next-step suggestions from the operator's last action: the estimated
    /// probability of each follow-on is count(prev → next) / count(prev → *),
    /// the bigram estimate from [21].
    /// </summary>
    private void AddNextStepSuggestions(Dictionary<string, SuggestedAction> candidates, string? issuedBy, DateTime now)
    {
        var last = !string.IsNullOrWhiteSpace(issuedBy) && _lastActionByOperator.TryGetValue(issuedBy.Trim(), out var mine)
            ? mine
            : _lastActivity;

        if (last is null || now - last.AtUtc > SessionGap || !_transitions.TryGetValue(last.Token, out var followers))
        {
            return;
        }

        var total = followers.Values.Sum();

        foreach (var (token, count) in followers.OrderByDescending(pair => pair.Value).Take(3))
        {
            var probability = count / (double)total;
            if (count < MinRuleCount || probability < MinTransitionProbability)
            {
                continue;
            }

            if (_lastActionAt.TryGetValue(token, out var doneAt) && doneAt > last.AtUtc)
            {
                continue;
            }

            var suggestion = FromToken(token, null);
            if (suggestion is null)
            {
                continue;
            }

            suggestion.Kind = SuggestionKind.NextStep;
            suggestion.Confidence = Math.Round(probability, 2);
            suggestion.Support = count;
            suggestion.Reason = $"You just {PastTense(last.Token)}. Operators who do that next {PastTense(token)} {Math.Round(probability * 100)}% of the time.";
            suggestion.Signals.Add($"{count} of {total} sessions");
            suggestion.Score = probability * Math.Log2(2 + count) * 1.5;

            AddCandidate(candidates, suggestion);
        }
    }

    /// <summary>
    /// Scores every node on its recent behaviour and flags the ones likely to be
    /// faulty, with the command most likely to help. Drift is judged by the
    /// z-score of the latest reading against the window's running mean and
    /// variance, accumulated in one pass with Welford's method [22].
    /// </summary>
    private void AddProblemDeviceSuggestions(Dictionary<string, SuggestedAction> candidates, DateTime now)
    {
        var readingCutoff = now.AddMinutes(-ProblemWindowMinutes);
        var failureCutoff = now.AddMinutes(-FailureWindowMinutes);

        // Failures per node in one backwards walk of the log, stopping at the
        // window edge rather than touching the whole history.
        var failures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = _store.DeviceCommands.Count - 1; index >= 0; index--)
        {
            var command = _store.DeviceCommands[index];
            if (command.IssuedUtc < failureCutoff)
            {
                break;
            }

            if (command.Status is CommandStatus.Failed or CommandStatus.Expired && !command.IsDryRun)
            {
                failures[command.NodeId] = failures.GetValueOrDefault(command.NodeId) + 1;
            }
        }

        var errorsByNode = new Dictionary<string, List<ErrorStateInfo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, info) in _errorStateInfo)
        {
            if (!errorsByNode.TryGetValue(key.NodeId, out var list))
            {
                list = new List<ErrorStateInfo>();
                errorsByNode[key.NodeId] = list;
            }

            list.Add(info);
        }

        foreach (var device in _devicesById.Values)
        {
            if (!device.IsActive)
            {
                continue;
            }

            var score = 0d;
            var signals = new List<string>();
            var drifting = false;

            var disconnected = _disconnectedNodes.Contains(device.NodeId);
            var droppedRecently = disconnected &&
                _disconnectedSince.TryGetValue(device.NodeId, out var since) &&
                since >= now.AddMinutes(-FailureWindowMinutes);

            // A node that has been dark for days is a known outage, not news.
            if (droppedRecently)
            {
                score += 4;
                signals.Add("dropped off the mesh");
            }

            if (errorsByNode.TryGetValue(device.NodeId, out var errors))
            {
                score += errors.Count * 1.5 + errors.Count(error => error.Severity == AlertSeverity.Critical);
                signals.Add(errors.Count == 1 ? "1 open breach" : $"{errors.Count} open breaches");
            }

            var failureCount = failures.GetValueOrDefault(device.NodeId);
            if (failureCount > 0)
            {
                score += failureCount * 1.2;
                signals.Add(failureCount == 1 ? "1 failed command" : $"{failureCount} failed commands");
            }

            if (_sensorLogs.TryGetValue(device.NodeId, out var log))
            {
                var stats = new Dictionary<ReadingType, RunningStats>();
                var flaps = 0;
                var outOfRange = 0;

                var values = log.Values;
                for (var index = LowerBound(log, readingCutoff); index < values.Count; index++)
                {
                    var entry = values[index];

                    if (entry.Kind == SensorLogKind.Disconnected)
                    {
                        flaps++;
                        continue;
                    }

                    if (entry.Kind != SensorLogKind.Reading || entry.ReadingType is not ReadingType type || entry.Value is not double value)
                    {
                        continue;
                    }

                    var limits = Limits(device.Id, type);
                    if (!limits.IsBoolean && (value < limits.Min || value > limits.Max))
                    {
                        outOfRange++;
                    }

                    stats[type] = stats.GetValueOrDefault(type).Add(value);
                }

                if (outOfRange > 0)
                {
                    score += Math.Min(3, outOfRange * 0.3);
                    signals.Add($"{outOfRange} out-of-range readings");
                }

                if (flaps >= 2)
                {
                    score += 1.5;
                    signals.Add($"link dropped {flaps}× in {ProblemWindowMinutes} min");
                }

                foreach (var (type, running) in stats)
                {
                    var z = running.ZScoreOfLast();
                    if (running.Count >= 8 && Math.Abs(z) >= DriftZScore)
                    {
                        score += 2;
                        drifting = true;
                        signals.Add($"{type} drifting (z = {z:0.0})");
                    }
                }
            }

            if (score < ProblemScoreThreshold)
            {
                continue;
            }

            var (commandType, parameters) = RecommendFor(device, disconnected, failureCount, drifting);
            if (IsInFlight(device.NodeId, commandType))
            {
                continue;
            }

            AddCandidate(candidates, new SuggestedAction
            {
                Id = $"problem:{device.NodeId}:{commandType}",
                Kind = SuggestionKind.ProblemDevice,
                Title = $"{Humanise(commandType.ToString())} on {device.NodeId}",
                Reason = $"{device.NodeId} ({device.Name}) looks faulty: {string.Join(", ", signals)}.",
                Confidence = Math.Round(Math.Min(1, score / 10), 2),
                Support = signals.Count,
                Score = score / 2,
                NodeId = device.NodeId,
                SensorName = device.Name,
                CommandType = commandType,
                Parameters = parameters,
                Priority = score >= 8 ? CommandPriority.Immediate : CommandPriority.High,
                DryRun = disconnected,
                Signals = signals
            });
        }
    }

    /// <summary>The command most likely to help a node showing these symptoms.</summary>
    private static (CommandType Type, string Parameters) RecommendFor(SensorProfile device, bool disconnected, int failures, bool drifting)
    {
        bool Supports(CommandType type) => CommandGenerator.Supports(device.Category, type);

        if (disconnected && Supports(CommandType.RestartNode))
        {
            return (CommandType.RestartNode, "mode=soft");
        }

        if (failures >= 2 && device.FirmwareVersion.Contains("beta", StringComparison.OrdinalIgnoreCase) && Supports(CommandType.FirmwarePush))
        {
            return (CommandType.FirmwarePush, StableFirmware);
        }

        if (failures >= 2 && Supports(CommandType.RestartNode))
        {
            return (CommandType.RestartNode, "mode=soft");
        }

        if (drifting && Supports(CommandType.Recalibrate))
        {
            return (CommandType.Recalibrate, "offset=auto");
        }

        if (Supports(CommandType.RequestSample))
        {
            return (CommandType.RequestSample, "count=5");
        }

        return Supports(CommandType.RestartNode)
            ? (CommandType.RestartNode, "mode=soft")
            : (CommandType.ToggleActuator, "relay=1,state=off");
    }

    private static void AddCandidate(Dictionary<string, SuggestedAction> candidates, SuggestedAction suggestion)
    {
        var key = $"{suggestion.NodeId}|{suggestion.CommandType}|{suggestion.SearchTerm}|{suggestion.FilterFacet}:{suggestion.FilterValue}";
        suggestion.Id = string.IsNullOrEmpty(suggestion.Id) ? $"{suggestion.Kind}:{key}" : suggestion.Id;

        if (!candidates.TryGetValue(key, out var existing) || suggestion.Score > existing.Score)
        {
            candidates[key] = suggestion;
        }
    }

    /// <summary>
    /// Turns a learned action back into something the operator can act on.
    /// <paramref name="selfNode"/> fills in a relative action ("the node that
    /// alerted"). Returns null for an action that no longer makes sense.
    /// </summary>
    private SuggestedAction? FromToken(string token, string? selfNode)
    {
        var (kind, detail, nodeId) = ParseToken(token, selfNode);
        _devicesByNode.TryGetValue(nodeId ?? string.Empty, out var device);

        switch (kind)
        {
            case "command":
            {
                if (device is null || !Enum.TryParse<CommandType>(detail, out var commandType) ||
                    !CommandGenerator.Supports(device.Category, commandType) ||
                    IsInFlight(device.NodeId, commandType))
                {
                    return null;
                }

                var parameters = _typicalParameters.GetValueOrDefault(ConcreteToken(token, selfNode))
                    ?? _typicalParameters.GetValueOrDefault(token)
                    ?? DefaultParameters(commandType);

                return new SuggestedAction
                {
                    Title = $"{Humanise(commandType.ToString())} on {device.NodeId}",
                    NodeId = device.NodeId,
                    SensorName = device.Name,
                    CommandType = commandType,
                    Parameters = parameters,
                    Priority = CommandPriority.High,
                    DryRun = !IsReachable(device)
                };
            }

            case "search":
            {
                var term = string.IsNullOrEmpty(detail) ? nodeId : detail;
                if (string.IsNullOrEmpty(term))
                {
                    return null;
                }

                return new SuggestedAction
                {
                    Title = $"Search for \"{term}\"",
                    NodeId = device?.NodeId,
                    SensorName = device?.Name,
                    SearchTerm = term
                };
            }

            case "select" when device is not null:
                return new SuggestedAction
                {
                    Title = $"Inspect {device.NodeId}",
                    NodeId = device.NodeId,
                    SensorName = device.Name
                };

            case "filter":
            {
                var separator = detail.IndexOf(':');
                if (separator <= 0 || separator == detail.Length - 1)
                {
                    return null;
                }

                var facet = detail[..separator];
                var filterValue = detail[(separator + 1)..];
                return new SuggestedAction
                {
                    Title = $"Filter to {Humanise(filterValue)} ({Humanise(facet).ToLowerInvariant()})",
                    FilterFacet = facet,
                    FilterValue = filterValue
                };
            }

            default:
                return null;
        }
    }

    /* ---------- Learning ---------- */

    /// <summary>
    /// A condition started. It joins the association window, and counts towards
    /// the rule's support if an operator is around to react to it.
    /// </summary>
    private void RegisterTrigger(string nodeId, string condition, DateTime at)
    {
        var trigger = new TriggerEvent($"{nodeId}|{condition}", $"*|{condition}", nodeId, at);

        if (at - _lastOperatorActivityUtc <= OperatorPresence)
        {
            CountTrigger(trigger);
        }

        _triggerWindow.Enqueue(trigger);
        EvictTriggers(at);
    }

    private void CountTrigger(TriggerEvent trigger)
    {
        if (trigger.Counted)
        {
            return;
        }

        trigger.Counted = true;
        _triggerSupport[trigger.Specific] = _triggerSupport.GetValueOrDefault(trigger.Specific) + 1;
        _triggerSupport[trigger.General] = _triggerSupport.GetValueOrDefault(trigger.General) + 1;
    }

    /// <summary>The window is a FIFO in time order, so expired conditions are always at the front.</summary>
    private void EvictTriggers(DateTime at)
    {
        while (_triggerWindow.TryPeek(out var oldest) && at - oldest.AtUtc > AssociationWindow)
        {
            _triggerWindow.Dequeue();
        }
    }

    /// <summary>
    /// Learns from one operator action: associates it with every condition
    /// still in the window, and records it as the next step after the
    /// operator's previous action.
    /// </summary>
    private void RecordAction(string operatorId, string token, string? nodeId, DateTime at)
    {
        EvictTriggers(at);

        foreach (var trigger in _triggerWindow)
        {
            if (trigger.AtUtc > at)
            {
                continue;
            }

            // Each action counts once per condition, however often it is repeated.
            if (trigger.Associated.Add(token))
            {
                // Reacting to an unattended condition proves someone saw it.
                CountTrigger(trigger);
                Increment(_associations, trigger.Specific, token);
            }

            // Acting on the node that raised the condition also teaches the
            // general rule, which then fires for any node in the same state.
            if (nodeId is not null && string.Equals(nodeId, trigger.NodeId, StringComparison.OrdinalIgnoreCase))
            {
                var relative = RelativeToken(token);
                if (trigger.Associated.Add(relative))
                {
                    CountTrigger(trigger);
                    Increment(_associations, trigger.General, relative);
                }
            }
        }

        if (_lastActionByOperator.TryGetValue(operatorId, out var previous) &&
            previous.Token != token &&
            at - previous.AtUtc <= SessionGap)
        {
            Increment(_transitions, previous.Token, token);
        }

        var activity = new OperatorActivity(operatorId, token, nodeId, at);
        _lastActionByOperator[operatorId] = activity;
        _lastActivity = activity;
        _lastActionAt[token] = at;
        _lastOperatorActivityUtc = at > _lastOperatorActivityUtc ? at : _lastOperatorActivityUtc;
        _observedActions++;

        _activityWindow.Enqueue(activity);
        while (_activityWindow.Count > MaxActivityWindow)
        {
            _activityWindow.Dequeue();
        }
    }

    private static void Increment(Dictionary<string, Dictionary<string, int>> table, string from, string to)
    {
        if (!table.TryGetValue(from, out var row))
        {
            row = new Dictionary<string, int>();
            table[from] = row;
        }

        row[to] = row.GetValueOrDefault(to) + 1;
    }

    /// <summary>
    /// Conditions a rule can fire on: open, and recent. A node that has been
    /// dark for days is a known outage — the habit being predicted is the
    /// reaction to something that has just happened.
    /// </summary>
    private List<ActiveTrigger> ActiveTriggers(DateTime now)
    {
        var triggers = new List<ActiveTrigger>();
        var horizon = now.AddMinutes(-FailureWindowMinutes);

        foreach (var (key, info) in _errorStateInfo)
        {
            if (info.SinceUtc >= horizon)
            {
                triggers.Add(new ActiveTrigger(key.NodeId, Condition(key.ReadingType, info.Direction), info.SinceUtc, info.Severity));
            }
        }

        foreach (var nodeId in _disconnectedNodes)
        {
            if (_disconnectedSince.TryGetValue(nodeId, out var since) && since >= horizon)
            {
                triggers.Add(new ActiveTrigger(nodeId, "Offline", since, AlertSeverity.Critical));
            }
        }

        return triggers;
    }

    /* ---------- Tokens ---------- */

    // An action is stored as "kind|detail|node", e.g. "command|RestartNode|NET-004"
    // or "search|pump a|". A general rule stores the node as @self.

    private static string ActionToken(string kind, string detail, string? nodeId) =>
        $"{kind}|{detail}|{nodeId ?? string.Empty}";

    private static (string Kind, string Detail, string? NodeId) ParseToken(string token, string? selfNode)
    {
        var parts = token.Split('|');
        var nodeId = parts.Length > 2 ? parts[2] : string.Empty;

        return (
            parts[0],
            parts.Length > 1 ? parts[1] : string.Empty,
            nodeId == SelfNode ? selfNode : nodeId.Length == 0 ? null : nodeId);
    }

    private static string RelativeToken(string token)
    {
        var (kind, detail, _) = ParseToken(token, null);

        // A search for the alerting node itself generalises to "look the node up".
        return kind == "search" ? ActionToken("search", string.Empty, SelfNode) : ActionToken(kind, detail, SelfNode);
    }

    private static string ConcreteToken(string token, string? selfNode)
    {
        var (kind, detail, nodeId) = ParseToken(token, selfNode);
        return kind == "search" && detail.Length == 0
            ? ActionToken("search", (nodeId ?? string.Empty).ToLowerInvariant(), nodeId)
            : ActionToken(kind, detail, nodeId);
    }

    private static string PastTense(string token)
    {
        var (kind, detail, nodeId) = ParseToken(token, "the node");
        return kind switch
        {
            "command" => $"sent {Humanise(detail)} to {nodeId}",
            "search" => $"searched for \"{(detail.Length == 0 ? nodeId : detail)}\"",
            "select" => $"inspected {nodeId}",
            "filter" => $"filtered by {detail.Replace(":", " ")}",
            "undo" => $"undid {Humanise(detail)} on {nodeId}",
            _ => "acted"
        };
    }

    private static string Condition(ReadingType readingType, BreachDirection direction) => $"{readingType}:{direction}";

    private static string DescribeCondition(ActiveTrigger trigger)
    {
        if (trigger.Condition == "Offline")
        {
            return $"{trigger.NodeId} drops off the mesh";
        }

        var parts = trigger.Condition.Split(':');
        var verb = parts[1] == nameof(BreachDirection.Low) ? "drops below" : "rises above";
        return $"{trigger.NodeId}'s {parts[0].ToLowerInvariant()} {verb} its limit";
    }

    private static string DescribeGeneralCondition(ActiveTrigger trigger)
    {
        if (trigger.Condition == "Offline")
        {
            return "a node drops off the mesh";
        }

        var parts = trigger.Condition.Split(':');
        var verb = parts[1] == nameof(BreachDirection.Low) ? "drops below" : "rises above";
        return $"a node's {parts[0].ToLowerInvariant()} {verb} its limit";
    }

    private static string DefaultParameters(CommandType commandType) => commandType switch
    {
        CommandType.SetThreshold => "temp.max=28.5",
        CommandType.Recalibrate => "offset=auto",
        CommandType.ToggleActuator => "relay=1,state=off",
        CommandType.RestartNode => "mode=soft",
        CommandType.FirmwarePush => StableFirmware,
        _ => "count=5"
    };

    // =====================================================================
    // Registry and initialisation — Dictionary [17]
    // Code attribution: the registry uses the Dictionary<TKey,TValue> constructor
    // with an IEqualityComparer (StringComparer.OrdinalIgnoreCase) and TryGetValue
    // lookups as documented in [17], so a node id or MAC address resolves in O(1)
    // whatever its case.
    // =====================================================================

    /// <summary>
    /// Builds the engine's state the first time it is used. Deferred rather than
    /// done in the constructor because the store is seeded after the container
    /// is built. Also picks up devices registered since (page 1 can add them).
    /// Caller holds the lock.
    /// </summary>
    private void EnsureReady()
    {
        if (!_initialised)
        {
            Initialise();
            _initialised = true;
            return;
        }

        if (_store.SensorProfiles.Count != _registeredProfileCount ||
            _store.SensorThresholds.Count != _registeredThresholdCount)
        {
            RefreshRegistry();
        }
    }

    private void Initialise()
    {
        var now = DateTime.UtcNow;
        RefreshRegistry();

        // Nodes the seed data already has offline start in the disconnected set.
        foreach (var device in _devicesById.Values)
        {
            if (!device.IsActive || device.Status == SensorStatus.Offline)
            {
                _disconnectedNodes.Add(device.NodeId);
                _disconnectedSince[device.NodeId] = device.LastSeenUtc;
            }
        }

        PickFlakyNodes();

        // Back-fill each node's sorted log from the seeded history, so the
        // timeline has something behind it at start-up.
        var logCutoff = now.AddHours(-LogRetentionHours);

        foreach (var reading in _store.TelemetryReadings)
        {
            if (reading.TimestampUtc < logCutoff || !_devicesById.TryGetValue(reading.SensorProfileId, out var device))
            {
                continue;
            }

            var value = reading.NumericValue ?? (reading.BooleanValue == true ? 1 : reading.BooleanValue == false ? 0 : null);
            if (value is null)
            {
                continue;
            }

            AppendLog(device.NodeId, new SensorLogEntry
            {
                TimestampUtc = reading.TimestampUtc,
                Kind = SensorLogKind.Reading,
                ReadingType = reading.ReadingType,
                Value = value
            });
        }

        RebuildRecentReadings();

        foreach (var alert in _store.Alerts)
        {
            if (alert.TriggeredUtc < logCutoff || !_devicesById.TryGetValue(alert.SensorProfileId, out var device))
            {
                continue;
            }

            AppendLog(device.NodeId, new SensorLogEntry
            {
                TimestampUtc = alert.TriggeredUtc,
                Kind = SensorLogKind.Alert,
                Label = alert.Message,
                Severity = alert.Severity
            });
        }

        // The seeded log is in issue order, so replaying it leaves each node's
        // settings at their latest values — what undo will restore to.
        foreach (var command in _store.DeviceCommands)
        {
            if (command.Status is CommandStatus.Queued or CommandStatus.Sent)
            {
                _inFlight.Add(command);
            }

            if (!command.IsDryRun)
            {
                ApplySettings(command.NodeId, command.CommandType, command.Parameters);
            }

            if (command.IssuedUtc >= logCutoff)
            {
                AppendLog(command.NodeId, CommandLogEntry(command));
            }
        }

        BootstrapOperatorHistory(now);
    }

    /// <summary>
    /// Rebuilds the registry dictionaries from the store. Every per-packet and
    /// per-dispatch lookup then costs one hash probe instead of a list scan.
    /// Caller holds the lock.
    /// </summary>
    private void RefreshRegistry()
    {
        _devicesByNode.Clear();
        _devicesByMac.Clear();
        _devicesById.Clear();
        _thresholds.Clear();

        foreach (var device in _store.SensorProfiles.ToList())
        {
            _devicesByNode[device.NodeId] = device;
            _devicesById[device.Id] = device;

            // Keyed in canonical form, so one device never sits under two notations.
            var macKey = MacAddress.Normalise(device.MacAddress) ?? device.MacAddress?.Trim();
            if (!string.IsNullOrWhiteSpace(macKey))
            {
                _devicesByMac[macKey] = device;
            }
        }

        foreach (var threshold in _store.SensorThresholds.ToList())
        {
            _thresholds[(threshold.SensorProfileId, threshold.ReadingType)] = threshold;
        }

        _registeredProfileCount = _store.SensorProfiles.Count;
        _registeredThresholdCount = _store.SensorThresholds.Count;
    }

    /// <summary>One reachable node per category of misbehaviour, deterministic across runs.</summary>
    private void PickFlakyNodes()
    {
        SensorCategory[] categories =
        [
            SensorCategory.Environmental, SensorCategory.PowerConsumption,
            SensorCategory.Actuator, SensorCategory.Connectivity
        ];

        foreach (var category in categories)
        {
            var node = _devicesById.Values
                .Where(device => device.Category == category && IsReachable(device))
                .OrderBy(device => device.NodeId)
                .FirstOrDefault();

            if (node is not null)
            {
                _flakyNodes.Add(node.NodeId);
            }
        }
    }

    /// <summary>
    /// Seeds the action engine with two days of operator behaviour so it has
    /// habits to recognise at start-up. The habits are the ones the live mesh
    /// reproduces — the flaky nodes keep breaching — so learned rules fire on
    /// real conditions, and every live action keeps adjusting them.
    /// </summary>
    private void BootstrapOperatorHistory(DateTime now)
    {
        var random = new Random(_options.RandomSeed + 19);
        string[] operators = ["t.kruger", "m.naidoo", "s.botha", "night-shift"];

        SensorProfile? Flaky(SensorCategory category) => _devicesById.Values
            .FirstOrDefault(device => device.Category == category && _flakyNodes.Contains(device.NodeId));

        var environmental = Flaky(SensorCategory.Environmental);
        var power = Flaky(SensorCategory.PowerConsumption);
        var actuator = Flaky(SensorCategory.Actuator);
        var network = Flaky(SensorCategory.Connectivity);

        var habits = new List<(SensorProfile Node, string Condition, (string Token, string? NodeId, string? Parameters)[] Actions)>();

        // The brief's example: when the soil sensor's moisture crashes, operators
        // confirm the reading, look the irrigation actuator up and switch it on.
        if (environmental is not null && actuator is not null)
        {
            habits.Add((environmental, Condition(ReadingType.Humidity, BreachDirection.Low),
            [
                (ActionToken("command", nameof(CommandType.RequestSample), environmental.NodeId), environmental.NodeId, "count=5"),
                (ActionToken("search", actuator.Name.ToLowerInvariant(), actuator.NodeId), actuator.NodeId, null),
                (ActionToken("command", nameof(CommandType.ToggleActuator), actuator.NodeId), actuator.NodeId, "relay=1,state=on")
            ]));
        }

        if (power is not null)
        {
            habits.Add((power, Condition(ReadingType.Power, BreachDirection.High),
            [
                (ActionToken("command", nameof(CommandType.RequestSample), power.NodeId), power.NodeId, "count=5"),
                (ActionToken("command", nameof(CommandType.SetThreshold), power.NodeId), power.NodeId, "power.max=4.5kW")
            ]));
        }

        if (network is not null)
        {
            habits.Add((network, "Offline",
            [
                (ActionToken("select", string.Empty, network.NodeId), network.NodeId, null),
                (ActionToken("search", network.Zone.ToLowerInvariant(), null), null, null)
            ]));
        }

        if (actuator is not null)
        {
            habits.Add((actuator, Condition(ReadingType.Vibration, BreachDirection.High),
            [
                (ActionToken("command", nameof(CommandType.ToggleActuator), actuator.NodeId), actuator.NodeId, "relay=1,state=off")
            ]));
        }

        // Sessions are replayed in time order: the association window evicts by
        // time, so out-of-order replay would associate the wrong things.
        var sessions = habits
            .SelectMany(habit => Enumerable.Range(0, random.Next(6, 9))
                .Select(_ => (Habit: habit, At: now.AddHours(-random.Next(1, 48)).AddMinutes(-random.Next(0, 60)))))
            .OrderBy(session => session.At)
            .ToList();

        foreach (var (habit, at) in sessions)
        {
            var operatorId = operators[random.Next(operators.Length)];
            _lastOperatorActivityUtc = at;
            RegisterTrigger(habit.Node.NodeId, habit.Condition, at);

            var step = at;
            foreach (var (token, nodeId, parameters) in habit.Actions)
            {
                // Habits, not rules: operators skip steps now and then.
                if (random.NextDouble() > 0.85)
                {
                    continue;
                }

                step = step.AddMinutes(random.Next(1, 4));
                if (parameters is not null)
                {
                    _typicalParameters[token] = parameters;
                    _typicalParameters[RelativeToken(token)] = parameters;
                }

                RecordAction(operatorId, token, nodeId, step);
            }
        }

        // History is learned; now start the live session clean, so a condition
        // firing overnight before anyone opens the page is not "attended".
        _triggerWindow.Clear();
        _lastActionByOperator.Clear();
        _lastActivity = null;
        _lastOperatorActivityUtc = DateTime.MinValue;
    }

    /* ---------- Small helpers ---------- */

    private bool IsReachable(SensorProfile device) =>
        device.IsActive && !_disconnectedNodes.Contains(device.NodeId);

    private bool IsInFlight(string nodeId, CommandType commandType) =>
        _inFlight.Exists(command =>
            command.CommandType == commandType &&
            string.Equals(command.NodeId, nodeId, StringComparison.OrdinalIgnoreCase) &&
            command.Status is CommandStatus.Queued or CommandStatus.Sent);

    /// <summary>The limits a reading is judged against: the node's own threshold, or the type's defaults.</summary>
    private LimitSet Limits(Guid sensorId, ReadingType readingType)
    {
        var profile = ReadingTypeProfile.For(readingType);

        if (_thresholds.TryGetValue((sensorId, readingType), out var threshold) &&
            threshold.IsEnabled && threshold.MinValue.HasValue && threshold.MaxValue.HasValue)
        {
            return new LimitSet(threshold.MinValue.Value, threshold.MaxValue.Value, threshold.Severity, profile.IsBoolean);
        }

        return new LimitSet(profile.MinThreshold, profile.MaxThreshold, AlertSeverity.Warning, profile.IsBoolean);
    }

    private static AlertSeverity? MaxSeverity(AlertSeverity? left, AlertSeverity? right)
    {
        if (left is null) return right;
        if (right is null) return left;
        return left > right ? left : right;
    }

    /// <summary>Splits "RestartNode" into "Restart Node" for display.</summary>
    private static string Humanise(string value) => Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");

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
    /// Counts per bucket, oldest first, converted to a per-minute rate — a 24h
    /// bucket spans 48 minutes and a 15m bucket only 30 seconds.
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

    /* ---------- Private types ---------- */

    private enum IntakeOutcome
    {
        Standard,
        Critical,
        Suppressed
    }

    /// <summary>
    /// One error condition. A record struct, so two keys for the same node,
    /// alert type and metric are equal and hash alike — which is what lets the
    /// HashSet recognise a repeat.
    /// </summary>
    private readonly record struct ErrorStateKey(string NodeId, AlertType AlertType, ReadingType ReadingType);

    /// <summary>When an error state opened, how bad it is, and how many repeats of it were absorbed.</summary>
    private sealed record ErrorStateInfo(DateTime SinceUtc, AlertSeverity Severity, BreachDirection Direction, int Suppressed = 0);

    private readonly record struct LimitSet(double Min, double Max, AlertSeverity Severity, bool IsBoolean);

    private readonly record struct Excursion(BreachDirection Direction, double Margin, int TicksLeft);

    private readonly record struct RevertPlan(CommandType? CommandType, string? Parameters, string Description);

    private readonly record struct ActiveTrigger(string NodeId, string Condition, DateTime SinceUtc, AlertSeverity Severity);

    private sealed record OperatorActivity(string Operator, string Token, string? NodeId, DateTime AtUtc);

    /// <summary>A condition inside the association window, and the actions already credited to it.</summary>
    private sealed class TriggerEvent(string specific, string general, string nodeId, DateTime atUtc)
    {
        public string Specific { get; } = specific;
        public string General { get; } = general;
        public string NodeId { get; } = nodeId;
        public DateTime AtUtc { get; } = atUtc;
        public bool Counted { get; set; }
        public HashSet<string> Associated { get; } = new();
    }

    /// <summary>
    /// Running mean and variance by Welford's method [22]: one pass, constant
    /// memory, and numerically stable where the naive sum-of-squares is not.
    /// </summary>
    private readonly record struct RunningStats(int Count, double Mean, double M2, double Last)
    {
        public RunningStats Add(double value)
        {
            var count = Count + 1;
            var delta = value - Mean;
            var mean = Mean + delta / count;
            return new RunningStats(count, mean, M2 + delta * (value - mean), value);
        }

        /// <summary>How unusual the latest value is against the window, in standard deviations.</summary>
        public double ZScoreOfLast()
        {
            if (Count < 2)
            {
                return 0;
            }

            var deviation = Math.Sqrt(M2 / (Count - 1));
            return deviation < 1e-9 ? 0 : (Last - Mean) / deviation;
        }
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
