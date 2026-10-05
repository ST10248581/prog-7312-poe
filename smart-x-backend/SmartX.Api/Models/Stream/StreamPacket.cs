namespace SmartX.Api.Models.Stream;

/// <summary>Which intake lane a packet was routed to.</summary>
public enum PacketLane
{
    /// <summary>FIFO: processed in arrival order, a budget per tick.</summary>
    Standard,

    /// <summary>Priority queue: drained immediately, worst breach first.</summary>
    Critical
}

public enum BreachDirection
{
    Low,
    High
}

/// <summary>
/// One telemetry packet waiting in the command engine's intake. Classified on
/// arrival — before it is queued — so the lane decision is made once and a
/// critical reading never waits behind routine traffic.
/// </summary>
public class StreamPacket
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SensorProfileId { get; init; }
    public string NodeId { get; init; } = string.Empty;

    public ReadingType ReadingType { get; init; }

    /// <summary>Null when the gateway lost the sample.</summary>
    public double? Value { get; init; }

    public string Unit { get; init; } = string.Empty;

    /// <summary>When the node took the sample. May be older than <see cref="ReceivedUtc"/>.</summary>
    public DateTime TimestampUtc { get; init; }

    /// <summary>When the packet reached the API; queue wait is measured from here.</summary>
    public DateTime ReceivedUtc { get; init; }

    /// <summary>False when the gateway is reporting that it lost the node.</summary>
    public bool LinkUp { get; init; } = true;

    /// <summary>
    /// How the packet arrived. Every packet now comes through the intake
    /// endpoint — the device simulator posts over HTTP like a real device — so
    /// this is "api".
    /// </summary>
    public string Source { get; init; } = "api";

    /* ---------- Set by classification ---------- */

    public PacketLane Lane { get; set; }
    public BreachDirection? Breach { get; set; }
    public AlertSeverity? Severity { get; set; }

    /// <summary>The limit that was crossed, when one was.</summary>
    public double? Limit { get; set; }

    /// <summary>How far past the limit, as a fraction of the normal operating span.</summary>
    public double BreachMargin { get; set; }
}
