namespace SmartX.Api.Models.Stream;

public enum SuggestionKind
{
    /// <summary>Learned association: operators usually do this after a condition like the one active now.</summary>
    PredictedAction,

    /// <summary>Learned sequence: what usually follows the operator's last action.</summary>
    NextStep,

    /// <summary>Anomaly scoring flagged the node as likely faulty.</summary>
    ProblemDevice
}

public enum OperatorActivityKind
{
    Search,
    SelectNode
}

/// <summary>
/// One recommendation from the automated action engine. It carries enough to
/// act on directly: a search term to apply, or a command to put in the console.
/// </summary>
public class SuggestedAction
{
    /// <summary>Stable across refreshes, so the UI can keep a dismissal.</summary>
    public string Id { get; set; } = string.Empty;

    public SuggestionKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Why the engine is suggesting it, in words an operator can check.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>0–1. For learned suggestions, the rule's confidence.</summary>
    public double Confidence { get; set; }

    /// <summary>How many observations the suggestion rests on.</summary>
    public int Support { get; set; }

    /// <summary>Ranking score; higher is shown first.</summary>
    public double Score { get; set; }

    public string? NodeId { get; set; }
    public string? SensorName { get; set; }

    public string? SearchTerm { get; set; }

    public CommandType? CommandType { get; set; }
    public string? Parameters { get; set; }
    public CommandPriority? Priority { get; set; }

    /// <summary>True when the target is unreachable, so the command can only be logged.</summary>
    public bool DryRun { get; set; }

    /// <summary>Short evidence chips, e.g. "3 failed commands", "z = 3.1".</summary>
    public List<string> Signals { get; set; } = new();
}

public class InsightsResponse
{
    public List<SuggestedAction> Suggestions { get; set; } = new();

    /// <summary>Operator actions the engine has learned from.</summary>
    public int ObservedActions { get; set; }

    /// <summary>Distinct condition → action associations held.</summary>
    public int LearnedAssociations { get; set; }

    /// <summary>Conditions active right now that rules can fire on.</summary>
    public int ActiveTriggers { get; set; }

    public DateTime GeneratedUtc { get; set; }
}
