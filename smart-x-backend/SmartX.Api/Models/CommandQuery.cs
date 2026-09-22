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
    public string? Zone { get; set; }

    /// <summary>Matched against node id and sensor name, case-insensitively.</summary>
    public string? Search { get; set; }

    public bool ManualOnly { get; set; }

    /// <summary>Minutes of history to consider, counted back from now.</summary>
    public int WindowMinutes { get; set; } = 60;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
