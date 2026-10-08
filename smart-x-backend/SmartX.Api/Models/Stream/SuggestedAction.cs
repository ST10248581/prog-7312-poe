// =============================================================================
// CODE ATTRIBUTION — Predictive action and recommendation engine (Part 2)
//
// The suggestion kinds and learning statistics in this file describe the output
// of the command engine's action engine, which was written with reference to
// the sources below.
//
// Code Attribution [20]
// Author: Agrawal, R., Imieliński, T. and Swami, A.
// Year: 1993
// Title: Mining association rules between sets of items in large databases
// Type: [Source code]
// Available at: <https://doi.org/10.1145/170035.170072>
// Accessed: [Accessed 29 September 2026]
// Modifications: SuggestionKind.PredictedAction and LearnedRule carry
//   association-rule suggestions with their support and confidence, the two
//   measures defined in the paper.
// Reference: Agrawal, R., Imieliński, T. and Swami, A., 1993. Mining association rules between sets of items in large databases [Source code] Available at: <https://doi.org/10.1145/170035.170072> [Accessed 29 September 2026].
//
// Code Attribution [21]
// Author: Jurafsky, D. and Martin, J.H.
// Year: 2025
// Title: Speech and Language Processing, Chapter 3: N-gram Language Models
// Version: 3rd edition draft
// Type: [Source code]
// Available at: <https://web.stanford.edu/~jurafsky/slp3/>
// Accessed: [Accessed 29 September 2026]
// Modifications: SuggestionKind.NextStep carries the bigram (first-order
//   Markov) next-action suggestions and their estimated probability.
// Reference: Jurafsky, D. and Martin, J.H., 2025. Speech and Language Processing, Chapter 3: N-gram Language Models (3rd edition draft) [Source code] Available at: <https://web.stanford.edu/~jurafsky/slp3/> [Accessed 29 September 2026].
//
// Code Attribution [22]
// Author: Welford, B.P.
// Year: 1962
// Title: Note on a method for calculating corrected sums of squares and products
// Type: [Source code]
// Available at: <https://doi.org/10.1080/00401706.1962.10490022>
// Accessed: [Accessed 29 September 2026]
// Modifications: SuggestionKind.ProblemDevice carries the drift-based device
//   flags, scored with a z-score over Welford's running mean and variance.
// Reference: Welford, B.P., 1962. Note on a method for calculating corrected sums of squares and products [Source code] Available at: <https://doi.org/10.1080/00401706.1962.10490022> [Accessed 29 September 2026].
// =============================================================================

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
    SelectNode,

    /// <summary>A filter facet the operator switched on, sent as "facet:value", e.g. "zone:Zone B".</summary>
    Filter
}

/// <summary>What the operator did with a suggestion.</summary>
public enum SuggestionFeedback
{
    Applied,
    Dismissed
}

/// <summary>
/// One recommendation from the automated action engine. It carries enough to
/// act on directly: a search term or filter to apply, a node to inspect, or a
/// command to send.
/// </summary>
public class SuggestedAction
{
    /// <summary>Stable across refreshes, so feedback and dismissals attach to the same suggestion.</summary>
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

    /// <summary>For a learned filter: which facet (zone, sensorCategory, alertState, status, commandType) and its value.</summary>
    public string? FilterFacet { get; set; }
    public string? FilterValue { get; set; }

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

    /// <summary>What the engine has learned so far, and how its suggestions have been received.</summary>
    public LearningStats Learning { get; set; } = new();

    public DateTime GeneratedUtc { get; set; }
}

public class LearningStats
{
    /// <summary>Condition ⇒ action rules that currently clear the support and confidence thresholds.</summary>
    public int RulesLearned { get; set; }

    /// <summary>Distinct action → next-action transitions in the session model.</summary>
    public int TransitionsLearned { get; set; }

    /// <summary>The strongest rules, best first.</summary>
    public List<LearnedRule> TopRules { get; set; } = new();

    public int Applied { get; set; }
    public int Dismissed { get; set; }

    /// <summary>Applied ÷ (applied + dismissed); null before any feedback.</summary>
    public double? AcceptanceRate { get; set; }

    /// <summary>The operator's most recent recorded actions, newest first.</summary>
    public List<ActivityEntry> RecentActivity { get; set; } = new();
}

public class LearnedRule
{
    public string Condition { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    /// <summary>Times the action followed the condition.</summary>
    public int Count { get; set; }

    /// <summary>Times the condition was seen with an operator present.</summary>
    public int Support { get; set; }

    public double Confidence { get; set; }
}

public class ActivityEntry
{
    public string Description { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; }
}
