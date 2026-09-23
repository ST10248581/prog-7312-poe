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

    /// <summary>
    /// Derived from <see cref="CommandType"/> rather than stored, so the two can
    /// never disagree. Serialised with the command so the dashboard can group
    /// and label by category without repeating the mapping.
    /// </summary>
    public OperationCategory OperationCategory => CommandOperations.CategoryOf(CommandType);

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

    /* ---------- Alert context ----------
       Not part of the command: the worst alert standing against the target node
       at the moment of the request. Filled in on the copy the query returns, so
       the stored log stays a record of what was dispatched. */

    public NodeAlertState NodeAlertState { get; set; }

    /// <summary>Severity of the worst open alert on the node; null when there is none.</summary>
    public AlertSeverity? NodeAlertSeverity { get; set; }

    /// <summary>Alerts on the node still awaiting resolution.</summary>
    public int NodeOpenAlertCount { get; set; }

    /// <summary>
    /// A copy carrying the node's alert context. The query decorates copies
    /// rather than the stored commands because the dispatch simulator is
    /// mutating those on a background timer while requests are reading them.
    /// </summary>
    public DeviceCommand WithAlertContext(NodeAlertState state, AlertSeverity? severity, int openAlertCount)
    {
        var copy = (DeviceCommand)MemberwiseClone();
        copy.NodeAlertState = state;
        copy.NodeAlertSeverity = severity;
        copy.NodeOpenAlertCount = openAlertCount;
        return copy;
    }
}
