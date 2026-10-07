using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public interface ITestService
{
    Task<TestResult> GetStatusAsync(CancellationToken cancellationToken = default);
}
