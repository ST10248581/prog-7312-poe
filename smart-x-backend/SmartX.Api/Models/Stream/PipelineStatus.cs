namespace SmartX.Api.Models.Stream;

/// <summary>
/// The state of the command engine's intake: both queues, the error-state and
/// disconnected-node sets, and the alerts the pipeline raised recently.
/// </summary>
public class PipelineStatus
{
    public int StandardQueueDepth { get; set; }
    public int CriticalQueueDepth { get; set; }

    public long TotalReceived { get; set; }
    public long StandardProcessed { get; set; }
    public long CriticalProcessed { get; set; }

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

    public List<DisconnectedNode> DisconnectedNodes { get; set; } = new();
    public List<ActiveErrorState> ErrorStates { get; set; } = new();

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
}

public class ActiveErrorState
{
    public string NodeId { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public ReadingType? ReadingType { get; set; }
    public BreachDirection? Direction { get; set; }
    public AlertSeverity Severity { get; set; }
    public DateTime SinceUtc { get; set; }
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

    /// <summary>Alerts the critical lane raised for this batch.</summary>
    public List<StreamAlert> CriticalAlerts { get; set; } = new();
}
