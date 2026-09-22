using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public interface ICommandService
{
    PagedResult<DeviceCommand> GetCommands(CommandQuery query);
    List<DeviceCommand> GetStream(CommandQuery query, int take);
    CommandSummary GetSummary(CommandQuery query);
    CommandFilterOptions GetFilterOptions();

    /// <summary>
    /// Queues a manual override. Returns the queued command, or the reason it
    /// was rejected — a dispatch to live hardware fails loudly, not silently.
    /// </summary>
    (DeviceCommand? command, string? error) Dispatch(DispatchCommandRequest request);
}
