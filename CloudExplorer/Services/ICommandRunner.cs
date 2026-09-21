using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudExplorer.Services;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed record CommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false)
{
    public bool IsSuccess => ExitCode == 0 && !TimedOut;

    public string BestError => TimedOut
        ? "The command timed out."
        : string.IsNullOrWhiteSpace(StandardError)
            ? string.IsNullOrWhiteSpace(StandardOutput) ? $"Command exited with code {ExitCode}." : StandardOutput.Trim()
            : StandardError.Trim();
}
