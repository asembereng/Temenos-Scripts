using NUnit.Framework;

[assembly: LevelOfParallelism(4)]
[assembly: Parallelizable(ParallelScope.Fixtures)]

namespace TemenosAlertManager.Tests.Playwright;

[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // Global test setup
        TestContext.WriteLine("Starting Playwright tests for Temenos Alert Manager");
        
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        // Global test cleanup
        TestContext.WriteLine("Completed Playwright tests for Temenos Alert Manager");
    }
}