using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudExplorer.Models;

namespace CloudExplorer.Services;

public interface IAwsCliService
{
    Task<IReadOnlyList<CloudAccountOption>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<AuthenticationResult> GetAuthenticationAsync(string profile, CancellationToken cancellationToken = default);

    Task<OperationResult> LoginAsync(string profile, CancellationToken cancellationToken = default);

    Task<ResourceQueryResult> GetResourcesAsync(string profile, CancellationToken cancellationToken = default);
}
