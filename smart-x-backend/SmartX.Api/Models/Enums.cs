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
