// =============================================================================
// CODE ATTRIBUTION — Queues and priority queues (Part 2)
//
// StreamPacket is the element type of the command engine's two intake lanes,
// which were written with reference to the sources below.
//
// Code Attribution [14]
// Author: Microsoft
// Year: 2025
// Title: Queue<T> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1>
// Accessed: [Accessed 29 September 2026]
// Modifications: StreamPacket is the element of the Queue<StreamPacket>
//   standard lane; PacketLane.Standard records that a packet was routed there
//   for first-in, first-out processing.
// Reference: Microsoft, 2025. Queue<T> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1> [Accessed 29 September 2026].
//
// Code Attribution [15]
// Author: Microsoft
// Year: 2025
// Title: PriorityQueue<TElement,TPriority> Class
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2>
// Accessed: [Accessed 29 September 2026]
// Modifications: StreamPacket is the element of the critical PriorityQueue; its
//   breach rank and ReceivedTicks form the (rank, arrival) priority tuple, and
//   PacketLane.Critical records the routing.
// Reference: Microsoft, 2025. PriorityQueue<TElement,TPriority> Class [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2> [Accessed 29 September 2026].
// =============================================================================

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
