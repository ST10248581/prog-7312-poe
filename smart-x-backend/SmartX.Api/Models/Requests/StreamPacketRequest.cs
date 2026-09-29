using SmartX.Api.Models.Stream;

namespace SmartX.Api.Models.Requests;

/// <summary>
/// One telemetry packet posted to the command engine's intake. The device is
/// named by node id or MAC address, whichever the gateway carries.
/// </summary>
public class StreamPacketRequest
{
    public string? NodeId { get; set; }
    public string? MacAddress { get; set; }
    public ReadingType ReadingType { get; set; }

    /// <summary>Null (or NaN) for a lost sample.</summary>
    public double? Value { get; set; }

    /// <summary>When the sample was taken. Defaults to now.</summary>
    public DateTime? TimestampUtc { get; set; }

    /// <summary>False reports that the gateway has lost the node.</summary>
    public bool LinkUp { get; set; } = true;
}

/// <summary>Something an operator did on the page that the action engine learns from.</summary>
public class OperatorActivityRequest
{
    public OperatorActivityKind Kind { get; set; }
    public string Value { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = "operator";
}

public class UndoOverrideRequest
{
    public string IssuedBy { get; set; } = "operator";
}
