using System.Collections.Generic;

namespace CloudExplorer.Features.ResourceExplorer;

public sealed record AuthenticationResult(
    bool IsAuthenticated,
    string Identity = "",
    string? ErrorMessage = null);

public sealed record ResourceQueryResult(
    bool IsSuccess,
    IReadOnlyList<CloudResource> Resources,
    string? ErrorMessage = null)
{
    public static ResourceQueryResult Failure(string message) => new(false, [], message);

    public static ResourceQueryResult Success(IReadOnlyList<CloudResource> resources) => new(true, resources);
}

public sealed record OperationResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static OperationResult Success() => new(true);

    public static OperationResult Failure(string message) => new(false, message);
}
