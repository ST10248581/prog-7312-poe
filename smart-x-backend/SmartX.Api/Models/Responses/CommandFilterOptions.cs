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

    /// <summary>
    /// Operation categories, each with the command types it covers, so the
    /// filter bar can explain what a category selects without duplicating the
    /// mapping the API filters by.
    /// </summary>
    public List<OperationCategoryOption> OperationCategories { get; set; } = new();

    /// <summary>Node alert states, escalating — Clear through Active.</summary>
    public List<string> AlertStates { get; set; } = new();

    public List<string> AlertSeverities { get; set; } = new();

    /// <summary>Node ids that can be targeted by a manual override.</summary>
    public List<string> Nodes { get; set; } = new();
}

/// <summary>One operation category and the command types it groups.</summary>
public class OperationCategoryOption
{
    public string Category { get; set; } = string.Empty;
    public List<string> CommandTypes { get; set; } = new();
}
