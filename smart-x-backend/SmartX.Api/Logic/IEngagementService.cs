using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public interface IEngagementService
{
    EngagementState? GetEngagement(string? userId);
}
