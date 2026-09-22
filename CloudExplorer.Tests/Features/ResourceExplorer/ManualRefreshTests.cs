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

public class ManualRefreshTests
{
    [Test]
    public async Task AwsViewModel_QueriesOnceInitially_ThenOnlyOnExplicitRefresh()
    {
        var service = new FakeAwsCliService();
        var viewModel = new AwsExplorerViewModel(service);

        await viewModel.InitializeAsync();
        viewModel.SearchText = "bucket";
        viewModel.SelectedGrouping = AwsExplorerViewModel.GroupByTag;

        service.ResourceQueryCount.Should().Be(1);
        viewModel.ResourceCount.Should().Be(1);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        service.ResourceQueryCount.Should().Be(2);
    }

    [Test]
    public async Task AwsViewModel_GroupsCachedResourcesByTagWithoutCloudCall()
    {
        var service = new FakeAwsCliService();
        var viewModel = new AwsExplorerViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.SelectedGrouping = AwsExplorerViewModel.GroupByTag;

        service.ResourceQueryCount.Should().Be(1);
        viewModel.Rows.Should().Contain(static row => row.IsGroupHeader && row.GroupTitle == "Environment = Test");
    }

    [Test]
    public async Task AzureViewModel_GroupsCachedResourcesWithoutAnotherCloudCall()
    {
        var service = new FakeAzureCliService();
        var viewModel = new AzureExplorerViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.SelectedGrouping = AzureExplorerViewModel.GroupByType;
        viewModel.SearchText = "web";

        service.ResourceQueryCount.Should().Be(1);
        viewModel.Rows.Should().Contain(static row => row.IsGroupHeader && row.GroupTitle == "Microsoft.Web/sites");
    }

    [TestCase("sample-bucket")]
    [TestCase("s3:bucket")]
    [TestCase("storage-service")]
    [TestCase("us-east-1")]
    [TestCase("123456789012")]
    [TestCase("  OWNER=PLATFORM  ")]
    public async Task AwsViewModel_FiltersCachedResourcesAcrossSearchableFields(string searchText)
    {
        var service = new FakeAwsCliService();
        var viewModel = new AwsExplorerViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.SearchText = searchText;

        service.ResourceQueryCount.Should().Be(1);
        viewModel.VisibleResourceCount.Should().Be(1);
    }

    [Test]
    public async Task AwsViewModel_RebuildsSearchIndexWhenCachedResourcesAreRefreshed()
    {
        var service = new FakeAwsCliService(requestCount =>
        {
            var resourceName = requestCount == 1 ? "initial-resource" : "current-resource";
            return
            [
                new CloudResource
                {
                    Provider = CloudProvider.Aws,
                    Id = $"arn:aws:s3:::{resourceName}",
                    Name = resourceName,
                    ResourceType = "s3:bucket",
                    GroupName = "storage-service",
                },
            ];
        });
        var viewModel = new AwsExplorerViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.SearchText = "initial-resource";
        viewModel.VisibleResourceCount.Should().Be(1);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        viewModel.SearchText = "initial-resource";
        viewModel.VisibleResourceCount.Should().Be(0);
        viewModel.SearchText = "current-resource";
        viewModel.VisibleResourceCount.Should().Be(1);
        service.ResourceQueryCount.Should().Be(2);
    }

    [Test]
    public async Task AwsViewModel_FiltersUsingCurrentCachedResourceTags()
    {
        var service = new FakeAwsCliService();
        var viewModel = new AwsExplorerViewModel(service);
        await viewModel.InitializeAsync();

        service.Tags["Owner"] = "Updated";
        viewModel.SearchText = "Owner=Updated";

        service.ResourceQueryCount.Should().Be(1);
        viewModel.VisibleResourceCount.Should().Be(1);
    }

    private sealed class FakeAwsCliService : IAwsCliService
    {
        private readonly Func<int, IReadOnlyList<CloudResource>>? _resourcesFactory;

        public FakeAwsCliService(Func<int, IReadOnlyList<CloudResource>>? resourcesFactory = null)
        {
            _resourcesFactory = resourcesFactory;
        }

        public int ResourceQueryCount { get; private set; }

        public Dictionary<string, string> Tags { get; } = new()
        {
            ["Environment"] = "Test",
            ["Owner"] = "Platform",
        };

        public Task<IReadOnlyList<CloudAccountOption>> GetProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudAccountOption>>([new("dev", "dev", IsDefault: true)]);

        public Task<AuthenticationResult> GetAuthenticationAsync(string profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthenticationResult(true, "123 · test-user"));

        public Task<OperationResult> LoginAsync(string profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());

        public Task<ResourceQueryResult> GetResourcesAsync(string profile, CancellationToken cancellationToken = default)
        {
            ResourceQueryCount++;
            IReadOnlyList<CloudResource> resources = _resourcesFactory?.Invoke(ResourceQueryCount) ??
            [
                new CloudResource
                {
                    Provider = CloudProvider.Aws,
                    Id = "arn:aws:s3:::sample-bucket",
                    Name = "sample-bucket",
                    ResourceType = "s3:bucket",
                    GroupName = "storage-service",
                    Location = "us-east-1",
                    AccountId = "123456789012",
                    Tags = Tags,
                },
            ];
            return Task.FromResult(ResourceQueryResult.Success(resources));
        }
    }

    private sealed class FakeAzureCliService : IAzureCliService
    {
        public int ResourceQueryCount { get; private set; }

        public Task<IReadOnlyList<CloudAccountOption>> GetSubscriptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudAccountOption>>([new("sub-1", "Development", IsDefault: true)]);

        public Task<AuthenticationResult> GetAuthenticationAsync(string subscriptionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthenticationResult(true, "Development · sub-1"));

        public Task<OperationResult> LoginAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());

        public Task<ResourceQueryResult> GetResourcesAsync(string subscriptionId, CancellationToken cancellationToken = default)
        {
            ResourceQueryCount++;
            IReadOnlyList<CloudResource> resources =
            [
                new CloudResource
                {
                    Provider = CloudProvider.Azure,
                    Id = "/subscriptions/sub-1/resourceGroups/rg-web/providers/Microsoft.Web/sites/web-app",
                    Name = "web-app",
                    ResourceType = "Microsoft.Web/sites",
                    GroupName = "rg-web",
                },
            ];
            return Task.FromResult(ResourceQueryResult.Success(resources));
        }
    }
}
