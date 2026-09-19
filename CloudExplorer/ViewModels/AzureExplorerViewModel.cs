namespace CloudExplorer.ViewModels;

public sealed partial class AzureExplorerViewModel : ProviderExplorerViewModel
{
    public const string GroupByResourceGroup = "Resource group";
    public const string GroupByType = "Resource type";

    private readonly IAzureCliService _service;
    private bool _initialized;

    [ObservableProperty]
    private CloudAccountOption? _selectedAccount;

    public AzureExplorerViewModel(IAzureCliService service)
        : base([GroupByResourceGroup, GroupByType])
    {
        _service = service;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoginCommand = new AsyncRelayCommand(LoginAsync);
    }

    public ObservableCollection<CloudAccountOption> Accounts { get; } = [];

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand LoginCommand { get; }

    public override async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await RunExclusiveAsync(async () =>
        {
            await LoadSubscriptionsAsync();
            if (SelectedAccount is not null)
            {
                await RefreshCoreAsync();
            }
        });
    }

    public override async Task ChangeAccountAsync(CloudAccountOption? account)
    {
        if (account is null || account == SelectedAccount)
        {
            return;
        }

        SelectedAccount = account;
        ClearResources();
        await RunExclusiveAsync(RefreshCoreAsync);
    }

    protected override IEnumerable<string> GetGroupKeys(CloudResource resource) =>
        SelectedGrouping == GroupByType ? [resource.ResourceType] : [resource.GroupName];

    private Task RefreshAsync() => RunExclusiveAsync(async () =>
    {
        if (Accounts.Count == 0)
        {
            await LoadSubscriptionsAsync();
        }

        if (SelectedAccount is null)
        {
            IsAuthenticated = false;
            StatusMessage = "Authenticate with Azure CLI to continue";
            return;
        }

        await RefreshCoreAsync();
    });

    private Task LoginAsync() => RunExclusiveAsync(async () =>
    {
        StatusMessage = "Waiting for Azure CLI sign-in…";
        var result = await _service.LoginAsync();
        if (!result.IsSuccess)
        {
            IsAuthenticated = false;
            ErrorMessage = result.ErrorMessage;
            StatusMessage = "Azure sign-in was not completed";
            return;
        }

        await LoadSubscriptionsAsync();
        if (SelectedAccount is not null)
        {
            await RefreshCoreAsync();
        }
    });

    private async Task LoadSubscriptionsAsync()
    {
        StatusMessage = "Reading Azure CLI subscriptions…";
        try
        {
            var subscriptions = await _service.GetSubscriptionsAsync();
            Accounts.Clear();
            foreach (var subscription in subscriptions)
            {
                Accounts.Add(subscription);
            }

            SelectedAccount = Accounts.FirstOrDefault(static account => account.IsDefault) ?? Accounts.FirstOrDefault();
            IsAuthenticated = SelectedAccount is not null;
            if (SelectedAccount is null)
            {
                StatusMessage = "Authenticate with Azure CLI to continue";
            }
        }
        catch (InvalidOperationException exception)
        {
            Accounts.Clear();
            SelectedAccount = null;
            IsAuthenticated = false;
            Identity = "Sign-in required";
            ErrorMessage = exception.Message;
            StatusMessage = "Authenticate with Azure CLI to continue";
        }
    }

    private async Task RefreshCoreAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        StatusMessage = "Checking Azure CLI session…";
        var authentication = await _service.GetAuthenticationAsync(SelectedAccount.Id);
        IsAuthenticated = authentication.IsAuthenticated;
        Identity = authentication.IsAuthenticated ? authentication.Identity : "Sign-in required";
        if (!authentication.IsAuthenticated)
        {
            ErrorMessage = authentication.ErrorMessage;
            StatusMessage = "Authenticate with Azure CLI to continue";
            ClearResources();
            return;
        }

        StatusMessage = "Loading Azure resources…";
        var result = await _service.GetResourcesAsync(SelectedAccount.Id);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            StatusMessage = "Azure resources could not be loaded";
            return;
        }

        ApplyResources(result.Resources);
        StatusMessage = result.Resources.Count == 0
            ? "No resources found in this subscription"
            : $"Loaded {result.Resources.Count:N0} Azure resources";
    }
}
