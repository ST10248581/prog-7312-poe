using SmartX.Api.Models.Requests;

namespace SmartX.Api.Models.Stream;

/// <summary>
/// The device filter for the live readings panel. The same values the command
/// filter carries, minus the ones that only describe a command, so one filter
/// bar narrows both the devices and the traffic sent to them.
/// </summary>
public class DeviceQuery
{
    /// <summary>Matched against node id, name, zone, room, MAC address and category.</summary>
    public string? Search { get; set; }

    /// <summary>Operational categories to keep — Environmental, PowerConsumption and so on.</summary>
    public List<SensorCategory>? SensorCategories { get; set; }

    public List<NodeAlertState>? AlertStates { get; set; }

    public AlertSeverity? MinAlertSeverity { get; set; }

    public string? Zone { get; set; }
}

/// <summary>Every registered device that matched, with what it last reported.</summary>
public class LiveDeviceResult
{
    public List<LiveDevice> Items { get; set; } = new();

    /// <summary>Devices in the registry, before any filter.</summary>
    public int TotalRegistered { get; set; }

    /// <summary>
    /// Matches per category and per alert state, counted with every other filter
    /// applied but that facet's own — so a chip shows what selecting it would add.
    /// </summary>
    public Dictionary<string, int> CategoryCounts { get; set; } = new();
    public Dictionary<string, int> AlertStateCounts { get; set; } = new();

    public DateTime GeneratedUtc { get; set; }
}

/// <summary>
/// An exact lookup by node id or MAC address against the registry
/// dictionaries, with how long the probe took, so the O(1) claim is visible.
/// </summary>
public class DeviceLookupResult
{
    public string Key { get; set; } = string.Empty;

    /// <summary>The key as it was probed: node ids as typed, MAC addresses in canonical form.</summary>
    public string NormalisedKey { get; set; } = string.Empty;

    public bool Found { get; set; }

    /// <summary>"NodeId" or "MacAddress": which dictionary answered. Null when neither did.</summary>
    public string? MatchedBy { get; set; }

    /// <summary>Time spent in the dictionary probes alone.</summary>
    public double ElapsedMicroseconds { get; set; }

    /// <summary>Entries in the dictionary that answered, to show the time does not grow with it.</summary>
    public int RegistrySize { get; set; }

    public LiveDevice? Device { get; set; }
}

/// <summary>One device as the live panel shows it.</summary>
public class LiveDevice
{
    public string NodeId { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;

    public bool IsDisconnected { get; set; }
    public NodeAlertState AlertState { get; set; }
    public AlertSeverity? AlertSeverity { get; set; }
    public int OpenAlertCount { get; set; }

    /// <summary>When the intake last processed a reading from the device.</summary>
    public DateTime? LastReadingUtc { get; set; }

    public List<LiveReading> Readings { get; set; } = new();
}

/// <summary>One metric on a device: its latest value, its limits and a short trail behind it.</summary>
public class LiveReading
{
    public ReadingType ReadingType { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double? Value { get; set; }
    public DateTime? TimestampUtc { get; set; }
    public double? MinThreshold { get; set; }
    public double? MaxThreshold { get; set; }
    public bool IsBoolean { get; set; }

    /// <summary>Whether the latest value is outside the limits.</summary>
    public bool OutOfRange { get; set; }

    /// <summary>The most recent values, oldest first, for a sparkline.</summary>
    public List<double> Recent { get; set; } = new();
}

/// <summary>
/// One HTTP request an emulated device (or its gateway) makes to the intake:
/// who sent it and the packets it carries.
/// </summary>
public class DeviceTransmission
{
    /// <summary>The device's node id, or "gateway" for link reports and buffer flushes.</summary>
    public string Sender { get; init; } = string.Empty;

    public List<StreamPacketRequest> Packets { get; init; } = new();
}
