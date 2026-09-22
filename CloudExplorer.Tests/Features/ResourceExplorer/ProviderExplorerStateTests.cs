using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Features.Aws;
using CloudExplorer.Features.Azure;
using CloudExplorer.Features.ResourceExplorer;
using FluentAssertions;
using NUnit.Framework;

namespace CloudExplorer.Tests.Features.ResourceExplorer;

public class ProviderExplorerStateTests
{
    [Test]
    public async Task AwsAccountChange_ClearsPreviousAccountWhileLoadingAndQueriesOnlyNewAccount()
    {
        var service = new ControllableAwsCliService();
        var viewModel = new AwsExplorerViewModel(service);
        await viewModel.InitializeAsync();
        var newAccountAuthentication = NewCompletionSource<AuthenticationResult>();
        service.NextAuthentication = newAccountAuthentication.Task;

        var changeTask = viewModel.ChangeAccountAsync(service.SecondAccount);

        viewModel.SelectedAccount.Should().Be(service.SecondAccount);
        viewModel.IsBusy.Should().BeTrue();
        viewModel.ResourceCount.Should().Be(0);
        viewModel.Rows.Should().BeEmpty();
        viewModel.LastRefreshedAt.Should().BeNull();
        service.AuthenticationAccounts.Should().Equal("aws-a", "aws-b");
        service.ResourceAccounts.Should().Equal("aws-a");

        newAccountAuthentication.SetResult(new AuthenticationResult(true, "AWS B identity"));
        await changeTask;

        viewModel.IsBusy.Should().BeFalse();
        viewModel.Identity.Should().Be("AWS B identity");
        viewModel.ResourceCount.Should().Be(1);
        viewModel.LastRefreshedAt.Should().NotBeNull();
        service.ResourceAccounts.Should().Equal("aws-a", "aws-b");
    }

    [Test]
    public async Task AzureAccountChange_ClearsPreviousAccountWhileLoadingAndQueriesOnlyNewAccount()
    {
        var service = new ControllableAzureCliService();
        var viewModel = new AzureExplorerViewModel(service);
        await viewModel.InitializeAsync();
        var newAccountAuthentication = NewCompletionSource<AuthenticationResult>();
        service.NextAuthentication = newAccountAuthentication.Task;

        var changeTask = viewModel.ChangeAccountAsync(service.SecondAccount);

        viewModel.SelectedAccount.Should().Be(service.SecondAccount);
        viewModel.IsBusy.Should().BeTrue();
        viewModel.ResourceCount.Should().Be(0);
        viewModel.Rows.Should().BeEmpty();
        viewModel.LastRefreshedAt.Should().BeNull();
        service.AuthenticationAccounts.Should().Equal("azure-a", "azure-b");
        service.ResourceAccounts.Should().Equal("azure-a");

        newAccountAuthentication.SetResult(new AuthenticationResult(true, "Azure B identity"));
        await changeTask;

        viewModel.IsBusy.Should().BeFalse();
        viewModel.Identity.Should().Be("Azure B identity");
        viewModel.ResourceCount.Should().Be(1);
        service.ResourceAccounts.Should().Equal("azure-a", "azure-b");
    }

    [Test]
    public async Task RefreshCommand_WhileRefreshIsRunning_DoesNotStartOverlappingOperation()
    {
        var service = new ControllableAwsCliService();
        var viewModel = new AwsExplorerViewModel(service);
        await viewModel.InitializeAsync();
        var refreshAuthentication = NewCompletionSource<AuthenticationResult>();
        service.NextAuthentication = refreshAuthentication.Task;

        var firstRefresh = viewModel.RefreshCommand.ExecuteAsync(null);
        var overlappingRefresh = viewModel.RefreshCommand.ExecuteAsync(null);
        await overlappingRefresh;

        viewModel.IsBusy.Should().BeTrue();
        service.AuthenticationAccounts.Should().Equal("aws-a", "aws-a");
        service.ResourceAccounts.Should().Equal("aws-a");

        refreshAuthentication.SetResult(new AuthenticationResult(true, "refreshed identity"));
        await firstRefresh;

        viewModel.IsBusy.Should().BeFalse();
        service.AuthenticationAccounts.Should().HaveCount(2);
        service.ResourceAccounts.Should().Equal("aws-a", "aws-a");
    }

    [Test]
    public async Task RefreshCommand_WhenServiceThrows_MapsErrorAndRetainsDisplayedResources()
    {
        var service = new ControllableAzureCliService();
        var viewModel = new AzureExplorerViewModel(service);
        await viewModel.InitializeAsync();
        var originalRows = viewModel.Rows;
        service.AuthenticationException = new InvalidOperationException("Azure CLI cache is unavailable");

        await viewModel.RefreshCommand.ExecuteAsync(null);

        viewModel.IsBusy.Should().BeFalse();
        viewModel.HasError.Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("Azure CLI cache is unavailable");
        viewModel.StatusMessage.Should().Be("Could not complete the operation");
        viewModel.ResourceCount.Should().Be(1);
        viewModel.Rows.Should().BeSameAs(originalRows);
        service.ResourceAccounts.Should().Equal("azure-a");
    }

    private static TaskCompletionSource<T> NewCompletionSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static CloudResource Resource(CloudProvider provider, string accountId) => new()
    {
        Provider = provider,
        Id = $"{provider}-{accountId}",
        Name = $"resource-{accountId}",
        ResourceType = "test:resource",
        GroupName = "test-group",
        AccountId = accountId,
    };

    private sealed class ControllableAwsCliService : IAwsCliService
    {
        public CloudAccountOption SecondAccount { get; } = new("aws-b", "AWS B");

        public Task<AuthenticationResult>? NextAuthentication { get; set; }

        public List<string> AuthenticationAccounts { get; } = [];

        public List<string> ResourceAccounts { get; } = [];

        public Task<IReadOnlyList<CloudAccountOption>> GetProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudAccountOption>>(
                [new("aws-a", "AWS A", IsDefault: true), SecondAccount]);

        public Task<AuthenticationResult> GetAuthenticationAsync(
            string profile,
            CancellationToken cancellationToken = default)
        {
            AuthenticationAccounts.Add(profile);
            var result = NextAuthentication;
            NextAuthentication = null;
            return result ?? Task.FromResult(new AuthenticationResult(true, $"{profile} identity"));
        }

        public Task<OperationResult> LoginAsync(string profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());

        public Task<ResourceQueryResult> GetResourcesAsync(
            string profile,
            CancellationToken cancellationToken = default)
        {
            ResourceAccounts.Add(profile);
            return Task.FromResult(ResourceQueryResult.Success([Resource(CloudProvider.Aws, profile)]));
        }
    }

    private sealed class ControllableAzureCliService : IAzureCliService
    {
        public CloudAccountOption SecondAccount { get; } = new("azure-b", "Azure B");

        public Task<AuthenticationResult>? NextAuthentication { get; set; }

        public Exception? AuthenticationException { get; set; }

        public List<string> AuthenticationAccounts { get; } = [];

        public List<string> ResourceAccounts { get; } = [];

        public Task<IReadOnlyList<CloudAccountOption>> GetSubscriptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudAccountOption>>(
                [new("azure-a", "Azure A", IsDefault: true), SecondAccount]);

        public Task<AuthenticationResult> GetAuthenticationAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            AuthenticationAccounts.Add(subscriptionId);
            if (AuthenticationException is not null)
            {
                throw AuthenticationException;
            }

            var result = NextAuthentication;
            NextAuthentication = null;
            return result ?? Task.FromResult(new AuthenticationResult(true, $"{subscriptionId} identity"));
        }

        public Task<OperationResult> LoginAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());

        public Task<ResourceQueryResult> GetResourcesAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            ResourceAccounts.Add(subscriptionId);
            return Task.FromResult(ResourceQueryResult.Success([Resource(CloudProvider.Azure, subscriptionId)]));
        }
    }
}
