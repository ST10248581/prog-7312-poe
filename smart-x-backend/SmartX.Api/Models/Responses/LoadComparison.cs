namespace SmartX.Api.Models.Responses;

/// <summary>One side of a load comparison, flattened for the wire.</summary>
public class LoadReading
{
    public Guid SensorProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SampleCount { get; set; }
    public DateTime? LastReadingUtc { get; set; }
}

/// <summary>
/// The result of subtracting one meter from another, plus the verdict produced by
/// the overloaded comparison operators.
/// </summary>
public class LoadComparison
{
    public LoadReading Left { get; set; } = new();
    public LoadReading Right { get; set; } = new();

    /// <summary>Left minus right.</summary>
    public double Delta { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>Delta as a percentage of the right-hand meter.</summary>
    public double DeltaPercent { get; set; }

    public bool LeftIsHeavier { get; set; }
    public bool AreEquivalent { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>Aggregate draw for a set of meters, produced by folding with <c>operator +</c>.</summary>
public class AggregateLoad
{
    public string Scope { get; set; } = string.Empty;
    public double TotalValue { get; set; }
    public double AverageValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int MeterCount { get; set; }
    public List<LoadReading> Contributors { get; set; } = new();
    public DateTime GeneratedUtc { get; set; }
}
