// =============================================================================
// CODE ATTRIBUTION — Advanced Arrays and Lists (Technical and Language Requirement 3)
//
// The `double[][]` jagged array declaration used to carry ragged batches was
// written with reference to the source below.
//
// Code Attribution [9]
// Author: Microsoft
// Year: 2026
// Title: The array reference type – C# reference
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/arrays>
// Accessed: [Accessed 13 September 2026]
// Modifications: Used the jagged array declaration syntax for the `double[][]`
//   request property: one row per gateway batch, with rows of different
//   lengths. No example code copied.
// Reference: Microsoft, 2026. The array reference type – C# reference [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/arrays> [Accessed 13 September 2026].
// =============================================================================

using SmartX.Api.Models.Telemetry;

namespace SmartX.Api.Models.Requests;

/// <summary>
/// A gateway flushing its buffer: several sequential batches of raw samples for a
/// single sensor, newest batch last. Batches are ragged by nature — a gateway
/// sends whatever it captured between flushes — so they arrive as a jagged array.
/// </summary>
public class IngestTelemetryRequest
{
    public ReadingType ReadingType { get; set; }

    /// <summary>
    /// How the gateway encoded the payload. Left null, a sensible default is
    /// chosen from <see cref="ReadingType"/>.
    /// </summary>
    public TelemetryPayloadKind? PayloadKind { get; set; }

    /// <summary>Timestamp of the very first sample. Defaults so the block ends now.</summary>
    public DateTime? StartUtc { get; set; }

    /// <summary>Sampling interval between consecutive samples, in seconds.</summary>
    public int IntervalSeconds { get; set; } = 600;

    public string SourceIpAddress { get; set; } = string.Empty;

    /// <summary>Jagged: one row per batch, each row a run of raw samples.</summary>
    public double[][] Batches { get; set; } = Array.Empty<double[]>();
}
