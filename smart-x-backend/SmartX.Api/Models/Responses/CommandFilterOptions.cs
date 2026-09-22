namespace SmartX.Api.Models.Responses;

/// <summary>
/// Drives the command filter controls and the override target picker, so the UI
/// never hard-codes an enum value or a node id.
/// </summary>
public class CommandFilterOptions
{
    public List<string> Statuses { get; set; } = new();
    public List<string> Origins { get; set; } = new();
    public List<string> CommandTypes { get; set; } = new();
    public List<string> Priorities { get; set; } = new();
    public List<string> Zones { get; set; } = new();

    /// <summary>Node ids that can be targeted by a manual override.</summary>
    public List<string> Nodes { get; set; } = new();
}
