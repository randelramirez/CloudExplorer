using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CloudExplorer.Features.ResourceExplorer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CloudExplorer.Features.Aws;

public sealed partial class AwsExplorerViewModel : ProviderExplorerViewModel
{
    public const string GroupByType = "Resource type";
    public const string GroupByTag = "Tag";

    private readonly IAwsCliService _service;
    private bool _initialized;

    [ObservableProperty]
    private CloudAccountOption? _selectedAccount;

    public AwsExplorerViewModel(IAwsCliService service)
        : base([GroupByType, GroupByTag])
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
            await LoadProfilesAsync();
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

    protected override IEnumerable<string> GetGroupKeys(CloudResource resource)
    {
        if (SelectedGrouping == GroupByTag)
        {
            return resource.Tags.Count == 0
                ? ["No tags"]
                : resource.Tags.Select(static tag => $"{tag.Key} = {tag.Value}");
        }

        return [resource.ResourceType];
    }

    private Task RefreshAsync() => RunExclusiveAsync(async () =>
    {
        if (Accounts.Count == 0)
        {
            await LoadProfilesAsync();
        }

        if (SelectedAccount is null)
        {
            ErrorMessage = "No AWS CLI profiles were found. Configure a profile before refreshing.";
            StatusMessage = "AWS profile required";
            IsAuthenticated = false;
            return;
        }

        await RefreshCoreAsync();
    });

    private Task LoginAsync() => RunExclusiveAsync(async () =>
    {
        if (SelectedAccount is null)
        {
            ErrorMessage = "Create an AWS CLI profile before signing in.";
            StatusMessage = "AWS profile required";
            return;
        }

        StatusMessage = "Waiting for AWS CLI sign-in…";
        var result = await _service.LoginAsync(SelectedAccount.Id);
        if (!result.IsSuccess)
        {
            IsAuthenticated = false;
            ErrorMessage = result.ErrorMessage;
            StatusMessage = "AWS sign-in was not completed";
            return;
        }

        await RefreshCoreAsync();
    });

    private async Task LoadProfilesAsync()
    {
        StatusMessage = "Reading AWS CLI profiles…";
        var profiles = await _service.GetProfilesAsync();
        ReplaceItems(Accounts, profiles);
        SelectedAccount = GetPreferredAccount(Accounts);
    }

    private async Task RefreshCoreAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        StatusMessage = "Checking AWS CLI session…";
        var authentication = await _service.GetAuthenticationAsync(SelectedAccount.Id);
        IsAuthenticated = authentication.IsAuthenticated;
        Identity = authentication.IsAuthenticated ? authentication.Identity : "Sign-in required";
        if (!authentication.IsAuthenticated)
        {
            ErrorMessage = authentication.ErrorMessage;
            StatusMessage = "Authenticate with AWS CLI to continue";
            ClearResources();
            return;
        }

        StatusMessage = "Loading AWS resources…";
        var result = await _service.GetResourcesAsync(SelectedAccount.Id);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            StatusMessage = "AWS resources could not be loaded";
            return;
        }

        ApplyResources(result.Resources);
        StatusMessage = result.Resources.Count == 0
            ? "No resources found in the Resource Explorer view"
            : $"Loaded {result.Resources.Count:N0} AWS resources";
    }
}
