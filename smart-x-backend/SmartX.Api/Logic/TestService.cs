using SmartX.Api.Data;
using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public class TestService : ITestService
{
    private readonly ITestRepository _testRepository;

    public TestService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public Task<TestResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_testRepository.GetStatus());
    }
}
