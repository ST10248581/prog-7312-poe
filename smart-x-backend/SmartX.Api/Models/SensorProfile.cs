namespace SmartX.Api.Models;

public class SensorProfile
{
    public Guid Id { get; set; }
    public string MacAddress { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
    public string Room { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public SensorStatus Status { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
    public DateTime RegisteredUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public bool IsActive { get; set; }
}
