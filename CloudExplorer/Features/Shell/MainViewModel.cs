using System;
using System.Threading.Tasks;
using CloudExplorer.Features.Aws;
using CloudExplorer.Features.Azure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CloudExplorer.Features.Shell;

public enum ProviderViewMode
{
    Both,
    Aws,
    Azure,
}

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ProviderViewMode _selectedProviderViewMode = ProviderViewMode.Both;

    [ObservableProperty]
    private bool _isDarkMode;

    public MainViewModel(AwsExplorerViewModel aws, AzureExplorerViewModel azure)
    {
        Aws = aws;
        Azure = azure;
    }

    public AwsExplorerViewModel Aws { get; }

    public AzureExplorerViewModel Azure { get; }

    public Task InitializeVisibleProvidersAsync() => SelectedProviderViewMode switch
    {
        ProviderViewMode.Both => Task.WhenAll(Aws.InitializeAsync(), Azure.InitializeAsync()),
        ProviderViewMode.Aws => Aws.InitializeAsync(),
        ProviderViewMode.Azure => Azure.InitializeAsync(),
        _ => throw new ArgumentOutOfRangeException(
            nameof(SelectedProviderViewMode),
            SelectedProviderViewMode,
            "Unknown provider view mode."),
    };
}
