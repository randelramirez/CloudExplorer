using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CloudExplorer.Infrastructure.CommandLine;

public sealed class ProcessCommandRunner(ILogger<ProcessCommandRunner> logger) : ICommandRunner
{
    private const int NoSuchFileOrDirectory = 2;
    private const int ExecFormatError = 8;
    private const int BadArchitecture = 86;

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
        catch (Win32Exception exception)
        {
            logger.LogWarning(exception, "Could not start {Executable}.", fileName);
            return new CommandResult(-1, "", DescribeStartFailure(fileName, exception));
        }
    }

    private static string DescribeStartFailure(string fileName, Win32Exception exception)
    {
        if (exception.NativeErrorCode == NoSuchFileOrDirectory || !File.Exists(fileName))
        {
            return $"{fileName} was not found. Install it and ensure it is available on PATH.";
        }

        // The executable exists but cannot be launched, most often an x86_64 CLI on an
        // Apple Silicon Mac without Rosetta, or an architecture mismatch on Linux.
        if (exception.NativeErrorCode is ExecFormatError or BadArchitecture)
        {
            return $"{fileName} was found but was built for a different processor architecture than this machine ({RuntimeInformation.OSArchitecture}). Reinstall the CLI for {RuntimeInformation.OSArchitecture}.";
        }

        return $"{fileName} could not be started. {exception.Message}";
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
