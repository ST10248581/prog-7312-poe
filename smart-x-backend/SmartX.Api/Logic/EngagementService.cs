using SmartX.Api.Data;
using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public class EngagementService : IEngagementService
{
    private readonly IEngagementRepository _engagementRepository;

    public EngagementService(IEngagementRepository engagementRepository)
    {
        _engagementRepository = engagementRepository;
    }

    public EngagementState? GetEngagement(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? _engagementRepository.GetPrimary()
            : _engagementRepository.GetByUserId(userId);
    }
}
