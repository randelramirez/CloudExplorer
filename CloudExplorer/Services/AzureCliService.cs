namespace CloudExplorer.Services;

public sealed class AzureCliService(ICommandRunner commandRunner, ILogger<AzureCliService> logger) : IAzureCliService
{
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(10);
    private readonly string _azureExecutable = CliExecutableLocator.Resolve("az");

    public async Task<IReadOnlyList<CloudAccountOption>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync(
            _azureExecutable,
            ["account", "list", "--all", "--output", "json", "--only-show-errors"],
            StatusTimeout,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.BestError);
        }

        try
        {
            return JsonResourceParser.ParseAzureSubscriptions(result.StandardOutput);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Azure CLI returned an unexpected subscription response.");
            throw new InvalidOperationException("Azure CLI returned an unexpected subscription response.", exception);
        }
    }

    public async Task<AuthenticationResult> GetAuthenticationAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        // account show only reads the local Azure CLI cache. Requesting a token verifies that
        // the cached session can still authenticate, without returning the token to this app.
        var tokenResult = await commandRunner.RunAsync(
            _azureExecutable,
            [
                "account", "get-access-token",
                "--subscription", subscriptionId,
                "--query", "expires_on",
                "--output", "tsv",
                "--only-show-errors",
            ],
            StatusTimeout,
            cancellationToken);

        if (!tokenResult.IsSuccess)
        {
            return new AuthenticationResult(false, ErrorMessage: tokenResult.BestError);
        }

        var result = await commandRunner.RunAsync(
            _azureExecutable,
            ["account", "show", "--subscription", subscriptionId, "--output", "json", "--only-show-errors"],
            StatusTimeout,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return new AuthenticationResult(false, ErrorMessage: result.BestError);
        }

        try
        {
            return new AuthenticationResult(true, JsonResourceParser.ParseAzureIdentity(result.StandardOutput));
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Azure CLI returned an unexpected identity response.");
            return new AuthenticationResult(false, ErrorMessage: "Azure CLI returned an unexpected identity response.");
        }
    }

    public async Task<OperationResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync(
            _azureExecutable,
            ["login", "--output", "none", "--only-show-errors"],
            LoginTimeout,
            cancellationToken);
        return result.IsSuccess
            ? OperationResult.Success()
            : OperationResult.Failure(result.BestError);
    }

    public async Task<ResourceQueryResult> GetResourcesAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync(
            _azureExecutable,
            ["resource", "list", "--subscription", subscriptionId, "--output", "json", "--only-show-errors"],
            QueryTimeout,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ResourceQueryResult.Failure(result.BestError);
        }

        try
        {
            return ResourceQueryResult.Success(JsonResourceParser.ParseAzureResources(result.StandardOutput, subscriptionId));
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Azure CLI returned an unexpected resource response.");
            return ResourceQueryResult.Failure("Azure CLI returned an unexpected resource response.");
        }
    }
}
