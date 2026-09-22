using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Features.Aws;
using CloudExplorer.Infrastructure.CommandLine;
using CloudExplorer.Tests.Infrastructure.CommandLine;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CloudExplorer.Tests.Features.Aws;

public class AwsCliServiceTests
{
    [Test]
    public async Task GetProfilesAsync_UsesListProfilesCommandAndStatusTimeout()
    {
        using var cancellation = new CancellationTokenSource();
        var runner = new RecordingCommandRunner(Success("zeta\ndefault\nalpha\n"));
        var service = CreateService(runner);

        var profiles = await service.GetProfilesAsync(cancellation.Token);

        profiles.Select(static profile => profile.Id).Should().Equal("default", "alpha", "zeta");
        runner.Commands.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecordedCommand(
                ["configure", "list-profiles"],
                TimeSpan.FromSeconds(30),
                cancellation.Token),
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task GetAuthenticationAsync_CommandFailureMapsBestErrorWithoutParsingOutput()
    {
        var runner = new RecordingCommandRunner(new CommandResult(2, "ignored output", "expired credentials"));
        var service = CreateService(runner);

        var result = await service.GetAuthenticationAsync("team profile");

        result.IsAuthenticated.Should().BeFalse();
        result.Identity.Should().BeEmpty();
        result.ErrorMessage.Should().Be("expired credentials");
        runner.Commands.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecordedCommand(
                ["sts", "get-caller-identity", "--profile", "team profile", "--output", "json", "--no-cli-pager"],
                TimeSpan.FromSeconds(30),
                CancellationToken.None),
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task GetAuthenticationAsync_MalformedJsonMapsStableFailure()
    {
        var runner = new RecordingCommandRunner(Success("not-json"));
        var service = CreateService(runner);

        var result = await service.GetAuthenticationAsync("dev");

        result.IsAuthenticated.Should().BeFalse();
        result.ErrorMessage.Should().Be("AWS CLI returned an unexpected identity response.");
    }

    [Test]
    public async Task LoginAsync_SsoProfile_ProbesConfigurationThenUsesSsoLogin()
    {
        var runner = new RecordingCommandRunner(
            Success("company-session\n"),
            new CommandResult(1, "", "not configured"),
            Success());
        var service = CreateService(runner);

        var result = await service.LoginAsync("dev");

        result.IsSuccess.Should().BeTrue();
        runner.Commands.Select(static command => command.Arguments).Should().BeEquivalentTo(
            new[]
            {
                new[] { "configure", "get", "sso_session", "--profile", "dev" },
                new[] { "configure", "get", "sso_start_url", "--profile", "dev" },
                new[] { "sso", "login", "--profile", "dev", "--no-cli-pager" },
            },
            options => options.WithStrictOrdering());
        runner.Commands.Select(static command => command.Timeout).Should().Equal(
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(10));
    }

    [Test]
    public async Task LoginAsync_NonSsoProfile_UsesLoginAndMapsFailure()
    {
        var runner = new RecordingCommandRunner(
            new CommandResult(1, "", "unset"),
            new CommandResult(1, "", "unset"),
            new CommandResult(255, "", "login rejected"));
        var service = CreateService(runner);

        var result = await service.LoginAsync("legacy");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("login rejected");
        runner.Commands[2].Arguments.Should().Equal("login", "--profile", "legacy", "--no-cli-pager");
        runner.Commands[2].Timeout.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Test]
    public async Task GetResourcesAsync_UsesConfiguredRegionThenSearchesWithExactArguments()
    {
        const string json = """
            { "Resources": [] }
            """;
        var runner = new RecordingCommandRunner(Success("ap-southeast-1\n"), Success(json));
        var service = CreateService(runner);

        var result = await service.GetResourcesAsync("prod");

        result.IsSuccess.Should().BeTrue();
        result.Resources.Should().BeEmpty();
        runner.Commands.Select(static command => command.Arguments).Should().BeEquivalentTo(
            new[]
            {
                new[] { "configure", "get", "region", "--profile", "prod" },
                new[]
                {
                    "resource-explorer-2", "search",
                    "--query-string", "",
                    "--max-items", "1000",
                    "--profile", "prod",
                    "--region", "ap-southeast-1",
                    "--output", "json",
                    "--no-cli-pager",
                },
            },
            options => options.WithStrictOrdering());
        runner.Commands.Select(static command => command.Timeout).Should().Equal(
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(3));
    }

    [Test]
    public async Task GetResourcesAsync_UnauthorizedFailureAddsResourceExplorerGuidance()
    {
        var runner = new RecordingCommandRunner(
            Success("us-west-2"),
            new CommandResult(254, "", "Unauthorized to search"));
        var service = CreateService(runner);

        var result = await service.GetResourcesAsync("prod");

        result.IsSuccess.Should().BeFalse();
        result.Resources.Should().BeEmpty();
        result.ErrorMessage.Should().Be(
            "Unauthorized to search AWS Resource Explorer needs an index and a default view in the selected profile's configured region.");
    }

    [Test]
    public async Task GetResourcesAsync_MalformedJsonMapsStableFailure()
    {
        var runner = new RecordingCommandRunner(Success("us-east-2"), Success("not-json"));
        var service = CreateService(runner);

        var result = await service.GetResourcesAsync("dev");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("AWS CLI returned an unexpected resource response.");
    }

    private static AwsCliService CreateService(RecordingCommandRunner runner) =>
        new(runner, NullLogger<AwsCliService>.Instance);

    private static CommandResult Success(string standardOutput = "") => new(0, standardOutput, "");
}
