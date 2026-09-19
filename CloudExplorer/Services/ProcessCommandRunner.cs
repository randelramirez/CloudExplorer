using System.Diagnostics;

namespace CloudExplorer.Services;

public sealed class ProcessCommandRunner(ILogger<ProcessCommandRunner> logger) : ICommandRunner
{
    public async Task<CommandResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        logger.LogDebug("Starting {Executable} with {ArgumentCount} arguments.", fileName, process.StartInfo.ArgumentList.Count);

        try
        {
            if (!process.Start())
            {
                return new CommandResult(-1, "", $"Could not start {fileName}.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                return new CommandResult(-1, await SafeReadAsync(outputTask), await SafeReadAsync(errorTask), TimedOut: true);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            return new CommandResult(
                process.ExitCode,
                await outputTask,
                await errorTask);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CommandResult(-1, "", $"{fileName} was not found. Install it and ensure it is available on PATH.");
        }
    }

    private static async Task<string> SafeReadAsync(Task<string> task)
    {
        try
        {
            return await task;
        }
        catch (OperationCanceledException)
        {
            return "";
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
