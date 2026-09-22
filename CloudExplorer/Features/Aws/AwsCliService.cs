using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Features.ResourceExplorer;
using CloudExplorer.Infrastructure.CommandLine;
using Microsoft.Extensions.Logging;

namespace CloudExplorer.Features.Aws;

public sealed class AwsCliService(ICommandRunner commandRunner, ILogger<AwsCliService> logger) : IAwsCliService
{
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(10);
    private readonly string _awsExecutable = CliExecutableLocator.Resolve("aws");

    public async Task<IReadOnlyList<CloudAccountOption>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync(
            _awsExecutable,
            ["configure", "list-profiles"],
            StatusTimeout,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.BestError);
        }

        var requestedProfile = Environment.GetEnvironmentVariable("AWS_PROFILE");
        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(profile => string.Equals(profile, requestedProfile, StringComparison.Ordinal))
            .ThenByDescending(static profile => string.Equals(profile, "default", StringComparison.Ordinal))
            .ThenBy(static profile => profile, StringComparer.OrdinalIgnoreCase)
            .Select(profile => new CloudAccountOption(profile, profile, "AWS CLI profile", profile == requestedProfile))
            .ToArray();
    }

    public async Task<AuthenticationResult> GetAuthenticationAsync(
        string profile,
        CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync(
            _awsExecutable,
            ["sts", "get-caller-identity", "--profile", profile, "--output", "json", "--no-cli-pager"],
            StatusTimeout,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return new AuthenticationResult(false, ErrorMessage: result.BestError);
        }

        try
        {
            return new AuthenticationResult(true, JsonResourceParser.ParseAwsIdentity(result.StandardOutput));
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "AWS CLI returned an unexpected identity response.");
            return new AuthenticationResult(false, ErrorMessage: "AWS CLI returned an unexpected identity response.");
        }
    }

    public async Task<OperationResult> LoginAsync(string profile, CancellationToken cancellationToken = default)
    {
        var ssoSession = await commandRunner.RunAsync(
            _awsExecutable,
            ["configure", "get", "sso_session", "--profile", profile],
            StatusTimeout,
            cancellationToken);
        var ssoStartUrl = await commandRunner.RunAsync(
            _awsExecutable,
            ["configure", "get", "sso_start_url", "--profile", profile],
            StatusTimeout,
            cancellationToken);

        var usesSso = !string.IsNullOrWhiteSpace(ssoSession.StandardOutput) ||
            !string.IsNullOrWhiteSpace(ssoStartUrl.StandardOutput);
        var arguments = usesSso
            ? new[] { "sso", "login", "--profile", profile, "--no-cli-pager" }
            : new[] { "login", "--profile", profile, "--no-cli-pager" };

        var result = await commandRunner.RunAsync(_awsExecutable, arguments, LoginTimeout, cancellationToken);
        return result.IsSuccess
            ? OperationResult.Success()
            : OperationResult.Failure(result.BestError);
    }

    public async Task<ResourceQueryResult> GetResourcesAsync(
        string profile,
        CancellationToken cancellationToken = default)
    {
        var regionResult = await commandRunner.RunAsync(
            _awsExecutable,
            ["configure", "get", "region", "--profile", profile],
            StatusTimeout,
            cancellationToken);
        var region = regionResult.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(region))
        {
            region = Environment.GetEnvironmentVariable("AWS_REGION") ??
                Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ??
                "us-east-1";
        }

        var result = await commandRunner.RunAsync(
            _awsExecutable,
            [
                "resource-explorer-2", "search",
                "--query-string", "",
                "--max-items", "1000",
                "--profile", profile,
                "--region", region,
                "--output", "json",
                "--no-cli-pager",
            ],
            QueryTimeout,
            cancellationToken);

        if (!result.IsSuccess)
        {
            var guidance = result.BestError.Contains("default view", StringComparison.OrdinalIgnoreCase) ||
                result.BestError.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
                ? " AWS Resource Explorer needs an index and a default view in the selected profile's configured region."
                : "";
            return ResourceQueryResult.Failure(result.BestError + guidance);
        }

        try
        {
            return ResourceQueryResult.Success(JsonResourceParser.ParseAwsResources(result.StandardOutput));
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "AWS CLI returned an unexpected resource response.");
            return ResourceQueryResult.Failure("AWS CLI returned an unexpected resource response.");
        }
    }
}
