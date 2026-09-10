namespace SmartX.Api.Models.Responses;

/// <summary>
/// Drives the filter controls so the UI never hard-codes enum values.
/// </summary>
public class FilterOptions
{
    public List<string> Categories { get; set; } = new();
    public List<string> Statuses { get; set; } = new();
    public List<string> ReadingTypes { get; set; } = new();
    public List<string> Zones { get; set; } = new();
    public List<string> Rooms { get; set; } = new();
}
