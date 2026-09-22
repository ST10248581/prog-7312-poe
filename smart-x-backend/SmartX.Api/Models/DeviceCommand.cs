namespace SmartX.Api.Models;

/// <summary>
/// One dispatch to a mesh node. Commands are written once when issued and then
/// mutated in place as the node works through them (Queued → Sent →
/// Acknowledged, or Failed / Expired), which is what the stream is watching.
/// </summary>
public class DeviceCommand
{
    public Guid Id { get; set; }
    public Guid SensorProfileId { get; set; }

    /// <summary>Denormalised from the sensor profile so the stream renders without a join.</summary>
    public string NodeId { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;

    public CommandType CommandType { get; set; }

    /// <summary>Free-form and rendered as-is, e.g. <c>temp.max=28.5</c>.</summary>
    public string Parameters { get; set; } = string.Empty;

    public CommandOrigin Origin { get; set; }
    public CommandPriority Priority { get; set; }
    public CommandStatus Status { get; set; }

    public DateTime IssuedUtc { get; set; }
    public DateTime? DispatchedUtc { get; set; }
    public DateTime? AcknowledgedUtc { get; set; }

    /// <summary>Dispatch to acknowledgement; null while the command is still in flight.</summary>
    public int? RoundTripMs { get; set; }

    public string IssuedBy { get; set; } = string.Empty;
    public int Retries { get; set; }

    /// <summary>A dry run is validated and logged but never reaches the node.</summary>
    public bool IsDryRun { get; set; }
}
