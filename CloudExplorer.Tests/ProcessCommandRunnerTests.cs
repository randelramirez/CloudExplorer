using CloudExplorer.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudExplorer.Tests;

public class ProcessCommandRunnerTests
{
    [Test]
    public async Task MissingExecutable_ReturnsActionableFailure()
    {
        var runner = new ProcessCommandRunner(NullLogger<ProcessCommandRunner>.Instance);

        var result = await runner.RunAsync(
            "cloud-explorer-command-that-does-not-exist",
            [],
            TimeSpan.FromSeconds(1));

        result.IsSuccess.Should().BeFalse();
        result.BestError.Should().Contain("was not found");
    }
}
