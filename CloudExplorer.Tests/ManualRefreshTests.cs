using CloudExplorer.Models;
using CloudExplorer.Services;
using CloudExplorer.ViewModels;

namespace CloudExplorer.Tests;

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

    private sealed class FakeAwsCliService : IAwsCliService
    {
        public int ResourceQueryCount { get; private set; }

        public Task<IReadOnlyList<CloudAccountOption>> GetProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudAccountOption>>([new("dev", "dev", IsDefault: true)]);

        public Task<AuthenticationResult> GetAuthenticationAsync(string profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthenticationResult(true, "123 · test-user"));

        public Task<OperationResult> LoginAsync(string profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());

        public Task<ResourceQueryResult> GetResourcesAsync(string profile, CancellationToken cancellationToken = default)
        {
            ResourceQueryCount++;
            IReadOnlyList<CloudResource> resources =
            [
                new CloudResource
                {
                    Provider = CloudProvider.Aws,
                    Id = "arn:aws:s3:::sample-bucket",
                    Name = "sample-bucket",
                    ResourceType = "s3:bucket",
                    GroupName = "s3",
                    Tags = new Dictionary<string, string> { ["Environment"] = "Test" },
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
