using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public interface ICommandRepository
{
    /// <summary>The audit trail: the filtered slice, newest first, one page at a time.</summary>
    PagedResult<DeviceCommand> Query(CommandQuery query);

    /// <summary>The live tail: the newest commands matching the filter, capped.</summary>
    List<DeviceCommand> GetStream(CommandQuery query, int take);

    /// <summary>Dispatch health and the per-minute throughput for the same slice.</summary>
    CommandSummary GetSummary(CommandQuery query);

    CommandFilterOptions GetFilterOptions();

    /// <summary>Appends an already-built command to the log.</summary>
    DeviceCommand Append(DeviceCommand command);
}
