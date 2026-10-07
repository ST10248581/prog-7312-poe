using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public interface IEngagementService
{
    Task<EngagementState?> GetEngagementAsync(string? userId, CancellationToken cancellationToken = default);
}
