using SmartX.Api.Models;

namespace SmartX.Api.Data;

public class EngagementRepository : IEngagementRepository
{
    private readonly ISmartXDataStore _store;

    public EngagementRepository(ISmartXDataStore store)
    {
        _store = store;
    }

    public EngagementState? GetByUserId(string userId)
    {
        return _store.EngagementStates.FirstOrDefault(state =>
            string.Equals(state.UserId, userId, StringComparison.OrdinalIgnoreCase));
    }

    public EngagementState? GetPrimary()
    {
        return _store.EngagementStates.FirstOrDefault();
    }
}
