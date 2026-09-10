using SmartX.Api.Models;

namespace SmartX.Api.Data;

public class TestRepository : ITestRepository
{
    public TestResult GetStatus()
    {
        return new TestResult
        {
            Status = "ok",
            Message = "Smart-X API is running",
            Timestamp = DateTime.UtcNow
        };
    }
}
