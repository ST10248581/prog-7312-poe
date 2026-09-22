using SmartX.Api.Data;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

/// <summary>
/// Command-stream application logic. Reads pass straight through to the
/// repository; a dispatch is validated against the live sensor profiles first,
/// so an override can only ever be aimed at a node that could accept it.
/// </summary>
public class CommandService : ICommandService
{
    private readonly ICommandRepository _commandRepository;
    private readonly ISensorProfileRepository _sensorRepository;

    public CommandService(
        ICommandRepository commandRepository,
        ISensorProfileRepository sensorRepository)
    {
        _commandRepository = commandRepository;
        _sensorRepository = sensorRepository;
    }

    public PagedResult<DeviceCommand> GetCommands(CommandQuery query)
    {
        return _commandRepository.Query(query);
    }

    public List<DeviceCommand> GetStream(CommandQuery query, int take)
    {
        return _commandRepository.GetStream(query, take);
    }

    public CommandSummary GetSummary(CommandQuery query)
    {
        return _commandRepository.GetSummary(query);
    }

    public CommandFilterOptions GetFilterOptions()
    {
        return _commandRepository.GetFilterOptions();
    }

    public (DeviceCommand? command, string? error) Dispatch(DispatchCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NodeId))
        {
            return (null, "A target node is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Parameters))
        {
            return (null, "Command parameters are required.");
        }

        var sensor = _sensorRepository.GetProfiles().FirstOrDefault(profile =>
            string.Equals(profile.NodeId, request.NodeId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (sensor is null)
        {
            return (null, $"No node registered with id '{request.NodeId}'.");
        }

        // A dry run is validated and logged against an unreachable node, but a
        // real dispatch to one would sit queued until it expired.
        if (!request.DryRun && (!sensor.IsActive || sensor.Status == SensorStatus.Offline))
        {
            return (null, $"{sensor.NodeId} is offline and cannot accept a dispatch. Send it as a dry run to log the intent.");
        }

        var command = new DeviceCommand
        {
            Id = Guid.NewGuid(),
            SensorProfileId = sensor.Id,
            NodeId = sensor.NodeId,
            SensorName = sensor.Name,
            Zone = sensor.Zone,
            CommandType = request.CommandType,
            Parameters = request.Parameters.Trim(),
            Origin = CommandOrigin.Manual,
            Priority = request.Priority,
            Status = CommandStatus.Queued,
            IssuedUtc = DateTime.UtcNow,
            IssuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? "operator" : request.IssuedBy.Trim(),
            Retries = 0,
            IsDryRun = request.DryRun
        };

        // Queued only. The dispatch simulator picks it up on its next tick and
        // advances it exactly as it does automated traffic.
        return (_commandRepository.Append(command), null);
    }
}
