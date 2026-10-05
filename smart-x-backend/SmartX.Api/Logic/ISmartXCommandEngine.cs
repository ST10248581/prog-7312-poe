using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;
using SmartX.Api.Models.Stream;

namespace SmartX.Api.Logic;

/// <summary>
/// Everything behind the Real-Time Command Stream and History page: the command
/// log and its queries, manual overrides and their undo history, the telemetry
/// intake that drives alerts, and the automated action engine.
/// </summary>
public interface ISmartXCommandEngine
{
    /* ---------- Command log ---------- */

    PagedResult<DeviceCommand> GetCommands(CommandQuery query);
    List<DeviceCommand> GetStream(CommandQuery query, int take);
    CommandSummary GetSummary(CommandQuery query);
    CommandFilterOptions GetFilterOptions();

    /* ---------- Manual overrides ---------- */

    /// <summary>
    /// Queues a manual override. Returns the queued command, or the reason it
    /// was rejected — a dispatch to live hardware fails loudly, not silently.
    /// </summary>
    (DeviceCommand? command, string? error) Dispatch(DispatchCommandRequest request);

    /// <summary>The undo stack, most recent override first.</summary>
    List<OverrideHistoryEntry> GetOverrideHistory();

    /// <summary>Pops the most recent override and cancels or reverts it.</summary>
    (UndoResult? result, string? error) UndoLastOverride(string? issuedBy);

    /* ---------- Telemetry intake ---------- */

    PacketIntakeResult IngestPackets(IEnumerable<StreamPacketRequest> packets);
    PipelineStatus GetPipelineStatus();
    NodeTimeline? GetNodeTimeline(string nodeId, int minutes, int maxPoints);

    /// <summary>Every registered device matching the query, with its latest readings.</summary>
    LiveDeviceResult GetLiveDevices(DeviceQuery query);

    /* ---------- Device emulation ---------- */

    /// <summary>
    /// What the emulated devices and gateways would send this tick. Nothing here
    /// touches the intake: <see cref="DeviceTelemetrySimulator"/> posts each
    /// transmission to the packets endpoint over HTTP, as a real device would.
    /// </summary>
    IReadOnlyList<DeviceTransmission> ComposeDeviceTransmissions();

    /* ---------- Automated action engine ---------- */

    InsightsResponse GetInsights(string? issuedBy);
    void RecordActivity(OperatorActivityRequest request);

    /* ---------- Live loop ---------- */

    /// <summary>
    /// One tick of the mesh: advances in-flight commands, issues automated
    /// traffic and drains both intake lanes.
    /// </summary>
    void RunDispatchCycle();
}
