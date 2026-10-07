namespace SmartX.Api.Models.Stream;

/// <summary>
/// The state of the command engine's intake: both queues, the error-state and
/// disconnected-node sets, and the alerts the pipeline raised recently.
/// </summary>
public class PipelineStatus
{
    public int StandardQueueDepth { get; set; }
    public int CriticalQueueDepth { get; set; }

    /// <summary>
    /// Backpressure limit on the standard queue. Past it the oldest routine
    /// packets are shed; the critical lane is never shed.
    /// </summary>
    public int StandardQueueCapacity { get; set; }

    /// <summary>Standard packets drained per dispatch tick.</summary>
    public int StandardBudgetPerTick { get; set; }

    public long TotalReceived { get; set; }
    public long StandardProcessed { get; set; }
    public long CriticalProcessed { get; set; }

    /// <summary>
    /// Standard packets that were already waiting when a critical packet was
    /// processed — i.e. how many the priority lane has overtaken in total.
    /// </summary>
    public long BypassedStandard { get; set; }

    /// <summary>How many queued standard packets the most recent critical packet overtook.</summary>
    public int LastCriticalBypassed { get; set; }

    /// <summary>Packets or alerts the sets recognised as repeats and dropped.</summary>
    public long DuplicatesSuppressed { get; set; }

    /// <summary>Standard-lane packets dropped because the queue hit its cap.</summary>
    public long Dropped { get; set; }

    /// <summary>Rolling average time a packet waited in each lane.</summary>
    public double AverageStandardWaitMs { get; set; }
    public double AverageCriticalWaitMs { get; set; }

    /// <summary>Devices in the registry dictionary.</summary>
    public int RegisteredDevices { get; set; }

    public int UndoDepth { get; set; }
    public int RedoDepth { get; set; }

    public List<DisconnectedNode> DisconnectedNodes { get; set; } = new();
    public List<ActiveErrorState> ErrorStates { get; set; } = new();

    /// <summary>What changed in the disconnected set since the caller's last poll.</summary>
    public PipelineSetChanges SetChanges { get; set; } = new();

    /// <summary>Newest first.</summary>
    public List<StreamAlert> RecentAlerts { get; set; } = new();

    public DateTime GeneratedUtc { get; set; }
}

public class DisconnectedNode
{
    public string NodeId { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public DateTime? SinceUtc { get; set; }

    /// <summary>Repeat "link lost" reports for this node that the set absorbed.</summary>
    public int SuppressedCount { get; set; }
}

public class ActiveErrorState
{
    public string NodeId { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public ReadingType? ReadingType { get; set; }
    public BreachDirection? Direction { get; set; }
    public AlertSeverity Severity { get; set; }
    public DateTime SinceUtc { get; set; }

    /// <summary>Repeat breach reports for this state that were recognised and not re-alerted.</summary>
    public int SuppressedCount { get; set; }
}

/// <summary>
/// The disconnected set compared with the one the caller saw last time, using
/// set algebra: newly disconnected = current \ known, recovered = known \ current,
/// still down = current ∩ known. Needs attention = disconnected ∪ nodes with a
/// critical open breach.
/// </summary>
public class PipelineSetChanges
{
    /// <summary>False when the caller sent no previous set (its first poll).</summary>
    public bool Compared { get; set; }

    public List<string> NewlyDisconnected { get; set; } = new();
    public List<string> Recovered { get; set; } = new();
    public int StillDisconnected { get; set; }

    public List<string> NeedsAttention { get; set; } = new();
}

/// <summary>What happened to a batch of packets posted to the intake.</summary>
public class PacketIntakeResult
{
    public int Received { get; set; }
    public int QueuedStandard { get; set; }

    /// <summary>Critical packets processed before the response was sent.</summary>
    public int ProcessedCritical { get; set; }

    public int SuppressedDuplicates { get; set; }

    /// <summary>Why each rejected packet was turned away.</summary>
    public List<string> Rejected { get; set; } = new();

    /// <summary>Standard-queue depth after the intake, i.e. how far back the batch sits.</summary>
    public int StandardQueueDepth { get; set; }

    /// <summary>Queued standard packets the batch's critical packets overtook.</summary>
    public int BypassedStandard { get; set; }

    /// <summary>Oldest standard packets shed because this batch pushed the queue past its capacity.</summary>
    public int DroppedStandard { get; set; }

    /// <summary>Alerts the critical lane raised for this batch.</summary>
    public List<StreamAlert> CriticalAlerts { get; set; } = new();
}
