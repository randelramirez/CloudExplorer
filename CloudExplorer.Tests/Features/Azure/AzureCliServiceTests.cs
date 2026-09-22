using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Features.Azure;
using CloudExplorer.Infrastructure.CommandLine;
using CloudExplorer.Tests.Infrastructure.CommandLine;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CloudExplorer.Tests.Features.Azure;

public class AzureCliServiceTests
{
    [Test]
    public async Task GetSubscriptionsAsync_UsesAccountListCommandAndStatusTimeout()
    {
        const string json = """
            [{ "id": "sub-1", "name": "Primary", "state": "Enabled", "isDefault": true }]
            """;
        using var cancellation = new CancellationTokenSource();
        var runner = new RecordingCommandRunner(Success(json));
        var service = CreateService(runner);

        var subscriptions = await service.GetSubscriptionsAsync(cancellation.Token);

        subscriptions.Should().ContainSingle().Which.Id.Should().Be("sub-1");
        runner.Commands.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecordedCommand(
                ["account", "list", "--all", "--output", "json", "--only-show-errors"],
                TimeSpan.FromSeconds(30),
                cancellation.Token),
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task GetSubscriptionsAsync_CommandFailureThrowsBestError()
    {
        var runner = new RecordingCommandRunner(new CommandResult(1, "", "run az login"));
        var service = CreateService(runner);

        var action = () => service.GetSubscriptionsAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("run az login");
    }

    [Test]
    public async Task GetAuthenticationAsync_VerifiesTokenBeforeReadingAccountIdentity()
    {
        const string identityJson = """
            { "id": "sub-1", "name": "Primary", "tenantId": "tenant-1", "user": { "name": "person@example.com" } }
            """;
        var runner = new RecordingCommandRunner(Success("1790000000"), Success(identityJson));
        var service = CreateService(runner);

        var result = await service.GetAuthenticationAsync("sub-1");

        result.IsAuthenticated.Should().BeTrue();
        result.Identity.Should().NotBeEmpty();
        runner.Commands.Select(static command => command.Arguments).Should().BeEquivalentTo(
            new[]
            {
                new[]
                {
                    "account", "get-access-token",
                    "--subscription", "sub-1",
                    "--query", "expires_on",
                    "--output", "tsv",
                    "--only-show-errors",
                },
                new[]
                {
                    "account", "show",
                    "--subscription", "sub-1",
                    "--output", "json",
                    "--only-show-errors",
                },
            },
            options => options.WithStrictOrdering());
        runner.Commands.Select(static command => command.Timeout).Should().OnlyContain(timeout => timeout == TimeSpan.FromSeconds(30));
    }

    [Test]
    public async Task GetAuthenticationAsync_TokenFailureMapsErrorAndStopsCommandSequence()
    {
        var runner = new RecordingCommandRunner(new CommandResult(1, "", "token expired"));
        var service = CreateService(runner);

        var result = await service.GetAuthenticationAsync("sub-2");

        result.IsAuthenticated.Should().BeFalse();
        result.ErrorMessage.Should().Be("token expired");
        runner.Commands.Should().ContainSingle();
    }

    [Test]
    public async Task GetAuthenticationAsync_MalformedIdentityMapsStableFailure()
    {
        var runner = new RecordingCommandRunner(Success("1790000000"), Success("not-json"));
        var service = CreateService(runner);

        var result = await service.GetAuthenticationAsync("sub-1");

        result.IsAuthenticated.Should().BeFalse();
        result.ErrorMessage.Should().Be("Azure CLI returned an unexpected identity response.");
    }

    [Test]
    public async Task LoginAsync_UsesNonInteractiveOutputArgumentsAndMapsFailure()
    {
        var runner = new RecordingCommandRunner(new CommandResult(1, "", "browser login cancelled"));
        var service = CreateService(runner);

        var result = await service.LoginAsync();

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("browser login cancelled");
        runner.Commands.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecordedCommand(
                ["login", "--output", "none", "--only-show-errors"],
                TimeSpan.FromMinutes(10),
                CancellationToken.None),
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task GetResourcesAsync_UsesSubscriptionCommandAndMapsFailureVerbatim()
    {
        var runner = new RecordingCommandRunner(new CommandResult(3, "", "subscription disabled"));
        var service = CreateService(runner);

        var result = await service.GetResourcesAsync("sub-3");

        result.IsSuccess.Should().BeFalse();
        result.Resources.Should().BeEmpty();
        result.ErrorMessage.Should().Be("subscription disabled");
        runner.Commands.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecordedCommand(
                ["resource", "list", "--subscription", "sub-3", "--output", "json", "--only-show-errors"],
                TimeSpan.FromMinutes(3),
                CancellationToken.None),
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task GetResourcesAsync_MalformedJsonMapsStableFailure()
    {
        var runner = new RecordingCommandRunner(Success("not-json"));
        var service = CreateService(runner);

        var result = await service.GetResourcesAsync("sub-1");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Azure CLI returned an unexpected resource response.");
    }

    private static AzureCliService CreateService(RecordingCommandRunner runner) =>
        new(runner, NullLogger<AzureCliService>.Instance);

    private static CommandResult Success(string standardOutput = "") => new(0, standardOutput, "");
}
