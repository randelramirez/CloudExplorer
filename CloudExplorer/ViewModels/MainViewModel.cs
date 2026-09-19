namespace CloudExplorer.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullScreenButtonText))]
    private bool _isFullScreenComparison;

    [ObservableProperty]
    private bool _isDarkMode;

    public MainViewModel(AwsExplorerViewModel aws, AzureExplorerViewModel azure)
    {
        Aws = aws;
        Azure = azure;
    }

    public AwsExplorerViewModel Aws { get; }

    public AzureExplorerViewModel Azure { get; }

    public string FullScreenButtonText => IsFullScreenComparison ? "Exit full screen" : "Full screen compare";
}
