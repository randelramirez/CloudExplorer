namespace CloudExplorer;

public sealed partial class MainPage : Page
{
    private bool _loaded;

    public MainPage()
    {
        InitializeComponent();
        DataContext = ((App)Application.Current).Services.GetRequiredService<MainViewModel>();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private async void PageLoaded(object sender, RoutedEventArgs args)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        ViewModel.IsDarkMode = ActualTheme == ElementTheme.Dark;
        await ViewModel.Aws.InitializeAsync();
    }

    private async void ProviderTabSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_loaded && ProviderTabs.SelectedIndex == 1)
        {
            await ViewModel.Azure.InitializeAsync();
        }
    }

    private void ThemeToggled(object sender, RoutedEventArgs args)
    {
        RequestedTheme = ViewModel.IsDarkMode ? ElementTheme.Dark : ElementTheme.Light;
    }

    private async void FullScreenComparisonClicked(object sender, RoutedEventArgs args)
    {
        ViewModel.IsFullScreenComparison = !ViewModel.IsFullScreenComparison;
        ((App)Application.Current).TrySetFullScreen(ViewModel.IsFullScreenComparison);

        if (ViewModel.IsFullScreenComparison)
        {
            await Task.WhenAll(
                ViewModel.Aws.InitializeAsync(),
                ViewModel.Azure.InitializeAsync());
        }
    }
}
