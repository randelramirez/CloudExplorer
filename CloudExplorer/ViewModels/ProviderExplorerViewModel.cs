namespace CloudExplorer.ViewModels;

public abstract partial class ProviderExplorerViewModel : ObservableObject
{
    private readonly List<CloudResource> _resources = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuthenticationRequired))]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _identity = "Not connected";

    [ObservableProperty]
    private string _statusMessage = "Ready to connect";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastRefreshedDisplay))]
    private DateTimeOffset? _lastRefreshedAt;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedGrouping = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResourceCountDisplay))]
    private int _resourceCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    private int _visibleResourceCount;

    [ObservableProperty]
    private IReadOnlyList<ResourceListRow> _rows = [];

    protected ProviderExplorerViewModel(IEnumerable<string> groupingOptions)
    {
        foreach (var grouping in groupingOptions)
        {
            GroupingOptions.Add(grouping);
        }

        SelectedGrouping = GroupingOptions.FirstOrDefault() ?? "";
    }

    public ObservableCollection<string> GroupingOptions { get; } = [];

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsAuthenticationRequired => !IsAuthenticated;

    public bool HasRows => VisibleResourceCount > 0;

    public string ResourceCountDisplay => ResourceCount == 1 ? "1 resource" : $"{ResourceCount:N0} resources";

    public string LastRefreshedDisplay => LastRefreshedAt is null
        ? "Not refreshed yet"
        : $"Updated {LastRefreshedAt.Value.ToLocalTime():g}";

    public abstract Task InitializeAsync();

    public abstract Task ChangeAccountAsync(CloudAccountOption? account);

    partial void OnSearchTextChanged(string value) => RebuildRows();

    partial void OnSelectedGroupingChanged(string value) => RebuildRows();

    protected abstract IEnumerable<string> GetGroupKeys(CloudResource resource);

    protected void ApplyResources(IEnumerable<CloudResource> resources)
    {
        _resources.Clear();
        _resources.AddRange(resources);
        ResourceCount = _resources.Count;
        LastRefreshedAt = DateTimeOffset.Now;
        RebuildRows();
    }

    protected void ClearResources()
    {
        _resources.Clear();
        ResourceCount = 0;
        Rows = [];
        VisibleResourceCount = 0;
        LastRefreshedAt = null;
    }

    protected async Task RunExclusiveAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "Could not complete the operation";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildRows()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _resources
            : _resources
                .Where(resource => resource.SearchableText.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        VisibleResourceCount = filtered.Count;

        var groups = filtered
            .SelectMany(resource => GetGroupKeys(resource)
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .DefaultIfEmpty("Other")
                .Select(key => (Key: key, Resource: resource)))
            .GroupBy(static item => item.Key, static item => item.Resource, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase);

        var rows = new List<ResourceListRow>();
        foreach (var group in groups)
        {
            var groupResources = group
                .OrderBy(static resource => resource.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            rows.Add(ResourceListRow.Header(group.Key, groupResources.Length));
            foreach (var resource in groupResources)
            {
                rows.Add(ResourceListRow.Item(resource));
            }
        }

        Rows = rows;
    }
}
