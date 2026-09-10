using SmartX.Api.Models;

namespace SmartX.Api.Data;

public interface ITestRepository
{
    TestResult GetStatus();
}
