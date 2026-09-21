using System;
using System.IO;
using System.Threading.Tasks;
using CloudExplorer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

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

    [Test]
    public async Task UnlaunchableExecutable_DoesNotReportItAsMissing()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("The failure mode is specific to exec on Unix.");
            return;
        }

        var executable = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"cloud-explorer-not-an-executable-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(executable, "\u0000not a real binary");
        File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        try
        {
            var runner = new ProcessCommandRunner(NullLogger<ProcessCommandRunner>.Instance);

            var result = await runner.RunAsync(executable, [], TimeSpan.FromSeconds(5));

            result.IsSuccess.Should().BeFalse();
            result.BestError.Should().NotContain("was not found");
            result.BestError.Should().Contain(executable);
        }
        finally
        {
            File.Delete(executable);
        }
    }
}
