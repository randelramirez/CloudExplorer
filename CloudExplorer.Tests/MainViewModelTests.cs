using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Models;
using CloudExplorer.Services;
using CloudExplorer.ViewModels;
using FluentAssertions;
using NUnit.Framework;

namespace CloudExplorer.Tests;

public class MainViewModelTests
{
    [Test]
    public void ProviderViewMode_DefaultsToBoth()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedProviderViewMode.Should().Be(ProviderViewMode.Both);
    }

    [TestCase(ProviderViewMode.Both)]
    [TestCase(ProviderViewMode.Aws)]
    [TestCase(ProviderViewMode.Azure)]
    public void ProviderViewMode_AllowsEachExplicitSelection(ProviderViewMode selection)
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedProviderViewMode = selection;

        viewModel.SelectedProviderViewMode.Should().Be(selection);
    }

    [Test]
    public async Task InitializeVisibleProvidersAsync_DefaultBoth_InitializesEachProviderExactlyOnce()
    {
        var awsService = new CountingAwsCliService();
        var azureService = new CountingAzureCliService();
        var viewModel = new MainViewModel(
            new AwsExplorerViewModel(awsService),
            new AzureExplorerViewModel(azureService));

        await viewModel.InitializeVisibleProvidersAsync();
        await viewModel.InitializeVisibleProvidersAsync();

        awsService.ResourceQueryCount.Should().Be(1);
        azureService.ResourceQueryCount.Should().Be(1);
    }

    [Test]
    public async Task InitializeVisibleProvidersAsync_InitializesNewlySelectedProviderWithoutReinitializingOtherProvider()
    {
        var awsService = new CountingAwsCliService();
        var azureService = new CountingAzureCliService();
        var viewModel = new MainViewModel(
            new AwsExplorerViewModel(awsService),
            new AzureExplorerViewModel(azureService))
        {
            SelectedProviderViewMode = ProviderViewMode.Aws,
        };

        await viewModel.InitializeVisibleProvidersAsync();

        awsService.ResourceQueryCount.Should().Be(1);
        azureService.ResourceQueryCount.Should().Be(0);

        viewModel.SelectedProviderViewMode = ProviderViewMode.Azure;
        await viewModel.InitializeVisibleProvidersAsync();

        awsService.ResourceQueryCount.Should().Be(1);
        azureService.ResourceQueryCount.Should().Be(1);

        viewModel.SelectedProviderViewMode = ProviderViewMode.Both;
        await viewModel.InitializeVisibleProvidersAsync();

        awsService.ResourceQueryCount.Should().Be(1);
        azureService.ResourceQueryCount.Should().Be(1);
    }

    [Test]
    public async Task RefreshCommands_InBothView_RefreshOnlyTheirOwnProvider()
    {
        var awsService = new CountingAwsCliService();
        var azureService = new CountingAzureCliService();
        var viewModel = new MainViewModel(
            new AwsExplorerViewModel(awsService),
            new AzureExplorerViewModel(azureService));

        await viewModel.InitializeVisibleProvidersAsync();
        await viewModel.Aws.RefreshCommand.ExecuteAsync(null);

        awsService.ResourceQueryCount.Should().Be(2);
        azureService.ResourceQueryCount.Should().Be(1);

        await viewModel.Azure.RefreshCommand.ExecuteAsync(null);

        awsService.ResourceQueryCount.Should().Be(2);
        azureService.ResourceQueryCount.Should().Be(2);
    }

    private static MainViewModel CreateViewModel() => new(
        new AwsExplorerViewModel(null!),
        new AzureExplorerViewModel(null!));

    private sealed class CountingAwsCliService : IAwsCliService
    {
        public int ResourceQueryCount { get; private set; }

        public Task<IReadOnlyList<CloudAccountOption>> GetProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudAccountOption>>([new("default", "Default", IsDefault: true)]);

        public Task<AuthenticationResult> GetAuthenticationAsync(
            string profile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthenticationResult(true, "AWS identity"));

        public Task<OperationResult> LoginAsync(string profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());

        public Task<ResourceQueryResult> GetResourcesAsync(
            string profile,
            CancellationToken cancellationToken = default)
        {
            ResourceQueryCount++;
            return Task.FromResult(ResourceQueryResult.Success([]));
        }
    }

    private sealed class CountingAzureCliService : IAzureCliService
    {
        public int ResourceQueryCount { get; private set; }

        public Task<IReadOnlyList<CloudAccountOption>> GetSubscriptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudAccountOption>>([new("subscription", "Subscription", IsDefault: true)]);

        public Task<AuthenticationResult> GetAuthenticationAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthenticationResult(true, "Azure identity"));

        public Task<OperationResult> LoginAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());

        public Task<ResourceQueryResult> GetResourcesAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            ResourceQueryCount++;
            return Task.FromResult(ResourceQueryResult.Success([]));
        }
    }
}
