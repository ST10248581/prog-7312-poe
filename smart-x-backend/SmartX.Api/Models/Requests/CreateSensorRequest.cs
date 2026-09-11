namespace SmartX.Api.Models.Requests;

public class CreateSensorRequest
{
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
}
