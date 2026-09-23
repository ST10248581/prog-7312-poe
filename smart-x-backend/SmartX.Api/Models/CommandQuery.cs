namespace SmartX.Api.Models;

/// <summary>
/// The whole command filter state as one object. Empty collections mean "no
/// constraint" rather than "match nothing", which keeps the default query —
/// everything in the window — free of special cases.
/// </summary>
public class CommandQuery
{
    public List<CommandStatus>? Statuses { get; set; }
    public List<CommandOrigin>? Origins { get; set; }
    public List<CommandType>? CommandTypes { get; set; }

    /// <summary>
    /// Operation categories to keep. Sits above <see cref="CommandTypes"/>: the
    /// two are combined with AND, so selecting a category and a type outside it
    /// correctly returns nothing rather than quietly widening the slice.
    /// </summary>
    public List<OperationCategory>? OperationCategories { get; set; }

    /// <summary>
    /// Alert states of the target node to keep — the "show me only commands
    /// going to nodes that are currently alerting" filter.
    /// </summary>
    public List<NodeAlertState>? AlertStates { get; set; }

    /// <summary>Lowest alert severity a node's open alerts must reach to match.</summary>
    public AlertSeverity? MinAlertSeverity { get; set; }

    public string? Zone { get; set; }

    /// <summary>Matched against node id and sensor name, case-insensitively.</summary>
    public string? Search { get; set; }

    public bool ManualOnly { get; set; }

    /// <summary>Minutes of history to consider, counted back from now.</summary>
    public int WindowMinutes { get; set; } = 60;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
