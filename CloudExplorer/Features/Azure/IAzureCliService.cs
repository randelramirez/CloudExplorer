using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Features.ResourceExplorer;

namespace CloudExplorer.Features.Azure;

public interface IAzureCliService
{
    Task<IReadOnlyList<CloudAccountOption>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task<AuthenticationResult> GetAuthenticationAsync(string subscriptionId, CancellationToken cancellationToken = default);

    Task<OperationResult> LoginAsync(CancellationToken cancellationToken = default);

    Task<ResourceQueryResult> GetResourcesAsync(string subscriptionId, CancellationToken cancellationToken = default);
}
