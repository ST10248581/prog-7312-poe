namespace SmartX.Api.Models;

public enum SensorCategory
{
    Environmental,
    PowerConsumption,
    Actuator,
    Motion,
    Connectivity
}

public enum SensorStatus
{
    Online,
    Warning,
    Offline
}

public enum ReadingType
{
    Temperature,
    Humidity,
    Pressure,
    Power,
    Vibration,
    Motion
}

public enum ReadingQuality
{
    Good,
    Suspect,
    Bad
}

public enum AttachmentType
{
    ConfigFile,
    DeploymentPhoto,
    HardwareLog
}

public enum AlertType
{
    ThresholdBreach,
    DeviceOffline,
    DataGap,
    LowBattery
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum AlertStatus
{
    Active,
    Acknowledged,
    Resolved
}

public enum CommandType
{
    SetThreshold,
    Recalibrate,
    ToggleActuator,
    RestartNode,
    FirmwarePush,
    RequestSample
}

public enum CommandOrigin
{
    Automation,
    Manual,
    Schedule
}

public enum CommandStatus
{
    Queued,
    Sent,
    Acknowledged,
    Failed,
    Expired
}

public enum CommandPriority
{
    Normal,
    High,
    Immediate
}

/// <summary>
/// The kind of operation a command performs, above the individual command type.
/// An operator investigating a node asks what was being changed on it, not which
/// of the six command types was used, so the stream is filterable at that
/// altitude too. The mapping lives in <see cref="CommandOperations"/>.
/// </summary>
public enum OperationCategory
{
    Configuration,
    Maintenance,
    Control,
    Diagnostics
}

/// <summary>
/// The worst alert state standing against the node a command targets, resolved
/// per request from the alert log rather than stored on the command — a node's
/// alerts move independently of the commands already sent to it. Ordered by
/// escalation, so the highest value is the one worth showing.
/// </summary>
public enum NodeAlertState
{
    /// <summary>Nothing has alerted on the node inside the recency horizon.</summary>
    Clear,

    /// <summary>The node alerted recently, but nothing is outstanding.</summary>
    Resolved,

    /// <summary>Seen by an operator, but not yet resolved.</summary>
    Acknowledged,

    /// <summary>Raised and still unacknowledged.</summary>
    Active
}
