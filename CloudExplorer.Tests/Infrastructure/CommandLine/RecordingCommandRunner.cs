using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Infrastructure.CommandLine;

namespace CloudExplorer.Tests.Infrastructure.CommandLine;

internal sealed class RecordingCommandRunner(params CommandResult[] results) : ICommandRunner
{
    private readonly Queue<CommandResult> _results = new(results);

    public List<RecordedCommand> Commands { get; } = [];

    public Task<CommandResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Commands.Add(new RecordedCommand(arguments.ToArray(), timeout, cancellationToken));
        if (_results.Count == 0)
        {
            throw new InvalidOperationException("The test did not configure a result for this command.");
        }

        return Task.FromResult(_results.Dequeue());
    }
}

internal sealed record RecordedCommand(
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout,
    CancellationToken CancellationToken);
