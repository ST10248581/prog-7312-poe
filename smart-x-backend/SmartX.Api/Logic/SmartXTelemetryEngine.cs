// =============================================================================
// CODE ATTRIBUTION — Technical and Language Requirements
//
// This class is where the four required advanced C# concepts do their work. The
// language constructs used for each were written with reference to the sources
// below; the individual sections are marked with the matching numbers, and the
// full list is repeated in README.md.
//
//   Generics
//   [1] Microsoft Learn, "Generic types and methods - C#".
//       https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics
//   [3] Microsoft Learn, "Boxing and Unboxing - C#".
//       https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing
//
//   Operator overloading
//   [5] Microsoft Learn, "Operator overloading - ... - C# reference".
//       https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading
//   [6] Microsoft Learn, "Operator Overloads - Framework Design Guidelines".
//       https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/operator-overloads
//
//   Advanced arrays and lists
//   [9] Microsoft Learn, "The array reference type - C# reference".
//       https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/arrays
//  [10] Microsoft Learn, "Array.GetLength(Int32) Method".
//       https://learn.microsoft.com/en-us/dotnet/api/system.array.getlength
//  [11] Microsoft Learn, "List<T> Constructors" (the List<T>(Int32) capacity
//       overload).
//       https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.-ctor
//
//   Recursion
//  [12] Microsoft Learn, "Iterate Through All Nodes of TreeView Control -
//       Windows Forms".
//       https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-iterate-through-all-nodes-of-a-windows-forms-treeview-control
//  [13] Microsoft Learn, "ReferenceEqualityComparer Class".
//       https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.referenceequalitycomparer
// =============================================================================

using System.Diagnostics;
using SmartX.Api.Data;
using SmartX.Api.Data.Seeding;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;
using SmartX.Api.Models.Telemetry;

namespace SmartX.Api.Logic;

/// <summary>
/// Central application-logic service for the Smart-X mesh.
/// <para>
/// Everything the controllers need lives here: the sensor, telemetry, alert and
/// engagement operations that used to sit in four thin pass-through services, plus
/// the mesh-level work that spans more than one repository. Centralising it keeps
/// the ingest pipeline, the load arithmetic and the deployment rules in one place
/// rather than scattered across controllers.
/// </para>
/// <para>
/// The class is also where the four required language features do real work:
/// <list type="bullet">
///   <item><description><b>Generics</b> - <see cref="TelemetryPacket{T}"/> wraps every incoming sample without boxing.</description></item>
///   <item><description><b>Operator overloading</b> - <see cref="SensorLoad"/> aggregates and compares meter draw with +, - and the relational operators.</description></item>
///   <item><description><b>Advanced arrays</b> - a jagged double[][] of raw batches is reduced through a rectangular double[,] working matrix before being transferred into List&lt;T&gt;.</description></item>
///   <item><description><b>Recursion</b> - the nested deployment tree is validated by a self-calling walk.</description></item>
/// </list>
/// </para>
/// </summary>
public class SmartXTelemetryEngine : ISmartXTelemetryEngine
{
    private const int DetailSeriesMaxPoints = 240;
    private const int MaxSeriesWindowHours = 24 * 30;
    private const string FacilityName = "Facility A";

    /// <summary>Recursion guard: a deployment profile deeper than this is malformed.</summary>
    private const int MaxDeploymentDepth = 8;

    // Column layout of the rectangular batch-statistics matrix.
    private const int StatSamples = 0;
    private const int StatAccepted = 1;
    private const int StatRejected = 2;
    private const int StatAnomalies = 3;
    private const int StatMin = 4;
    private const int StatMax = 5;
    private const int StatSum = 6;
    private const int StatColumnCount = 7;

    private readonly ISensorProfileRepository _sensorProfileRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IEngagementRepository _engagementRepository;

    public SmartXTelemetryEngine(
        ISensorProfileRepository sensorProfileRepository,
        ITelemetryRepository telemetryRepository,
        IAlertRepository alertRepository,
        IEngagementRepository engagementRepository)
    {
        _sensorProfileRepository = sensorProfileRepository;
        _telemetryRepository = telemetryRepository;
        _alertRepository = alertRepository;
        _engagementRepository = engagementRepository;
    }

    // =====================================================================
    // Sensors
    // =====================================================================

    public List<SensorListItem> GetSensors(TelemetryQuery query)
    {
        return _sensorProfileRepository.GetAll(query);
    }

    public SensorDetail? GetSensorDetail(Guid id)
    {
        var detail = _sensorProfileRepository.GetDetail(id);
        if (detail is null)
        {
            return null;
        }

        detail.Series = _telemetryRepository.GetSeries(
            id,
            DateTime.UtcNow.AddHours(-24),
            DetailSeriesMaxPoints);

        return detail;
    }

    public FilterOptions GetFilterOptions()
    {
        return _sensorProfileRepository.GetFilterOptions();
    }

    public SensorProfile? UpdateSensorPayload(Guid id, UpdateSensorPayloadRequest request)
    {
        return _sensorProfileRepository.UpdatePayload(id, request);
    }

    public SensorProfile CreateSensor(CreateSensorRequest request)
    {
        return _sensorProfileRepository.Create(request);
    }

    public SensorAttachment? UploadAttachment(
        Guid sensorId,
        IFormFile file,
        AttachmentType attachmentType,
        string description)
    {
        if (_sensorProfileRepository.GetById(sensorId) is null)
        {
            return null;
        }

        using var memoryStream = new MemoryStream();
        file.CopyTo(memoryStream);
        var fileData = memoryStream.ToArray();

        var attachment = new SensorAttachment
        {
            Id = Guid.NewGuid(),
            SensorProfileId = sensorId,
            FileName = file.FileName,
            StoredFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}",
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            AttachmentType = attachmentType,
            UploadedUtc = DateTime.UtcNow,
            UploadedBy = "user",
            Description = description
        };

        return _sensorProfileRepository.AddAttachment(sensorId, attachment, fileData);
    }

    public (SensorAttachment attachment, byte[] fileData)? DownloadAttachment(Guid sensorId, Guid attachmentId)
    {
        return _sensorProfileRepository.GetAttachmentFile(sensorId, attachmentId);
    }

    // =====================================================================
    // Telemetry
    // =====================================================================

    public PagedResult<TelemetryReading> GetReadings(TelemetryQuery query)
    {
        return _telemetryRepository.Query(query);
    }

    public List<SensorSeries> GetSeries(Guid sensorProfileId, int hours, int maxPoints)
    {
        var window = Math.Clamp(hours, 1, MaxSeriesWindowHours);
        return _telemetryRepository.GetSeries(
            sensorProfileId,
            DateTime.UtcNow.AddHours(-window),
            maxPoints);
    }

    public EcosystemSummary GetSummary()
    {
        return _telemetryRepository.GetSummary();
    }

    // =====================================================================
    // Alerts and engagement
    // =====================================================================

    public List<Alert> GetAlerts(AlertStatus? status, int take)
    {
        return _alertRepository.GetPrioritised(status, take);
    }

    public EngagementState? GetEngagement(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? _engagementRepository.GetPrimary()
            : _engagementRepository.GetByUserId(userId);
    }

    // =====================================================================
    // Batch ingestion - generics + jagged and multi-dimensional arrays
    // Code attribution: jagged double[][] and rectangular double[,] forms and
    // their `[row, column]` indexing follow [9]; pre-sizing the List<T> with the
    // capacity constructor to avoid repeated internal re-allocation follows [11];
    // the generic packets built per sample follow [1] and [3].
    // =====================================================================

    /// <summary>
    /// Takes the buffer a gateway flushed - a jagged double[][], one ragged row
    /// per batch - and turns it into persisted readings.
    /// <para>
    /// The raw block stays in arrays for as long as it is being reduced: the
    /// jagged array preserves the batch boundaries the gateway sent, and a
    /// rectangular double[,] holds the per-batch running statistics in one
    /// contiguous, fixed-size allocation. Only once every sample has been
    /// classified is the data transferred into the List&lt;T&gt; collections the
    /// rest of the application works with, pre-sized so they never have to grow
    /// and re-copy.
    /// </para>
    /// </summary>
    public TelemetryIngestResult? IngestHistoricalBatches(Guid sensorProfileId, IngestTelemetryRequest request)
    {
        var sensor = _sensorProfileRepository.GetById(sensorProfileId);
        if (sensor is null)
        {
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        var readingProfile = ReadingTypeProfile.For(request.ReadingType);
        var payloadKind = request.PayloadKind ?? DefaultPayloadKind(request.ReadingType);
        var interval = TimeSpan.FromSeconds(Math.Clamp(request.IntervalSeconds, 1, 86_400));

        // Jagged array: rows are independent, so batches of different sizes cost
        // nothing extra and each row can be scanned on its own.
        var rawBatches = request.Batches ?? Array.Empty<double[]>();
        var totalSamples = CountSamples(rawBatches);

        // Rectangular array: one row per batch, one column per statistic. Fixed
        // shape and contiguous storage, so the reduction allocates once rather
        // than once per batch.
        var statistics = new double[rawBatches.Length, StatColumnCount];

        // Pre-sized so the transfer out of the arrays never triggers a re-grow.
        var packets = new List<ITelemetryPacket>(totalSamples);

        var timestamp = request.StartUtc ?? DateTime.UtcNow - (interval * Math.Max(totalSamples - 1, 0));

        for (var batchIndex = 0; batchIndex < rawBatches.Length; batchIndex++)
        {
            var batch = rawBatches[batchIndex] ?? Array.Empty<double>();

            statistics[batchIndex, StatMin] = double.MaxValue;
            statistics[batchIndex, StatMax] = double.MinValue;

            for (var sampleIndex = 0; sampleIndex < batch.Length; sampleIndex++)
            {
                var raw = batch[sampleIndex];
                statistics[batchIndex, StatSamples]++;

                // A gateway that loses a sample sends NaN rather than dropping the
                // slot, so the positions in the sequence stay aligned.
                if (double.IsNaN(raw) || double.IsInfinity(raw))
                {
                    statistics[batchIndex, StatRejected]++;
                    timestamp += interval;
                    continue;
                }

                var isAnomaly = raw < readingProfile.MinThreshold || raw > readingProfile.MaxThreshold;
                var quality = isAnomaly ? ReadingQuality.Suspect : ReadingQuality.Good;

                // Generic wrapper: the sample is carried in a strongly typed packet
                // whose T matches how the gateway encoded it.
                packets.Add(CreatePacket(
                    sensorProfileId,
                    request.ReadingType,
                    payloadKind,
                    raw,
                    readingProfile.Unit,
                    timestamp,
                    quality,
                    isAnomaly));

                statistics[batchIndex, StatAccepted]++;
                statistics[batchIndex, StatSum] += raw;

                if (isAnomaly)
                {
                    statistics[batchIndex, StatAnomalies]++;
                }

                if (raw < statistics[batchIndex, StatMin])
                {
                    statistics[batchIndex, StatMin] = raw;
                }

                if (raw > statistics[batchIndex, StatMax])
                {
                    statistics[batchIndex, StatMax] = raw;
                }

                timestamp += interval;
            }
        }

        // Transfer out of the arrays into the optimised collections.
        var readings = new List<TelemetryReading>(packets.Count);
        foreach (var packet in packets)
        {
            readings.Add(packet.ToReading());
        }

        var batchRecord = new IngestionBatch
        {
            Id = Guid.NewGuid(),
            SensorProfileId = sensorProfileId,
            ReceivedUtc = DateTime.UtcNow,
            ReadingCount = totalSamples,
            AcceptedCount = readings.Count,
            RejectedCount = totalSamples - readings.Count,
            SourceIpAddress = string.IsNullOrWhiteSpace(request.SourceIpAddress)
                ? "0.0.0.0"
                : request.SourceIpAddress,
            ProcessingMs = (int)stopwatch.ElapsedMilliseconds
        };

        _telemetryRepository.AppendReadings(readings, batchRecord);

        var result = new TelemetryIngestResult
        {
            SensorProfileId = sensorProfileId,
            ReadingType = request.ReadingType,
            Unit = readingProfile.Unit,
            PayloadType = packets.Count > 0 ? packets[0].PayloadType.Name : payloadKind.ToString(),
            BatchCount = rawBatches.Length,
            RawSampleCount = totalSamples,
            AcceptedCount = readings.Count,
            RejectedCount = totalSamples - readings.Count,
            BatchStatistics = ProjectStatistics(statistics),
            IngestedUtc = batchRecord.ReceivedUtc
        };

        result.AnomalyCount = result.BatchStatistics.Sum(statistic => statistic.AnomalyCount);

        stopwatch.Stop();
        result.ProcessingMs = (int)stopwatch.ElapsedMilliseconds;
        batchRecord.ProcessingMs = result.ProcessingMs;

        return result;
    }

    /// <summary>
    /// Chooses the payload type for a metric. The mesh is mixed hardware: switch
    /// state is a single bit, meters report fractional kW, and environmental nodes
    /// send 32-bit floats to keep their radio packets small.
    /// </summary>
    private static TelemetryPayloadKind DefaultPayloadKind(ReadingType readingType) => readingType switch
    {
        ReadingType.Motion => TelemetryPayloadKind.Bool,
        ReadingType.Power => TelemetryPayloadKind.Double,
        _ => TelemetryPayloadKind.Float
    };

    /// <summary>
    /// Builds the packet for one sample. Each arm closes a different generic
    /// instantiation, so the payload is stored in a bool, int, float or double
    /// field rather than as a boxed object.
    /// </summary>
    private static ITelemetryPacket CreatePacket(
        Guid sensorProfileId,
        ReadingType readingType,
        TelemetryPayloadKind payloadKind,
        double raw,
        string unit,
        DateTime timestampUtc,
        ReadingQuality quality,
        bool isAnomaly) => payloadKind switch
        {
            TelemetryPayloadKind.Bool => TelemetryPacket.Create(
                sensorProfileId, readingType, raw >= 0.5d, unit, timestampUtc, quality, isAnomaly),

            TelemetryPayloadKind.Int => TelemetryPacket.Create(
                sensorProfileId, readingType, (int)Math.Round(raw), unit, timestampUtc, quality, isAnomaly),

            TelemetryPayloadKind.Double => TelemetryPacket.Create(
                sensorProfileId, readingType, raw, unit, timestampUtc, quality, isAnomaly),

            _ => TelemetryPacket.Create(
                sensorProfileId, readingType, (float)raw, unit, timestampUtc, quality, isAnomaly)
        };

    private static int CountSamples(double[][] rawBatches)
    {
        var total = 0;
        foreach (var batch in rawBatches)
        {
            total += batch?.Length ?? 0;
        }

        return total;
    }

    /// <summary>Lifts the rectangular working matrix into a serialisable collection.</summary>
    private static List<BatchStatistic> ProjectStatistics(double[,] statistics)
    {
        // GetLength(0) returns the number of elements in the first dimension (the
        // row count) of a multi-dimensional array - see [10] and [9].
        var rowCount = statistics.GetLength(0);
        var projected = new List<BatchStatistic>(rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            var accepted = (int)statistics[row, StatAccepted];

            projected.Add(new BatchStatistic
            {
                BatchIndex = row,
                SampleCount = (int)statistics[row, StatSamples],
                AcceptedCount = accepted,
                RejectedCount = (int)statistics[row, StatRejected],
                AnomalyCount = (int)statistics[row, StatAnomalies],
                MinValue = accepted == 0 ? 0d : Math.Round(statistics[row, StatMin], 3),
                MaxValue = accepted == 0 ? 0d : Math.Round(statistics[row, StatMax], 3),
                MeanValue = accepted == 0 ? 0d : Math.Round(statistics[row, StatSum] / accepted, 3)
            });
        }

        return projected;
    }

    // =====================================================================
    // Load arithmetic - operator overloading
    // Code attribution: the call sites below consume the operators declared on
    // SensorLoad; the operators themselves follow [5], and expressing zone
    // aggregation as `+`/`-` on a numeric-style value type rather than as named
    // helper methods follows the guidance in [6].
    // =====================================================================

    /// <summary>
    /// Aggregate draw of a set of meters. The fold is plain addition because
    /// <see cref="SensorLoad"/> overloads operator +, which carries the unit and
    /// the meter count through the sum as well as the value.
    /// </summary>
    public AggregateLoad GetAggregateLoad(IEnumerable<Guid> sensorProfileIds)
    {
        var contributors = new List<LoadReading>();
        var total = SensorLoad.Zero;

        foreach (var sensorProfileId in sensorProfileIds.Distinct())
        {
            var measurement = MeasureLoad(sensorProfileId);
            if (measurement is null)
            {
                continue;
            }

            var (sensor, load, lastReadingUtc) = measurement.Value;

            total += load;

            contributors.Add(ToLoadReading(sensor, load, lastReadingUtc));
        }

        return new AggregateLoad
        {
            Scope = "selection",
            TotalValue = Math.Round(total.Value, 3),
            AverageValue = Math.Round(total.Average, 3),
            Unit = total.Unit,
            MeterCount = total.SampleCount,
            Contributors = contributors
                .OrderByDescending(contributor => contributor.Value)
                .ToList(),
            GeneratedUtc = DateTime.UtcNow
        };
    }

    public AggregateLoad GetZoneLoad(string zone)
    {
        var sensorIds = _sensorProfileRepository.GetProfiles()
            .Where(profile => string.Equals(profile.Zone, zone, StringComparison.OrdinalIgnoreCase))
            .Select(profile => profile.Id);

        var aggregate = GetAggregateLoad(sensorIds);
        aggregate.Scope = zone;
        return aggregate;
    }

    /// <summary>
    /// Compares two meters. The delta is a subtraction and the verdict comes from
    /// the relational operators, so the intent reads directly off the code.
    /// </summary>
    public LoadComparison? CompareLoad(Guid leftSensorId, Guid rightSensorId)
    {
        var leftMeasurement = MeasureLoad(leftSensorId);
        var rightMeasurement = MeasureLoad(rightSensorId);

        if (leftMeasurement is null || rightMeasurement is null)
        {
            return null;
        }

        var (leftSensor, leftLoad, leftLastUtc) = leftMeasurement.Value;
        var (rightSensor, rightLoad, rightLastUtc) = rightMeasurement.Value;

        // Throws if the two meters report in incompatible units - see SensorLoad.
        var delta = leftLoad - rightLoad;
        var leftIsHeavier = leftLoad > rightLoad;
        var areEquivalent = leftLoad == rightLoad;

        return new LoadComparison
        {
            Left = ToLoadReading(leftSensor, leftLoad, leftLastUtc),
            Right = ToLoadReading(rightSensor, rightLoad, rightLastUtc),
            Delta = Math.Round(delta.Value, 3),
            Unit = delta.Unit,
            DeltaPercent = rightLoad.Value == 0d
                ? 0d
                : Math.Round(delta.Value / Math.Abs(rightLoad.Value) * 100d, 1),
            LeftIsHeavier = leftIsHeavier,
            AreEquivalent = areEquivalent,
            Summary = areEquivalent
                ? $"{leftSensor.NodeId} and {rightSensor.NodeId} are drawing the same load ({leftLoad})."
                : leftIsHeavier
                    ? $"{leftSensor.NodeId} is drawing {delta} more than {rightSensor.NodeId}."
                    : $"{rightSensor.NodeId} is drawing {-delta} more than {leftSensor.NodeId}."
        };
    }

    private (SensorProfile Sensor, SensorLoad Load, DateTime? LastReadingUtc)? MeasureLoad(Guid sensorProfileId)
    {
        var sensor = _sensorProfileRepository.GetById(sensorProfileId);
        if (sensor is null)
        {
            return null;
        }

        var reading = _telemetryRepository.GetLatest(sensorProfileId, ReadingType.Power);
        if (reading?.NumericValue is not double value)
        {
            return null;
        }

        var unit = string.IsNullOrWhiteSpace(reading.Unit)
            ? ReadingTypeProfile.For(ReadingType.Power).Unit
            : reading.Unit;

        return (sensor, new SensorLoad(value, unit), reading.TimestampUtc);
    }

    private static LoadReading ToLoadReading(SensorProfile sensor, SensorLoad load, DateTime? lastReadingUtc) =>
        new()
        {
            SensorProfileId = sensor.Id,
            Name = sensor.Name,
            NodeId = sensor.NodeId,
            Value = Math.Round(load.Value, 3),
            Unit = load.Unit,
            SampleCount = load.SampleCount,
            LastReadingUtc = lastReadingUtc
        };

    // =====================================================================
    // Deployment validation - recursion
    // Code attribution: the walk follows the recursive approach in [12] - process
    // the node, then call the same method for each child - and adds the two base
    // cases [12] warns are needed on a large or malformed tree (a depth guard and
    // an ancestor set). The ancestor set uses ReferenceEqualityComparer.Instance
    // so nodes are tracked by identity rather than by value; see [13].
    // =====================================================================

    public DeploymentValidationReport ValidateDeployment(string? zone = null)
    {
        return ValidateDeployment(BuildDeploymentTree(zone));
    }

    /// <summary>
    /// Validates a nested deployment profile. The structure is recursive - every
    /// tier holds a list of the tier below it, and the depth is not known in
    /// advance - so it is walked by a method that calls itself once per child
    /// rather than by a fixed ladder of loops.
    /// </summary>
    public DeploymentValidationReport ValidateDeployment(DeploymentNode root)
    {
        var report = new DeploymentValidationReport
        {
            Root = root,
            GeneratedUtc = DateTime.UtcNow
        };

        // Tracks the nodes on the current path so a profile that references one of
        // its own ancestors is reported instead of recursing forever.
        var ancestors = new HashSet<DeploymentNode>(ReferenceEqualityComparer.Instance);

        ValidateNode(root, DeploymentTier.Facility, parentPath: null, depth: 0, report, ancestors);

        report.Issues = report.Issues
            .OrderByDescending(issue => issue.Severity)
            .ThenBy(issue => issue.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        report.ValidPaths.Sort(StringComparer.OrdinalIgnoreCase);

        return report;
    }

    /// <summary>
    /// The recursive step: validate this node, then hand each child the tier it is
    /// required to occupy and let the same method validate it.
    /// </summary>
    // Code attribution: recursive method structure adapted from the PrintRecursive
    // example in [12] (process the node, then foreach child call the same method).
    private static void ValidateNode(
        DeploymentNode node,
        DeploymentTier expectedTier,
        string? parentPath,
        int depth,
        DeploymentValidationReport report,
        HashSet<DeploymentNode> ancestors)
    {
        report.NodesVisited++;
        report.MaxDepthReached = Math.Max(report.MaxDepthReached, depth);

        var displayName = string.IsNullOrWhiteSpace(node.Name) ? "(unnamed)" : node.Name.Trim();
        var path = parentPath is null ? displayName : $"{parentPath} -> {displayName}";

        // Base case 1: the profile is nested deeper than the hierarchy allows.
        if (depth > MaxDeploymentDepth)
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.DepthExceeded, AlertSeverity.Critical,
                $"Deployment nesting exceeds the {MaxDeploymentDepth}-tier limit; the branch below was not validated."));
            return;
        }

        // Base case 2: this node is already on the path back to the root.
        if (!ancestors.Add(node))
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.CircularReference, AlertSeverity.Critical,
                "Deployment profile contains a circular reference back to one of its own parents."));
            return;
        }

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.UnnamedNode, AlertSeverity.Warning,
                "Node has no name, so it cannot be addressed from the dashboard."));
        }

        if (node.Tier != expectedTier)
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.TierOutOfOrder, AlertSeverity.Critical,
                $"Expected a {expectedTier} at this level but found a {node.Tier}."));
        }

        if (node.Tier == DeploymentTier.Node)
        {
            ValidateLeaf(node, path, report);
            ancestors.Remove(node);
            return;
        }

        if (node.Children.Count == 0)
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.EmptyBranch, AlertSeverity.Warning,
                $"{node.Tier} {displayName} contains no child deployments."));
            ancestors.Remove(node);
            return;
        }

        foreach (var duplicate in node.Children
                     .GroupBy(child => child.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.DuplicateSibling, AlertSeverity.Warning,
                $"{duplicate.Key} appears {duplicate.Count()} times under {displayName}; paths to it are ambiguous."));
        }

        // Recursive step: each child must sit exactly one tier deeper.
        foreach (var child in node.Children)
        {
            ValidateNode(child, node.Tier + 1, path, depth + 1, report, ancestors);
        }

        ancestors.Remove(node);
    }

    /// <summary>Leaf rules: a Node tier must carry a sensor and must not have children.</summary>
    private static void ValidateLeaf(DeploymentNode node, string path, DeploymentValidationReport report)
    {
        if (node.Children.Count > 0)
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.TierOutOfOrder, AlertSeverity.Critical,
                "A Node is a leaf and cannot contain further deployments."));
        }

        if (node.SensorProfileId is null || node.SensorProfileId == Guid.Empty)
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.OrphanedSensor, AlertSeverity.Critical,
                "Node is not bound to a registered sensor profile."));
            return;
        }

        if (!node.IsActive || node.Status == SensorStatus.Offline)
        {
            report.Issues.Add(Issue(path, node.Tier, DeploymentIssueKind.UnreachableNode, AlertSeverity.Warning,
                "Node is configured but is not currently reachable on the mesh."));
        }

        report.SensorsPlaced++;
        report.ValidPaths.Add(path);
    }

    private static DeploymentIssue Issue(
        string path,
        DeploymentTier tier,
        DeploymentIssueKind kind,
        AlertSeverity severity,
        string message) =>
        new()
        {
            Path = path,
            Tier = tier,
            Kind = kind,
            Severity = severity,
            Message = message
        };

    /// <summary>
    /// Projects the flat sensor register into the nested tree the mesh is actually
    /// deployed as: Facility A -> Zone 1 -> Sub-Zone B -> NODE-07.
    /// </summary>
    private DeploymentNode BuildDeploymentTree(string? zone)
    {
        var profiles = _sensorProfileRepository.GetProfiles();

        if (!string.IsNullOrWhiteSpace(zone))
        {
            profiles = profiles
                .Where(profile => string.Equals(profile.Zone, zone, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var facility = new DeploymentNode
        {
            Name = FacilityName,
            Tier = DeploymentTier.Facility
        };

        foreach (var zoneGroup in profiles
                     .GroupBy(profile => profile.Zone)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var zoneNode = new DeploymentNode
            {
                Name = zoneGroup.Key,
                Tier = DeploymentTier.Zone
            };

            foreach (var roomGroup in zoneGroup
                         .GroupBy(profile => profile.Room)
                         .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
            {
                var subZoneNode = new DeploymentNode
                {
                    Name = roomGroup.Key,
                    Tier = DeploymentTier.SubZone
                };

                foreach (var profile in roomGroup.OrderBy(item => item.NodeId, StringComparer.OrdinalIgnoreCase))
                {
                    subZoneNode.Children.Add(new DeploymentNode
                    {
                        Name = profile.NodeId,
                        Tier = DeploymentTier.Node,
                        SensorProfileId = profile.Id,
                        Status = profile.Status,
                        IsActive = profile.IsActive
                    });
                }

                zoneNode.Children.Add(subZoneNode);
            }

            facility.Children.Add(zoneNode);
        }

        return facility;
    }
}
