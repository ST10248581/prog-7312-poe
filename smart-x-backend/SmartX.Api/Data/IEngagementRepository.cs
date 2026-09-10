using SmartX.Api.Models;

namespace SmartX.Api.Data;

public interface IEngagementRepository
{
    EngagementState? GetByUserId(string userId);
    EngagementState? GetPrimary();
}
